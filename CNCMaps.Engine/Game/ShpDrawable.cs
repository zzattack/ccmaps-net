using System.Drawing;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats.FileFormats;
using CNCMaps.Shared;

namespace CNCMaps.Engine.Game {
	class ShpDrawable : Drawable {
		public ShpFile Shp { get; set; }

		public ShpDrawable(IniFile.IniSection rules, IniFile.IniSection art)
			: base(rules, art) {
		}

		public ShpDrawable(ShpFile shpFile) {
			this.Shp = shpFile;
		}

		public ShpDrawable(IniFile.IniSection rules, IniFile.IniSection art, ShpFile shpFile)
			: base(rules, art) {
			this.Shp = shpFile;
		}

		public override void Draw(GameObject obj, DrawingSurface ds) {
			if (InvisibleInGame || Shp == null) return;
			ShpDrawer.Draw(obj, Shp, Props, ds);
			if (Props.HasShadow)
				ShpDrawer.DrawShadow(obj, Shp, Props, ds);
		}

		public override Rectangle GetBounds(GameObject obj) {
			if (InvisibleInGame || Shp == null) return Rectangle.Empty;

			var bounds = ShpDrawer.GetBounds(obj, Shp, Props);
			bounds.Offset(obj.Tile.Dx * TileWidth / 2, (obj.Tile.Dy - obj.Tile.Z) * TileHeight / 2);
			bounds.Offset(Props.GetOffset(obj));
			return bounds;
		}

		public string GetFilename() {
			string fn = Image;
			if (TheaterExtension)
				fn += ModConfig.ActiveTheater.Extension;
			else
				fn+= ".shp";
			if (NewTheater)
				fn = OwnerCollection.ApplyNewTheaterIfNeeded(Art.Name, fn);
			return fn;

		}
	}
}
