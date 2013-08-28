using System.Windows.Forms;

namespace CNCMaps.MapLogic {
	public class OverlayObject : NumberedObject {
		public byte OverlayID {
			get { return (byte) Number; }
			set { Number = value; }
		}

		public byte OverlayValue { get; set; }
		public MapTile DrawTile { get; set; }

		public OverlayObject(byte overlayID, byte overlayValue) {
			OverlayID = overlayID;
			OverlayValue = overlayValue;
		}
	}
}