using System;
using System.IO;
using CNCMaps.FileFormats;
using CNCMaps.VirtualFileSystem;
using CNCMaps.Utility;

namespace CNCMaps {
	enum StartPositionMarking {
		None,
		Tiled,
		Squared
	}

	public enum EngineType {
		AutoDetect,
		RedAlert2,
		YurisRevenge,
		TiberianSun,
		FireStorm
	}

	struct RenderSettings {

		public string InputFile { get; set; }

		public string OutputFile { get; set; }

		public string OutputDir { get; set; }

		public bool SavePNG { get; set; }

		public bool SaveJPEG { get; set; }

		public int PNGQuality { get; set; }

		public int JPEGCompression { get; set; }

		public string MixFilesDirectory { get; set; }

		public bool ShowHelp { get; set; }

		public bool MarkOreFields { get; set; }

		public bool IgnoreLocalSize { get; set; }

		public EngineType Engine;

		public StartPositionMarking StartPositionMarking;

		internal static RenderSettings CreateDefaults() {
			return new RenderSettings() {
				PNGQuality = 6,
				SavePNG = false,
				JPEGCompression = 95,
				SaveJPEG = false,
				ShowHelp = false,
				MarkOreFields = false,
				Engine = EngineType.AutoDetect,
				StartPositionMarking = StartPositionMarking.None,
				InputFile = "",
				OutputDir = "",
				OutputFile = "",
				MixFilesDirectory = ""
			};
		}
	}

	class Program {

		public static void Main(string[] args) {
			RenderSettings rs = RenderSettings.CreateDefaults();
			var options = new NDesk.Options.OptionSet() {
				{ "h|help", "Help", v => rs.ShowHelp = true },
				{ "i|infile=", "Input file", v => rs.InputFile = v },
				{ "o|outfile=", "Output file, without extension", v => rs.OutputFile = v },
				{ "d|outdir=", "Output directiory", v => rs.OutputDir = v },
				{ "y|force-ra2", "Force using the Red Alert 2 engine for rendering", v => rs.Engine = EngineType.RedAlert2 },
				{ "Y|force-yr", "Force using the Yuri's Revenge engine for rendering", v => rs.Engine = EngineType.YurisRevenge },
				{ "t|force-ts", "Force using the Tiberian Sun engine for rendering", v => rs.Engine = EngineType.TiberianSun },
				{ "T|force-fs", "Force using the FireStorm engine for rendering", v => rs.Engine = EngineType.FireStorm },
				{ "j", "Output JPEG file", v => rs.SaveJPEG = true },
				{ "q|jpeg-quality=", "Set JPEG quality level (0-100)", (int v) => rs.JPEGCompression = v },
				{ "p", "Output PNG file", v => rs.SavePNG = true },
				{ "c|png-compression=", "Set PNG compression level (1-9)", (int v) => rs.PNGQuality = v },
				{ "m|mixdir=", "Specify location of .mix files", v => rs.MixFilesDirectory = v },
				{ "s|start-pos-tiled", "Mark starting positions in a tiled manner", v => rs.StartPositionMarking = StartPositionMarking.Tiled },
				{ "S|start-pos-squared", "Mark starting positions in a squared manner", v => rs.StartPositionMarking = StartPositionMarking.Squared },
				{ "r|mark-pre", "Mark ore and gem fields more explicity, looks good when resizing to a preview", v => rs.MarkOreFields = true },
				{ "F|force-fullmap", "Ignore LocalSize definition and just save the full map", v => rs.IgnoreLocalSize = true }
			};
			options.Parse(args);

			if (rs.ShowHelp) {
				ShowHelp();
				return;
			}
			else if (!System.IO.File.Exists(rs.InputFile)) {
				Logger.WriteLine("Error: specified input file does not exist");
				ShowHelp(); 
				return;
			}
			else if (!rs.SaveJPEG && !rs.SavePNG) {
				Logger.WriteLine("Error: no output format selected. Either specify -j, -p or both");
				ShowHelp(); 
				return;
			}
			else if (rs.OutputDir != "" && !System.IO.Directory.Exists(rs.OutputDir)) {
				ShowHelp(); 
				Logger.WriteLine("Error: specified output directory does not exist");
			}

			Logger.WriteLine("Initializing virtual filesystem");
			var vfs = VFS.GetInstance();
			vfs.ScanMixDir(rs.Engine, rs.MixFilesDirectory);

			MapFile map = new MapFile(File.Open(rs.InputFile, FileMode.Open, FileAccess.Read, FileShare.Read), Path.GetFileName(rs.InputFile));
			map.FileName = rs.InputFile;

			map.LoadMap(rs.Engine);
			if (rs.StartPositionMarking == StartPositionMarking.Tiled)
				map.DrawTiledStartPositions();

			map.DrawMap();

			if (rs.StartPositionMarking == StartPositionMarking.Squared)
				map.DrawSquaredStartPositions();
			
			if (rs.OutputFile == "")
				rs.OutputFile = map.DetermineMapName();

			CNCMaps.Utility.DrawingSurface ds = map.GetDrawingSurface();

			System.Drawing.Rectangle saveRect;
			if (rs.IgnoreLocalSize)
				saveRect = new System.Drawing.Rectangle(0, 0, ds.Width, ds.Height);
			else
				saveRect = map.GetLocalSizePixels();

			if (rs.OutputDir == "")
				rs.OutputDir = Path.GetDirectoryName(rs.InputFile);

			if (rs.SaveJPEG)
				ds.SaveJPEG(Path.Combine(rs.OutputDir, rs.OutputFile + ".jpg"), rs.JPEGCompression, saveRect);

			if (rs.SavePNG)
				ds.SavePNG(Path.Combine(rs.OutputDir, rs.OutputFile + ".png"), rs.PNGQuality, saveRect);
		}

		private static void ShowHelp() {
			Logger.WriteLine("Usage: ");
			Logger.WriteLine("");
			Logger.WriteLine(" -i   --infile \"c:\\myMap.mpr\"   Input map file (.mpr, .map, .yrm)");
			Logger.WriteLine(" -o   --outfile myMap           Output base filename. Read from map if not specified.");
			Logger.WriteLine(" -d   --outdir \"c:\\\"            Output directory");
			Logger.WriteLine(" -Y   --force-yr                Force rendering using YR engine");
			Logger.WriteLine(" -y   --force-ra2               Force rendering using RA2 engine");
			Logger.WriteLine(" -j   --jpeg                    Produce JPEG file (myMap.jpg)");
			Logger.WriteLine(" -q   --jpeg-quality [0-100]     JPEG quality (0-100, default 90)");
			Logger.WriteLine(" -p   --png                     Produce PNG file (myMap.png)");
			Logger.WriteLine(" -c   --png-compression [0-9]    PNG compression level (0-9, default 6)");
			Logger.WriteLine(" -m   --mixdir \"c:\\westwood\\\"   Mix files location (registry if not specified)");
			Logger.WriteLine(" -s   --start-pos-tiled         Mark start positions as 4x4 tiled red spots");
			Logger.WriteLine(" -S   --start-pos-squared       Mark start positions as a large square");
			Logger.WriteLine(" -r   --mark-ore                Mark ore clearly");
			Logger.WriteLine(" -F   --force-fullmap           Ignore LocalSize definition");
			Logger.WriteLine(" -f   --force-localsize         Force usage of localsize");
			Logger.WriteLine(" -h   --help                    Show this short help text");
			Logger.WriteLine(" ");
		}
	}
}