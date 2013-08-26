﻿using System;
using System.Collections.Generic;
using System.Drawing;
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
			public byte height;
			public byte terrain_type;
			public byte ramp_type;
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
			public byte[] extraZData;
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
				if (img.header.HasZData && img.header.HasExtraData && 0 < img.header.extra_z_ofs && img.header.extra_z_ofs < Length) {
					img.extraZData = Read(img.header.cx_extra * img.header.cy_extra);
				}
				images.Add(img);
			}
		}


		unsafe public void Draw(MapTile tile, DrawingSurface ds) {
			if (!isInitialized) Initialize();

			logger.Trace("Initializing TMP data for file {0}", FileName);

			if (tile.SubTile >= images.Count) return;
			TmpImage img = images[tile.SubTile];
			var zBuffer = ds.GetZBuffer();
			Palette p = tile.Palette;

			// calculate tile index -> pixel index
			short zBufVal = (short)((tile.Rx + tile.Ry) * fileHeader.cy / 2);
			int xOffset = tile.Dx * fileHeader.cx / 2;
			int yOffset = (tile.Dy - tile.Z) * fileHeader.cy / 2;

			// make touched tiles (used for determining image cutoff)
			int gx = tile.Dx, gy = (tile.Dy - tile.Z) / 2;
			if (gx >= 0 && gy >= 0 && gx < tile.Layer.GridTouched.GetLength(0) && gy < tile.Layer.GridTouched.GetLength(1)) {
				tile.Layer.GridTouched[gx, gy] |= TileLayer.TouchType.ByNormalData;
				tile.Layer.GridTouchedBy[gx, gy] = tile;
			}

			logger.Trace("Drawing TMP file {0} (subtile {1}) at ({2},{3})", FileName, tile.SubTile, xOffset, yOffset);

			int stride = ds.bmd.Stride;

			int halfCx = fileHeader.cx / 2,
				halfCy = fileHeader.cy / 2;

			// writing bounds
			var w_low = (byte*)ds.bmd.Scan0;
			byte* w_high = (byte*)ds.bmd.Scan0 + stride * ds.bmd.Height;
			byte* w = (byte*)ds.bmd.Scan0 + stride * yOffset + (xOffset + halfCx - 2) * 3;

			int rIdx = 0, x, y = 0;
			int zIdx = yOffset * ds.Width + xOffset + halfCx - 2;
			int cx = 0; // Amount of pixel to copy

			for (; y < halfCy; y++) {
				cx += 4;
				for (ushort c = 0; c < cx; c++) {
					byte paletteValue = img.tileData[rIdx];
					short z = (img.zData != null) ? (short)(zBufVal - img.zData[rIdx]) : short.MaxValue;
					if (w_low <= w && w < w_high && z >= zBuffer[zIdx]) {
						*(w + 0) = p.colors[paletteValue].B;
						*(w + 1) = p.colors[paletteValue].G;
						*(w + 2) = p.colors[paletteValue].R;
						zBuffer[zIdx] = z;
					}
					w += 3;
					zIdx++;
					rIdx++;
				}
				w += stride - 3 * (cx + 2);
				zIdx += ds.Width - (cx + 2);
			}

			w += 12;
			zIdx += 4;
			for (; y < fileHeader.cy; y++) {
				cx -= 4;
				for (ushort c = 0; c < cx; c++) {
					byte paletteValue = img.tileData[rIdx];
					short z = (img.zData != null) ? (short)(zBufVal - img.zData[rIdx]) : short.MaxValue;
					if (w_low <= w && w < w_high && z >= zBuffer[zIdx]) {
						*(w + 0) = p.colors[paletteValue].B;
						*(w + 1) = p.colors[paletteValue].G;
						*(w + 2) = p.colors[paletteValue].R;
						zBuffer[zIdx] = z;
					}
					w += 3;
					zIdx++;
					rIdx++;
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

			// identify extra-data affected tiles for cutoff
			var extraBounds = Rectangle.FromLTRB(
				Math.Max(0, (int)Math.Floor(dx / (fileHeader.cx / 2.0))),
				Math.Max(0, (int)Math.Floor(dy / (fileHeader.cy / 2.0))),
				Math.Min(tile.Layer.Width - 1, (int)Math.Ceiling((dx + img.header.cx_extra) / (fileHeader.cx / 2.0))),
				Math.Min((tile.Layer.Height - 1) * 2, (int)Math.Ceiling((dy + img.header.cy_extra) / (fileHeader.cy / 2.0))));
			for (int by = extraBounds.Top; by < extraBounds.Bottom; by++) {
				for (int bx = extraBounds.Left; bx < extraBounds.Right; bx++) {
					logger.Trace("Tile at ({0},{1}) has extradata affecting ({2},{3})", tile.Dx, tile.Dy, bx, by);
					tile.Layer.GridTouched[bx, by / 2] |= TileLayer.TouchType.ByExtraData;
					tile.Layer.GridTouchedBy[bx, by / 2] = tile;
				}
			}

			// Extra graphics are just a square
			for (y = 0; y < img.header.cy_extra; y++) {
				for (x = 0; x < img.header.cx_extra; x++) {
					// Checking per line is required because v needs to be checked every time
					byte paletteValue = img.extraData[rIdx];
					short z = (img.extraZData != null) ? (short)(zBufVal - img.extraZData[rIdx]) : short.MaxValue;

					if (paletteValue != 0 && w_low <= w && w < w_high && z > zBuffer[zIdx]) {
						*w++ = p.colors[paletteValue].B;
						*w++ = p.colors[paletteValue].G;
						*w++ = p.colors[paletteValue].R;
						zBuffer[zIdx] = z;
					}
					else
						w += 3;
					zIdx++;
					rIdx++;
				}
				w += stride - img.header.cx_extra * 3;
				zIdx += ds.Width - img.header.cx_extra;
			}
		}
	}
}