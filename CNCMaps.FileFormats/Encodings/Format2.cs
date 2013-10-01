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

using CNCMaps.FileFormats.VirtualFileSystem;

namespace CNCMaps.FileFormats.Encodings {
	public static class Format2 {
		public static uint DecodeInto(byte[] src, byte[] dest) {
			VirtualFile vfile = new MemoryFile(src);
			uint i = 0;
			while (vfile.CanRead) {
				byte cmd = vfile.ReadByte();
				if (cmd == 0) {
					byte count = vfile.ReadUInt8();
					while (count-- > 0)
						dest[i++] = 0;
				}
				else
					dest[i++] = cmd;
			}
			return i;
		}

		public static unsafe uint DecodeInto(byte* src, uint srcLen, byte* dest) {
			uint idx = 0;
			while (idx < srcLen) {
				byte cmd = *src++;
				if (cmd == 0) {
					byte count = *src;
					while (count-- > 0)
						dest[idx++] = 0;
				}
				else
					dest[idx++] = cmd;
			}
			return idx;
		}
	}
}