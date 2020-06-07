using System;
using CNCMaps.Engine.Map;
using CNCMaps.Shared;

namespace CNCMaps.Engine.Game {

	public static class Defaults {
		// Managed at:
		// https://docs.google.com/spreadsheet/ccc?key=0AiVQdoAJ4w7bdE9ILUpvNVVDa2J2MG04RnBURU96VUE#gid=0

		public static PaletteType GetDefaultPalette(CollectionType t, EngineType engine) {
			switch (t) {
				case CollectionType.Building:
				case CollectionType.Aircraft:
				case CollectionType.Infantry:
				case CollectionType.Vehicle:
					return PaletteType.Unit;
				case CollectionType.Overlay:
					return PaletteType.Overlay;
				case CollectionType.Smudge:
				case CollectionType.Terrain:
				case CollectionType.Animation:
				default:
					return PaletteType.Iso;
			}
		}

		internal static LightingType GetDefaultLighting(CollectionType type) {
			switch (type) {
				case CollectionType.Aircraft:
				case CollectionType.Building:
				case CollectionType.Infantry:
				case CollectionType.Vehicle:
					return LightingType.Ambient;
				case CollectionType.Overlay:
				case CollectionType.Smudge:
				case CollectionType.Terrain:
				case CollectionType.Animation:
					return LightingType.Full;
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		public static bool GetDefaultRemappability(CollectionType type, EngineType engine) {
			switch (type) {
				case CollectionType.Aircraft:
				case CollectionType.Building:
				case CollectionType.Infantry:
				case CollectionType.Vehicle:
					return true;
				case CollectionType.Overlay:
				case CollectionType.Smudge:
				case CollectionType.Terrain:
				case CollectionType.Animation:
					return false;
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		public static bool GetShadowAssumption(CollectionType t) {
			switch (t) {
				case CollectionType.Overlay:
				case CollectionType.Building:
				case CollectionType.Infantry:
				case CollectionType.Terrain:
				case CollectionType.Vehicle:
				case CollectionType.Aircraft:
					return true;
				default:
				case CollectionType.Smudge:
				case CollectionType.Animation:
					return false;
			}
		}
		public static bool GetFlatnessAssumption(CollectionType t) {
			switch (t) {
				case CollectionType.Overlay:
				case CollectionType.Smudge:
					return true;
				case CollectionType.Building:
				case CollectionType.Aircraft:
				case CollectionType.Infantry:
				case CollectionType.Terrain:
				case CollectionType.Vehicle:
					return false;
				default:
					return true;
			}
		}

		public static Func<GameObject, int> GetDefaultFrameDecider(CollectionType collection) {
			switch (collection) {
				case CollectionType.Vehicle:
				case CollectionType.Aircraft:
				case CollectionType.Infantry:
					return FrameDeciders.DirectionBasedFrameDecider;
				case CollectionType.Building:
					return FrameDeciders.HealthBasedFrameDecider;
				case CollectionType.Overlay:
					return FrameDeciders.OverlayValueFrameDecider;
				case CollectionType.Smudge:
				case CollectionType.Terrain:
					return FrameDeciders.NullFrameDecider;
				case CollectionType.Animation:
					return FrameDeciders.NullFrameDecider;
				default:
					throw new ArgumentOutOfRangeException("collection");
			}
		}


	}
}