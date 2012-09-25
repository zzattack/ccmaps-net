using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
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
		public static RenderSettings settings;
		static NLog.Logger logger;

		public static void Main(string[] args) {
#if DEBUG
			try { LogManager.Configuration = new XmlLoggingConfiguration("NLog.Debug.config"); }
			catch { }
#endif
			if (LogManager.Configuration == null) {
				// init default config
				ColoredConsoleTarget target = new ColoredConsoleTarget();
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
			settings = RenderSettings.CreateDefaults();
			options = new OptionSet {
			                        	{"h|help", "Show this short help text", v => settings.ShowHelp = true},
			                        	{"i|infile=", "Input file", v => settings.InputFile = v},
			                        	{"o|outfile=", "Output file, without extension, read from map if not specified.", v => settings.OutputFile = v},
			                        	{"d|outdir=", "Output directiory", v => settings.OutputDir = v},
			                        	{"y|force-ra2", "Force using the Red Alert 2 engine for rendering", v => settings.Engine = EngineType.RedAlert2},
			                        	{"Y|force-yr", "Force using the Yuri's Revenge engine for rendering", v => settings.Engine = EngineType.YurisRevenge},
			                        	{"t|force-ts", "Force using the Tiberian Sun engine for rendering", v => settings.Engine = EngineType.TiberianSun},
			                        	{"T|force-fs", "Force using the FireStorm engine for rendering", v => settings.Engine = EngineType.FireStorm},
			                        	{"j", "Output JPEG file", v => settings.SaveJPEG = true},
			                        	{"q|jpeg-quality=", "Set JPEG quality level (0-100)", (int v) => settings.JPEGCompression = v},
			                        	{"p", "Output PNG file", v => settings.SavePNG = true},
			                        	{"c|png-compression=", "Set PNG compression level (1-9)", (int v) => settings.PNGQuality = v},
			                        	{"m|mixdir=", "Specify location of .mix files, read from registry if not specified (win only)", v => settings.MixFilesDirectory = v},
			                        	{"s|start-pos-tiled", "Mark starting positions in a tiled manner", v => settings.StartPositionMarking = StartPositionMarking.Tiled},
			                        	{"S|start-pos-squared", "Mark starting positions in a squared manner", v => settings.StartPositionMarking = StartPositionMarking.Squared},
			                        	{"r|mark-ore", "Mark ore and gem fields more explicity, looks good when resizing to a preview", v => settings.MarkOreFields = true},
			                        	{"F|force-fullmap", "Ignore LocalSize definition and just save the full map", v => settings.IgnoreLocalSize = true},
			                        	{"f|force-localsize", "Use localsize for map dimensions (default)", v => settings.IgnoreLocalSize = true},
			                        	{"k|replace-preview", "Update the maps [PreviewPack] data with the rendered image", v => settings.GeneratePreviewPack = true}
			                        };

			options.Parse(args);

			if (settings.ShowHelp) {
				ShowHelp();
			}
			else if (!File.Exists(settings.InputFile)) {
				logger.Error("Specified input file does not exist");
				ShowHelp();
			}
			else if (!settings.SaveJPEG && !settings.SavePNG && !settings.GeneratePreviewPack) {
				logger.Error("No output format selected. Either specify -j, -p, -k or a combination");
				ShowHelp();
			}
			else if (settings.OutputDir != "" && !System.IO.Directory.Exists(settings.OutputDir)) {
				logger.Error("Specified output directory does not exist");
				ShowHelp();
			}
			else {
				logger.Info("Initializing virtual filesystem");
				var vfs = VFS.GetInstance();
				vfs.ScanMixDir(settings.Engine, settings.MixFilesDirectory);

				var map = new MapFile(File.Open(settings.InputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Path.GetFileName(settings.InputFile));
				map.FileName = settings.InputFile;

				if (!map.LoadMap(settings.Engine)) {
					logger.Error("Could not successfully load all required components for this map. Aborting.");
					return;
				}
				if (settings.StartPositionMarking == StartPositionMarking.Tiled)
					map.DrawTiledStartPositions();

				if (settings.MarkOreFields)
					map.MarkOreAndGems();

				map.DrawMap();

				if (settings.StartPositionMarking == StartPositionMarking.Squared)
					map.DrawSquaredStartPositions();

				if (settings.OutputFile == "")
					settings.OutputFile = map.DetermineMapName(map.EngineType);

				DrawingSurface ds = map.GetDrawingSurface();

				Rectangle saveRect;
				if (settings.IgnoreLocalSize)
					saveRect = new Rectangle(0, 0, ds.Width, ds.Height);
				else
					saveRect = map.GetLocalSizePixels();

				if (settings.OutputDir == "")
					settings.OutputDir = Path.GetDirectoryName(settings.InputFile);

				if (settings.SaveJPEG)
					ds.SaveJPEG(Path.Combine(settings.OutputDir, settings.OutputFile + ".jpg"), settings.JPEGCompression, saveRect);

				if (settings.SavePNG)
					ds.SavePNG(Path.Combine(settings.OutputDir, settings.OutputFile + ".png"), settings.PNGQuality, saveRect);

				if (settings.GeneratePreviewPack) {
					logger.Info("Generating PreviewPack data");
					// we will have to re-lock the bmd
					ds.Lock(ds.bm.PixelFormat);

					if (settings.MarkOreFields == false) {
						map.MarkOreAndGems();
						logger.Debug("Redrawing ore and gems areas");
						map.RedrawOreAndGems();
					}
					if (settings.StartPositionMarking != StartPositionMarking.Squared) {
						// undo tiled, if needed
						if (settings.StartPositionMarking == StartPositionMarking.Tiled)
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
				int p = (int)System.Environment.OSVersion.Platform;
				return (p == 4) || (p == 6) || (p == 128);
			}
		}
	}
}