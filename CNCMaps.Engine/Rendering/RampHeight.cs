using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Map;

namespace CNCMaps.Engine.Rendering {

	/// <summary>Ground height inside a sloped cell.</summary>
	/// <remarks>
	/// A ramp cell slopes across its own width, so its stored height only describes one corner.
	/// Anything standing on it sits on the slope, and the game samples the surface at the object's
	/// own position (CellClass::Get_Height). Objects are drawn from the centre of their cell, so the
	/// table below is the game's ramp control table evaluated there: the x and y terms each
	/// contribute half a level at the centre, leaving base + extra + (xchange + ychange) * level/2,
	/// clamped to the ramp's maximum.
	/// </remarks>
	static class RampHeight {

		// Height above the cell's own level at the cell centre, in half-levels: 0, 1 or 2.
		// Index is the tile's RampType; ramps 5-8 cancel out and need no lift at all.
		private static readonly byte[] HalfLevelsAtCentre = {
			0,              // 0  flat
			1, 1, 1, 1,     // 1-4   single-direction slopes
			0, 0, 0, 0,     // 5-8   opposing corner pairs, which cancel at the centre
			2, 2, 2, 2,     // 9-12  raised corner pairs
			2, 2, 2, 2,     // 13-16 double-height variants, still one level at the centre
			1, 1, 1, 1,     // 17-20 the flat-topped halves
		};

		/// <summary>Pixels to lift an object standing on this tile, above the tile's own height.</summary>
		public static int PixelLift(MapTile tile, int tileHeight) {
			if (tile == null) return 0;
			int ramp = (tile.Drawable as TileDrawable)?.GetTileImage(tile)?.RampType ?? 0;
			if (ramp <= 0 || ramp >= HalfLevelsAtCentre.Length) return 0;
			// one height level is tileHeight/2 pixels, so a half level is tileHeight/4
			return HalfLevelsAtCentre[ramp] * tileHeight / 4;
		}
	}
}
