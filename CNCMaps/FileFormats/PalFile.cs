using System.Drawing;
using System.IO;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	public class PalFile : VirtualFile {

		public PalFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
		}
	}
}