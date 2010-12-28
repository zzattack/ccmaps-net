using CNCMaps.VirtualFileSystem;
using System.IO;

namespace CNCMaps.FileFormats {
	class HvaFile : VirtualFile {
		public HvaFile(Stream baseStream, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, baseOffset, fileSize, isBuffered) {
		}
	}
}