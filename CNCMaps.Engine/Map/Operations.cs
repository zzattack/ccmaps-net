using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			foreach (OverlayObject o in ovls.Where(o => o.Drawable.IsVeins && !o.Drawable.IsVeinHoleMonster)) {
				int veins = 0, rnd = 0;

				// see if veins are positioned on ramp
				var tmpImg = (o.Tile.Drawable as TileDrawable).GetTileImage(o.Tile);
				if (tmpImg != null && tmpImg.RampType != 0) {
					if (tmpImg.RampType == 7) veins = 51;
					else if (tmpImg.RampType == 2) veins = 53;
					else if (tmpImg.RampType == 3) veins = 55;
					else if (tmpImg.RampType == 4) veins = 57;
					else {
						continue;
					}
					rnd = 2;
				}
				else {
					var ne = tiles.GetNeighbourTile(o.Tile, TileLayer.TileDirection.TopRight);
					var se = tiles.GetNeighbourTile(o.Tile, TileLayer.TileDirection.BottomRight);
					var sw = tiles.GetNeighbourTile(o.Tile, TileLayer.TileDirection.BottomLeft);
					var nw = tiles.GetNeighbourTile(o.Tile, TileLayer.TileDirection.TopLeft);

					if (ne != null && ne.AllObjects.OfType<OverlayObject>().Any(v => v.Drawable.IsVeins || v.Drawable.IsVeinHoleMonster))
						veins += 1;
					if (se != null && se.AllObjects.OfType<OverlayObject>().Any(v => v.Drawable.IsVeins || v.Drawable.IsVeinHoleMonster))
						veins += 2;
					if (sw != null && sw.AllObjects.OfType<OverlayObject>().Any(v => v.Drawable.IsVeins || v.Drawable.IsVeinHoleMonster))
						veins += 4;
					if (nw != null && nw.AllObjects.OfType<OverlayObject>().Any(v => v.Drawable.IsVeins || v.Drawable.IsVeinHoleMonster))
						veins += 8;

					rnd = 3;
				}
				o.OverlayValue = (byte)(veins * rnd + r.Next(rnd));
			}
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
