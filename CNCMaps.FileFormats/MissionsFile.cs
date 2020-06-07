using System.Collections.Generic;
using System.IO;

namespace CNCMaps.FileFormats {

	public class MissionsFile : IniFile {

		public Dictionary<string, MissionEntry> MissionEntries { get; set; }

		public MissionsFile(Stream baseStream, string filename, bool isBuffered = true)
			: this(baseStream, filename, 0, baseStream.Length, isBuffered) { }

		public MissionsFile(Stream baseStream, string filename, int offset, long length, bool isBuffered = true)
			: base(baseStream, filename, offset, length, isBuffered) {
			Parse();
		}

		private void Parse() {
			MissionEntries = new Dictionary<string, MissionEntry>();
			foreach (IniSection s in Sections) {
				MissionEntries.Add(s.Name.ToLower(), new MissionEntry(s));
			}
		}

		public MissionEntry GetMissionEntry(string missionName) {
			// skip extension
			MissionEntry ret;
			MissionEntries.TryGetValue(missionName.ToLower(), out ret);
			return ret;
		}

		public class MissionEntry {
			public string Briefing { get; set; }
			public string UIName { get; set; } // used by RA2/YR, localized by CSF file
			public string Name { get; set; } // used by TS/FS
			public string LSLoadMessage { get; set; }
			public string LSLoadBriefing { get; set; }
			public int LS640BriefLocX { get; set; }
			public int LS640BriefLocY { get; set; }
			public int LS800BriefLocX { get; set; }
			public int LS800BriefLocY { get; set; }
			public string LS640BkgdName { get; set; }
			public string LS800BkgdName { get; set; }

			public MissionEntry(IniSection iniSection) {
				Briefing = iniSection.ReadString("Briefing");
				UIName = iniSection.ReadString("UIName");
				Name = iniSection.ReadString("Name");
				LS640BriefLocX = iniSection.ReadInt("LS640BriefLocX");
				LS640BriefLocY = iniSection.ReadInt("LS640BriefLocY");
				LS800BriefLocX = iniSection.ReadInt("LS800BriefLocX");
				LS800BriefLocY = iniSection.ReadInt("LS800BriefLocY");
				LSLoadMessage = iniSection.ReadString("LSLoadMessage");
				LSLoadBriefing = iniSection.ReadString("LSLoadBriefing");
				LS640BkgdName = iniSection.ReadString("LS640BkgdName");
				LS800BkgdName = iniSection.ReadString("LS800BkgdName");
			}
		}
	}
}