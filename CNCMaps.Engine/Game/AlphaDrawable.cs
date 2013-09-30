using System.Drawing;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;

namespace CNCMaps.Engine.Game {
	class AlphaDrawable  : ShpDrawable {

		public AlphaDrawable(IniFile.IniSection rules, IniFile.IniSection art, ShpFile alphaShpFile)
			: base(rules, art, alphaShpFile) {
			Shp = alphaShpFile;

			Props.Offset = new Point(0, 15);
			Props.FrameDecider = FrameDeciders.AlphaImageFrameDecider(Shp);
		}

		public override void Draw(GameObject obj, DrawingSurface ds) {
			ShpDrawer.DrawAlpha(obj, Shp, Props, ds);
		}
	}
}
