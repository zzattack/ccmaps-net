using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CNCMaps.MapLogic {
	public class PaletteOwner { // may need to become an interface eventually
		public Palette Palette { get; set; }
		public PaletteType PaletteType { get; set; }
		public LightingType Lighting { get; set; }
	}
}
