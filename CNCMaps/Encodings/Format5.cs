using System;
using System.IO;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.Encodings {
	// basec on code from XCC mixer
	
	class Format5 {

		public unsafe static uint DecodeInto(byte[] src, byte[] dest, int format = 5) {
			fixed (byte* pr = src, pw = dest) {
				byte* r = pr, w = pw;
				byte* w_end = w + dest.Length;

				while (w < w_end) {
					ushort size_in = *(ushort*)r;
					r += 2;
					ushort size_out = *(ushort*)r;
					r += 2;

					if (size_in == 0 || size_out == 0)
						break;

					if (format == 80)
						Format80.DecodeInto(r, w);
					else
						MiniLZO.Decompress(r, w, size_in, size_out);
					r += size_in;
					w += size_out;
				}
				return (uint)(w - pw);
			}
		}

		public unsafe static byte[] EncodeSection(byte[] s) {
			byte[] compressed; // 128kb
			MiniLZO.Compress(s, out compressed);
			return compressed;
		}

		public unsafe static byte[] Encode(byte[] source, int format) {
			byte[] dest = new byte[source.Length*2];
			MemoryFile src = new MemoryFile(source);

			int w = 0;
			while (!src.Eof) {
				short cb_in = (short)Math.Min(src.Remaining, 8192);
				var chunk_in = src.Read((int)cb_in);
				var chunk_out = format == 80 ? Format80.Encode(chunk_in) : Format5.EncodeSection(chunk_in);
				uint cb_out = (ushort)chunk_out.Length;

				Array.Copy(BitConverter.GetBytes(cb_out), 0, dest, w, 2);
				w += 2;
				Array.Copy(BitConverter.GetBytes(cb_in), 0, dest, w, 2);
				w += 2;
				Array.Copy(chunk_out, 0, dest, w, chunk_out.Length);
				w += chunk_out.Length;
			}
			Array.Resize(ref dest, w);
			return dest;
		}
	}
}