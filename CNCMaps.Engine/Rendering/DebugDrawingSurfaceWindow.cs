using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
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
			pictureBox1.Image = ds.Bitmap;
		}

		private void pictureBox1_MouseMove(object sender, MouseEventArgs e) {
			StringBuilder sb = new StringBuilder();
			int rIdx = e.Location.X + e.Location.Y * _drawingSurface.Width;

			var tile = _tiles.GetTileScreen(e.Location);
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
				sb.AppendFormat("\nMouse: ({0},{1}) ", e.Location.X, e.Location.Y);
				sb.AppendFormat(": d({0},{1}) ", tile.Dx, tile.Dy);

				var gridTilenoZ = _tiles.GetTileScreen(e.Location, true, true);
				sb.AppendFormat(" Touched: {0}", _tiles.GridTouched[gridTilenoZ.Dx, gridTilenoZ.Dy / 2]);

				if (_tiles.GridTouchedBy[gridTilenoZ.Dx, gridTilenoZ.Dy / 2] != null)
					sb.AppendFormat(" by {0} ", _tiles.GridTouchedBy[gridTilenoZ.Dx, gridTilenoZ.Dy / 2]);

				sb.AppendFormat(" Z-buf: {0}", _drawingSurface.GetZBuffer()[rIdx]);
				sb.AppendFormat(" S-buf: {0}", _drawingSurface.GetShadows()[rIdx]);
#endif
			}

			toolStripStatusLabel1.Text = sb.ToString();

			if (e.Button == MouseButtons.Right) {
				Point newPoint = new Point(e.Location.X - _oldPoint.X,  e.Location.Y - _oldPoint.Y);
				panel1.AutoScrollPosition = new Point(-panel1.AutoScrollPosition.X - newPoint.X, -panel1.AutoScrollPosition.Y - newPoint.Y);
			}
		}

		public delegate void TileEvaluationDelegate(MapTile t);
		public event TileEvaluationDelegate RequestTileEvaluate;
		private void pictureBox1_MouseDown(object sender, MouseEventArgs e) {
			if (e.Button == MouseButtons.Left) {
				var tile = _tiles.GetTileScreen(e.Location);
				if (tile == null) return;
				_drawingSurface.Lock();
				RequestTileEvaluate(tile);
				_drawingSurface.Unlock();
			}
			if (e.Button == MouseButtons.Right) {
				_oldPoint = e.Location;
			}
		}
	}
}
