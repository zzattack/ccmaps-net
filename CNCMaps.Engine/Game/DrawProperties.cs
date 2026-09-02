using System;
using System.Drawing;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.Shared;

namespace CNCMaps.Engine.Game {
	public enum DrawFrame : int {
		DirectionBased = -1,
		Random = -2,
		//RandomHealthy = -3,
		//Damaged = -4,
	};

	public class DrawProperties {
		public bool HasShadow { get; set; }
		public bool Cloakable { get; set; }

		public PaletteType PaletteType { get; set; }
		public LightingType LightingType { get; set; }
		public string CustomPaletteName { get; set; }
		public Palette PaletteOverride { get; set; } // if palettetype should be ignored
		public Point ZShapePointMove { get; set; }

		public Func<GameObject, int> FrameDecider { get; set; }
		public Func<GameObject, Point> OffsetHack { get; set; } // used to reposition bridges based on their overlay value
		public Func<GameObject, Point> ShadowOffsetHack { get; set; } // used to reposition bridges based on their overlay value
		public Point Offset;
		public Point ShadowOffset;
		public int SortIndex { get; set; }
		public float TurretVoxelOffset { get; set; }
		public int FlightHeight { get; set; } // pixels above the ground plane; body only, shadow stays grounded

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
		// An [Infantry] entry's sub-cell picks one of the engine's StoppingCoordAbs spots, a quarter
		// cell (64 leptons) off the cell centre. gamemd draws 0 and 1 at the centre, 2 up-right, 3
		// down-left and 4 down; TS keeps the five-spot table with 1 up-left (OpenTS const.cpp).
		public static Func<GameObject, Point> InfantrySubCell(ModConfig config) => obj => {
			int lx = 0, ly = 0;
			switch ((obj as InfantryObject)?.SubCell ?? 0) {
				case 1: if (config.Engine <= EngineType.Firestorm) { lx = -64; ly = -64; } break;
				case 2: lx = 64; ly = -64; break;
				case 3: lx = -64; ly = 64; break;
				case 4: lx = 64; ly = 64; break;
			}
			return new Point((lx - ly) * config.TileWidth / 512, (lx + ly) * config.TileHeight / 512);
		};

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
