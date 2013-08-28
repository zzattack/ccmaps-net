using System;
using System.Collections.Generic;

namespace CNCMaps.MapLogic {

	public static class TheaterDefaults {

		public static char GetTheaterPrefix(TheaterType T) {
			switch (T) {
				case TheaterType.Temperate:
				case TheaterType.TemperateYR:
					return 'T';
				case TheaterType.Urban:
				case TheaterType.UrbanYR:
					return 'U';
				case TheaterType.Snow:
				case TheaterType.SnowYR:
					return 'A';
				case TheaterType.Lunar: return 'L';
				case TheaterType.Desert: return 'D';
				case TheaterType.NewUrban: return 'N';
				default: return 'G'; // generic
			}
		}

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
					return LightingType.Full;
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		public static bool GetDefaultRemappability(CollectionType type) {
			switch (type) {
				case CollectionType.Aircraft:
				case CollectionType.Building:
				case CollectionType.Infantry:
				case CollectionType.Vehicle:
					return true;
				case CollectionType.Overlay:
				case CollectionType.Smudge:
				case CollectionType.Terrain:
					return false;
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		public static string GetExtension(TheaterType t) {
			switch (t) {
				case TheaterType.Temperate:
				case TheaterType.TemperateYR:
					return ".tem";
				case TheaterType.Urban:
				case TheaterType.UrbanYR:
					return ".urb";
				case TheaterType.Snow:
				case TheaterType.SnowYR:
					return ".sno";
				case TheaterType.Lunar: return ".lun";
				case TheaterType.Desert: return ".des";
				case TheaterType.NewUrban: return ".ubn";
				default: return "";
			}
		}

		public static string GetExtension(TheaterType t, CollectionType s) {
			switch (s) {
				case CollectionType.Overlay:
				case CollectionType.Smudge:
				case CollectionType.Building:
				case CollectionType.Aircraft:
				case CollectionType.Infantry:
				case CollectionType.Vehicle:
					return ".shp";
			}
			return GetExtension(t);
		}

		public static bool GetShadowAssumption(CollectionType t) {
			switch (t) {
				case CollectionType.Overlay:
					return true;
				case CollectionType.Smudge:
					return false;
				case CollectionType.Building:
					return true;
				case CollectionType.Aircraft:
					return false;
				case CollectionType.Infantry:
					return true;
				case CollectionType.Terrain:
					return true;
				case CollectionType.Vehicle:
					return false;
				default:
					return false;
			}
		}

		public static IEnumerable<string> GetTheaterMixes(TheaterType theaterType) {
			var ret = new List<string>();

			switch (theaterType) {
				case TheaterType.Desert:
					ret.Add("isodesmd.mix");
					ret.Add("desert.mix");
					ret.Add("des.mix");
					ret.Add("isodes.mix");
					break;

				case TheaterType.Lunar:
					ret.Add("isolunmd.mix");
					ret.Add("isolun.mix");
					ret.Add("lun.mix");
					ret.Add("lunar.mix");
					break;

				case TheaterType.NewUrban:
					ret.Add("isoubnmd.mix");
					ret.Add("isoubn.mix");
					ret.Add("ubn.mix");
					ret.Add("urbann.mix");
					break;

				case TheaterType.Snow:
					ret.Add("snow.mix");
					ret.Add("sno.mix");
					break;

				case TheaterType.SnowYR:
					ret.Add("isosnomd.mix");
					ret.Add("snowmd.mix");
					ret.Add("snow.mix");
					ret.Add("sno.mix");
					break;

				case TheaterType.Temperate:
					ret.Add("isotemp.mix");
					ret.Add("temperat.mix");
					ret.Add("tem.mix");
					break;

				case TheaterType.TemperateYR:
					ret.Add("isotemp.mix");
					ret.Add("isotemmd.mix");
					ret.Add("temperat.mix");
					ret.Add("tem.mix");
					break;

				case TheaterType.Urban:
					ret.Add("isourb.mix");
					ret.Add("urb.mix");
					ret.Add("urban.mix");
					break;

				case TheaterType.UrbanYR:
					ret.Add("isourbmd.mix");
					ret.Add("isourb.mix");
					ret.Add("urb.mix");
					ret.Add("urban.mix");
					break;
			}

			// there's a few files in isosnow.mix (flag shps mainly) that shouldn't be there,
			// but they can be used outside snow theaters anyway
			ret.Add("isosnow.mix");
			ret.Add("snow.mix");

			return ret;
		}

		public static string GetTheaterIni(TheaterType theaterType) {
			switch (theaterType) {
				case TheaterType.Desert:
					return "desertmd.ini";

				case TheaterType.Lunar:
					return "lunarmd.ini";

				case TheaterType.NewUrban:
					return "urbannmd.ini";

				case TheaterType.Snow:
					return "snow.ini";

				case TheaterType.SnowYR:
					return "snowmd.ini";

				case TheaterType.Temperate:
					return "temperat.ini";

				case TheaterType.TemperateYR:
					return "temperatmd.ini";

				case TheaterType.Urban:
					return "urban.ini";

				case TheaterType.UrbanYR:
					return "urbanmd.ini";

				default:
					throw new InvalidOperationException();
			}
		}

		public static string GetTileExtension(TheaterType theaterType) {
			switch (theaterType) {
				case TheaterType.Urban:
				case TheaterType.UrbanYR:
					return ".urb";
				case TheaterType.Snow:
				case TheaterType.SnowYR:
					return ".sno";
				case TheaterType.Temperate:
				case TheaterType.TemperateYR:
					return ".tem";
				case TheaterType.NewUrban:
					return ".ubn";
				case TheaterType.Lunar:
					return ".lun";
				case TheaterType.Desert:
					return ".des";
				default:
					throw new InvalidOperationException("invalid theater");
			}
		}


	}
}