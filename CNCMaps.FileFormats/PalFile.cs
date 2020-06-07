using System.IO;
using CNCMaps.FileFormats.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	public class PalFile : VirtualFile {

		public PalFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
		}

		public byte[] GetOriginalColors() {
			// read originalPalette
			Position = 0;
			return Read(256 * 3);
		}
	}
}