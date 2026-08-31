using System;
using System.IO;
using System.Linq;
using System.Text;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.Encodings;
using CNCMaps.FileFormats.Map;
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

	public class PreCaptureTests {

		// Seven tags on one map: a plain hand-over, one whose trigger is disabled, one waiting on a
		// timer, one waiting on a zero timer (which the game springs at once), one naming a start
		// position nobody occupies, one whose second Change House names an occupied slot, and one
		// pointing at a trigger that is not there.
		const string Map = @"[Tags]
0100000A=0,plain,01000001
0100000B=0,off,01000002
0100000C=0,timed,01000003
0100000D=0,nodelay,01000004
0100000E=0,empty slot,01000005
0100000F=0,two actions,01000006
01000010=0,dangling,0100BEEF
[Triggers]
01000001=Neutral,<none>,plain,0,1,1,1,0
01000002=Neutral,<none>,off,1,1,1,1,0
01000003=Neutral,<none>,timed,0,1,1,1,0
01000004=Neutral,<none>,nodelay,0,1,1,1,0
01000005=Neutral,<none>,empty slot,0,1,1,1,0
01000006=Neutral,<none>,two actions,0,1,1,1,0
[Events]
01000001=1,8,0,0
01000002=1,8,0,0
01000003=1,13,0,5
01000004=1,13,0,0
01000005=1,8,0,0
01000006=2,61,2,0,GTGCAN,8,0,0
[Actions]
01000001=1,14,0,4475,0,0,0,0,A
01000002=1,14,0,4475,0,0,0,0,A
01000003=1,14,0,4476,0,0,0,0,A
01000004=1,14,0,4476,0,0,0,0,A
01000005=1,14,0,4478,0,0,0,0,A
01000006=3,14,0,4477,0,0,0,0,A,14,0,4478,0,0,0,0,A,21,6,EVA_Tech,0,0,0,0,A
";

		static IniFile Parse(string content) {
			var bytes = Encoding.ASCII.GetBytes(content);
			return new IniFile(new MemoryStream(bytes), "test.ini", 0, bytes.Length);
		}

		[Fact]
		public void GameStartHandoversResolveToTheirStartSlot() {
			// Start positions A, B and C exist; D does not.
			var available = new[] { true, true, true, false, false, false, false, false };
			var slots = MapFile.ResolveTagOwnerSlots(Parse(Map), available);

			Assert.Equal(0, slots["0100000A"]);            // Any Event
			Assert.Equal(1, slots["0100000D"]);            // Elapsed Time 0 springs at once
			Assert.Equal(2, slots["0100000F"]);            // last action naming an OCCUPIED slot wins
			Assert.False(slots.ContainsKey("0100000B"));   // trigger is disabled
			Assert.False(slots.ContainsKey("0100000C"));   // Elapsed Time 5 has not fired
			Assert.False(slots.ContainsKey("0100000E"));   // nobody starts at D
			Assert.False(slots.ContainsKey("01000010"));   // tag points at a trigger that is not there
		}

		[Fact]
		public void NoStartPositionsMeansNoHandovers() {
			Assert.Empty(MapFile.ResolveTagOwnerSlots(Parse(Map), new bool[8]));
		}

		[Fact]
		public void MissingSectionsAreTolerated() {
			Assert.Empty(MapFile.ResolveTagOwnerSlots(Parse(@"[Basic]
Name=x
"), new bool[8]));
		}
	}
}
