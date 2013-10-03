using System.Drawing;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.Shared;

namespace CNCMaps.Engine.Game {
	class ShpDrawable : Drawable {
		public ShpFile Shp { get; set; }

		public ShpDrawable(IniFile.IniSection rules, IniFile.IniSection art)
			: base(rules, art) {
		}

		public ShpDrawable(ShpFile shpFile) {
			Shp = shpFile;
		}

		public ShpDrawable(IniFile.IniSection rules, IniFile.IniSection art, ShpFile shpFile)
			: base(rules, art) {
			Shp = shpFile;
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool shadow = true) {
			if (InvisibleInGame || Shp == null) return;
			ShpRenderer.Draw(obj, Shp, Props, ds);
			if (Props.HasShadow && shadow)
				ShpRenderer.DrawShadow(obj, Shp, Props, ds);
		}

		public override void DrawShadow(GameObject obj, DrawingSurface ds) {
			if (InvisibleInGame || Shp == null) return;
			if (Props.HasShadow)
				ShpRenderer.DrawShadow(obj, Shp, Props, ds);
		}

		public override Rectangle GetBounds(GameObject obj) {
			if (InvisibleInGame || Shp == null) return Rectangle.Empty;

			var bounds = ShpRenderer.GetBounds(obj, Shp, Props);
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
