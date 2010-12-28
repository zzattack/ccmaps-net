using CNCMaps.VirtualFileSystem;
using System.IO;

namespace CNCMaps.FileFormats {
	class TmpFile : VirtualFile {
		public TmpFile(Stream baseStream, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, baseOffset, fileSize, isBuffered) {
		}
	}
}