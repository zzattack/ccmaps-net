using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Game;
using CNCMaps.Shared;
using CNCMaps.Shared.Utility;
using NLog;

namespace CNCMaps.Engine.Map {
	class Operations {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static void RecalculateOreSpread(IEnumerable<OverlayObject> ovls, EngineType engine) {
			Logger.Info("Redistributing ore-spread over patches");

			foreach (OverlayObject o in ovls) {
				// The value consists of the sum of all dx's with a little magic offsets
				// plus the sum of all dy's with also a little magic offset, and also
				// everything is calculated modulo 12
				var type = SpecialOverlays.GetOverlayTibType(o, engine);

				if (type == OverlayTibType.Ore) {
					int x = o.Tile.Dx;
					int y = o.Tile.Dy;
					double yInc = ((((y - 9) / 2) % 12) * (((y - 8) / 2) % 12)) % 12;
					double xInc = ((((x - 13) / 2) % 12) * (((x - 12) / 2) % 12)) % 12;

					// x_inc may be > y_inc so adding a big number outside of cell bounds
					// will surely keep num positive
					var num = (int)(yInc - xInc + 120000);
					num %= 12;

					if (engine <= EngineType.RedAlert2)
						o.OverlayID = (byte)(SpecialOverlays.Ra2MinIdRiparius + num);
					else
						o.OverlayID = (byte)(SpecialOverlays.TsMinIdRiparius + num);
				}

				else if (type == OverlayTibType.Gems) {
					int x = o.Tile.Dx;
					int y = o.Tile.Dy;
					double yInc = ((((y - 9) / 2) % 12) * (((y - 8) / 2) % 12)) % 12;
					double xInc = ((((x - 13) / 2) % 12) * (((x - 12) / 2) % 12)) % 12;

					// x_inc may be > y_inc so adding a big number outside of cell bounds
					// will surely keep num positive
					var num = (int)(yInc - xInc + 120000);
					num %= 12;

					// replace gems
					if (engine <= EngineType.RedAlert2)
						o.OverlayID = (byte)(SpecialOverlays.Ra2MinIdCruentus + num);
					else
						o.OverlayID = (byte)(SpecialOverlays.TsMinIdCruentus + num);
				}

				else if (type == OverlayTibType.Vinifera) {
					int x = o.Tile.Dx;
					int y = o.Tile.Dy;
					double yInc = ((((y - 9) / 2) % 12) * (((y - 8) / 2) % 12)) % 12;
					double xInc = ((((x - 13) / 2) % 12) * (((x - 12) / 2) % 12)) % 12;

					// x_inc may be > y_inc so adding a big number outside of cell bounds
					// will surely keep num positive
					var num = (int)(yInc - xInc + 120000);
					num %= 12;

					// replace gems
					if (engine <= EngineType.RedAlert2)
						o.OverlayID = (byte)(SpecialOverlays.Ra2MinIdVinifera + num);
					else
						o.OverlayID = (byte)(SpecialOverlays.TsMinIdVinifera + num);
				}

				else if (type == OverlayTibType.Aboreus) {
					int x = o.Tile.Dx;
					int y = o.Tile.Dy;
					double yInc = ((((y - 9) / 2) % 12) * (((y - 8) / 2) % 12)) % 12;
					double xInc = ((((x - 13) / 2) % 12) * (((x - 12) / 2) % 12)) % 12;

					// x_inc may be > y_inc so adding a big number outside of cell bounds
					// will surely keep num positive
					var num = (int)(yInc - xInc + 120000);
					num %= 12;

					// replace gems
					if (engine <= EngineType.RedAlert2)
						o.OverlayID = (byte)(SpecialOverlays.Ra2MinIdAboreus + num);
					else
						o.OverlayID = (byte)(SpecialOverlays.TsMinIdAboreus + num);
				}

			}
		}

		public static void RecalculateVeinsSpread(IEnumerable<OverlayObject> ovls, TileLayer tiles) {
			OverlayObject anyVeins = null;

			foreach (var o in ovls) {
				if (IsVeins(o) && !o.Drawable.IsVeinHoleMonster && o.OverlayValue / 3 == 15)
					o.IsGeneratedVeins = true;
			}

			foreach (var t in tiles) {
				var o = t.AllObjects.OfType<OverlayObject>().FirstOrDefault();

				int veins = 0, rnd = 0, mul = 1;
				bool amIVeins = IsVeins(o);

				if (amIVeins && !o.Drawable.IsVeinHoleMonster) {
					// see if veins are positioned on ramp
					anyVeins = o;
					var tmpImg = (o.Tile.Drawable as TileDrawable).GetTileImage(o.Tile);
					if (tmpImg != null && tmpImg.RampType != 0) {
						if (tmpImg.RampType == 7) veins = 51;
						else if (tmpImg.RampType == 2) veins = 55;
						else if (tmpImg.RampType == 3) veins = 57;
						else if (tmpImg.RampType == 4) veins = 59;
						else {
							continue;
						}
						rnd = 2;
						mul = 1;
					}
					else {
						var ne = t.Layer.GetNeighbourTile(t, TileLayer.TileDirection.TopRight);
						var se = t.Layer.GetNeighbourTile(t, TileLayer.TileDirection.BottomRight);
						var sw = t.Layer.GetNeighbourTile(t, TileLayer.TileDirection.BottomLeft);
						var nw = t.Layer.GetNeighbourTile(t, TileLayer.TileDirection.TopLeft);

						bool neV = ne != null && ne.AllObjects.OfType<OverlayObject>().Any(IsVeins);
						bool seV = se != null && se.AllObjects.OfType<OverlayObject>().Any(IsVeins);
						bool swV = sw != null && sw.AllObjects.OfType<OverlayObject>().Any(IsVeins);
						bool nwV = nw != null && nw.AllObjects.OfType<OverlayObject>().Any(IsVeins);

						int numNeighbours = CountNeighbouringVeins(ne, se, sw, nw, IsVeins);
						int threshold = numNeighbours != 4 ? 4 : 0;
						var compare = numNeighbours == 4 ? (Func<OverlayObject, bool>)IsFullVeins : IsVeins;
						Func<OverlayObject, bool> thresholdCompare = ov => threshold <= CountNeighbouringVeins(ov.Tile, compare);

						if (neV && ne.AllObjects.OfType<OverlayObject>().Any(thresholdCompare))
							veins += 1;

						if (seV && se.AllObjects.OfType<OverlayObject>().Any(thresholdCompare))
							veins += 2;

						if (swV && sw.AllObjects.OfType<OverlayObject>().Any(thresholdCompare))
							veins += 4;

						if (nwV && nw.AllObjects.OfType<OverlayObject>().Any(thresholdCompare))
							veins += 8;

						if (veins == 15 && !o.IsGeneratedVeins)
							veins++;

						mul = 3;
						rnd = 3;
					}
				}

				if (veins != 0 || amIVeins) {
					if (o == null) {
						// on the fly veins creation..
						o = new OverlayObject(anyVeins.OverlayID, (byte)Rand.Next(3));
						o.IsGeneratedVeins = true;
						o.Drawable = anyVeins.Drawable;
						o.Palette = anyVeins.Palette;
						o.TopTile = o.BottomTile = o.Tile;
						t.AddObject(o);
					}
					else {
						o.OverlayValue = (byte)(veins * mul + Rand.Next(rnd));
						Debug.WriteLine("Replacing veins with value {0} ({1})", o.OverlayValue, veins);
					}
				}


			}
		}

		public static bool IsVeins(OverlayObject o) {
			return o != null && o.Drawable.IsVeins;
		}
		public static bool IsFullVeins(OverlayObject o) {
			return o != null && !o.IsGeneratedVeins && o.Drawable.IsVeins && (o.Drawable.IsVeinHoleMonster || o.OverlayValue / 3 == 16);
		}

		public static int CountNeighbouringVeins(MapTile t, Func<OverlayObject, bool> test) {
			var ne = t.Layer.GetNeighbourTile(t, TileLayer.TileDirection.TopRight);
			var se = t.Layer.GetNeighbourTile(t, TileLayer.TileDirection.BottomRight);
			var sw = t.Layer.GetNeighbourTile(t, TileLayer.TileDirection.BottomLeft);
			var nw = t.Layer.GetNeighbourTile(t, TileLayer.TileDirection.TopLeft);
			return CountNeighbouringVeins(ne, se, sw, nw, test);
		}

		private static int CountNeighbouringVeins(MapTile ne, MapTile se, MapTile sw, MapTile nw, Func<OverlayObject, bool> test) {
			bool neV = ne != null && ne.AllObjects.OfType<OverlayObject>().Any(test);
			bool seV = se != null && se.AllObjects.OfType<OverlayObject>().Any(test);
			bool swV = sw != null && sw.AllObjects.OfType<OverlayObject>().Any(test);
			bool nwV = nw != null && nw.AllObjects.OfType<OverlayObject>().Any(test);
			int numNeighbours =
							(neV ? 1 : 0) + (seV ? 1 : 0) + (swV ? 1 : 0) + (nwV ? 1 : 0);
			return numNeighbours;
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

				// apply ramp fixup
				else if (t.SetNum == collection.RampBase) {
					var ti = t.GetTileImage();
					if (ti.RampType < 1 || 4 < ti.TerrainType) continue;

					int fixup = -1;
					MapTile tileTopRight = tiles.GetNeighbourTile(t, TileLayer.TileDirection.TopRight);
					MapTile tileBottomRight = tiles.GetNeighbourTile(t, TileLayer.TileDirection.BottomRight);
					MapTile tileBottomLeft = tiles.GetNeighbourTile(t, TileLayer.TileDirection.BottomLeft);
					MapTile tileTopLeft = tiles.GetNeighbourTile(t, TileLayer.TileDirection.TopLeft);


					switch (ti.RampType) {
						case 1:
							// northwest facing
							if (tileTopLeft != null && tileTopLeft.GetTileImage().RampType == 0)
								fixup++;
							if (tileBottomRight != null && tileBottomRight.GetTileImage().RampType == 0)
								fixup += 2;
							break;

						case 2: // northeast facing
							if (tileTopRight != null && tileTopRight.GetTileImage().RampType == 0)
								fixup++;
							if (tileBottomLeft != null && tileBottomLeft.GetTileImage().RampType == 0)
								fixup += 2;
							break;

						case 3: // southeast facing
							if (tileBottomRight != null && tileBottomRight.GetTileImage().RampType == 0)
								fixup++;
							if (tileTopLeft != null && tileTopLeft.GetTileImage().RampType == 0)
								fixup += 2;
							break;

						case 4: // southwest facing
							if (tileBottomLeft != null && tileBottomLeft.GetTileImage().RampType == 0)
								fixup++;
							if (tileTopRight != null && tileTopRight.GetTileImage().RampType == 0)
								fixup += 2;
							break;
					}

					if (fixup != -1) {
						t.TileNum = collection.GetTileNumFromSet(collection.RampSmooth, (byte)((ti.RampType - 1) * 3 + fixup));
						// update drawable too
						t.Drawable = collection.GetDrawable(t);
					}
				}

			}


		}

	}
}
