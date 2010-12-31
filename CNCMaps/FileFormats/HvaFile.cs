using System.IO;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	/// <summary>Hva file.</summary>
	class HvaFile : VirtualFile {

		public HvaFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
		}
	}
}