using System;
using System.Collections.Generic;

namespace CNCMaps.MapLogic {

	public class MapTile {

		public ushort Dx { get; private set; }

		public ushort Dy { get; private set; }

		public ushort Rx { get; private set; }

		public ushort Ry { get; private set; }

		public short Z { get; private set; }

		public short TileNum { get; set; }

		public short SetNum { get; set; }

		public ushort SubTile { get; private set; }

		List<RA2Object> AllObjects = new List<RA2Object>();

		public Palette Palette { get; set; }

		public bool PaletteIsOriginal { get; set; }

		public MapTile(ushort dx, ushort dy, ushort rx, ushort ry, short rz, short tilenum, ushort subtile, short setnum = 0) {
			Dx = dx;
			Dy = dy;
			Rx = rx;
			Ry = ry;
			Z = rz;
			TileNum = tilenum;
			SetNum = setnum;
			SubTile = subtile;
		}

		internal void AddObject(RA2Object obj) {
			AllObjects.Add(obj);
			obj.Tile = this;
		}

		/// <summary>
		/// Applies a lamp to this object's palette if it's in range
		/// </summary>
		/// <param name="lamp">The lamp to apply</param>
		/// <returns>Whether the palette was replaced, meaning it needs to be recalculated</returns>
		public virtual bool ApplyLamp(LightSource lamp) {
			if (lamp.LightIntensity == 0.0)
				return false;

			double sqX = (lamp.Tile.Rx - Rx) * (lamp.Tile.Rx - Rx);
			double sqY = (lamp.Tile.Ry - (Ry)) * (lamp.Tile.Ry - (Ry));

			double distance = Math.Sqrt(sqX + sqY);

			// checks whether we're in range
			if ((0 < lamp.LightVisibility) && (distance < lamp.LightVisibility / 256)) {
				double lsEffect = (lamp.LightVisibility - 256 * distance) / lamp.LightVisibility;
				// make sure we copy the palette only once
				if (PaletteIsOriginal) {
					Palette = Palette.Clone();
					PaletteIsOriginal = false;
				}
				Palette.ApplyLamp(lamp, lsEffect);
				return true;
			}
			else
				return false;
		}

		public override string ToString() {
			return string.Format("d({0},{1}),r({2},{3})", Dx, Dy, Rx, Ry);
		}
	}
}