using CNCMaps.Engine.Rendering;
using CNCMaps.Shared;
using Xunit;

namespace CNCMaps.Tests {

	/// <summary>An object standing on a ramp is lifted onto the slope surface. The game reaches the
	/// lift through Tactical::Z_Lepton_To_Pixel, so a half level rounds either side of the .5
	/// boundary depending on the cell's own height; the RA2/YR band matches gamemd captures
	/// (Cold War, TREE10 on a height-8 ramp).</summary>
	public class RampHeightTests {

		// a local config, not the shared ModConfig defaults, so this class cannot disturb the
		// golden renders it runs alongside; the lift only reads Engine and the tile size
		private static ModConfig Config(EngineType engine) => new ModConfig { Engine = engine };

		[Theory]
		[InlineData(0, 5, 0)]       // flat cell, no lift at any height
		[InlineData(6, 5, 0)]       // opposing corners cancel at the cell centre
		[InlineData(4, 0, 7)]
		[InlineData(4, 6, 7)]
		[InlineData(4, 7, 8)]       // Z_Lepton_To_Pixel's fudge kicks in at 7 levels
		[InlineData(4, 8, 8)]
		[InlineData(4, 13, 8)]
		[InlineData(4, 14, 7)]
		[InlineData(18, 8, 8)]      // the flat-topped halves lift like a single-direction slope
		[InlineData(11, 8, 15)]     // raised corner pairs stand a whole level up
		public void YrHalfLevelRounding(int ramp, int height, int expected) {
			Assert.Equal(expected, RampHeight.PixelLift(ramp, height, Config(EngineType.YurisRevenge)));
		}

		[Theory]
		[InlineData(4, 0, 6)]
		[InlineData(4, 8, 6)]
		[InlineData(4, 9, 6)]       // TS's fudge threshold keeps the half level at 6 throughout
		[InlineData(4, 14, 6)]
		[InlineData(11, 9, 12)]
		public void TsHalfLevelIsConstant(int ramp, int height, int expected) {
			Assert.Equal(expected, RampHeight.PixelLift(ramp, height, Config(EngineType.TiberianSun)));
		}
	}
}
