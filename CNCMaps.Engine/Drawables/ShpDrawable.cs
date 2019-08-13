using System;
using System.Drawing;
using System.Linq;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.Shared;

namespace CNCMaps.Engine.Game {
	class ShpDrawable : Drawable {
		private Random random;

		public ShpFile Shp { get; set; }

		public ShpDrawable(IniFile.IniSection rules, IniFile.IniSection art)
			: base(rules, art) {
			random = new Random();
		}

		public ShpDrawable(ShpFile shpFile) {
			Shp = shpFile;
			random = new Random();
		}

		public ShpDrawable(IniFile.IniSection rules, IniFile.IniSection art, ShpFile shpFile)
			: base(rules, art) {
			Shp = shpFile;
			random = new Random();
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool shadow = true) {
			if (InvisibleInGame || Shp == null) return;
			if (OwnerCollection != null && OwnerCollection.Type == CollectionType.Infantry) {
				int randomDir = -1;
				if (ModConfig.ActiveConfig.ExtraOptions.FirstOrDefault() != null && ModConfig.ActiveConfig.ExtraOptions.FirstOrDefault().EnableRandomInfantryFacing)
					randomDir = random.Next(256);
				Props.FrameDecider = FrameDeciders.InfantryFrameDecider(Ready_Start, Ready_Count, Ready_CountNext, randomDir);
			}
			if (Props.HasShadow && shadow)
				ShpRenderer.DrawShadow(obj, Shp, Props, ds);
			ShpRenderer.Draw(Shp, obj, this, Props, ds);
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
