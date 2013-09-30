using CNCMaps.FileFormats.FileFormats;

namespace CNCMaps.Engine.Game {
	class AnimDrawable: ShpDrawable {
		public AnimDrawable(IniFile.IniSection rules, IniFile.IniSection art) : base(rules, art) {
		}
		public AnimDrawable(IniFile.IniSection rules, IniFile.IniSection art, ShpFile shp) : base(rules, art, shp) {
		}

		public override void LoadFromRules() {
			base.LoadFromRulesEssential();
			// don't care for the rest..
		}

	}
}
