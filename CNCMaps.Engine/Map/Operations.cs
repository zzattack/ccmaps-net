using System;
using System.Collections.Generic;
using System.Linq;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Game;
using CNCMaps.FileFormats;
using CNCMaps.Shared;
using CNCMaps.Shared.Utility;
using NLog;

namespace CNCMaps.Engine.Map {
	class Operations {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Give a tiberium overlay the art the game would draw for its cell.
		/// The engine never rewrites the cell's overlay; it picks one of the type's twelve images
		/// at draw time from the cell's own coordinates, and one of the eight slope pieces that
		/// follow them when the cell ramps. The stored drawable is kept because the shadow still
		/// comes from the id the map holds.
		/// </summary>
		public static void ApplyTiberiumArt(MapTile tile, OverlayObject ovl, EngineType engine) {
			if (ovl.Drawable == null || ovl.Collection == null)
				return;
			int rampType = (tile.Drawable as TileDrawable)?.GetTileImage(tile)?.RampType ?? 0;
			int pooled = SpecialOverlays.GetPooledDrawId(ovl, engine, rampType);
			if (pooled == ovl.OverlayID || pooled >= ovl.Collection.DrawableCount)
				return;
			var pooledDrawable = ovl.Collection.GetDrawable(pooled);
			if (pooledDrawable == null)
				return;
			ovl.StoredDrawable = ovl.Drawable;
			ovl.Drawable = pooledDrawable;
		}

		// Tiberian Sun rebuilds the vein field when a scenario loads (OverlayClass::Post_Read_Vein_Fixups):
		// every VEINS cell is cleared and only the solid pieces (OverlayData 48 and up, ramp pieces
		// included) are placed again, each spreading a connecting piece onto its four cardinal
		// neighbours. The map's own connecting pieces are discarded, and a solid piece the terrain
		// rejects disappears with them. The engine walks the solid cells in reverse, which only changes
		// which random roll a cell gets.
		public static void RecalculateVeinsSpread(List<OverlayObject> ovls) {
			var veins = ovls.Where(o => IsVeins(o) && !o.Drawable.IsVeinHoleMonster && (o.Drawable as ShpDrawable)?.Shp != null).ToList();
			if (veins.Count == 0) return;
			var field = new VeinField(ovls, veins[0].OverlayID, veins[0].Drawable);
			// MapClass::Iterate walks screen rows top to bottom, left to right; the fixup takes the
			// solid cells it collected from the last back to the first
			var solid = veins.Where(o => o.OverlayValue >= VeinField.FirstSolid).Select(o => o.Tile)
				.OrderByDescending(t => t.Rx + t.Ry).ThenByDescending(t => t.Rx).ToList();
			foreach (var o in veins) {
				o.Tile.RemoveObject(o, true);
				ovls.Remove(o);
			}
			foreach (var t in solid)
				if (field.CanPlaceVeins(t))
					field.PlaceVeins(t);
		}

		/// <summary>The scenario randomizer as the engine entered its vein fixup, from a capture; null
		/// rolls the pieces from the renderer's own generator instead.</summary>
		public static void SetVeinRandomizer(uint[] state) {
			_veinRandom = state == null ? null : new Random2(state);
		}

		private static Random2 _veinRandom;

		// Random2Class: a 250-entry XOR lagged-Fibonacci table, Index1 and Index2 = Index1 + 103
		private class Random2 {
			private readonly int[] _table = new int[250];
			private int _i1, _i2;

			public Random2(uint[] state) {
				_i1 = (int)state[0];
				_i2 = (int)state[1];
				for (int i = 0; i < 250; i++)
					_table[i] = unchecked((int)state[i + 2]);
			}

			public int Next() {
				_table[_i1] ^= _table[_i2];
				int val = _table[_i1];
				if (++_i1 >= 250) _i1 = 0;
				if (++_i2 >= 250) _i2 = 0;
				return val;
			}
		}

		public static bool IsVeins(OverlayObject o) {
			return o != null && o.Drawable.IsVeins;
		}

		private class VeinField {
			public const int FirstSolid = 48;
			private const int FirstRamp = FirstSolid + 3;

			// the engine's abs(RandomNumber()) % 3 and abs(RandomNumber()) & 1
			private static int Roll3() => _veinRandom != null ? Math.Abs(_veinRandom.Next()) % 3 : Rand.Next(3);
			private static int Roll2() => _veinRandom != null ? Math.Abs(_veinRandom.Next()) & 1 : Rand.Next(2);

			// CellClass::Adjacent_Cell order N, E, S, W; map north is the screen's top right
			private static readonly TileLayer.TileDirection[] Cardinal = {
				TileLayer.TileDirection.TopRight, TileLayer.TileDirection.BottomRight,
				TileLayer.TileDirection.BottomLeft, TileLayer.TileDirection.TopLeft,
			};

			private readonly List<OverlayObject> _ovls;
			private readonly byte _id;
			private readonly Drawable _drawable;

			public VeinField(List<OverlayObject> ovls, byte id, Drawable drawable) {
				_ovls = ovls;
				_id = id;
				_drawable = drawable;
			}

			private static OverlayObject Overlay(MapTile t) => t?.AllObjects.OfType<OverlayObject>().FirstOrDefault();
			private static TmpFile.TmpImage Image(MapTile t) => (t?.Drawable as TileDrawable)?.GetTileImage(t);
			private static int Ramp(MapTile t) => Image(t)?.RampType ?? 0;

			// IsometricTileTypeClass::Land_Type maps the tmp terrain byte to ice (1-4), rock (7, 8, 15),
			// water (9) and beach (10); CellClass::Can_Place_Veins refuses those four land types
			private static bool LandRefusesVeins(MapTile t) {
				int type = Image(t)?.TerrainType ?? 0;
				return type is >= 1 and <= 4 or 7 or 8 or 9 or 10 or 15;
			}

			private static bool IsVeinType(OverlayObject o) => o != null && o.Drawable.IsVeins;
			private bool IsPlain(OverlayObject o) => o != null && o.Drawable == _drawable;

			public bool CanPlaceVeins(MapTile t) {
				if (Ramp(t) > 4 || LandRefusesVeins(t)) return false;
				var own = Overlay(t);
				if (own != null && !own.Drawable.IsVeins) return false;
				foreach (var dir in Cardinal) {
					var n = t.Layer.GetNeighbourTile(t, dir);
					if (n == null) continue;
					var ovl = Overlay(n);
					if (Ramp(n) > 4 && Ramp(t) == 0 && !IsVeinType(ovl)) return false;
					if (LandRefusesVeins(n)) return false;
					if (ovl != null && !ovl.Drawable.IsVeins) return false;
				}
				return true;
			}

			public void PlaceVeins(MapTile t) {
				int ramp = Ramp(t);
				if (ramp != 0) {
					Set(t, FirstRamp + 2 * ramp + Roll2());
					return;
				}
				Set(t, FirstSolid + Roll3());
				foreach (var dir in Cardinal) {
					var n = t.Layer.GetNeighbourTile(t, dir);
					if (n == null) continue;
					var ovl = Overlay(n);
					if (IsVeinType(ovl) && (!IsPlain(ovl) || ovl.OverlayValue >= FirstSolid)) continue;
					int nRamp = Ramp(n);
					if (nRamp != 0) {
						Set(n, FirstRamp + 2 * nRamp + Roll2());
						continue;
					}
					int frame = VeinFrame(n);
					if (ovl == null || ovl.OverlayValue / 3 != frame)
						Set(n, 3 * frame + Roll3());
				}
			}

			// CellClass::Get_Vein_Frame: one bit per cardinal neighbour holding a solid or ramp piece
			// or a veinhole cell
			private int VeinFrame(MapTile t) {
				int frame = 0;
				for (int i = 0; i < Cardinal.Length; i++) {
					var ovl = Overlay(t.Layer.GetNeighbourTile(t, Cardinal[i]));
					if (IsPlain(ovl) ? ovl.OverlayValue >= FirstSolid : IsVeinType(ovl))
						frame |= 1 << i;
				}
				return frame;
			}

			private void Set(MapTile t, int value) {
				var ovl = Overlay(t);
				if (ovl == null) {
					ovl = new OverlayObject(_id, 0) { Drawable = _drawable };
					t.AddObject(ovl);
					_ovls.Add(ovl);
				}
				ovl.OverlayValue = (byte)value;
			}
		}

		/// <summary>Recalculates tile system. </summary>
		public static void FixTiles(TileLayer tiles, TileCollection collection) {
			Logger.Info("Recalculating tile LAT system");

			// change all CLAT tiles to their corresponding LAT tiles
			foreach (MapTile t in tiles) {
				// If this tile comes from a CLAT (connecting lat) set,
				// then replace it's set and tilenr by corresponding LAT sets'
				t.SetNum = collection.GetSetNum(t.TileNum);

				if (collection.IsCLAT(t.SetNum)) {
					t.SetNum = collection.GetLAT(t.SetNum);
					t.TileNum = collection.GetTileNumFromSet(t.SetNum);
					// the drawable was picked from the map's tile before this pass; a cell the
					// autolat below leaves plain would otherwise keep drawing the CLAT art
					t.Drawable = collection.GetDrawable(t);
				}
			}

			// apply autolat
			foreach (MapTile t in tiles) {
				// If this tile is a LAT tile, we might have to connect it
				if (collection.IsLAT(t.SetNum)) {
					// Which tile to use from CLAT tileset
					byte transitionTile = 0;
					MapTile tileTopRight = tiles.GetNeighbourTile(t, TileLayer.TileDirection.TopRight);
					MapTile tileBottomRight = tiles.GetNeighbourTile(t, TileLayer.TileDirection.BottomRight);
					MapTile tileBottomLeft = tiles.GetNeighbourTile(t, TileLayer.TileDirection.BottomLeft);
					MapTile tileTopLeft = tiles.GetNeighbourTile(t, TileLayer.TileDirection.TopLeft);

					// Find out setnums of adjacent cells
					if (tileTopRight != null && collection.ConnectTiles(t.SetNum, tileTopRight.SetNum))
						transitionTile += 1;

					if (tileBottomRight != null && collection.ConnectTiles(t.SetNum, tileBottomRight.SetNum))
						transitionTile += 2;

					if (tileBottomLeft != null && collection.ConnectTiles(t.SetNum, tileBottomLeft.SetNum))
						transitionTile += 4;

					if (tileTopLeft != null && collection.ConnectTiles(t.SetNum, tileTopLeft.SetNum))
						transitionTile += 8;

					// Crystal LAT tile connects to specific tiles in CrystalCliff
					if (collection.IsCrystalLAT(t.SetNum)) {
						if (tileTopRight != null && collection.IsCrystalCliff(tileTopRight.SetNum) &&
							tileTopRight.TileNum == collection.GetTileNumFromSet(tileTopRight.SetNum, 1))
							transitionTile = 0;
						if (tileBottomRight != null && collection.IsCrystalCliff(tileBottomRight.SetNum) &&
							tileBottomRight.TileNum == collection.GetTileNumFromSet(tileBottomRight.SetNum, 4))
							transitionTile = 0;
						if (tileBottomLeft != null && collection.IsCrystalCliff(tileBottomLeft.SetNum) &&
							tileBottomLeft.TileNum == collection.GetTileNumFromSet(tileBottomLeft.SetNum, 0))
							transitionTile = 0;
						if (tileTopLeft != null && collection.IsCrystalCliff(tileTopLeft.SetNum) &&
							tileTopLeft.TileNum == collection.GetTileNumFromSet(tileTopLeft.SetNum, 5))
							transitionTile = 0;
					}

					// Swamp has TilesInSet=9 instead of 1 for LAT tilesets
					// which doubles as a normal set for remaining tiles.
					if (collection.IsSwampLAT(t.SetNum) && t.TileNum > collection.GetTileNumFromSet(t.SetNum, 0))
						transitionTile = 0;

					if (transitionTile > 0) {
						// Find Tileset that contains the connecting pieces
						short clatSet = collection.GetCLATSet(t.SetNum);
						// Do not change this setnum, as then we could recognize it as
						// a different tileset for later tiles around this one.
						// (T->SetNum = clatSet;)
						t.TileNum = collection.GetTileNumFromSet(clatSet, transitionTile);
						t.Drawable = collection.GetDrawable(t);
					}
				}

				// apply ramp fixup (CellClass LAT recalc 0x47CA80): ramps 1-4 bordering flat
				// ground on a diagonal are replaced by the matching RampSmooth piece; a
				// smooth piece whose flat neighbours are gone reverts to the plain ramp
				else if (t.SetNum == collection.RampBase || t.SetNum == collection.RampSmooth) {
					var ti = t.GetTileImage();
					if (ti == null || ti.RampType < 1 || 4 < ti.RampType) continue;

					// an off-map neighbour counts as flat, like the game's blank cell
					bool FlatAt(TileLayer.TileDirection dir) {
						var n = tiles.GetNeighbourTile(t, dir);
						return (n?.GetTileImage()?.RampType ?? 0) == 0;
					}

					int fixup = -1;
					switch (ti.RampType) {
						case 1: // northwest facing
							if (FlatAt(TileLayer.TileDirection.TopLeft))
								fixup++;
							if (FlatAt(TileLayer.TileDirection.BottomRight))
								fixup += 2;
							break;

						case 2: // northeast facing
							if (FlatAt(TileLayer.TileDirection.TopRight))
								fixup++;
							if (FlatAt(TileLayer.TileDirection.BottomLeft))
								fixup += 2;
							break;

						case 3: // southeast facing
							if (FlatAt(TileLayer.TileDirection.BottomRight))
								fixup++;
							if (FlatAt(TileLayer.TileDirection.TopLeft))
								fixup += 2;
							break;

						case 4: // southwest facing
							if (FlatAt(TileLayer.TileDirection.BottomLeft))
								fixup++;
							if (FlatAt(TileLayer.TileDirection.TopRight))
								fixup += 2;
							break;
					}

					t.TileNum = fixup != -1
						? collection.GetTileNumFromSet(collection.RampSmooth, (byte)((ti.RampType - 1) * 3 + fixup))
						: collection.GetTileNumFromSet(collection.RampBase, (byte)(ti.RampType - 1));
					t.Drawable = collection.GetDrawable(t);
				}

			}


		}

	}
}
