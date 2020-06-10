using CNCMaps.Engine.Game;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.Engine.Types;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;
using NLog;

namespace CNCMaps.Engine.Drawables {
	class AnimDrawable : ShpDrawable {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private Animation _animProps;
		private int _translucency;

		public AnimDrawable(ModConfig config, VirtualFileSystem vfs, IniFile.IniSection rules, IniFile.IniSection art, ShpFile shpFile = null)
			: base(config, vfs, rules, art, shpFile) {
		}

		public override void LoadFromRules() {
			base.LoadFromArtEssential();

			_animProps = new Animation(Name);
			_animProps.LoadArt(Art);

			_translucency = Art.ReadBool("Translucent") ? 50 : Art.ReadInt("Translucency", 0);

			Props.HasShadow = Art.ReadBool("Shadow", Defaults.GetShadowAssumption(CollectionType.Animation));

			Props.FrameDecider = FrameDeciders.LoopFrameDecider(
				Art.ReadInt("LoopStart"),
				Art.ReadInt("LoopEnd", 1));

			Flat = Art.ReadBool("DrawFlat", Defaults.GetFlatnessAssumption(OwnerCollection.Type))
				|| Art.ReadBool("Flat");

			if (!_animProps.ShouldUseCellDrawer)
				Props.PaletteType = PaletteType.Anim;
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
