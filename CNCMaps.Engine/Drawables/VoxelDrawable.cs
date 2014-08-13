using System.Drawing;
using System.IO;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;

namespace CNCMaps.Engine.Game {
	class VoxelDrawable : Drawable {
		internal static readonly VxlRenderer VoxelRenderer = new VxlRenderer();
		public VxlFile Vxl;
		public HvaFile Hva;

		public VoxelDrawable() {
		}

		public VoxelDrawable(IniFile.IniSection rules, IniFile.IniSection art)
			: base(rules, art) {
		}

		public VoxelDrawable(VxlFile vxl, HvaFile hva) {
			Vxl = vxl;
			Hva = hva;
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool shadows = true) {
			if (Vxl == null || Hva == Stream.Null) return;
			DrawingSurface vxl_ds = VoxelRenderer.Render(Vxl, Hva, obj, Props);
			if (vxl_ds != null)
				BlitVoxelToSurface(ds, vxl_ds, obj, Props);

			// todo: voxel shadows
		}

		public override Rectangle GetBounds(GameObject obj) {
			if (Vxl == null || Hva == null) return Rectangle.Empty;
			var bounds = VxlRenderer.GetBounds(obj, Vxl, Hva, Props);
			bounds.Offset(obj.Tile.Dx * TileWidth / 2, (obj.Tile.Dy - obj.Tile.Z) * TileHeight / 2);
			bounds.Offset(Props.GetOffset(obj));
			return bounds;
		}

		private unsafe void BlitVoxelToSurface(DrawingSurface ds, DrawingSurface vxl_ds, GameObject obj, DrawProperties props) {
			Point d = new Point(obj.Tile.Dx * TileWidth / 2, (obj.Tile.Dy - obj.Tile.Z) * TileHeight / 2);
			d.Offset(props.GetOffset(obj));
			d.Offset(-vxl_ds.BitmapData.Width / 2, -vxl_ds.BitmapData.Height / 2);

			// rows inverted!
			var w_low = (byte*)ds.BitmapData.Scan0;
			byte* w_high = w_low + ds.BitmapData.Stride * ds.BitmapData.Height;
			var zBuffer = ds.GetZBuffer();
			// int rowsTouched = 0;

			// short firstRowTouched = short.MaxValue;
			for (int y = 0; y < vxl_ds.Height; y++) {

				byte* src_row = (byte*)vxl_ds.BitmapData.Scan0 + vxl_ds.BitmapData.Stride * (vxl_ds.Height - y - 1);
				byte* dst_row = ((byte*)ds.BitmapData.Scan0 + (d.Y + y) * ds.BitmapData.Stride + d.X * 3);
				int zIdx = (d.Y + y) * ds.Width + d.X;
				if (dst_row < w_low || dst_row >= w_high) continue;

				for (int x = 0; x < vxl_ds.Width; x++) {
					// only non-transparent pixels
					if (*(src_row + x * 4 + 3) > 0) {
						*(dst_row + x * 3) = *(src_row + x * 4);
						*(dst_row + x * 3 + 1) = *(src_row + x * 4 + 1);
						*(dst_row + x * 3 + 2) = *(src_row + x * 4 + 2);

						// if (y < firstRowTouched)
						// 	firstRowTouched = (short)y;

						short zBufVal = (short)((obj.Tile.Rx + obj.Tile.Ry + obj.Tile.Z) * TileHeight / 2);
						if (zBufVal >= zBuffer[zIdx])
							zBuffer[zIdx] = zBufVal;
					}
					zIdx++;
				}
			}
		}

	}
}
