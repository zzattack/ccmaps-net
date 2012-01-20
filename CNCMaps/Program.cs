using System.IO;
using CNCMaps.FileFormats;
using CNCMaps.MapLogic;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps {
	class Program {
		static OptionSet options;
		public static RenderSettings settings;
		public static void Main(string[] args) {
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
			                        	{"g|opengl-mesa", "Use software-only 3d rendering for voxels (useful for services)", v => settings.SoftwareRendering = true}
			                        };
			options.Parse(args);

			if (settings.ShowHelp) {
				ShowHelp();
			}
			else if (!File.Exists(settings.InputFile)) {
				Logger.WriteLine("Error: specified input file does not exist");
				ShowHelp();
			}
			else if (!settings.SaveJPEG && !settings.SavePNG) {
				Logger.WriteLine("Error: no output format selected. Either specify -j, -p or both");
				ShowHelp();
			}
			else if (settings.OutputDir != "" && !System.IO.Directory.Exists(settings.OutputDir)) {
				Logger.WriteLine("Error: specified output directory does not exist");
				ShowHelp();
			}
			else {
				Logger.WriteLine("Initializing virtual filesystem");
				var vfs = VFS.GetInstance();
				vfs.ScanMixDir(settings.Engine, settings.MixFilesDirectory);

				var map = new MapFile(File.Open(settings.InputFile, FileMode.Open, FileAccess.Read, FileShare.Read), Path.GetFileName(settings.InputFile));
				map.FileName = settings.InputFile;

				if (settings.SoftwareRendering) {
					if (File.Exists("opengl32.dll"))
						Logger.WriteLine("Warning: opengl32.dll already exists. Did a previous render abort unexpectedly?");
					else if (File.Exists("opengl32_mesa.dll")) {
						try { File.Move("opengl32_mesa.dll", "opengl32.dll"); }
						catch { Logger.WriteLine("Warning: could not move opengl32_mesa.dll to opengl32.dll"); }
					}
					else
						Logger.WriteLine("Warning: cannot use software rendering for voxels, opengl32_mesa.dll missing");
				}
				else {
					if (File.Exists("opengl32.dll")) {
						Logger.WriteLine("Warning: opengl32.dll exists but software rendering was not specified. Rename manually to opengl32_mesa.dll to disable software rendering");
					}
				}

				map.LoadMap(settings.Engine);
				if (settings.StartPositionMarking == StartPositionMarking.Tiled)
					map.DrawTiledStartPositions();

				map.DrawMap();

				if (settings.StartPositionMarking == StartPositionMarking.Squared)
					map.DrawSquaredStartPositions();

				if (settings.OutputFile == "")
					settings.OutputFile = map.DetermineMapName();

				DrawingSurface ds = map.GetDrawingSurface();

				System.Drawing.Rectangle saveRect;
				if (settings.IgnoreLocalSize)
					saveRect = new System.Drawing.Rectangle(0, 0, ds.Width, ds.Height);
				else
					saveRect = map.GetLocalSizePixels();

				if (settings.OutputDir == "")
					settings.OutputDir = Path.GetDirectoryName(settings.InputFile);

				if (settings.SaveJPEG)
					ds.SaveJPEG(Path.Combine(settings.OutputDir, settings.OutputFile + ".jpg"), settings.JPEGCompression, saveRect);

				if (settings.SavePNG)
					ds.SavePNG(Path.Combine(settings.OutputDir, settings.OutputFile + ".png"), settings.PNGQuality, saveRect);

				if (settings.SoftwareRendering && File.Exists("opengl32.dll") && !File.Exists("opengl32_mesa.dll"))
					File.Move("opengl32.dll", "opengl32_mesa.dll");
			}
		}

		private static void ShowHelp() {
			Logger.WriteLine("Usage: ");
			Logger.WriteLine("");
			var sb = new System.Text.StringBuilder();
			var sw = new StringWriter(sb);
			options.WriteOptionDescriptions(sw);
			Logger.WriteLine(sb.ToString());
		}
	}
}