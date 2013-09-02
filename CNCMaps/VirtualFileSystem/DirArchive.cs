using System.IO;
using CNCMaps.FileFormats;

namespace CNCMaps.VirtualFileSystem {

	class DirArchive : IArchive {
		readonly string path;

		public DirArchive(string path) {
			this.path = path;
		}

		public bool ContainsFile(string filename) {
			return File.Exists(Path.Combine(path, filename));
		}

		public VirtualFile OpenFile(string filename, FileFormat format = FileFormat.None, CacheMethod m = CacheMethod.Default) {
			var fs = new FileStream(Path.Combine(path, filename), FileMode.Open, FileAccess.Read, FileShare.Read);
			return FormatHelper.OpenAsFormat(fs, filename, 0, (int)fs.Length, format);
		}
	}
}