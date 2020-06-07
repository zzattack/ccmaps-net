using CNCMaps.Engine.Game;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;
using NLog;

namespace CNCMaps.Engine.Drawables {
	class AnimDrawable : ShpDrawable {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private int _translucency;

		public AnimDrawable(VirtualFileSystem vfs, IniFile.IniSection rules, IniFile.IniSection art)
			: base(vfs, rules, art) {
		}
		public AnimDrawable(VirtualFileSystem vfs, IniFile.IniSection rules, IniFile.IniSection art, ShpFile shp)
			: base(vfs, rules, art, shp) {
		}

		public override void LoadFromRules() {
			base.LoadFromArtEssential();

			_translucency = Art.ReadBool("Translucent") ? 50 : Art.ReadInt("Translucency", 0);

			Props.HasShadow = Art.ReadBool("Shadow", Defaults.GetShadowAssumption(CollectionType.Animation));

			Props.FrameDecider = FrameDeciders.LoopFrameDecider(
				Art.ReadInt("LoopStart"),
				Art.ReadInt("LoopEnd", 1));

			Flat = Art.ReadBool("DrawFlat", Defaults.GetFlatnessAssumption(OwnerCollection.Type))
				|| Art.ReadBool("Flat");
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool omitShadow = false) {
			if (Props.HasShadow && !omitShadow && !obj.Drawable.Props.Cloakable)
				_renderer.DrawShadow(obj, Shp, Props, ds);
			if (_translucency == 0)
				base.Draw(obj, ds, omitShadow);
			else if (!(obj.Drawable.Props.Cloakable && _translucency > 0)) {
				Logger.Debug("Drawing object {0} with {1}% translucency", obj, _translucency);
				_renderer.Draw(Shp, obj, this, Props, ds, _translucency);
			}
		}
	}
}
