using System.IO;

namespace CNCMaps.VirtualFileSystem {

	class VirtualFile : FileStream {
		FileStream file;
		int base_offset;
		int size;
		int pos = 0;
		FileStream fs;

		public VirtualFile()
			: base() {
		}

		public override bool CanWrite {
			get { return false; }
		}

		public override long Length {
			get { return size; }
		}

		public override void SetLength(long value) {
			this.size = value;
		}

		public override long Position {
			get {
				return fs.Position - base_offset;
			}
			set {
				fs.Seek(base_offset + value, SeekOrigin.Begin);
			}
		}

		public override bool CanSeek {
			get { return true; }
		}

		public override long Seek(long offset, SeekOrigin origin) {
			switch (origin) {
				case SeekOrigin.Begin:
					Position = offset;
					break;
				case SeekOrigin.Current:
					Position += offset;
					break;
				case SeekOrigin.End:
					Position = Length - offset;
					break;
			}
			return Position;
		}

		override
	}
}