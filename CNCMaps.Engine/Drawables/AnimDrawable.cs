using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.Shared;
using NLog;

namespace CNCMaps.Engine.Game {
	class AnimDrawable : ShpDrawable {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private int TransLucency;

		public AnimDrawable(IniFile.IniSection rules, IniFile.IniSection art)
			: base(rules, art) {
		}
		public AnimDrawable(IniFile.IniSection rules, IniFile.IniSection art, ShpFile shp)
			: base(rules, art, shp) {
		}

		public override void LoadFromRules() {
			base.LoadFromArtEssential();

			TransLucency = Art.ReadBool("Translucent") ? 50 : Art.ReadInt("Translucency", 0);

			Props.HasShadow = Art.ReadBool("Shadow", Defaults.GetShadowAssumption(CollectionType.Animation));

			Props.FrameDecider = FrameDeciders.LoopFrameDecider(
				Art.ReadInt("LoopStart"),
				Art.ReadInt("LoopEnd", 1));

			Flat = Art.ReadBool("DrawFlat", Defaults.GetFlatnessAssumption(OwnerCollection.Type))
				|| Art.ReadBool("Flat");
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool omitShadow = false) {
			if (Props.HasShadow && !omitShadow && !obj.Drawable.Props.Cloakable)
				ShpRenderer.DrawShadow(obj, Shp, Props, ds);
			if (TransLucency == 0)
				base.Draw(obj, ds, omitShadow);
			else if (!(obj.Drawable.Props.Cloakable && TransLucency > 0)) {
				Logger.Debug("Drawing object {0} with {1}% translucency", obj, TransLucency);
				ShpRenderer.Draw(Shp, obj, this, Props, ds, TransLucency);
			}
		}
	}
}
