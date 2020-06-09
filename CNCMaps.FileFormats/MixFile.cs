#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers
 * (see https://raw.github.com/OpenRA/OpenRA/master/AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CNCMaps.FileFormats.Encodings;
using CNCMaps.FileFormats.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	public class MixFile : VirtualFile, IArchive {
		public Dictionary<uint, MixEntry> Index;
		long dataStart; // indicates first byte of body, directly after end of mix header
		public MixFile(Stream baseStream, string filename = "", bool isBuffered = false) : this(baseStream, filename, 0, baseStream.Length, isBuffered) { }

		public MixFile(Stream baseStream, string filename, int baseOffset, long fileSize, bool isBuffered = false, bool parseHeader = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
			if (parseHeader)
				ParseHeader();
		}

		public bool ContainsFile(string filename) {
			return Index.ContainsKey(MixEntry.HashFilename(filename));
		}

		public class MixEntry {
			public readonly uint Hash;
			public readonly uint Offset;
			public readonly uint Length;

			public MixEntry(uint hash, uint offset, uint length) {
				Hash = hash;
				Offset = offset;
				Length = length;
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
				//if (filename.Length > 12)
				//	filename = filename.Substring(0, 12);
				filename = filename.ToUpperInvariant();
				int l = filename.Length;
				int a = l >> 2;
				if ((l & 3) != 0) {
					filename += (char)(l - (a << 2));
					int i = 3 - (l & 3);
					while (i-- != 0) filename += filename[a << 2];
				}
				return CRC32.CalculateCrc(Encoding.ASCII.GetBytes(filename));
			}

			static Dictionary<uint, string> Names = new Dictionary<uint, string>();

			public static void AddStandardName(string s) {
				uint hash = HashFilename(s);
				Names.Add(hash, s);
			}

			public const int Size = 12;
		}

		public bool IsValid() {
			Position = 0;
			uint signature = ReadUInt32();
			if ((signature & ~(uint)(MixFileFlags.Encrypted | MixFileFlags.Checksum)) != 0)
				return false;

			if ((signature & (uint)MixFileFlags.Encrypted) != 0) {
				byte[] keyblock = Read(80);
				byte[] blowfishKey = new BlowfishKeyProvider().DecryptKey(keyblock);

				uint[] h = ReadUints(this, 2);
				var fish = new Blowfish(blowfishKey);
				MemoryStream ms = Decrypt(h, fish);
				var reader2 = new BinaryReader(ms);

				ushort numFiles = reader2.ReadUInt16();
				uint dataSize = reader2.ReadUInt32(); // datasize
				return numFiles > 0 && 84 + (6 + numFiles * 12 + 7 & ~7) + dataSize + ((signature & (uint)MixFileFlags.Checksum) != 0 ? 20 : 0) == Length;
			}
			else {
				ushort numFiles = ReadUInt16();
				uint dataSize = ReadUInt32();
				return numFiles > 0 && 4 + 6 + numFiles * 12 + dataSize + ((signature & (uint)MixFileFlags.Checksum) != 0 ? 20 : 0) == Length;
			}
		}

		private void ParseHeader() {
			Position = 0;
			var reader = new BinaryReader(this);

			uint signature = reader.ReadUInt32();
			bool isCncMix = (signature & 0xFFFF) != 0;
			bool isEncrypted = !isCncMix && (signature & (uint)MixFileFlags.Encrypted) != 0;

			VirtualFile header; // After next block this points to a decrypted header
			if (!isCncMix) {
				if (isEncrypted) {
					header = new VirtualFile(DecryptHeader(this));
				}
				else {
					header = this; // implicit no seek; points at beginning of unencrypted header already
				}
			}
			else {
				// first 4 bytes already contained beginning of header, so rewind
				header = this;
				header.Seek(0, SeekOrigin.Begin);
			}

			// Now header can be parsed
			ushort numFiles = header.ReadUInt16();
			uint dataSize = header.ReadUInt32();

			Index = new Dictionary<uint, MixEntry>(numFiles);
			for (int i = 0; i < numFiles; i++) {
				var entry = new MixEntry(header.ReadUInt32(), header.ReadUInt32(), header.ReadUInt32());
				Index[entry.Hash] = entry;
			}

			dataStart = this.Position; // body follows end of header
		}

		MemoryStream DecryptHeader(VirtualFile reader) {
			byte[] keyblock = reader.Read(80);
			byte[] blowfishKey = new BlowfishKeyProvider().DecryptKey(keyblock);

			// Decrypt just the 1st block to determine the number of items, and thereby the length of the header
			var fish = new Blowfish(blowfishKey);
			uint[] h = fish.Decrypt(ReadUints(reader, 2));

			// First 2 decrypted bytes indicate number of files defined in header
			ushort numFiles = (ushort)(h[0] & 0xFFFF);
			// now we can determine the actual header length, rounded up to full number of blocks
			const int blockSize = 8;
			int headerLength = (6 + numFiles * MixEntry.Size + (blockSize - 1)) & ~(blockSize - 1);

			// Decrypt full header
			reader.Position = 84;
			return Decrypt(ReadUints(reader, headerLength / 4), fish);
		}

		static MemoryStream Decrypt(uint[] h, Blowfish fish) {
			uint[] decrypted = fish.Decrypt(h);

			var ms = new MemoryStream();
			var writer = new BinaryWriter(ms);
			foreach (uint t in decrypted)
				writer.Write(t);
			writer.Flush();

			ms.Position = 0;
			return ms;
		}

		uint[] ReadUints(VirtualFile r, int count) {
			var ret = new uint[count];
			for (int i = 0; i < ret.Length; i++)
				ret[i] = r.ReadUInt32();

			return ret;
		}

		public VirtualFile OpenFile(string filename, FileFormat f = FileFormat.None, CacheMethod m = CacheMethod.Default) {
			MixEntry e;
			if (!Index.TryGetValue(MixEntry.HashFilename(filename), out e))
				return null;
			else
				return FormatHelper.OpenAsFormat(BaseStream, filename, (int)(BaseOffset + dataStart + e.Offset), (int)e.Length, f, m);
		}

		public VirtualFile OpenFile(uint mixEntry, string filename = "", FileFormat f = FileFormat.None, CacheMethod m = CacheMethod.Default) {
			var e = Index[mixEntry];
			return FormatHelper.OpenAsFormat(BaseStream, filename, (int)(BaseOffset + dataStart + e.Offset), (int)e.Length, f, m);
		}

		public IEnumerable<uint> AllFileHashes() {
			return Index.Keys;
		}

		[Flags]
		enum MixFileFlags : uint {
			Checksum = 0x10000,
			Encrypted = 0x20000,
		}

	}
}
