using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {
	class MissionsFile : IniFile {

		public Dictionary<string, MissionEntry> MissionEntries { get; set; }

		public MissionsFile(VirtualFile f) : base(f) {
			MissionEntries = new Dictionary<string, MissionEntry>();
			foreach (IniSection s in Sections) {
				MissionEntries.Add(s.Name.ToLower(), new MissionEntry(s));
			}
		}

		public MissionEntry GetMissionEntry(string missionName) {
			// skip extension
			MissionEntry ret = null;
			MissionEntries.TryGetValue(missionName.ToLower(), out ret);
			return ret;
		}

		public class MissionEntry {
			public string Briefing { get; set; }
			public string UIName { get; set; }
			public string LSLoadMessage { get; set; }
			public string LSLoadBriefing { get; set; }
			public int LS640BriefLocX { get; set; }
			public int LS640BriefLocY { get; set; }
			public int LS800BriefLocX { get; set; }
			public int LS800BriefLocY { get; set; }
			public string LS640BkgdName { get; set; }
			public string LS800BkgdName { get; set; }

			public MissionEntry(IniSection iniSection) {
				this.Briefing = iniSection.ReadString("Briefing");
				this.UIName = iniSection.ReadString("UIName");
				this.LS640BriefLocX = iniSection.ReadInt("LS640BriefLocX");
				this.LS640BriefLocY = iniSection.ReadInt("LS640BriefLocY");
				this.LS800BriefLocX = iniSection.ReadInt("LS800BriefLocX");
				this.LS800BriefLocY = iniSection.ReadInt("LS800BriefLocY");
				this.LSLoadMessage = iniSection.ReadString("LSLoadMessage");
				this.LSLoadBriefing = iniSection.ReadString("LSLoadBriefing");
				this.LS640BkgdName = iniSection.ReadString("LS640BkgdName");
				this.LS800BkgdName = iniSection.ReadString("LS800BkgdName");
			}
		}
	}
}
