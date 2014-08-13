using System.Drawing;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;

namespace CNCMaps.Engine.Game {
	public class TileDrawable : Drawable {
		public TileCollection.TileSetEntry TsEntry;

		public TileDrawable(IniFile.IniSection rules, IniFile.IniSection art, TileCollection.TileSetEntry entry)
			: base(rules, art) {
			TsEntry = entry;
			if (entry != null)
				Name = entry.ToString();
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool shadows = true) {
			if (obj == null || TsEntry == null) return;

			var tmpFile = TsEntry.GetTmpFile(obj as MapTile);
			if (tmpFile != null)
				TmpRenderer.Draw((MapTile)obj, tmpFile, ds);

			// todo: tile shadows
		}

		public override Rectangle GetBounds(GameObject obj) {
			var tile = (MapTile)obj;
			return TmpRenderer.GetBounds(tile, TsEntry.GetTmpFile(tile));
		}

		public override void DrawBoundingBox(GameObject obj, Graphics gfx) {
			// meh
		}

		public TileCollection.TileSetEntry GetTileSetEntry() {
			return TsEntry;
		}

		public TmpFile GetTileFile(MapTile t) {
			return TsEntry.GetTmpFile(t);
		}

		public TmpFile.TmpImage GetTileImage(MapTile t) {
			var tmp = TsEntry.GetTmpFile(t);
			if (tmp.Images.Count > t.SubTile) return tmp.Images[t.SubTile];
			return tmp.Images.Count > 0 ? tmp.Images[0] : null;
		}

	}
}