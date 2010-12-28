using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using CNCMaps.FileFormats;

namespace CNCMaps.VirtualFileSystem {

	class VirtuaFileSystem {
		private static VirtuaFileSystem instance = new VirtuaFileSystem();
		private VirtuaFileSystem() { }
		public static VirtuaFileSystem GetInstance() {
			return instance;
		}

		static string[] MixArchiveExtensions = { ".mix", ".yro", ".mmx" };

		List<IArchive> AllArchives = new List<IArchive>();

		bool FileExists(string filename) {
			return AllArchives.Any(v => v.ContainsFile(filename));
		}
		public VirtualFile Open(string filename) {
			var archive = AllArchives.FirstOrDefault(v => v.ContainsFile(filename));
			if (archive == null)
				return null;
			var file = archive.OpenFile(filename);
			file.FileName = filename;
			return file;
		}
		private MixFile OpenMix(string filename) {
			var archive = AllArchives.FirstOrDefault(v => v.ContainsFile(filename));
			if (archive == null)
				return null;
			else
				return archive.OpenFile(filename, true) as MixFile;
		}
		public bool Add(string path) {
			// directory
			if (Directory.Exists(path)) {
				AllArchives.Add(new DirArchive(path));
				return true;
			}
			// regular file
			else if (File.Exists(path)) {
				FileInfo fi = new FileInfo(path);
				// mix file
				if (MixArchiveExtensions.Contains(fi.Extension, StringComparer.InvariantCultureIgnoreCase)) {
					MixFile mf = new MixFile(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
					AllArchives.Add(mf);
					return true;
				}
			}
			// virtual mix file
			else if (FileExists(path)) {
				MixFile mx = OpenMix(path);
				AllArchives.Add(mx);
				return true;
			}
			return false;
		}
		
		public void ScanMixDir(string mixDir, bool YR) {
			// see http://modenc.renegadeprojects.com/MIX for more info
			Console.WriteLine("Initializing filesystem on {0}, {1} Yuri's Revenge support", mixDir, YR ? "with" : "without");
			Add(mixDir);

			if (YR) Add(Path.Combine(mixDir, "langmd.mix"));
			Add(Path.Combine(mixDir, "language.mix"));

			// try all expand\d{2}md?\.mix files
			for (int i = 99; i >= 0; i--) {
				string file = "expand" + i.ToString("00") + ".mix";
				string path = Path.Combine(mixDir, file);
				if (File.Exists(path))
					Add(path);
				if (YR) {
					file = "expandmd" + i.ToString("00") + ".mix";
					path = Path.Combine(mixDir, file);
					if (File.Exists(path))
						Add(path);
				}
			}

			if (YR) Add(Path.Combine(mixDir, "ra2md.mix"));
			Add(Path.Combine(mixDir, "ra2.mix"));

			if (YR) Add("cachemd.mix");
			Add("cache.mix");

			if (YR) Add("localmd.mix");
			Add("local.mix");

			if (YR) Add("audiomd.mix");
			
			foreach (string file in Directory.GetFiles(mixDir, "ecache*.mix")) {
				Add(Path.Combine(mixDir, file));
			}
			
			foreach (string file in Directory.GetFiles(mixDir, "elocal*.mix")) {
				Add(Path.Combine(mixDir, file));
			}
						
			foreach (string file in Directory.GetFiles(mixDir, "*.mmx")) {
				Add(Path.Combine(mixDir, file));
			}

			foreach (string file in Directory.GetFiles(mixDir, "*.yro")) {
				Add(Path.Combine(mixDir, file));
			}

			if (YR) Add("conqmd.mix");
			if (YR) Add("genermd.mix");
			Add("generic.mix");
			if (YR) Add("isogenmd.mix");
			Add("isogen.mix");
			Add("conquer.mix");
			if (YR) Add("cameomd.mix");
			Add("cameo.mix");
			if (YR) {
				Add(Path.Combine(mixDir, "mapsmd03.mix"));
				Add(Path.Combine(mixDir, "multimd.mix"));
				Add(Path.Combine(mixDir, "thememd.mix"));
				Add(Path.Combine(mixDir, "movmd03.mix"));
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