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

		public ShpDrawable(ModConfig config, VirtualFileSystem vfs, IniFile.IniSection rules, IniFile.IniSection art, ShpFile shpFile = null)
			: base(config, vfs, rules, art) {
			_renderer =  new ShpRenderer(config, vfs);
			Shp = shpFile;
		}

		public ShpDrawable(ShpRenderer renderer, ShpFile shpFile) {
			_renderer = renderer;
			Shp = shpFile;
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool shadow = true) {
			if (InvisibleInGame || Shp == null) return;
			Size onBridgeOffset = Size.Empty;
			if (OwnerCollection != null && OwnerCollection.Type == CollectionType.Infantry) {
				int randomDir = -1;
				if (_config.ExtraOptions.FirstOrDefault() != null && _config.ExtraOptions.FirstOrDefault().EnableRandomInfantryFacing)
					randomDir = Rand.Next(256);
				Props.FrameDecider = FrameDeciders.InfantryFrameDecider(Ready_Start, Ready_Count, Ready_CountNext, randomDir);
				if (obj is OwnableObject && (obj as OwnableObject).OnBridge)
					onBridgeOffset = new Size(0, -4 * _config.TileHeight / 2);
			}

			Props.Offset += onBridgeOffset;
			if (Props.HasShadow && shadow && !Props.Cloakable)
				_renderer.DrawShadow(obj, Shp, Props, ds);
			_renderer.Draw(Shp, obj, this, Props, ds, Props.Cloakable ? 50 : 0);
			Props.Offset -= onBridgeOffset;

			if (IsVeinHoleMonster)
				DrawSurroundingVeins(obj, ds);
		}

		// The veinhole monster's image spans its 3x3 foundation of VEINHOLEDUMMY cells, and the
		// game draws those cells' fully grown veins on top of its dull background so the monster
		// connects to the surrounding vein field. Half of those cells are drawn before this
		// object, so repeat their veins here to guarantee they end up on top.
		private void DrawSurroundingVeins(GameObject obj, DrawingSurface ds) {
			if (obj.Tile?.Layer == null) return;
			foreach (TileLayer.TileDirection dir in System.Enum.GetValues(typeof(TileLayer.TileDirection))) {
				var neighbour = obj.Tile.Layer.GetNeighbourTile(obj.Tile, dir);
				if (neighbour == null) continue;
				foreach (var ovl in neighbour.AllObjects.OfType<OverlayObject>())
					if (ovl.Drawable != null && ovl.Drawable.IsVeins && !ovl.Drawable.IsVeinHoleMonster)
						ovl.Drawable.Draw(ovl, ds, false);
			}
		}

		public override void DrawShadow(GameObject obj, DrawingSurface ds) {
			if (InvisibleInGame || Shp == null) return;
			if (Props.HasShadow && !Props.Cloakable)
				_renderer.DrawShadow(obj, Shp, Props, ds);
		}

		public override Rectangle GetBounds(GameObject obj) {
			if (InvisibleInGame || Shp == null) return Rectangle.Empty;

			var bounds = _renderer.GetBounds(obj, Shp, Props);
			bounds.Offset(obj.Tile.Dx * _config.TileWidth / 2, (obj.Tile.Dy - obj.Tile.Z) * _config.TileHeight / 2);
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
