using System.Drawing;

namespace CNCMaps.MapLogic {
	public class DrawProperties {
		public Point offset, shadowOffset;
		public bool hasShadow;
		public int ySort;
		
		public DrawProperties() {
		}

		public DrawProperties(Point offset, bool hasShadow = false, int ySort = 0) {
			this.offset = offset;
			this.shadowOffset = offset;
			this.hasShadow = hasShadow;
			this.ySort = ySort;
		}

		public DrawProperties(Point offset, Point shadowOffset, bool hasShadow = false, int ySort = 0) {
			this.offset = offset;
			this.shadowOffset = shadowOffset;
			this.hasShadow = hasShadow;
			this.ySort = ySort;
		}

	}
}