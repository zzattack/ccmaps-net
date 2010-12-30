using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CNCMaps.VirtualFileSystem {
	class MemoryFile : VirtualFile {
		public MemoryFile(byte[] buffer, bool isBuffered = true) :
			base(new MemoryStream(buffer), 0, buffer.Length, isBuffered) { }
	}
}
