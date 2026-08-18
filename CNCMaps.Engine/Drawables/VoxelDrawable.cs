using System.Drawing;
using System.IO;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;

namespace CNCMaps.Engine.Drawables {
	class VoxelDrawable : Drawable {
		internal static readonly VxlRenderer VoxelRenderer = new VxlRenderer();
		public VxlFile Vxl;
		public HvaFile Hva;

		public VoxelDrawable(ModConfig config, VirtualFileSystem vfs, IniFile.IniSection rules, IniFile.IniSection art) : base(config, vfs, rules, art) { }
		public VoxelDrawable(ModConfig config, VxlFile vxl, HvaFile hva) : base(config, null, null, null) {
			Vxl = vxl;
			Hva = hva;
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool shadows = true) {
			if (Vxl == null || Hva == Stream.Null) return;
			DrawingSurface vxl_ds = VoxelRenderer.Render(Vxl, Hva, obj, Props);
			if (vxl_ds != null)
				BlitVoxelToSurface(ds, vxl_ds, obj, Props, Props.Cloakable ? 50 : 0);
		}

		public override Rectangle GetBounds(GameObject obj) {
			if (Vxl == null || Hva == null) return Rectangle.Empty;
			var bounds = VxlRenderer.GetBounds(obj, Vxl, Hva, Props);
			bounds.Offset(obj.Tile.Dx * _config.TileWidth / 2, (obj.Tile.Dy - obj.Tile.Z) * _config.TileHeight / 2);
			bounds.Offset(Props.GetOffset(obj));
			if (Props.FlightHeight > 0) // raised body plus grounded shadow
				bounds = Rectangle.Union(bounds, new Rectangle(bounds.X, bounds.Y - Props.FlightHeight, bounds.Width, bounds.Height));
			return bounds;
		}

		private unsafe void BlitVoxelToSurface(DrawingSurface ds, DrawingSurface vxl_ds, GameObject obj, DrawProperties props, int transLucency = 0) {
			Point d = new Point(obj.Tile.Dx * _config.TileWidth / 2, (obj.Tile.Dy - obj.Tile.Z) * _config.TileHeight / 2);
			d.Offset(props.GetOffset(obj));
			d.Offset(-vxl_ds.BitmapData.Width / 2, -vxl_ds.BitmapData.Height / 2);

			// rows inverted!
			var w_low = (byte*)ds.BitmapData.Scan0;
			byte* w_high = w_low + ds.BitmapData.Stride * ds.BitmapData.Height;
			var zBuffer = ds.GetZBuffer();
			var heightBuffer = ds.GetHeightBuffer();
			var shadowBufVxl = vxl_ds.GetShadows();
			var shadowBuf = ds.GetShadows();
			// int rowsTouched = 0;

			// the drawn sprite's vertical extent, standing in for the SHP path's shp.Height
			int firstDrawnRow = int.MaxValue, lastDrawnRow = int.MinValue;
			for (int y = 0; y < vxl_ds.Height; y++) {
				byte* src = (byte*)vxl_ds.BitmapData.Scan0 + vxl_ds.BitmapData.Stride * y;
				for (int x = 0; x < vxl_ds.Width; x++) {
					if (*(src + x * 4 + 3) > 0) {
						if (y < firstDrawnRow) firstDrawnRow = y;
						lastDrawnRow = y;
					}
				}
			}
			int vxlHeight = lastDrawnRow >= firstDrawnRow ? lastDrawnRow - firstDrawnRow + 1 : 0;

			// like the SHP path (ShpRenderer.Draw/DrawShadow): bodies stand vxlHeight above
			// their tile, shadows lie on the ground plane and may not darken anything
			// standing taller than that plane -- most notably this unit's own hull, drawn
			// by an earlier blit of the same UnitDrawable
			// flying units (props.FlightHeight) draw their body raised while the
			// shadow stays on the ground plane beneath them
			int flight = props.FlightHeight;
			short hBufVal = (short)(obj.Tile.Z * _config.TileHeight / 2 + vxlHeight + flight);
			int castHeight = obj.Tile.Z * _config.TileHeight / 2;

			// clip to 25-50-75-100
			transLucency = transLucency / 25 * 25;
			float a = transLucency / 100f;
			float b = 1 - a;

			// short firstRowTouched = short.MaxValue;
			for (int y = 0; y < vxl_ds.Height; y++) {
				byte* src_row = (byte*)vxl_ds.BitmapData.Scan0 + vxl_ds.BitmapData.Stride * (vxl_ds.Height - y - 1);
				byte* body_row = ((byte*)ds.BitmapData.Scan0 + (d.Y + y - flight) * ds.BitmapData.Stride + d.X * 3);
				byte* shad_row = ((byte*)ds.BitmapData.Scan0 + (d.Y + y) * ds.BitmapData.Stride + d.X * 3);
				int zIdx = (d.Y + y - flight) * ds.Width + d.X;
				bool bodyRowValid = body_row >= w_low && body_row < w_high;
				bool shadRowValid = shad_row >= w_low && shad_row < w_high;
				if (!bodyRowValid && !shadRowValid) continue;

				for (int x = 0; x < vxl_ds.Width; x++) {
					bool bodyPx = *(src_row + x * 4 + 3) > 0;
					// only non-transparent pixels
					if (bodyPx && bodyRowValid) {
						if (transLucency != 0) {
							*(body_row + x * 3) = (byte)(a * *(body_row + x * 3) + b * *(src_row + x * 4));
							*(body_row + x * 3 + 1) = (byte)(a * *(body_row + x * 3 + 1) + b * *(src_row + x * 4 + 1));
							*(body_row + x * 3 + 2) = (byte)(a * *(body_row + x * 3 + 2) + b * *(src_row + x * 4 + 2));
						}
						else {
							*(body_row + x * 3) = *(src_row + x * 4);
							*(body_row + x * 3 + 1) = *(src_row + x * 4 + 1);
							*(body_row + x * 3 + 2) = *(src_row + x * 4 + 2);
						}

						// if (y < firstRowTouched)
						// 	firstRowTouched = (short)y;

						short zBufVal = (short)((obj.Tile.Rx + obj.Tile.Ry + obj.Tile.Z) * _config.TileHeight / 2);
						if (zBufVal >= zBuffer[zIdx])
							zBuffer[zIdx] = zBufVal;
						heightBuffer[zIdx] = hBufVal;
					}
					// shadows fall where the surface has no body pixel; a raised body no
					// longer occludes its own ground shadow
					if ((!bodyPx || flight != 0) && shadRowValid && shadowBufVxl[x + y * vxl_ds.Width]) {
						int shadIdx = (d.Y + y) * ds.Width + d.X + x;
						if (!shadowBuf[shadIdx] && castHeight >= heightBuffer[shadIdx]) {
							*(shad_row + x * 3) /= 2;
							*(shad_row + x * 3 + 1) /= 2;
							*(shad_row + x * 3 + 2) /= 2;
							shadowBuf[shadIdx] = true;
						}
					}
					zIdx++;
				}
			}
		}

	}
}
