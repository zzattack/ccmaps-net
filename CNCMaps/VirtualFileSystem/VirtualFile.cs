using System;
using System.IO;

namespace CNCMaps.VirtualFileSystem {

	/// <summary>
	/// Virtual file class
	/// </summary>
	public class VirtualFile : Stream {
		protected Stream baseStream;
		protected int baseOffset;
		protected long size;
		protected long pos;

		virtual public string FileName { get; set; }

		byte[] buff;
		bool isBuffered;
		bool IsBufferInitialized;

		public VirtualFile(Stream BaseStream, string filename, int baseOffset, long fileSize, bool isBuffered = false) {
			size = fileSize;
			this.baseOffset = baseOffset;
			baseStream = BaseStream;
			this.isBuffered = isBuffered;
			FileName = filename;
		}

		public VirtualFile(Stream BaseStream, string filename = "", bool isBuffered = false) {
			baseStream = BaseStream;
			baseOffset = 0;
			size = BaseStream.Length;
			this.isBuffered = isBuffered;
			FileName = filename;
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

				Array.Copy(buff, pos, buffer, offset, count);
			}
			else {
				// ensure
				baseStream.Position = baseOffset + pos;
				baseStream.Read(buffer, offset, count);
			}
			pos += count;
			return count;
		}

		public unsafe int Read(byte* buffer, int count) {
			count = Math.Min(count, (int)(Length - Position));
			if (isBuffered) {
				if (!IsBufferInitialized)
					InitBuffer();

				for (int i = 0; i < count; i++)
					*buffer++ = buff[pos + i];
			}
			else {
				// ensure
				baseStream.Position = baseOffset + pos;

				byte[] rbuff = Read(count);
				for (int i = 0; i < count; i++)
					*buffer++ = rbuff[pos + i];
			}
			pos += count;
			return count;
		}

		private void InitBuffer() {
			// ensure
			baseStream.Position = baseOffset + pos;
			buff = new byte[size];
			baseStream.Read(buff, 0, (int)size);
			IsBufferInitialized = true;
		}

		public byte[] Read(int numBytes) {
			var ret = new byte[numBytes];
			Read(ret, 0, numBytes);
			return ret;
		}

		public sbyte[] ReadSigned(int numBytes) {
			var b = new byte[numBytes];
			Read(b, 0, numBytes);
			sbyte[] ret = new sbyte[numBytes];
			Buffer.BlockCopy(b, 0, ret, 0, b.Length);
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

		public override void Close() {
			base.Close();
			baseStream.Close();
		}

		public override void SetLength(long value) {
			size = value;
		}

		public override long Position {
			get {
				return pos;
			}
			set {
				pos = value;
				if (!isBuffered && pos + baseOffset != baseStream.Position)
					baseStream.Seek(pos + baseOffset, SeekOrigin.Begin);
			}
		}

		public long Remaining {
			get {
				return Length - pos;
			}
		}

		public bool Eof {
			get {
				return Remaining <= 0;
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

		public override string ToString() {
			return FileName;
		}
	}
}