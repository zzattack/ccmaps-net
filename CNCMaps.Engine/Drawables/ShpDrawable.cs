using System.Drawing;
using System.Linq;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;
using CNCMaps.Shared.Utility;

namespace CNCMaps.Engine.Drawables {
	class ShpDrawable : Drawable {

		public ShpFile Shp { get; set; }
		protected readonly ShpRenderer _renderer;

		public ShpDrawable(VirtualFileSystem vfs, IniFile.IniSection rules, IniFile.IniSection art, ShpFile shpFile = null)
			: base(vfs, rules, art) {
			_renderer =  new ShpRenderer(vfs);
			Shp = shpFile;
		}

		public ShpDrawable(ShpRenderer renderer, ShpFile shpFile) {
			_renderer = renderer;
			Shp = shpFile;
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool shadow = true) {
			if (InvisibleInGame || Shp == null) return;
			if (OwnerCollection != null && OwnerCollection.Type == CollectionType.Infantry) {
				int randomDir = -1;
				if (ModConfig.ActiveConfig.ExtraOptions.FirstOrDefault() != null && ModConfig.ActiveConfig.ExtraOptions.FirstOrDefault().EnableRandomInfantryFacing)
					randomDir = Rand.Next(256);
				Props.FrameDecider = FrameDeciders.InfantryFrameDecider(Ready_Start, Ready_Count, Ready_CountNext, randomDir);
			}
			if (Props.HasShadow && shadow && !Props.Cloakable)
				_renderer.DrawShadow(obj, Shp, Props, ds);
			_renderer.Draw(Shp, obj, this, Props, ds, Props.Cloakable ? 50 : 0);
		}

		public override void DrawShadow(GameObject obj, DrawingSurface ds) {
			if (InvisibleInGame || Shp == null) return;
			if (Props.HasShadow && !Props.Cloakable)
				_renderer.DrawShadow(obj, Shp, Props, ds);
		}

		public override Rectangle GetBounds(GameObject obj) {
			if (InvisibleInGame || Shp == null) return Rectangle.Empty;

			var bounds = _renderer.GetBounds(obj, Shp, Props);
			bounds.Offset(obj.Tile.Dx * TileWidth / 2, (obj.Tile.Dy - obj.Tile.Z) * TileHeight / 2);
			bounds.Offset(Props.GetOffset(obj));
			return bounds;
		}

		public string GetFilename() {
			string fn = Image;
			if (TheaterExtension)
				fn += ModConfig.ActiveTheater.Extension;
			else
				fn += ".shp";
			if (NewTheater)
				fn = OwnerCollection.ApplyNewTheaterIfNeeded(Art.Name, fn);
			return fn;

		}
	}
}
