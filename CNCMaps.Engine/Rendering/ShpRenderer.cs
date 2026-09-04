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

		// BuildingClass::Draw_It hands Draw_Shape a z-shape reference point, moved by the art
		// ZShapePointMove and back by the foundation far corner, laid on the building draw point. Both
		// are engine literals, not derived from the shape, so a mod's BUILDNGZ of another size keeps
		// them: Tiberian Sun's (144, 172) is in OpenTS building.cpp, gamemd's (198, 446) at 0x43d6ff
		private int _zShapeRefX, _zShapeRefY;
		// the body stands 2 in front of the ground at its sprite bottom row (Techno_Draw_Object's
		// zadjust - 2) and each shape byte adds its value less a bias. Tiberian Sun takes 39 off every
		// byte when it loads the shape (BuildingTypeClass::Fetch_Z_Data) and the blitter reads them
		// signed; captured depth buffers put every building one z behind that, so 40. gamemd's 66 is a
		// constant measured the same way, not a normalisation. Where the shape holds no byte the pixel
		// keeps the plain 2, which draws a wide sprite's lower fringe beside its foundation (CAMEX02)
		// instead of sinking it
		private const int BodyLift = 2;
		private int _zShapeBias;
		private byte[] _buildingZShape;
		private int _zShapeX, _zShapeY, _zShapeWidth, _zShapeHeight; // frame 0's place and size on its canvas
		private bool _buildingZShapeTried;

		/// <summary>BUILDNGZ, the per-pixel z pyramid the game blits under a building's own shapes: 288x197
		/// BUILDNGZ.SHP in Tiberian Sun's conquer.mix, 396x477 BUILDNGZ.SHA in gamemd's conqmd.mix (Red Alert 2's
		/// game.exe reads the same file as BUILDNGZ.SHP from conquer.mix).</summary>
		private byte[] BuildingZShape {
			get {
				if (!_buildingZShapeTried) {
					_buildingZShapeTried = true;
					if (_config.Engine >= EngineType.RedAlert2) {
						_zShapeRefX = 198;
						_zShapeRefY = 446;
						_zShapeBias = -66;
					}
					else {
						_zShapeRefX = 144;
						_zShapeRefY = 172;
						_zShapeBias = -40;
					}
					var sha = _vfs.Open<ShpFile>("buildngz.sha") ?? _vfs.Open<ShpFile>("buildngz.shp");
					if (sha != null) {
						sha.Initialize();
						var frame = sha.NumImages > 0 ? sha.GetImage(0) : null;
						var data = frame?.GetImageData();
						if (data != null && data.Length == frame.Width * frame.Height && data.Length > 0) {
							_buildingZShape = data;
							_zShapeX = frame.X;
							_zShapeY = frame.Y;
							_zShapeWidth = frame.Width;
							_zShapeHeight = frame.Height;
						}
					}
					if (_buildingZShape == null)
						Logger.Debug("No usable BUILDNGZ z-shape; buildings fall back to the flat standing z profile");
				}
				return _buildingZShape;
			}
		}

		private int SampleZShape(byte[] zShape, int x, int y, int originX, int originY) {
			int col = x - originX - _zShapeX, row = y - originY - _zShapeY;
			if (col < 0 || col >= _zShapeWidth || row < 0 || row >= _zShapeHeight)
				return 0;
			return zShape[row * _zShapeWidth + col];
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
			// a foundation cell's copy of a smudge draws at the entry cell's spot (SmudgeTypeClass::DrawIt)
			if (obj is SmudgeObject smudge) {
				offset.X -= (smudge.FoundationCell.X - smudge.FoundationCell.Y) * _config.TileWidth / 2;
				offset.Y -= (smudge.FoundationCell.X + smudge.FoundationCell.Y) * _config.TileHeight / 2;
			}
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
			// A smudge is a plain blit (SmudgeTypeClass::DrawIt flags 0xE00: neither z-tested nor
			// written); it is drawn in the tile pass, so the tiles of later cells paint over it.
			bool unitLike = obj is UnitObject || obj is InfantryObject || obj is AircraftObject;
			bool plainBlit = obj is SmudgeObject;
			bool isBuilding = obj is StructureObject;
			// AnimClass never carries SHAPE_ZWRITE: it is constructed with SHAPE_WIN_REL|SHAPE_CENTER and
			// no draw path adds the flag, so an anim paints colour without storing depth. This covers a
			// building's ActiveAnim too, whose flat standing profile would otherwise overwrite the body's
			// z cone across the whole footprint. A tile's animation is not an AnimClass: the game draws
			// those from IsoTileTypeClass with SHAPE_ZWRITE set (isotype.cpp, cell.cpp), so animated
			// water and its kin keep storing depth.
			// a SHP turret is the building's turret anim (BANIM_TURRET), an AnimClass like the rest
			bool animLike = dr is AnimDrawable || dr.IsTurret;
			// the veinhole monster draws with SHAPE_ZGRAD and no ZWRITE either (VeinholeMonsterClass::Draw_It)
			bool isAnim = (animLike || dr.IsVeinHoleMonster) && !(obj is MapTile);
			// the building body takes its z from BUILDNGZ below. Tiberian Sun draws a foundation six or more
			// cells wide (UFO) on the plain standing profile instead (BuildingClass::Draw_It); gamemd keeps the
			// shape on its 6x4s. Anims are drawn by AnimClass and get no shape either way
			byte[] zShape = isBuilding && !dr.Flat && !animLike ? BuildingZShape : null;
			if (zShape != null && _config.Engine <= EngineType.Firestorm && (obj.Drawable?.Foundation.Width ?? 1) >= 6)
				zShape = null;
			int zLift;
			if (obj is OverlayObject)
				// flat overlays sit at ground+1 (the Ground gradient keeps 1 of the game's +2 overlay ZAdjust),
				// standing ones take the full lift
				zLift = dr.Flat ? 1 : dr.IsWall || dr.IsRock ? 2 : 17;
			else if (isBuilding)
				// an attached anim draws at ZAdjust -2 plus its own art value (AnimClass::Draw_It), a turret
				// with TurretAnimZAdjust as that value. A bib or flat anim lies on the Ground gradient one in
				// front of its tile like ore. A body without the shape stands on the plain profile at the
				// same -2 (Techno_Draw_Object)
				zLift = dr.Flat ? 1 : animLike || zShape == null ? 2 : 0;
			else if (unitLike)
				zLift = 1;
			else
				// a smudge or a flat anim lies on the Ground gradient one in front of its tile, like ore
				zLift = dr.Flat ? 1 : 12;
			// a high bridge piece's BottomTile is two cells down-right for draw order only; its deck sits
			// TileElevation above its own cell, so a piece landing on the abutment must not also take that
			// tile's height or the units on the last deck piece vanish behind it
			var bt = obj is OverlayObject ? obj.Tile : obj.BottomTile;
			int cellBottomY = (bt.Dy - bt.Z) * _config.TileHeight / 2 + _config.TileHeight - 1;
			int spriteBottomY = offset.Y + img.Height - 1;
			// every shape anchors its gradient at its own drawn bottom row (ddrect bottom in Shape_Draw_Z).
			// A damage fire is the exception: its game coordinate carries a height we do not model, so it
			// borrows the body anchor and burns against the body
			int zAnchorY = spriteBottomY;
			if (isBuilding && animLike && dr.AnchorToBody) {
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

			// BuildingClass::Draw_It (gamemd 0x43d767) hands the building's own shapes BUILDNGZ as the
			// Draw_Shape z-shape: a pyramid that falls 1 z per 3 rows down and per 3 px sideways from
			// its top centre, cut off below by the lower half of an iso diamond whose tip is the
			// foundation bottom corner. The blitter adds the biased byte to the z of the sprite bottom
			// row and applies no standing gradient of its own (RLE_Blit rounds only without a shape).
			int zShapeX = 0, zShapeY = 0;
			if (zShape != null) {
				var fnd = obj.Drawable?.Foundation ?? new Size(1, 1);
				var move = props.ZShapePointMove;
				int drawX = offset.X - img.X + shp.Width / 2, drawY = offset.Y - img.Y + shp.Height / 2;
				zShapeX = drawX - _zShapeRefX - move.X + (fnd.Width - fnd.Height) * (_config.TileWidth / 2);
				zShapeY = drawY - _zShapeRefY - move.Y + (fnd.Width + fnd.Height - 2) * (_config.TileHeight / 2);
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
							zBufVal = (short)(zGround + BodyLift - props.ZAdjust + (sample > 0 ? sample + _zShapeBias : 0));
						}
						else
							zBufVal = (short)(zGround + standingLift + (zAnchorY - (offset.Y + y)) / 3);

						// the RLE blitters draw only a strictly nearer pixel: ties keep the earlier drawing
						if (w_low <= w && w < w_high && (plainBlit || zBufVal > zBuffer[zIdx])) {
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
								if (!isAnim && !plainBlit)
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
			bool animLike = dr is AnimDrawable || (dr != null && dr.IsTurret);
			bool building = obj is StructureObject && !animLike && (dr == null || !dr.Flat);
			bool unitLike = obj is UnitObject || obj is InfantryObject || obj is AircraftObject;
			bool isAnim = animLike && !(obj is MapTile);
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

		/// <summary>A cliff piece's cast shadow (Tiberian Sun Draw_Shadow_Caster): the frame is centred on the
		/// given point and darkens whatever lies behind the plane the game gives it, the Ground gradient at
		/// ZAdjust -2 - 12*(Height-4), written to z like a building shadow.</summary>
		public unsafe void DrawTileShadow(MapTile tile, ShpFile shp, int frameIndex, Point centre, DrawingSurface ds) {
			shp.Initialize();
			if (frameIndex < 0 || frameIndex >= shp.Images.Count)
				return;
			var img = shp.GetImage(frameIndex);
			var imgData = img.GetImageData();
			if (imgData == null || img.Width * img.Height != imgData.Length)
				return;
			var offset = new Point(centre.X - shp.Width / 2 + img.X, centre.Y - shp.Height / 2 + img.Y);

			int stride = ds.BitmapData.Stride;
			var zBuffer = ds.GetZBuffer();
			byte* w = (byte*)ds.BitmapData.Scan0 + offset.X * 3 + stride * offset.Y;
			int zIdx = offset.X + offset.Y * ds.Width;
			int rIdx = 0;
			int cellBottomY = (tile.Dy - tile.Z) * _config.TileHeight / 2 + _config.TileHeight - 1;
			int zBase = (tile.Rx + tile.Ry) * _config.TileHeight / 2;
			// ZAdjust -2 - 12*(Height-4) at rows the game draws 12*Height higher: against the anchor above,
			// which already carries the height, the plane lies 12*4 - 2 behind the caster's own tile, less
			// the usual 1 (the building shadow's 3 is -1 - (-4)). That is one z in front of ground four
			// levels lower, so the shadow darkens the low ground beyond the rim and never the plateau
			int lift = -1 - (4 * _config.TileHeight / 2 - 2);

			for (int y = 0; y < img.Height; y++) {
				short zBufVal = (short)(zBase + (offset.Y + y) - cellBottomY + lift);
				for (int x = 0; x < img.Width; x++) {
					if (0 <= offset.X + x && offset.X + x < ds.Width && 0 <= offset.Y + y && offset.Y + y < ds.Height &&
						imgData[rIdx] != 0 && zBufVal > zBuffer[zIdx]) {
						*(w + 0) /= 2;
						*(w + 1) /= 2;
						*(w + 2) /= 2;
						zBuffer[zIdx] = zBufVal;
					}
					rIdx++;
					zIdx++;
					w += 3;
				}
				w += stride - 3 * img.Width;
				zIdx += ds.Width - img.Width;
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
