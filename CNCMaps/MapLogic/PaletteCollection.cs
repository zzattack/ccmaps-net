using System.Collections;
using System.Collections.Generic;

namespace CNCMaps.MapLogic {
	struct PaletteCollection : IEnumerable<Palette> {
		public Palette isoPalette, libPalette, ovlPalette, unitPalette, animPalette;

		internal Palette GetPalette(PaletteType paletteType) {
			switch (paletteType) {
				case PaletteType.Anim: return animPalette;
				case PaletteType.Lib: return libPalette;
				case PaletteType.Overlay: return ovlPalette;
				case PaletteType.Unit: return unitPalette;
				case PaletteType.Iso:
				default:
					return isoPalette;
			}
		}

		public IEnumerator<Palette> GetEnumerator() {
			var p = new List<Palette>();
			p.Add(isoPalette);
			p.Add(libPalette);
			p.Add(ovlPalette);
			p.Add(unitPalette);
			p.Add(animPalette);
			return p.GetEnumerator();
		}

		IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}