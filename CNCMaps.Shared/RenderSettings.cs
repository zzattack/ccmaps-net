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
		public bool FixupTiles { get; set; }
		public bool GeneratePreviewPack { get; set; }
		public PreviewMarkersType PreviewMarkers { get; set; }
		public bool SavePNGThumbnails { get; set; }
		public bool FixPreviewDimensions { get; set; }
		public bool Debug { get; set; }
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
			SavePNGThumbnails = false;
			SizeMode = SizeMode.Auto;
			FixPreviewDimensions = true;
			Debug = false;
			MarkIceGrowth = false;
			Backup = true;
			FixOverlays = false;
			CompressTiles = false;
			TunnelPaths = false;
			TunnelPosition = false;
			MarkStartPos = false;
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
			Value<string>("--outdir", "-d", "Output directiory", v => OutputDir = v);
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
			Value<string>("--create-thumbnail", "-z", "Also save a thumbnail along with the fullmap in dimensions (x,y), prefix with + to keep aspect ratio", v => ThumbnailConfig = v);
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
