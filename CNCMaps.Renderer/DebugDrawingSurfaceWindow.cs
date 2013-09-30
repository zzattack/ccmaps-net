using System.Linq;
using System.Text;
using System.Windows.Forms;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;

namespace CNCMaps {
	internal partial class DebugDrawingSurfaceWindow : Form {
		private DrawingSurface _drawingSurface;
		private TileLayer _tiles;
		private Theater _theater;
		private MapFile _map;

		public DebugDrawingSurfaceWindow() {
			InitializeComponent();
		}

		public DebugDrawingSurfaceWindow(DrawingSurface ds, TileLayer tiles, Theater t, MapFile map)
			: this() {
			_drawingSurface = ds;
			_tiles = tiles;
			_theater = t;
			_map = map;

			ds.Unlock();
			pictureBox1.Image = ds.Bitmap;
		}

		private void pictureBox1_MouseMove(object sender, MouseEventArgs e) {
			StringBuilder sb = new StringBuilder();
			int rIdx = e.Location.X + e.Location.Y * _drawingSurface.Width;

			sb.AppendFormat("Mouse: ({0},{1})", e.Location.X, e.Location.Y);
			var tile = _tiles.GetTileScreen(e.Location);
			if (tile == null) {
				sb.Append("No valid tile under mouse");
			}
			else {
				var tileFile = (tile.Drawable as TmpDrawable).GetTileFile(tile);
				sb.AppendFormat("   Tile {4}: d({0},{1}) r({2},{3})", tile.Dx, tile.Dy, tile.Rx, tile.Ry, tileFile.FileName.ToUpper());
				
				var gridTilenoZ = _tiles.GetTileScreen(e.Location, true, true);
				sb.AppendFormat("   Touched: {0}", _tiles.GridTouched[gridTilenoZ.Dx, gridTilenoZ.Dy / 2]);

				if (_tiles.GridTouchedBy[gridTilenoZ.Dx, gridTilenoZ.Dy / 2] != null)
					sb.AppendFormat(" by {0} ", _tiles.GridTouchedBy[gridTilenoZ.Dx, gridTilenoZ.Dy / 2]);

				sb.AppendFormat("   S-buf: {0}", _drawingSurface.GetShadows()[rIdx]);
				sb.AppendFormat("   H-buf: {0}", _drawingSurface.GetHeightBuffer()[rIdx]);
				sb.AppendFormat("   Z-buf: {0}", _drawingSurface.GetZBuffer()[rIdx]);
			
				var objs = _map.GetObjectsAt(tile.Dx, tile.Dy / 2);
				if (objs.Any()) {
					sb.Append("   Objects: ");
					foreach (var obj in objs) {
						sb.Append(obj);
						sb.Append(" ");
					}
				}
			}

			toolStripStatusLabel1.Text = sb.ToString();
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
		}

	}
}
