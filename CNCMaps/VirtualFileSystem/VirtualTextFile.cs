using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CNCMaps.VirtualFileSystem {
	class VirtualTextFile : VirtualFile {
		StreamReader sr;

		public VirtualTextFile(Stream File) : base(File, true) {
			Position = 0;
			sr = new StreamReader(this);
		}

		public VirtualTextFile(Stream File, int baseOffset, long length, bool isBuffered = true)
			: base(File, baseOffset, length, isBuffered) {
			Position = 0;
			sr = new StreamReader(this);
		}

		public override bool CanRead {
			get {
				return pos < size || !sr.EndOfStream;
			}
		}
		public string ReadLine() {
			return sr.ReadLine();
		}

		public string ReadToEnd() {
			return sr.ReadToEnd();
		}

		public int Peek() {
			return sr.Peek();
		}

		public int ReadBlock(char[] buffer, int index, int count) {
			return sr.ReadBlock(buffer, index, count);
		}

	}
}
