using System;

namespace CNCMaps.Shared.Utility {
	public class Rand {
		private const int Seed = 32846238;
		private static Random r = new Random(Seed);

		/// <summary>
		/// When set, every draw yields its first option instead of a random one. The sequence is
		/// shared by all callers, so adding or removing one object shifts every later draw; pinning
		/// removes that coupling for A/B renders against an engine capture.
		/// </summary>
		public static bool Pinned { get; set; }

		/// <summary>
		/// Restarts the deterministic sequence. Called at the start of every render so
		/// output does not depend on how many renders ran earlier in the same process.
		/// </summary>
		public static void Reset() {
			r = new Random(Seed);
		}

		public static int Next() {
			return Pinned ? 0 : r.Next();
		}
		public static int Next(int maxValue) {
			return Pinned ? 0 : r.Next(maxValue);
		}
		public static double NextDouble() {
			return Pinned ? 0.0 : r.NextDouble();
		}
	}
}
