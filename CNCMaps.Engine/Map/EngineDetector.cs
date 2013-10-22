using System;
using System.IO;
using System.Linq;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Utility;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.Map;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;
using NLog;

namespace CNCMaps.Engine.Map {
	public class EngineDetector {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary>Detect map type.</summary>
		/// <param name="rules">The rules.ini file to be used.</param>
		/// <returns>The engine to be used to render this map.</returns>
		public static EngineType DetectEngineType(MapFile mf) {
			var vfsTS = new VFS();
			var vfsFS = new VFS();
			var vfsRA2 = new VFS();
			var vfsYR = new VFS();

			if (Directory.Exists(VFS.TSInstallDir)) {
				vfsTS.ScanMixDir(VFS.TSInstallDir, EngineType.TiberianSun);
				vfsFS.ScanMixDir(VFS.TSInstallDir, EngineType.Firestorm);
			}

			if (Directory.Exists(VFS.RA2InstallDir)) {
				vfsRA2.ScanMixDir(VFS.RA2InstallDir, EngineType.RedAlert2);
				vfsYR.ScanMixDir(VFS.RA2InstallDir, EngineType.YurisRevenge);
			}

			IniFile rulesTS = vfsTS.OpenFile<IniFile>("rules.ini");
			IniFile rulesFS = vfsFS.OpenFile<IniFile>("rules.ini");
			if (rulesFS != null)
				rulesFS.MergeWith(vfsFS.OpenFile<IniFile>("firestrm.ini"));

			IniFile rulesRA2 = vfsRA2.OpenFile<IniFile>("rules.ini");
			IniFile rulesYR = vfsYR.OpenFile<IniFile>("rulesmd.ini");

			TheaterType theater = Theater.TheaterTypeFromString(mf.ReadString("Map", "Theater"));
			TheaterSettings thsTS = ModConfig.DefaultsTS.GetTheater(theater);
			TheaterSettings thsFS = ModConfig.DefaultsFS.GetTheater(theater);
			TheaterSettings thsRA2 = ModConfig.DefaultsRA2.GetTheater(theater);
			TheaterSettings thsYR = ModConfig.DefaultsYR.GetTheater(theater);

			if (thsTS != null)
				foreach (var f in thsTS.Mixes)
					vfsTS.AddFile(f);

			if (thsFS != null)
				foreach (var f in thsFS.Mixes)
					vfsFS.AddFile(f);

			if (thsRA2 != null)
				foreach (var f in thsRA2.Mixes)
					vfsRA2.AddFile(f);

			if (thsYR != null)
				foreach (var f in thsYR.Mixes)
					vfsYR.AddFile(f);

			var ret = DetectEngineFromRules(mf, rulesTS, rulesFS, rulesRA2, rulesYR, thsTS, thsFS, thsRA2, thsYR, vfsTS, vfsFS, vfsRA2, vfsYR);
			Logger.Debug("Engine type detected as {0}", ret);
			return ret;
		}

		private static EngineType DetectEngineFromRules(MapFile mf,
			IniFile rulesTS, IniFile rulesFS, IniFile rulesRA2, IniFile rulesYR,
			TheaterSettings theaterTS, TheaterSettings theaterFS, TheaterSettings theaterRA2, TheaterSettings theaterYR,
			VFS vfsTS, VFS vfsFS, VFS vfsRA2, VFS vfsYR) {

			double tsScore = PercentageObjectsKnown(mf, vfsTS, rulesTS, theaterTS);
			double fsScore = PercentageObjectsKnown(mf, vfsFS, rulesFS, theaterFS);
			double ra2Score = PercentageObjectsKnown(mf, vfsRA2, rulesRA2, theaterRA2);
			double yrScore = PercentageObjectsKnown(mf, vfsYR, rulesYR, theaterYR);

			double maxScore = Math.Max(Math.Max(Math.Max(tsScore, fsScore), ra2Score), yrScore);
			if (maxScore == ra2Score) return EngineType.RedAlert2;
			else if (maxScore == yrScore) return EngineType.YurisRevenge;
			else if (maxScore == tsScore) return EngineType.TiberianSun;
			else if (maxScore == fsScore) return EngineType.Firestorm;
			return EngineType.YurisRevenge; // default
		}

		private static double PercentageObjectsKnown(MapFile mf, VFS vfs, IniFile rules, TheaterSettings ths) {
			if (rules == null || ths == null) return 0.0;
			var theaterIni = vfs.OpenFile<IniFile>(ths.TheaterIni);
			if (theaterIni == null) return 0.0;

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

			var tiles = mf.Tiles.Where(t => t != null).DistinctBy(t => t.TileNum);
			var tilesCollection = new TileCollection(ths, vfs.OpenFile<IniFile>(ths.TheaterIni));
			tilesCollection.InitTilesets();
			known += mf.Tiles.Count(o => o.TileNum <= tilesCollection.NumTiles);
			total += mf.Tiles.Count();

			var infs = mf.Infantries.DistinctBy(o => o.Name);
			known += infs.Count(o => objectKnown(o, rules.GetSection("InfantryTypes")));
			total += infs.Count();

			var terrains = mf.Infantries.DistinctBy(o => o.Name);
			known += terrains.Count(o => objectKnown(o, rules.GetSection("TerrainTypes")));
			total += terrains.Count();

			var units = mf.Infantries.DistinctBy(o => o.Name);
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
