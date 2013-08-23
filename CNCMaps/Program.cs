using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
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
		static NLog.Logger logger;

		public static void Main(string[] args) {
#if DEBUG
			try { LogManager.Configuration = new XmlLoggingConfiguration("NLog.Debug.config"); }
			catch { }
#endif
			if (LogManager.Configuration == null) {
				// init default config
				var target = new ColoredConsoleTarget();
				target.Name = "console";
				target.Layout = "${processtime:format=ss.fff} [${level}] ${message}";
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.Magenta, Condition = "level = LogLevel.Fatal"
				});
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.Red, Condition = "level = LogLevel.Error"
				});
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.Yellow, Condition = "level = LogLevel.Warn"
				});
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.Gray, Condition = "level = LogLevel.Info"
				});
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.DarkGray, Condition = "level = LogLevel.Debug"
				});
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.White, Condition = "level = LogLevel.Trace"
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
			logger = NLog.LogManager.GetCurrentClassLogger();
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
				{"m|mixdir=", "Specify location of .mix files, read from registry if not specified (win only)", v => Settings.MixFilesDirectory = v},
				{"s|start-pos-tiled", "Mark starting positions in a tiled manner", v => Settings.StartPositionMarking = StartPositionMarking.Tiled},
				{"S|start-pos-squared", "Mark starting positions in a squared manner", v => Settings.StartPositionMarking = StartPositionMarking.Squared},
				{"r|mark-ore", "Mark ore and gem fields more explicity, looks good when resizing to a preview", v => Settings.MarkOreFields = true},
				{"F|force-fullmap", "Ignore LocalSize definition and just save the full map", v => Settings.IgnoreLocalSize = true},
				{"f|force-localsize", "Use localsize for map dimensions (default)", v => Settings.IgnoreLocalSize = true},
				{"k|replace-preview", "Update the maps [PreviewPack] data with the rendered image", v => Settings.GeneratePreviewPack = true},
				{"G|graphics-winmgr", "Attempt rendering voxels using window manager context first (default)", v => Settings.PreferOSMesa = false},
				{"g|graphics-osmesa", "Attempt rendering voxels using OSMesa context first", v => Settings.PreferOSMesa = true},
			};

			options.Parse(args);

			if (Settings.ShowHelp) {
				ShowHelp();
			}
			else if (!File.Exists(Settings.InputFile)) {
				logger.Error("Specified input file does not exist");
			}
			else if (!Settings.SaveJPEG && !Settings.SavePNG && !Settings.GeneratePreviewPack) {
				logger.Error("No output format selected. Either specify -j, -p, -k or a combination");
			}
			else if (Settings.OutputDir != "" && !System.IO.Directory.Exists(Settings.OutputDir)) {
				logger.Error("Specified output directory does not exist.");
			}
			else {
				logger.Info("Initializing virtual filesystem");
				var vfs = VFS.GetInstance();
				if (!vfs.ScanMixDir(Settings.Engine, Settings.MixFilesDirectory)) {
					logger.Fatal("Scanning for mix files failed. If on Linux, specify the --mixdir command line argument");
					return;
				}

				var map = new MapFile(
					File.Open(Settings.InputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
					Path.GetFileName(Settings.InputFile));
				map.FileName = Settings.InputFile;

				// crap thingie to move maps in a directory for themselves
				/*string mapName = map.DetermineMapName(EngineType.FireStorm);
				string dir = Path.Combine(Path.GetDirectoryName(map.FileName), mapName);
				if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
				map.Close();
				map.Dispose();
 				File.Move(map.FileName, Path.Combine(dir, Path.GetFileName(map.FileName)));
				return;*/
				
				if (!map.LoadMap(Settings.Engine)) {
					logger.Error("Could not successfully load all required components for this map. Aborting.");
					return;
				}

				if (Settings.StartPositionMarking == StartPositionMarking.Tiled)
					map.DrawTiledStartPositions();

				if (Settings.MarkOreFields)
					map.MarkOreAndGems();

				map.DrawMap();

				if (Settings.StartPositionMarking == StartPositionMarking.Squared)
					map.DrawSquaredStartPositions();

				if (Settings.OutputFile == "")
					Settings.OutputFile = map.DetermineMapName(map.EngineType);

				DrawingSurface ds = map.GetDrawingSurface();

				Rectangle saveRect;
				if (Settings.IgnoreLocalSize)
					saveRect = new Rectangle(map.TileWidth / 2, map.TileHeight / 2, ds.Width - map.TileWidth, ds.Height - map.TileHeight);
				else
					saveRect = map.GetLocalSizePixels();

				if (Settings.OutputDir == "")
					Settings.OutputDir = Path.GetDirectoryName(Settings.InputFile);

				// free up as much memory as possible
				ds.FreeNonBitmap();
				map.FreeUseless(); 
				GC.Collect();

				if (Settings.SaveJPEG)
					ds.SaveJPEG(Path.Combine(Settings.OutputDir, Settings.OutputFile + ".jpg"), Settings.JPEGCompression, saveRect);

				if (Settings.SavePNG)
					ds.SavePNG(Path.Combine(Settings.OutputDir, Settings.OutputFile + ".png"), Settings.PNGQuality, saveRect);

				if (Settings.GeneratePreviewPack) {
					logger.Info("Generating PreviewPack data");
					// we will have to re-lock the bmd
					ds.Lock(ds.bm.PixelFormat);

					if (Settings.MarkOreFields == false) {
						map.MarkOreAndGems();
						logger.Debug("Redrawing ore and gems areas");
						map.RedrawOreAndGems();
					}
					if (Settings.StartPositionMarking != StartPositionMarking.Squared) {
						// undo tiled, if needed
						if (Settings.StartPositionMarking == StartPositionMarking.Tiled)
							map.UndrawTiledStartPositions();
						map.DrawSquaredStartPositions();
					}

					double ratioX = (double)144 / (double)ds.Width;
					double ratioY = (double)133 / (double)ds.Height;
					double ratio = ratioX < ratioY ? ratioX : ratioY; // use whichever multiplier is smaller

					ds.Unlock();

					Bitmap preview = new Bitmap((int)Math.Round(ds.Width * ratio, 0), (int)Math.Round(ds.Height * ratio, 0));
					using (Graphics gfx = Graphics.FromImage(preview)) {
						// use high-quality scaling
						gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
						gfx.SmoothingMode = SmoothingMode.HighQuality;
						gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;
						gfx.CompositingQuality = CompositingQuality.HighQuality;

						gfx.DrawImage(ds.bm, new Rectangle(0, 0, preview.Width, preview.Height), saveRect, GraphicsUnit.Pixel);
					}

					logger.Info("Injecting thumbnail into map");
					ThumbInjector.InjectThumb(preview, map);

					logger.Info("Saving map");
					map.Save(map.FileName);
				}
			}

			LogManager.Configuration = null; // required for mono release to flush possible targets
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