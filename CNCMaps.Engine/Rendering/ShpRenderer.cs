using System;
using System.Drawing;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Map;
using CNCMaps.FileFormats;
using NLog;

namespace CNCMaps.Engine.Rendering {
	class ShpRenderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static Rectangle GetBounds(GameObject obj, ShpFile shp, DrawProperties props) {
			shp.Initialize();
			int frameIndex = DecideFrameIndex(props.FrameDecider(obj), shp.NumImages);
			var offset = new Point(-shp.Width / 2, -shp.Height / 2);
			Size size = new Size(0, 0);
			var img = shp.GetImage(frameIndex);
			if (img != null) {
				offset.Offset(img.X, img.Y);
				size = new Size(img.Width, img.Height);
			}
			return new Rectangle(offset, size);
		}

		unsafe public static void Draw(GameObject obj, ShpFile shp, DrawProperties props, DrawingSurface ds) {
			shp.Initialize();

			int frameIndex = props.FrameDecider(obj);
			Palette p = props.PaletteOverride ?? obj.Palette;

			frameIndex = DecideFrameIndex(frameIndex, shp.NumImages);
			if (frameIndex >= shp.Images.Count)
				return;

			var img = shp.GetImage(frameIndex);
			var imgData = img.GetImageData();
			if (imgData == null || img.Width * img.Height != imgData.Length)
				return;

			Point offset = props.GetOffset(obj);
			offset.X += obj.Tile.Dx * Drawable.TileWidth / 2 - shp.Width / 2 + img.X;
			offset.Y += (obj.Tile.Dy - obj.Tile.Z) * Drawable.TileHeight / 2 - shp.Height / 2 + img.Y;
			Logger.Trace("Drawing SHP file {0} (Frame {1}) at ({2},{3})", shp.FileName, frameIndex, offset.X, offset.Y);

			int stride = ds.BitmapData.Stride;
			var zBuffer = ds.GetZBuffer();

			var w_low = (byte*)ds.BitmapData.Scan0;
			byte* w_high = (byte*)ds.BitmapData.Scan0 + stride * ds.BitmapData.Height;


			byte* w = (byte*)ds.BitmapData.Scan0 + offset.X * 3 + stride * offset.Y;
			int zIdx = offset.X + offset.Y * ds.Width;
			int rIdx = 0;
			short zOffset = (short)((obj.Tile.Rx + obj.Tile.Ry) * Drawable.TileHeight / 2 - shp.Height / 2 + img.Y);

			for (int y = 0; y < img.Height; y++) {
				if (offset.Y + y < 0) {
					w += stride;
					rIdx += img.Width;
					zIdx += ds.Width;
					continue; // out of bounds
				}
				
				short zBufVal = zOffset;
				if (obj.Drawable.Flat)
					zBufVal += (short)y;
				else
					zBufVal += img.Height;

				for (int x = 0; x < img.Width; x++) {
					byte paletteValue = imgData[rIdx];
					if (paletteValue != 0 && w_low <= w && w < w_high) {
						*(w + 0) =  p.Colors[paletteValue].B;
						*(w + 1) =  p.Colors[paletteValue].G;
						*(w + 2) =  p.Colors[paletteValue].R;
						zBuffer[zIdx] = zBufVal;
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

		unsafe public static void DrawTranslucent(GameObject obj, ShpFile shp, DrawProperties props, DrawingSurface ds, int transLucency) {
			shp.Initialize();

			int frameIndex = props.FrameDecider(obj);
			Palette p = props.PaletteOverride ?? obj.Palette;

			frameIndex = DecideFrameIndex(frameIndex, shp.NumImages);
			if (frameIndex >= shp.Images.Count)
				return;

			var img = shp.GetImage(frameIndex);
			var imgData = img.GetImageData();
			if (imgData == null || img.Width * img.Height != imgData.Length)
				return;

			Point offset = props.GetOffset(obj);
			offset.X += obj.Tile.Dx * Drawable.TileWidth / 2 - shp.Width / 2 + img.X;
			offset.Y += (obj.Tile.Dy - obj.Tile.Z) * Drawable.TileHeight / 2 - shp.Height / 2 + img.Y;
			Logger.Trace("Drawing SHP file {0} (Frame {1}) at ({2},{3})", shp.FileName, frameIndex, offset.X, offset.Y);

			int stride = ds.BitmapData.Stride;
			var zBuffer = ds.GetZBuffer();

			var w_low = (byte*)ds.BitmapData.Scan0;
			byte* w_high = (byte*)ds.BitmapData.Scan0 + stride * ds.BitmapData.Height;

			byte* w = (byte*)ds.BitmapData.Scan0 + offset.X * 3 + stride * offset.Y;
			int zIdx = offset.X + offset.Y * ds.Width;
			int rIdx = 0;

			// clip to 25-50-75-100
			transLucency = (transLucency / 25) * 25;
			float a = transLucency / 100f;
			float b = 1 - a;

			for (int y = 0; y < img.Height; y++) {
				if (offset.Y + y < 0) {
					w += stride;
					rIdx += img.Width;
					zIdx += ds.Width;
					continue; // out of bounds
				}

				short zBufVal = (short)((obj.Tile.Rx + obj.Tile.Ry) * Drawable.TileHeight / 2);
				if (obj.Drawable != null && !obj.Drawable.Flat)
					zBufVal += (short)(shp.Height - img.Y - y);

				for (int x = 0; x < img.Width; x++) {
					byte paletteValue = imgData[rIdx];
					if (paletteValue != 0 && w_low <= w && w < w_high) {
						*(w + 0) = (byte)(a * *(w + 0) + b * p.Colors[paletteValue].B);
						*(w + 1) = (byte)(a * *(w + 1) + b * p.Colors[paletteValue].G);
						*(w + 2) = (byte)(a * *(w + 2) + b * p.Colors[paletteValue].R);
						zBuffer[zIdx] = zBufVal;
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

		unsafe public static void DrawShadow(GameObject obj, ShpFile shp, DrawProperties props, DrawingSurface ds) {
			int frameIndex = props.FrameDecider(obj);
			frameIndex = DecideFrameIndex(frameIndex, shp.NumImages);
			frameIndex += shp.Images.Count / 2; // latter half are shadow Images
			if (frameIndex >= shp.Images.Count)
				return;

			var img = shp.GetImage(frameIndex);
			var imgData = img.GetImageData();
			if (imgData == null || img.Width * img.Height != imgData.Length)
				return;

			Point offset = props.GetShadowOffset(obj);
			offset.X += obj.Tile.Dx * Drawable.TileWidth / 2 - shp.Width / 2 + img.X;
			offset.Y += (obj.Tile.Dy - obj.Tile.Z) * Drawable.TileHeight / 2 - shp.Height / 2 + img.Y;
			Logger.Trace("Drawing SHP shadow {0} (frame {1}) at ({2},{3})", shp.FileName, frameIndex, offset.X, offset.Y);

			int stride = ds.BitmapData.Stride;
			var shadows = ds.GetShadows();
			var zBuffer = ds.GetZBuffer();

			var w_low = (byte*)ds.BitmapData.Scan0;
			byte* w_high = (byte*)ds.BitmapData.Scan0 + stride * ds.BitmapData.Height;

			byte* w = (byte*)ds.BitmapData.Scan0 + offset.X * 3 + stride * offset.Y;
			int zIdx = offset.X + offset.Y * ds.Width;
			int rIdx = 0;
			short zOffset = (short)((obj.Tile.Rx + obj.Tile.Ry) * Drawable.TileHeight / 2 - shp.Height / 2 + img.Y);

			for (int y = 0; y < img.Height; y++) {
				if (offset.Y + y < 0) {
					w += stride;
					rIdx += img.Width;
					zIdx += ds.Width;
					continue; // out of bounds
				}

				short zBufVal = zOffset;
				if (obj.Drawable.Flat)
					zBufVal += (short)y;
				else
					zBufVal += img.Height;

				for (int x = 0; x < img.Width; x++) {
					if (w_low <= w && w < w_high && imgData[rIdx] != 0 && !shadows[zIdx] && zBufVal > zBuffer[zIdx]) {
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

		private static Random R = new Random();
		private static int DecideFrameIndex(int frameIndex, int numImages) {
			DrawFrame f = (DrawFrame)frameIndex;
			if (f == DrawFrame.Random)
				frameIndex = R.Next(numImages);
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

		unsafe public static void DrawAlpha(GameObject obj, ShpFile shp, DrawProperties props, DrawingSurface ds) {
			shp.Initialize();

			// Change originally implemented by Starkku: Ares supports multiframe AlphaImages, based on frame count 
			// the direction the unit it facing.
			int frameIndex = props.FrameDecider(obj);

			var img = shp.GetImage(frameIndex);
			var imgData = img.GetImageData();
			var c_px = (uint)(img.Width * img.Height);
			if (c_px <= 0 || img.Width < 0 || img.Height < 0 || frameIndex > shp.NumImages)
				return;

			Point offset = props.GetOffset(obj);
			offset.X += obj.Tile.Dx * Drawable.TileWidth / 2;
			offset.Y += (obj.Tile.Dy - obj.Tile.Z) * Drawable.TileHeight / 2;
			Logger.Trace("Drawing AlphaImage SHP file {0} (frame {1}) at ({2},{3})", shp.FileName, frameIndex, offset.X, offset.Y);

			int stride = ds.BitmapData.Stride;
			var w_low = (byte*)ds.BitmapData.Scan0;
			byte* w_high = (byte*)ds.BitmapData.Scan0 + stride * ds.BitmapData.Height;

			int dx = offset.X + Drawable.TileWidth / 2 - shp.Width / 2 + img.X,
				dy = offset.Y - shp.Height / 2 + img.Y;
			byte* w = (byte*)ds.BitmapData.Scan0 + dx * 3 + stride * dy;
			short zOffset = (short)((obj.Tile.Rx + obj.Tile.Ry) * Drawable.TileHeight / 2 - shp.Height / 2 + img.Y);
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
