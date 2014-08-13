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
		public Lighting Lighting;

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

			Lighting = new Lighting(GetOrCreateSection("Lighting"));
		}

		/// <summary>Reads the tiles. </summary>
		private void ReadTiles() {
			var mapSection = GetSection("IsoMapPack5");
			byte[] lzoData = Convert.FromBase64String(mapSection.ConcatenatedValues());
			int cells = (FullSize.Width * 2 - 1) * FullSize.Height;
			int lzoPackSize = cells * 11 + 4; // last 4 bytes contains a lzo pack header saying no more data is left

			var isoMapPack = new byte[lzoPackSize];
			uint totalDecompressSize = Format5.DecodeInto(lzoData, isoMapPack);

			var mf = new MemoryFile(isoMapPack);
			int numtiles = 0;
			for (int i = 0; i < cells; i++) {
				ushort rx = mf.ReadUInt16();
				ushort ry = mf.ReadUInt16();
				short tilenum = mf.ReadInt16();
				short zero1 = mf.ReadInt16();
				ushort subtile = mf.ReadByte();
				short z = mf.ReadByte();
				byte zero2 = mf.ReadByte();

				int dx = rx - ry + FullSize.Width - 1;
				int dy = rx + ry - FullSize.Width - 1;
				numtiles++;
				if (dx >= 0 && dx < 2 * Tiles.Width &&
					dy >= 0 && dy < 2 * Tiles.Height) {
					var tile = new IsoTile((ushort)dx, (ushort)dy, rx, ry, z, tilenum, subtile);
					Tiles[(ushort)dx, (ushort)dy / 2] = tile;
				}
			}

			// fix missing tiles

			// import tiles
			for (ushort y = 0; y < FullSize.Height; y++) {
				for (ushort x = 0; x <= FullSize.Width * 2 - 2; x++) {
					var isoTile = Tiles[x, y];
					if (isoTile == null) {
						// fix null tiles to blank
						ushort dx = (ushort)(x);
						ushort dy = (ushort)(y * 2 + x % 2);
						ushort rx = (ushort)((dx + dy) / 2 + 1);
						ushort ry = (ushort)(dy - rx + FullSize.Width + 1);
						Tiles[x, y] = new IsoTile(dx, dy, rx, ry, 0, 0, 0);
					}
				}
			}


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

			byte[] format80Data = Convert.FromBase64String(overlaySection.ConcatenatedValues());
			var overlayPack = new byte[1 << 18];
			Format5.DecodeInto(format80Data, overlayPack, 80);

			IniSection overlayDataSection = GetSection("OverlayDataPack");
			if (overlayDataSection == null) {
				Logger.Debug("OverlayDataPack section unavailable in {0}, overlay will be unavailable", Path.GetFileName(FileName));
				return;
			}
			format80Data = Convert.FromBase64String(overlayDataSection.ConcatenatedValues());
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
					short direction = short.Parse(entries[7]);
					bool onBridge = entries[11] == "1";
					var i = new Infantry(owner, name, health, direction, onBridge);
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
					short direction = short.Parse(entries[5]);
					bool onBridge = entries[10] == "1";
					var u = new Unit(owner, name, health, direction, onBridge);
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
					short direction = short.Parse(entries[5]);
					bool onBridge = entries[entries.Length - 4] == "1";
					var a = new Aircraft(owner, name, health, direction, onBridge);
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
					short direction = short.Parse(entries[5]);
					var s = new Structure(owner, name, health, direction);
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


	}
}
