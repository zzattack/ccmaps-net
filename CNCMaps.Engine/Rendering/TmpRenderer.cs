using System;
using System.Drawing;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Map;
using CNCMaps.FileFormats;
using NLog;

namespace CNCMaps.Engine.Rendering {
	class TmpRenderer {
		static Logger Logger = LogManager.GetCurrentClassLogger();

		public static Rectangle GetBounds(MapTile tile, TmpFile tmp) {
			tmp.Initialize();

			if (tile.SubTile >= tmp.Images.Count) return Rectangle.Empty;
			var img = tmp.Images[tile.SubTile];

			int left = tile.Dx * tmp.BlockWidth / 2;
			int top = (tile.Dy - tile.Z) * tmp.BlockHeight / 2;
			int width = tmp.BlockWidth;
			int height = tmp.BlockHeight;
			if (img.HasExtraData) {
				if (img.ExtraX < 0) { left += img.ExtraX; width -= img.ExtraX; }
				if (img.ExtraY < 0) { top += img.ExtraY; height -= img.ExtraY; }
				width = Math.Max(width, img.ExtraWidth);
				height = Math.Max(height, img.ExtraHeight);
			}

			return new Rectangle(left, top, width, height);
		}

		unsafe public static void Draw(MapTile tile, TmpFile tmp, DrawingSurface ds) {
			tmp.Initialize();

			if (tile.SubTile >= tmp.Images.Count) return;
			TmpFile.TmpImage img = tmp.Images[tile.SubTile];
			var zBuffer = ds.GetZBuffer();
			var heightBuffer = ds.GetHeightBuffer();
			Palette p = tile.Palette;

			// calculate tile index -> pixel index
			Point offset = new Point(tile.Dx * tmp.BlockWidth / 2, (tile.Dy - tile.Z) * tmp.BlockHeight / 2);

			// make touched tiles (used for determining image cutoff)
			Point center = offset + new Size(tmp.BlockWidth / 2, tmp.BlockHeight / 2);
			var centerGridTile = tile.Layer.GetTileScreen(center, true, true);
			if (centerGridTile != null) {
				tile.Layer.GridTouched[centerGridTile.Dx, centerGridTile.Dy / 2] |= TileLayer.TouchType.ByNormalData;
				tile.Layer.GridTouchedBy[centerGridTile.Dx, centerGridTile.Dy / 2] = tile;
			}

			Logger.Trace("Drawing TMP file {0} (subtile {1}) at ({2},{3})", tmp.FileName, tile.SubTile, offset.X, offset.Y);

			int stride = ds.BitmapData.Stride;

			int halfCx = tmp.BlockWidth / 2,
				halfCy = tmp.BlockHeight / 2;

			// writing bounds
			var w_low = (byte*)ds.BitmapData.Scan0;
			byte* w_high = (byte*)ds.BitmapData.Scan0 + stride * ds.BitmapData.Height;
			byte* w = (byte*)ds.BitmapData.Scan0 + stride * offset.Y + (offset.X + halfCx - 2) * 3;

			int rIdx = 0, x, y = 0;
			int zIdx = offset.Y * ds.Width + offset.X + halfCx - 2;
			int cx = 0; // Amount of pixel to copy

			for (; y < halfCy; y++) {
				cx += 4;
				for (ushort c = 0; c < cx; c++) {
					byte paletteValue = img.TileData[rIdx];

					short zBufVal = (short)((tile.Rx + tile.Ry) * tmp.BlockHeight / 2 - (img.ZData != null ? img.ZData[rIdx] : 0));
					if (paletteValue != 0 && w_low <= w && w < w_high && zBufVal >= zBuffer[zIdx]) {
						*(w + 0) = p.Colors[paletteValue].B;
						*(w + 1) = p.Colors[paletteValue].G;
						*(w + 2) = p.Colors[paletteValue].R;
						zBuffer[zIdx] = zBufVal;
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
			for (; y < tmp.BlockHeight; y++) {
				cx -= 4;
				for (ushort c = 0; c < cx; c++) {
					byte paletteValue = img.TileData[rIdx];

					short zBufVal = (short)((tile.Rx + tile.Ry) * tmp.BlockHeight / 2 - (img.ZData != null ? img.ZData[rIdx] : 0));
					if (paletteValue != 0 && w_low <= w && w < w_high && zBufVal >= zBuffer[zIdx]) {
						*(w + 0) = p.Colors[paletteValue].B;
						*(w + 1) = p.Colors[paletteValue].G;
						*(w + 2) = p.Colors[paletteValue].R;
						zBuffer[zIdx] = zBufVal;
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
			var extraScreenBounds = Rectangle.FromLTRB(
				Math.Max(0, offset.X), Math.Max(0, offset.Y),
				Math.Min(offset.X + img.ExtraWidth, ds.Width), Math.Min(offset.Y + img.ExtraHeight, ds.Height));

			for (int by = extraScreenBounds.Top; by < extraScreenBounds.Bottom; by += tmp.BlockHeight / 2) {
				for (int bx = extraScreenBounds.Left; bx < extraScreenBounds.Right; bx += tmp.BlockWidth / 2) {
					var gridTileNoZ = tile.Layer.GetTileScreen(new Point(bx, by), true, true);
					if (gridTileNoZ != null) {
						Logger.Trace("Tile at ({0},{1}) has extradata affecting ({2},{3})", tile.Dx, tile.Dy, gridTileNoZ.Dx,
							gridTileNoZ.Dy);
						tile.Layer.GridTouched[gridTileNoZ.Dx, gridTileNoZ.Dy / 2] |= TileLayer.TouchType.ByExtraData;
						tile.Layer.GridTouchedBy[gridTileNoZ.Dx, gridTileNoZ.Dy / 2] = tile;
					}
				}
			}

			// Extra graphics are just a square
			for (y = 0; y < img.ExtraHeight; y++) {
				for (x = 0; x < img.ExtraWidth; x++) {
					// Checking per line is required because v needs to be checked every time
					byte paletteValue = img.ExtraData[rIdx];
                    // Starkku: Matched to formula with normal tile zdata one to fix several glitches with certain kind of tile setups.
                    short zBufVal = (short)((tile.Rx + tile.Ry) * tmp.BlockHeight / 2 - (img.ExtraZData != null ? img.ExtraZData[rIdx] : 0));

					if (paletteValue != 0 && w_low <= w && w < w_high && zBufVal >= zBuffer[zIdx]) {
						*w++ = p.Colors[paletteValue].B;
						*w++ = p.Colors[paletteValue].G;
						*w++ = p.Colors[paletteValue].R;
						zBuffer[zIdx] = zBufVal;
						heightBuffer[zIdx] = (short)(img.ExtraHeight - y + tile.Z * Drawable.TileHeight / 2);
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
