using System.IO;
using CNCMaps.FileFormats.FileFormats;

namespace CNCMaps.FileFormats.VirtualFileSystem {

	public class DirArchive : IArchive {
		public readonly string Directory;

		public DirArchive(string path) {
			this.Directory = path;
		}

		public bool ContainsFile(string filename) {
			return File.Exists(Path.Combine(Directory, filename));
		}

		public VirtualFile OpenFile(string filename, FileFormat format = FileFormat.None, CacheMethod m = CacheMethod.Default) {
			var fs = new FileStream(Path.Combine(Directory, filename), FileMode.Open, FileAccess.Read, FileShare.Read);
			return FormatHelper.OpenAsFormat(fs, filename, 0, (int)fs.Length, format);
		}

		public void Close() {}
	}
}