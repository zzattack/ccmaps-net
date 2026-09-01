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
			var shadowBufVxl = vxl_ds.GetShadows();
			var voxelMask = ds.GetVoxelMask();

			// bottom-most drawn source row; source rows are stored bottom-up, so source
			// row r appears on display row (Height - 1 - r)
			int firstDrawnRow = int.MaxValue;
			for (int y = 0; y < vxl_ds.Height; y++) {
				byte* src = (byte*)vxl_ds.BitmapData.Scan0 + vxl_ds.BitmapData.Stride * y;
				for (int x = 0; x < vxl_ds.Width; x++) {
					if (*(src + x * 4 + 3) > 0) {
						firstDrawnRow = y;
						break;
					}
				}
				if (firstDrawnRow != int.MaxValue) break;
			}
			if (firstDrawnRow == int.MaxValue)
				return;

			// gamemd blits the cached voxel through the same Shape_Draw_Z path as SHP objects, with the
			// Deg90 standing gradient anchored at the bottom row of the region the model's volume projects
			// to, and BlitterFlags Alpha|Flat: the pixels are z-tested against the buffer but never written
			// back. Anchoring at the last drawn pixel instead puts a turret whose box reaches under its
			// geometry behind its own post. Flying bodies draw raised while their z stays anchored at the
			// ground-projected row.
			int flight = props.FlightHeight;
			var t = obj.Tile;
			int cellBottomY = (t.Dy - t.Z) * _config.TileHeight / 2 + _config.TileHeight - 1;
			int anchorY = d.Y + VoxelRenderer.VolumeBottomRow;
			// ZAdjust uses the game's sign, as in ShpRenderer: positive pushes away from the screen.
			// A voxel turret on a building carries the building's TurretAnimZAdjust; without it the
			// turret loses the z-test against the body it sits on (the Grand Cannon's mounting plate
			// then draws over its own gun).
			int zBase = (t.Rx + t.Ry) * _config.TileHeight / 2 + (anchorY - cellBottomY) + 1 - props.ZAdjust;
			int zShadowBase = (t.Rx + t.Ry) * _config.TileHeight / 2 + 2;
			// units on a bridge draw raised; their z stays anchored on the deck plane
			if (obj is OwnableObject oo && oo.OnBridge)
				zBase += 4 * _config.TileHeight / 2;

			// clip to 25-50-75-100
			transLucency = transLucency / 25 * 25;
			float a = transLucency / 100f;
			float b = 1 - a;

			for (int y = 0; y < vxl_ds.Height; y++) {
				byte* src_row = (byte*)vxl_ds.BitmapData.Scan0 + vxl_ds.BitmapData.Stride * (vxl_ds.Height - y - 1);
				byte* body_row = ((byte*)ds.BitmapData.Scan0 + (d.Y + y - flight) * ds.BitmapData.Stride + d.X * 3);
				byte* shad_row = ((byte*)ds.BitmapData.Scan0 + (d.Y + y) * ds.BitmapData.Stride + d.X * 3);
				int zIdx = (d.Y + y - flight) * ds.Width + d.X;
				bool bodyRowValid = body_row >= w_low && body_row < w_high;
				bool shadRowValid = shad_row >= w_low && shad_row < w_high;
				if (!bodyRowValid && !shadRowValid) continue;

				short zBufVal = (short)(zBase + (anchorY - (d.Y + y - flight)) / 3);
				short zShadowVal = (short)(zShadowBase + (d.Y + y) - cellBottomY);

				for (int x = 0; x < vxl_ds.Width; x++) {
					bool bodyPx = *(src_row + x * 4 + 3) > 0;
					// only non-transparent pixels in front of what the buffer holds
					if (bodyPx && bodyRowValid && zBufVal > zBuffer[zIdx]) {
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
						if (voxelMask != null)
							voxelMask[zIdx] = true;
					}
					// shadows lie on the caster's ground plane and darken only where that
					// plane is in front of the buffer; the body pixels drawn by this same
					// blit keep covering their own shadow
					if ((!bodyPx || flight != 0) && shadRowValid && shadowBufVxl[x + y * vxl_ds.Width]) {
						int shadIdx = (d.Y + y) * ds.Width + d.X + x;
						if (zShadowVal > zBuffer[shadIdx]) {
							*(shad_row + x * 3) /= 2;
							*(shad_row + x * 3 + 1) /= 2;
							*(shad_row + x * 3 + 2) /= 2;
						}
					}
					zIdx++;
				}
			}
		}

	}
}
