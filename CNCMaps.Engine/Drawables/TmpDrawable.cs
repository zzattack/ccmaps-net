using System.Drawing;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;

namespace CNCMaps.Engine.Game {
	public class TileDrawable : Drawable {
		private readonly TileCollection.TileSetEntry tsEntry;

		public TileDrawable(IniFile.IniSection rules, IniFile.IniSection art, TileCollection.TileSetEntry entry)
			: base(rules, art) {
			tsEntry = entry;
			Name = entry.ToString();
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool shadows = true) {
			if (obj == null || tsEntry == null) return;

			var tmpFile = tsEntry.GetTmpFile((obj as MapTile).SubTile);
			if (tmpFile != null)
				TmpRenderer.Draw((MapTile)obj, tmpFile, ds);

			// todo: tile shadows
		}

		public override Rectangle GetBounds(GameObject obj) {
			var tile = (MapTile)obj;
			return TmpRenderer.GetBounds(tile, tsEntry.GetTmpFile(tile.SubTile));
		}

		public override void DrawBoundingBox(GameObject obj, Graphics gfx) {
			// meh
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