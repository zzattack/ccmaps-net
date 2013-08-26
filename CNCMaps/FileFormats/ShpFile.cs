﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using CNCMaps.Encodings;
using CNCMaps.MapLogic;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	class ShpFile : VirtualFile {

		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct ShpFileHeader {
			private short zero;
			public short cx;
			public short cy;
			public short c_images;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct ShpImageHeader {
			public short x;
			public short y;
			public short cx;
			public short cy;
			public byte compression;
			public byte unknown1;
			public byte unknown2;
			public byte unknown3;
			private int unknown4;
			private int zero;
			public int offset;
		}

		struct ShpImage {
			public ShpImageHeader header;
			public byte[] imageData;
		}

		ShpFileHeader fileHeader;
		List<ShpImage> images;

		bool initialized;

		public ShpFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
		}

		public void Initialize() {
			initialized = true;
			logger.Debug("Initializing SHP data for file {0}", FileName);
			fileHeader = EzMarshal.ByteArrayToStructure<ShpFileHeader>(Read(Marshal.SizeOf(typeof(ShpFileHeader))));
			images = new List<ShpImage>(fileHeader.c_images);
			for (int i = 0; i < fileHeader.c_images; i++) {
				var img = new ShpImage();
				img.header = EzMarshal.ByteArrayToStructure<ShpImageHeader>(Read(Marshal.SizeOf(typeof(ShpImageHeader))));
				images.Add(img);
			}
		}

		private ShpImage GetImage(int imageIndex) {
			if (imageIndex >= images.Count) return new ShpImage();

			ShpImage img = images[imageIndex];

			// make sure imageData is present/decoded if needed
			if (img.imageData == null) {
				Position = img.header.offset;
				int c_px = img.header.cx * img.header.cy;

				//img.header.compression &= 0x03;
				if (img.header.compression <= 1) {
					// Raw 8 bits-per-pixel image data
					img.imageData = Read(c_px);
				}
				else if (img.header.compression == 2) {
					// Image data divided into scanlines {
					// -- Length of scanline (ImageHeader.width + 2) : uint16
					// -- Raw 8 bits-per-pixel image data : uint8[ImageHeader.width]
					img.imageData = new byte[c_px];
					int offset = 0;
					for (int y = 0; y < img.header.cy; y++) {
						ushort scanlineLength = (ushort)(ReadUInt16() - sizeof(ushort));
						Read(img.imageData, offset, scanlineLength);
						offset += scanlineLength;
					}
				}
				else if (img.header.compression == 3) {
					img.imageData = new byte[c_px];
					var compressedEnd = (int)Length;
					if (imageIndex < images.Count - 1)
						compressedEnd = images[imageIndex + 1].header.offset;
					if (compressedEnd < img.header.offset)
						compressedEnd = (int)Length;
					Format3.DecodeInto(Read(compressedEnd - img.header.offset), img.imageData, img.header.cx, img.header.cy);
				}
				else {
					logger.Warn("SHP image {0} frame {1} has unknown compression!", FileName, imageIndex);
				}
			}
			return img;
		}

		/// <summary>
		/// Draws a SHP image 
		/// </summary>
		/// <param name="frameIndex">Frame of SHP image</param>
		/// <param name="ds">Drawing surface buffer</param>
		/// <param name="offset">Offset from tile where object is stored</param>
		/// <param name="tile">Tile used to </param>
		/// <param name="p">Pallette used to draw this object</param>
		/// <param name="overrides">Whether z-buffer should be ignored</param>
		unsafe public void Draw(int frameIndex, DrawingSurface ds, Point offset, MapTile tile, Palette p, bool overrides = false) {
			if (!initialized) Initialize();

			logger.Trace("Drawing SHP file {0} (Frame {1}) at ({2},{3})", FileName, frameIndex, offset.X, offset.Y);

			var image = GetImage(frameIndex);
			if (image.imageData == null || image.header.cx * image.header.cy != image.imageData.Length)
				return;

			var h = image.header;
			var c_px = (uint)(h.cx * h.cy);
			int stride = ds.bmd.Stride;
			var zBuffer = ds.GetZBuffer();

			if (c_px <= 0 || h.cx < 0 || h.cy < 0 || frameIndex > fileHeader.c_images)
				return;

			short zBufVal = (short)((tile.Rx + tile.Ry + tile.Z) * Drawable.TileHeight / 2 - fileHeader.cy / 2 + h.y + offset.Y);

			var w_low = (byte*)ds.bmd.Scan0;
			byte* w_high = (byte*)ds.bmd.Scan0 + stride * ds.bmd.Height;

			int dx = offset.X + tile.Dx * Drawable.TileWidth / 2 + Drawable.TileWidth / 2 - fileHeader.cx / 2 + h.x,
				dy = offset.Y + (tile.Dy - tile.Z) * Drawable.TileHeight / 2 - fileHeader.cy / 2 + h.y;
			byte* w = (byte*)ds.bmd.Scan0 + dx * 3 + stride * dy;
			int zIdx = dx + dy * ds.Width;
			int rIdx = 0;

			for (int y = 0; y < h.cy; y++) {
				if (dy + y < 0) {
					w += stride;
					rIdx += h.cx;
					zIdx += ds.Width;
					continue; // out of bounds
				}
				short z = (short)(zBufVal + y + 2); // why the +2? oh well

				for (int x = 0; x < h.cx; x++) {
					byte paletteValue = image.imageData[rIdx];
					if (paletteValue != 0 && w_low <= w && w < w_high && (overrides || z >= zBuffer[zIdx])) {
						*(w + 0) = p.colors[paletteValue].B;
						*(w + 1) = p.colors[paletteValue].G;
						*(w + 2) = p.colors[paletteValue].R;
						zBuffer[zIdx] = zBufVal;
					}
					// Up to the next pixel
					rIdx++;
					zIdx++;
					w += 3;
				}
				w += stride - 3 * h.cx;
				zIdx += ds.Width - h.cx;
			}
		}

		unsafe public void DrawShadow(int frameIndex, DrawingSurface ds, Point offset, MapTile tile) {
			if (frameIndex >= images.Count / 2) return;

			logger.Trace("Drawing SHP shadow {0} (frame {1}) at ({2},{3})", FileName, frameIndex, offset.X, offset.Y);

			var image = GetImage(frameIndex + images.Count / 2);
			if (image.imageData == null || image.header.cx * image.header.cy != image.imageData.Length)
				return;

			var h = image.header;
			var c_px = (uint)(h.cx * h.cy);
			int stride = ds.bmd.Stride;
			var shadows = ds.GetShadows();
			var zBuffer = ds.GetZBuffer();

			if (c_px <= 0 || h.cx < 0 || h.cy < 0 || frameIndex > fileHeader.c_images)
				return;

			short zBufVal = (short)((tile.Rx + tile.Ry) * Drawable.TileHeight / 2 - fileHeader.cy / 2 + h.y + offset.Y);

			var w_low = (byte*)ds.bmd.Scan0;
			byte* w_high = (byte*)ds.bmd.Scan0 + stride * ds.bmd.Height;

			int dx = offset.X + tile.Dx * Drawable.TileWidth / 2 + Drawable.TileWidth / 2 - fileHeader.cx / 2 + h.x,
				dy = offset.Y + (tile.Dy - tile.Z) * Drawable.TileHeight / 2 - fileHeader.cy / 2 + h.y;
			byte* w = (byte*)ds.bmd.Scan0 + dx * 3 + stride * dy;
			int zIdx = dx + dy * ds.Width;
			int rIdx = 0;

			for (int y = 0; y < h.cy; y++) {
				short z = (short)(zBufVal + y + 2); // why the +2? oh well
				for (int x = 0; x < h.cx; x++) {
					if (w_low <= w && w < w_high && image.imageData[rIdx] != 0 && !shadows[zIdx] && z >= zBuffer[zIdx]) {
						*(w + 0) /= 2;
						*(w + 1) /= 2;
						*(w + 2) /= 2;
						shadows[zIdx] = true;
					}
					// Up to the next pixel
					rIdx++;
					zIdx++;
					w += 3;
				}
				w += stride - 3 * h.cx;	// ... and if we're no more on the same row,
				zIdx += ds.Width - h.cx;
				// adjust the writing pointer accordingy
			}
		}

		public unsafe void DrawAlpha(int frameIndex, DrawingSurface ds, int xOffset, int yOffset) {
			logger.Trace("Drawing AlphaImage SHP file {0} (frame {1}) at ({2},{3})", FileName, frameIndex, xOffset, yOffset);

			var image = GetImage(frameIndex + images.Count / 2);
			var h = image.header;
			var c_px = (uint)(h.cx * h.cy);
			int stride = ds.bmd.Stride;

			if (c_px <= 0 || h.cx < 0 || h.cy < 0 || frameIndex > fileHeader.c_images)
				return;

			var w_low = (byte*)ds.bmd.Scan0;
			byte* w_high = (byte*)ds.bmd.Scan0 + stride * ds.bmd.Height;

			int dx = xOffset + 30 - fileHeader.cx / 2 + h.x,
				dy = yOffset - fileHeader.cy / 2 + h.y;
			byte* w = (byte*)ds.bmd.Scan0 + dx * 3 + stride * dy;

			int rIdx = 0;

			for (int y = 0; y < h.cy; y++) {
				for (int x = 0; x < h.cx; x++) {
					if (image.imageData[rIdx] != 0 && w_low <= w && w < w_high) {
						float mult = image.imageData[rIdx] / 128.0f;
						*(w + 0) = limit(mult, *(w + 0));
						*(w + 1) = limit(mult, *(w + 1));
						*(w + 2) = limit(mult, *(w + 2));
					}
					// Up to the next pixel
					rIdx++;
					w += 3;
				}
				w += stride - 3 * h.cx;	// ... and if we're no more on the same row,
				// adjust the writing pointer accordingy
			}
		}

		private byte limit(float mult, byte p) {
			return (byte)Math.Max(0f, Math.Min(255f, mult * p));
		}
	}
}