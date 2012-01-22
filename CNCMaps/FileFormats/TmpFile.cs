using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using CNCMaps.MapLogic;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	class TmpFile : VirtualFile {

		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		bool isInitialized;
		TmpFileHeader fileHeader;
		List<TmpImage> images;

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct TmpFileHeader {
			public int cblocks_x;                  // width of blocks
			public int cblocks_y;                  // height in blocks
			public int cx;                         // width of each block
			public int cy;                         // height of each block
		};

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct TmpImageHeader {
			public int x;
			public int y;
			public int extra_ofs;
			public int z_ofs;
			public int extra_z_ofs;
			public int x_extra;
			public int y_extra;
			public int cx_extra;
			public int cy_extra;
			private uint datapresency;
			public char height;
			public char terrain_type;
			public char ramp_type;
			public sbyte radar_red_left;
			public sbyte radar_green_left;
			public sbyte radar_blue_left;
			public sbyte radar_red_right;
			public sbyte radar_green_right;
			public sbyte radar_blue_right;
			public byte pad1;
			public byte pad2;
			public byte pad3;

			public bool HasExtraData {
				get {
					return (datapresency & 0x01) == 0x01;
				}
			}
			public bool HasZData {
				get {
					return (datapresency & 0x02) == 0x02;
				}
			}
			public bool HasDamagedData {
				get {
					return (datapresency & 0x04) == 0x04;
				}
			}
			[Flags]
			public enum DataPresencyFlags : uint {
				has_extra_data = 1 << 0,
				has_z_data = 1 << 1,
				has_damaged_data = 1 << 2
			}
		}

		class TmpImage {
			public TmpImageHeader header;
			public byte[] tileData;
			public byte[] extraData;
			public byte[] zData;
		}

		public TmpFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
		}

		public void Initialize() {
			logger.Debug("Initializing TMP data for file {0}", FileName);

			isInitialized = true;
			Position = 0;
			byte[] header = Read(Marshal.SizeOf(typeof(TmpFileHeader)));
			fileHeader = EzMarshal.ByteArrayToStructure<TmpFileHeader>(header);
			byte[] index = Read(fileHeader.cblocks_x * fileHeader.cblocks_y * sizeof(int));

			images = new List<TmpImage>(fileHeader.cblocks_x * fileHeader.cblocks_y);
			for (int x = 0; x < fileHeader.cblocks_x * fileHeader.cblocks_y; x++) {
				int imageData = BitConverter.ToInt32(index, x * 4);
				Position = imageData;
				var img = new TmpImage();
				img.header = EzMarshal.ByteArrayToStructure<TmpImageHeader>(Read(Marshal.SizeOf(typeof(TmpImageHeader))));
				img.tileData = Read(fileHeader.cx * fileHeader.cy / 2);
				if (img.header.HasZData) {
					img.zData = Read(fileHeader.cx * fileHeader.cy / 2);
				}
				if (img.header.HasExtraData) {
					img.extraData = Read(img.header.cx_extra * img.header.cy_extra);
				}
				images.Add(img);
			}
		}


		unsafe public void Draw(MapTile t, DrawingSurface ds) {
			if (!isInitialized) Initialize();

			logger.Trace("Initializing TMP data for file {0}", FileName);
			
			if (t.SubTile >= images.Count) return;
			TmpImage img = images[t.SubTile];
			var zBuf = ds.GetZBuffer();
			Palette p = t.Palette;

			// calculate tile index -> pixel index
			short height = t.Z;
			int x_offset = t.Dx * fileHeader.cx / 2;
			int y_offset = (t.Dy - height) * fileHeader.cy / 2;
			logger.Trace("Drawing TMP file {0} (subtile {1}) at ({2},{3})", FileName, t.SubTile, x_offset, y_offset);

			int stride = ds.bmd.Stride;

			int half_cx = fileHeader.cx / 2,
				half_cy = fileHeader.cy / 2;

			height += (short)img.header.height;

			var w_low = (byte*)ds.bmd.Scan0;
			byte* w_high = (byte*)ds.bmd.Scan0 + stride * ds.bmd.Height;

			byte* w = (byte*)ds.bmd.Scan0 + stride * y_offset + (x_offset + half_cx - 2) * 3;
			int zIdx = y_offset * ds.Width + x_offset + half_cx - 2;

			int cx = 0, // Amount of pixel to copy
				y = 0;

			int rIdx = 0;
			for (; y < half_cy; y++) {
				cx += 4;
				for (ushort c = 0; c < cx; c++) {
					byte paletteValue = img.tileData[rIdx++];
					if (paletteValue != 0 && w_low <= w && w < w_high) {
						*(w + 0) = p.colors[paletteValue].B;
						*(w + 1) = p.colors[paletteValue].G;
						*(w + 2) = p.colors[paletteValue].R;
						zBuf[zIdx] = Math.Max(height, zBuf[zIdx]);
					}
					w += 3;
					zIdx++;
				}
				w += stride - 3 * (cx + 2);
				zIdx += ds.Width - (cx + 2);
			}

			w += 12;
			zIdx += 4;
			for (; y < fileHeader.cy; y++) {
				cx -= 4;
				for (ushort c = 0; c < cx; c++) {
					byte paletteValue = img.tileData[rIdx++];
					if (paletteValue != 0 && w_low <= w && w < w_high) {
						*(w + 0) = p.colors[paletteValue].B;
						*(w + 1) = p.colors[paletteValue].G;
						*(w + 2) = p.colors[paletteValue].R;
						zBuf[zIdx] = Math.Max(height, zBuf[zIdx]);
					}
					w += 3;
					zIdx++;
				}
				w += stride - 3 * (cx - 2);
				zIdx += ds.Width - (cx - 2);
			}

			if (img.header.HasExtraData) {

				rIdx = 0;
				int dx = x_offset + img.header.x_extra - img.header.x;
				int dy = y_offset + img.header.y_extra - img.header.y;
				w = w_low + stride * dy + 3 * dx;
				zIdx = dx + dy * ds.Width;

				// Extra graphics are just a square
				for (y = 0; y < img.header.cy_extra; y++) {
					for (int x = 0; x < img.header.cx_extra; x++) {
						// Checking per line is required because v needs to be checked every time
						byte paletteValue = img.extraData[rIdx++];

						if (paletteValue != 0 && w_low <= w && w < w_high) {
							*w++ = p.colors[paletteValue].B;
							*w++ = p.colors[paletteValue].G;
							*w++ = p.colors[paletteValue].R;

							zBuf[zIdx++] = short.MaxValue;
						}
						else { w += 3; zIdx++; }
					}
					w += stride - img.header.cx_extra * 3;
					zIdx += ds.Width - img.header.cx_extra;
				}
			}
		}
	}
}