using System.Drawing;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;

namespace CNCMaps.Engine.Game {
	class AlphaDrawable : ShpDrawable {
		public AlphaDrawable(ShpRenderer renderer, ShpFile alphaShpFile) : base(renderer, alphaShpFile) {
			Props.Offset = new Point(0, 15);
			Props.FrameDecider = FrameDeciders.AlphaImageFrameDecider(Shp);
		}

		public AlphaDrawable(VFS vfs, IniFile.IniSection rules, IniFile.IniSection art, ShpFile alphaShpFile)
			: base(vfs, rules, art, alphaShpFile) {
			Props.Offset = new Point(0, 15);
			Props.FrameDecider = FrameDeciders.AlphaImageFrameDecider(Shp);
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool shadow = true) {
			if (!obj.Drawable.Props.Cloakable)
				_renderer.DrawAlpha(obj, Shp, Props, ds);
		}
	}
}
