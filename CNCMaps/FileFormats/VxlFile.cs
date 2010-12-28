using CNCMaps.VirtualFileSystem;
using System.IO;

namespace CNCMaps.FileFormats {
	class VxlFile : VirtualFile {
		public VxlFile(Stream baseStream, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, baseOffset, fileSize, isBuffered) {
		}
		public VxlFile(Stream baseStream, bool isBuffered = true)
			: base(baseStream, isBuffered) {
		}
	}
}