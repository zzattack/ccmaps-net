using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Utility;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.Map;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;
using CNCMaps.Shared.Utility;
using NLog;

namespace CNCMaps.Engine.Map {
	public class EngineDetector {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Everything needed to score one (engine, theater) combination. The contents
		/// are game data only - fully independent of any map - so probes are cached
		/// and reused across renders in long-running processes (GUI, render service).
		/// </summary>
		private class ProbeContext {
			public VirtualFileSystem Vfs;
			public IniFile Rules;
			public TheaterSettings TheaterSettings;
			public TileCollection TileCollection;
		}

		private static readonly object CacheLock = new object();
		private static readonly Dictionary<(string dir, EngineType engine, TheaterType theater), ProbeContext> ProbeCache
			= new Dictionary<(string, EngineType, TheaterType), ProbeContext>();

		/// <summary>Preloads the probe contexts for all engines available in a mix
		/// directory, so later detections only need to score. Optional; detection
		/// fills the cache on demand as well.</summary>
		public static void Preload(string mixDir, TheaterType theater) {
			foreach (var engine in new[] { EngineType.RedAlert2, EngineType.YurisRevenge, EngineType.TiberianSun, EngineType.Firestorm })
				GetProbeContext(mixDir, engine, theater);
		}

		private static ProbeContext GetProbeContext(string mixDir, EngineType engine, TheaterType theater) {
			string dir = !string.IsNullOrEmpty(mixDir) ? mixDir
				: engine <= EngineType.Firestorm ? VirtualFileSystem.TSInstallDir : VirtualFileSystem.RA2InstallDir;
			if (dir == null || !Directory.Exists(dir)) return null;

			var key = (Path.GetFullPath(dir).ToLowerInvariant(), engine, theater);
			lock (CacheLock) {
				if (ProbeCache.TryGetValue(key, out var cached))
					return cached;
			}

			var ctx = new ProbeContext();
			var vfs = new VirtualFileSystem();
			vfs.LoadMixes(dir, engine);
			ctx.Vfs = vfs;

			// cache.mix and local.mix exist under the same names in both the TS and RA2 games, so
			// a probe against a directory holding only the other game would quietly score the map
			// against that game's rules. Require the engine's own main mix, unless the directory
			// contains no main mix at all (loose-file mod setups).
			string mainMix = engine >= EngineType.RedAlert2
				? (engine == EngineType.YurisRevenge ? "ra2md.mix" : "ra2.mix") : "tibsun.mix";
			bool anyMainMix = vfs.FileExists("ra2.mix") || vfs.FileExists("ra2md.mix") || vfs.FileExists("tibsun.mix");
			if (anyMainMix && !vfs.FileExists(mainMix)) {
				Logger.Debug("Skipping {0} probe: {1} not present in mix directory", engine, mainMix);
				lock (CacheLock) {
					ProbeCache[key] = null;
				}
				return null;
			}

			switch (engine) {
				case EngineType.TiberianSun:
				case EngineType.RedAlert2:
					ctx.Rules = vfs.OpenFile<IniFile>("rules.ini");
					break;
				case EngineType.Firestorm:
					ctx.Rules = vfs.OpenFile<IniFile>("rules.ini");
					if (ctx.Rules != null)
						ctx.Rules.MergeWith(vfs.OpenFile<IniFile>("firestrm.ini"));
					break;
				case EngineType.YurisRevenge:
					ctx.Rules = vfs.OpenFile<IniFile>("rulesmd.ini");
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(engine));
			}

			ctx.TheaterSettings = ModConfig.GetDefaultConfig(engine).GetTheater(theater);
			if (ctx.TheaterSettings != null) {
				foreach (var f in ctx.TheaterSettings.Mixes)
					vfs.AddItem(f);

				var theaterIni = vfs.OpenFile<IniFile>(ctx.TheaterSettings.TheaterIni);
				if (ctx.Rules != null && theaterIni != null) {
					ctx.TileCollection = new TileCollection(ctx.TheaterSettings.Type, null, vfs, null, null, ctx.TheaterSettings, theaterIni);
					ctx.TileCollection.InitTilesets();
				}
			}

			lock (CacheLock) {
				ProbeCache[key] = ctx;
			}
			return ctx;
		}

		/// <summary>Detect map type.</summary>
		/// <returns>The engine to be used to render this map.</returns>
		public static EngineType DetectEngineType(MapFile mf, string mixDir = null, string inputFile = null) {
			TheaterType theater = Theater.TheaterTypeFromString(mf.ReadString("Map", "Theater"));

			// FinalAlert2 extensions identify the target game (.mpr = RA2, .yrm/.yro = YR), while
			// .map and .mmx are used by every engine and mean nothing. Probing the indicated
			// engine first lets it claim the early exit over the equally perfect score of its
			// sibling, which knows most of the same objects.
			string ext = Path.GetExtension(inputFile ?? mf.FileName ?? string.Empty).ToLowerInvariant();

			// scores cannot exceed 1.0 and ties are resolved in this same order, so the
			// first engine whose data knows every object on the map wins outright and
			// the remaining probes can be skipped
			var order = ext == ".yrm" || ext == ".yro"
				? new[] { EngineType.YurisRevenge, EngineType.RedAlert2, EngineType.TiberianSun, EngineType.Firestorm }
				: new[] { EngineType.RedAlert2, EngineType.YurisRevenge, EngineType.TiberianSun, EngineType.Firestorm };
			var scores = new double[order.Length];
			for (int i = 0; i < order.Length; i++) {
				var ctx = GetProbeContext(mixDir, order[i], theater);
				scores[i] = ctx == null ? 0.0 : PercentageObjectsKnown(mf, ctx);
				Logger.Debug("Engine {0} scores {1:P1}", order[i], scores[i]);
				if (scores[i] == 1.0) {
					Logger.Debug("Engine type detected as {0}", order[i]);
					return order[i];
				}
			}

			// highest score wins; ties go to the earliest probe
			double maxScore = scores.Max();
			EngineType ret = order[Array.IndexOf(scores, maxScore)];
			Logger.Debug("Engine type detected as {0}", ret);
			return ret;
		}

		private static double PercentageObjectsKnown(MapFile mf, ProbeContext ctx) {
			if (ctx.Rules == null || ctx.TheaterSettings == null || ctx.TileCollection == null) return 0.0;
			var rules = ctx.Rules;

			Func<MapObject, IniFile.IniSection, bool> objectKnown = (obj, section) => {
				if (obj is NamedMapObject) {
					string name = (obj as NamedMapObject).Name;
					return section.OrderedEntries.Any(kvp => kvp.Value.ToString().Equals(name, StringComparison.InvariantCultureIgnoreCase));
				}
				else if (obj is NumberedMapObject) {
					int number = (obj as NumberedMapObject).Number;
					return section.HasKey(number.ToString());
				}
				return false; // should not happen
			};

			int known = 0;
			int total = 0;

			known += mf.Tiles.Count(o => o.TileNum <= ctx.TileCollection.NumTiles);
			total += mf.Tiles.Count();

			var infs = mf.Infantries.DistinctBy(o => o.Name);
			known += infs.Count(o => objectKnown(o, rules.GetSection("InfantryTypes")));
			total += infs.Count();

			var terrains = mf.Terrains.DistinctBy(o => o.Name);
			known += terrains.Count(o => objectKnown(o, rules.GetSection("TerrainTypes")));
			total += terrains.Count();

			var units = mf.Units.DistinctBy(o => o.Name);
			known += units.Count(o => objectKnown(o, rules.GetSection("VehicleTypes")));
			total += units.Count();

			var aircrafts = mf.Aircrafts.DistinctBy(o => o.Name);
			known += aircrafts.Count(o => objectKnown(o, rules.GetSection("AircraftTypes")));
			total += aircrafts.Count();

			var smudges = mf.Smudges.DistinctBy(o => o.Name);
			known += smudges.Count(o => objectKnown(o, rules.GetSection("SmudgeTypes")));
			total += smudges.Count();

			var structures = mf.Structures.DistinctBy(o => o.Name);
			known += structures.Count(o => objectKnown(o, rules.GetSection("BuildingTypes"))
				|| objectKnown(o, rules.GetSection("OverlayTypes")));
			total += structures.Count();

			var overlays = mf.Overlays.DistinctBy(o => o.Number);
			known += overlays.Count(o => objectKnown(o, rules.GetSection("OverlayTypes")));
			total += overlays.Count();


			return known / (double)total;
		}

	}
}
