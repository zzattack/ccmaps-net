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
				_renderer.DrawShadow(obj, ShadowShp(obj), this, Props, ds);
			_renderer.Draw(Shp, obj, this, Props, ds, Props.Cloakable ? 50 : 0);
			Props.Offset -= onBridgeOffset;
		}

		public override void DrawShadow(GameObject obj, DrawingSurface ds) {
			if (InvisibleInGame || Shp == null) return;
			if (Props.HasShadow && !Props.Cloakable)
				_renderer.DrawShadow(obj, ShadowShp(obj), this, Props, ds);
		}

		// CellClass::Draw_Overlay_Shadow reads OverlayTypes[Overlay] back directly, so tiberium
		// casts the shadow of the id the map stored even though its body comes from the pooled art.
		private ShpFile ShadowShp(GameObject obj) {
			var stored = (obj as OverlayObject)?.StoredDrawable as ShpDrawable;
			return stored?.Shp ?? Shp;
		}

		public override Rectangle GetBounds(GameObject obj) {
			if (InvisibleInGame || Shp == null) return Rectangle.Empty;

			var bounds = _renderer.GetBounds(obj, Shp, Props);
			bounds.Offset(obj.Tile.Dx * _config.TileWidth / 2, (obj.Tile.Dy - obj.Tile.Z) * _config.TileHeight / 2);
			bounds.Offset(Props.GetOffset(obj));
			return bounds;
		}

		/// <summary>Screen row of this shape's drawn bottom for the given object, or null
		/// when no drawable frame exists. Mirrors the offset math in ShpRenderer.Draw.</summary>
		public int? GetDrawnBottomY(GameObject obj) {
			if (Shp == null) return null;
			Shp.Initialize();
			int frameIndex = Props.FrameDecider(obj);
			if (obj.Drawable != null && obj.Drawable.IsActualWall)
				frameIndex = ((StructureObject)obj).WallBuildingFrame;
			if (frameIndex < 0 || frameIndex >= Shp.Images.Count) return null;
			var img = Shp.GetImage(frameIndex);
			if (img == null || img.Height == 0) return null;
			return Props.GetOffset(obj).Y + (obj.Tile.Dy - obj.Tile.Z) * _config.TileHeight / 2
				- Shp.Height / 2 + img.Y + img.Height - 1;
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
