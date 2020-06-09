using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CNCMaps.Shared;
using Microsoft.Win32;
using NLog;

namespace CNCMaps.FileFormats.VirtualFileSystem {

	public class VirtualFileSystem {
		public readonly List<IArchive> AllArchives = new List<IArchive>();
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VirtualFile Open(string filename) {
			return OpenFile(filename);
		}

		static FileFormat GetFormatFromTypeclass(Type t) {
			if (t == typeof(IniFile)) return FileFormat.Ini;
			if (t == typeof(CsfFile)) return FileFormat.Csf;
			if (t == typeof(HvaFile)) return FileFormat.Hva;
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

		public bool Add(string filename, CacheMethod cache = CacheMethod.Default) {
			return AddItem(filename, cache);
		}

		public bool FileExists(string filename) {
			return AllArchives.Any(v => v != null && v.ContainsFile(filename));
		}

		public VirtualFile OpenFile(string filename) {
			var format = FormatHelper.GuessFormat(filename);
			return Open(filename, format);
		}

		public T Open<T>(string filename, CacheMethod m = CacheMethod.Default) where T : VirtualFile {
			return Open(filename, GetFormatFromTypeclass(typeof(T)), m) as T;
		}

		public T OpenFile<T>(string filename, CacheMethod m = CacheMethod.Default) where T : VirtualFile {
			return Open(filename, GetFormatFromTypeclass(typeof(T)), m) as T;
		}

		public VirtualFile Open(string filename, FileFormat format = FileFormat.None, CacheMethod m = CacheMethod.Default) {
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

		public bool AddItem(string path, CacheMethod m = CacheMethod.Default) {
			// directory
			if (Directory.Exists(path)) {
				AllArchives.Add(new DirArchive(path));
				Logger.Trace("Added <DirArchive> {0} to VFS", path);
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
					Logger.Trace("Added <MixFile> {0} to VFS", path);
					return true;
				}
			}
			// virtual mix file
			else if (FileExists(path)) {
				var mx = Open(path, FileFormat.Mix) as MixFile;
				AllArchives.Add(mx);
				Logger.Trace("Added <VirtualMixFile> {0} to VFS", path);
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

		public bool LoadMixes(string dir, EngineType engine) {
			// if we don't have this directory in the VFS yet
			if (!AllArchives.OfType<DirArchive>().Any(da => string.Equals(
				Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
				Path.GetFullPath(da.Directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
				StringComparison.InvariantCultureIgnoreCase))) {
				this.AddItem(dir);
			}
			return LoadMixes(engine);
		}

		public bool LoadMixes(EngineType engine) {
			if (engine == EngineType.AutoDetect) {
				Logger.Fatal("Scanning mixdir for auto detect theater is no longer supported!");
				return false;
			}

			// see http://modenc.renegadeprojects.com/MIX for more info
			Logger.Info("Initializing filesystem for the {0} engine", engine.ToString());

			if (engine == EngineType.Firestorm)
				if (FileExists("patch.mix"))
					AddItem("patch.mix");

			// try all expand(md).mix files
			for (int i = 99; i >= 0; i--) {
				string file = "";
				if (engine == EngineType.YurisRevenge) {
					file = "expandmd" + i.ToString("00") + ".mix";
					if (FileExists(file))
						AddItem(file);
				}
				else {
					file = "expand" + i.ToString("00") + ".mix";
					if (FileExists(file))
						AddItem(file);
				}
			}

			if (engine <= EngineType.Firestorm) {
				for (int i = 99; i >= 0; i--) {
					string file = string.Format("ecache{0:d2}.mix", i);
					if (FileExists(file))
						AddItem(file);
				}
			}

			// Game loads this earlier, but Ares override it for YR
			if (engine >= EngineType.RedAlert2) {
				if (engine == EngineType.YurisRevenge) AddItem("langmd.mix");
				AddItem("language.mix");
			}

			if (engine >= EngineType.RedAlert2) {
				if (engine == EngineType.YurisRevenge)
					AddItem("ra2md.mix");
				AddItem("ra2.mix");
			}
			else {
				AddItem("tibsun.mix");
			}

			if (engine == EngineType.YurisRevenge)
				AddItem("cachemd.mix");
			AddItem("cache.mix");

			if (engine == EngineType.YurisRevenge)
				AddItem("localmd.mix");
			AddItem("local.mix");

			if (engine == EngineType.YurisRevenge)
				AddItem("audiomd.mix");

			if (engine >= EngineType.RedAlert2) {
				List <string> ecacheList = new List<string>();
				foreach (var dir in AllArchives.OfType<DirArchive>().ToList()) {
					foreach (string file in Directory.GetFiles(dir.Directory, "ecache*.mix"))
						ecacheList.Add(Path.GetFileName(file));
				}

				foreach (string ecachefile in ecacheList.OrderByDescending(f => f))
					AddItem(ecachefile);
			}

			for (int i = 99; i >= 0; i--) {
				string file = string.Format("elocal{0:d2}.mix", i);
				if (FileExists(file))
					AddItem(file);
			}

			if (engine >= EngineType.RedAlert2) {
				foreach (var dir in AllArchives.OfType<DirArchive>().ToList()) {
					foreach (string file in Directory.GetFiles(dir.Directory, "*.mmx"))
						AddItem(Path.Combine(dir.Directory, file));

					if (engine == EngineType.YurisRevenge)
						foreach (string file in Directory.GetFiles(dir.Directory, "*.yro"))
							AddItem(Path.Combine(dir.Directory, file));
				}
			}

			AddItem("conquer.mix");

			if (engine >= EngineType.RedAlert2) {
				if (engine == EngineType.YurisRevenge) {
					AddItem("conqmd.mix");
					AddItem("genermd.mix");
				}
				AddItem("generic.mix");
				if (engine == EngineType.YurisRevenge)
					AddItem("isogenmd.mix");
				AddItem("isogen.mix");
				if (engine == EngineType.YurisRevenge) AddItem("cameomd.mix");
				AddItem("cameo.mix");
				if (engine == EngineType.YurisRevenge) {
					AddItem("mapsmd03.mix");
					AddItem("multimd.mix");
					AddItem("thememd.mix");
				}
			}

			return true;
		}

		public void Reset() {
			foreach (var arch in AllArchives)
				if (arch != null) arch.Close();
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

		private static string ra2DirCached ;
		public static string RA2InstallDir {
			get {
				if (ra2DirCached == null) {
					try { ra2DirCached = Path.GetDirectoryName(RA2InstallPath); }
					catch { }
				}
				return ra2DirCached;
			}
		}


		private static string tsDirCached = null;
		public static string TSInstallDir {
			get {
				if (tsDirCached == null) {
					try { tsDirCached = !string.IsNullOrEmpty(TSInstallPath) ? Path.GetDirectoryName(TSInstallPath) : ""; }
					catch { }
				}
				return tsDirCached;
			}
		}

		public static string ReadRegistryString(RegistryKey rkey, string regpath, string keyname) {
			string ret = string.Empty;
			try {
				var key = rkey.OpenSubKey(regpath);
				if (key != null) ret = key.GetValue(keyname, "").ToString();
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
