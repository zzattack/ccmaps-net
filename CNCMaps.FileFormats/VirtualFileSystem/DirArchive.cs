using System;
using System.Collections.Generic;
using System.IO;

namespace CNCMaps.FileFormats.VirtualFileSystem {

	public class DirArchive : IArchive {
		public readonly string Directory;
		private Dictionary<string, FileStream> _openedFiles = new Dictionary<string, FileStream>();
		// requested name --> actual filename; a File.Exists syscall per lookup miss is a
		// measurable share of a render, and the case-insensitive index also makes lookups
		// behave the same on Linux as on Windows
		private readonly Dictionary<string, string> _index;

		public DirArchive(string path) {
			Directory = path;
			_index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (string f in System.IO.Directory.GetFiles(path))
				_index[Path.GetFileName(f)] = Path.GetFileName(f);
		}

		public bool ContainsFile(string filename) {
			return _index.ContainsKey(filename);
		}

		public VirtualFile OpenFile(string filename, FileFormat format = FileFormat.None, CacheMethod m = CacheMethod.Default) {
			if (!_openedFiles.TryGetValue(filename, out FileStream file)) {
				string actual = _index.TryGetValue(filename, out string a) ? a : filename;
				file = _openedFiles[filename] = new FileStream(Path.Combine(Directory, actual), FileMode.Open, FileAccess.Read, FileShare.Read);
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
