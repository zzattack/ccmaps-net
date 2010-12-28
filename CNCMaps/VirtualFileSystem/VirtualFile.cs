using System.IO;
using System;

namespace CNCMaps.VirtualFileSystem {
	/// <summary>
	/// Virtual file class
	/// </summary>
	class VirtualFile : Stream {
		protected Stream BaseStream;
		protected int baseOffset;
		protected long size;
		protected long pos = 0;
		virtual public string FileName { get; set; }
		byte[] buff;
		bool isBuffered;
		bool IsBufferInitialized = false;

		public VirtualFile(Stream BaseStream, int baseOffset, int fileSize, bool isBuffered = false) {
			this.size = fileSize;
			this.baseOffset = baseOffset;
			this.BaseStream = BaseStream;
			this.isBuffered = isBuffered;
		}

		public VirtualFile(Stream BaseStream, bool isBuffered = false) {
			this.BaseStream = BaseStream;
			this.baseOffset = 0;
			this.size = BaseStream.Length;
			this.isBuffered = isBuffered;
		}

		public override bool CanRead {
			get { return pos < size; }
		}

		public override bool CanWrite {
			get { return false; }
		}

		public override long Length {
			get { return size; }
		}

		public override void Flush() {
		}

		public override int Read(byte[] buffer, int offset, int count) {
			count = Math.Min(count, (int)(Length - Position));
			if (isBuffered) {
				if (!IsBufferInitialized)
					InitBuffer();
				
				Array.Copy(this.buff, pos, buffer, offset, count);
			}
			else {
				// ensure
				BaseStream.Position = baseOffset + pos;
				BaseStream.Read(buffer, offset, count);
			}
			pos += count;
			return count;
		}

		private void InitBuffer() {

			// ensure
			BaseStream.Position = baseOffset + pos;
			this.buff = new byte[this.size];
			BaseStream.Read(this.buff, 0, (int)this.size);
			IsBufferInitialized = true;
		}

		public byte[] Read(int numBytes) {
			byte[] ret = new byte[numBytes];
			Read(ret, 0, numBytes);
			return ret;
		}

		public new byte ReadByte() {
			return ReadUInt8();
		}
		public byte ReadUInt8() {
			return Read(1)[0];
		}
		public int ReadInt32() {
			return BitConverter.ToInt32(Read(sizeof(Int32)), 0);
		}
		public uint ReadUInt32() {
			return BitConverter.ToUInt32(Read(sizeof(UInt32)), 0);
		}
		public short ReadInt16() {
			return BitConverter.ToInt16(Read(sizeof(Int16)), 0);
		}
		public ushort ReadUInt16() {
			return BitConverter.ToUInt16(Read(sizeof(UInt16)), 0);
		}
		public float ReadFloat() {
			return BitConverter.ToSingle(Read(sizeof(Single)), 0);
		}
		public double ReadDouble() {
			return BitConverter.ToDouble(Read(sizeof(Double)), 0);
		}		

		public override void Write(byte[] buffer, int offset, int count) {
			throw new NotSupportedException();
		}

		public override void SetLength(long value) {
			this.size = value;
		}

		public override long Position {
			get {
				return pos;
			}
			set {
				pos = value;
				if (!isBuffered)
					BaseStream.Seek(pos + baseOffset, SeekOrigin.Begin);
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

	}
}