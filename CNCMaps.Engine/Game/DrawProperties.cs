using System;
using System.Drawing;
using CNCMaps.Engine.Map;
using CNCMaps.Shared;

namespace CNCMaps.Engine.Rendering {
	public enum DrawFrame : int {
		DirectionBased = -1,
		Random = -2,
		//RandomHealthy = -3,
		//Damaged = -4,
	};

	public class DrawProperties {
		public bool HasShadow { get; set; }
		public bool IsUnderWater { get; set; }

		public PaletteType PaletteType { get; set; }
		public LightingType LightingType { get; set; }
		public string CustomPaletteName { get; set; }
		public Palette PaletteOverride { get; set; } // if palettetype should be ignored
		public Point ZShapePointMove { get; set; }

		//private Func<GameObject, int> CachedFrameDecider;
		public Func<GameObject, int> FrameDecider {
			get; set;
			//	get { return CachedFrameDecider; }
			//	set { CachedFrameDecider = FrameDeciders.CreateCacher(value); }
		}
		public Func<GameObject, Point> OffsetHack { get; set; } // used to reposition bridges based on their overlay value
		public Func<GameObject, Point> ShadowOffsetHack { get; set; } // used to reposition bridges based on their overlay value
		public Point Offset;
		public Point ShadowOffset;
		public int SortIndex { get; set; }
		public float TurretVoxelOffset { get; set; }

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

		public DrawProperties Clone() {
			return (DrawProperties)MemberwiseClone();
		}

		public int ZAdjust { get; set; }
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
				return new Point(0, -1);
			else
				return new Point(-15, -9);
		};

		public static Func<GameObject, Point> TSBridgeOffsets = delegate(GameObject obj) {
			var bridgeOvl = obj as OverlayObject;
			if (bridgeOvl.OverlayValue <= 8)
				return new Point(0, -1);
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