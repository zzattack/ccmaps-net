using System;
using System.IO;
using System.Linq;
using System.Text;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.Encodings;
using CNCMaps.FileFormats.VirtualFileSystem;
using Xunit;

namespace CNCMaps.Tests {

	/// <summary>A stream that returns at most a few bytes per Read call, like modern
	/// .NET streams are allowed to; used to verify partial-read robustness.</summary>
	class TricklingStream : MemoryStream {
		public TricklingStream(byte[] data) : base(data) { }
		public override int Read(byte[] buffer, int offset, int count) {
			return base.Read(buffer, offset, Math.Min(count, 3));
		}
	}

	public class VirtualFileTests {

		[Fact]
		public void BufferedRead_SurvivesPartialUnderlyingReads() {
			var data = Enumerable.Range(0, 100_000).Select(i => (byte)(i * 31)).ToArray();
			var vf = new VirtualFile(new TricklingStream(data), "test", 0, data.Length, isBuffered: true);
			var read = vf.Read(data.Length);
			Assert.Equal(data, read);
		}

		[Fact]
		public void UnbufferedRead_SurvivesPartialUnderlyingReads() {
			var data = Enumerable.Range(0, 10_000).Select(i => (byte)(i * 17)).ToArray();
			var vf = new VirtualFile(new TricklingStream(data), "test", 0, data.Length, isBuffered: false);
			var read = vf.Read(data.Length);
			Assert.Equal(data, read);
		}

		[Fact]
		public void OffsetRead_ReturnsCorrectSlice() {
			var data = Enumerable.Range(0, 1000).Select(i => (byte)i).ToArray();
			var vf = new VirtualFile(new TricklingStream(data), "test", 100, 50, isBuffered: true);
			var read = vf.Read(50);
			Assert.Equal(data.Skip(100).Take(50).ToArray(), read);
		}
	}

	public class Format5Tests {

		[Theory]
		[InlineData(4)]  // format 80
		[InlineData(5)]  // format 5 (preview pack)
		public void EncodeDecode_Roundtrips(int format) {
			var rnd = new Random(42);
			var data = new byte[30_000];
			rnd.NextBytes(data);
			// mix in compressible spans
			Array.Fill(data, (byte)7, 1000, 5000);

			var encoded = Format5.Encode(data, format);
			var decoded = new byte[data.Length];
			Format5.DecodeInto(encoded, decoded, format);
			Assert.Equal(data, decoded);
		}

		/// <summary>
		/// Map files are untrusted input and some in the wild are malformed. Before
		/// the chunk headers were validated, a chunk claiming more output than the
		/// destination held ran the LZO decompressor off the end of the buffer and
		/// killed the process with an AccessViolationException.
		/// </summary>
		[Fact]
		public void DecodeInto_ChunkLargerThanDestination_DoesNotOverrun() {
			var data = new byte[20_000];
			Array.Fill(data, (byte)3);
			var encoded = Format5.Encode(data, 5);

			var tooSmall = new byte[500];
			uint written = Format5.DecodeInto(encoded, tooSmall, 5);

			Assert.True(written <= tooSmall.Length);
		}

		[Fact]
		public void DecodeInto_TruncatedStream_StopsCleanly() {
			var data = new byte[20_000];
			Array.Fill(data, (byte)9);
			var encoded = Format5.Encode(data, 5);

			foreach (int keep in new[] { 1, 2, 3, 4, 5, 17, encoded.Length / 2, encoded.Length - 1 }) {
				var truncated = encoded.Take(keep).ToArray();
				var decoded = new byte[data.Length];
				uint written = Format5.DecodeInto(truncated, decoded, 5);
				Assert.True(written <= decoded.Length);
			}
		}

		[Fact]
		public void DecodeInto_GarbageChunkBody_StopsCleanly() {
			var rnd = new Random(7);
			var garbage = new byte[8192];
			rnd.NextBytes(garbage);
			// plausible header, nonsense payload
			garbage[0] = 0x00; garbage[1] = 0x10;   // size_in  = 4096
			garbage[2] = 0x00; garbage[3] = 0x20;   // size_out = 8192

			var decoded = new byte[8192];
			uint written = Format5.DecodeInto(garbage, decoded, 5);

			Assert.True(written <= decoded.Length);
		}
	}

	public class IniFileTests {

		static IniFile Parse(string content) {
			var bytes = Encoding.ASCII.GetBytes(content);
			return new IniFile(new MemoryStream(bytes), "test.ini", 0, bytes.Length);
		}

		[Fact]
		public void SectionsAndValues_AreParsed() {
			var ini = Parse("[General]\nName=Test Map\nSize=0,0,100,200\n\n[Waypoints]\n0=45019\n");
			Assert.Equal("Test Map", ini.GetSection("General").ReadString("Name"));
			Assert.Equal("45019", ini.GetSection("Waypoints").ReadString("0"));
			Assert.Equal("", ini.GetSection("General").ReadString("Missing"));
		}

		[Fact]
		public void Comments_AreIgnored()  {
			var ini = Parse("[S]\nA=1;inline comment\n;full line comment\nB=2\n");
			Assert.Equal("1", ini.GetSection("S").ReadString("A"));
			Assert.Equal("2", ini.GetSection("S").ReadString("B"));
		}
	}
}
