using System.Net;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;

namespace CNCMaps.Engine.Game {
	class AnimDrawable : ShpDrawable {
		private int TransLucency;

		public AnimDrawable(IniFile.IniSection rules, IniFile.IniSection art)
			: base(rules, art) {
		}
		public AnimDrawable(IniFile.IniSection rules, IniFile.IniSection art, ShpFile shp)
			: base(rules, art, shp) {
		}

		public override void LoadFromRules() {
			base.LoadFromRulesEssential();
			// don't care for the rest..
			if (Art.ReadBool("Translucent"))
				TransLucency = 50;
			else
				TransLucency = Art.ReadInt("Translucency", 100);
		}

		public override void Draw(GameObject obj, DrawingSurface ds) {
			if (TransLucency == 100)
				base.Draw(obj, ds);
			else
				ShpRenderer.DrawTranslucent(obj, Shp, Props, ds, TransLucency);
		}

	}
}
