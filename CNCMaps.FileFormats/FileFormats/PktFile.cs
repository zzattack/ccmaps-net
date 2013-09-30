using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CNCMaps.FileFormats.VirtualFileSystem;

namespace CNCMaps.FileFormats.FileFormats {

	public class PktFile : IniFile {

		public Dictionary<string, PktMapEntry> MapEntries { get; set; }

		public PktFile(VirtualFile f, string filename = "", bool isBuffered = true) : this(f, filename, 0, f.Length, isBuffered) { }

		public PktFile(Stream baseStream, string filename, int offset, long length, bool isBuffered = true)
			: base(baseStream, filename, offset, length, isBuffered) {
			Parse();
		}

		private void Parse() {
			MapEntries = new Dictionary<string, PktMapEntry>();
			IniSection maplist = GetSection("MultiMaps");
			foreach (var v in maplist.OrderedEntries) {
				IniSection mapsection = GetSection(v.Value);
				if (mapsection != null)
					MapEntries.Add(((string)v.Value).ToLower(), new PktMapEntry(mapsection));
			}
		}

		public PktMapEntry GetMapEntry(string mapname) {
			// skip extension
			if (mapname.Contains('.'))
				mapname = mapname.Substring(0, mapname.IndexOf('.'));
			PktMapEntry ret = null;
			MapEntries.TryGetValue(mapname, out ret);
			return ret;
		}

		[Flags]
		public enum GameMode : uint {
			None = 0x00,
			Standard = 0x01,
			MeatGrind = 0x02,
			NavalWar = 0x04,
			NukeWar = 0x08,
			AirWar = 0x10,
			Cooperative = 0x20,
			Duel = 0x40,
			Megawealth = 0x80,
			TeamGame = 0x100,
			Siege = 0x200,
		}

		public class PktMapEntry {

			public string Description { get; private set; }

			public int MinPlayers { get; private set; }

			public int MaxPlayer { get; private set; }

			public GameMode GameModes { get; private set; }

			public PktMapEntry(IniSection sect) {
				Description = sect.ReadString("Description");
				MinPlayers = sect.ReadInt("MinPlayers");
				MaxPlayer = sect.ReadInt("MaxPlayers");
				string[] GameModes = sect.ReadString("GameMode").Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string g in GameModes)
					this.GameModes |= (GameMode)Enum.Parse(typeof(GameMode), g, true);
			}
		}
	}
}