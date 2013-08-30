using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace CNCMaps.MapLogic {

	public enum OverlayType {
		Riparius = 0, // note: don't change the indices of 0-3! they're hardcoded in the game too!
		Cruentus = 1,
		Vinifera = 2,
		Aboreus = 3,
		Ore = 4,
		Gems = 5,
		NotSpecial = -1,
	};

	public static class SpecialOverlays {
		public const byte MaxOreID = 127;
		public const byte MinOreID = 102;
		public const byte MaxGemsID = 38;
		public const byte MinGemsID = 27;

		public static bool IsOre(OverlayObject o) {
			return o.OverlayID >= MinOreID && o.OverlayID <= MaxOreID;
		}
		public static bool IsGem(OverlayObject o) {
			return o.OverlayID >= MinGemsID && o.OverlayID <= MaxGemsID;
		}
		public static bool IsOreOrGem(OverlayObject o) {
			return IsOre(o) || IsGem(o);
		}

		public const byte Min_ID_Riparius = 102;
		public const byte Max_ID_Riparius = 121;
		public const byte Min_ID_Vinifera = 127;
		public const byte Max_ID_Vinifera = 146;
		public const byte Min_ID_Cruentus = 27;
		public const byte Max_ID_Cruentus = 38;
		public const byte Min_ID_Aboreus = 147;
		public const byte Max_ID_Aboreus = 166;

		public static bool IsTib_Riparius(OverlayObject o) {
			return o.OverlayID >= Min_ID_Riparius && o.OverlayID <= Max_ID_Riparius;
		}
		public static bool IsTib_Cruentus(OverlayObject o) {
			return o.OverlayID >= Min_ID_Cruentus && o.OverlayID <= Max_ID_Cruentus;
		}
		public static bool IsTib_Vinifera(OverlayObject o) {
			return o.OverlayID >= Min_ID_Vinifera && o.OverlayID <= Max_ID_Vinifera;
		}
		public static bool IsTib_Aboreus(OverlayObject o) {
			return o.OverlayID >= Min_ID_Aboreus && o.OverlayID <= Max_ID_Aboreus;
		}
		public static bool IsTib(OverlayObject o) {
			return IsTib_Riparius(o) || IsTib_Cruentus(o) || IsTib_Vinifera(o) || IsTib_Aboreus(o);
		}
		public static bool IsHighBridge(OverlayObject o) {
			return o.OverlayID == 24 || o.OverlayID == 25 || o.OverlayID == 238 || o.OverlayID == 237;
		}
		public static bool IsTSRails(OverlayObject o) {
			return 43 <= o.OverlayID && o.OverlayID <= 57;
		}
		public static bool IsOreOrGemOrTib(OverlayObject o) {
			return IsOre(o) || IsGem(o) || IsTib(o);
		}

		public static OverlayType GetOverlayType(OverlayObject o, EngineType engine) {
			if (engine <= EngineType.FireStorm) {
				if (IsTib_Riparius(o)) return OverlayType.Riparius;
				else if (IsTib_Cruentus(o)) return OverlayType.Cruentus;
				else if (IsTib_Vinifera(o)) return OverlayType.Vinifera;
				else if (IsTib_Aboreus(o)) return OverlayType.Aboreus;
			}
			else {
				if (IsOre(o)) return OverlayType.Ore;
				else if (IsGem(o)) return OverlayType.Gems;
			}
			return OverlayType.NotSpecial;
		}
	}


}
