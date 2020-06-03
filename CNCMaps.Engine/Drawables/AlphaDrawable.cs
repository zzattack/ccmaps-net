using System.Drawing;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;

namespace CNCMaps.Engine.Game {
	class AlphaDrawable  : ShpDrawable {
		public AlphaDrawable(ShpFile alphaShpFile) : base(alphaShpFile) {
			Props.Offset = new Point(0, 15);
			Props.FrameDecider = FrameDeciders.AlphaImageFrameDecider(Shp);
		}

		public AlphaDrawable(IniFile.IniSection rules, IniFile.IniSection art, ShpFile alphaShpFile)
			: base(rules, art, alphaShpFile) {
			Props.Offset = new Point(0, 15);
			Props.FrameDecider = FrameDeciders.AlphaImageFrameDecider(Shp);
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool shadow = true) {
            if (!obj.Drawable.Props.Cloakable)
                ShpRenderer.DrawAlpha(obj, Shp, Props, ds);
		}
	}
}
