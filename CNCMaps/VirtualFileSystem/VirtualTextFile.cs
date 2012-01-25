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
			get {
				return !Eof;
			}
		}

		public string ReadLine() {
			// works for ascii only!
			if (Eof) return null;

			StringBuilder builder = new StringBuilder(80);
			while (!Eof) {
				char c =(char)ReadByte();
				if (c == '\n')
					break;
				else if (c != '\r')
					builder.Append(c);
			}
			return builder.ToString();
		}

	}
}