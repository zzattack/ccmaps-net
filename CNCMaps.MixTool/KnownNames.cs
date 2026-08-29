using System.Collections.Generic;

namespace CNCMaps.MixTool {

	/// <summary>
	/// Filenames worth trying when resolving mix entries. A mix stores only name hashes, so this
	/// is a guess list: names that hash to an entry get reported, the rest cost nothing.
	/// </summary>
	static class KnownNames {

		// theater ini stem -> palette/file infix used by that theater
		static readonly (string Ini, string Code)[] Theaters = {
			("temperat", "tem"), ("snow", "sno"), ("urban", "urb"),
			("urbann", "ubn"), ("lunar", "lun"), ("desert", "des"),
		};

		static readonly string[] GlobalStems = {
			"rules", "art", "ai", "sound", "eva", "theme", "mission", "missions",
			"tutorial", "battle", "mpmodesmd", "elevation",
		};

		public static IEnumerable<string> All() {
			// global ini files, plain and Yuri's Revenge (md) flavours
			foreach (string stem in GlobalStems) {
				yield return stem + ".ini";
				yield return stem + "md.ini";
			}

			// theater control files and their palettes
			foreach (var (ini, code) in Theaters) {
				yield return ini + ".ini";
				yield return ini + "md.ini";
				foreach (string prefix in new[] { "iso", "unit", "lib", "anim", "" })
					yield return prefix + code + ".pal";
				yield return code + "md.pal";
			}

			// string tables, mission lists, common odds and ends
			foreach (string n in new[] {
				"ra2.csf", "ra2md.csf", "language.mix", "missionmd.pkt", "missions.pkt",
				"multi01.map", "local mix database.dat", "global mix database.dat",
			}) yield return n;

			// nested archives commonly found inside game mixes
			for (int i = 1; i <= 99; i++) {
				yield return string.Format("expand{0:00}.mix", i);
				yield return string.Format("expandmd{0:00}.mix", i);
				yield return string.Format("ecache{0:00}.mix", i);
				yield return string.Format("ecachemd{0:00}.mix", i);
				yield return string.Format("elocal{0:00}.mix", i);
				yield return string.Format("elocalmd{0:00}.mix", i);
			}
			foreach (string n in new[] {
				"ra2.mix", "ra2md.mix", "cache.mix", "cachemd.mix", "local.mix", "localmd.mix",
				"conquer.mix", "conquermd.mix", "generic.mix", "genericmd.mix",
				"isogen.mix", "isogenmd.mix", "maps01.mix", "maps02.mix", "mapsmd03.mix",
				"multi.mix", "multimd.mix", "theme.mix", "thememd.mix", "wdt.mix", "langmd.mix",
			}) yield return n;

			// theater tile archives, e.g. temperat.mix / tem.mix / temmd.mix
			foreach (var (ini, code) in Theaters) {
				yield return ini + ".mix";
				yield return ini + "md.mix";
				yield return code + ".mix";
				yield return code + "md.mix";
			}
		}
	}
}
