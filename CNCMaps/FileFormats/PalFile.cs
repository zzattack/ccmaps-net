using CNCMaps.VirtualFileSystem;
using System.IO;

namespace CNCMaps.FileFormats {
	class PalFile : VirtualFile {
		public PalFile(Stream baseStream, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, baseOffset, fileSize, isBuffered) {
		}
	}
}