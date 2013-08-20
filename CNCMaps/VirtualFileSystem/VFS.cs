using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CNCMaps.FileFormats;
using CNCMaps.MapLogic;
using Microsoft.Win32;

namespace CNCMaps.VirtualFileSystem {

	public class VFS {
		static VFS instance = new VFS();
		private VFS() { }

		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public static VFS GetInstance() {
			return instance;
		}

		public static VirtualFile Open(string filename) {
			return instance.OpenFile(filename);
		}

		public static T Open<T>(string filename) where T : VirtualFile {
			return Open(filename, GetFormatFromTypeclass(typeof(T))) as T;
		}

		public static T Open<T>(string filename, FileFormat f) where T : VirtualFile {
			return Open(filename, f) as T;
		}

		static FileFormat GetFormatFromTypeclass(Type t) {
			if (t == typeof(IniFile)) return FileFormat.Ini;
			if (t == typeof(CsfFile)) return FileFormat.Csf;
			if (t == typeof(HvaFile)) return FileFormat.Hva;
			if (t == typeof(MapFile)) return FileFormat.Map;
			if (t == typeof(MissionsFile)) return FileFormat.Missions;
			if (t == typeof(MixFile)) return FileFormat.Mix;
			if (t == typeof(PalFile)) return FileFormat.Pal;
			if (t == typeof(PktFile)) return FileFormat.Pkt;
			if (t == typeof(ShpFile)) return FileFormat.Shp;
			if (t == typeof(TmpFile)) return FileFormat.Tmp;
			if (t == typeof(VxlFile)) return FileFormat.Vxl;
			return FileFormat.Ukn;
		}

		public static VirtualFile Open(string filename, FileFormat format = FileFormat.None) {
			return instance.OpenFile(filename, format);
		}

		public static bool Add(string filename) {
			return instance.AddFile(filename);
		}

		public static bool Exists(string imageFileName) {
			return instance.FileExists(imageFileName);
		}

		List<IArchive> AllArchives = new List<IArchive>();

		bool FileExists(string filename) {
			return AllArchives.Any(v => v != null && v.ContainsFile(filename));
		}

		public VirtualFile OpenFile(string filename) {
			var format = FormatHelper.GuessFormat(filename);
			return OpenFile(filename, format);
		}

		public VirtualFile OpenFile(string filename, FileFormat format = FileFormat.None) {
			if (AllArchives == null || AllArchives.Count == 0) return null;
			var archive = AllArchives.FirstOrDefault(v => v != null && v.ContainsFile(filename));
			if (archive == null) return null;

			try {
				return archive.OpenFile(filename, format);
			}
			catch {
				return null;
			}
		}

		public bool AddFile(string path) {
			// directory
			if (Directory.Exists(path)) {
				AllArchives.Add(new DirArchive(path));
				return true;
			}
			// regular file
			else if (File.Exists(path)) {
				var fi = new FileInfo(path);
				// mix file
				if (FormatHelper.MixArchiveExtensions.Contains(fi.Extension, StringComparer.InvariantCultureIgnoreCase)) {
					var mf = new MixFile(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
					mf.FileName = path;
					AllArchives.Add(mf);
					return true;
				}
			}
			// virtual mix file
			else if (Exists(path)) {
				var mx = Open<MixFile>(path);
				AllArchives.Add(mx);
				return true;
			}
			return false;
		}

		public bool AddMix(MixFile mix) {
			AllArchives.Add(mix);
			return true;
		}

		public void ScanMixDir(EngineType engine, string installDir = "") {
			if (installDir == "")
				installDir = engine <= EngineType.FireStorm ? TSInstallDir : RA2InstallDir;

			ScanMixDir(installDir, engine == EngineType.AutoDetect ? EngineType.YurisRevenge : engine);
		}

		public void ScanMixDir(string mixDir, EngineType engine) {
			if (string.IsNullOrEmpty(mixDir))
				logger.Fatal("No mix directory detected!");

			// see http://modenc.renegadeprojects.com/MIX for more info
			logger.Info("Initializing filesystem on {0} for the {1} engine", mixDir, engine.ToString());
			AddFile(mixDir);

			if (engine >= EngineType.RedAlert2) {
				if (engine == EngineType.YurisRevenge) AddFile("langmd.mix");
				AddFile(Path.Combine(mixDir, "language.mix"));
			}

			// try all expand\d{2}md?\.mix files
			for (int i = 99; i >= 0; i--) {
				string file = "expand" + i.ToString("00") + ".mix";
				string path = Path.Combine(mixDir, file);
				if (File.Exists(path))
					AddFile(path);
				if (engine == EngineType.YurisRevenge) {
					file = "expandmd" + i.ToString("00") + ".mix";
					path = Path.Combine(mixDir, file);
					if (File.Exists(path))
						AddFile(path);
				}
			}

			if (engine >= EngineType.RedAlert2) {
				if (engine == EngineType.YurisRevenge) AddFile("ra2md.mix");
				AddFile(Path.Combine(mixDir, "ra2.mix"));
			}
			else {
				if (engine == EngineType.YurisRevenge) AddFile("tibsunmd.mix");
				AddFile("tibsun.mix");
			}

			if (engine == EngineType.YurisRevenge) AddFile("cachemd.mix");
			AddFile("cache.mix");

			if (engine == EngineType.YurisRevenge) AddFile("localmd.mix");
			AddFile("local.mix");

			if (engine == EngineType.YurisRevenge) AddFile("audiomd.mix");

			foreach (string file in Directory.GetFiles(mixDir, "ecache*.mix")) {
				AddFile(Path.Combine(mixDir, file));
			}

			foreach (string file in Directory.GetFiles(mixDir, "elocal*.mix")) {
				AddFile(Path.Combine(mixDir, file));
			}

			if (engine >= EngineType.RedAlert2) {
				foreach (string file in Directory.GetFiles(mixDir, "*.mmx")) {
					AddFile(Path.Combine(mixDir, file));
				}
				if (engine == EngineType.YurisRevenge) {
					foreach (string file in Directory.GetFiles(mixDir, "*.yro")) {
						AddFile(Path.Combine(mixDir, file));
					}
				}
			}

			if (engine >= EngineType.RedAlert2) {
				if (engine == EngineType.YurisRevenge) {
					AddFile("conqmd.mix");
					AddFile("genermd.mix");
				}
				AddFile("generic.mix");
				if (engine == EngineType.YurisRevenge)
					AddFile("isogenmd.mix");
				AddFile("isogen.mix");
				AddFile("conquer.mix");
				if (engine == EngineType.YurisRevenge) AddFile("cameomd.mix");
				AddFile("cameo.mix");
				if (engine == EngineType.YurisRevenge) {
					AddFile("mapsmd03.mix");
					AddFile("multimd.mix");
					AddFile("thememd.mix");
					AddFile("movmd03.mix");
				}
			}
		}

		public static string RA2InstallPath {
			get {
				return ReadRegistryString(Registry.LocalMachine, "SOFTWARE\\Westwood\\Red Alert 2", "InstallPath");
			}
		}
		public static string TSInstallPath {
			get {
				return ReadRegistryString(Registry.LocalMachine, "SOFTWARE\\Westwood\\Tiberian Sun", "InstallPath");
			}
		}

		public static string RA2InstallDir {
			get {
				return Path.GetDirectoryName(RA2InstallPath);
			}
		}
		public static string TSInstallDir {
			get {
				return Path.GetDirectoryName(TSInstallPath);
			}
		}

		public static string ReadRegistryString(RegistryKey rkey, string regpath, string keyname) {
			string ret = string.Empty;
			try {
				ret = rkey.OpenSubKey(regpath).GetValue(keyname, "").ToString();
			}
			catch {
				logger.Error("Could not read registry key {0} at {1}", keyname, regpath);
			}
			return ret;
		}
	}
}