using CNCMaps.Engine.Map;
using CNCMaps.Shared;

namespace CNCMaps.Engine.Game {

	public enum OverlayTibType {
		Riparius = 0, // note: don't change the indices of 0-3! they're hardcoded in the game too!
		Cruentus = 1,
		Vinifera = 2,
		Aboreus = 3,
		Ore = 0, // ts: rip
		Gems = 1, // ts: cru
		NotSpecial = -1,
	};

	public static class SpecialOverlays {
		// Riparius = 1, Cruentus = 2, Vinifera = 3, Aboreus = 4
		public const byte Ra2MinIdRiparius = 102; // Ore
		public const byte Ra2MaxIdRiparius = 127; // Ore
		public const byte Ra2MinIdCruentus = 27; // Gems
		public const byte Ra2MaxIdCruentus = 38; // Gems
		public const byte Ra2MinIdVinifera = 127;
		public const byte Ra2MaxIdVinifera = 146;
		public const byte Ra2MinIdAboreus = 147;
		public const byte Ra2MaxIdAboreus = 166;

		public const byte TsMinIdRiparius = 102;
		public const byte TsMaxIdRiparius = 121;
		public const byte TsMinIdCruentus = 27;
		public const byte TsMaxIdCruentus = 38;
		public const byte TsMinIdVinifera = 127;
		public const byte TsMaxIdVinifera = 146;
		public const byte TsMinIdAboreus = 147;
		public const byte TsMaxIdAboreus = 166;

		private static bool IsRA2_Riparius(OverlayObject o) {
			return o.OverlayID >= Ra2MinIdRiparius && o.OverlayID <= Ra2MaxIdRiparius;
		}
		private static bool IsRA2_Cruentus(OverlayObject o) {
			return o.OverlayID >= Ra2MinIdCruentus && o.OverlayID <= Ra2MaxIdCruentus;
		}
		private static bool IsRA2_Vinifera(OverlayObject o) {
			return o.OverlayID >= Ra2MinIdVinifera && o.OverlayID <= Ra2MaxIdVinifera;
		}
		private static bool IsRA2_Aboreus(OverlayObject o) {
			return o.OverlayID >= Ra2MinIdAboreus && o.OverlayID <= Ra2MaxIdAboreus;
		}

		private static bool IsTS_Riparius(OverlayObject o) {
			return o.OverlayID >= TsMinIdRiparius && o.OverlayID <= TsMaxIdRiparius;
		}
		private static bool IsTS_Cruentus(OverlayObject o) {
			return o.OverlayID >= TsMinIdCruentus && o.OverlayID <= TsMaxIdCruentus;
		}
		private static bool IsTS_Vinifera(OverlayObject o) {
			return o.OverlayID >= TsMinIdVinifera && o.OverlayID <= TsMaxIdVinifera;
		}
		private static bool IsTS_Aboreus(OverlayObject o) {
			return o.OverlayID >= TsMinIdAboreus && o.OverlayID <= TsMaxIdAboreus;
		}
		private static bool IsTib(OverlayObject o) {
			return IsTS_Riparius(o) || IsTS_Cruentus(o) || IsTS_Vinifera(o) || IsTS_Aboreus(o);
		}
		
		public static bool IsHighBridge(OverlayObject o) {
			return o.OverlayID == 24 || o.OverlayID == 25 || o.OverlayID == 238 || o.OverlayID == 237;
		}
		public static bool IsTSHighRailsBridge(OverlayObject o) {
			return o.OverlayID == 59 || o.OverlayID == 60;
		}

		public static OverlayTibType GetOverlayTibType(OverlayObject o, EngineType engine) {
			if (engine <= EngineType.Firestorm) {
				if (IsTS_Riparius(o)) return OverlayTibType.Riparius;
				else if (IsTS_Cruentus(o)) return OverlayTibType.Cruentus;
				else if (IsTS_Vinifera(o)) return OverlayTibType.Vinifera;
				else if (IsTS_Aboreus(o)) return OverlayTibType.Aboreus;
			}
			else {
				if (IsRA2_Riparius(o)) return OverlayTibType.Riparius;
				else if (IsRA2_Cruentus(o)) return OverlayTibType.Cruentus;
				else if (IsRA2_Vinifera(o)) return OverlayTibType.Vinifera;
				else if (IsRA2_Aboreus(o)) return OverlayTibType.Aboreus;
			}
			return OverlayTibType.NotSpecial;
		}

		internal static string GetTibName(OverlayObject o, EngineType engine) {
			if (engine <= EngineType.Firestorm) {
				if (IsTS_Riparius(o)) return "Riparius";
				else if (IsTS_Cruentus(o)) return "Cruentus";
				else if (IsTS_Vinifera(o)) return "Vinifera";
				else if (IsTS_Aboreus(o)) return "Aboreus";
			}
			else {
				if (IsRA2_Riparius(o)) return "Riparius";
				else if (IsRA2_Cruentus(o)) return "Cruentus";
				else if (IsRA2_Vinifera(o)) return "Vinifera";
				else if (IsRA2_Aboreus(o)) return "Aboreus";
			}
			return "";
		}
	}


}
