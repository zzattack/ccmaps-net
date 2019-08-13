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
		public bool MarkStartPos { get; set; }
		public string MarkStartSize { get; set; }
		public bool PreferOSMesa { get; set; }
		public string ThumbnailConfig { get; set; }
		public bool FixupTiles { get; set; }
		public bool GeneratePreviewPack { get; set; }
		public PreviewMarkersType PreviewMarkers { get; set; }
        public bool SavePNGThumbnails { get; set; }
        public bool FixPreviewDimensions { get; set; }
		public bool Debug { get; set; }
		public bool MarkIceGrowth { get; set; }
		public bool DiagnosticWindow { get; set; }
		public bool Backup { get; set; }
		public bool FixOverlays { get; set; }
		public bool CompressTiles { get; set; }
		public bool TunnelPaths { get; set; }
		public bool TunnelPosition { get; set; }

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
            SavePNGThumbnails = false;
			SizeMode = SizeMode.Auto;
			FixPreviewDimensions = true;
			Debug = false;
			MarkIceGrowth = false;
			DiagnosticWindow = false;
			Backup = true;
			FixOverlays = false;
			CompressTiles = false;
			TunnelPaths = false;
			TunnelPosition = false;
			MarkStartPos = false;
			MarkStartSize = "4.0";
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
				{"mark-start-pos", "Mark starting positions",v => MarkStartPos = true},
				{"S|start-pos-squared", "Mark starting positions in a squared manner",v => StartPositionMarking = StartPositionMarking.Squared}, 
				{"start-pos-circled", "Mark starting positions in a circled manner",v => StartPositionMarking = StartPositionMarking.Circled},
				{"start-pos-diamond", "Mark starting positions in a diamond manner",v => StartPositionMarking = StartPositionMarking.Diamond},
				{"start-pos-ellipsed", "Mark starting positions in a ellipsed manner",v => StartPositionMarking = StartPositionMarking.Ellipsed}, 
				{"s|start-pos-tiled", "Mark starting positions in a tiled manner",v => StartPositionMarking = StartPositionMarking.Tiled},
				{"start-pos-size", "Mark starting positions with given size (2-6)", v => MarkStartSize = v},
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
				{"K|preview-markers-selected", "Update the maps [PreviewPack] data with the rendered image, using the selected options of marker type and size on the start positions",
					v => {
						GeneratePreviewPack = true;
						PreviewMarkers = PreviewMarkersType.SelectedAsAbove;
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
				//{"g|graphics-osmesa", "Attempt rendering voxels using OSMesa context first", v => PreferOSMesa = true},
				{"z|create-thumbnail=", "Also save a thumbnail along with the fullmap in dimensions (x,y), prefix with + to keep aspect ratio	", v => ThumbnailConfig = v},
				{"x|no-preview-fixup=", "Also save a thumbnail along with the fullmap in dimensions (x,y), prefix with + to keep aspect ratio	", v => ThumbnailConfig = v},
                {"thumb-png", "Save thumbnails as PNG instead of JPEG.", v => SavePNGThumbnails = true },
                {"fixup-tiles", "Remove undefined tiles and overwrite IsoMapPack5 section in map", v => FixupTiles = true },
				{"g|icegrowth", "Mark cells with ice growth set, used in TS snow maps", v => MarkIceGrowth = true},
				{"e|diagwindow", "Show the diagnostic window", v => DiagnosticWindow = true},
				{"b|bkp", "Create map file backup when modifying", v => Backup = true},
				{"fix-overlays", "Remove undefined overlays and update overlay packs in map", v => FixOverlays = true},
				{"cmprs-tiles", "Compress and update IsoMapPack5 in map", v => CompressTiles = true},
				{"tunnels", "Show tunnels path lines", v => TunnelPaths = true},
				{"tunnelpos", "Adjust position of tunnel path lines", v => TunnelPaths = true},
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