using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Map;

namespace CNCMaps.Engine.Rendering {
	internal partial class DebugDrawingSurfaceWindow : Form {
		private DrawingSurface _drawingSurface;
		private TileLayer _tiles;
		private Theater _theater;
		private Map.Map _map;
		private int _cells;
		private Point _oldPoint;
		private Bitmap _heightMap;
		private Bitmap _shadowMap;

		public DebugDrawingSurfaceWindow() {
			InitializeComponent();
		}

		public DebugDrawingSurfaceWindow(DrawingSurface ds, TileLayer tiles, Theater t, Map.Map map)
			: this() {
			_drawingSurface = ds;
			_tiles = tiles;
			_theater = t;
			_map = map;

			_cells = (_map.FullSize.Width * 2 - 1) * _map.FullSize.Height;

			ds.Unlock();

			// map is pre-rendered bitmap, shadow/heightmap are rendered on demand
			canvasMap.Image = ds.Bitmap;
		}

		private void canvas_MouseMove(object sender, MouseEventArgs e) {
			StringBuilder sb = new StringBuilder();
			var canvas = sender as ZoomableCanvas;
			var pixelLocationF = canvas.PointToImagePixel(e.Location);
			var location = new Point((int)Math.Round(pixelLocationF.X, 0), (int)Math.Round(pixelLocationF.Y, 0));
			if (location.X < 0 || location.Y < 0 || location.X >= canvas.ImageSize.Width || location.Y >= canvas.ImageSize.Height)
				return;

			int rIdx = location.X + location.Y * _drawingSurface.Width;


			var tile = _tiles.GetTileScreen(location);
			if (tile == null || !(tile.Drawable is TileDrawable)) {
				sb.Append("No valid tile under mouse");
			}
			else {
				sb.AppendFormat("Cells: {0} Coords (X, Y / H): {1}, {2} / {3}", _cells, tile.Rx, tile.Ry, tile.Z);

				var objs = _map.GetObjectsAt(tile.Dx, tile.Dy / 2);
				if (objs.Any()) {
					sb.Append(" Objects:");
					foreach (var obj in objs) {
						sb.Append(" " + obj);

						if (obj is OverlayObject) {
							var ovl = (obj as OverlayObject);
							if (ovl.IsGeneratedVeins)
								sb.Append("(gen)");
						}
						sb.Append(" ");
					}
				}

				var tileFile = (tile.Drawable as TileDrawable).GetTileFile(tile);
				if (tileFile != null) {
					sb.AppendFormat("\nTile: {0}", (tileFile?.FileName??"").ToLower());
					sb.AppendFormat(" TileNum: {0} SubTile: {1}", tile.TileNum, tile.SubTile);
					if (tileFile.Images[tile.SubTile].RampType != 0)
						sb.AppendFormat(" Ramp: {0}", tileFile.Images[tile.SubTile].RampType);
					if (tileFile.Images[tile.SubTile].TerrainType != 0)
						sb.AppendFormat(" Terrain: {0}", tileFile.Images[tile.SubTile].TerrainType);
					if (tile.IceGrowth > 0)
						sb.Append(" IceGrowth");
				}

#if DEBUG
				sb.AppendFormat("\nMouse: ({0},{1}) ", location.X, location.Y);
				sb.AppendFormat(": d({0},{1}) ", tile.Dx, tile.Dy);

				var gridTilenoZ = _tiles.GetTileScreen(location, true, true);
				sb.AppendFormat(" Touched: {0}", _tiles.GridTouched[gridTilenoZ.Dx, gridTilenoZ.Dy / 2]);

				if (_tiles.GridTouchedBy[gridTilenoZ.Dx, gridTilenoZ.Dy / 2] != null)
					sb.AppendFormat(" by {0} ", _tiles.GridTouchedBy[gridTilenoZ.Dx, gridTilenoZ.Dy / 2]);

				sb.AppendFormat(" Z-buf: {0}", _drawingSurface.GetZBuffer()[rIdx]);
				sb.AppendFormat(" S-buf: {0}", _drawingSurface.GetShadows()[rIdx]);
#endif
			}

			toolStripStatusLabel1.Text = sb.ToString();

			if (e.Button == MouseButtons.Right) {
				Point newPoint = new Point(location.X - _oldPoint.X,  location.Y - _oldPoint.Y);
				panel1.AutoScrollPosition = new Point(-panel1.AutoScrollPosition.X - newPoint.X, -panel1.AutoScrollPosition.Y - newPoint.Y);
			}
		}

		public delegate void TileEvaluationDelegate(MapTile t);
		public event TileEvaluationDelegate RequestTileEvaluate;
		private void canvas_MouseDown(object sender, MouseEventArgs e) {
			if (e.Button == MouseButtons.Left) {
				var canvas = sender as ZoomableCanvas;
				var pixelLocationF = canvas.PointToImagePixel(e.Location);
				var location = new Point((int)Math.Round(pixelLocationF.X, 0), (int)Math.Round(pixelLocationF.Y, 0));
				if (location.X < 0 || location.Y < 0 || location.X >= canvas.ImageSize.Width || location.Y >= canvas.ImageSize.Height)
					return;

				var tile = _tiles.GetTileScreen(location);
				if (tile == null) return;
				_drawingSurface.Lock();
				RequestTileEvaluate(tile);
				_drawingSurface.Unlock();
			}
			if (e.Button == MouseButtons.Right) {
				_oldPoint = e.Location;
			}
		}

		private void tabs_SelectedIndexChanges(object sender, EventArgs e) {
			if (tabs.SelectedTab == tpHeightmap && _heightMap == null) {
				_heightMap = new Bitmap(_drawingSurface.Width, _drawingSurface.Height, PixelFormat.Format8bppIndexed);

				// Create grayscale palette
				ColorPalette pal = _heightMap.Palette;
				for (int i = 0; i <= 255; i++) {
					pal.Entries[i] = Color.FromArgb(i, i, i);
				}
				_heightMap.Palette = pal; // re-setting activates palette

				// Draw map
				var heightBuffer = _drawingSurface.GetHeightBuffer();
				var bmd = _heightMap.LockBits(new Rectangle(0, 0, _drawingSurface.Width, _drawingSurface.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
				uint idx = 0;
				unsafe {
					for (int row = 0; row < _drawingSurface.Height; row++) {
						byte* w = (byte*)bmd.Scan0.ToPointer() + bmd.Stride * row;
						for (int col = 0; col < _drawingSurface.Width; col++) {
							*w++ = (byte)(heightBuffer[idx++] & 0xFF);
						}
					}
				}

				_heightMap.UnlockBits(bmd);
				canvasHeight.Image = _heightMap;
			}
			else if (tabs.SelectedTab == tpShadowMap && _shadowMap == null) {
				_shadowMap = new Bitmap(_drawingSurface.Width, _drawingSurface.Height, PixelFormat.Format8bppIndexed);

				// Create palette with index 0 blue, all others red
				ColorPalette pal = _heightMap.Palette;
				for (int i = 0; i <= 255; i++) {
					// create greyscale color table
					pal.Entries[i] = i == 0 ? Color.Blue : Color.Red;
				}
				_shadowMap.Palette = pal; // re-setting activates palette

				// Draw map
				var bmd = _shadowMap.LockBits(new Rectangle(0, 0, _drawingSurface.Width, _drawingSurface.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
				uint idx = 0;
				unsafe {
					for (int row = 0; row < _drawingSurface.Height; row++) {
						byte* w = (byte*)bmd.Scan0.ToPointer() + bmd.Stride * row;
						for (int col = 0; col < _drawingSurface.Width; col++) {
							*w++ = (byte)(_drawingSurface.IsShadow(col, row) ? 1 : 0);
						}
					}
				}

				_shadowMap.UnlockBits(bmd);
				canvasShadows.Image = _shadowMap;
			}
		}

		private void DebugDrawingSurfaceWindow_FormClosed(object sender, FormClosedEventArgs e) {
			_heightMap?.Dispose();
			_shadowMap?.Dispose();
		}

		private void form_KeyDown(object sender, KeyEventArgs e) {
			if (tabs.SelectedTab.Controls[0] is ZoomableCanvas canvas) {
				if (e.Control && e.KeyCode == Keys.NumPad0) {
					var focus = new PointF(canvas.Width / 2f, canvas.Height / 2f); // center of currently visible area
					canvas.ZoomToLevel(0, focus);
				}
				else if (e.Control && e.KeyCode == Keys.NumPad1) {
					canvas.ZoomToFit();
				} 
			}
		}
	}
}
