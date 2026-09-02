using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using CNCMaps.FileFormats.Encodings;
using CNCMaps.FileFormats.VirtualFileSystem;
using NLog;

namespace CNCMaps.FileFormats.Map {

	/// <summary>Map file.</summary>
	public class MapFile : IniFile {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public Rectangle FullSize { get; private set; }
		public Rectangle LocalSize { get; private set; }

		public TileLayer Tiles;
		public readonly List<Overlay> Overlays = new List<Overlay>();
		public readonly List<Smudge> Smudges = new List<Smudge>();
		public readonly List<Terrain> Terrains = new List<Terrain>();
		public readonly List<Structure> Structures = new List<Structure>();
		public readonly List<Infantry> Infantries = new List<Infantry>();
		public readonly List<Unit> Units = new List<Unit>();
		public readonly List<Aircraft> Aircrafts = new List<Aircraft>();
		public readonly List<Waypoint> Waypoints = new List<Waypoint>();
		public readonly List<IniSection> MiscSections = new List<IniSection>();
		public readonly List<TunnelLine> TunnelEntries = new List<TunnelLine>();
		public Lighting Lighting;

		/// <summary>House value a trigger action uses for "Player @ A"; A..H run to 4482.
		/// The engine maps it to the player who spawns at start waypoint 0..7.</summary>
		private const int PlayerAtStartHouse = 4475;

		/// <summary>Constructor.</summary>
		/// <param name="baseStream">The base stream.</param>
		public MapFile(Stream baseStream, string filename = "")
			: this(baseStream, filename, 0, baseStream.Length) {
		}

		public MapFile(Stream baseStream, string filename, int offset, long length, bool isBuffered = true) :
			base(baseStream, filename, offset, length, isBuffered) {
			if (isBuffered)
				Close(); // we no longer need the file handle anyway
			Initialize();
		}

		public void Initialize() {
			var map = GetSection("Map");
			if (map == null)
				throw new InvalidDataException(
					"Map file has no [Map] section. If it uses [INISystem]BasedOn inheritance, " +
					"the base map file must be present next to it.");
			string[] size = map.ReadString("Size").Split(',');
			FullSize = new Rectangle(int.Parse(size[0]), int.Parse(size[1]), int.Parse(size[2]), int.Parse(size[3]));
			Tiles = new TileLayer(FullSize.Width, FullSize.Height);
			size = map.ReadString("LocalSize").Split(',');
			LocalSize = new Rectangle(int.Parse(size[0]), int.Parse(size[1]), int.Parse(size[2]), int.Parse(size[3]));

			Logger.Info("Reading map");
			Logger.Debug("Reading tiles");
			ReadTiles();

			Logger.Debug("Reading map overlay");
			ReadOverlay();

			Logger.Debug("Reading map terrain objects");
			ReadTerrain();

			Logger.Debug("Reading map smudge objects");
			ReadSmudges();

			Logger.Debug("Reading infantry on map");
			ReadInfantry();

			Logger.Debug("Reading vehicles on map");
			ReadUnits();

			Logger.Debug("Reading aircraft on map");
			ReadAircraft();

			Logger.Debug("Reading map structures");
			ReadStructures();

			Logger.Debug("Waypoints");
			ReadWaypoints();

			Logger.Debug("Reading tunnels");
			ReadTubes();

			Lighting = new Lighting(GetOrCreateSection("Lighting"));
		}

		/// <summary>Decodes a base64 pack section. Some map editors leave a stray character or bad
		/// padding at the end; the game ignores that, so this decodes the largest valid prefix.</summary>
		private static byte[] DecodePackBase64(string s) {
			try {
				return Convert.FromBase64String(s);
			}
			catch (FormatException) {
				var sb = new System.Text.StringBuilder(s.Length);
				foreach (char c in s)
					if (!char.IsWhiteSpace(c)) sb.Append(c);
				while (sb.Length > 0 && sb[sb.Length - 1] == '=')
					sb.Length--;
				sb.Length -= sb.Length % 4;
				return Convert.FromBase64String(sb.ToString());
			}
		}

		/// <summary>Reads the tiles. </summary>
		private void ReadTiles() {
			var mapSection = GetSection("IsoMapPack5");
			byte[] lzoData = DecodePackBase64(mapSection.ConcatenatedValues());
			int cells = (FullSize.Width * 2 - 1) * FullSize.Height;
			int lzoPackSize = cells * 11 + 4; // last 4 bytes contains a lzo pack header saying no more data is left

			var isoMapPack = new byte[lzoPackSize];

			// In case, IsoMapPack5 contains less entries than the number of cells, fill up any number greater 
			// than 511 and filter later.
			int j = 0;
			for (int i = 0; i < cells; i++) {
				isoMapPack[j] = 0x88;
				isoMapPack[j + 1] = 0x40;
				isoMapPack[j + 2] = 0x88;
				isoMapPack[j + 3] = 0x40;
				j += 11;
			}

			Format5.DecodeInto(lzoData, isoMapPack);

			// Fill level 0 clear tiles for all array values
			for (ushort y = 0; y < FullSize.Height; y++) {
				for (ushort x = 0; x <= FullSize.Width * 2 - 2; x++) {
					ushort dx = (ushort)(x);
					ushort dy = (ushort)(y * 2 + x % 2);
					ushort rx = (ushort)((dx + dy) / 2 + 1);
					ushort ry = (ushort)(dy - rx + FullSize.Width + 1);
					Tiles[x, y] = new IsoTile(dx, dy, rx, ry, 0, 0, 0, 0);
				}
			}

			// Overwrite with actual entries found in IsoMapPack5
			var mf = new MemoryFile(isoMapPack);
			int numtiles = 0;
			int outOfBounds = 0;
			for (int i = 0; i < cells; i++) {
				ushort rx = mf.ReadUInt16();
				ushort ry = mf.ReadUInt16();
				int tilenum = mf.ReadInt32();
				byte subtile = mf.ReadByte();
				byte z = mf.ReadByte();
				byte icegrowth = mf.ReadByte();

				if (tilenum >= 65535) tilenum = 0; // Tile 0xFFFF used as empty/clear

				if (rx <= 511 && ry <= 511) {
					int dx = rx - ry + FullSize.Width - 1;
					int dy = rx + ry - FullSize.Width - 1;
					numtiles++;
					// the tile array is (2 * Width - 1) x Height, indexed [dx, dy / 2]
					if (dx >= 0 && dx < 2 * Tiles.Width - 1 && dy >= 0 && dy < 2 * Tiles.Height) {
						var tile = new IsoTile((ushort)dx, (ushort)dy, rx, ry, z, tilenum, subtile, icegrowth);
						Tiles[(ushort)dx, (ushort)dy / 2] = tile;
					}
					else
						outOfBounds++;
				}
			}

			if (outOfBounds > 0)
				Logger.Warn("Ignored {0} tile entries outside map bounds", outOfBounds);
			Logger.Debug("Read {0} tiles", numtiles);
		}

		/// <summary>Reads the terrain. </summary>
		private void ReadTerrain() {
			IniSection terrainSection = GetSection("Terrain");
			if (terrainSection == null) return;
			foreach (var v in terrainSection.OrderedEntries) {
				int pos;
				if (int.TryParse(v.Key, out pos)) {
					string name = v.Value;
					int rx = pos % 1000;
					int ry = pos / 1000;
					var t = new Terrain(name);
					t.Tile = Tiles.GetTileR(rx, ry);
					if (t.Tile != null)
						Terrains.Add(t);
				}
			}
			Logger.Debug("Read {0} terrain objects", Terrains.Count);
		}

		/// <summary>Reads the smudges. </summary>
		private void ReadSmudges() {
			IniSection smudgesSection = GetSection("Smudge");
			if (smudgesSection == null) return;
			foreach (var v in smudgesSection.OrderedEntries) {
				try {
					string[] entries = ((string)v.Value).Split(',');
					if (entries.Length <= 2) continue;
					string name = entries[0];
					int rx = int.Parse(entries[1]);
					int ry = int.Parse(entries[2]);
					var s = new Smudge(name);
					s.Tile = Tiles.GetTileR(rx, ry);
					if (s.Tile != null)
						Smudges.Add(s);
				}
				catch (FormatException) {
				}
			}
			Logger.Debug("Read {0} smudges", Smudges.Count);
		}

		/// <summary>Reads the overlay.</summary>
		private void ReadOverlay() {
			IniSection overlaySection = GetSection("OverlayPack");
			if (overlaySection == null) {
				Logger.Info("OverlayPack section unavailable in {0}, overlay will be unavailable", Path.GetFileName(FileName));
				return;
			}

			byte[] format80Data = DecodePackBase64(overlaySection.ConcatenatedValues());
			var overlayPack = new byte[1 << 18];
			Format5.DecodeInto(format80Data, overlayPack, 80);

			IniSection overlayDataSection = GetSection("OverlayDataPack");
			if (overlayDataSection == null) {
				Logger.Debug("OverlayDataPack section unavailable in {0}, overlay will be unavailable", Path.GetFileName(FileName));
				return;
			}
			format80Data = DecodePackBase64(overlayDataSection.ConcatenatedValues());
			var overlayDataPack = new byte[1 << 18];
			Format5.DecodeInto(format80Data, overlayDataPack, 80);

			for (int y = 0; y < FullSize.Height; y++) {
				for (int x = FullSize.Width * 2 - 2; x >= 0; x--) {
					var t = Tiles[x, y];
					if (t == null) continue;
					int idx = t.Rx + 512 * t.Ry;
					byte overlay_id = overlayPack[idx];
					if (overlay_id != 0xFF) {
						byte overlay_value = overlayDataPack[idx];
						var ovl = new Overlay(overlay_id, overlay_value);
						ovl.Tile = t;
						Overlays.Add(ovl);
					}
				}
			}

			Logger.Debug("Read {0} overlay types", Overlays.Count);
		}

		/// <summary>Reads the infantry. </summary>
		private void ReadInfantry() {
			IniSection infantrySection = GetSection("Infantry");
			if (infantrySection == null) {
				Logger.Info("Infantry section unavailable in {0}", Path.GetFileName(FileName));
				return;
			}

			foreach (var v in infantrySection.OrderedEntries) {
				try {

					string[] entries = ((string)v.Value).Split(',');
					if (entries.Length <= 8) continue;
					string owner = entries[0];
					string name = entries[1];
					short health = short.Parse(entries[2]);
					int rx = int.Parse(entries[3]);
					int ry = int.Parse(entries[4]);
					int subCell = int.Parse(entries[5]);
					short direction = (short)(short.Parse(entries[7]) & 0xFF); // the game stores facings as a byte
					bool onBridge = entries[11] == "1";
					var i = new Infantry(owner, name, health, direction, subCell, onBridge);
					i.Tag = ReadTag(entries, 8);
					i.Tile = Tiles.GetTileR(rx, ry);
					if (i.Tile != null)
						Infantries.Add(i);
				}
				catch (IndexOutOfRangeException) {
				}
				catch (FormatException) {
				}
			}
			Logger.Trace("Read {0} infantry objects", Infantries.Count);

		}

		/// <summary>Reads the units.</summary>
		private void ReadUnits() {
			IniSection unitsSection = GetSection("Units");
			if (unitsSection == null) {
				Logger.Info("Units section unavailable in {0}", Path.GetFileName(FileName));
				return;
			}
			foreach (var v in unitsSection.OrderedEntries) {
				try {
					string[] entries = ((string)v.Value).Split(',');
					if (entries.Length <= 11) continue;

					string owner = entries[0];
					string name = entries[1];
					short health = short.Parse(entries[2]);
					int rx = int.Parse(entries[3]);
					int ry = int.Parse(entries[4]);
					short direction = (short)(short.Parse(entries[5]) & 0xFF); // the game stores facings as a byte
					bool onBridge = entries[10] == "1";
					var u = new Unit(owner, name, health, direction, onBridge);
					u.Tag = ReadTag(entries, 7);
					u.Tile = Tiles.GetTileR(rx, ry);
					if (u.Tile != null)
						Units.Add(u);
				}
				catch (FormatException) {
				}
				catch (IndexOutOfRangeException) {
				}
			}
			Logger.Trace("Read {0} units", Units.Count);
		}

		/// <summary>Reads the aircraft.</summary>
		private void ReadAircraft() {
			IniSection aircraftSection = GetSection("Aircraft");
			if (aircraftSection == null) {
				Logger.Info("Aircraft section unavailable in {0}", Path.GetFileName(FileName));
				return;
			}
			foreach (var v in aircraftSection.OrderedEntries) {
				try {
					string[] entries = ((string)v.Value).Split(',');
					string owner = entries[0];
					string name = entries[1];
					short health = short.Parse(entries[2]);
					int rx = int.Parse(entries[3]);
					int ry = int.Parse(entries[4]);
					short direction = (short)(short.Parse(entries[5]) & 0xFF); // the game stores facings as a byte
					bool onBridge = entries[entries.Length - 4] == "1";
					var a = new Aircraft(owner, name, health, direction, onBridge);
					a.Tag = ReadTag(entries, 7);
					a.Tile = Tiles.GetTileR(rx, ry);
					if (a.Tile != null)
						Aircrafts.Add(a);
				}
				catch (FormatException) {
				}
				catch (IndexOutOfRangeException) {
				}
			}
			Logger.Trace("Read {0} aircraft objects", Aircrafts.Count);
		}

		/// <summary>Reads the structures.</summary>
		private void ReadStructures() {
			IniSection structsSection = GetSection("Structures");
			if (structsSection == null) {
				Logger.Info("Structures section unavailable in {0}", Path.GetFileName(FileName));
				return;
			}
			foreach (var v in structsSection.OrderedEntries) {
				try {
					string[] entries = ((string)v.Value).Split(',');
					if (entries.Length <= 15) continue;
					string owner = entries[0];
					string name = entries[1];
					short health = short.Parse(entries[2]);
					int rx = int.Parse(entries[3]);
					int ry = int.Parse(entries[4]);
					short direction = (short)(short.Parse(entries[5]) & 0xFF); // the game stores facings as a byte
					var s = new Structure(owner, name, health, direction);
					s.Tag = ReadTag(entries, 6);
					s.Upgrade1 = entries[12];
					s.Upgrade2 = entries[13];
					s.Upgrade3 = entries[14];
					s.Tile = Tiles.GetTileR(rx, ry);

					if (s.Tile != null)
						Structures.Add(s);
				}
				catch (IndexOutOfRangeException) {
				} // catch invalid entries
				catch (FormatException) {
				}
			}
			Logger.Trace("Read {0} structures", Structures.Count);
		}

		/// <summary>The trigger tag in an object's line, or null when it carries none.</summary>
		private static string ReadTag(string[] entries, int index) {
			if (entries.Length <= index) return null;
			string tag = entries[index].Trim();
			return tag.Length == 0 || tag.Equals("None", StringComparison.OrdinalIgnoreCase) ? null : tag;
		}

		/// <summary>Tag id -> the start slot whose player a game-start trigger hands the tagged objects
		/// to, for every tag that carries one.
		///
		/// [Tags] TagID=Repeat,Name,TriggerID -> [Triggers] TrigID=House,Attached,Name,Disabled,...
		/// -> [Actions] TrigID=Count, then Count groups of eight ActionID,P1..P6,Waypoints. Action 14
		/// is "Change House" and its house parameter P2 is 4475+N for "Player @ A".."H", the player who
		/// spawns at start waypoint N. A slot nobody occupies makes the action a no-op in the game, so
		/// the last action naming an available slot wins.</summary>
		public static Dictionary<string, int> ResolveTagOwnerSlots(IniFile ini, bool[] slotAvailable) {
			var byTag = new Dictionary<string, int>();
			var tags = ini.GetSection("Tags");
			var triggers = ini.GetSection("Triggers");
			var actions = ini.GetSection("Actions");
			var events = ini.GetSection("Events");
			if (tags == null || triggers == null || actions == null) return byTag;

			// Trigger id -> slot, for the triggers that hand objects to a starting player.
			var byTrigger = new Dictionary<string, int>();
			foreach (var entry in actions.OrderedEntries) {
				string[] f = ((string)entry.Value).Split(',');
				if (f.Length == 0 || !int.TryParse(f[0], out int count)) continue;
				int slot = -1;
				for (int i = 0; i < count; i++) {
					int g = 1 + i * 8;
					if (g + 8 > f.Length) break;
					if (f[g].Trim() != "14") continue;
					if (!int.TryParse(f[g + 2].Trim(), out int house)) continue;
					int n = house - PlayerAtStartHouse;
					if (n >= 0 && n < slotAvailable.Length && slotAvailable[n])
						slot = n;
				}
				if (slot >= 0) byTrigger[entry.Key] = slot;
			}

			foreach (var entry in tags.OrderedEntries) {
				string[] f = ((string)entry.Value).Split(',');
				if (f.Length < 3) continue;
				if (!byTrigger.TryGetValue(f[2].Trim(), out int slot)) continue;
				// Field 3 is Disabled. An empty read also lands here when the trigger id is not in [Triggers]
				// at all, which is the wanted result.
				string[] trigger = triggers.ReadString(f[2].Trim()).Split(',');
				if (trigger.Length < 4 || trigger[3].Trim() == "1") continue;
				if (events != null && IsDelayed(events.ReadString(f[2].Trim()))) continue;
				byTag[entry.Key] = slot;
			}
			return byTag;
		}

		/// <summary>Whether a trigger waits on the clock. [Events] is Count, then per event
		/// EventID,ArgFlag,Arg; ArgFlag 2 means two args follow, so the list has to be walked rather
		/// than chunked. Event 13 is "Elapsed Time"; with a non-zero argument the hand-over has not
		/// happened yet at the moment we render.</summary>
		private static bool IsDelayed(string eventList) {
			string[] f = eventList.Split(',');
			if (f.Length == 0 || !int.TryParse(f[0], out int count)) return false;
			int i = 1;
			for (int n = 0; n < count && i + 2 < f.Length + 1; n++) {
				if (!int.TryParse(f[i].Trim(), out int id) || !int.TryParse(f[i + 1].Trim(), out int argFlag))
					return false;
				int args = argFlag == 2 ? 2 : 1;
				if (i + 2 + args > f.Length) return false;
				if (id == 13 && (!int.TryParse(f[i + 2].Trim(), out int delay) || delay != 0))
					return true;
				i += 2 + args;
			}
			return false;
		}

		/// <summary>Rewrites the owner of every object a game-start trigger hands to a starting
		/// player. slotOwners[N] is the owner name for start position N, null when nobody starts
		/// there. Returns the number of objects changed.</summary>
		public int ApplyPreCapturedOwners(string[] slotOwners) {
			// Only multiplayer maps: on a campaign map the waypoints are script positions, not
			// start positions, and "Player @ A" resolves to nothing.
			var basic = GetSection("Basic");
			if (basic == null || !basic.ReadBool("MultiplayerOnly")) return 0;

			// Read [Waypoints] straight from the ini: ReadWaypoints has its own gate, and a slot
			// with no start position is one the game leaves neutral.
			var waypoints = GetSection("Waypoints");
			var available = new bool[slotOwners.Length];
			for (int i = 0; i < available.Length; i++)
				available[i] = !string.IsNullOrEmpty(slotOwners[i])
					&& waypoints != null && waypoints.HasKey(i.ToString());

			var slots = ResolveTagOwnerSlots(this, available);
			if (slots.Count == 0) return 0;

			int changed = 0;
			foreach (var o in Structures)
				if (o.Tag != null && slots.TryGetValue(o.Tag, out int s)) { o.Owner = slotOwners[s]; o.PreCaptured = true; changed++; }
			foreach (var o in Infantries)
				if (o.Tag != null && slots.TryGetValue(o.Tag, out int s)) { o.Owner = slotOwners[s]; changed++; }
			foreach (var o in Units)
				if (o.Tag != null && slots.TryGetValue(o.Tag, out int s)) { o.Owner = slotOwners[s]; changed++; }
			foreach (var o in Aircrafts)
				if (o.Tag != null && slots.TryGetValue(o.Tag, out int s)) { o.Owner = slotOwners[s]; changed++; }

			Logger.Debug("Pre-captured {0} objects from {1} tagged triggers", changed, slots.Count);
			return changed;
		}

		private void ReadWaypoints() {
			IniSection basic = GetSection("Basic");
			if (basic == null || !basic.ReadBool("MultiplayerOnly")) return;
			IniSection waypoints = GetOrCreateSection("Waypoints");

			foreach (var entry in waypoints.OrderedEntries) {
				try {
					int num, pos;
					if (int.TryParse(entry.Key, out num) && int.TryParse(entry.Value, out pos)) {
						int ry = pos / 1000;
						int rx = pos - ry * 1000;

						Waypoints.Add(new Waypoint {
							Number = int.Parse(entry.Key),
							Tile = Tiles.GetTileR(rx, ry),
						});
					}
				}
				catch {
				}
			}
		}

		private void ReadTubes() {
			IniSection tubesSection = GetSection("Tubes");
			if (tubesSection == null) {
				Logger.Info("Tubes section unavailable in {0}", Path.GetFileName(FileName));
				return;
			}

			foreach (var v in tubesSection.OrderedEntries) {
				try {
					string[] entries = ((string)v.Value).Split(',');
					if (entries.Length <= 5) continue;
					int startx = int.Parse(entries[0]);
					int starty = int.Parse(entries[1]);
					int facing = int.Parse(entries[2]);
					int endx = int.Parse(entries[3]);
					int endy = int.Parse(entries[4]);
					List<int> directions = new List<int>();

					// Game takes a maximum of 100 direction entries with atleast one last being -1.
					for (int i = 5; i < 105 && i < entries.Length; i++) {
						int direction = int.Parse(entries[i]);
						if (direction < 0 || direction >= 8) break;
						directions.Add(direction);
					}
					if (startx > 0 && startx < 512 && starty > 0 && starty < 512 && facing >= 0 && facing < 8 && endx > 0 && endx < 512 && endy > 0 && endy < 512)
						TunnelEntries.Add(new TunnelLine(startx, starty, facing, endx, endy, directions));
				}
				catch (IndexOutOfRangeException) {
				} // catch invalid entries
				catch (FormatException) {
				}
			}
			Logger.Trace("Read {0} tunnel entries", TunnelEntries.Count);
		}

	}
}
