using System.IO;

namespace CNCMaps.VirtualFileSystem {

	/// <summary>Virtual file from a memory buffer.</summary>
	class MemoryFile : VirtualFile {

		public MemoryFile(byte[] buffer, bool isBuffered = true) :
			base(new MemoryStream(buffer), "Memory file", 0, buffer.Length, isBuffered) { }
	}
}