using System.Drawing;

namespace CNCMaps.MapLogic {
	public class DrawProperties {
		public Point offset;
		public bool hasShadow;
		public int ySort;

		public DrawProperties(Point offset, bool hasShadow, int ySort) {
			this.offset = offset;
			this.hasShadow = hasShadow;
			this.ySort = ySort;
		}
	}
}