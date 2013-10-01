using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CNCMaps.Engine.Game;
using CNCMaps.Shared;
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
			Random r = new Random();
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

						if (neV  && ne.AllObjects.OfType<OverlayObject>().Any(thresholdCompare))
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
						o = new OverlayObject(anyVeins.OverlayID, (byte)r.Next(3));
						o.IsGeneratedVeins = true;
						o.Drawable = anyVeins.Drawable;
						o.Palette = anyVeins.Palette;
						o.TopTile = o.BottomTile = o.Tile;
						t.AddObject(o);
					}
					else {
						o.OverlayValue = (byte)(veins * mul + r.Next(rnd));
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
		public static void RecalculateTileSystem(TileLayer tiles, TileCollection collection) {
			Logger.Info("Recalculating tile LAT system");

			// change all CLAT tiles to their corresponding LAT tiles
			foreach (MapTile t in tiles) {
				if (t == null) continue;

				// If this tile comes from a CLAT (connecting lat) set,
				// then replace it's set and tilenr by corresponding LAT sets'
				t.SetNum = collection.GetSetNum(t.TileNum);

				if (collection.IsCLAT(t.SetNum)) {
					t.SetNum = collection.GetLAT(t.SetNum);
					t.TileNum = collection.GetTileNumFromSet(t.SetNum);
				}
			}

			foreach (MapTile t in tiles) {
				if (t == null) continue;

				// If this tile is a LAT tile, we might have to connect it
				if (collection.IsLAT(t.SetNum)) {
					// Which tile to use from CLAT tileset
					byte transitionTile = 0;

					// Find out setnums of adjacent cells

					MapTile tileTopRight = tiles.GetNeighbourTile(t, TileLayer.TileDirection.TopRight);
					if (tileTopRight != null && collection.ConnectTiles(t.SetNum, tileTopRight.SetNum))
						transitionTile += 1;

					MapTile tileBottomRight = tiles.GetNeighbourTile(t, TileLayer.TileDirection.BottomRight);
					if (tileBottomRight != null && collection.ConnectTiles(t.SetNum, tileBottomRight.SetNum))
						transitionTile += 2;

					MapTile tileBottomLeft = tiles.GetNeighbourTile(t, TileLayer.TileDirection.BottomLeft);
					if (tileBottomLeft != null && collection.ConnectTiles(t.SetNum, tileBottomLeft.SetNum))
						transitionTile += 4;

					MapTile tileTopLeft = tiles.GetNeighbourTile(t, TileLayer.TileDirection.TopLeft);
					if (tileTopLeft != null && collection.ConnectTiles(t.SetNum, tileTopLeft.SetNum))
						transitionTile += 8;

					if (transitionTile > 0) {
						// Find Tileset that contains the connecting pieces
						short clatSet =collection.GetCLATSet(t.SetNum);
						// Do not change this setnum, as then we could recognize it as
						// a different tileset for later tiles around this one.
						// (T->SetNum = clatSet;)
						t.TileNum = collection.GetTileNumFromSet(clatSet, transitionTile);
					}
				}

			}
		}

	}
}
