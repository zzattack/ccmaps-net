using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using CNCMaps.Encodings;
using CNCMaps.FileFormats;
using CNCMaps.MapLogic;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;
using System;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace CNCMaps {
	class Program {
		static OptionSet options;
		public static RenderSettings Settings;
		static Logger _logger;

		public static int Main(string[] args) {
			InitLoggerConfig();
			InitSettings(args);
			if (!ValidateSettings())
				return 1;

			try {
				_logger.Info("Initializing virtual filesystem");
				var vfs = VFS.GetInstance();
				if (!vfs.ScanMixDir(Settings.Engine, Settings.MixFilesDirectory)) {
					_logger.Fatal("Scanning for mix files failed. If on Linux, specify the --mixdir command line argument");
					return 1;
				}

				var map = new MapFile(
					File.Open(Settings.InputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
					Path.GetFileName(Settings.InputFile));
				map.FileName = Settings.InputFile;

				// Code to organize moving of maps in a directory for themselves
				/*string mapName = map.DetermineMapName(EngineType.FireStorm);
				string dir = Path.Combine(Path.GetDirectoryName(map.FileName), mapName);
				if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
				map.Close();
				map.Dispose();
				File.Move(map.FileName, Path.Combine(dir, Path.GetFileName(map.FileName)));
				return;*/

				if (!map.LoadMap(Settings.Engine)) {
					_logger.Error("Could not successfully load all required components for this map. Aborting.");
					return 1;
				}

				if (Settings.StartPositionMarking == StartPositionMarking.Tiled)
					map.DrawTiledStartPositions();

				if (Settings.MarkOreFields)
					map.MarkOreAndGems();

				map.DrawMap();

				//using (var form = new DebugDrawingSurfaceWindow(map.GetDrawingSurface(), map.GetTiles(), map.GetTheater(), map)) {
				//	form.RequestTileEvaluate += tile => map.DebugDrawTile(tile);
				//	form.ShowDialog();
				//}

				if (Settings.StartPositionMarking == StartPositionMarking.Squared)
					map.DrawSquaredStartPositions();

				if (Settings.OutputFile == "")
					Settings.OutputFile = map.DetermineMapName();

				if (Settings.OutputDir == "")
					Settings.OutputDir = Path.GetDirectoryName(Settings.InputFile);

				// free up as much memory as possible before saving the large images
				Rectangle saveRect = Settings.IgnoreLocalSize ? map.GetFullMapSizePixels() : map.GetLocalSizePixels();
				DrawingSurface ds = map.GetDrawingSurface();
				// if we don't need this data anymore, we can try to save some memory
				if (!Settings.GeneratePreviewPack) {
					ds.FreeNonBitmap();
					map.FreeUseless();
					GC.Collect();
				}

				if (Settings.SaveJPEG)
					ds.SaveJPEG(Path.Combine(Settings.OutputDir, Settings.OutputFile + ".jpg"), Settings.JPEGCompression, saveRect);

				if (Settings.SavePNG)
					ds.SavePNG(Path.Combine(Settings.OutputDir, Settings.OutputFile + ".png"), Settings.PNGQuality, saveRect);

				if (Settings.GeneratePreviewPack)
					map.GeneratePreviewPack(Settings.OmitPreviewPackMarkers);
			}
			catch (Exception exc) {
				_logger.Error(string.Format("An unknown fatal exception occured: {0}", exc), exc);
				return 1;
			}

			LogManager.Configuration = null; // required for mono release to flush possible targets
			return 0;
		}


		private static void InitLoggerConfig() {
			// for release, logmanager automatically picks the correct NLog.config file
#if DEBUG
			try {
				LogManager.Configuration = new XmlLoggingConfiguration("NLog.Debug.config");
			}
			catch {
			}
#endif
			if (LogManager.Configuration == null) {
				// init default config
				var target = new ColoredConsoleTarget();
				target.Name = "console";
				target.Layout = "${processtime:format=ss.fff} [${level}] ${message}";
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.Magenta,
					Condition = "level = LogLevel.Fatal"
				});
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.Red,
					Condition = "level = LogLevel.Error"
				});
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.Yellow,
					Condition = "level = LogLevel.Warn"
				});
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.Gray,
					Condition = "level = LogLevel.Info"
				});
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.DarkGray,
					Condition = "level = LogLevel.Debug"
				});
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.White,
					Condition = "level = LogLevel.Trace"
				});
				LogManager.Configuration = new LoggingConfiguration();
				LogManager.Configuration.AddTarget("console", target);
#if DEBUG
				LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, target));
#else
				LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, target));

#endif
				LogManager.ReconfigExistingLoggers();
			}
			_logger = LogManager.GetCurrentClassLogger();
		}

		private static void InitSettings(string[] args) {
			Settings = RenderSettings.CreateDefaults();
			options = new OptionSet {
				{"h|help", "Show this short help text", v => Settings.ShowHelp = true},
				{"i|infile=", "Input file", v => Settings.InputFile = v},
				{"o|outfile=", "Output file, without extension, read from map if not specified.", v => Settings.OutputFile = v},
				{"d|outdir=", "Output directiory", v => Settings.OutputDir = v},
				{"y|force-ra2", "Force using the Red Alert 2 engine for rendering", v => Settings.Engine = EngineType.RedAlert2}, 
				{"Y|force-yr", "Force using the Yuri's Revenge engine for rendering", v => Settings.Engine = EngineType.YurisRevenge},
				{"t|force-ts", "Force using the Tiberian Sun engine for rendering", v => Settings.Engine = EngineType.TiberianSun},
				{"T|force-fs", "Force using the FireStorm engine for rendering", v => Settings.Engine = EngineType.FireStorm},
				{"j|output-jpg", "Output JPEG file", v => Settings.SaveJPEG = true},
				{"q|jpeg-quality=", "Set JPEG quality level (0-100)", (int v) => Settings.JPEGCompression = v},
				{"p|output-png", "Output PNG file", v => Settings.SavePNG = true},
				{"c|png-compression=", "Set PNG compression level (1-9)", (int v) => Settings.PNGQuality = v}, 
				{"m|mixdir=", "Specify location of .mix files, read from registry if not specified (win only)",v => Settings.MixFilesDirectory = v},
				{"s|start-pos-tiled", "Mark starting positions in a tiled manner",v => Settings.StartPositionMarking = StartPositionMarking.Tiled},
				{"S|start-pos-squared", "Mark starting positions in a squared manner",v => Settings.StartPositionMarking = StartPositionMarking.Squared}, 
				{"r|mark-ore", "Mark ore and gem fields more explicity, looks good when resizing to a preview",v => Settings.MarkOreFields = true},
				{"F|force-fullmap", "Ignore LocalSize definition and just save the full map", v => Settings.IgnoreLocalSize = true},
				{"f|force-localsize", "Use localsize for map dimensions (default)", v => Settings.IgnoreLocalSize = false}, 
				{"k|replace-preview", "Update the maps [PreviewPack] data with the rendered image",v => Settings.GeneratePreviewPack = true}, 
				{"K|replace-preview-nosquares", "Update the maps [PreviewPack] data with the rendered image, without squares",
					v => {
						Settings.GeneratePreviewPack = true;
						Settings.OmitPreviewPackMarkers = true;
					}
				}, 
				{"G|graphics-winmgr", "Attempt rendering voxels using window manager context first (default)",v => Settings.PreferOSMesa = false},
				{"g|graphics-osmesa", "Attempt rendering voxels using OSMesa context first", v => Settings.PreferOSMesa = true},
			};

			options.Parse(args);
		}

		private static bool ValidateSettings() {
			if (Settings.ShowHelp) {
				ShowHelp();
				return false; // not really false :/
			}
			else if (!File.Exists(Settings.InputFile)) {
				_logger.Error("Specified input file does not exist");
				return false;
			}
			else if (!Settings.SaveJPEG && !Settings.SavePNG && !Settings.GeneratePreviewPack) {
				_logger.Error("No output format selected. Either specify -j, -p, -k or a combination");
				return false;
			}
			else if (Settings.OutputDir != "" && !System.IO.Directory.Exists(Settings.OutputDir)) {
				_logger.Error("Specified output directory does not exist.");
				return false;
			}
			return true;
		}

		private static void ShowHelp() {
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write("Usage: ");
			Console.WriteLine("");
			var sb = new System.Text.StringBuilder();
			var sw = new StringWriter(sb);
			options.WriteOptionDescriptions(sw);
			Console.WriteLine(sb.ToString());
		}

		public static bool IsLinux {
			get {
				int p = (int)Environment.OSVersion.Platform;
				return (p == 4) || (p == 6) || (p == 128);
			}
		}
	}
}