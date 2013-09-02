using System;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using CNCMaps.FileFormats;

namespace CNCMaps.MapLogic {
	public enum DrawFrame : int {
		DirectionBased = -1,
		Random = -2,
		//RandomHealthy = -3,
		//Damaged = -4,
	};

	public class DrawProperties {
		public bool HasShadow { get; set; }
		public Palette PaletteOverride { get; set; }
		public Func<GameObject, int> FrameDecider { get; set; }
		public Func<GameObject, Point> OffsetHack { get; set; } // used to reposition bridges based on their overlay value
		public Func<GameObject, Point> ShadowOffsetHack { get; set; } // used to reposition bridges based on their overlay value
		public Point Offset { private get; set; }
		public Point ShadowOffset { private get; set; }
		public int FirstFrame { get; set; } // for animations
		public int LastFrame { get; set; }

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
		
	}

	internal static class FrameDeciders {
		public static Func<GameObject, int> TurretFrameDecider = delegate(GameObject obj) {
			int direction = (obj is OwnableObject) ? (obj as OwnableObject).Direction : 0;
			switch (direction) {
				case 0: return 28;
				case 32: return 24;
				case 64: return 20;
				case 96: return 16;
				case 128: return 12;
				case 160: return 8;
				case 192: return 4;
				default: return 0;
			}
		};

		/// <summary>
		/// Use this for non-animated building parts that show frame 0 for healthy and frame 1 for damaged buildings
		/// </summary>
		public static Func<GameObject, int> HealthBasedFrameDecider = delegate(GameObject obj) {
			int health = (obj is OwnableObject) ? (obj as OwnableObject).Health : 255;
			if (health >= 128) return 0;
			else return 1;
		};

		/// <summary>
		/// Use this for animations that have a loopstart and loopend
		/// </summary>
		/// <returns>A framedecider between loopend and loopstart</returns>
		public static Func<GameObject, int> LoopFrameDecider(int loopstart, int loopend) {
			return delegate(GameObject obj) {
				// loopstart > loopend is possible
				return Math.Min(loopstart, loopend) + R.Next(Math.Abs(loopend - loopstart));
			};
		}
		public static Random R = new Random();

		public static Func<GameObject, int> RandomFrameDecider = delegate(GameObject obj) {
			return (int)DrawFrame.Random;
		};


		public static Func<GameObject, int> DirectionBasedFrameDecider = delegate(GameObject obj) {
			int direction = (obj as OwnableObject).Direction;
			return direction / 32;
		};

		public static Func<GameObject, int> OverlayValueFrameDecider = delegate(GameObject obj) {
			return (obj as OverlayObject).OverlayValue;
		};

		public static Func<GameObject, int> NullFrameDecider = arg => 0;

		public static Func<GameObject, int> AlphaImageFrameDecider(ShpFile shp) {
			return delegate(GameObject obj) {
				int direction = 0;
				if (obj is OwnableObject)
					direction = (obj as OwnableObject).Direction;
				shp.Initialize(); // header needs to be loaded at least
				int imgCount = shp.Header.NumImages;
				if (imgCount % 8 == 0)
					return (imgCount / 8) * (direction / 32);
				else
					return 0;
			};
		}


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