using System;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Map;
using CNCMaps.Shared;

namespace CNCMaps.Engine.Rendering {

	/// <summary>Ground height inside a sloped cell.</summary>
	/// <remarks>
	/// A ramp cell slopes across its own width, so its stored height only describes one corner.
	/// Anything standing on it sits on the slope, and the game samples the surface at the object's
	/// own position (CellClass::Get_Height). Objects are drawn from the centre of their cell, so the
	/// table below is the game's ramp control table evaluated there: the x and y terms each
	/// contribute half a level at the centre, leaving base + extra + (xchange + ychange) * level/2,
	/// clamped to the ramp's maximum.
	///
	/// Tiles and overlays step by a whole pixel level per height (CellClass::Draw_It uses
	/// LEVEL_PIXEL_H * Height), but an object's lift comes from its lepton height through
	/// Z_Lepton_To_Pixel (gamemd 0x6D20E0). The two only agree because of that routine's fudge
	/// term, and a half level rounds either side of the .5 boundary depending on the cell's own
	/// height: in RA2/YR a half level is 7 px up to height 6 and from 14, but 8 px between. TS
	/// never crosses the boundary.
	/// </remarks>
	static class RampHeight {

		// One height level in leptons: (int)(tan(30 deg) * cell diagonal / 2).
		private const int LevelLeptons = 104;

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
		public static int PixelLift(MapTile tile, ModConfig config) {
			if (tile == null) return 0;
			int ramp = (tile.Drawable as TileDrawable)?.GetTileImage(tile)?.RampType ?? 0;
			return PixelLift(ramp, tile.Z, config);
		}

		public static int PixelLift(int rampType, int cellHeight, ModConfig config) {
			if (rampType <= 0 || rampType >= HalfLevelsAtCentre.Length) return 0;
			int half = HalfLevelsAtCentre[rampType];
			if (half == 0) return 0;
			int ground = LevelLeptons * cellHeight;
			return ZPixel(ground + half * (LevelLeptons / 2), config) - ZPixel(ground, config);
		}

		/// <summary>Vertical pixel lift of a height in leptons (Tactical::Z_Lepton_To_Pixel).</summary>
		private static int ZPixel(int leptons, ModConfig config) {
			double perLepton = Math.Sin(Math.PI / 3.0) * config.TileWidth / (256.0 * Math.Sqrt(2.0));
			// the game nudges tall heights up by a pixel so that whole levels keep landing on
			// the tile grid; the threshold is 7 levels in RA2/YR and 9 in TS
			int fudgeAbove = config.Engine == EngineType.TiberianSun || config.Engine == EngineType.Firestorm
				? 9 * LevelLeptons : 7 * LevelLeptons;
			int fudge = leptons >= fudgeAbove ? 1 : 0;
			return (int)(leptons * perLepton + fudge + 0.5);
		}
	}
}
