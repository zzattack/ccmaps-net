using System.Windows.Forms;

namespace CNCMaps.MapLogic {
	public class OverlayObject : NumberedObject {
		public byte OverlayID {
			get { return (byte)Number; }
			set { Number = value; }
		}

		public byte OverlayValue { get; set; }
		public MapTile DrawTile { get; set; }

		public OverlayObject(byte overlayID, byte overlayValue) {
			OverlayID = overlayID;
			OverlayValue = overlayValue;
		}

		public const byte Max_ID_Riparius = 121;
		public const byte Min_ID_Riparius = 102;
		public const byte Max_ID_Vinifera = 146;
		public const byte Min_ID_Vinifera = 127;
		public const byte Max_ID_Cruentus = 38;
		public const byte Min_ID_Cruentus = 27;
		public const byte Max_ID_Aboreus = 166;
		public const byte Min_ID_Aboreus = 147;

		public bool IsOre_Riparius {
			get { return OverlayID >= Min_ID_Riparius && OverlayID <= Max_ID_Riparius; }
		}

		public bool IsOre_Cruentus {
			get { return OverlayID >= Min_ID_Cruentus && OverlayID <= Max_ID_Cruentus; }
		}

		public bool IsOre_Vinifera {
			get { return OverlayID >= Min_ID_Vinifera && OverlayID <= Max_ID_Vinifera; }
		}

		public bool IsOre_Aboreus {
			get { return OverlayID >= Min_ID_Aboreus && OverlayID <= Max_ID_Aboreus; }
		}

		public bool IsOreOverlay {
			get { return IsOre_Riparius || IsOre_Cruentus || IsOre_Vinifera || IsOre_Aboreus; }
		}

		public bool IsHighBridge {
			get { return OverlayID == 24 || OverlayID == 25 || OverlayID == 238 || OverlayID == 237; }
		}

		public Palette Palette { get; set; }

		public bool IsTSRails {
			get { return 43 <= OverlayID && OverlayID <= 57; }
		}
	}
}