using System;
using System.Drawing;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Map;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;
using CNCMaps.Shared.Utility;
using NLog;

namespace CNCMaps.Engine.Rendering {
	class ShpRenderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		public ShpRenderer(VirtualFileSystem vfs) {
			_vfs = vfs;
		}

		public Rectangle GetBounds(GameObject obj, ShpFile shp, DrawProperties props) {
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

		public unsafe void Draw(ShpFile shp, GameObject obj, Drawable dr, DrawProperties props, DrawingSurface ds, int transLucency = 0) {
			shp.Initialize();
			Palette p = props.PaletteOverride ?? obj.Palette;
			int frameIndex = props.FrameDecider(obj);
			if (obj.Drawable.IsActualWall)
				frameIndex = ((StructureObject)obj).WallBuildingFrame;
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
			var heightBuffer = ds.GetHeightBuffer();
			var zBuffer = ds.GetZBuffer();

			var w_low = (byte*)ds.BitmapData.Scan0;
			byte* w_high = (byte*)ds.BitmapData.Scan0 + stride * ds.BitmapData.Height;
			byte* w = (byte*)ds.BitmapData.Scan0 + offset.X * 3 + stride * offset.Y;

			// clip to 25-50-75-100
			transLucency = (transLucency / 25) * 25;
			float a = transLucency / 100f;
			float b = 1 - a;

			int rIdx = 0; // image pixel index
			int zIdx = offset.X + offset.Y * ds.Width; // z-buffer pixel index
			short hBufVal = (short)(obj.Tile.Z * Drawable.TileHeight / 2);
			short zOffset = (short)((obj.BottomTile.Rx + obj.BottomTile.Ry) * Drawable.TileHeight / 2 + props.ZAdjust);

			if (!dr.Flat)
				hBufVal += shp.Height;

			for (int y = 0; y < img.Height; y++) {
				if (offset.Y + y < 0) {
					w += stride;
					rIdx += img.Width;
					zIdx += ds.Width;
					continue; // out of bounds
				}

				for (int x = 0; x < img.Width; x++) {
					byte paletteValue = imgData[rIdx];

					short zshapeOffset = obj is StructureObject ? (GetBuildingZ(x, y, shp, img, obj)) : (short)0;

					if (paletteValue != 0) {
						short zBufVal = zOffset;
						if (dr.Flat)
							zBufVal += (short)(y - img.Height);
						else if (dr.IsBuildingPart) {
							// nonflat building
							zBufVal += zshapeOffset;
						}
						else
							zBufVal += img.Height;

						if (w_low <= w && w < w_high  /*&& zBufVal >= zBuffer[zIdx]*/) {
							if (transLucency != 0) {
								*(w + 0) = (byte)(a * *(w + 0) + b * p.Colors[paletteValue].B);
								*(w + 1) = (byte)(a * *(w + 1) + b * p.Colors[paletteValue].G);
								*(w + 2) = (byte)(a * *(w + 2) + b * p.Colors[paletteValue].R);
							}
							else {
								*(w + 0) = p.Colors[paletteValue].B;
								*(w + 1) = p.Colors[paletteValue].G;
								*(w + 2) = p.Colors[paletteValue].R;

								//var pal = Theater.Active.GetPalettes().UnitPalette.Colors;
								//*(w + 0) = pal[zshapeOffset].R;
								//*(w + 1) = pal[zshapeOffset].G;
								//*(w + 2) = pal[zshapeOffset].B;
							}
							zBuffer[zIdx] = zBufVal;
							heightBuffer[zIdx] = hBufVal;
						}
					}
					//else {
					//	*(w + 0) = 0;
					//	*(w + 1) = 0;
					//	*(w + 2) = 255;
					//}

					// Up to the next pixel
					rIdx++;
					zIdx++;
					w += 3;
				}
				w += stride - 3 * img.Width;
				zIdx += ds.Width - img.Width;
			}
		}

		public unsafe void DrawShadow(GameObject obj, ShpFile shp, DrawProperties props, DrawingSurface ds) {
			shp.Initialize();
			int frameIndex = props.FrameDecider(obj);
			if (obj.Drawable.IsActualWall)
				frameIndex = ((StructureObject)obj).WallBuildingFrame;
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
			var heightBuffer = ds.GetHeightBuffer();

			var w_low = (byte*)ds.BitmapData.Scan0;
			byte* w_high = (byte*)ds.BitmapData.Scan0 + stride * ds.BitmapData.Height;

			byte* w = (byte*)ds.BitmapData.Scan0 + offset.X * 3 + stride * offset.Y;
			int zIdx = offset.X + offset.Y * ds.Width;
			int rIdx = 0;
			short zOffset = (short)((obj.Tile.Rx + obj.Tile.Ry) * Drawable.TileHeight / 2 - shp.Height / 2 + img.Y);
			int castHeight = obj.Tile.Z * Drawable.TileHeight / 2;
			if (obj.Drawable != null && !obj.Drawable.Flat) {
				castHeight += shp.Height;
				castHeight += obj.Drawable.TileElevation * Drawable.TileHeight / 2;
			}

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
					if (0 <= offset.X + x && offset.X + x < ds.Width && 0 <= y + offset.Y && y + offset.Y < ds.Height &&
						imgData[rIdx] != 0 && !shadows[zIdx] &&
						// zBufVal >= zBuffer[zIdx] &&
						castHeight >= heightBuffer[zIdx]) {

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
				w += stride - 3 * img.Width;    // ... and if we're no more on the same row,
				zIdx += ds.Width - img.Width;
				// adjust the writing pointer accordingy
			}
		}

		public unsafe void DrawAlpha(GameObject obj, ShpFile shp, DrawProperties props, DrawingSurface ds) {
			shp.Initialize();

			// Ares supports multiframe AlphaImages, based on frame count and the direction the unit it facing.
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
			short zOffset = (short)((obj.Tile.Rx + obj.Tile.Ry) * Drawable.TileHeight / 2 - shp.Height / 2 + img.Y + props.ZAdjust);
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
				w += stride - 3 * img.Width;    // ... and if we're no more on the same row,
												// adjust the writing pointer accordingly
			}
		}

		private static byte limit(float mult, byte p) {
			return (byte)Math.Max(0f, Math.Min(255f, mult * p));
		}

		private static int DecideFrameIndex(int frameIndex, int numImages) {
			DrawFrame f = (DrawFrame)frameIndex;
			if (f == DrawFrame.Random)
				frameIndex = Rand.Next(numImages);
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

		static ShpFile BuildingZ;
		private bool _noBuildingZAvailable = false;
		private readonly VirtualFileSystem _vfs;

		private short GetBuildingZ(int x, int y, ShpFile shp, ShpFile.ShpImage img, GameObject obj) {
			if (_noBuildingZAvailable)
				return 0;

			else if (BuildingZ == null) {
				if (ModConfig.ActiveConfig.Engine < EngineType.YurisRevenge)
					BuildingZ = _vfs.Open<ShpFile>("buildngz.shp");
				else // Yuri's Revenge uses .sha as a file extension for this
					BuildingZ = _vfs.Open<ShpFile>("buildngz.sha");
				if (BuildingZ != null)
					BuildingZ.Initialize();
				else
					_noBuildingZAvailable = true;
			}

			var zImg = BuildingZ.GetImage(0);
			byte[] zData = zImg.GetImageData();

			// center x
			x += zImg.Width / 2 - shp.Width / 2 + img.X;

			// correct for foundation
			x -= (obj.Drawable.Foundation.Width - obj.Drawable.Foundation.Height) * 30;

			// add zshapepointmove
			x += obj.Drawable.Props.ZShapePointMove.X;

			// align y on bottom
			y += zImg.Height - shp.Height;

			// add zshapepointmove
			y -= obj.Drawable.Props.ZShapePointMove.Y;

			x = Math.Min(zImg.Width - 1, Math.Max(0, x));
			y = Math.Min(zImg.Height - 1, Math.Max(0, y));

			return (short)(-64 + zData[y * zImg.Width + x]);
		}

	}
}
