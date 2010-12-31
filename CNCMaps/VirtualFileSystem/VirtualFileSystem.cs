using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CNCMaps.FileFormats;
using Microsoft.Win32;

namespace CNCMaps.VirtualFileSystem {

	class VFS {
		private static VFS instance = new VFS();

		public VFS() { }

		public static VFS GetInstance() {
			return instance;
		}

		public static VirtualFile Open(string filename) {
			return instance.OpenFile(filename);
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
			return AllArchives.Any(v => v.ContainsFile(filename));
		}

		public VirtualFile OpenFile(string filename) {
			var format = FormatHelper.GuessFormat(filename);
			return OpenFile(filename, format);
		}

		public VirtualFile OpenFile(string filename, FileFormat format = FileFormat.None) {
			var archive = AllArchives.FirstOrDefault(v => v.ContainsFile(filename));
			if (archive == null)
				return null;

			return archive.OpenFile(filename, format);
		}

		public bool AddFile(string path) {
			// directory
			if (Directory.Exists(path)) {
				AllArchives.Add(new DirArchive(path));
				return true;
			}
			// regular file
			else if (File.Exists(path)) {
				FileInfo fi = new FileInfo(path);
				// mix file
				if (FormatHelper.MixArchiveExtensions.Contains(fi.Extension, StringComparer.InvariantCultureIgnoreCase)) {
					MixFile mf = new MixFile(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
					mf.FileName = path;
					AllArchives.Add(mf);
					return true;
				}
			}
			// virtual mix file
			else if (Exists(path)) {
				MixFile mx = Open(path) as MixFile;
				AllArchives.Add(mx);
				return true;
			}
			return false;
		}

		public bool AddMix(MixFile mix) {
			AllArchives.Add(mix);
			return true;
		}

		public void ScanMixDir(string mixDir, bool YR) {
			// see http://modenc.renegadeprojects.com/MIX for more info
			CNCMaps.Utility.Logger.WriteLine("Initializing filesystem on {0}, {1} Yuri's Revenge support", mixDir, YR ? "with" : "without");
			AddFile(mixDir);

			if (YR) AddFile(Path.Combine(mixDir, "langmd.mix"));
			AddFile(Path.Combine(mixDir, "language.mix"));

			// try all expand\d{2}md?\.mix files
			for (int i = 99; i >= 0; i--) {
				string file = "expand" + i.ToString("00") + ".mix";
				string path = Path.Combine(mixDir, file);
				if (File.Exists(path))
					AddFile(path);
				if (YR) {
					file = "expandmd" + i.ToString("00") + ".mix";
					path = Path.Combine(mixDir, file);
					if (File.Exists(path))
						AddFile(path);
				}
			}

			if (YR) AddFile(Path.Combine(mixDir, "ra2md.mix"));
			AddFile(Path.Combine(mixDir, "ra2.mix"));

			if (YR) AddFile("cachemd.mix");
			AddFile("cache.mix");

			if (YR) AddFile("localmd.mix");
			AddFile("local.mix");

			if (YR) AddFile("audiomd.mix");

			foreach (string file in Directory.GetFiles(mixDir, "ecache*.mix")) {
				AddFile(Path.Combine(mixDir, file));
			}

			foreach (string file in Directory.GetFiles(mixDir, "elocal*.mix")) {
				AddFile(Path.Combine(mixDir, file));
			}

			foreach (string file in Directory.GetFiles(mixDir, "*.mmx")) {
				AddFile(Path.Combine(mixDir, file));
			}

			foreach (string file in Directory.GetFiles(mixDir, "*.yro")) {
				AddFile(Path.Combine(mixDir, file));
			}

			if (YR) AddFile("conqmd.mix");
			if (YR) AddFile("genermd.mix");
			AddFile("generic.mix");
			if (YR) AddFile("isogenmd.mix");
			AddFile("isogen.mix");
			AddFile("conquer.mix");
			if (YR) AddFile("cameomd.mix");
			AddFile("cameo.mix");
			if (YR) {
				AddFile(Path.Combine(mixDir, "mapsmd03.mix"));
				AddFile(Path.Combine(mixDir, "multimd.mix"));
				AddFile(Path.Combine(mixDir, "thememd.mix"));
				AddFile(Path.Combine(mixDir, "movmd03.mix"));
			}
		}

		public static string RA2InstallPath {
			get {
				return ReadRegistryString(Registry.LocalMachine, "SOFTWARE\\Westwood\\Red Alert 2", "InstallPath");
			}
		}

		public static string RA2InstallDir {
			get {
				return Path.GetDirectoryName(RA2InstallPath);
			}
		}

		public static string ReadRegistryString(RegistryKey rkey, string regpath, string keyname) {
			string ret = string.Empty;
			try {
				ret = rkey.OpenSubKey(regpath).GetValue(keyname, "").ToString();
			}
			catch { }
			return ret;
		}
	}
}