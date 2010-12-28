using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CNCMaps.Encodings;
using CNCMaps.Encodings.FileFormats;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	class MixFile : VirtualFile, IArchive {

		public MixFile(Stream baseStream, int baseOffset, int fileSize, bool isBuffered = false)
			: base(baseStream, baseOffset, fileSize, isBuffered) {
			ParseHeader();
		}

		public MixFile(Stream baseStream, bool isBuffered = false)
			: base(baseStream, isBuffered) {
			ParseHeader();
		}

		public VirtualFile OpenFile(string filename, bool openAsMix = false) {
			return GetContent(filename, openAsMix);
		}

		public bool ContainsFile(string filename) {
			return index.ContainsKey(MixEntry.HashFilename(filename));
		}

		class MixEntry {
			public readonly uint Hash;
			public readonly uint Offset;
			public readonly uint Length;


			public MixEntry(uint hash, uint offset, uint length) {
				Hash = hash;
				Offset = offset;
				Length = length;
			}

			public MixEntry(BinaryReader r) {
				Hash = r.ReadUInt32();
				Offset = r.ReadUInt32();
				Length = r.ReadUInt32();
			}

			public void Write(BinaryWriter w) {
				w.Write(Hash);
				w.Write(Offset);
				w.Write(Length);
			}

			public override string ToString() {
				string filename;
				if (Names.TryGetValue(Hash, out filename))
					return string.Format("{0} - offset 0x{1:x8} - length 0x{2:x8}", filename, Offset, Length);
				else
					return string.Format("0x{0:x8} - offset 0x{1:x8} - length 0x{2:x8}", Hash, Offset, Length);
			}

			public static uint HashFilename(string filename) {
				if (filename.Length > 12)
					filename = filename.Substring(0, 12);
				filename = filename.ToUpperInvariant();
				int l = filename.Length;
				int a = l >> 2;
				if ((l & 3) != 0) {
					filename += (char)(l - (a << 2));
					int i = 3 - (l & 3);
					while (i-- != 0) filename += filename[a << 2];
				}
				return CNCMaps.Encodings.CRC32.CalculateCrc(Encoding.ASCII.GetBytes(filename));
			}

			static Dictionary<uint, string> Names = new Dictionary<uint, string>();

			public static void AddStandardName(string s) {
				uint hash = HashFilename(s);
				Names.Add(hash, s);
			}

			public const int Size = 12;
		}


		Dictionary<uint, MixEntry> index;
		bool isRmix, isEncrypted;
		long dataStart;

		private void ParseHeader() {
			Position = 0;
			BinaryReader reader = new BinaryReader(this);
			uint signature = reader.ReadUInt32();

			isRmix = 0 == (signature & ~(uint)(MixFileFlags.Checksum | MixFileFlags.Encrypted));

			if (isRmix) {
				isEncrypted = 0 != (signature & (uint)MixFileFlags.Encrypted);
				if (isEncrypted) {
					index = ParseRaHeader(this, out dataStart).ToDictionary(x => x.Hash);
					return;
				}
			}
			else
				Seek(0, SeekOrigin.Begin);

			isEncrypted = false;
			index = ParseTdHeader(this, out dataStart).ToDictionary(x => x.Hash);
		}

		const long headerStart = 84;

		List<MixEntry> ParseRaHeader(Stream s, out long dataStart) {
			BinaryReader reader = new BinaryReader(s);
			byte[] keyblock = reader.ReadBytes(80);
			byte[] blowfishKey = new BlowfishKeyProvider().DecryptKey(keyblock);

			uint[] h = ReadUints(reader, 2);

			Blowfish fish = new Blowfish(blowfishKey);
			MemoryStream ms = Decrypt(h, fish);
			BinaryReader reader2 = new BinaryReader(ms);

			ushort numFiles = reader2.ReadUInt16();
			reader2.ReadUInt32(); /*datasize*/

			s.Position = headerStart;
			reader = new BinaryReader(s);

			int byteCount = 6 + numFiles * MixEntry.Size;
			h = ReadUints(reader, (byteCount + 3) / 4);

			ms = Decrypt(h, fish);

			dataStart = headerStart + byteCount + ((~byteCount + 1) & 7);

			long ds;
			return ParseTdHeader(ms, out ds);
		}

		static MemoryStream Decrypt(uint[] h, Blowfish fish) {
			uint[] decrypted = fish.Decrypt(h);

			MemoryStream ms = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(ms);
			foreach (uint t in decrypted)
				writer.Write(t);
			writer.Flush();

			ms.Position = 0;
			return ms;
		}

		uint[] ReadUints(BinaryReader r, int count) {
			uint[] ret = new uint[count];
			for (int i = 0; i < ret.Length; i++)
				ret[i] = r.ReadUInt32();

			return ret;
		}

		List<MixEntry> ParseTdHeader(Stream s, out long dataStart) {
			List<MixEntry> items = new List<MixEntry>();

			BinaryReader reader = new BinaryReader(s);
			ushort numFiles = reader.ReadUInt16();
			/*uint dataSize = */
			reader.ReadUInt32();

			for (int i = 0; i < numFiles; i++)
				items.Add(new MixEntry(reader));

			dataStart = s.Position;
			return items;
		}

		public VirtualFile GetContent(uint hash, bool openAsMix = false) {
			MixEntry e;
			if (!index.TryGetValue(hash, out e))
				return null;
			if (openAsMix)
				return new MixFile(this.BaseStream, (int)(this.baseOffset + dataStart + e.Offset), (int)e.Length);
			else
				return new VirtualFile(this.BaseStream, (int)(this.baseOffset + dataStart + e.Offset), (int)e.Length, true);
		}

		public VirtualFile GetContent(string filename, bool openAsMix = false) {
			return GetContent(MixEntry.HashFilename(filename), openAsMix);
		}

		public IEnumerable<uint> AllFileHashes() {
			return index.Keys;
		}
		
		[Flags]
		enum MixFileFlags : uint {
			Checksum = 0x10000,
			Encrypted = 0x20000,
		}
	}
}
