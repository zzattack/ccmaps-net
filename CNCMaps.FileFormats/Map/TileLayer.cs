using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using CNCMaps.Shared.Utility;

namespace CNCMaps.FileFormats.Map {
	public class TileLayer : IEnumerable<IsoTile> {
		IsoTile[,] isoTiles;
		private Size fullSize;

		public TileLayer(int w, int h)
			: this(new Size(w, h)) {
		}

		public TileLayer(Size fullSize) {
			this.fullSize = fullSize;
			isoTiles = new IsoTile[fullSize.Width * 2 - 1, fullSize.Height];
		}

		public int Width {
			get { return fullSize.Width; }
		}

		public int Height {
			get { return fullSize.Height; }
		}

		public virtual IsoTile this[int x, int y] {
			get {
				if (0 <= x && x < isoTiles.GetLength(0) && 0 <= y && y < isoTiles.GetLength(1))
					return isoTiles[x, y];
				else
					return null;
			}
			set {
				isoTiles[x, y] = value;
			}
		}

		/// <summary>Gets a tile at display coordinates.</summary>
		/// <param name="dx">The dx.</param>
		/// <param name="dy">The dy.</param>
		/// <returns>The tile.</returns>
		public IsoTile GetTile(int dx, int dy) {
			return isoTiles[dx, dy];
		}

		/// <summary>Gets a tile at map coordinates.</summary>
		/// <param name="rx">The rx.</param>
		/// <param name="ry">The ry.</param>
		/// <returns>The tile r.</returns>
		public IsoTile GetTileR(int rx, int ry) {
			int dx = (rx - ry + fullSize.Width - 1);
			int dy = rx + ry - fullSize.Width - 1;

			if (dx < 0 || dy < 0 || dx >= isoTiles.GetLength(0) || (dy / 2) >= isoTiles.GetLength(1))
				return null;
			else
				return GetTile(dx, dy / 2);
		}

		#region enumerator stuff
		public IEnumerator<IsoTile> GetEnumerator() {
			return new TwoDimensionalEnumerator<IsoTile>(isoTiles);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return new TwoDimensionalEnumerator<IsoTile>(isoTiles);
		}
		#endregion

	}

}
