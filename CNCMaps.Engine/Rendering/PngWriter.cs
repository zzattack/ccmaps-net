using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace CNCMaps.Engine.Rendering {
	/// <summary>
	/// Minimal PNG writer for truecolor images with unfiltered scanlines, using
	/// parallel deflate: the raw stream is split into blocks that are compressed
	/// concurrently as sync-flushed fragments (pigz-style) and concatenated into a
	/// single valid zlib stream. Encodes large map renders an order of magnitude
	/// faster than a serial encoder.
	/// </summary>
	static class PngWriter {
		const int MinChunkSize = 1 << 20;

		/// <param name="data">source pixels, B,G,R(,A) byte order</param>
		/// <param name="surfaceWidth">width of the source buffer in pixels</param>
		/// <param name="bytesPerPixel">3 (BGR) or 4 (BGRA, alpha is dropped)</param>
		/// <param name="rect">region of the source to save</param>
		/// <param name="compressionLevel">1-9, mapped onto the deflate implementation's levels</param>
		public static void Save(string path, byte[] data, int surfaceWidth, int bytesPerPixel, System.Drawing.Rectangle rect, int compressionLevel, Action<double> progress = null) {
			int rowBytes = rect.Width * 3;
			var raw = new byte[(rowBytes + 1) * rect.Height];

			// build the filtered stream: every scanline prefixed with filter type 0,
			// pixels converted from BGR memory order to PNG's RGB
			Parallel.For(0, rect.Height, y => {
				int rawIdx = y * (rowBytes + 1);
				raw[rawIdx++] = 0; // filter: None
				int srcIdx = ((rect.Top + y) * surfaceWidth + rect.Left) * bytesPerPixel;
				for (int x = 0; x < rect.Width; x++) {
					raw[rawIdx++] = data[srcIdx + 2]; // r
					raw[rawIdx++] = data[srcIdx + 1]; // g
					raw[rawIdx++] = data[srcIdx + 0]; // b
					srcIdx += bytesPerPixel;
				}
			});

			var level = compressionLevel <= 3 ? CompressionLevel.Fastest
				: compressionLevel <= 7 ? CompressionLevel.Optimal
				: CompressionLevel.SmallestSize;

			// compress independent blocks concurrently; Flush() performs a zlib sync
			// flush, leaving each fragment byte-aligned and without a final block, so
			// the concatenation below forms a single valid deflate stream
			int chunkCount = Math.Clamp(raw.Length / MinChunkSize, 1, Environment.ProcessorCount);
			int chunkSize = (raw.Length + chunkCount - 1) / chunkCount;
			var fragments = new byte[chunkCount][];
			var adlers = new uint[chunkCount];
			var lengths = new int[chunkCount];
			int blocksDone = 0;
			Parallel.For(0, chunkCount, i => {
				int off = i * chunkSize;
				int len = Math.Min(chunkSize, raw.Length - off);
				lengths[i] = len;
				adlers[i] = Adler32(raw.AsSpan(off, len));
				var ms = new MemoryStream();
				var ds = new DeflateStream(ms, level, true);
				ds.Write(raw, off, len);
				ds.Flush();
				fragments[i] = ms.ToArray(); // snapshot before Dispose would emit a final block
				ds.Dispose();
				progress?.Invoke((double)Interlocked.Increment(ref blocksDone) / chunkCount);
			});

			uint adler = 1;
			for (int i = 0; i < chunkCount; i++)
				adler = Adler32Combine(adler, adlers[i], lengths[i]);

			using (var f = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16)) {
				// signature
				f.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

				// IHDR: 8-bit truecolor
				var ihdr = new byte[13];
				WriteBE(ihdr, 0, (uint)rect.Width);
				WriteBE(ihdr, 4, (uint)rect.Height);
				ihdr[8] = 8;  // bit depth
				ihdr[9] = 2;  // color type: truecolor
				WriteChunk(f, "IHDR", ihdr);

				// IDAT: zlib header + deflate fragments + final empty stored block + adler
				long idatLen = 2 + 5 + 4;
				foreach (var frag in fragments) idatLen += frag.Length;
				WriteBE(f, (uint)idatLen);
				var idatType = new byte[] { (byte)'I', (byte)'D', (byte)'A', (byte)'T' };
				f.Write(idatType, 0, 4);
				uint crc = UpdateCrc(0xFFFFFFFF, idatType);
				void Put(byte[] b) { f.Write(b, 0, b.Length); crc = UpdateCrc(crc, b); }
				Put(new byte[] { 0x78, 0x9C });
				foreach (var frag in fragments) Put(frag);
				Put(new byte[] { 0x01, 0x00, 0x00, 0xFF, 0xFF }); // final empty stored block
				var adlerBytes = new byte[4];
				WriteBE(adlerBytes, 0, adler);
				Put(adlerBytes);
				WriteBE(f, crc ^ 0xFFFFFFFF);

				WriteChunk(f, "IEND", Array.Empty<byte>());
			}
		}

		static void WriteChunk(Stream s, string type, byte[] payload) {
			WriteBE(s, (uint)payload.Length);
			var typeBytes = new byte[] { (byte)type[0], (byte)type[1], (byte)type[2], (byte)type[3] };
			s.Write(typeBytes, 0, 4);
			s.Write(payload, 0, payload.Length);
			uint crc = UpdateCrc(UpdateCrc(0xFFFFFFFF, typeBytes), payload);
			WriteBE(s, crc ^ 0xFFFFFFFF);
		}

		static void WriteBE(Stream s, uint v) {
			s.Write(new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v }, 0, 4);
		}

		static void WriteBE(byte[] b, int offset, uint v) {
			b[offset] = (byte)(v >> 24);
			b[offset + 1] = (byte)(v >> 16);
			b[offset + 2] = (byte)(v >> 8);
			b[offset + 3] = (byte)v;
		}

		#region checksums

		const uint AdlerMod = 65521;

		static uint Adler32(ReadOnlySpan<byte> data) {
			uint a = 1, b = 0;
			int i = 0;
			while (i < data.Length) {
				// process in runs small enough to defer the expensive modulo
				int run = Math.Min(5552, data.Length - i);
				for (int j = 0; j < run; j++) {
					a += data[i + j];
					b += a;
				}
				a %= AdlerMod;
				b %= AdlerMod;
				i += run;
			}
			return (b << 16) | a;
		}

		static uint Adler32Combine(uint adler1, uint adler2, long len2) {
			// zlib's adler32_combine
			uint rem = (uint)(len2 % AdlerMod);
			uint sum1 = adler1 & 0xFFFF;
			uint sum2 = (rem * sum1) % AdlerMod;
			sum1 += (adler2 & 0xFFFF) + AdlerMod - 1;
			sum2 += (adler1 >> 16) + (adler2 >> 16) + AdlerMod - rem;
			if (sum1 >= AdlerMod) sum1 -= AdlerMod;
			if (sum1 >= AdlerMod) sum1 -= AdlerMod;
			if (sum2 >= AdlerMod * 2) sum2 -= AdlerMod * 2;
			if (sum2 >= AdlerMod) sum2 -= AdlerMod;
			return (sum2 << 16) | sum1;
		}

		static readonly uint[] CrcTable = BuildCrcTable();

		static uint[] BuildCrcTable() {
			var table = new uint[256];
			for (uint n = 0; n < 256; n++) {
				uint c = n;
				for (int k = 0; k < 8; k++)
					c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
				table[n] = c;
			}
			return table;
		}

		static uint UpdateCrc(uint crc, ReadOnlySpan<byte> data) {
			foreach (byte b in data)
				crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
			return crc;
		}

		#endregion
	}
}
