using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using CNCMaps.FileFormats.Encodings;
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

		public void SerializeIsoMapPack5(IniFile.IniSection isoMapPack5) {
			int cells = (Width * 2 - 1) * Height;
			int lzoPackSize = cells * 11 + 4; // last 4 bytes contains a lzo pack header saying no more data is left
			
			var isoMapPack = new byte[lzoPackSize];
			var isoMapPack2 = new byte[lzoPackSize];
			long di = 0;
			foreach (var tile in this.isoTiles) {
				var bs = tile.ToMapPack5Entry().ToArray();
				Array.Copy(bs, 0, isoMapPack, di, 11);
				di += 11;
			}

			var compressed = Format5.Encode(isoMapPack, 5);
			string compressed64 = Convert.ToBase64String(compressed);
			
			int i = 1;
			int idx = 0;
			isoMapPack5.Clear();
			while (idx < compressed64.Length) {
				int adv = Math.Min(74, compressed64.Length - idx);
				isoMapPack5.SetValue(i++.ToString(), compressed64.Substring(idx, adv));
				idx += adv;
			}
		}

	}

}
