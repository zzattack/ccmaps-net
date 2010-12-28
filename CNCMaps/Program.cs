using System;
using System.IO;
using CNCMaps.VirtualFileSystem;
using CNCMaps.FileFormats;
using System.Text;

namespace CNCMaps {
	enum StartPositionMarking {
		None,
		Tiled,
		Squared
	}

	enum EngineType {
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
			int start_tick = Environment.TickCount;
			
			RenderSettings rs = RenderSettings.CreateDefaults();
			var options = new NDesk.Options.OptionSet() {
				{ "i|infile=", "Input file", v => rs.InputFile = v },
				{ "o|outfile=", "Output file, without extension", v => rs.OutputFile = v },
				{ "d|outdir=", "Output directiory", v => rs.OutputDir = v },
				{ "Y|force-yr", "Force using the Yuri's Revenge engine for rendering", v => rs.Engine = EngineType.YurisRevenge },
				{ "y|force-ra2", "Force using the Red Alert 2 engine for rendering", v => rs.Engine = EngineType.RedAlert2 },
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
				Console.WriteLine("Usage: ");
				Console.WriteLine("");
				Console.WriteLine(" -i   --infile \"c:\\myMap.mpr\"   Input map file (.mpr, .map, .yrm)");
				Console.WriteLine(" -o   --outfile myMap           Output base filename. Read from map if not specified.");
				Console.WriteLine(" -d   --outdir \"c:\\\"            Output directory");
				Console.WriteLine(" -Y   --force-yr                Force rendering using YR engine");
				Console.WriteLine(" -y   --force-ra2               Force rendering using RA2 engine");
				Console.WriteLine(" -j   --jpeg                    Produce JPEG file (myMap.jpg)");
				Console.WriteLine(" -q   --jpeg-quality [0-100]     JPEG quality (0-100, default 90)");
				Console.WriteLine(" -p   --png                     Produce PNG file (myMap.png)");
				Console.WriteLine(" -c   --png-compression [0-9]    PNG compression level (0-9, default 6)");
				Console.WriteLine(" -m   --mixdir \"c:\\westwood\\\"   Mix files location (registry if not specified)");
				Console.WriteLine(" -s   --start-pos-tiled         Mark start positions as 4x4 tiled red spots");
				Console.WriteLine(" -S   --start-pos-squared       Mark start positions as a large square");
				Console.WriteLine(" -r   --mark-ore                Mark ore clearly");
				Console.WriteLine(" -F   --force-fullmap           Ignore LocalSize definition");
				Console.WriteLine(" -f   --force-localsize         Force usage of localsize");
				Console.WriteLine(" -h   --help                    Show this short help text");
				Console.WriteLine(" ");
				return;
			}
			else if (!System.IO.File.Exists(rs.InputFile)) {
				Console.WriteLine("Error: specified input file does not exist");
				return;
			}
			else if (!rs.SaveJPEG && !rs.SavePNG) {
				Console.WriteLine("Error: no output format selected. Either specify -j, -p or both");
				return;
			}
			else if (rs.OutputDir != "" && !System.IO.Directory.Exists(rs.OutputDir)) {
				Console.WriteLine("Error: specified output directory does not exist");
			}

			var vfs = VirtuaFileSystem.GetInstance();
			Console.WriteLine("{0:0000} - Initializing virtual filesystem", Environment.TickCount - start_tick);
			vfs.ScanMixDir(VirtuaFileSystem.RA2InstallDir, false);

			MapFile map = new MapFile(File.Open(rs.InputFile, FileMode.Open, FileAccess.Read, FileShare.Read));
			map.FileName = rs.InputFile;

			map.LoadMap();
			
			if (rs.OutputFile == "") {
				rs.OutputFile = map.DetermineMapName();
			}



		}
	}
}