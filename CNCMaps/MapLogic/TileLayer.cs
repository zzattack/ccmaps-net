using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Collections;

namespace CNCMaps.MapLogic {

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

	class TileLayer : IEnumerable<MapTile> {
		MapTile[,] tiles;
		private Size fullSize;

		public TileLayer(int w, int h)
			: this(new Size(w, h)) {
		}

		public TileLayer(Size fullSize) {
			tiles = new MapTile[fullSize.Width * 2 - 1, fullSize.Height];
			this.fullSize = fullSize;
		}

		public int GetWidth() {
			return fullSize.Width;
		}

		public int GetHeight() {
			return fullSize.Height;
		}

		public MapTile this[int x, int y] {
			get {
				if (x < fullSize.Width && y < fullSize.Height)
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
			int dy = (rx + ry - fullSize.Width - 1) / 2;
			return tiles[dx, dy];
		}

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

		public MapTile GetNeighbourTile(MapTile t, TileDirection tileDirection) {
			// find index for t
			int x = t.Dx;
			int y = (t.Dy + (t.Dx + 1) % 2) / 2;
			Debug.Assert(tiles[x, y] == t);
			return GetNeighbourTile(x, y, tileDirection);
		}

		public MapTile GetNeighbourTile(int x, int y, TileDirection direction) {
			switch (direction) {
				// in non-diagonal direction we don't need to check odd/evenness of x
				case TileDirection.Bottom:
					if (y >= fullSize.Height - 1) return null;
					return this[x, y + 1];

				case TileDirection.Top:
					if (y < 2) return null;
					return this[x, y - 1];

				case TileDirection.Left:
					if (x < 2) return null;
					return this[x - 2, y];

				case TileDirection.Right:
					if (x >= fullSize.Width - 1) return null;
					return this[x + 2, y];
			}

			// the horizontally neighbouring tiles have dy' = dy + 1 if x is odd,
			// and the horizontally neighbouring tiles have dy' = dy - 1 if x is even,
			y += x % 2;
			switch (direction) {
				case TileDirection.BottomLeft:
					if (x < 1 || y >= fullSize.Height) return null;
					return this[x - 1, y];

				case TileDirection.BottomRight:
					if (x >= fullSize.Width - 1 || y >= fullSize.Height) return null;
					return this[x + 1, y];

				case TileDirection.TopLeft:
					if (x < 1 || y < 1) return null;
					return this[x - 1, y - 1];

				case TileDirection.TopRight:
					if (y < 1 || x >= fullSize.Width - 1) return null;
					return this[x + 1, y - 1];
			}
			throw new InvalidOperationException();
		}

		public IEnumerator<MapTile> GetEnumerator() {
			return new TwoDimensionalEnumerator<MapTile>(tiles);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return new TwoDimensionalEnumerator<MapTile>(tiles);
		}

		class TwoDimensionalEnumerator<T> : IEnumerator<T> {
			T[,] array;
			int curX, curY;
			public TwoDimensionalEnumerator(T[,] array) {
				this.array = array;
				Reset();
			}
			public bool MoveNext() {
				curX++;
				if (curX == array.GetLength(0)) {
					curX = 0;
					curY++;
				}
				return curY < array.GetLength(1);
			}
			public void Reset() {
				this.curX = this.curY = 0;
			}
			T IEnumerator<T>.Current {
				get {
					return array[curX, curY];
				}
			}
			object IEnumerator.Current {
				get { return array[curX, curY]; }
			}
			public void Dispose() { }

		}

	}

}
