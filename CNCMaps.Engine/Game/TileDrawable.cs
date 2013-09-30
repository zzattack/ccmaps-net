using CNCMaps.Engine.Game;
using CNCMaps.FileFormats;

namespace CNCMaps.Engine.Map {
	class TileDrawable : Drawable {
		public TileDrawable(IniFile.IniSection rules, IniFile.IniSection art)
			: base(rules, art) {
		}
	}
}
