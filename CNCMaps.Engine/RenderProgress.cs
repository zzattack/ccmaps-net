using System;

namespace CNCMaps.Engine {
	/// <summary>
	/// Monotonic 0-100 progress over a whole render. Phase boundaries are fixed
	/// percentages taken from measured renders; within the drawing and encoding
	/// phases progress follows real units (rows drawn, blocks compressed), so the
	/// bar never guesses from wall-clock time.
	/// </summary>
	public class RenderProgress {
		private readonly Action<int, string> _sink;
		private readonly object _lock = new object();
		private int _last = -1;

		/// <summary>Where the drawing phase ends and encoding begins: 100 minus 5 for a
		/// JPEG output and 10 for a PNG output, so encoding gets a share of the bar that
		/// matches its real cost.</summary>
		public int DrawEnd { get; }

		public RenderProgress(Action<int, string> sink, int drawEnd = 90) {
			_sink = sink;
			DrawEnd = drawEnd;
		}

		public void Report(int percent, string phase) {
			if (_sink == null) return;
			if (percent > 100) percent = 100;
			lock (_lock) {
				if (percent <= _last) return;
				_last = percent;
			}
			_sink(percent, phase);
		}

		/// <summary>Reports fraction <paramref name="frac"/> of the span [from, to].</summary>
		public void Span(int from, int to, double frac, string phase) {
			if (frac < 0) frac = 0;
			else if (frac > 1) frac = 1;
			Report(from + (int)((to - from) * frac), phase);
		}
	}
}
