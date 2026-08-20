using System;
using CNCMaps.FileFormats.VirtualFileSystem;

namespace CNCMaps.FileFormats.Encodings {
	// basec on code from XCC mixer

	public class Format5 {
		/// <summary>
		/// Decode a chunked stream into <paramref name="dest"/>. Map files are
		/// untrusted input, so every chunk header is checked against both buffers
		/// before it is used: a truncated stream or a chunk claiming more output
		/// than is left simply ends the loop instead of running off the end.
		/// </summary>
		public static unsafe uint DecodeInto(byte[] src, byte[] dest, int format = 5) {
			// LZO copies in 4-byte steps and may overshoot a run's logical end, so
			// chunks decompress into a scratch buffer with headroom and are then
			// copied across; dest itself is never written past its own length.
			byte[] scratch = format == 80 ? null : new byte[ushort.MaxValue + MiniLZO.LZO_OUT_SLACK];

			fixed (byte* pr = src, pw = dest, ps = scratch) {
				byte* r = pr, w = pw;
				byte* r_end = r + src.Length;
				byte* w_end = w + dest.Length;

				while (w < w_end) {
					if (r + 4 > r_end)
						break;                       // truncated chunk header
					ushort size_in = *(ushort*)r;
					r += 2;
					uint size_out = *(ushort*)r;
					r += 2;

					if (size_in == 0 || size_out == 0)
						break;
					if (r + size_in > r_end)
						break;                       // chunk body runs past the input
					if (size_out > w_end - w)
						break;                       // chunk claims more than dest has left

					if (format == 80) {
						Format80.DecodeInto(r, w);
					}
					else {
						uint produced = size_out;
						int status = MiniLZO.Decompress(r, size_in, ps, ref produced);
						if (status < 0 || produced > size_out)
							break;                   // corrupt chunk; keep what we have
						Buffer.MemoryCopy(ps, w, w_end - w, produced);
						size_out = produced;
					}
					r += size_in;
					w += size_out;
				}
				return (uint)(w - pw);
			}
		}

		public static byte[] EncodeSection(byte[] s) {
			return MiniLZO.Compress(s);
		}

		public static byte[] Encode(byte[] source, int format) {
			var dest = new byte[source.Length * 2];
			var src = new MemoryFile(source);

			int w = 0;
			while (!src.Eof) {
				var cb_in = (short)Math.Min(src.Remaining, 8192);
				var chunk_in = src.Read(cb_in);
				var chunk_out = format == 80 ? Format80.Encode(chunk_in) : EncodeSection(chunk_in);
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