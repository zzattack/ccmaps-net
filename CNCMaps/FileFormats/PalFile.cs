using System.Drawing;
using System.IO;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	public class Palette {
		Color[] colors = new Color[256];
		PalFile originalPalette;

		public Palette(PalFile originalPalette) {
			this.originalPalette = originalPalette;
		}
	}

	public class PalFile : VirtualFile {

		public PalFile(Stream baseStream, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, baseOffset, fileSize, isBuffered) {
		}
	}
}