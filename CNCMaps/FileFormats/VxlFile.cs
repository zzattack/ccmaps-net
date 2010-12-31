using System.IO;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	class VxlFile : VirtualFile {

		public VxlFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
		}

		public VxlFile(Stream baseStream, string filename = "", bool isBuffered = true)
			: base(baseStream, filename, isBuffered) {
		}
	}
}