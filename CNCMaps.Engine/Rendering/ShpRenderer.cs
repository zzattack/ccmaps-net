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

		private readonly ModConfig _config;
		private readonly VirtualFileSystem _vfs;

		public ShpRenderer(ModConfig config, VirtualFileSystem vfs) {
			_config = config;
			_vfs = vfs;
		}

		private const int ZShapeWidth = 396, ZShapeHeight = 477;
		// gamemd BuildingClass::Draw_It hands CC_Draw_Shape the z-shape reference point (198, 446),
		// moved by the art ZShapePointMove and back by the foundation far corner, laid on the building
		// draw point
		private const int ZShapeRefY = 446;
		// the body stands 2 in front of the ground at its sprite bottom row (ZAdjust -2 - Zpix) and
		// each shape byte adds its value less 66, a constant bias, not a normalisation. Where the
		// shape holds no byte the pixel keeps the plain 2, which draws a wide sprite's lower fringe
		// beside its foundation (CAMEX02) instead of sinking it
		private const int BodyLift = 2, ZShapeBias = -66;
		private byte[] _buildingZShape;
		private bool _buildingZShapeTried;

		/// <summary>BUILDNGZ.SHA, the per-pixel z cone the game blits under a building's own
		/// shapes. Absent in Tiberian Sun, where buildings keep the plain standing profile.</summary>
		private byte[] BuildingZShape {
			get {
				if (!_buildingZShapeTried) {
					_buildingZShapeTried = true;
					var sha = _vfs.Open<ShpFile>("buildngz.sha");
					if (sha != null) {
						sha.Initialize();
						var frame = sha.NumImages > 0 ? sha.GetImage(0) : null;
						var data = frame?.GetImageData();
						if (data != null && frame.Width == ZShapeWidth && frame.Height == ZShapeHeight)
							_buildingZShape = data;
					}
					if (_buildingZShape == null)
						Logger.Debug("No usable buildngz.sha; buildings fall back to the flat standing z profile");
				}
				return _buildingZShape;
			}
		}

		private static int SampleZShape(byte[] zShape, int x, int y, int originX, int originY) {
			int col = x - originX, row = y - originY;
			if (col < 0 || col >= ZShapeWidth || row < 0 || row >= ZShapeHeight)
				return 0;
			return zShape[row * ZShapeWidth + col];
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
			byte[] bgr = p.GetBgrBytes();
			int frameIndex = props.FrameDecider(obj);
			if (obj.Drawable.IsActualWall)
				frameIndex = ((StructureObject)obj).WallBuildingFrame;
			frameIndex = DecideFrameIndex(frameIndex, shp.NumImages);
			if (frameIndex < 0 || frameIndex >= shp.Images.Count)
				return;

			var img = shp.GetImage(frameIndex);
			var imgData = img.GetImageData();
			if (imgData == null || img.Width * img.Height != imgData.Length)
				return;

			Point offset = props.GetOffset(obj);
			offset.X += obj.Tile.Dx * _config.TileWidth / 2 - shp.Width / 2 + img.X;
			offset.Y += (obj.Tile.Dy - obj.Tile.Z) * _config.TileHeight / 2 - shp.Height / 2 + img.Y;
			// something standing on a slope stands on its surface, not on the cell's stored corner;
			// an overlay is drawn from its cell's level alone (CellClass::Overlay_Draw_Offset 0x480110)
			int rampLift = dr.Flat || obj is OverlayObject ? 0 : RampHeight.PixelLift(obj.Tile, _config);
			offset.Y -= rampLift;
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
			short hBufVal = (short)(obj.Tile.Z * _config.TileHeight / 2);

			// Game z model (gamemd Shape_Draw_Z, CellClass::DrawOverlay): tiles write the ground ramp
			// that reaches zBase at the cell diamond's bottom row (TmpRenderer: zBase - ZData). A standing
			// shape anchors at its drawn bottom row, sits a per-class lift in front of the ground there,
			// and recedes 1 z per 3 rows toward its top; a flat shape follows the ground ramp. Lifts:
			// terrain objects 12 (game ZAdjust -AdjustForZ-12), wall/rock overlays and all flat overlays
			// (ore, roads, bridge decks) 2, other standing overlays 17, units/infantry/aircraft 1.
			// Units and their shadows are only z-tested, never written (the game blits them with the
			// ZRead blitter family), so anything drawn later must carry its own closer z or cover them.
			// A smudge is tested but never written either (SmudgeTypeClass::Draw_It blits without
			// SHAPE_ZWRITE): a cliff face in front of it keeps its pixels, ore drawn after it paints over it.
			bool unitLike = obj is UnitObject || obj is InfantryObject || obj is AircraftObject;
			bool zWrite = !(obj is SmudgeObject);
			bool isBuilding = obj is StructureObject;
			// AnimClass never carries SHAPE_ZWRITE: it is constructed with SHAPE_WIN_REL|SHAPE_CENTER and
			// no draw path adds the flag, so an anim paints colour without storing depth. This covers a
			// building's ActiveAnim too, whose flat standing profile would otherwise overwrite the body's
			// z cone across the whole footprint. A tile's animation is not an AnimClass: the game draws
			// those from IsoTileTypeClass with SHAPE_ZWRITE set (isotype.cpp, cell.cpp), so animated
			// water and its kin keep storing depth.
			bool isAnim = dr is AnimDrawable && !(obj is MapTile);
			int zLift;
			if (obj is OverlayObject)
				// flat overlays sit at ground+1 (the Ground gradient keeps 1 of the game's +2 overlay ZAdjust),
				// standing ones take the full lift
				zLift = dr.Flat ? 1 : dr.IsWall || dr.IsRock ? 2 : 17;
			else if (isBuilding)
				// the body itself takes its z from the z-shape below. An attached anim draws at ZAdjust -2
				// plus its own art value (AnimClass::Draw_It), a turret at TurretAnimZAdjust alone
				// (BuildingClass turret z). A bib or flat anim lies on the Ground gradient one in front of
				// its tile like ore; Tiberian Sun bodies, which have no shape, keep 1
				zLift = BuildingZShape != null ? (dr.Flat ? 1 : dr is AnimDrawable ? 2 : 0) : 1;
			else if (unitLike)
				zLift = 1;
			else
				// a smudge or a flat anim lies on the Ground gradient one in front of its tile, like ore
				zLift = dr.Flat ? 1 : 12;
			var bt = obj.BottomTile;
			int cellBottomY = (bt.Dy - bt.Z) * _config.TileHeight / 2 + _config.TileHeight - 1;
			int spriteBottomY = offset.Y + img.Height - 1;
			// every shape anchors its gradient at its own drawn bottom row (ddrect bottom in Shape_Draw_Z).
			// A damage fire is the exception: its game coordinate carries a height we do not model, so it
			// borrows the body anchor and burns against the body
			int zAnchorY = spriteBottomY;
			if (isBuilding && dr is AnimDrawable && dr.AnchorToBody) {
				int? bodyAnchor = ((StructureObject)obj).DrawnBodyAnchorY;
				zAnchorY = Math.Max(spriteBottomY, bodyAnchor ?? cellBottomY);
			}
			// the game's ZAdjust is -Z_Lepton_To_Pixel(Position.Z), an absolute height that already
			// contains the ramp surface, so the lift moves the sprite on screen without moving it
			// in z; add it back here or a shape on a slope sits a lift behind its own ground
			int zGround = (bt.Rx + bt.Ry) * _config.TileHeight / 2 + (zAnchorY + rampLift - cellBottomY)
				+ dr.TileElevation * _config.TileHeight / 2;
			// units on a bridge draw raised; their z stays anchored on the deck plane
			if (unitLike && obj is OwnableObject oo && oo.OnBridge)
				zGround += 4 * _config.TileHeight / 2;

			if (!dr.Flat)
				hBufVal += shp.Height;

			// Shape_Draw_Z (0x4373b0) starts the standing profile at the bottom row's depth plus ZAdjust
			// rounded down to a multiple of 3, plus 1. Over the ground that is -ZAdjust - 1 plus the phase
			// (depth + ZAdjust) mod 3; the phase constant 2 is where the goldens' capture puts the buffer.
			// A z-shape body skips the rounding; units keep the flat +1 until their gradient is measured.
			int lift = zLift - props.ZAdjust;
			int standingLift = unitLike ? lift : lift - 1 + (((2 - zAnchorY - lift) % 3) + 3) % 3;

			// BuildingClass::Draw_It (0x43d767) hands the building's own shapes BUILDNGZ.SHA as
			// Shape_Draw_Z z-shape: a pyramid that falls 1 z per 3 rows down and per 3 px sideways from
			// its top centre, cut off below by the lower half of an iso diamond whose tip is the
			// foundation bottom corner. The blitter adds the byte (unsigned, biased) to the z of the
			// sprite bottom row and applies no standing gradient of its own. Anims are drawn by
			// AnimClass and get no shape, so they keep the plain profile.
			byte[] zShape = isBuilding && !dr.Flat && !(dr is AnimDrawable) ? BuildingZShape : null;
			int zShapeX = 0, zShapeY = 0;
			if (zShape != null) {
				var fnd = obj.Drawable?.Foundation ?? new Size(1, 1);
				var move = props.ZShapePointMove;
				int drawX = offset.X - img.X + shp.Width / 2, drawY = offset.Y - img.Y + shp.Height / 2;
				zShapeX = drawX - ZShapeWidth / 2 - move.X + (fnd.Width - fnd.Height) * (_config.TileWidth / 2);
				zShapeY = drawY - ZShapeRefY - move.Y + (fnd.Width + fnd.Height - 2) * (_config.TileHeight / 2);
			}

			for (int y = 0; y < img.Height; y++) {
				if (offset.Y + y < 0) {
					w += stride;
					rIdx += img.Width;
					zIdx += ds.Width;
					continue; // out of bounds
				}

				for (int x = 0; x < img.Width; x++) {
					byte paletteValue = imgData[rIdx];

					if (paletteValue != 0) {
						// ZAdjust uses the game's sign: positive pushes away from the screen
						short zBufVal;
						if (dr.Flat)
							zBufVal = (short)(zGround + zLift + (offset.Y + y) - zAnchorY - props.ZAdjust);
						else if (zShape != null) {
							int sample = SampleZShape(zShape, offset.X + x, offset.Y + y, zShapeX, zShapeY);
							zBufVal = (short)(zGround + BodyLift - props.ZAdjust + (sample > 0 ? sample + ZShapeBias : 0));
						}
						else
							zBufVal = (short)(zGround + standingLift + (zAnchorY - (offset.Y + y)) / 3);

						// the RLE blitters draw only a strictly nearer pixel: ties keep the earlier drawing
						if (w_low <= w && w < w_high && zBufVal > zBuffer[zIdx]) {
							int ci = paletteValue * 3;
							if (transLucency != 0) {
								*(w + 0) = (byte)(a * *(w + 0) + b * bgr[ci]);
								*(w + 1) = (byte)(a * *(w + 1) + b * bgr[ci + 1]);
								*(w + 2) = (byte)(a * *(w + 2) + b * bgr[ci + 2]);
							}
							else {
								*(w + 0) = bgr[ci];
								*(w + 1) = bgr[ci + 1];
								*(w + 2) = bgr[ci + 2];
							}
							if (!unitLike) {
								if (!isAnim && zWrite)
									zBuffer[zIdx] = zBufVal;
								heightBuffer[zIdx] = hBufVal;
							}
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

		public unsafe void DrawShadow(GameObject obj, ShpFile shp, Drawable dr, DrawProperties props, DrawingSurface ds) {
			shp.Initialize();
			int frameIndex = props.FrameDecider(obj);
			if (obj.Drawable.IsActualWall)
				frameIndex = ((StructureObject)obj).WallBuildingFrame;
			frameIndex = DecideFrameIndex(frameIndex, shp.NumImages);
			if (frameIndex < 0)
				return;
			frameIndex += shp.Images.Count / 2; // latter half are shadow Images
			if (frameIndex >= shp.Images.Count)
				return;

			var img = shp.GetImage(frameIndex);
			var imgData = img.GetImageData();
			if (imgData == null || img.Width * img.Height != imgData.Length)
				return;

			Point offset = props.GetShadowOffset(obj);
			offset.X += obj.Tile.Dx * _config.TileWidth / 2 - shp.Width / 2 + img.X;
			offset.Y += (obj.Tile.Dy - obj.Tile.Z) * _config.TileHeight / 2 - shp.Height / 2 + img.Y;
			int rampLift = obj.Drawable != null && !obj.Drawable.Flat && !(obj is OverlayObject)
				? RampHeight.PixelLift(obj.Tile, _config) : 0;
			offset.Y -= rampLift;
			Logger.Trace("Drawing SHP shadow {0} (frame {1}) at ({2},{3})", shp.FileName, frameIndex, offset.X, offset.Y);

			int stride = ds.BitmapData.Stride;
			var zBuffer = ds.GetZBuffer();

			byte* w = (byte*)ds.BitmapData.Scan0 + offset.X * 3 + stride * offset.Y;
			int zIdx = offset.X + offset.Y * ds.Width;
			int rIdx = 0;

			// Shadows lie on the caster's ground plane, a per-class lift in front of it: gamemd draws them
			// with the Ground z-gradient and darkens only where that plane is in front of what the pixel
			// holds; as with overlays, the gradient keeps one less than the game's ZAdjust. A building's
			// shadow keeps the ground gradient and carries no z-shape, but the engine gives it ZAdjust -4
			// against the body's -2 (FUN_00705e00: iStack_c = -4 - heightAdjust, gradient 0, z-shape args
			// zeroed) and draws it one call after the body, so it darkens the body wherever the cone has
			// dipped below it. Terrain shadows carry ZAdjust base-3 (0x71c320); unit shadows only test.
			// gamemd stacks a building shadow on a tree shadow but never two shadows of one class: the
			// strict test on the lifts alone does that, a tie is never darkened twice.
			bool building = obj is StructureObject && !(dr is AnimDrawable) && (dr == null || !dr.Flat);
			bool unitLike = obj is UnitObject || obj is InfantryObject || obj is AircraftObject;
			bool isAnim = dr is AnimDrawable && !(obj is MapTile);
			int shadowLift = building || unitLike ? 3 : 2;
			var t = obj.Tile;
			int cellBottomY = (t.Dy - t.Z) * _config.TileHeight / 2 + _config.TileHeight - 1;
			int zBase = (t.Rx + t.Ry) * _config.TileHeight / 2;

			for (int y = 0; y < img.Height; y++) {
				if (offset.Y + y < 0) {
					w += stride;
					rIdx += img.Width;
					zIdx += ds.Width;
					continue; // out of bounds
				}

				// as in Draw: the ramp lift is a screen offset, not a depth one
				short zBufVal = (short)(zBase + (offset.Y + y + rampLift) - cellBottomY + shadowLift);

				for (int x = 0; x < img.Width; x++) {
					if (0 <= offset.X + x && offset.X + x < ds.Width && 0 <= y + offset.Y && y + offset.Y < ds.Height &&
						imgData[rIdx] != 0 &&
						zBufVal > zBuffer[zIdx]) {

						*(w + 0) /= 2;
						*(w + 1) /= 2;
						*(w + 2) /= 2;
						if (!unitLike && !isAnim)
							zBuffer[zIdx] = zBufVal;
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
			offset.X += obj.Tile.Dx * _config.TileWidth / 2;
			offset.Y += (obj.Tile.Dy - obj.Tile.Z) * _config.TileHeight / 2;
			Logger.Trace("Drawing AlphaImage SHP file {0} (frame {1}) at ({2},{3})", shp.FileName, frameIndex, offset.X, offset.Y);

			int stride = ds.BitmapData.Stride;
			var w_low = (byte*)ds.BitmapData.Scan0;
			byte* w_high = (byte*)ds.BitmapData.Scan0 + stride * ds.BitmapData.Height;

			int dx = offset.X + _config.TileWidth / 2 - shp.Width / 2 + img.X,
				dy = offset.Y - shp.Height / 2 + img.Y;
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

	}
}
