using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using CNCMaps.Engine.Drawables;
using CNCMaps.FileFormats.Map;
using CNCMaps.Shared;
using CNCMaps.Shared.Utility;
using NLog;

namespace CNCMaps.Engine.Map {
	public class TileLayer : IEnumerable<MapTile> {
		/* 
		Coordinate formulas
		dx = rx - ry + mapwidth - 1
		dy = rx + ry - mapwidth - 1

		rx = (dx + dy) / 2 + 1		
		ry = dy - rx + mapwidth + 1
		*/

		static Logger logger = LogManager.GetCurrentClassLogger();

		public TouchType[,] GridTouched { get; private set; }
		public MapTile[,] GridTouchedBy { get; private set; }
		MapTile[,] tiles;
		private Size fullSize;
		private readonly ModConfig _config;

		public TileLayer(int w, int h, ModConfig config)
			: this(new Size(w, h), config) {
		}

		public TileLayer(Size fullSize, ModConfig config) {
			this.fullSize = fullSize;
			_config = config;
			tiles = new MapTile[fullSize.Width * 2 - 1, fullSize.Height];
			GridTouched = new TouchType[fullSize.Width * 2 - 1, fullSize.Height];
			GridTouchedBy = new MapTile[fullSize.Width * 2 - 1, fullSize.Height];
		}

		public int Width {
			get { return fullSize.Width; }
		}

		public int Height {
			get { return fullSize.Height; }
		}

		public MapTile this[int x, int y] {
			get {
				if (0 <= x && x < tiles.GetLength(0) && 0 <= y && y < tiles.GetLength(1))
					return tiles[x, y];
				else
					return null;
			}
			set {
				tiles[x, y] = value;
			}
		}

		/// <summary>Gets a tile at display coordinates.</summary>
		/// <param name="dx">The dx.</param>
		/// <param name="dy">The dy.</param>
		/// <returns>The tile.</returns>
		public MapTile GetTile(int dx, int dy) {
			return tiles[dx, dy];
		}

		/// <summary>Gets a tile at map coordinates.</summary>
		/// <param name="rx">The rx.</param>
		/// <param name="ry">The ry.</param>
		/// <returns>The tile r.</returns>
		public MapTile GetTileR(int rx, int ry) {
			int dx = (rx - ry + fullSize.Width - 1);
			int dy = rx + ry - fullSize.Width - 1;

			if (dx < 0 || dy < 0 || dx >= tiles.GetLength(0) || (dy / 2) >= tiles.GetLength(1)) {
				logger.Trace("Referencing empty tile at (rx,ry)=({0},{1}); (dx,dy)=({2},{3})", rx, ry, dx, dy);
				return null;
			}
			else
				return GetTile(dx, dy / 2);
		}

		public Point GetTilePixelCenter(IsoTile t) {
			var ret = new Point(t.Dx * _config.TileWidth / 2, (t.Dy - t.Z) * _config.TileHeight);
			ret.Offset(_config.TileWidth / 2, _config.TileHeight / 2);
			return ret;
		}

		public MapTile GetTileScreen(Point p, bool fixOOB = true, bool omitHeight = false) {
			// use inverse matrix of world projection for screen to world
			int w = _config.TileWidth / 2;
			int h = _config.TileHeight / 2;
			int fx = w * Width;
			int fy = h * (-1 - Width);
			int rx = (p.X * h + p.Y * w - fx * h - fy * w) / (2 * w * h);
			int ry = (p.X * -h + p.Y * w + fx * h - fy * w) / (2 * w * h);

			int dx = rx - ry + Width - 1;
			int dy = rx + ry - Width - 1;
			if (fixOOB) {
				dx = Math.Min(Width * 2 - 2, Math.Max(0, dx));
				dy = Math.Min(Height * 2 - 2, Math.Max(0, dy));
			}
			var tile_noheight = this[dx, dy / 2];
			if (omitHeight)
				return tile_noheight;

			else dy += tile_noheight.Z;
			if (fixOOB)
				dy = Math.Min(Height * 2 - 2, Math.Max(0, dy));
			return this[dx, dy / 2];
		}

		#region neighbouring tiles tests (auto-lat tests)
		public void testNeighbours() {
			testNeighbours(15, 9);
			testNeighbours(14, 13);
		}

		private void testNeighbours(int x, int y) {
			int x_ = tiles[x, y].Dx;
			int y_ = tiles[x, y].Dy;
			Debug.Assert(GetNeighbourTile(x, y, TileDirection.Bottom).Dx == x_ + 0 && GetNeighbourTile(x, y, TileDirection.Bottom).Dy == y_ + 2);
			Debug.Assert(GetNeighbourTile(x, y, TileDirection.BottomLeft).Dx == x_ - 1 && GetNeighbourTile(x, y, TileDirection.BottomLeft).Dy == y_ + 1);
			Debug.Assert(GetNeighbourTile(x, y, TileDirection.BottomRight).Dx == x_ + 1 && GetNeighbourTile(x, y, TileDirection.BottomRight).Dy == y_ + 1);
			Debug.Assert(GetNeighbourTile(x, y, TileDirection.Left).Dx == x_ - 2 && GetNeighbourTile(x, y, TileDirection.Left).Dy == y_ + 0);
			Debug.Assert(GetNeighbourTile(x, y, TileDirection.Right).Dx == x_ + 2 && GetNeighbourTile(x, y, TileDirection.Right).Dy == y_ + 0);
			Debug.Assert(GetNeighbourTile(x, y, TileDirection.Top).Dx == x_ + 0 && GetNeighbourTile(x, y, TileDirection.Top).Dy == y_ - 2);
			Debug.Assert(GetNeighbourTile(x, y, TileDirection.TopLeft).Dx == x_ - 1 && GetNeighbourTile(x, y, TileDirection.TopLeft).Dy == y_ - 1);
			Debug.Assert(GetNeighbourTile(x, y, TileDirection.TopRight).Dx == x_ + 1 && GetNeighbourTile(x, y, TileDirection.TopRight).Dy == y_ - 1);
		}

		public enum TileDirection {
			Top,
			TopLeft,
			TopRight,
			Left,
			Right,
			BottomLeft,
			Bottom,
			BottomRight
		}

		public MapTile GetNeighbourTile(MapTile t, TileDirection tileDirection) {
			// find index for t
			int x = t.Dx;
			int y = (t.Dy + (t.Dx + 1) % 2) / 2;
			Debug.Assert(tiles[x, y] == t);
			return GetNeighbourTile(x, y, tileDirection);
		}

		MapTile GetNeighbourTile(int x, int y, TileDirection direction) {
			switch (direction) {
				// in non-diagonal direction we don't need to check odd/evenness of x
				case TileDirection.Bottom:
					if (y >= tiles.GetLength(1)) return null;
					return this[x, y + 1];

				case TileDirection.Top:
					if (y < 2) return null;
					return this[x, y - 1];

				case TileDirection.Left:
					if (x < 2) return null;
					return this[x - 2, y];

				case TileDirection.Right:
					if (x >= tiles.GetLength(0) - 1) return null;
					return this[x + 2, y];
			}

			// the horizontally neighbouring tiles have dy' = dy + 1 if x is odd,
			// and the horizontally neighbouring tiles have dy' = dy - 1 if x is even,
			y += x % 2;
			switch (direction) {
				case TileDirection.BottomLeft:
					if (x < 1 || y >= tiles.GetLength(1)) return null;
					return this[x - 1, y];

				case TileDirection.BottomRight:
					if (x >= tiles.GetLength(0) || y >= tiles.GetLength(1)) return null;
					return this[x + 1, y];

				case TileDirection.TopLeft:
					if (x < 1 || y < 1) return null;
					return this[x - 1, y - 1];

				case TileDirection.TopRight:
					if (y < 1 || x >= tiles.GetLength(0) - 1) return null;
					return this[x + 1, y - 1];
			}
			throw new InvalidOperationException();
		}

		#endregion

		#region enumerator stuff
		public IEnumerator<MapTile> GetEnumerator() {
			return new TwoDimensionalEnumerator<MapTile>(tiles);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return new TwoDimensionalEnumerator<MapTile>(tiles);
		}
		#endregion

		[Flags]
		public enum TouchType {
			Untouched = 0,
			ByNormalData = 1,
			ByExtraData = 2,
		}


		public MapTile GetTile(IsoTile isoTile) {
			return this[isoTile.Dx, isoTile.Dy / 2];
		}

	}

}
