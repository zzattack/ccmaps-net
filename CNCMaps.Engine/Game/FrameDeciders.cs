using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.Engine.Utility;
using CNCMaps.FileFormats;
using CNCMaps.Shared;
using NLog;

namespace CNCMaps.Engine.Game {
	public static class FrameDeciders {
		public static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
			if (obj is OverlayObject) return (obj as OverlayObject).OverlayValue;
			else return 0;
		};

		public static Func<GameObject, int> NullFrameDecider = arg => 0;

		public static Func<GameObject, int> AlphaImageFrameDecider(ShpFile shp) {
			return delegate(GameObject obj) {
				int direction = 0;
				if (obj is OwnableObject)
					direction = (obj as OwnableObject).Direction;
				shp.Initialize(); // header needs to be loaded at least
				int imgCount = shp.NumImages;
				if (imgCount % 8 == 0)
					return (imgCount / 8) * (direction / 32);
				else
					return 0;
			};
		}

        // Starkku: Necessary due to SHP vehicles not obeying the unwritten rule that infantry have with standing frames coming first.
        // Plus the frame order is different compared to infantry.
        public static Func<GameObject, int> SHPVehicleFrameDecider(int StartStandFrame, int StandingFrames, int Facings)
        {
            return delegate(GameObject obj)
            {
                int direction = 0;
				if (obj is OwnableObject)
					direction = (obj as OwnableObject).Direction;
                if (StandingFrames > 0) return StartStandFrame + (((direction / 32)+1) * StandingFrames);
                return StartStandFrame;
            };
        }

        // Starkku: DirectionBasedFrameDecider does not actually get infantry facings right (it displays them in same way as FA2 does, which is wrong).
        public static Func<GameObject, int> InfantryFrameDecider(int Ready_Start = 0, int Ready_Count = 1, int Ready_CountNext = 1)
        {
            return delegate(GameObject obj)
            {
                int val = 0;
                int direction = 0;
                if (obj is OwnableObject)
                    direction = (obj as OwnableObject).Direction;
                if (Ready_Count > 0) val = Ready_Start + Ready_CountNext * (7 - (direction / 32));
                return val;
            };
        }

		private static Dictionary<ObjectOverride, Func<GameObject, int>> cachedDeciders =
			new Dictionary<ObjectOverride, Func<GameObject, int>>();
		public static Func<GameObject, int> GetOverrideFrameDecider(ObjectOverride ovr) {
			Func<GameObject, int> fd;
			if (!cachedDeciders.TryGetValue(ovr, out fd)) {
				cachedDeciders[ovr] = fd = FrameDeciderCompiler.CompileFrameDecider(ovr.FrameDeciderCode);
			}
			return fd;
		}

		/*
		public static Func<GameObject, int> CreateCacher(Func<GameObject, int> wrap) {
			int cachedFrame = -1;
			return delegate(GameObject obj) {
				if (cachedFrame == -1 || obj.RequiresFrameInvalidation) {
					cachedFrame = wrap(obj);
					obj.RequiresFrameInvalidation = false;
				}
				return cachedFrame;
			};
		}*/

	}
}