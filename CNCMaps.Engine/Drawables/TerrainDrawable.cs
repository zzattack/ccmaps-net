using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;

namespace CNCMaps.Engine.Game {
	internal class TerrainDrawable : Drawable {

		private ShpDrawable terrainShp;

		public TerrainDrawable(VFS vfs, IniFile.IniSection rules, IniFile.IniSection art)
			: base(vfs, rules, art) { }

		public override void Draw(GameObject obj, DrawingSurface ds, bool shadows = true) {
            terrainShp = new ShpDrawable(_vfs, Rules, Art);
			terrainShp.OwnerCollection = OwnerCollection;
			terrainShp.LoadFromArtEssential();
			terrainShp.Props = Props;
			terrainShp.Shp = _vfs.Open<ShpFile>(terrainShp.GetFilename());

			foreach (var sub in SubDrawables.OfType<AlphaDrawable>()) {
				sub.Draw(obj, ds, false);
			}

			if (shadows)
				terrainShp.DrawShadow(obj, ds);
			terrainShp.Draw(obj, ds, false);
		}

		public override Rectangle GetBounds(GameObject obj) {
			if (InvisibleInGame || terrainShp?.Shp == null) return Rectangle.Empty;
			var renderer = new ShpRenderer(_vfs);
			var bounds = renderer.GetBounds(obj, terrainShp.Shp, Props);
			bounds.Offset(obj.Tile.Dx * TileWidth / 2, (obj.Tile.Dy - obj.Tile.Z) * TileHeight / 2);
			bounds.Offset(Props.GetOffset(obj));
			return bounds;
		}
	}
}