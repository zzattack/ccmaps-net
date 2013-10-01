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
using System.IO;
using CNCMaps.FileFormats.VirtualFileSystem;

namespace CNCMaps.FileFormats.Encodings {
	public static class Format80 {
		private static void ReplicatePrevious(byte[] dest, int destIndex, int srcIndex, int count) {
			if (srcIndex > destIndex)
				throw new NotImplementedException(string.Format("srcIndex > destIndex  {0}  {1}", srcIndex, destIndex));

			if (destIndex - srcIndex == 1) {
				for (int i = 0; i < count; i++)
					dest[destIndex + i] = dest[destIndex - 1];
			}
			else {
				for (int i = 0; i < count; i++)
					dest[destIndex + i] = dest[srcIndex + i];
			}
		}

		public static int DecodeInto(byte[] src, byte[] dest) {
			VirtualFile ctx = new MemoryFile(src);
			int destIndex = 0;

			while (true) {
				byte i = ctx.ReadByte();
				if ((i & 0x80) == 0) {
					// case 2
					byte secondByte = ctx.ReadByte();
					int count = ((i & 0x70) >> 4) + 3;
					int rpos = ((i & 0xf) << 8) + secondByte;

					ReplicatePrevious(dest, destIndex, destIndex - rpos, count);
					destIndex += count;
				}
				else if ((i & 0x40) == 0) {
					// case 1
					int count = i & 0x3F;
					if (count == 0)
						return destIndex;

					ctx.Read(dest, destIndex, count);
					destIndex += count;
				}
				else {
					int count3 = i & 0x3F;
					if (count3 == 0x3E) {
						// case 4
						int count = ctx.ReadInt16();
						byte color = ctx.ReadByte();

						for (int end = destIndex + count; destIndex < end; destIndex++)
							dest[destIndex] = color;
					}
					else if (count3 == 0x3F) {
						// case 5
						int count = ctx.ReadInt16();
						int srcIndex = ctx.ReadInt16();
						if (srcIndex >= destIndex)
							throw new NotImplementedException(string.Format("srcIndex >= destIndex  {0}  {1}", srcIndex, destIndex));

						for (int end = destIndex + count; destIndex < end; destIndex++)
							dest[destIndex] = dest[srcIndex++];
					}
					else {
						// case 3
						int count = count3 + 3;
						int srcIndex = ctx.ReadInt16();
						if (srcIndex >= destIndex)
							throw new NotImplementedException(string.Format("srcIndex >= destIndex  {0}  {1}", srcIndex, destIndex));

						for (int end = destIndex + count; destIndex < end; destIndex++)
							dest[destIndex] = dest[srcIndex++];
					}
				}
			}
		}

		public static unsafe uint DecodeInto(byte* src, byte* dest) {
			byte* pdest = dest;
			byte* readp = src;
			byte* writep = dest;

			while (true) {
				byte code = *readp++;
				byte* copyp;
				int count;
				if ((~code & 0x80) != 0) {
					//bit 7 = 0
					//command 0 (0cccpppp p): copy
					count = (code >> 4) + 3;
					copyp = writep - (((code & 0xf) << 8) + *readp++);
					while (count-- != 0)
						*writep++ = *copyp++;
				}
				else {
					//bit 7 = 1
					count = code & 0x3f;
					if ((~code & 0x40) != 0) {
						//bit 6 = 0
						if (count == 0)
							//end of image
							break;
						//command 1 (10cccccc): copy
						while (count-- != 0)
							*writep++ = *readp++;
					}
					else {
						//bit 6 = 1
						if (count < 0x3e) {
							//command 2 (11cccccc p p): copy
							count += 3;
							copyp = &pdest[*(ushort*) readp];

							readp += 2;
							while (count-- != 0)
								*writep++ = *copyp++;
						}
						else if (count == 0x3e) {
							//command 3 (11111110 c c v): fill
							count = *(ushort*) readp;
							readp += 2;
							code = *readp++;
							while (count-- != 0)
								*writep++ = code;
						}
						else {
							//command 4 (copy 11111111 c c p p): copy
							count = *(ushort*) readp;
							readp += 2;
							copyp = &pdest[*(ushort*) readp];
							readp += 2;
							while (count-- != 0)
								*writep++ = *copyp++;
						}
					}
				}
			}

			return (uint) (dest - pdest);
		}

		public static byte[] Encode(byte[] src) {
			/* quick & dirty format80 encoder -- only uses raw copy operator, terminated with a zero-run. */
			/* this does not produce good compression, but it's valid format80 */
			var ctx = new MemoryFile(src);
			var ms = new MemoryStream();

			do {
				var len = Math.Min(ctx.Position, 0x3F);
				ms.WriteByte((byte) (0x80 | len));
				while (len-- > 0)
					ms.WriteByte(ctx.ReadByte());
			} while (!ctx.Eof);

			ms.WriteByte(0x80); // terminator -- 0-length run.

			return ms.ToArray();
		}
	}
}