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

		/// <summary>Power-gated anim on a building that must be captured before it operates
		/// (e.g. a neutral oil derrick's pump): the game holds it at its start frame.</summary>
		public bool HoldAtStart;

		public AnimDrawable(ModConfig config, VirtualFileSystem vfs, IniFile.IniSection rules, IniFile.IniSection art, ShpFile shpFile = null)
			: base(config, vfs, rules, art, shpFile) {
		}

		public override void LoadFromRules() {
			base.LoadFromArtEssential();

			_animProps = new Animation(Name);
			_animProps.LoadArt(Art);

			_translucency = Art.ReadBool("Translucent") ? 50 : Art.ReadInt("Translucency", 0);

			Props.HasShadow = Art.ReadBool("Shadow", Defaults.GetShadowAssumption(CollectionType.Animation));

			if (FrameDeciders.AnimSimFrame < 0)
				Props.FrameDecider = FrameDeciders.LoopFrameDecider(
					Art.ReadInt("LoopStart"),
					Art.ReadInt("LoopEnd", 1));
			else if (HoldAtStart)
				Props.FrameDecider = obj => _animProps.Start;
			else
				Props.FrameDecider = FrameDeciders.AnimTickFrameDecider(_animProps, this);

			Flat = Art.ReadBool("DrawFlat", Defaults.GetFlatnessAssumption(OwnerCollection.Type))
				|| Art.ReadBool("Flat");

			if (!_animProps.ShouldUseCellDrawer)
				Props.PaletteType = PaletteType.Anim;
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool omitShadow = false) {
			if (Props.HasShadow && !omitShadow && !obj.Drawable.Props.Cloakable)
				_renderer.DrawShadow(obj, Shp, this, Props, ds);
			if (_translucency == 0)
				base.Draw(obj, ds, omitShadow);
			else if (!(obj.Drawable.Props.Cloakable && _translucency > 0)) {
				Logger.Debug("Drawing object {0} with {1}% translucency", obj, _translucency);
				_renderer.Draw(Shp, obj, this, Props, ds, _translucency);
			}
		}
	}
}
