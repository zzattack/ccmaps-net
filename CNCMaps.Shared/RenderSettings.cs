using NLog;

namespace CNCMaps.Shared {

	public class RenderSettings {
		OptionSet _options;
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		public string InputFile { get; set; }
		public string OutputFile { get; set; }
		public string OutputDir { get; set; }
		public bool SavePNG { get; set; }
		public bool SaveJPEG { get; set; }
		public int PNGQuality { get; set; }
		public int JPEGCompression { get; set; }
		public string MixFilesDirectory { get; set; }
		public string ModConfig { get; set; }
		public bool ShowHelp { get; set; }
		public bool MarkOreFields { get; set; }
		public bool IgnoreLighting { get; set; }
		public SizeMode SizeMode { get; set; }
		public EngineType Engine { get; set; }
		public StartPositionMarking StartPositionMarking;
		public bool PreferOSMesa { get; set; }
		public string ThumbnailConfig { get; set; }
		public bool GeneratePreviewPack { get; set; }
		public PreviewMarkersType PreviewMarkers { get; set; }
		public bool FixPreviewDimensions { get; set; }
		public bool Debug { get; set; }

		public RenderSettings() {
			PNGQuality = 6;
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
			MixFilesDirectory = "";
			ModConfig = "";
			ThumbnailConfig = "";
			SizeMode = SizeMode.Auto;
			FixPreviewDimensions = true;
			Debug = false;
		}

		public void ConfigureFromArgs(string[] args) {
			var unprocessed = GetOptions().Parse(args);
			foreach (var opt in unprocessed) { 
				_logger.Warn("Unknown option '{0}' passed", opt);
			}
		}

		public OptionSet GetOptions() {
			if (_options == null)_options = new OptionSet {
				{"h|help", "Show this short help text", v => ShowHelp = true},
				{"i|infile=", "Input file", v => InputFile = v},
				{"o|outfile=", "Output file, without extension, read from map if not specified.", v => OutputFile = v},
				{"d|outdir=", "Output directiory", v => OutputDir = v},
				{"y|force-ra2", "Force using the Red Alert 2 engine for rendering", v => Engine = EngineType.RedAlert2}, 
				{"Y|force-yr", "Force using the Yuri's Revenge engine for rendering", v => Engine = EngineType.YurisRevenge},
				{"t|force-ts", "Force using the Tiberian Sun engine for rendering", v => Engine = EngineType.TiberianSun},
				{"T|force-fs", "Force using the Firestorm engine for rendering", v => Engine = EngineType.Firestorm},
				{"j|output-jpg", "Output JPEG file", v => SaveJPEG = true},
				{"q|jpeg-quality=", "Set JPEG quality level (0-100)", (int v) => JPEGCompression = v},
				{"p|output-png", "Output PNG file", v => SavePNG = true},
				{"c|png-compression=", "Set PNG compression level (1-9)", (int v) => PNGQuality = v}, 
				{"m|mixdir=", "Specify location of .mix files, read from registry if not specified (win only)",v => MixFilesDirectory = v},
				{"M|modconfig=", "Filename of a game configuration specific to your mod (create with GUI)",v => ModConfig = v},
				{"s|start-pos-tiled", "Mark starting positions in a tiled manner",v => StartPositionMarking = StartPositionMarking.Tiled},
				{"S|start-pos-squared", "Mark starting positions in a squared manner",v => StartPositionMarking = StartPositionMarking.Squared}, 
				{"r|mark-ore", "Mark ore and gem fields more explicity, looks good when resizing to a preview", v => MarkOreFields = true},
				{"F|force-fullmap", "Ignore LocalSize definition and just save the full map", v => SizeMode = SizeMode.Full},
				{"f|force-localsize", "Use localsize for map dimensions (default)", v => SizeMode = SizeMode.Local}, 
				{"D|debug", v => Debug = true },
				{"k|replace-preview-nomarkers", "Update the maps [PreviewPack] data with the rendered image, using no markers on the start positions",
					v => {
						GeneratePreviewPack = true;
						PreviewMarkers = PreviewMarkersType.None;
					}
				},
				{"K|preview-markers-squared", "Update the maps [PreviewPack] data with the rendered image, using a red squared marker on the start positions",
					v => {
						GeneratePreviewPack = true;
						PreviewMarkers = PreviewMarkersType.Squared;
					}
				},
				{"l|preview-markers-bittah", "Update the maps [PreviewPack] data with the rendered image, using Bittah's image on the start positions",
					v => {
						GeneratePreviewPack = true;
						PreviewMarkers = PreviewMarkersType.Bittah;
					}
				},
				{"L|preview-markers-aro", "Update the maps [PreviewPack] data with the rendered image, using Aro's image on the start positions",
					v => {
						GeneratePreviewPack = true;
						PreviewMarkers = PreviewMarkersType.Aro;
					}
				},

				{"n|ignore-lighting", "Ignore all lighting and lamps on the map",v => IgnoreLighting = true}, 
				// {"G|graphics-winmgr", "Attempt rendering voxels using window manager context first (default)",v => Settings.PreferOSMesa = false},
				{"g|graphics-osmesa", "Attempt rendering voxels using OSMesa context first", v => PreferOSMesa = true},
				{"z|create-thumbnail=", "Also save a thumbnail along with the fullmap in dimensions (x,y), prefix with + to keep aspect ratio	", v => ThumbnailConfig = v},
				{"x|no-preview-fixup=", "Also save a thumbnail along with the fullmap in dimensions (x,y), prefix with + to keep aspect ratio	", v => ThumbnailConfig = v},
			};

			return _options;
		}
	}

	public enum SizeMode {
		Local,
		Full,
		Auto,
	}
}