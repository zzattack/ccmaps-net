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

		public VirtualFile OpenFile(string filename, FileFormat format = FileFormat.None) {
			FileStream fs = new FileStream(Path.Combine(this.path, filename), FileMode.Open, FileAccess.Read, FileShare.Read);
			return FormatHelper.OpenAsFormat(fs, filename, 0, (int)fs.Length, format);			
		}
	}
}
