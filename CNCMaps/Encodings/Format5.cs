namespace CNCMaps.Encodings {

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
						ManagedLZO.MiniLZO.Decompress(r, w, size_in, size_out);
					r += size_in;
					w += size_out;
				}
				return (uint)(w - pw);
			}
		}
	}
}