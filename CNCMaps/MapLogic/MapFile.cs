using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CNCMaps.Encodings;
using CNCMaps.MapLogic;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	/// <summary>Map file.</summary>
	class MapFile : IniFile {
		/// <summary>Values that represent MapType.</summary>
		enum MapType {
			/// <summary>A Red Alert 2 map</summary>
			RedAlert2,
			/// <summary>A Yuri's Revenge map</summary>
			YurisRevenge
		}
		MapType mapType = MapType.RedAlert2;

		Rectangle fullSize, localSize;
		EngineType engineType;
		Theater theater;
		IniFile rules;
		IniFile art;

		TileLayer tiles;
		OverlayObject[,] overlayObjects;
		SmudgeObject[,] smudgeObjects;
		TerrainObject[,] terrainObjects;
		StructureObject[,] structureObjects;
		List<InfantryObject>[,] infantryObjects;
		UnitObject[,] unitObjects;
		AircraftObject[,] aircraftObjects;

		List<string> houses = new List<string>();
		Dictionary<string, Color> countryColors = new Dictionary<string, Color>();
		Dictionary<string, Color> namedColors = new Dictionary<string, Color>();

		Lighting lighting;
		List<LightSource> lightSources = new List<LightSource>();
		List<Palette> PalettePerLevel = new List<Palette>(15);
		List<Palette> PalettesToBeRecalculated = new List<Palette>(15);

		private DrawingSurface drawingSurface;


		/// <summary>Constructor.</summary>
		/// <param name="baseStream">The base stream.</param>
		public MapFile(Stream baseStream, string filename = "") : this(baseStream, filename, 0, baseStream.Length) { }

		public MapFile(Stream baseStream, string filename, int offset, long length, bool isBuffered = true) :
			base(baseStream, filename, offset, length, isBuffered) { }

		/// <summary>Gets the determine map name. </summary>
		/// <returns>The filename to save the map as</returns>
		internal string DetermineMapName() {
			string infile_nopath = Path.GetFileNameWithoutExtension(this.FileName);

			IniSection Basic = GetSection("Basic");
			if (Basic.ReadBool("Official") == false)
				return Basic.ReadString("Name", infile_nopath);

			string mapext = Path.GetExtension(this.FileName);

			string pktfile;
			bool custom_pkt = false;
			bool isyr = this.mapType == MapType.YurisRevenge;
			string csfEntry = "";
			string mapName = "";
			PktFile.PktMapEntry mapEntry = null;
			bool isMission;

			// campaign mission
			if (!Basic.ReadBool("MultiplayerOnly") && Basic.ReadBool("Official")) {
				MissionsFile mf = VFS.Open<MissionsFile>(isyr ? "missionmd.ini" : "mission.ini");
				var me = mf.GetMissionEntry(Path.GetFileName(this.FileName));
				csfEntry = me.UIName;
				isMission = true;
			}
			// multiplayer map
			else {
				isMission = false;
				if (mapext == ".mmx" || mapext == ".yro") {
					// this file contains the pkt file
					VFS.Add(this.FileName);
					pktfile = infile_nopath + ".pkt";
					custom_pkt = true;
					if (mapext == ".yro") // definitely YR map
						isyr = true;
				}
				else if (isyr)
					pktfile = "missionsmd.pkt";
				else
					pktfile = "missions.pkt";

				PktFile pkt = VFS.Open<PktFile>(pktfile);
				string pkt_mapname = "";
				if (custom_pkt)
					pkt_mapname = pkt.MapEntries.First().Key;
				else {
					// fallback for multiplayer maps with, .map extension,
					// no YR objects so assumed to be ra2, but actually meant to be used on yr
					if (!isyr && mapext == ".map" && !pkt.MapEntries.ContainsKey(infile_nopath) && Basic.ReadBool("MultiplayerOnly")) {
						pktfile = "missionsmd.pkt";
						var pkt_yr = VFS.Open<PktFile>(pktfile);
						if (pkt_yr.MapEntries.ContainsKey(infile_nopath)) {
							isyr = true;
							pkt = pkt_yr;
						}
					}
				}
				// last resort
				if (pkt_mapname == "")
					pkt_mapname = infile_nopath;

				mapEntry = pkt.GetMapEntry(pkt_mapname);
				if (mapEntry != null)
					csfEntry = mapEntry.Description;
			}

			if (csfEntry != "") {
				csfEntry = csfEntry.ToLower();

				string csfFile = isyr ? "ra2md.csf" : "ra2.csf";
				Logger.WriteLine("Loading csf file {0}", csfFile);
				CsfFile csf = VFS.Open<CsfFile>(csfFile);
				mapName = csf.GetValue(csfEntry);

				if (mapName.IndexOf(" (") != -1)
					mapName = mapName.Substring(0, mapName.IndexOf(" ("));

				if (isMission) {
					if (mapName.Contains("Operation: ")) {
						string missionMapName = Path.GetFileName(this.FileName);
						if (char.IsDigit(missionMapName[3]) && char.IsDigit(missionMapName[4])) {
							string missionNr = Path.GetFileName(this.FileName).Substring(3, 2);
							mapName = mapName.Substring(0, mapName.IndexOf(":")) + " " + missionNr + " -" + mapName.Substring(mapName.IndexOf(":") + 1);
						}
					}
				}
				else {
					// not standard map
					if ((mapEntry.GameModes & PktFile.GameMode.Standard) == 0) {
						if ((mapEntry.GameModes & PktFile.GameMode.Megawealth) == PktFile.GameMode.Megawealth)
							mapName += " (Megawealth)";
						if ((mapEntry.GameModes & PktFile.GameMode.Duel) == PktFile.GameMode.Duel)
							mapName += " (Land Rush)";
						if ((mapEntry.GameModes & PktFile.GameMode.NavalWar) == PktFile.GameMode.NavalWar)
							mapName += " (Naval War)";
					}
				}
			}
			mapName = MakeValidFileName(mapName);
			Logger.WriteLine("Mapname found: {0}", mapName);
			return mapName;
		}

		/// <summary>Makes a valid file name.</summary>
		/// <param name="name">The filename to be made valid.</param>
		/// <returns>The valid file name.</returns>
		private static string MakeValidFileName(string name) {
			string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
			string invalidReStr = string.Format(@"[{0}]+", invalidChars);
			return Regex.Replace(name, invalidReStr, "_");
		}

		/// <summary>Loads a map. </summary>
		/// <param name="et">The engine type to be forced, or autodetect.</param>
		internal void LoadMap(EngineType et = EngineType.AutoDetect) {
			var map = GetSection("Map");
			string[] size = map.ReadString("Size").Split(',');
			fullSize = new Rectangle(int.Parse(size[0]), int.Parse(size[1]), int.Parse(size[2]), int.Parse(size[3]));
			size = map.ReadString("LocalSize").Split(',');
			localSize = new Rectangle(int.Parse(size[0]), int.Parse(size[1]), int.Parse(size[2]), int.Parse(size[3]));

			ReadAllObjects();

			// if we have to autodetect, we need to load rules.ini,
			// and we don't want to parse it again when constructing the theater
			if (et == EngineType.AutoDetect) {
				rules = VFS.Open<IniFile>("rules.ini");
				this.engineType = DetectMapType(rules);
				if (this.engineType == EngineType.YurisRevenge) {
					rules = VFS.Open<IniFile>("rulesmd.ini");
					art = VFS.Open<IniFile>("artmd.ini");
				}
				else art = VFS.Open<IniFile>("art.ini");
			}
			else {
				if (this.engineType == EngineType.YurisRevenge) {
					rules = VFS.Open<IniFile>("rulesmd.ini");
					art = VFS.Open<IniFile>("artmd.ini");
				}
				else {
					rules = VFS.Open<IniFile>("rules.ini");
					art = VFS.Open<IniFile>("art.ini");
				}
			}
			theater = new Theater(ReadString("Map", "Theater"), this.engineType, rules, art);
			theater.Initialize();

			// we need foundations from the theater to plce the structures at the correct tile,
			// so we load these last
			Logger.WriteLine("Reading map structures");
			ReadStructures();

			PalettesToBeRecalculated.AddRange(theater.GetPalettes());

			LoadColors();
			LoadCountries();
			LoadHouses();
			theater.GetTileCollection().RecalculateTileSystem(this.tiles);
			RecalculateOreSpread();
			LoadLighting();
			CreateLevelPalettes();
			LoadLightSources();
			ApplyLightSources();
			ApplyRemappables();
			// now everything is loaded and we can prepare the palettes before using them to draw
			RecalculateAllPalettes();
		}

		/// <summary>Loads the countries. </summary>
		private void LoadCountries() {
			Logger.WriteLine("Loading countries");

			var countriesSection = rules.GetSection("Countries");
			foreach (var entry in countriesSection.OrderedEntries) {
				IniSection countrySection = rules.GetSection(entry.Value);
				this.countryColors[entry.Value] = this.namedColors[countrySection.ReadString("Color")];
			}
		}

		/// <summary>Loads the colors. </summary>
		private void LoadColors() {
			var colorsSection = rules.GetSection("Colors");
			foreach (var entry in colorsSection.OrderedEntries) {
				string[] colorComponents = entry.Value.Split(',');
				var h = new HsvColor(int.Parse(colorComponents[0]),
					int.Parse(colorComponents[1]), int.Parse(colorComponents[2]));
				namedColors[entry.Key] = h.ToRGB();
			}
		}

		/// <summary>Loads the houses. </summary>
		private void LoadHouses() {
			Logger.WriteLine("Loading houses");
			IniSection housesSection = GetSection("Houses");
			if (housesSection == null) return;
			foreach (var v in housesSection.OrderedEntries) {
				var houseSection = GetSection(v.Value);
				if (houseSection == null) continue;
				string color;
				if (v.Value == "Neutral" || v.Value == "Special")
					color = "LightGrey"; // this is hardcoded in the game
				else
					color = houseSection.ReadString("Color"); ;
				countryColors[v.Value] = namedColors[color];
			}
		}

		/// <summary>Detect map type.</summary>
		/// <param name="rules">The rules.ini file to be used.</param>
		/// <returns>The engine to be used to render this map.</returns>
		private EngineType DetectMapType(IniFile rules) {
			Logger.WriteLine("Determining map name");

			if (this.ReadBool("Basic", "RequiredAddon"))
				return EngineType.YurisRevenge;

			string theater = this.ReadString("Map", "Theater").ToLower();
			// decision based on theatre
			if (theater == "lunar" || theater == "newurban" || theater == "desert")
				return EngineType.YurisRevenge;

			// decision based on overlay/trees/structs
			if (!AllObjectsFromRA2(rules))
				return EngineType.YurisRevenge;

			// decision based on max tile/threatre
			int maxTileNum = int.MinValue;
			foreach (MapTile t in tiles)
				maxTileNum = Math.Max(t.TileNum, maxTileNum);

			if (theater == "temperate") {
				if (maxTileNum > 838) return EngineType.YurisRevenge;
				return EngineType.RedAlert2;
			}
			else if (theater == "urban") {
				if (maxTileNum > 1077) return EngineType.YurisRevenge;
				return EngineType.RedAlert2;
			}
			else if (theater == "snow") {
				if (maxTileNum > 798) return EngineType.YurisRevenge;
				return EngineType.RedAlert2;
			}
			// decision based on extension
			else if (Path.GetExtension(this.FileName) == ".yrm") return EngineType.YurisRevenge;
			else return EngineType.RedAlert2;
		}

		/// <summary>Tests whether all objects on the map are present in RA2</summary>
		/// <param name="rules">The rules.ini file from RA2.</param>
		/// <returns>True if all objects are from RA2, else false.</returns>
		private bool AllObjectsFromRA2(IniFile rules) {
			foreach (var obj in this.overlayObjects)
				if (obj.OverlayID > 246) return false;
			IniSection objSection = rules.GetSection("TerrainTypes");
			for (int y = 0; y < fullSize.Height; y++) {
				for (int x = 0; x < fullSize.Width * 2 - 1; x++) {
					var obj = terrainObjects[x, y];
					int idx = objSection.FindValueIndex(obj.Name);
					if (idx == -1 || idx > 73) return false;
				}
			}

			objSection = rules.GetSection("InfantryTypes");
			for (int y = 0; y < fullSize.Height; y++) {
				for (int x = 0; x < fullSize.Width * 2 - 1; x++) {
					var objList = infantryObjects[x, y];
					foreach (var obj in objList) {
						int idx = objSection.FindValueIndex(obj.Name);
						if (idx == -1 || idx > 45) return false;
					}
				}
			}

			objSection = rules.GetSection("VehicleTypes");
			for (int y = 0; y < fullSize.Height; y++) {
				for (int x = 0; x < fullSize.Width * 2 - 1; x++) {
					var obj = unitObjects[x, y];
					int idx = objSection.FindValueIndex(obj.Name);
					if (idx == -1 || idx > 57) return false;
				}
			}

			objSection = rules.GetSection("AircraftTypes");
			for (int y = 0; y < fullSize.Height; y++) {
				for (int x = 0; x < fullSize.Width * 2 - 1; x++) {
					var obj = aircraftObjects[x, y];
					int idx = objSection.FindValueIndex(obj.Name);
					if (idx == -1 || idx > 9) return false;
				}
			}

			objSection = rules.GetSection("BuildingTypes");
			IniSection objSectionAlt = rules.GetSection("OverlayTypes");
			for (int y = 0; y < fullSize.Height; y++) {
				for (int x = 0; x < fullSize.Width * 2 - 1; x++) {
					var obj = structureObjects[x, y];
					int idx1 = objSection.FindValueIndex(obj.Name);
					int idx2 = objSectionAlt.FindValueIndex(obj.Name);
					if (idx1 == -1 && idx2 == -1) return false;
					else if (idx1 != -1 && idx1 > 303)
						return false;
					else if (idx2 != -1 && idx2 > 246)
						return false;
				}
			}

			// no need to test smudge types as no new ones were introduced with yr
			return true;
		}

		/// <summary>Reads all objects. </summary>
		private void ReadAllObjects() {
			Logger.WriteLine("Reading tiles");
			ReadTiles();

			Logger.WriteLine("Reading map overlay");
			ReadOverlay();

			Logger.WriteLine("Reading map overlay objects");
			ReadTerrain();

			Logger.WriteLine("Reading map terrain object");
			ReadSmudges();

			Logger.WriteLine("Reading infantry on map");
			ReadInfantry();

			Logger.WriteLine("Reading vehicles on map");
			ReadUnits();

			Logger.WriteLine("Reading aircraft on map");
			ReadAircraft();
		}

		/// <summary>Reads the tiles. </summary>
		private void ReadTiles() {
			var mapSection = GetSection("IsoMapPack5");
			byte[] lzoData = Convert.FromBase64String(mapSection.ConcatenatedValues());
			int cells = (fullSize.Width * 2 - 1) * fullSize.Height;
			int lzoPackSize = cells * 11 + 4; // last 4 bytes contains a lzo pack header saying no more data is left

			byte[] isoMapPack = new byte[lzoPackSize];
			uint total_decompress_size = Format5.DecodeInto(lzoData, isoMapPack);

			tiles = new TileLayer(fullSize.Size);
			MemoryFile mf = new MemoryFile(isoMapPack);
			int numtiles = 0;
			for (int i = 0; i < cells; i++) {
				ushort rx = mf.ReadUInt16();
				ushort ry = mf.ReadUInt16();
				short tilenum = mf.ReadInt16();
				short zero1 = mf.ReadInt16();
				ushort subtile = mf.ReadByte();
				short z = mf.ReadByte();
				byte zero2 = mf.ReadByte();

				ushort dx = (ushort)(rx - ry + fullSize.Width - 1);
				ushort dy = (ushort)(rx + ry - fullSize.Width - 1);
				numtiles++;
				this.tiles[dx, dy / 2] = new MapTile(dx, dy, rx, ry, z, tilenum, subtile);
			}
		}

		/// <summary>Reads the terrain. </summary>
		private void ReadTerrain() {
			IniSection terrainSection = GetSection("Terrain");
			terrainObjects = new TerrainObject[fullSize.Width * 2 - 1, fullSize.Height];
			if (terrainSection == null) return;
			foreach (var v in terrainSection.OrderedEntries) {
				int pos = int.Parse(v.Key);
				string name = v.Value;
				int rx = pos % 1000;
				int ry = pos / 1000;
				TerrainObject t = new TerrainObject(name);
				tiles.GetTileR(rx, ry).AddObject(t);
				this.terrainObjects[t.Tile.Dx, t.Tile.Dy / 2] = t;
			}
		}

		/// <summary>Reads the smudges. </summary>
		private void ReadSmudges() {
			IniSection smudgesSection = GetSection("Smudge");
			smudgeObjects = new SmudgeObject[fullSize.Width * 2 - 1, fullSize.Height];
			if (smudgesSection == null) return;
			foreach (var v in smudgesSection.OrderedEntries) {
				string[] entries = v.Value.Split(',');
				string name = entries[0];
				int rx = int.Parse(entries[1]);
				int ry = int.Parse(entries[2]);
				SmudgeObject s = new SmudgeObject(name);
				tiles.GetTileR(rx, ry).AddObject(s);
				this.smudgeObjects[s.Tile.Dx, s.Tile.Dy / 2] = s;
			}
		}

		/// <summary>Reads the overlay.</summary>
		private void ReadOverlay() {
			IniSection overlaySection = GetSection("OverlayPack");
			byte[] format80Data = Convert.FromBase64String(overlaySection.ConcatenatedValues());
			byte[] overlayPack = new byte[1 << 18];
			Format5.DecodeInto(format80Data, overlayPack, 80);

			IniSection overlayDataSection = GetSection("OverlayDataPack");
			format80Data = Convert.FromBase64String(overlayDataSection.ConcatenatedValues());
			byte[] overlayDataPack = new byte[1 << 18];
			Format5.DecodeInto(format80Data, overlayDataPack, 80);

			overlayObjects = new OverlayObject[fullSize.Width * 2 - 1, fullSize.Height];

			foreach (MapTile t in tiles) {
				int idx = t.Rx + 512 * t.Ry;
				byte overlay_id = overlayPack[idx];
				if (overlay_id != 0xFF) {
					byte overlay_value = overlayDataPack[idx];
					OverlayObject ovl = new OverlayObject(overlay_id, overlay_value);
					t.AddObject(ovl);
					this.overlayObjects[ovl.Tile.Dx, ovl.Tile.Dy / 2] = ovl;
				}
			}
		}

		/// <summary>Reads the structures.</summary>
		private void ReadStructures() {
			IniSection structsSection = GetSection("Structures");
			structureObjects = new StructureObject[fullSize.Width * 2 - 1, fullSize.Height];
			if (structsSection == null) return;
			foreach (var v in structsSection.OrderedEntries) {
				string[] entries = v.Value.Split(',');
				string owner = entries[0];
				string name = entries[1];
				short health = short.Parse(entries[2]);
				int rx = int.Parse(entries[3]);
				int ry = int.Parse(entries[4]);
				short direction = short.Parse(entries[5]);
				StructureObject s = new StructureObject(owner, name, health, direction);

				Size foundation = theater.GetFoundation(s);
				s.DrawTile = tiles.GetTileR(rx + foundation.Width - 1, ry + foundation.Height - 1);
				s.DrawTile.AddObject(s);
				s.Tile = tiles.GetTileR(rx, ry);
				this.structureObjects[s.Tile.Dx, s.Tile.Dy / 2] = s;
			}
		}

		/// <summary>Reads the infantry. </summary>
		private void ReadInfantry() {
			IniSection infantrySection = GetSection("Infantry");
			infantryObjects = new List<InfantryObject>[fullSize.Width * 2 - 1, fullSize.Height];
			if (infantrySection == null) return;
			foreach (var v in infantrySection.OrderedEntries) {
				string[] entries = v.Value.Split(',');
				string owner = entries[0];
				string name = entries[1];
				short health = short.Parse(entries[2]);
				int rx = int.Parse(entries[3]);
				int ry = int.Parse(entries[4]);
				short direction = short.Parse(entries[7]);
				InfantryObject i = new InfantryObject(owner, name, health, direction);
				tiles.GetTileR(rx, ry).AddObject(i);
				var infantryList = this.infantryObjects[i.Tile.Dx, i.Tile.Dy / 2];
				if (infantryList == null)
					this.infantryObjects[i.Tile.Dx, i.Tile.Dy / 2] = infantryList = new List<InfantryObject>();
				infantryList.Add(i);
			}
		}

		/// <summary>Reads the units.</summary>
		private void ReadUnits() {
			IniSection unitsSection = GetSection("Units");
			unitObjects = new UnitObject[fullSize.Width * 2 - 1, fullSize.Height];
			if (unitsSection == null) return;
			foreach (var v in unitsSection.OrderedEntries) {
				string[] entries = v.Value.Split(',');
				string owner = entries[0];
				string name = entries[1];
				short health = short.Parse(entries[2]);
				int rx = int.Parse(entries[3]);
				int ry = int.Parse(entries[4]);
				short direction = short.Parse(entries[5]);
				UnitObject u = new UnitObject(owner, name, health, direction);
				tiles.GetTileR(rx, ry).AddObject(u);
				this.unitObjects[u.Tile.Dx, u.Tile.Dy / 2] = u;
			}
		}

		/// <summary>Reads the aircraft.</summary>
		private void ReadAircraft() {
			IniSection aircraftSection = GetSection("Aircraft");
			aircraftObjects = new AircraftObject[fullSize.Width * 2 - 1, fullSize.Height];
			if (aircraftSection == null) return;
			foreach (var v in aircraftSection.OrderedEntries) {
				string[] entries = v.Value.Split(',');
				string owner = entries[0];
				string name = entries[1];
				short health = short.Parse(entries[2]);
				int rx = int.Parse(entries[3]);
				int ry = int.Parse(entries[4]);
				short direction = short.Parse(entries[5]);
				AircraftObject a = new AircraftObject(owner, name, health, direction);
				tiles.GetTileR(rx, ry).AddObject(a);
				this.aircraftObjects[a.Tile.Dx, a.Tile.Dy / 2] = a;
			}
		}

		private void RecalculateOreSpread() {
			Logger.WriteLine("Recalculating ore-spread");
			foreach (OverlayObject o in this.overlayObjects) {
				if (o == null) continue;
				// The value consists of the sum of all x's with a little magic offsets
				// plus the sum of all y's with also a little magic offset, and also
				// everything is calculated modulo 12
				if (o.IsOre()) {
					int x = o.Tile.Dx;
					int y = o.Tile.Dy;
					double y_inc = ((((y - 9) / 2) % 12) * (((y - 8) / 2) % 12)) % 12;
					double x_inc = ((((x - 13) / 2) % 12) * (((x - 12) / 2) % 12)) % 12;

					// x_inc may be > y_inc so adding a big number outside of cell bounds
					// will surely keep num positive
					int num = (int)(y_inc - x_inc + 120000);
					num %= 12;

					// replace ore
					o.OverlayID = (byte)(OverlayObject.MinOreID + num);
				}

				else if (o.IsGem()) {
					int x = o.Tile.Dx;
					int y = o.Tile.Dy;
					double y_inc = ((((y - 9) / 2) % 12) * (((y - 8) / 2) % 12)) % 12;
					double x_inc = ((((x - 13) / 2) % 12) * (((x - 12) / 2) % 12)) % 12;

					// x_inc may be > y_inc so adding a big number outside of cell bounds
					// will surely keep num positive
					int num = (int)(y_inc - x_inc + 120000);
					num %= 12;

					// replace gems
					o.OverlayID = (byte)(OverlayObject.MinGemsID + num);
				}
			}
		}

		private void LoadLighting() {
			Logger.WriteLine("Loading lighting");
			this.lighting = new Lighting(GetSection("Lighting"));
		}

		private void CreateLevelPalettes() {
			Logger.WriteLine("Create per-height palettes");
			PaletteCollection palettes = theater.GetPalettes();
			for (int i = 0; i < 15; i++) {
				Palette isoHeight = palettes.isoPalette.Clone();
				isoHeight.ApplyLighting(this.lighting, i);
				PalettePerLevel.Add(isoHeight);
				PalettesToBeRecalculated.Add(isoHeight);
			}

			foreach (MapTile t in tiles) {
				t.Palette = PalettePerLevel[t.Z];
				t.PaletteIsOriginal = true;
			}
		}

		static string[] lampNames = new string[] {
			"REDLAMP", "BLUELAMP", "GRENLAMP", "YELWLAMP", "PURPLAMP", "INORANLAMP", "INGRNLMP", "INREDLMP", "INBLULMP", "INGALITE",
			"INYELWLAMP", "INPURPLAMP", "NEGLAMP", "NERGRED", "TEMMORLAMP", "TEMPDAYLAMP", "TEMDAYLAMP", "TEMDUSLAMP", "TEMNITLAMP", "SNOMORLAMP",
			"SNODAYLAMP", "SNODUSLAMP", "SNONITLAMP"
		};
		private void LoadLightSources() {
			Logger.WriteLine("Loading light sources");
			foreach (StructureObject s in structureObjects) {
				if (s == null) continue;
				if (lampNames.Contains(s.Name)) {
					LightSource ls = new LightSource(rules.GetSection(s.Name), this.lighting);
					ls.Tile = s.Tile;
					this.lightSources.Add(ls);
					// make sure these don't get drawn
				}
			}
		}

		private void ApplyLightSources() {
			foreach (LightSource s in lightSources) {
				foreach (MapTile t in this.tiles) {
					// make sure this tile can only end up in the "to-be-recalculated list" once
					bool wasOriginal = t.PaletteIsOriginal;
					bool tileAffected = t.ApplyLamp(s);
					if (wasOriginal && tileAffected) {
						PalettesToBeRecalculated.Add(t.Palette);
					}
				}
			}
		}

		private void ApplyRemappables() {
			foreach (StructureObject s in structureObjects) {
				if (s == null) continue;
				s.Palette = theater.GetPalette(s).Clone();
				s.Palette.Remap(this.countryColors[s.Owner]);
				PalettesToBeRecalculated.Add(s.Palette);
			}
			foreach (UnitObject u in unitObjects) {
				if (u == null) continue;
				u.Palette = theater.GetPalette(u).Clone();
				u.Palette.Remap(this.countryColors[u.Owner]);
				PalettesToBeRecalculated.Add(u.Palette);
			}
		}

		private void RecalculateAllPalettes() {
			Logger.WriteLine("Calculating palette-values for all objects");
			foreach (Palette p in PalettesToBeRecalculated)
				p.Recalculate();
		}

		public void DrawTiledStartPositions() {
			Logger.WriteLine("Marking tiled startpositions");
			IniSection basic = GetSection("Basic");
			if (basic == null || !basic.ReadBool("MultiplayerOnly")) return;
			IniSection waypoints = GetSection("Waypoints");
			Palette red = Palette.MakePalette(Color.Red);

			foreach (var entry in waypoints.OrderedEntries) {
				if (int.Parse(entry.Key) >= 8)
					continue;
				int pos = int.Parse(entry.Value);
				int wy = pos / 1000;
				int wx = pos - wy * 1000;

				// Draw 4x4 cell around start pos
				for (int x = wx - 2; x < wx + 2; x++) {
					for (int y = wy - 2; y < wy + 2; y++) {
						MapTile t = tiles.GetTileR(x, y);
						if (t != null) {
							t.Palette = Palette.MergePalettes(t.Palette, red, 0.4);
						}
					}
				}
			}
		}

		public unsafe void DrawSquaredStartPositions() {
			Logger.WriteLine("Marking squared startpositions");
			IniSection basic = GetSection("Basic");
			if (basic == null || !basic.ReadBool("MultiplayerOnly")) return;
			IniSection waypoints = GetSection("Waypoints");

			foreach (var entry in waypoints.OrderedEntries) {
				if (int.Parse(entry.Key) >= 8)
					continue;
				int pos = int.Parse(entry.Value);
				int wy = pos / 1000;
				int wx = pos - wy * 1000;

				MapTile t = tiles.GetTileR(wx, wy);
				int dest_x = t.Dx * 30;
				int dest_y = (t.Dy - t.Z) * 15;

				bool vert = fullSize.Height * 2 > fullSize.Width;
				int radius;
				if (vert)
					radius = 10 * fullSize.Height * 15 / 144;
				else
					radius = 10 * fullSize.Width * 30 / 133;

				int h = radius, w = radius;
				for (int draw_y = dest_y - h / 2; draw_y < dest_y + h; draw_y++) {
					for (int draw_x = dest_x - w / 2; draw_x < dest_x + w; draw_x++) {
						byte* p = (byte*)drawingSurface.bmd.Scan0 + draw_y * drawingSurface.bmd.Stride + 3 * draw_x;
						*p++ = 0x00;
						*p++ = 0x00;
						*p++ = 0xFF;
					}
				}
			}
		}

		private int FindCutoffHeight() {
			int y = fullSize.Height - 1;
			int highest_height = 0;
			for (int x = 0; x < fullSize.Width; x++) {
				MapTile t = tiles.GetTile(x, y);
				if (t != null)
					highest_height = Math.Max(highest_height, t.Z);
			}
			return highest_height;
		}

		public Rectangle GetLocalSizePixels() {
			int left = Math.Max(localSize.Left * 60, 0),
			top = Math.Max(localSize.Top * 30 - 90, 0);
			int width = localSize.Width * 60;
			int height = localSize.Height * 30 + 135;
			int height2 = (fullSize.Height - ((FindCutoffHeight() / 2 - 1) % 2)) * 30;
			height = Math.Min(height, height2);
			return new Rectangle(left, top, width, height);
		}

		public void MarkOreAndGems() {
			Logger.WriteLine("Marking ore and gems");
			Palette yellow = Palette.MakePalette(Color.Yellow);
			Palette purple = Palette.MakePalette(Color.Purple);
			foreach (var o in overlayObjects) {
				if (o.IsOre())
					o.Tile.Palette = Palette.MergePalettes(o.Tile.Palette, yellow, o.OverlayValue / 11.0 * 0.6 + 0.1);

				else if (o.IsGem())
					o.Tile.Palette = Palette.MergePalettes(o.Tile.Palette, purple, o.OverlayValue / 11.0 * 0.6 + 0.25);
			}
		}

		//		}
		//	}
		//	for (int y = 0; y < fullSize.Height; y++) {
		//		for (int x = 0; x < fullSize.Width * 2 - 1; x++) {


		internal void DrawMap() {
			Logger.WriteLine("Drawing map");
			drawingSurface = new DrawingSurface(fullSize.Width * 60, fullSize.Height * 30, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			var tileCollection = theater.GetTileCollection();
			for (int y = 0; y < fullSize.Height; y++) {
				for (int x = fullSize.Width * 2 - 2; x >= 0; x -= 2) {
					tileCollection.DrawTile(tiles.GetTile(x, y), drawingSurface);
					List<RA2Object> objs = GetObjectsAt(x, y);
					foreach (RA2Object o in objs)
						theater.DrawObject(o, drawingSurface);
				}
				for (int x = fullSize.Width * 2 - 3; x >= 0; x -= 2) {
					tileCollection.DrawTile(tiles.GetTile(x, y), drawingSurface);
					List<RA2Object> objs = GetObjectsAt(x, y);
					foreach (RA2Object o in objs)
						theater.DrawObject(o, drawingSurface);
				}
			}
		}

		private List<RA2Object> GetObjectsAt(int x, int y) {
			List<RA2Object> ret = new List<RA2Object>();
			MapTile t = tiles.GetTile(x, y);

			if (smudgeObjects[x, y] != null)
				ret.Add(smudgeObjects[x, y]);

			if (overlayObjects[x, y] != null)
				ret.Add(overlayObjects[x, y]);

			if (terrainObjects[x, y] != null)
				ret.Add(terrainObjects[x, y]);

			if (infantryObjects[x, y] != null)
				ret.AddRange(infantryObjects[x, y]);

			if (aircraftObjects[x, y] != null)
				ret.Add(aircraftObjects[x, y]);

			if (unitObjects[x, y] != null)
				ret.Add(unitObjects[x, y]);

			foreach (var s in structureObjects)
				if (s != null && s.DrawTile == t)
					ret.Add(s);

			return ret;
		}

		public DrawingSurface GetDrawingSurface() {
			return drawingSurface;
		}
	}
}