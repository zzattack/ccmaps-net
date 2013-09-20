﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using CNCMaps.Game;
using CNCMaps.Map;
using CNCMaps.Rendering;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	class TmpFile : VirtualFile {
		bool _isInitialized;
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		// header stuff
		public int Width;                  // width of blocks
		public int Height;                 // height in blocks
		public int BlockWidth;             // width of each block
		public int BlockHeight;            // height of each block

		public List<TmpImage> Images;

		public class TmpImage {
			// header stuff
			public int X;
			public int Y;
			private int _extraDataOffset;
			private int _zDataOffset;
			private int _extraZDataOffset;
			public int ExtraX;
			public int ExtraY;
			public int ExtraWidth;
			public int ExtraHeight;
			private DataPrecencyFlags _dataPrecencyFlags;
			public byte Height;
			public byte TerrainType;
			public byte RampType;
			public sbyte RadarRedLeft;
			public sbyte RadarGreenLeft;
			public sbyte RadarBlueLeft;
			public sbyte RadarRedRight;
			public sbyte RadarGreenRight;
			public sbyte RadarBlueRight;

			public byte[] TileData; // always available
			public byte[] ExtraData; // available is presency flags says so
			public byte[] ZData; // available is presency flags says so
			public byte[] ExtraZData; // available is presency flags says so

			public void Read(TmpFile f) {
				X = f.ReadInt32();
				Y = f.ReadInt32();
				_extraDataOffset = f.ReadInt32();
				_zDataOffset = f.ReadInt32();
				_extraZDataOffset = f.ReadInt32();
				ExtraX = f.ReadInt32();
				ExtraY = f.ReadInt32();
				ExtraWidth = f.ReadInt32();
				ExtraHeight = f.ReadInt32();
				_dataPrecencyFlags = (DataPrecencyFlags)f.ReadUInt32();
				Height = f.ReadByte();
				TerrainType = f.ReadByte();
				RampType = f.ReadByte();
				RadarRedLeft = f.ReadSByte();
				RadarGreenLeft = f.ReadSByte(); ;
				RadarBlueLeft = f.ReadSByte(); ;
				RadarRedRight = f.ReadSByte(); ;
				RadarGreenRight = f.ReadSByte(); ;
				RadarBlueRight = f.ReadSByte(); ;
				f.Read(3); // discard padding

				TileData = f.Read(f.BlockWidth * f.BlockHeight / 2);
				if (HasZData)
					ZData = f.Read(f.BlockWidth * f.BlockHeight / 2);

				if (HasExtraData)
					ExtraData = f.Read(Math.Abs(ExtraWidth * ExtraHeight));

				if (HasZData && HasExtraData && 0 < _extraZDataOffset && _extraZDataOffset < f.Length)
					ExtraZData = f.Read(Math.Abs(ExtraWidth * ExtraHeight));
			}

			[Flags]
			private enum DataPrecencyFlags : uint {
				ExtraData = 0x01,
				ZData = 0x02,
				DamagedData = 0x04,
			}

			public bool HasExtraData {
				get { return (_dataPrecencyFlags & DataPrecencyFlags.ExtraData) == DataPrecencyFlags.ExtraData; }
			}
			public bool HasZData {
				get { return (_dataPrecencyFlags & DataPrecencyFlags.ZData) == DataPrecencyFlags.ZData; }
			}
			public bool HasDamagedData {
				get { return (_dataPrecencyFlags & DataPrecencyFlags.DamagedData) == DataPrecencyFlags.DamagedData; }
			}
		}

		public TmpFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
		}

		public void Initialize() {
			if (_isInitialized) return;

			logger.Debug("Initializing TMP data for file {0}", FileName);
			_isInitialized = true;
			Position = 0;

			Width = ReadInt32();
			Height = ReadInt32();
			BlockWidth = ReadInt32();
			BlockHeight = ReadInt32();

			byte[] index = Read(Width * Height * sizeof(int));
			Images = new List<TmpImage>(Width * Height);
			for (int x = 0; x < Width * Height; x++) {
				int imageData = BitConverter.ToInt32(index, x * 4);
				Seek(imageData, SeekOrigin.Begin);
				var img = new TmpImage();
				img.Read(this);
				Images.Add(img);
			}
		}

		public Rectangle GetBounds(MapTile tile) {
			Initialize();
			var img = Images[tile.SubTile];

			int left = tile.Dx * BlockWidth / 2;
			int top = (tile.Dy - tile.Z) * BlockHeight / 2;
			int width = BlockWidth;
			int height = BlockHeight;
			if (img.HasExtraData) {
				if (img.ExtraX < 0) { left += img.ExtraX; width -= img.ExtraX; }
				if (img.ExtraY < 0) { top += img.ExtraY; height -= img.ExtraY; }
				width = Math.Max(width, img.ExtraWidth);
				height = Math.Max(height, img.ExtraHeight);
			}

			return new Rectangle(left, top, width, height);
		}

		unsafe public void Draw(MapTile tile, DrawingSurface ds) {
			Initialize();

			if (tile.SubTile >= Images.Count) return;
			TmpImage img = Images[tile.SubTile];
			var heightBuffer = ds.GetHeightBuffer();
			Palette p = tile.Palette;

			// calculate tile index -> pixel index
			Point offset = new Point(tile.Dx * BlockWidth / 2, (tile.Dy - tile.Z) * BlockHeight / 2);

			// make touched tiles (used for determining image cutoff)
			int gx = tile.Dx, gy = (tile.Dy - tile.Z) / 2;
			if (gx >= 0 && gy >= 0 && gx < tile.Layer.GridTouched.GetLength(0) && gy < tile.Layer.GridTouched.GetLength(1)) {
				tile.Layer.GridTouched[gx, gy] |= TileLayer.TouchType.ByNormalData;
				tile.Layer.GridTouchedBy[gx, gy] = tile;
			}

			logger.Trace("Drawing TMP file {0} (subtile {1}) at ({2},{3})", FileName, tile.SubTile, offset.X, offset.Y);

			int stride = ds.bmd.Stride;

			int halfCx = BlockWidth / 2,
				halfCy = BlockHeight / 2;

			// writing bounds
			var w_low = (byte*)ds.bmd.Scan0;
			byte* w_high = (byte*)ds.bmd.Scan0 + stride * ds.bmd.Height;
			byte* w = (byte*)ds.bmd.Scan0 + stride * offset.Y + (offset.X + halfCx - 2) * 3;

			int rIdx = 0, x, y = 0;
			int zIdx = offset.Y * ds.Width + offset.X + halfCx - 2;
			int cx = 0; // Amount of pixel to copy

			for (; y < halfCy; y++) {
				cx += 4;
				for (ushort c = 0; c < cx; c++) {
					byte paletteValue = img.TileData[rIdx];
					
					if (paletteValue != 0 && w_low <= w && w < w_high) {
						*(w + 0) = p.Colors[paletteValue].B;
						*(w + 1) = p.Colors[paletteValue].G;
						*(w + 2) = p.Colors[paletteValue].R;
						heightBuffer[zIdx] = (short)(tile.Z * Drawable.TileHeight / 2);
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
			for (; y < BlockHeight; y++) {
				cx -= 4;
				for (ushort c = 0; c < cx; c++) {
					byte paletteValue = img.TileData[rIdx];
					
					if (paletteValue != 0 && w_low <= w && w < w_high) {
						*(w + 0) = p.Colors[paletteValue].B;
						*(w + 1) = p.Colors[paletteValue].G;
						*(w + 2) = p.Colors[paletteValue].R;
						heightBuffer[zIdx] = (short)(tile.Z * Drawable.TileHeight / 2);
					}
					w += 3;
					zIdx++;
					rIdx++;
				}
				w += stride - 3 * (cx - 2);
				zIdx += ds.Width - (cx - 2);
			}

			if (!img.HasExtraData) return; // we're done now

			offset.X += img.ExtraX - img.X;
			offset.Y += img.ExtraY - img.Y;
			w = w_low + stride * offset.Y + 3 * offset.X;
			zIdx = offset.X + offset.Y * ds.Width;
			rIdx = 0;

			// identify extra-data affected tiles for cutoff
			var extraBounds = Rectangle.FromLTRB(
				Math.Max(0, (int)Math.Floor(offset.X / (BlockWidth / 2.0))),
				Math.Max(0, (int)Math.Floor(offset.Y / (BlockHeight / 2.0))),
				Math.Min(tile.Layer.Width - 1, (int)Math.Ceiling((offset.X + img.ExtraWidth) / (BlockWidth / 2.0))),
				Math.Min((tile.Layer.Height - 1) * 2, (int)Math.Ceiling((offset.X + img.ExtraHeight) / (BlockHeight / 2.0))));
			for (int by = extraBounds.Top; by < extraBounds.Bottom; by++) {
				for (int bx = extraBounds.Left; bx < extraBounds.Right; bx++) {
					logger.Trace("Tile at ({0},{1}) has extradata affecting ({2},{3})", tile.Dx, tile.Dy, bx, by);
					tile.Layer.GridTouched[bx, by / 2] |= TileLayer.TouchType.ByExtraData;
					tile.Layer.GridTouchedBy[bx, by / 2] = tile;
				}
			}

			// Extra graphics are just a square
			for (y = 0; y < img.ExtraHeight; y++) {
				for (x = 0; x < img.ExtraWidth; x++) {
					// Checking per line is required because v needs to be checked every time
					byte paletteValue = img.ExtraData[rIdx];
					
					if (paletteValue != 0 && w_low <= w && w < w_high) {
						*w++ = p.Colors[paletteValue].B;
						*w++ = p.Colors[paletteValue].G;
						*w++ = p.Colors[paletteValue].R;
						heightBuffer[zIdx] = (short)(img.ExtraHeight + tile.Z * Drawable.TileHeight / 2);
					}
					else
						w += 3;
					zIdx++;
					rIdx++;
				}
				w += stride - img.ExtraWidth * 3;
				zIdx += ds.Width - img.ExtraWidth;
			}
		}
	}
}