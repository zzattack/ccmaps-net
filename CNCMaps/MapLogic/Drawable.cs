using System.Collections.Generic;
using System.Drawing;
using CNCMaps.FileFormats;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.MapLogic {
	class Drawable {
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public Drawable(string name) {
			Name = name;
			Foundation = new Size(1, 1);
		}

		bool sorted;
		void Sort() {
			fires.Sort();
			shps.Sort();
			damagedShps.Sort();
			voxels.Sort();
			sorted = true;
		}

		public virtual void Draw(RA2Object obj, DrawingSurface ds) {
			logger.Trace("Drawing object {0} (type {1})", obj, obj.GetType());

			if (!sorted) Sort();

			if (obj is DamageableObject && (obj as DamageableObject).Health < 128) {
				foreach (var v in damagedShps)
					DrawFile(obj, ds, v.file, v.props);

				foreach (var v in fires)
					DrawFile(obj, ds, v.file, v.props, palettes.animPalette);
			}
			else {
				foreach (var v in shps)
					DrawFile(obj, ds, v.file, v.props);
			}

			if (alphaImage != null) {
				int dx = obj.Tile.Dx * TileWidth / 2;
				int dy = (obj.Tile.Dy - obj.Tile.Z) * TileHeight / 2;
				dx += globalOffset.X;
				dy += globalOffset.Y;
				alphaImage.DrawAlpha(0, ds, dx, dy);
			}

			for (int i = 0; i < voxels.Count; i++) {
				Palette p = null;

				if (obj is RemappableObject) p = (obj as RemappableObject).Palette;
				if (obj is UnitObject) direction = (obj as UnitObject).Direction;
				else if (obj is StructureObject) direction = (obj as StructureObject).Direction;
				DrawingSurface vxl_ds = voxelrenderer.Render(voxels[i].file, hvas[i], -(double)direction / 256.0 * 360 + 45, p ?? Palette);
				if (vxl_ds == null)
					continue;

				// rows inverted!
				int dx = obj.Tile.Dx * TileWidth / 2;
				int dy = (obj.Tile.Dy - obj.Tile.Z) * TileHeight / 2;
				dx += globalOffset.X;
				dy += globalOffset.Y;
				var props = voxels[i].props;
				dx += props.offset.X;
				dy += props.offset.Y;
				dx -= vxl_ds.bmd.Width / 2;
				dy -= vxl_ds.bmd.Height / 2;
				
				unsafe {
					var w_low = (byte*)ds.bmd.Scan0;
					byte* w_high = w_low + ds.bmd.Stride * ds.bmd.Height;

					for (int y = 0; y < vxl_ds.Height; y++) {
						byte* src_row = (byte*)vxl_ds.bmd.Scan0 + vxl_ds.bmd.Stride * (vxl_ds.Height - y - 1);
						byte* dst_row = ((byte*)ds.bmd.Scan0 + (dy + y) * ds.bmd.Stride + dx * 3);
						if (dst_row < w_low || dst_row >= w_high) continue;

						for (int x = 0; x < vxl_ds.Width; x++) {
							// only non-transparent pixels
							if (*(src_row + x * 4 + 3) > 0) {
								*(dst_row + x * 3) = *(src_row + x * 4);
								*(dst_row + x * 3 + 1) = *(src_row + x * 4 + 1);
								*(dst_row + x * 3 + 2) = *(src_row + x * 4 + 2);
							}
						}

					}
				}

			}

		}

		static VoxelRenderer voxelrenderer = new VoxelRenderer();

		private void DrawFile(RA2Object obj, DrawingSurface ds, ShpFile file, DrawProperties props, Palette p = null) {
			if (file == null || obj == null || obj.Tile == null) return;

			Point offset = globalOffset;
			offset.Offset(props.offset);

			if (UseTilePalette) p = obj.Tile.Palette;
			else if (p == null && obj is RemappableObject)
				p = (obj as RemappableObject).Palette;

			if (objectOverrides && obj is OverlayObject) {
				var o = obj as OverlayObject;
				if (TileWidth == 60) {
					// bridge
					if (o.IsBridge())
						offset.Y += o.OverlayValue > 8 ? -16 : -1;
				}
				else {
					// tibsun
					offset.X += o.OverlayValue > 8 ? -7 : -6;
					offset.Y += o.OverlayValue > 8 ? -13 : -1;
				}
			}
			file.Draw(frame, ds, offset, obj.Tile, p ?? Palette);
			if (props.hasShadow)
				file.DrawShadow(frame, ds, offset, obj.Tile);
		}

		public static PaletteCollection palettes { get; set; }
		public Palette Palette { get; set; }
		ShpFile alphaImage;
		Point globalOffset = new Point(0, 0);

		int heightOffset;
		bool objectOverrides;
		public Size Foundation { get; set; }
		int direction; // for voxels
		int frame; // for shps
		public string Name { get; private set; }

		List<DrawableFile<VxlFile>> voxels = new List<DrawableFile<VxlFile>>();
		List<HvaFile> hvas = new List<HvaFile>();

		List<DrawableFile<ShpFile>> shps = new List<DrawableFile<ShpFile>>();
		List<DrawableFile<ShpFile>> fires = new List<DrawableFile<ShpFile>>();
		List<DrawableFile<ShpFile>> damagedShps = new List<DrawableFile<ShpFile>>();

		internal void SetAlphaImage(ShpFile shpFile) {
			alphaImage = shpFile;
		}

		internal void SetOffset(int xOffset, int yOffset) {
			globalOffset.X = xOffset;
			globalOffset.Y = yOffset;
		}

		internal void SetHeightOffset(int heightOffset) {
			this.heightOffset = heightOffset;
		}

		internal void SetOverrides(bool overrides) {
			objectOverrides = overrides;
		}

		internal void SetFoundation(int w, int h) {
			Foundation = new Size(w, h);
		}

		internal void AddOffset(int extraXOffset, int extraYOffset) {
			globalOffset.X += extraXOffset;
			globalOffset.Y += extraYOffset;
		}

		internal void AddVoxel(VxlFile vxlFile, HvaFile hvaFile, int xOffset = 0, int yOffset = 0, bool hasShadow = false, int ySort = 0) {
			voxels.Add(new DrawableFile<VxlFile>(vxlFile, new DrawProperties(new Point(xOffset, yOffset), hasShadow, ySort), voxels.Count));
			hvas.Add(hvaFile);
		}

		internal void AddShp(ShpFile shpFile, int xOffset = 0, int yOffset = 0, bool hasShadow = false, int ySort = 0) {
			shps.Add(new DrawableFile<ShpFile>(shpFile, new DrawProperties(new Point(xOffset, yOffset), hasShadow, ySort), shps.Count));
		}

		internal void AddDamagedShp(ShpFile shpFile, int xOffset = 0, int yOffset = 0, bool hasShadow = false, int ySort = 0) {
			damagedShps.Add(new DrawableFile<ShpFile>(shpFile, new DrawProperties(new Point(xOffset, yOffset), hasShadow, ySort), damagedShps.Count));
		}

		internal void AddFire(ShpFile shpFile, int xOffset, int yOffset) {
			fires.Add(new DrawableFile<ShpFile>(shpFile, new DrawProperties(new Point(xOffset, yOffset), false, 0), fires.Count));
		}

		internal void SetFrame(int frameNum) {
			frame = frameNum;
		}

		public static ushort TileWidth { get; set; }
		public static ushort TileHeight { get; set; }

		public bool UseTilePalette { get; set; }
	}

	class DrawableFile<T> : System.IComparable where T : VirtualFile {
		public DrawProperties props;
		public T file;
		int index;
		public DrawableFile(T file, DrawProperties drawProperties, int index) {
			this.file = file;
			props = drawProperties;
			this.index = index;
		}

		public int CompareTo(object obj) {
			var other = obj as DrawableFile<T>;
			if (props.ySort != other.props.ySort)
				return props.ySort - other.props.ySort;
			else
				return index - other.index;
		}
	}
}