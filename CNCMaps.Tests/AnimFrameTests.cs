using CNCMaps.Engine.Game;
using CNCMaps.Engine.Types;
using Xunit;

namespace CNCMaps.Tests {

	/// <summary>SimulateAnimStage replays gamemd's AnimClass tick logic; the flag/fountain cases
	/// are pinned by pixel-matched frozen-logic captures (see tools/ab/HUMANEVAL.md).</summary>
	public class AnimFrameTests {

		private static Animation Anim(int rate = 900, int loopStart = 0, int loopEnd = 0,
			int loopCount = 0, int start = 0, int end = 0, bool shadow = false, bool normalized = false) {
			return new Animation("test") {
				Rate = rate, LoopStart = loopStart, LoopEnd = loopEnd, LoopCount = loopCount,
				Start = start, End = end, Shadow = shadow, Normalized = normalized,
			};
		}

		[Theory]
		// CAUSFGL_A flag: Rate=250 (delay 3), LoopEnd=15, LoopCount=-1, Shadow -> stage 2 at capture frame 6
		[InlineData(250, 15, 6, 2)]
		// CAWSH18A fountain: Rate=220 (delay 4), LoopEnd=10, LoopCount=-1, Shadow -> stage 2 at 6, stage 1 at 5
		[InlineData(220, 10, 6, 2)]
		[InlineData(220, 10, 5, 1)]
		public void CalibratedCaptureFrames(int rate, int loopEnd, int captureFrame, int expected) {
			var art = Anim(rate: rate, loopEnd: loopEnd, loopCount: -1, shadow: true);
			Assert.Equal(expected, FrameDeciders.SimulateAnimStage(art, 64, captureFrame));
		}

		[Fact]
		public void LoopsWrapToLoopStart() {
			// delay 1, 4-frame loop: 8 ticks = stages 1,2,3,wrap->0,1,2,3,wrap->0
			var art = Anim(loopEnd: 4, loopCount: -1);
			Assert.Equal(0, FrameDeciders.SimulateAnimStage(art, 4, 6));
			Assert.Equal(1, FrameDeciders.SimulateAnimStage(art, 4, 7));
		}

		[Fact]
		public void SingleLoopExpires() {
			// LoopCount default (one loop), 4 frames: expired well before 8 ticks -> out-of-range index
			var art = Anim();
			Assert.Equal(4, FrameDeciders.SimulateAnimStage(art, 4, 6));
		}

		[Fact]
		public void ZeroRateStaysAtStart() {
			var art = Anim(rate: 0, start: 3);
			Assert.Equal(3, FrameDeciders.SimulateAnimStage(art, 16, 6));
		}
	}
}
