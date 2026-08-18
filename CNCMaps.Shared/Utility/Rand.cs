using System;

namespace CNCMaps.Shared.Utility {
	public class Rand {
		private const int Seed = 32846238;
		private static Random r = new Random(Seed);

		/// <summary>
		/// Restarts the deterministic sequence. Called at the start of every render so
		/// output does not depend on how many renders ran earlier in the same process.
		/// </summary>
		public static void Reset() {
			r = new Random(Seed);
		}

		public static int Next() {
			return r.Next();
		}
		public static int Next(int maxValue) {
			return r.Next(maxValue);
		}
		public static double NextDouble() {
			return r.NextDouble();
		}
	}
}
