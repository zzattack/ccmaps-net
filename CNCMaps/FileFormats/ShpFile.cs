using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using CNCMaps.FileFormats.Encodings;
using CNCMaps.Game;
using CNCMaps.Map;
using CNCMaps.Rendering;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	class ShpFile : VirtualFile {
		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
		bool _isInitialized;

		public ShpFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
		}

		private short Zero;
		public short Width;
		public short Height;
		public short NumImages;
		public List<ShpImage> Images = new List<ShpImage>();

		public class ShpImage {
			ShpFile _f;
			int _frameIndex;

			public short X;
			public short Y;
			public short Width;
			public short Height;
			public byte CompressionType;
			public byte Unknown1;
			public byte Unknown2;
			public byte Unknown3;
			private int Unknown4;
			private int Zero;
			public int ImgDataOffset;

			private byte[] _decompressedImage;

			public void Read(ShpFile f, int frameIndex) {
				_f = f;
				_frameIndex = frameIndex;

				X = f.ReadInt16();
				Y = f.ReadInt16();
				Width = f.ReadInt16();
				Height = f.ReadInt16();
				CompressionType = f.ReadByte();
				Unknown1 = f.ReadByte();
				Unknown2 = f.ReadByte();
				Unknown3 = f.ReadByte();
				Unknown4 = f.ReadInt32();
				Zero = f.ReadInt32();
				ImgDataOffset = f.ReadInt32();
			}

			public byte[] GetImageData() {
				// make sure RawImageData is present/decoded if needed
				if (_decompressedImage == null) {
					_f.Seek(ImgDataOffset, SeekOrigin.Begin);
					int c_px = Width * Height;

					// img.Header.compression &= 0x03;
					if (CompressionType <= 1) {
						// Raw 8 bits-per-pixel image data
						_decompressedImage = _f.Read(c_px);
					}
					else if (CompressionType == 2) {
						// Image data divided into scanlines {
						// -- Length of scanline (ImageHeader.width + 2) : uint16
						// -- Raw 8 bits-per-pixel image data : uint8[ImageHeader.width]
						_decompressedImage = new byte[c_px];
						int lineOffset = 0;
						for (int y = 0; y < Height; y++) {
							ushort scanlineLength = (ushort)(_f.ReadUInt16() - sizeof(ushort));
							_f.Read(_decompressedImage, lineOffset, scanlineLength);
							lineOffset += scanlineLength;
						}
					}
					else if (CompressionType == 3) {
						_decompressedImage = new byte[c_px];
						var compressedEnd = (int)_f.Length;
						if (_frameIndex < _f.Images.Count - 1)
							compressedEnd = _f.Images[_frameIndex + 1].ImgDataOffset;
						if (compressedEnd < ImgDataOffset)
							compressedEnd = (int)_f.Length;
						Format3.DecodeInto(_f.Read(compressedEnd - ImgDataOffset), _decompressedImage, Width, Height);
					}
					else {
						Logger.Warn("SHP image {0} frame {1} has unknown compression!", _f.FileName, _frameIndex);
					}
				}

				return _decompressedImage;
			}
		}


		public void Initialize() {
			if (_isInitialized) return;
			_isInitialized = true;

			Logger.Debug("Initializing SHP data for file {0}", FileName);
			Zero = ReadInt16();
			Width = ReadInt16();
			Height = ReadInt16();
			NumImages = ReadInt16();

			Images = new List<ShpImage>(NumImages);
			for (int i = 0; i < NumImages; i++) {
				var img = new ShpImage();
				img.Read(this, i);
				Images.Add(img);
			}
		}
		
		public Rectangle GetBounds(GameObject obj, DrawProperties props) {
			Initialize();
			int frameIndex = 0;//DecideFrameIndex(props.FrameDecider(obj));
			Point offset = Point.Empty;
			Size size = new Size(0, 0);
			offset.Offset(props.GetOffset(obj));
			offset.Offset(-Width / 2, -Height / 2);
			var img = GetImage(frameIndex);
			if (img != null) {
				offset.Offset(img.X, img.Y);
				size = new Size(img.Width, img.Height);
			}
			return new Rectangle(offset, size);
		}

		private static Random R = new Random();
		private ShpImage GetImage(int imageIndex) {
			if (imageIndex >= Images.Count) return new ShpImage();
			return Images[imageIndex];
		}

		unsafe public void Draw(GameObject obj, DrawProperties props, DrawingSurface ds, Point globalOffset) {
			Initialize();

			int frameIndex = props.FrameDecider(obj);
			Palette p = props.PaletteOverride ?? obj.Palette;

			frameIndex = DecideFrameIndex(frameIndex);
			if (frameIndex >= Images.Count)
				return;

			var img = GetImage(frameIndex);
			var imgData = img.GetImageData();
			if (imgData == null || img.Width * img.Height != imgData.Length)
				return;

			Point offset = globalOffset;
			offset.Offset(props.GetOffset(obj));
			offset.X += obj.Tile.Dx * Drawable.TileWidth / 2 + Drawable.TileWidth / 2 - Width / 2 + img.X;
			offset.Y += (obj.Tile.Dy - obj.Tile.Z) * Drawable.TileHeight / 2 - Height / 2 + img.Y;
			Logger.Trace("Drawing SHP file {0} (Frame {1}) at ({2},{3})", FileName, frameIndex, offset.X, offset.Y);

			int stride = ds.BitmapData.Stride;
			var heightBuffer = ds.GetHeightBuffer();

			var w_low = (byte*)ds.BitmapData.Scan0;
			byte* w_high = (byte*)ds.BitmapData.Scan0 + stride * ds.BitmapData.Height;


			byte* w = (byte*)ds.BitmapData.Scan0 + offset.X * 3 + stride * offset.Y;
			int zIdx = offset.X + offset.Y * ds.Width;
			int rIdx = 0;

			for (int y = 0; y < img.Height; y++) {
				if (offset.Y + y < 0) {
					w += stride;
					rIdx += img.Width;
					zIdx += ds.Width;
					continue; // out of bounds
				}

				for (int x = 0; x < img.Width; x++) {
					byte paletteValue = imgData[rIdx];
					if (paletteValue != 0 && w_low <= w && w < w_high) {
						*(w + 0) = p.Colors[paletteValue].B;
						*(w + 1) = p.Colors[paletteValue].G;
						*(w + 2) = p.Colors[paletteValue].R;
						heightBuffer[zIdx] = (short)(Height + obj.Tile.Z * Drawable.TileHeight / 2);
					}
					// Up to the next pixel
					rIdx++;
					zIdx++;
					w += 3;
				}
				w += stride - 3 * img.Width;
				zIdx += ds.Width - img.Width;
			}
		}

		unsafe public void DrawShadow(GameObject obj, DrawProperties props, DrawingSurface ds, Point globalOffset) {
			int frameIndex = props.FrameDecider(obj);

			frameIndex = DecideFrameIndex(frameIndex);
			frameIndex += Images.Count / 2; // latter half are shadow Images
			if (frameIndex >= Images.Count)
				return;

			var img = GetImage(frameIndex);
			var imgData = img.GetImageData();
			if (imgData == null || img.Width * img.Height != imgData.Length)
				return;

			Point offset = globalOffset;
			offset.Offset(props.GetShadowOffset(obj));
			offset.X += obj.Tile.Dx * Drawable.TileWidth / 2 + Drawable.TileWidth / 2 - Width / 2 + img.X;
			offset.Y += (obj.Tile.Dy - obj.Tile.Z) * Drawable.TileHeight / 2 - Height / 2 + img.Y;
			Logger.Trace("Drawing SHP shadow {0} (frame {1}) at ({2},{3})", FileName, frameIndex, offset.X, offset.Y);

			int stride = ds.BitmapData.Stride;
			var shadows = ds.GetShadows();
			var heightBuffer = ds.GetHeightBuffer();

			var w_low = (byte*)ds.BitmapData.Scan0;
			byte* w_high = (byte*)ds.BitmapData.Scan0 + stride * ds.BitmapData.Height;

			byte* w = (byte*)ds.BitmapData.Scan0 + offset.X * 3 + stride * offset.Y;
			int zIdx = offset.X + offset.Y * ds.Width;
			int rIdx = 0;
			int castHeight = obj.Tile.Z * Drawable.TileHeight / 2;

			for (int y = 0; y < img.Height; y++) {
				if (offset.Y + y < 0) {
					w += stride;
					rIdx += img.Width;
					zIdx += ds.Width;
					continue; // out of bounds
				}

				for (int x = 0; x < img.Width; x++) {
					if (w_low <= w && w < w_high && imgData[rIdx] != 0 && castHeight >= heightBuffer[zIdx]) {
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
				w += stride - 3 * img.Width;	// ... and if we're no more on the same row,
				zIdx += ds.Width - img.Width;
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

			var img = GetImage(frameIndex);
			var imgData = img.GetImageData();
			var c_px = (uint)(img.Width * img.Height);
			if (c_px <= 0 || img.Width < 0 || img.Height < 0 || frameIndex > NumImages)
				return;

			Point offset = globalOffset;
			offset.Offset(props.GetOffset(obj));
			offset.X += obj.Tile.Dx * Drawable.TileWidth / 2;
			offset.Y += (obj.Tile.Dy - obj.Tile.Z) * Drawable.TileHeight / 2;
			Logger.Trace("Drawing AlphaImage SHP file {0} (frame {1}) at ({2},{3})", FileName, frameIndex, offset.X, offset.Y);

			int stride = ds.BitmapData.Stride;
			var w_low = (byte*)ds.BitmapData.Scan0;
			byte* w_high = (byte*)ds.BitmapData.Scan0 + stride * ds.BitmapData.Height;

			int dx = offset.X + Drawable.TileWidth / 2 - Width / 2 + img.X,
				dy = offset.Y - Height / 2 + img.Y;
			byte* w = (byte*)ds.BitmapData.Scan0 + dx * 3 + stride * dy;

			int rIdx = 0;

			for (int y = 0; y < img.Height; y++) {
				for (int x = 0; x < img.Width; x++) {
					if (imgData[rIdx] != 0 && w_low <= w && w < w_high) {
						float mult = imgData[rIdx] / 127.0f;
						*(w + 0) = limit(mult, *(w + 0));
						*(w + 1) = limit(mult, *(w + 1));
						*(w + 2) = limit(mult, *(w + 2));
					}
					// Up to the next pixel
					rIdx++;
					w += 3;
				}
				w += stride - 3 * img.Width;	// ... and if we're no more on the same row,
				// adjust the writing pointer accordingy
			}
		}

		private static byte limit(float mult, byte p) {
			return (byte)Math.Max(0f, Math.Min(255f, mult * p));
		}

	}
}