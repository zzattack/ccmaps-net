using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using NLog;

namespace CNCMaps.Shared {

	public class RenderSettings {
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		public string InputFile { get; set; }
		public string OutputFile { get; set; }
		public string OutputDir { get; set; }
		public bool SavePNG { get; set; }
		public bool SaveJPEG { get; set; }
		public int PNGQuality { get; set; }
		public int JPEGCompression { get; set; }
		public List<string> MixFilesDirectories { get; set; }
		public string ModConfig { get; set; }
		public string MetadataOutFile { get; set; }
		public bool ShowHelp { get; set; }
		public bool MarkOreFields { get; set; }
		public bool IgnoreLighting { get; set; }
		public SizeMode SizeMode { get; set; }
		public EngineType Engine { get; set; }
		public StartPositionMarking StartPositionMarking;
		public bool MarkStartPos { get; set; }
		public double? MarkerStartSize { get; set; }
		public bool PreferOSMesa { get; set; }
		public string ThumbnailConfig { get; set; }
		public StartPositionMarking ThumbnailMarkers { get; set; }
		public bool FixupTiles { get; set; }
		public bool GeneratePreviewPack { get; set; }
		public PreviewMarkersType PreviewMarkers { get; set; }
		public bool SavePNGThumbnails { get; set; }
		public bool FixPreviewDimensions { get; set; }
		public bool Debug { get; set; }
		public string DebugZBufferFile { get; set; }
		public string DebugVoxelMaskFile { get; set; }
		public string DebugTilesFile { get; set; }
		public string TileLattice { get; set; }
		public bool PinRandomDraws { get; set; }
		public int AnimFrame { get; set; }
		public string[] PreCaptureColors { get; set; }
		public bool ReportProgress { get; set; }
		public bool MarkIceGrowth { get; set; }
		public bool Backup { get; set; }
		public bool FixOverlays { get; set; }
		public bool CompressTiles { get; set; }
		public bool TunnelPaths { get; set; }
		public bool TunnelPosition { get; set; }

		public RenderSettings() {
			PNGQuality = 4; // deflate level; with unfiltered scanlines this compresses game graphics best for its speed
			SavePNG = false;
			JPEGCompression = 95;
			SaveJPEG = false;
			ShowHelp = false;
			MarkOreFields = false;
			Engine = EngineType.AutoDetect;
			StartPositionMarking = StartPositionMarking.None;
			InputFile = "";
			OutputDir = "";
			OutputFile = "";
			MixFilesDirectories = new List<string>();
			ModConfig = "";
			MetadataOutFile = "";
			ThumbnailConfig = "";
			ThumbnailMarkers = StartPositionMarking.None;
			SavePNGThumbnails = false;
			SizeMode = SizeMode.Auto;
			FixPreviewDimensions = true;
			Debug = false;
			DebugZBufferFile = "";
			DebugVoxelMaskFile = "";
			TileLattice = "";
			PinRandomDraws = false;
			AnimFrame = -1;
			PreCaptureColors = (string[])DefaultPreCaptureColors.Clone();
			MarkIceGrowth = false;
			Backup = false;
			FixOverlays = false;
			CompressTiles = false;
			TunnelPaths = false;
			TunnelPosition = false;
			MarkStartPos = false;
		}

		/// <summary>The game's own multiplayer colour order: start position N carries colour N.
		/// Read off captures whose spawn pinned one player per start position.</summary>
		public static readonly string[] DefaultPreCaptureColors =
			{ "Gold", "DarkRed", "DarkBlue", "DarkGreen", "Orange", "DarkSky", "Purple", "Magenta" };

		private static string[] ParsePreCaptureColors(string value) {
			if (string.IsNullOrWhiteSpace(value)) return (string[])DefaultPreCaptureColors.Clone();
			if (value.Trim().Equals("none", StringComparison.OrdinalIgnoreCase)) return null;
			var parts = value.Split(',');
			var colors = new string[DefaultPreCaptureColors.Length];
			for (int i = 0; i < colors.Length && i < parts.Length; i++) {
				string name = parts[i].Trim();
				if (name.Length > 0) colors[i] = name;
			}
			return colors;
		}

		private readonly List<(string invocation, string description)> _helpEntries = new List<(string, string)>();

		public void ConfigureFromArgs(string[] args) {
			var applications = new List<Action<ParseResult>>();
			var root = new RootCommand("Renders battle maps of RA2/YR and TS/FS to images") {
				TreatUnmatchedTokensAsErrors = false,
			};
			// the built-in help/version machinery is bypassed: -h only sets ShowHelp, and
			// the caller decides when to print GetHelpText()
			for (int i = root.Options.Count - 1; i >= 0; i--)
				root.Options.RemoveAt(i);

			void Register<T>(Option<T> option, string description, Action<ParseResult, Option<T>> apply) {
				option.Description = description;
				root.Options.Add(option);
				var names = option.Aliases.Concat(new[] { option.Name }).OrderBy(n => n.Length).ToList();
				string valueSuffix = option.ValueType == typeof(bool) ? "" : "=VALUE";
				_helpEntries.Add((string.Join(", ", names) + valueSuffix, description));
				applications.Add(r => apply(r, option));
			}
			void Flag(string name, string alias, string description, Action apply) {
				var option = alias != null ? new Option<bool>(name, alias) : new Option<bool>(name);
				Register(option, description, (r, o) => {
					if (r.GetValue(o))
						apply();
				});
			}
			void Value<T>(string name, string alias, string description, Action<T> apply) {
				var option = alias != null ? new Option<T>(name, alias) : new Option<T>(name);
				Register(option, description, (r, o) => {
					if (r.GetResult(o) is { Implicit: false })
						apply(r.GetValue(o));
				});
			}

			Flag("--help", "-h", "Show this short help text", () => ShowHelp = true);
			Value<string>("--infile", "-i", "Input file", v => InputFile = v);
			Value<string>("--outfile", "-o", "Output file, without extension, read from map if not specified.", v => OutputFile = v);
			Value<string>("--outdir", "-d", "Output directory", v => OutputDir = v);
			Flag("--force-ra2", "-y", "Force using the Red Alert 2 engine for rendering", () => Engine = EngineType.RedAlert2);
			Flag("--force-yr", "-Y", "Force using the Yuri's Revenge engine for rendering", () => Engine = EngineType.YurisRevenge);
			Flag("--force-ts", "-t", "Force using the Tiberian Sun engine for rendering", () => Engine = EngineType.TiberianSun);
			Flag("--force-fs", "-T", "Force using the Firestorm engine for rendering", () => Engine = EngineType.Firestorm);
			Flag("--output-jpg", "-j", "Output JPEG file", () => SaveJPEG = true);
			Value<int>("--jpeg-quality", "-q", "Set JPEG quality level (0-100)", v => JPEGCompression = v);
			Flag("--output-png", "-p", "Output PNG file", () => SavePNG = true);
			Value<int>("--png-compression", "-c", "Set PNG compression level (1-9)", v => PNGQuality = v);
			Value<string[]>("--mixdir", "-m", "Specify location of .mix files, read from registry if not specified (win only). May be repeated when a game keeps its mixes and inis in separate directories", v => MixFilesDirectories.AddRange(v.Where(d => !string.IsNullOrWhiteSpace(d))));
			Value<string>("--modconfig", "-M", "Filename of a game configuration specific to your mod (create with GUI)", v => ModConfig = v);
			Value<string>("--meta-json", null, "Write resolved map metadata (name, engine, theater, size, start positions) as JSON to the given file", v => MetadataOutFile = v);
			Flag("--progress", null, "Print machine-readable render progress to stdout as progress:N:phase lines", () => ReportProgress = true);
			Flag("--mark-start-pos", null, "Mark starting positions", () => MarkStartPos = true);
			Flag("--start-pos-squared", "-S", "Mark starting positions in a squared manner", () => StartPositionMarking = StartPositionMarking.Squared);
			Flag("--start-pos-circled", null, "Mark starting positions in a circled manner", () => StartPositionMarking = StartPositionMarking.Circled);
			Flag("--start-pos-diamond", null, "Mark starting positions in a diamond manner", () => StartPositionMarking = StartPositionMarking.Diamond);
			Flag("--start-pos-ellipsed", null, "Mark starting positions in a ellipsed manner", () => StartPositionMarking = StartPositionMarking.Ellipsed);
			Flag("--start-pos-star", null, "Mark starting positions in a star manner", () => StartPositionMarking = StartPositionMarking.Starred);
			Flag("--start-pos-tiled", "-s", "Mark starting positions in a tiled manner", () => StartPositionMarking = StartPositionMarking.Tiled);
			Value<double>("--start-pos-size", null, "Mark starting positions with given size (2-6), defaults to 4, or 3 for tiled markers on TS/FS", v => MarkerStartSize = v);
			Flag("--mark-ore", "-r", "Mark ore and gem fields more explicity, looks good when resizing to a preview", () => MarkOreFields = true);
			Flag("--force-fullmap", "-F", "Ignore LocalSize definition and just save the full map", () => SizeMode = SizeMode.Full);
			Flag("--force-localsize", "-f", "Use localsize for map dimensions; without this or -F the size is picked automatically", () => SizeMode = SizeMode.Local);
			Flag("--debug", "-D", "", () => Debug = true);
			Value<string>("--debug-zbuffer", null, "Write the render's z-buffer (.npy) and shadow mask (.shadow.npy) to the given path for diagnostics", v => DebugZBufferFile = v);
			Value<string>("--debug-tiles", null, "Write one CSV row per map cell (rx,ry,z,ramp,tile,subtile) for diagnostics that need to know a cell's height or slope", v => DebugTilesFile = v);
			Value<string>("--debug-voxelmask", null, "Write a mask (.npy) of the pixels drawn by the voxel rasterizer, so a comparison against a game capture can exclude them: the game shades voxels differently on purpose", v => DebugVoxelMaskFile = v);
			Value<string>("--tile-lattice", null, "Override the 8x8 tile-variant lattice with 64 comma-separated values 0-7 (row-major), e.g. one exported from an engine capture", v => TileLattice = v);
			Flag("--pin-random", null, "Pin every randomised draw choice (animation loop frame, random SHP frame, building fire art, generated veins) to its first option, so a render is byte-comparable with an engine capture whose game logic was frozen", () => PinRandomDraws = true);
			Value<int>("--anim-frame", null, "Draw every animation at the frame the game engine shows at game-loop frame VALUE, for comparing against an engine capture whose logic was frozen at that frame", v => AnimFrame = v);
			Value<string>("--precapture", null, "Colour of each start position A-H for objects a map trigger hands to a starting player at game start (oil derricks and other tech buildings): one rules [Colors] name per position, comma-separated, empty where nobody starts, or \"none\" to leave them neutral grey. Default " + string.Join(",", DefaultPreCaptureColors), v => PreCaptureColors = ParsePreCaptureColors(v));
			Flag("--replace-preview-nomarkers", "-k", "Update the maps [PreviewPack] data with the rendered image, using no markers on the start positions", () => {
				GeneratePreviewPack = true;
				PreviewMarkers = PreviewMarkersType.None;
			});
			Flag("--preview-markers-selected", "-K", "Update the maps [PreviewPack] data with the rendered image, using the selected options of marker type and size on the start positions", () => {
				GeneratePreviewPack = true;
				PreviewMarkers = PreviewMarkersType.SelectedAsAbove;
			});
			Flag("--preview-markers-bittah", "-l", "Update the maps [PreviewPack] data with the rendered image, using Bittah's image on the start positions", () => {
				GeneratePreviewPack = true;
				PreviewMarkers = PreviewMarkersType.Bittah;
			});
			Flag("--preview-markers-aro", "-L", "Update the maps [PreviewPack] data with the rendered image, using Aro's image on the start positions", () => {
				GeneratePreviewPack = true;
				PreviewMarkers = PreviewMarkersType.Aro;
			});
			Flag("--ignore-lighting", "-n", "Ignore all lighting and lamps on the map", () => IgnoreLighting = true);
			Value<string>("--create-thumbnail", "-z", "Also save thumbnail(s) along with the fullmap; comma-separated specs [name:][+](x,y)[@q] where name overrides the thumb_ file prefix, + keeps aspect ratio and @q sets JPEG quality (e.g. \"+(480,480),preview:+(1280,1280)@82\")", v => ThumbnailConfig = v);
			Value<string>("--thumb-markers", null, "Draw this start position marker style (squared|circled|diamond|ellipsed|star) onto the thumbnails only; it is stamped after the full-size image is saved, so the main render keeps its own marker style", v => {
				switch (v?.ToLowerInvariant()) {
					case "squared": ThumbnailMarkers = StartPositionMarking.Squared; break;
					case "circled": ThumbnailMarkers = StartPositionMarking.Circled; break;
					case "diamond": ThumbnailMarkers = StartPositionMarking.Diamond; break;
					case "ellipsed": ThumbnailMarkers = StartPositionMarking.Ellipsed; break;
					case "star":
					case "starred": ThumbnailMarkers = StartPositionMarking.Starred; break;
					// tiled is baked into the tile palettes before drawing, so it cannot be applied per-thumbnail
					default: _logger.Warn("Unknown --thumb-markers style '{0}' ignored", v); break;
				}
			});
			Flag("--no-preview-fixup", "-x", "Do not fix the [Preview] dimensions when injecting the rendered preview", () => FixPreviewDimensions = false);
			Flag("--thumb-png", null, "Save thumbnails as PNG instead of JPEG.", () => SavePNGThumbnails = true);
			Flag("--fixup-tiles", null, "Remove undefined tiles and overwrite IsoMapPack5 section in map", () => FixupTiles = true);
			Flag("--icegrowth", "-g", "Mark cells with ice growth set, used in TS snow maps", () => MarkIceGrowth = true);
			Flag("--bkp", "-b", "Create map file backup when modifying", () => Backup = true);
			Flag("--fix-overlays", null, "Remove undefined overlays and update overlay packs in map", () => FixOverlays = true);
			Flag("--cmprs-tiles", null, "Compress and update IsoMapPack5 in map", () => CompressTiles = true);
			Flag("--tunnels", null, "Show tunnels path lines", () => TunnelPaths = true);
			Flag("--tunnelpos", null, "Adjust position of tunnel path lines", () => TunnelPosition = true);

			var result = root.Parse(args);
			foreach (var token in result.UnmatchedTokens)
				_logger.Warn("Unknown option '{0}' passed", token);
			foreach (var error in result.Errors)
				_logger.Warn("Command line error: {0}", error.Message);
			foreach (var apply in applications)
				apply(result);
		}

		public string GetHelpText() {
			if (_helpEntries.Count == 0)
				ConfigureFromArgs(Array.Empty<string>());
			var sb = new StringBuilder();
			foreach (var (invocation, description) in _helpEntries)
				sb.AppendLine("  " + invocation.PadRight(30) + description);
			return sb.ToString();
		}
	}

	public enum SizeMode {
		Local,
		Full,
		Auto,
	}
}
