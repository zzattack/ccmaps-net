using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CNCMaps.FileFormats;
using CNCMaps.Map;
using CNCMaps.Utility;
using Microsoft.Win32;

namespace CNCMaps.VirtualFileSystem {

	public class VFS {
		private static readonly VFS Instance = new VFS();
		internal readonly List<IArchive> AllArchives = new List<IArchive>();
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public static VFS GetInstance() {
			return Instance;
		}

		public static VirtualFile Open(string filename) {
			return Instance.OpenFile(filename);
		}

		public static T Open<T>(string filename, CacheMethod m = CacheMethod.Default) where T : VirtualFile {
			return Open(filename, GetFormatFromTypeclass(typeof(T)), m) as T;
		}

		public static T Open<T>(string filename, FileFormat f, CacheMethod m) where T : VirtualFile {
			return Open(filename, f, m) as T;
		}

		public T OpenFile<T>(string filename, CacheMethod m = CacheMethod.Default) where T : VirtualFile {
			return this.OpenFile(filename, GetFormatFromTypeclass(typeof(T)), m) as T;
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
			if (t == typeof(VplFile)) return FileFormat.Vpl;
			if (t == typeof(VxlFile)) return FileFormat.Vxl;
			return FileFormat.Ukn;
		}

		public static VirtualFile Open(string filename, FileFormat f, CacheMethod m) {
			return Instance.OpenFile(filename, f, m);
		}

		public static bool Add(string filename, CacheMethod cache = CacheMethod.Default) {
			return Instance.AddFile(filename, cache);
		}

		public static bool Exists(string imageFileName) {
			return Instance.FileExists(imageFileName);
		}

		private bool FileExists(string filename) {
			return AllArchives.Any(v => v != null && v.ContainsFile(filename));
		}

		public VirtualFile OpenFile(string filename) {
			var format = FormatHelper.GuessFormat(filename);
			return OpenFile(filename, format);
		}

		public VirtualFile OpenFile(string filename, FileFormat format = FileFormat.None, CacheMethod m = CacheMethod.Default) {
			if (AllArchives == null || AllArchives.Count == 0) return null;
			var archive = AllArchives.FirstOrDefault(v => v != null && v.ContainsFile(filename));
			if (archive == null) return null;

			try {
				return archive.OpenFile(filename, format, m);
			}
			catch {
				return null;
			}
		}

		public bool AddFile(string path, CacheMethod m = CacheMethod.Default) {
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
			else if (FileExists(path)) {
				var mx = OpenFile(path, FileFormat.Mix) as MixFile;
				AllArchives.Add(mx);
				return true;
			}
			return false;
		}

		public bool AddMix(MixFile mix) {
			AllArchives.Add(mix);
			return true;
		}

		public static string DetermineMixDir(string mixDirOverride, EngineType engine) {
			if (string.IsNullOrEmpty(mixDirOverride))
				mixDirOverride = engine >= EngineType.RedAlert2 || engine == EngineType.AutoDetect ? RA2InstallDir : TSInstallDir;
			return mixDirOverride;
		}

		public bool ScanMixDir(string mixDir, EngineType engine) {
			if (engine == EngineType.AutoDetect) {
				Logger.Fatal("Scanning mixdir for auto detect theater is no longer supported!");
				return false;
			}

			if (string.IsNullOrEmpty(mixDir))
				mixDir = DetermineMixDir(mixDir, engine);

			if (string.IsNullOrEmpty(mixDir)) {
				Logger.Fatal("No mix directory detected!");
				return false;
			}

			// see http://modenc.renegadeprojects.com/MIX for more info
			Logger.Info("Initializing filesystem on {0} for the {1} engine", mixDir, engine.ToString());
			AddFile(mixDir);

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

			// the game actually loads these earlier, but modders like to override them
			// with ares or something
			if (engine >= EngineType.RedAlert2) {
				if (engine == EngineType.YurisRevenge) AddFile("langmd.mix");
				AddFile("language.mix");
			}

			if (engine >= EngineType.RedAlert2) {
				if (engine == EngineType.YurisRevenge)
					AddFile("ra2md.mix");
				AddFile("ra2.mix");
			}
			else {
				if (engine == EngineType.Firestorm)
					AddFile("patch.mix");
				AddFile("tibsun.mix");
			}

			if (engine == EngineType.YurisRevenge)
				AddFile("cachemd.mix");
			AddFile("cache.mix");

			if (engine == EngineType.YurisRevenge)
				AddFile("localmd.mix");
			AddFile("local.mix");

			if (engine == EngineType.YurisRevenge)
				AddFile("audiomd.mix");

			foreach (string file in Directory.GetFiles(mixDir, "ecache*.mix"))
				AddFile(Path.Combine(mixDir, file));


			foreach (string file in Directory.GetFiles(mixDir, "elocal*.mix"))
				AddFile(Path.Combine(mixDir, file));

			if (engine >= EngineType.RedAlert2) {
				foreach (string file in Directory.GetFiles(mixDir, "*.mmx"))
					AddFile(Path.Combine(mixDir, file));

				if (engine == EngineType.YurisRevenge)
					foreach (string file in Directory.GetFiles(mixDir, "*.yro"))
						AddFile(Path.Combine(mixDir, file));
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
				}
			}

			return true;
		}

		public void Clear() {
			foreach (var arch in AllArchives)
				arch.Close();
			AllArchives.Clear();
		}

		public static string RA2InstallPath {
			get {
				var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
				return ReadRegistryString(key, "SOFTWARE\\Westwood\\Red Alert 2", "InstallPath");
			}
		}

		public static string TSInstallPath {
			get {
				var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
				return ReadRegistryString(key, "SOFTWARE\\Westwood\\Tiberian Sun", "InstallPath");
			}
		}

		public static string RA2InstallDir {
			get {
				return Path.GetDirectoryName(RA2InstallPath);
			}
		}

		public static string TSInstallDir {
			get { return Path.GetDirectoryName(TSInstallPath); }
		}

		public static string ReadRegistryString(RegistryKey rkey, string regpath, string keyname) {
			string ret = string.Empty;
			try {
				ret = rkey.OpenSubKey(regpath).GetValue(keyname, "").ToString();
			}
			catch {
				Logger.Error("Could not read registry key {0} at {1}", keyname, regpath);
			}
			return ret;
		}

	}

	public enum CacheMethod {
		Default,
		Cache,
		NoCache
	}
}