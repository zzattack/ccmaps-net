using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.Shared;

namespace CNCMaps.Engine.Game {
	class ShpDrawable : Drawable {
		public ShpFile Shp { get; set; }

		public ShpDrawable(IniFile.IniSection rules, IniFile.IniSection art) : base(rules, art) {
		}

		public ShpDrawable(IniFile.IniSection rules, IniFile.IniSection art, ShpFile shpFile) : base(rules, art) {
			// TODO: Complete member initialization
			this.Shp = shpFile;
		}

		public override void Draw(GameObject obj, DrawingSurface ds) {
			ShpDrawer.Draw(obj, Shp, Props, ds, GlobalOffset);
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
