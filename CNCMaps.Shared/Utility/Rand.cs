using System;

namespace CNCMaps.Shared.Utility {
	public class Rand {
		private static readonly Random r = new Random(32846238);
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
