using System;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.Shared;
using CNCMaps.Shared.Utility;

namespace CNCMaps.Engine.Game {
	public static class FrameDeciders {

		/// <summary>
		/// Building turrets
		/// </summary>
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
		/// Dummy used as default
		/// </summary>
		public static Func<GameObject, int> HealthBasedFrameDecider = delegate(GameObject obj) {
			int health = (obj is OwnableObject) ? (obj as OwnableObject).Health : 255;
			if (health >= 128) return 0;
			else return 1;
		};

		/// <summary>
		/// For non-animated building parts that show frame 0/1 for damaged buildings based on ConditionYellow/ConditionRed values
		/// </summary>
		public static Func<GameObject, int> BaseBuildingFrameDecider(bool isDamaged) {
			return delegate (GameObject obj) {
				if (isDamaged) return 1;
				else return 0;
			};
		}

		/// <summary>
		/// For RA2/YR buildings with rubble and health set to 0 in maps. Mostly 4th frame but selecting last normal frame
		/// </summary>
		public static Func<GameObject, int> BuildingRubbleFrameDecider(int totalFrames) {
			return delegate (GameObject obj) {
				int frameNum = 0;
				if (totalFrames >= 8)
					frameNum = (totalFrames / 2) - 1;
				return frameNum;
			};
		}

		/// <summary>
		/// Use this for animations that have a loopstart and loopend
		/// </summary>
		/// <returns>A framedecider between loopend and loopstart</returns>
		public static Func<GameObject, int> LoopFrameDecider(int loopstart, int loopend) {
			return delegate (GameObject obj) {
				// loopstart > loopend is possible
				return Math.Min(loopstart, loopend) + Rand.Next(Math.Abs(loopend - loopstart));
			};
		}

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
			return delegate (GameObject obj) {
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

		// SHP vehicles and infantry behave differently, so they require different frame decider logic.
		// This is due to SHP vehicles not obeying the unwritten rule that infantry have standing frames coming first.
		public static Func<GameObject, int> SHPVehicleFrameDecider(int StartStandFrame, int StandingFrames, int StartWalkFrame, int WalkFrames, int Facings) {
			return delegate (GameObject obj) {
				int direction = 0;
				int frameoffset = 0;
				int framenumber = 0;

				if (obj is OwnableObject)
					direction = (obj as OwnableObject).Direction;

				if (Facings == 8) {
					frameoffset = (direction / 32) + 1;
					if (frameoffset >= 8) frameoffset -= 8;
				}

				if (Facings == 32) {
					if (ModConfig.ActiveConfig.Engine < EngineType.RedAlert2) {
						frameoffset = (direction / 8) + 1;
						if (frameoffset >= 32) frameoffset -= 32;
					}
					else {
						frameoffset = direction / 8 + 5;
						if (frameoffset >= 32) frameoffset -= 32;
					}
				}
				if (StandingFrames == 0 && StartStandFrame == 0 && WalkFrames > 0)
					framenumber = StartWalkFrame + (frameoffset * WalkFrames);
				else if (StandingFrames == 0 && StartStandFrame == 0 && WalkFrames == 0)
					framenumber = StartWalkFrame + frameoffset;
				else if (StandingFrames == 0 && StartStandFrame > 0)
					framenumber = StartStandFrame + frameoffset;
				else
					framenumber = StartStandFrame + (frameoffset * StandingFrames);

				return framenumber;
			};
		}

		public static Func<GameObject, int> SHPVehicleSHPTurretFrameDecider(int StartWalkFrame, int WalkFrames, int Facings) {
			return delegate (GameObject obj) {
				int direction = 0;
				int frameoffset = 0;
				int framenumber = 0;

				if (obj is OwnableObject)
					direction = (obj as OwnableObject).Direction;

				frameoffset = (direction / 8) + 4;
				if (frameoffset >= 32) frameoffset -= 32;

				// 8 instead of facings, is hardcoded in the game
				if (WalkFrames > 0) {
					framenumber = (WalkFrames * 8) + frameoffset;
				}
				else {
					framenumber = 8 + frameoffset;
				}

				return framenumber;
			};
		}

		// DirectionBasedFrameDecider does not actually get infantry facings right (it displays them in same way as FA2 does, which is wrong).
		public static Func<GameObject, int> InfantryFrameDecider(int Ready_Start = 0, int Ready_Count = 1, int Ready_CountNext = 1, int randomFacing = -1) {
			return delegate (GameObject obj) {
				int val = 0;
				int direction = 0;
				if (obj is OwnableObject)
					direction = (obj as OwnableObject).Direction;
				if (randomFacing >= 0)
					direction = randomFacing;
				if (Ready_Count > 0) val = Ready_Start + Ready_CountNext * (7 - (direction / 32));
				return val;
			};
		}
		
	}
}
