using System.Linq;
using System.Text;
using System.Windows.Forms;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Map;

namespace CNCMaps.Engine.Rendering {
	internal partial class DebugDrawingSurfaceWindow : Form {
		private DrawingSurface _drawingSurface;
		private TileLayer _tiles;
		private Theater _theater;
		private Game.Map _map;

		public DebugDrawingSurfaceWindow() {
			InitializeComponent();
		}

		public DebugDrawingSurfaceWindow(DrawingSurface ds, TileLayer tiles, Theater t, Game.Map map)
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
				var tileFile = (tile.Drawable as TileDrawable).GetTileFile(tile);
				sb.AppendFormat("   Tile {4}: d({0},{1}) r({2},{3})", tile.Dx, tile.Dy, tile.Rx, tile.Ry, tileFile.FileName.ToUpper());
				if (tileFile.Images[tile.SubTile].RampType != 0)
					sb.AppendFormat(" ramp {0}", tileFile.Images[tile.SubTile].RampType);
				if (tileFile.Images[tile.SubTile].TerrainType != 0)
					sb.AppendFormat(" terrain {0}", tileFile.Images[tile.SubTile].TerrainType);

				var gridTilenoZ = _tiles.GetTileScreen(e.Location, true, true);
				sb.AppendFormat("   Touched: {0}", _tiles.GridTouched[gridTilenoZ.Dx, gridTilenoZ.Dy / 2]);

				if (_tiles.GridTouchedBy[gridTilenoZ.Dx, gridTilenoZ.Dy / 2] != null)
					sb.AppendFormat(" by {0} ", _tiles.GridTouchedBy[gridTilenoZ.Dx, gridTilenoZ.Dy / 2]);

				sb.AppendFormat("   Z-buf: {0}", _drawingSurface.GetZBuffer()[rIdx]);
				sb.AppendFormat("   S-buf: {0}", _drawingSurface.GetShadows()[rIdx]);

				var objs = _map.GetObjectsAt(tile.Dx, tile.Dy / 2);
				if (objs.Any()) {
					sb.Append("   Objects: ");
					foreach (var obj in objs) {
						sb.Append(obj);

						if (obj is OverlayObject) {
							var ovl = (obj as OverlayObject);
							if (ovl.IsGeneratedVeins)
								sb.Append("(gen)");
						}

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
