namespace CNCMaps.Rendering {
	public enum Axis { X, Y, Z, None };

	public class Hexagon {
		public int xMin, xMax;
		public int yMin, yMax;
		public int zMin, zMax;

		public static Axis GetSeparationAxis(Hexagon a, Hexagon b) {
			if (RangesDisjoint(a.zMin, a.zMax, b.zMin, b.zMax)) {
				return Axis.Z;
			}
			if (RangesDisjoint(a.yMin, a.yMax, b.yMin, b.yMax)) {
				return Axis.Y;
			}
			if (RangesDisjoint(a.xMin, a.xMax, b.xMin, b.xMax)) {
				return Axis.X;
			}
			return Axis.None;
		}

		public static bool RangesDisjoint(int aMin, int aMax, int bMin, int bMax) {
			return (aMax < bMin || bMax < aMin);
		}
	}
}
