using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using CNCMaps.Encodings;
using CNCMaps.MapLogic;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	class ShpFile : VirtualFile {

		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct ShpFileHeader {
			private readonly short Zero;
			public readonly short Width;
			public readonly short Height;
			public readonly short NumImages;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct ShpImageHeader {
			public readonly short x;
			public readonly short y;
			public readonly short cx;
			public readonly short cy;
			public readonly byte compression;
			public readonly byte unknown1;
			public readonly byte unknown2;
			public readonly byte unknown3;
			private readonly int unknown4;
			private readonly int zero;
			public readonly int offset;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct ShpImage {
			public ShpImageHeader Header;
			public byte[] ImageData;
		}

		public ShpFileHeader Header { get; private set; }
		public List<ShpImage> Images { get; private set; }
		bool _isInitialized;

		public ShpFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
		}

		public void Initialize() {
			if (!_isInitialized) {
				_isInitialized = true;
				Logger.Debug("Initializing SHP data for file {0}", FileName);
				Header = EzMarshal.ByteArrayToStructure<ShpFileHeader>(Read(Marshal.SizeOf(typeof (ShpFileHeader))));
				Images = new List<ShpImage>(Header.NumImages);
				for (int i = 0; i < Header.NumImages; i++) {
					var img = new ShpImage();
					img.Header = EzMarshal.ByteArrayToStructure<ShpImageHeader>(Read(Marshal.SizeOf(typeof (ShpImageHeader))));
					Images.Add(img);
				}
			}
		}

		private static Random R = new Random();
		private ShpImage GetImage(int imageIndex) {
			if (imageIndex >= Images.Count) return new ShpImage();

			ShpImage img = Images[imageIndex];
			// make sure ImageData is present/decoded if needed
			if (img.ImageData == null) {
				Position = img.Header.offset;
				int c_px = img.Header.cx * img.Header.cy;

				// img.Header.compression &= 0x03;
				if (img.Header.compression <= 1) {
					// Raw 8 bits-per-pixel image data
					img.ImageData = Read(c_px);
				}
				else if (img.Header.compression == 2) {
					// Image data divided into scanlines {
					// -- Length of scanline (ImageHeader.width + 2) : uint16
					// -- Raw 8 bits-per-pixel image data : uint8[ImageHeader.width]
					img.ImageData = new byte[c_px];
					int offset = 0;
					for (int y = 0; y < img.Header.cy; y++) {
						ushort scanlineLength = (ushort)(ReadUInt16() - sizeof(ushort));
						Read(img.ImageData, offset, scanlineLength);
						offset += scanlineLength;
					}
				}
				else if (img.Header.compression == 3) {
					img.ImageData = new byte[c_px];
					var compressedEnd = (int)Length;
					if (imageIndex < Images.Count - 1)
						compressedEnd = Images[imageIndex + 1].Header.offset;
					if (compressedEnd < img.Header.offset)
						compressedEnd = (int)Length;
					Format3.DecodeInto(Read(compressedEnd - img.Header.offset), img.ImageData, img.Header.cx, img.Header.cy);
				}
				else {
					Logger.Warn("SHP image {0} frame {1} has unknown compression!", FileName, imageIndex);
				}
			}
			return img;
		}

		unsafe public void Draw(GameObject obj, DrawProperties props, DrawingSurface ds, Point globalOffset) {
			Initialize();

			int frameIndex = props.FrameDecider(obj);
			Palette p = props.PaletteOverride ?? obj.Palette;

			frameIndex = DecideFrameIndex(frameIndex);
			if (frameIndex >= Images.Count)
				return;
			
			var image = GetImage(frameIndex);
			if (image.ImageData == null || image.Header.cx * image.Header.cy != image.ImageData.Length)
				return;
			
			Point offset = globalOffset;
			offset.Offset(props.GetOffset(obj));
			offset.X += obj.Tile.Dx * Drawable.TileWidth / 2 + Drawable.TileWidth / 2 - Header.Width / 2 + image.Header.x;
			offset.Y += (obj.Tile.Dy - obj.Tile.Z) * Drawable.TileHeight / 2 - Header.Height / 2 + image.Header.y;
			Logger.Trace("Drawing SHP file {0} (Frame {1}) at ({2},{3})", FileName, frameIndex, offset.X, offset.Y);

			int stride = ds.bmd.Stride;
			var zBuffer = ds.GetZBuffer();
			
			var w_low = (byte*)ds.bmd.Scan0;
			byte* w_high = (byte*)ds.bmd.Scan0 + stride * ds.bmd.Height;

			var tile = obj.Tile;
			byte* w = (byte*)ds.bmd.Scan0 + offset.X * 3 + stride * offset.Y;
			int zIdx = offset.X + offset.Y * ds.Width;
			int rIdx = 0;

			// short zBufVal = (short)(offset.Y + tile.Dy * Drawable.TileHeight / 2 + Header.Height);
			short zBufVal = (short)(obj.BaseTile.Rx + obj.BaseTile.Ry + obj.BaseTile.Z + obj.Drawable.HeightOffset);

			for (int y = 0; y < image.Header.cy; y++) {
				if (offset.Y + y < 0) {
					w += stride;
					rIdx += image.Header.cx;
					zIdx += ds.Width;
					continue; // out of bounds
				}

				for (int x = 0; x < image.Header.cx; x++) {
					byte paletteValue = image.ImageData[rIdx];
					if (paletteValue != 0 && w_low <= w && w < w_high && (props.OverridesZbuffer || zBufVal >= zBuffer[zIdx])) {
						*(w + 0) = p.colors[paletteValue].B;
						*(w + 1) = p.colors[paletteValue].G;
						*(w + 2) = p.colors[paletteValue].R;
						zBuffer[zIdx] = Math.Max(zBufVal, zBuffer[zIdx]);
					}
					// Up to the next pixel
					rIdx++;
					zIdx++;
					w += 3;
				}
				w += stride - 3 * image.Header.cx;
				zIdx += ds.Width - image.Header.cx;
			}
		}

		unsafe public void DrawShadow(GameObject obj, DrawProperties props, DrawingSurface ds, Point globalOffset) {
			int frameIndex = props.FrameDecider(obj);
			
			frameIndex = DecideFrameIndex(frameIndex);
			frameIndex += Images.Count / 2; // latter half are shadow Images
			if (frameIndex >= Images.Count)
				return;
			
			var image = GetImage(frameIndex);
			if (image.ImageData == null || image.Header.cx * image.Header.cy != image.ImageData.Length)
				return;
			
			Point offset = globalOffset;
			offset.Offset(props.GetShadowOffset(obj));
			offset.X += obj.Tile.Dx * Drawable.TileWidth / 2 + Drawable.TileWidth / 2 - Header.Width / 2 + image.Header.x;
			offset.Y += (obj.Tile.Dy - obj.Tile.Z) * Drawable.TileHeight / 2 - Header.Height / 2 + image.Header.y;
			Logger.Trace("Drawing SHP shadow {0} (frame {1}) at ({2},{3})", FileName, frameIndex, offset.X, offset.Y);
			
			int stride = ds.bmd.Stride;
			var shadows = ds.GetShadows();
			var zBuffer = ds.GetZBuffer();	
		
			var w_low = (byte*)ds.bmd.Scan0;
			byte* w_high = (byte*)ds.bmd.Scan0 + stride * ds.bmd.Height;

			byte* w = (byte*)ds.bmd.Scan0 + offset.X * 3 + stride * offset.Y;
			int zIdx = offset.X + offset.Y * ds.Width;
			int rIdx = 0;
			short zBufVal = (short)(obj.BaseTile.Rx + obj.BaseTile.Ry + obj.BaseTile.Z + obj.Drawable.HeightOffset);

			for (int y = 0; y < image.Header.cy; y++) {
				if (offset.Y + y < 0) {
					w += stride;
					rIdx += image.Header.cx;
					zIdx += ds.Width;
					continue; // out of bounds
				}

				for (int x = 0; x < image.Header.cx; x++) {
					if (w_low <= w && w < w_high && image.ImageData[rIdx] != 0 && !shadows[zIdx] && zBufVal >= zBuffer[zIdx]) {
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
				w += stride - 3 * image.Header.cx;	// ... and if we're no more on the same row,
				zIdx += ds.Width - image.Header.cx;
				// adjust the writing pointer accordingy
			}
		}

		private int DecideFrameIndex(int frameIndex) {
			DrawFrame f = (DrawFrame)frameIndex;
			if (f == DrawFrame.Random)
				frameIndex = R.Next(Images.Count);
			//else if (f == DrawFrame.RandomHealthy) {
			//	// pick from the 1st 25% of the the Images
			//	frameIndex = R.Next(Images.Count / 4);
			//}
			//else if (f == DrawFrame.Damaged) {
			//	// first image of the 2nd half
			//	frameIndex = Images.Count / 4;
			//}
			return frameIndex;
		}

		unsafe public void DrawAlpha(GameObject obj, DrawProperties props, DrawingSurface ds, Point globalOffset) {
			Initialize();

			// Change originally implemented by Starkku: Ares supports multiframe AlphaImages, based on frame count 
			// the direction the unit it facing.
			int frameIndex = props.FrameDecider(obj);

			var image = GetImage(frameIndex);
			var h = image.Header;
			var c_px = (uint)(h.cx * h.cy);
			if (c_px <= 0 || h.cx < 0 || h.cy < 0 || frameIndex > Header.NumImages)
				return;

			Point offset = globalOffset;
			offset.Offset(props.GetOffset(obj));
			offset.X += obj.Tile.Dx * Drawable.TileWidth / 2;
			offset.Y += (obj.Tile.Dy - obj.Tile.Z) * Drawable.TileHeight / 2;
			Logger.Trace("Drawing AlphaImage SHP file {0} (frame {1}) at ({2},{3})", FileName, frameIndex, offset.X, offset.Y);

			int stride = ds.bmd.Stride;
			var w_low = (byte*)ds.bmd.Scan0;
			byte* w_high = (byte*)ds.bmd.Scan0 + stride * ds.bmd.Height;
			
			int dx = offset.X + Drawable.TileWidth / 2 - Header.Width / 2 + h.x,
				dy = offset.Y - Header.Height / 2 + h.y;
			byte* w = (byte*)ds.bmd.Scan0 + dx * 3 + stride * dy;

			int rIdx = 0;

			for (int y = 0; y < h.cy; y++) {
				for (int x = 0; x < h.cx; x++) {
					if (image.ImageData[rIdx] != 0 && w_low <= w && w < w_high) {
						float mult = image.ImageData[rIdx] / 127.0f;
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

		private static byte limit(float mult, byte p) {
			return (byte)Math.Max(0f, Math.Min(255f, mult * p));
		}


	}
}