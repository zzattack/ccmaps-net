using System;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;

namespace CNCMaps.MapLogic {
	public enum DrawFrame : int {
		DirectionBased = -1,
		Random = -2,
	};

	public class DrawProperties {
		public bool HasShadow { get; set; }
		public Palette PaletteOverride { get; set; }
		public Func<GameObject, int> FrameDecider { get; set; }
		public Func<GameObject, Point> OffsetHack { get; set; } // used to reposition bridges based on their overlay value
		public Func<GameObject, Point> ShadowOffsetHack { get; set; } // used to reposition bridges based on their overlay value
		public Point Offset { private get; set; }
		public Point ShadowOffset { private get; set; }
		
		public Point GetOffset(GameObject obj) {
			var ret = Offset;
			if (OffsetHack != null)
				ret.Offset(OffsetHack(obj));
			return ret;
		}
		public Point GetShadowOffset(GameObject obj) {
			var ret = Offset;
			if (ShadowOffsetHack != null)
				ret.Offset(ShadowOffsetHack(obj));
			return ret;
		}

		public int SortIndex { get; set; }
		public bool OverridesZbuffer { get; set; }

		public DrawProperties() {
			FrameDecider = FrameDeciders.NullFrameDecider;
		}

	}

	internal static class FrameDeciders {
		public static Func<GameObject, int> TurretFrameDecider = delegate(GameObject obj) {
			int direction = (obj is InfantryObject) ? (obj as InfantryObject).Direction : 0;
			switch (direction) {
				case 0:
					return 28;
				case 32:
					return 24;
				case 64:
					return 20;
				case 96:
					return 16;
				case 128:
					return 12;
				case 160:
					return 8;
				case 192:
					return 4;
				default:
					return 0;
			}
		};

		public static Func<GameObject, int> HealthBasedFrameDecider = delegate(GameObject obj) {
			int health = (obj is OwnableObject) ? (obj as OwnableObject).Health : 255;
			if (health >= 128) return 0;
			else return 1;
		};

		public static Func<GameObject, int> RandomFrameDecider = delegate(GameObject obj) {
			return (int) DrawFrame.Random; // this is delegated to SHP drawing level
		};

		public static Func<GameObject, int> DirectionBasedFrameDecider = delegate(GameObject obj) {
			int direction = (obj as OwnableObject).Direction;
			return direction / 32;
		};

		public static Func<GameObject, int> OverlayValueFrameDecider = delegate(GameObject obj) {
			return (obj as OverlayObject).OverlayValue;
		};

		public static Func<GameObject, int> NullFrameDecider = arg => 0;


	}

	internal static class OffsetHacks {
		public static Func<GameObject, Point> RA2BridgeOffsets = delegate(GameObject obj) {
			var bridgeOvl = obj as OverlayObject;
			if (bridgeOvl.OverlayValue <= 8)
				return new Point(0, -1);
			else
				return new Point(0, -16);
		};
		
		public static Func<GameObject, Point> RA2BridgeShadowOffsets = delegate(GameObject obj) {
			var bridgeOvl = obj as OverlayObject;
			if (bridgeOvl.OverlayValue <= 8)
				return new Point(0, -15);
			else
				return new Point(-1, -9);
		};

		public static Func<GameObject, Point> TSBridgeOffsets = delegate(GameObject obj) {
			var bridgeOvl = obj as OverlayObject;
			if (bridgeOvl.OverlayValue <= 8)
				return new Point(0, 0);
			else
				return new Point(0, -13);
		};
		
		public static Func<GameObject, Point> TSBridgeShadowOffsets = delegate(GameObject obj) {
			var bridgeOvl = obj as OverlayObject;
			if (bridgeOvl.OverlayValue <= 8)
				return new Point(0, -1);
			else
				return new Point(-15, -9);
		};
		
	}
}