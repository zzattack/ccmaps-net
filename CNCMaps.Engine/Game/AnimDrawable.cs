using CNCMaps.FileFormats;

namespace CNCMaps.Engine.Game {
	class AnimDrawable: ShpDrawable {
		public AnimDrawable(IniFile.IniSection rules, IniFile.IniSection art) : base(rules, art) {
		}
		public AnimDrawable(IniFile.IniSection rules, IniFile.IniSection art, ShpFile shp) : base(rules, art, shp) {
		}


	}
}
