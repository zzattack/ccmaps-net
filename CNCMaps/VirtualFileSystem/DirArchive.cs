using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CNCMaps.FileFormats;

namespace CNCMaps.VirtualFileSystem {
	class DirArchive : IArchive {
		string path;

		public DirArchive(string path) { 
			this.path = path;
		}

		public bool ContainsFile(string filename) {
			return File.Exists(Path.Combine(this.path, filename));
		}

		public VirtualFile OpenFile(string filename, bool openAsMix = false) {
			FileStream fs = new FileStream(Path.Combine(this.path, filename), FileMode.Open, FileAccess.Read, FileShare.Read);
			if (openAsMix)
				return new MixFile(fs, 0, (int)fs.Length, true);
 			else
				return new VirtualFile(fs, 0, (int)fs.Length, true);
		}
	}
}
