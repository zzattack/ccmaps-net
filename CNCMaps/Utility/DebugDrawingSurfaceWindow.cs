using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CNCMaps.Game;
using CNCMaps.Map;
using CNCMaps.Rendering;

namespace CNCMaps.Utility {
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
			pictureBox1.Image = ds.bm;
		}

		private void pictureBox1_MouseMove(object sender, MouseEventArgs e) {
			StringBuilder sb = new StringBuilder();
			int rIdx = e.Location.X + e.Location.Y * _drawingSurface.Width;

			sb.AppendFormat("Mouse: ({0},{1})", e.Location.X, e.Location.Y);
			var tile = _tiles.GetTileScreen(e.Location);
			if (tile == null) return;
			var tileFile = _theater.GetTileCollection().GetTileFile(tile);
			sb.AppendFormat("   Tile {4}: d({0},{1}) r({2},{3})", tile.Dx, tile.Dy, tile.Rx, tile.Ry, tileFile.FileName.ToUpper());
			sb.AppendFormat("   Z-buf: {0}", _drawingSurface.GetZBuffer()[rIdx]);
			sb.AppendFormat("   S-buf: {0}", _drawingSurface.GetShadows()[rIdx]);
			sb.AppendFormat("   H-buf: {0}", _drawingSurface.GetHeightBuffer()[rIdx]);

			if (tile.AllObjects.Count > 0) {
				sb.Append("   Objects: ");
				foreach (var obj in tile.AllObjects) {
					sb.Append(obj);
					sb.Append(" ");
				}
			}

			toolStripStatusLabel1.Text = sb.ToString();
		}

		public delegate void TileEvaluationDelegate(MapTile t);
		public event TileEvaluationDelegate RequestTileEvaluate;
		private void pictureBox1_MouseDown(object sender, MouseEventArgs e) {
			var tile = _tiles.GetTileScreen(e.Location);
			if (tile == null) return;
			_drawingSurface.Lock();
			RequestTileEvaluate(tile);
			_drawingSurface.Unlock();
		}

	}
}
