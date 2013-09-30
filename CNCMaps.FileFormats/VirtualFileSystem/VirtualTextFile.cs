using System.IO;
using System.Text;

namespace CNCMaps.VirtualFileSystem {

	public class VirtualTextFile : VirtualFile {
		public VirtualTextFile(Stream file, string filename = "")
			: base(file, filename, true) {
			Position = 0;
		}

		public VirtualTextFile(Stream file, string filename, int baseOffset, long length, bool isBuffered = true)
			: base(file, filename, baseOffset, length, isBuffered) {
			Position = 0;
		}

		public override bool CanRead {
			get { return !Eof; }
		}

		public virtual string ReadLine() {
			// works for ascii only!
			var builder = new StringBuilder(80);
			while (CanRead) {
				char c = (char)ReadByte();
				if (c == '\n')
					break;
				else if (c != '\r')
					builder.Append(c);
			}
			return builder.ToString();
		}

	}
}