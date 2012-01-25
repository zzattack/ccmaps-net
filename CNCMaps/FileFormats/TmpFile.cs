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


		unsafe public void Draw(MapTile tile, DrawingSurface ds) {
			if (!isInitialized) Initialize();

			logger.Trace("Initializing TMP data for file {0}", FileName);

			if (tile.SubTile >= images.Count) return;
			TmpImage img = images[tile.SubTile];
			var zBuf = ds.GetZBuffer();
			var shadows = ds.GetShadows();
			Palette p = tile.Palette;

			// calculate tile index -> pixel index
			short zBufVal = (short)(tile.Rx + tile.Ry + tile.Z);
			int xOffset = tile.Dx * fileHeader.cx / 2;
			int yOffset = (tile.Dy - tile.Z) * fileHeader.cy / 2;
			logger.Trace("Drawing TMP file {0} (subtile {1}) at ({2},{3})", FileName, tile.SubTile, xOffset, yOffset);

			int stride = ds.bmd.Stride;

			int halfCx = fileHeader.cx / 2,
				halfCy = fileHeader.cy / 2;
			
			// writing bounds
			var w_low = (byte*)ds.bmd.Scan0;
			byte* w_high = (byte*)ds.bmd.Scan0 + stride * ds.bmd.Height;
			byte* w = (byte*)ds.bmd.Scan0 + stride * yOffset + (xOffset + halfCx - 2) * 3;

			int rIdx = 0, zIdx = 0, x = 0, y = 0;

			zIdx = yOffset * ds.Width + xOffset + halfCx - 2;
			int cx = 0; // Amount of pixel to copy

			for (; y < halfCy; y++) {
				cx += 4;
				for (ushort c = 0; c < cx; c++) {
					byte paletteValue = img.tileData[rIdx++];
					if (paletteValue != 0 && w_low <= w && w < w_high && zBufVal > zBuf[zIdx]) {
						if (shadows[zIdx] && Math.Abs(zBuf[zIdx]) >= zBufVal) {
							*(w + 0) = (byte)(p.colors[paletteValue].B / 2);
							*(w + 1) = (byte)(p.colors[paletteValue].G / 2);
							*(w + 2) = (byte)(p.colors[paletteValue].R / 2);
						}
						else {
							*(w + 0) = p.colors[paletteValue].B;
							*(w + 1) = p.colors[paletteValue].G;
							*(w + 2) = p.colors[paletteValue].R;
						}
						zBuf[zIdx] = zBufVal;
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
					if (paletteValue != 0 && w_low <= w && w < w_high && zBufVal > zBuf[zIdx]) {
						*(w + 0) = p.colors[paletteValue].B;
						*(w + 1) = p.colors[paletteValue].G;
						*(w + 2) = p.colors[paletteValue].R;

						if (shadows[zIdx] && Math.Abs(zBuf[zIdx]) >= zBufVal) {
							*(w + 0) = (byte)(p.colors[paletteValue].B / 2);
							*(w + 1) = (byte)(p.colors[paletteValue].G / 2);
							*(w + 2) = (byte)(p.colors[paletteValue].R / 2);
						}
						else {
							*(w + 0) = p.colors[paletteValue].B;
							*(w + 1) = p.colors[paletteValue].G;
							*(w + 2) = p.colors[paletteValue].R;
						}
						zBuf[zIdx] = zBufVal;
					}
					w += 3;
					zIdx++;
				}
				w += stride - 3 * (cx - 2);
				zIdx += ds.Width - (cx - 2);
			}


			if (!img.header.HasExtraData) return; // we're done now

			int dx = xOffset + img.header.x_extra - img.header.x;
			int dy = yOffset + img.header.y_extra - img.header.y;
			w = w_low + stride * dy + 3 * dx;
			zIdx = dx + dy * ds.Width;
			rIdx = 0;

			// Extra graphics are just a square
			for (y = 0; y < img.header.cy_extra; y++) {
				for (x = 0; x < img.header.cx_extra; x++) {
					// Checking per line is required because v needs to be checked every time
					byte paletteValue = img.extraData[rIdx++];

					if (paletteValue != 0 && w_low <= w && w < w_high && zBufVal > zBuf[zIdx]) {

						if (shadows[zIdx] ) {
							*w++ = (byte)(p.colors[paletteValue].B / 2);
							*w++ = (byte)(p.colors[paletteValue].G / 2);
							*w++ = (byte)(p.colors[paletteValue].R / 2);
						}
						else {
							*w++ = p.colors[paletteValue].B;
							*w++ = p.colors[paletteValue].G;
							*w++ = p.colors[paletteValue].R;
						}
						//else
						//	w += 3;
						zBuf[zIdx] = zBufVal;
						//shadows[zIdx] = true;
					}
					else
						w += 3;
					zIdx++;
				}
				w += stride - img.header.cx_extra * 3;
				zIdx += ds.Width - img.header.cx_extra;
			}
		}
	}
}