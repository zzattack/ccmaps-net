using System.Drawing;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;

namespace CNCMaps.Engine.Drawables {
	class AlphaDrawable : ShpDrawable {
		public AlphaDrawable(ShpRenderer renderer, ShpFile alphaShpFile) : base(renderer, alphaShpFile) {
			Props.Offset = new Point(0, 15);
			Props.FrameDecider = FrameDeciders.AlphaImageFrameDecider(Shp);
		}

		public AlphaDrawable(ModConfig config, VirtualFileSystem vfs, IniFile.IniSection rules, IniFile.IniSection art, ShpFile alphaShpFile)
			: base(config, vfs, rules, art, alphaShpFile) {
			Props.Offset = new Point(0, 15);
			Props.FrameDecider = FrameDeciders.AlphaImageFrameDecider(Shp);
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool shadow = true) {
			if (!obj.Drawable.Props.Cloakable)
				_renderer.DrawAlpha(obj, Shp, Props, ds);
		}
	}
}
