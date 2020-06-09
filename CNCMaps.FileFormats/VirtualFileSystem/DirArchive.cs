using System.Collections.Generic;
using System.IO;

namespace CNCMaps.FileFormats.VirtualFileSystem {

	public class DirArchive : IArchive {
		public readonly string Directory;
		private Dictionary<string, FileStream> _openedFiles = new Dictionary<string, FileStream>();

		public DirArchive(string path) {
			Directory = path;
		}

		public bool ContainsFile(string filename) {
			return File.Exists(Path.Combine(Directory, filename));
		}

		public VirtualFile OpenFile(string filename, FileFormat format = FileFormat.None, CacheMethod m = CacheMethod.Default) {
			if (!_openedFiles.TryGetValue(filename, out FileStream file)) {
				file = _openedFiles[filename] = new FileStream(Path.Combine(Directory, filename), FileMode.Open, FileAccess.Read, FileShare.Read);
			}

			return FormatHelper.OpenAsFormat(file, filename, 0, (int)file.Length, format);
		}

		public void Dispose() {
			foreach (var file in _openedFiles.Values)
				file.Dispose();
			_openedFiles.Clear();
		}
	}
}
