using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;

namespace CNCMaps.Engine.Game {
	public class TmpDrawable : Drawable {
		private readonly TileCollection.TileSetEntry tsEntry;

		public TmpDrawable(IniFile.IniSection rules, IniFile.IniSection art, TileCollection.TileSetEntry entry)
			: base(rules, art) {
			tsEntry = entry;
			Name = entry.ToString();
		}

		public override void Draw(GameObject obj, DrawingSurface ds) {
			if (obj == null || tsEntry == null) return;

			var tmpFile = tsEntry.GetTmpFile((obj as MapTile).SubTile);
			if (tmpFile != null)
				TmpDrawer.Draw((MapTile)obj, tmpFile, ds);
		}

		public override System.Drawing.Rectangle GetBounds(GameObject obj) {
			var tile = (MapTile)obj;
			return TmpDrawer.GetBounds(tile, tsEntry.GetTmpFile(tile.SubTile));
		}

		public TileCollection.TileSetEntry GetTileSetEntry() {
			return tsEntry;
		}

		public TmpFile GetTileFile(MapTile t) {
			return tsEntry.GetTmpFile(t.SubTile);
		}

		public TmpFile.TmpImage GetTileImage(MapTile t) {
			var tmp = tsEntry.GetTmpFile(t.SubTile);
			if (tmp.Images.Count > t.SubTile) return tmp.Images[t.SubTile];
			return null;
		}

	}
}