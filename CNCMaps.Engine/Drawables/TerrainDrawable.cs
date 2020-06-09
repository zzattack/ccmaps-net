using System.Drawing;
using System.Linq;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;

namespace CNCMaps.Engine.Drawables {
	internal class TerrainDrawable : Drawable {

		private ShpDrawable terrainShp;

		public TerrainDrawable(ModConfig config, VirtualFileSystem vfs, IniFile.IniSection rules, IniFile.IniSection art)
			: base(config, vfs, rules, art) { }

		public override void Draw(GameObject obj, DrawingSurface ds, bool shadows = true) {
			terrainShp = new ShpDrawable(_config, _vfs, Rules, Art);
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
			var renderer = new ShpRenderer(_config, _vfs);
			var bounds = renderer.GetBounds(obj, terrainShp.Shp, Props);
			bounds.Offset(obj.Tile.Dx * _config.TileWidth / 2, (obj.Tile.Dy - obj.Tile.Z) * _config.TileHeight / 2);
			bounds.Offset(Props.GetOffset(obj));
			return bounds;
		}
	}
}
