using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using CNCMaps.Engine;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.Map;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace CNCMaps.Tests {

	/// <summary>
	/// End-to-end regression tests: render the committed test maps against the real
	/// game data and compare pixel hashes with the goldens in TestAssets/golden.json.
	/// Requires the CNCMAPS_MIX_DIR environment variable pointing at a directory with
	/// the RA2/YR mix files; the tests are skipped when it is not set. Note the
	/// goldens are only valid for unmodified original game data.
	/// </summary>
	public class GoldenRenderTests {

		public static string MixDir {
			get {
				var dir = Environment.GetEnvironmentVariable("CNCMAPS_MIX_DIR");
				return !string.IsNullOrEmpty(dir) && Directory.Exists(dir) ? dir : null;
			}
		}

		public static string TsMixDir {
			get {
				var dir = Environment.GetEnvironmentVariable("CNCMAPS_TS_MIX_DIR");
				return !string.IsNullOrEmpty(dir) && Directory.Exists(dir) ? dir : null;
			}
		}

		public sealed class GoldenFactAttribute : FactAttribute {
			public GoldenFactAttribute() {
				if (MixDir == null)
					Skip = "CNCMAPS_MIX_DIR is not set or does not exist";
			}
		}

		public sealed class TsGoldenFactAttribute : FactAttribute {
			public TsGoldenFactAttribute() {
				if (TsMixDir == null)
					Skip = "CNCMAPS_TS_MIX_DIR is not set or does not exist";
			}
		}

		static string AssetPath(string relative) =>
			Path.Combine(AppContext.BaseDirectory, "TestAssets", relative);

		static Dictionary<string, string> LoadGoldens() {
			var path = AssetPath("golden.json");
			if (!File.Exists(path)) return new Dictionary<string, string>();
			return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
		}

		static string Render(string mapName, Action<RenderSettings> configure = null) {
			string outDir = Path.Combine(Path.GetTempPath(), "cncmaps-tests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(outDir);
			// work on a copy: some render options write back into the map file
			string mapCopy = Path.Combine(outDir, mapName);
			File.Copy(AssetPath(Path.Combine("maps", mapName)), mapCopy);

			var settings = new RenderSettings {
				InputFile = mapCopy,
				OutputDir = outDir,
				OutputFile = "render",
				MixFilesDirectory = MixDir,
				SavePNG = true,
			};
			configure?.Invoke(settings);

			var engine = new RenderEngine();
			Assert.True(engine.ConfigureFromSettings(settings), "engine configuration failed");
			var result = engine.Execute();
			Assert.Equal(EngineResult.RenderedOk, result);
			return outDir;
		}

		/// <summary>Hash of the decoded pixel data, independent of the PNG encoding.</summary>
		static string PixelHash(string pngPath) {
			using var img = SixLabors.ImageSharp.Image.Load<Rgb24>(pngPath);
			using var sha = SHA256.Create();
			var header = System.Text.Encoding.ASCII.GetBytes($"{img.Width}x{img.Height}:");
			sha.TransformBlock(header, 0, header.Length, null, 0);
			img.ProcessPixelRows(accessor => {
				for (int y = 0; y < accessor.Height; y++) {
					var row = MemoryMarshal.AsBytes(accessor.GetRowSpan(y)).ToArray();
					sha.TransformBlock(row, 0, row.Length, null, 0);
				}
			});
			sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
			return Convert.ToHexString(sha.Hash).ToLowerInvariant();
		}

		static void AssertGolden(string key, string actualHash, string outDir) {
			var goldens = LoadGoldens();
			if (!goldens.TryGetValue(key, out var expected))
				Assert.Fail($"no golden for '{key}'; actual hash: {actualHash} (render kept in {outDir})");
			if (expected != actualHash)
				Assert.Fail($"golden mismatch for '{key}': expected {expected}, actual {actualHash} (render kept in {outDir})");
			// best-effort: the engine currently keeps the map file handle open
			try { Directory.Delete(outDir, true); }
			catch (IOException) { }
		}

		[GoldenFact]
		public void Ra2SnowMap_RendersGoldenPixels() {
			var outDir = Render("mp22s8.map");
			AssertGolden("mp22s8-png", PixelHash(Path.Combine(outDir, "render.png")), outDir);
		}

		[GoldenFact]
		public void YrUrbanMap_RendersGoldenPixels() {
			var outDir = Render("hillbtwn.map");
			AssertGolden("hillbtwn-png", PixelHash(Path.Combine(outDir, "render.png")), outDir);
		}

		[GoldenFact]
		public void YrVehicleHeavyMap_RendersGoldenPixels() {
			var outDir = Render("austintx.map");
			AssertGolden("austintx-png", PixelHash(Path.Combine(outDir, "render.png")), outDir);
		}

		[GoldenFact]
		public void StartPositionMarkers_RenderGoldenPixels() {
			var outDir = Render("hillbtwn.map", s => {
				s.MarkStartPos = true;
				s.StartPositionMarking = StartPositionMarking.Squared;
			});
			AssertGolden("hillbtwn-markers-png", PixelHash(Path.Combine(outDir, "render.png")), outDir);
		}

		[GoldenFact]
		public void PreviewPackInjection_ProducesGoldenData() {
			var outDir = Render("hillbtwn.map", s => {
				s.GeneratePreviewPack = true;
				s.PreviewMarkers = PreviewMarkersType.Bittah;
				s.Backup = false;
			});
			// hash the injected [PreviewPack] section of the modified map copy
			var lines = File.ReadAllLines(Path.Combine(outDir, "hillbtwn.map"));
			int start = Array.FindIndex(lines, l => l.Trim() == "[PreviewPack]");
			Assert.True(start >= 0, "no [PreviewPack] section was injected");
			var section = lines.Skip(start + 1).TakeWhile(l => !l.TrimStart().StartsWith("[")).Where(l => l.Contains('='));
			var data = string.Concat(section.Select(l => l.Substring(l.IndexOf('=') + 1).Trim()));
			var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(data))).ToLowerInvariant();
			AssertGolden("hillbtwn-previewpack", hash, outDir);
		}

		[GoldenFact]
		public void RepeatedInProcessRenders_StayGolden() {
			// a long-running service worker renders many maps in one process; verify
			// renders are not contaminated by earlier renders (shared caches, statics)
			var goldens = LoadGoldens();
			var first = Render("hillbtwn.map");
			AssertGolden("hillbtwn-png", PixelHash(Path.Combine(first, "render.png")), first);
			var other = Render("mp22s8.map");
			AssertGolden("mp22s8-png", PixelHash(Path.Combine(other, "render.png")), other);
			var again = Render("hillbtwn.map");
			AssertGolden("hillbtwn-png", PixelHash(Path.Combine(again, "render.png")), again);
		}

		[GoldenFact]
		public void MapIniRuleOverrides_AffectTheRender() {
			// maps can override rules/art entries in their own ini; verify the merge
			// still takes effect: swapping the ambulance image must change the output
			string overrideDir = Path.Combine(Path.GetTempPath(), "cncmaps-tests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(overrideDir);
			string modMap = Path.Combine(overrideDir, "hillbtwn_override.map");
			File.Copy(AssetPath(Path.Combine("maps", "hillbtwn.map")), modMap);
			File.AppendAllText(modMap, "\r\n[AMBU]\r\nImage=CAR\r\n");

			string outDir = Path.Combine(overrideDir, "out");
			Directory.CreateDirectory(outDir);
			var settings = new RenderSettings {
				InputFile = modMap,
				OutputDir = outDir,
				OutputFile = "render",
				MixFilesDirectory = MixDir,
				SavePNG = true,
			};
			var engine = new RenderEngine();
			Assert.True(engine.ConfigureFromSettings(settings));
			Assert.Equal(EngineResult.RenderedOk, engine.Execute());

			var hash = PixelHash(Path.Combine(outDir, "render.png"));
			var goldens = LoadGoldens();
			Assert.NotEqual(goldens["hillbtwn-png"], hash); // override took effect
			AssertGolden("hillbtwn-override-png", hash, overrideDir);
		}

		[GoldenFact]
		public void EngineDetection_DetectsCorrectEngines() {
			foreach (var (map, expected) in new[] { ("mp22s8.map", EngineType.RedAlert2), ("hillbtwn.map", EngineType.YurisRevenge), ("austintx.map", EngineType.YurisRevenge) }) {
				var detected = Detect(map, MixDir);
				Assert.Equal(expected, detected);
			}
		}

		[GoldenFact]
		public void EngineDetection_YrmExtensionBreaksTie() {
			// mp22s8 uses only RA2-era objects, so RA2 and YR both score a perfect 1.0 and probe
			// order decides. The FinalAlert2 YR extension must hand the tie to YR.
			Assert.Equal(EngineType.RedAlert2, Detect("mp22s8.map", MixDir));
			Assert.Equal(EngineType.YurisRevenge, Detect("mp22s8.map", MixDir, mapName: "mp22s8.yrm"));
		}

		[TsGoldenFact]
		public void EngineDetection_TsMapAgainstTsDirIsNotRa2() {
			// The TS and RA2 games share the cache.mix/local.mix names, so an unguarded RA2 probe
			// on a TS-only directory reads TS's own rules and scores 1.0.
			Assert.Equal(EngineType.TiberianSun, Detect("arivruns.map", TsMixDir));
		}

		static EngineType Detect(string map, string mixDir, string mapName = null) {
			using var stream = File.OpenRead(AssetPath(Path.Combine("maps", map)));
			var vmapFile = new VirtualFile(stream, mapName ?? map, true);
			var mapFile = new MapFile(vmapFile, mapName ?? map);
			return CNCMaps.Engine.Map.EngineDetector.DetectEngineType(mapFile, mixDir);
		}
	}
}
