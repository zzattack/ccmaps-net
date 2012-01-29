namespace CNCMaps.MapLogic {
	public class OverlayObject : NumberedObject, RemappableObject {
		public byte OverlayID { get { return (byte)Number; } set { Number = value; } }
		public byte OverlayValue { get; set; }

		public OverlayObject(byte overlayID, byte overlayValue) {
			OverlayID = overlayID;
			OverlayValue = overlayValue;
		}

		public const byte MaxOreID = 127;
		public const byte MinOreID = 102;
		public const byte MaxGemsID = 38;
		public const byte MinGemsID = 27;
		public bool IsOre() {
			return OverlayID >= MinOreID && OverlayID <= MaxOreID;
		}

		public bool IsGem() {
			return OverlayID >= MinGemsID && OverlayID <= MaxGemsID;
		}
		public bool IsOreOrGem() {
			return IsOre() || IsGem();
		}
		public bool IsHighBridge() {
			return OverlayID == 24 || OverlayID == 25 || OverlayID == 238 || OverlayID == 237;
		}
		public Palette Palette { get; set; }

	}
}