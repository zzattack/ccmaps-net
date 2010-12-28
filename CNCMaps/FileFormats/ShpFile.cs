using CNCMaps.VirtualFileSystem;
using System.IO;

namespace CNCMaps.FileFormats {
	class ShpFile : VirtualFile {
		public ShpFile(Stream baseStream, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, baseOffset, fileSize, isBuffered) {
		}
	}
}