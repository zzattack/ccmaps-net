using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CNCMaps.Encodings;
using CNCMaps.FileFormats;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.MapLogic {

	/// <summary>Map file.</summary>
	class MapFile : IniFile {
		public EngineType EngineType { get; private set; }

		private Rectangle _fullSize, _localSize;
		private Theater _theater;
		private IniFile _rules;
		private IniFile _art;

		private TileLayer _tiles;
		private OverlayObject[,] _overlayObjects;
		private SmudgeObject[,] _smudgeObjects;
		private TerrainObject[,] _terrainObjects;
		private StructureObject[,] _structureObjects;
		private List<InfantryObject>[,] _infantryObjects;
		private UnitObject[,] _unitObjects;
		private AircraftObject[,] _aircraftObjects;

		private readonly Dictionary<string, Color> _countryColors = new Dictionary<string, Color>();
		private readonly Dictionary<string, Color> _namedColors = new Dictionary<string, Color>();

		private Lighting _lighting;
		private readonly List<LightSource> _lightSources = new List<LightSource>();
		private readonly List<Palette> _palettePerLevel = new List<Palette>(15);
		private readonly List<Palette> _palettesToBeRecalculated = new List<Palette>(15);

		private DrawingSurface _drawingSurface;

		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		/// <summary>Constructor.</summary>
		/// <param name="baseStream">The base stream.</param>
		public MapFile(Stream baseStream, string filename = "")
			: this(baseStream, filename, 0, baseStream.Length) {
		}

		public MapFile(Stream baseStream, string filename, int offset, long length, bool isBuffered = true) :
			base(baseStream, filename, offset, length, isBuffered) {
			if (isBuffered)
				Close(); // we no longer need the file handle anyway
		}

		/// <summary>Gets the determine map name. </summary>
		/// <returns>The filename to save the map as</returns>
		internal string DetermineMapName(EngineType engine) {
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(FileName);

			IniSection basic = GetSection("Basic");
			if (basic.ReadBool("Official") == false)
				return StripPlayersFromName(basic.ReadString("Name", fileNameWithoutExtension));

			string mapExt = Path.GetExtension(FileName);
			string missionName = "";
			string mapName = "";
			PktFile.PktMapEntry pktMapEntry = null;
			MissionsFile.MissionEntry missionEntry = null;

			// campaign mission
			if (!basic.ReadBool("MultiplayerOnly") && basic.ReadBool("Official")) {
				string missionsFile;
				switch (engine) {
					case EngineType.TiberianSun:
					case EngineType.RedAlert2:
						missionsFile = "mission.ini";
						break;
					case EngineType.FireStorm:
						missionsFile = "mission1.ini";
						break;
					case EngineType.YurisRevenge:
						missionsFile = "missionmd.ini";
						break;
					default:
						throw new ArgumentOutOfRangeException("engine");
				}
				var mf = VFS.Open<MissionsFile>(missionsFile);
				missionEntry = mf.GetMissionEntry(Path.GetFileName(FileName));
				missionName = (engine >= EngineType.RedAlert2) ? missionEntry.UIName : missionEntry.Name;
			}

			else {
				// multiplayer map
				string pktEntryName = fileNameWithoutExtension;
				PktFile pkt = null;

				if (mapExt == ".mmx" || mapExt == ".yro") {
					// this is an 'official' map 'archive' containing a PKT file with its name
					try {
						var mix = new MixFile(File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
						pkt = mix.OpenFile(fileNameWithoutExtension + ".pkt", FileFormat.Pkt) as PktFile;
						// pkt file is cached by default, so we can close the handle to the file
						mix.Close();

						if (pkt != null && pkt.MapEntries.Count > 0)
							pktEntryName = pkt.MapEntries.First().Key;
					}
					catch (ArgumentException) { }
				}

				else {
					// determine pkt file based on engine
					switch (engine) {
						case EngineType.TiberianSun:
						case EngineType.RedAlert2:
							pkt = VFS.Open<PktFile>("missions.pkt");
							break;
						case EngineType.FireStorm:
							pkt = VFS.Open<PktFile>("multi01.pkt");
							break;
						case EngineType.YurisRevenge:
							pkt = VFS.Open<PktFile>("missionsmd.pkt");
							break;
						default:
							throw new ArgumentOutOfRangeException("engine");
					}
				}


				// fallback for multiplayer maps with, .map extension,
				// no YR objects so assumed to be ra2, but actually meant to be used on yr
				if (mapExt == ".map" && pkt != null && !pkt.MapEntries.ContainsKey(pktEntryName) && engine >= EngineType.RedAlert2) {
					VFS.GetInstance().ScanMixDir(EngineType.YurisRevenge, Program.Settings.MixFilesDirectory);
					pkt = VFS.Open<PktFile>("missionsmd.pkt");
				}

				if (pkt != null && !string.IsNullOrEmpty(pktEntryName))
					pktMapEntry = pkt.GetMapEntry(pktEntryName);
			}

			// now, if we have a map entry from a PKT file, 
			// for TS we are done, but for RA2 we need to look in the CSV file for the translated mapname
			if (engine <= EngineType.FireStorm) {
				if (pktMapEntry != null)
					mapName = pktMapEntry.Description;
				else if (missionEntry != null) {
					if (engine == EngineType.TiberianSun) {
						string campaignSide = missionEntry.Briefing.Length >= 3 ? missionEntry.Briefing.Substring(0, 3) : "XXX";
						string missionNumber = missionEntry.Briefing.Length > 3 ? missionEntry.Briefing.Substring(3) : "";
						mapName = string.Format("{0} {1} - {2}", campaignSide, missionNumber.TrimEnd('A').PadLeft(2, '0'), missionName);
					}
					else {
						// FS map names are constructed a bit easier
						mapName = missionName.Replace(":", " - ");
					}
				}
				else if (!string.IsNullOrEmpty(basic.ReadString("Name")))
					mapName = basic.ReadString("Name", fileNameWithoutExtension);
			}

				// if this is a RA2/YR mission (csfEntry set) or official map with valid pktMapEntry
			else if (missionEntry != null || pktMapEntry != null) {
				string csfEntryName = missionEntry != null ? missionName : pktMapEntry.Description;

				string csfFile = engine == EngineType.YurisRevenge ? "ra2md.csf" : "ra2.csf";
				Logger.Info("Loading csf file {0}", csfFile);
				var csf = VFS.Open<CsfFile>(csfFile);
				mapName = csf.GetValue(csfEntryName.ToLower());

				if (missionEntry != null) {
					if (mapName.Contains("Operation: ")) {
						string missionMapName = Path.GetFileName(FileName);
						if (char.IsDigit(missionMapName[3]) && char.IsDigit(missionMapName[4])) {
							string missionNr = Path.GetFileName(FileName).Substring(3, 2);
							mapName = mapName.Substring(0, mapName.IndexOf(":")) + " " + missionNr + " -" +
									  mapName.Substring(mapName.IndexOf(":") + 1);
						}
					}
				}
				else {
					// not standard map
					if ((pktMapEntry.GameModes & PktFile.GameMode.Standard) == 0) {
						if ((pktMapEntry.GameModes & PktFile.GameMode.Megawealth) == PktFile.GameMode.Megawealth)
							mapName += " (Megawealth)";
						if ((pktMapEntry.GameModes & PktFile.GameMode.Duel) == PktFile.GameMode.Duel)
							mapName += " (Land Rush)";
						if ((pktMapEntry.GameModes & PktFile.GameMode.NavalWar) == PktFile.GameMode.NavalWar)
							mapName += " (Naval War)";
					}
				}
			}

			// not really used, likely empty, but if this is filled in it's probably better than guessing
			if (mapName == "" && basic.SortedEntries.ContainsKey("Name"))
				mapName = basic.ReadString("Name");

			if (mapName == "") {
				Logger.Warn("No valid mapname given or found, reverting to default filename {0}", fileNameWithoutExtension);
				mapName = fileNameWithoutExtension;
			}
			else {
				Logger.Info("Mapname found: {0}", mapName);
			}

			mapName = StripPlayersFromName(MakeValidFileName(mapName));
			return mapName;
		}

		private static string StripPlayersFromName(string mapName) {
			if (mapName.IndexOf(" (") != -1)
				mapName = mapName.Substring(0, mapName.IndexOf(" ("));
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
		public bool LoadMap(EngineType et = EngineType.AutoDetect) {
			var map = GetSection("Map");
			string[] size = map.ReadString("Size").Split(',');
			_fullSize = new Rectangle(int.Parse(size[0]), int.Parse(size[1]), int.Parse(size[2]), int.Parse(size[3]));
			size = map.ReadString("LocalSize").Split(',');
			_localSize = new Rectangle(int.Parse(size[0]), int.Parse(size[1]), int.Parse(size[2]), int.Parse(size[3]));
			EngineType = et;

			ReadAllObjects();

			// if we have to autodetect, we need to load rules.ini,
			// and we don't want to parse it again when constructing the theater
			if (et == EngineType.AutoDetect) {
				_rules = VFS.Open<IniFile>("rules.ini");
				EngineType = DetectMapType(_rules);

				if (EngineType == EngineType.YurisRevenge) {
					// add YR mixes to VFS
					VFS.GetInstance().Clear();
					VFS.GetInstance().ScanMixDir(EngineType.YurisRevenge);

					var rulesmd = VFS.Open<IniFile>("rulesmd.ini");
					var artmd = VFS.Open<IniFile>("artmd.ini");

					if (rulesmd == null) {
						Logger.Error("rulesmd.ini or artmd.ini could not be loaded! You cannot render a YR/FS map " +
									 "without the expansion installed. Unavailable objects will not be rendered, reverting to rules.ini.");
						RemoveYRObjects();

						_art = VFS.Open<IniFile>("art.ini");
					}
					else {
						_rules = rulesmd;
						_art = artmd;
					}
				}
				else _art = VFS.Open<IniFile>("art.ini"); // rules is already loaded
			}
			else if (EngineType == EngineType.YurisRevenge) {
				_rules = VFS.Open("rulesmd.ini") as IniFile;
				_art = VFS.Open("artmd.ini") as IniFile;
			}
			else if (EngineType == EngineType.FireStorm) {
				_rules = VFS.Open("rules.ini") as IniFile;
				_art = VFS.Open("art.ini") as IniFile;

				Logger.Info("Merging Firestorm rules with TS rules");
				_rules.MergeWith(VFS.Open<IniFile>("firestrm.ini"));
				_art.MergeWith(VFS.Open<IniFile>("artfs.ini"));

			}
			else {
				_rules = VFS.Open("rules.ini") as IniFile;
				_art = VFS.Open("art.ini") as IniFile;
			}

			if (_rules == null || _art == null) {
				Logger.Fatal("Rules or art config file could not be loaded! You cannot render a YR/FS map" +
							 " without the expansion installed");
				return false;
			}

			Drawable.TileWidth = (ushort)TileWidth;
			Drawable.TileHeight = (ushort)TileHeight;

			_theater = new Theater(ReadString("Map", "Theater"), EngineType, _rules, _art);
			_theater.Initialize();
			RemoveUnknownObjects();

			Logger.Info("Overriding rules.ini with map INI entries");
			_rules.MergeWith(this);

			MoveStructuresToBaseTile();

			_palettesToBeRecalculated.AddRange(_theater.GetPalettes());

			LoadColors();
			if (EngineType == EngineType.RedAlert2 || EngineType == EngineType.YurisRevenge)
				LoadCountries();
			LoadHouses();

			if (EngineType == EngineType.RedAlert2 || EngineType == EngineType.YurisRevenge)
				// _theater.GetTileCollection().RecalculateTileSystem(_tiles);

			if (EngineType == EngineType.RedAlert2 || EngineType == EngineType.YurisRevenge)
				RecalculateOreSpread();

			LoadLighting();
			CreateLevelPalettes();
			LoadLightSources();
			ApplyLightSources();
			ApplyRemappables();
			// now everything is loaded and we can prepare the palettes before using them to draw
			RecalculateAllPalettes();

			return true;
		}

		private void MoveStructuresToBaseTile() {
			// we need foundations from the theater to place the structures at the correct tile,
			for (int y = 0; y < _structureObjects.GetLength(1); y++) {
				for (int x = 0; x < _structureObjects.GetLength(0); x++) {
					StructureObject s = _structureObjects[x, y];
					if (s == null || s.DrawTile != null) continue; // s.DrawTile set means we've already moved it

					Size foundation = _theater.GetFoundation(s);
					if (foundation == Size.Empty) continue;
					s.DrawTile = _tiles.GetTileR(s.Tile.Rx + foundation.Width - 1, s.Tile.Ry + foundation.Height - 1);

					// move structure
					_structureObjects[x, y] = null;
					_structureObjects[s.DrawTile.Dx, s.DrawTile.Dy / 2] = s;
				}
			}

			// bridges too
			for (int y = 0; y < _overlayObjects.GetLength(1); y++) {
				for (int x = 0; x < _overlayObjects.GetLength(0); x++) {
					OverlayObject o = _overlayObjects[x, y];
					if (o == null || o.DrawTile != null) continue; // DrawTile set means we've already moved it

					Size foundation = _theater.GetFoundation(o);
					if (foundation == Size.Empty) continue;
					o.DrawTile = _tiles.GetTileR(o.Tile.Rx - 2, o.Tile.Ry - 2);

					if (o.DrawTile != null) {
						// move structure
						_overlayObjects[x, y] = null;
						_overlayObjects[o.DrawTile.Dx, o.DrawTile.Dy / 2] = o;
					}
				}
			}
		}

		/// <summary>Loads the countries. </summary>
		private void LoadCountries() {
			Logger.Info("Loading countries");

			var countriesSection = _rules.GetSection(EngineType >= EngineType.RedAlert2 ? "Countries" : "Houses");
			foreach (var entry in countriesSection.OrderedEntries) {
				IniSection countrySection = _rules.GetSection(entry.Value);
				if (countrySection == null) continue;
				Color c;
				if (!_namedColors.TryGetValue(countrySection.ReadString("Color"), out c))
					c = _namedColors.Values.First();
				_countryColors[entry.Value] = c;
			}
		}

		/// <summary>Loads the colors. </summary>
		private void LoadColors() {
			var colorsSection = _rules.GetSection("Colors");
			foreach (var entry in colorsSection.OrderedEntries) {
				string[] colorComponents = ((string)entry.Value).Split(',');
				var h = new HsvColor(int.Parse(colorComponents[0]),
									 int.Parse(colorComponents[1]), int.Parse(colorComponents[2]));
				_namedColors[entry.Key] = h.ToRGB();
			}
		}

		/// <summary>Loads the houses. </summary>
		private void LoadHouses() {
			Logger.Info("Loading houses");
			IniSection housesSection = GetSection("Houses");
			LoadHousesFromIniSection(housesSection, this);
			housesSection = _rules.GetSection("Houses");
			LoadHousesFromIniSection(housesSection, _rules);
		}

		private void LoadHousesFromIniSection(IniSection housesSection, IniFile ini) {
			if (housesSection == null) return;
			foreach (var v in housesSection.OrderedEntries) {
				var houseSection = ini.GetSection(v.Value);
				if (houseSection == null) continue;
				string color;
				if (v.Value == "Neutral" || v.Value == "Special")
					color = "LightGrey"; // this is hardcoded in the game
				else
					color = houseSection.ReadString("Color");
				if (!string.IsNullOrEmpty(color) && !string.IsNullOrEmpty(v.Value))
					_countryColors[v.Value] = _namedColors[color];
			}
		}

		/// <summary>Detect map type.</summary>
		/// <param name="rules">The rules.ini file to be used.</param>
		/// <returns>The engine to be used to render this map.</returns>
		private EngineType DetectMapType(IniFile rules) {
			Logger.Info("Determining map type");

			if (ReadBool("Basic", "RequiredAddon"))
				return EngineType.YurisRevenge;

			string theater = ReadString("Map", "Theater").ToLower();
			// decision based on theatre
			if (theater == "lunar" || theater == "newurban" || theater == "desert")
				return EngineType.YurisRevenge;

			// decision based on overlay/trees/structs
			if (!AllObjectsFromRA2(rules))
				return EngineType.YurisRevenge;

			// decision based on max tile/threatre
			int maxTileNum = _tiles.Where(t => t != null).Aggregate(int.MinValue, (current, t) => Math.Max(t.TileNum, current));

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
			else if (Path.GetExtension(FileName) == ".yrm") return EngineType.YurisRevenge;
			else return EngineType.RedAlert2;
		}

		/// <summary>Tests whether all objects on the map are present in RA2</summary>
		/// <param name="rules">The rules.ini file from RA2.</param>
		/// <returns>True if all objects are from RA2, else false.</returns>
		private bool AllObjectsFromRA2(IniFile rules) {
			foreach (var obj in _overlayObjects)
				if (obj != null && obj.OverlayID > 246) return false;
			IniSection objSection = rules.GetSection("TerrainTypes");
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var obj = _terrainObjects[x, y];
					if (obj == null) continue;
					int idx = objSection.FindValueIndex(obj.Name);
					if (idx == -1 || idx > 73) return false;
				}
			}

			objSection = rules.GetSection("InfantryTypes");
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var objList = _infantryObjects[x, y];
					if (objList == null) continue;
					foreach (var obj in objList) {
						int idx = objSection.FindValueIndex(obj.Name);
						if (idx == -1 || idx > 45) return false;
					}
				}
			}

			objSection = rules.GetSection("VehicleTypes");
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var obj = _unitObjects[x, y];
					if (obj == null) continue;
					int idx = objSection.FindValueIndex(obj.Name);
					if (idx == -1 || idx > 57) return false;
				}
			}

			objSection = rules.GetSection("AircraftTypes");
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var obj = _aircraftObjects[x, y];
					if (obj == null) continue;
					int idx = objSection.FindValueIndex(obj.Name);
					if (idx == -1 || idx > 9) return false;
				}
			}


			objSection = rules.GetSection("BuildingTypes");
			IniSection objSectionAlt = rules.GetSection("OverlayTypes");
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var obj = _structureObjects[x, y];
					if (obj == null) continue;
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

		private void RemoveYRObjects() {
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var obj = _overlayObjects[x, y];
					if (obj != null && obj.OverlayID > 246) _overlayObjects[x, y] = null;
				}
			}

			IniSection objSection = _rules.GetSection("TerrainTypes");
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var obj = _terrainObjects[x, y];
					if (obj == null) continue;
					int idx = objSection.FindValueIndex(obj.Name);
					if (idx == -1 || idx > 73) _terrainObjects[x, y] = null;
				}
			}

			objSection = _rules.GetSection("InfantryTypes");
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var objList = _infantryObjects[x, y];
					if (objList == null) continue;

					objList.RemoveAll(i => objSection.FindValueIndex(i.Name) > 45 || objSection.FindValueIndex(i.Name) == -1);
				}
			}

			objSection = _rules.GetSection("VehicleTypes");
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var obj = _unitObjects[x, y];
					if (obj == null) continue;
					int idx = objSection.FindValueIndex(obj.Name);
					if (idx == -1 || idx > 57) _unitObjects[x, y] = null;
				}
			}

			objSection = _rules.GetSection("AircraftTypes");
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var obj = _aircraftObjects[x, y];
					if (obj == null) continue;
					int idx = objSection.FindValueIndex(obj.Name);
					if (idx == -1 || idx > 9) _aircraftObjects[x, y] = null;
				}
			}


			objSection = _rules.GetSection("BuildingTypes");
			IniSection objSectionAlt = _rules.GetSection("OverlayTypes");
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var obj = _structureObjects[x, y];
					if (obj == null) continue;
					int idx1 = objSection.FindValueIndex(obj.Name);
					int idx2 = objSectionAlt.FindValueIndex(obj.Name);
					if (idx1 == -1 && idx2 == -1) _structureObjects[x, y] = null;
					else if (idx1 != -1 && idx1 > 303)
						_structureObjects[x, y] = null;
					else if (idx2 != -1 && idx2 > 246)
						_structureObjects[x, y] = null;
				}
			}

			// no need to remove smudges as no new ones were introduced with yr
		}

		private void RemoveUnknownObjects() {
			ObjectCollection c = _theater.GetCollection(CollectionType.Terrain);
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var obj = _terrainObjects[x, y];
					if (obj == null) continue;
					if (!c.HasObject(obj))
						_terrainObjects[x, y] = null;
				}
			}

			c = _theater.GetCollection(CollectionType.Infantry);
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var objList = _infantryObjects[x, y];
					if (objList == null) continue;
					objList.RemoveAll(i => !c.HasObject(i));
				}
			}

			c = _theater.GetCollection(CollectionType.Vehicle);
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var obj = _unitObjects[x, y];
					if (obj == null) continue;
					if (!c.HasObject(obj))
						_unitObjects[x, y] = null;
				}
			}

			c = _theater.GetCollection(CollectionType.Aircraft);
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var obj = _aircraftObjects[x, y];
					if (obj == null) continue;
					if (!c.HasObject(obj))
						_aircraftObjects[x, y] = null;
				}
			}

			c = _theater.GetCollection(CollectionType.Smudge);
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var obj = _smudgeObjects[x, y];
					if (obj == null) continue;
					if (!c.HasObject(obj))
						_smudgeObjects[x, y] = null;
				}
			}

			c = _theater.GetCollection(CollectionType.Building);
			var cAlt = _theater.GetCollection(CollectionType.Overlay);
			IniSection objSectionAlt = _rules.GetSection("OverlayTypes");
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = 0; x < _fullSize.Width * 2 - 1; x++) {
					var obj = _structureObjects[x, y];
					if (obj == null) continue;
					if (!c.HasObject(obj) && !cAlt.HasObject(obj))
						_structureObjects[x, y] = null;
				}
			}

		}

		/// <summary>Reads all objects. </summary>
		private void ReadAllObjects() {
			Logger.Info("Reading tiles");
			ReadTiles();

			Logger.Info("Reading map overlay");
			ReadOverlay();

			Logger.Info("Reading map overlay objects");
			ReadTerrain();

			Logger.Info("Reading map terrain object");
			ReadSmudges();

			Logger.Info("Reading infantry on map");
			ReadInfantry();

			Logger.Info("Reading vehicles on map");
			ReadUnits();

			Logger.Info("Reading aircraft on map");
			ReadAircraft();

			Logger.Info("Reading map structures");
			ReadStructures();
		}

		/// <summary>Reads the tiles. </summary>
		private void ReadTiles() {
			var mapSection = GetSection("IsoMapPack5");
			byte[] lzoData = Convert.FromBase64String(mapSection.ConcatenatedValues());
			int cells = (_fullSize.Width * 2 - 1) * _fullSize.Height;
			int lzoPackSize = cells * 11 + 4; // last 4 bytes contains a lzo pack header saying no more data is left

			var isoMapPack = new byte[lzoPackSize];
			uint totalDecompressSize = Format5.DecodeInto(lzoData, isoMapPack);

			_tiles = new TileLayer(_fullSize.Size);
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

				int dx = rx - ry + _fullSize.Width - 1;
				int dy = rx + ry - _fullSize.Width - 1;
				numtiles++;
				if (dx >= 0 && dx < 2 * _tiles.Width &&
					dy >= 0 && dy < 2 * _tiles.Height) {
					_tiles[(ushort)dx, (ushort)dy / 2] = new MapTile((ushort)dx, (ushort)dy, rx, ry, z, tilenum, subtile, _tiles);
				}
			}
		}

		/// <summary>Reads the terrain. </summary>
		private void ReadTerrain() {
			IniSection terrainSection = GetSection("Terrain");
			_terrainObjects = new TerrainObject[_fullSize.Width * 2 - 1, _fullSize.Height];
			if (terrainSection == null) return;
			foreach (var v in terrainSection.OrderedEntries) {
				int pos = int.Parse(v.Key);
				string name = v.Value;
				int rx = pos % 1000;
				int ry = pos / 1000;
				var t = new TerrainObject(name);
				var tile = _tiles.GetTileR(rx, ry);
				if (tile != null) {
					tile.AddObject(t);
					_terrainObjects[tile.Dx, tile.Dy / 2] = t;
				}
			}
		}

		/// <summary>Reads the smudges. </summary>
		private void ReadSmudges() {
			IniSection smudgesSection = GetSection("Smudge");
			_smudgeObjects = new SmudgeObject[_fullSize.Width * 2 - 1, _fullSize.Height];
			if (smudgesSection == null) return;
			foreach (var v in smudgesSection.OrderedEntries) {
				string[] entries = ((string)v.Value).Split(',');
				string name = entries[0];
				int rx = int.Parse(entries[1]);
				int ry = int.Parse(entries[2]);
				var s = new SmudgeObject(name);
				var tile = _tiles.GetTileR(rx, ry);
				if (tile != null) {
					tile.AddObject(s);
					_smudgeObjects[tile.Dx, tile.Dy / 2] = s;
				}
			}
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

			_overlayObjects = new OverlayObject[_fullSize.Width * 2 - 1, _fullSize.Height];

			foreach (MapTile t in _tiles) {
				if (t == null) continue;
				int idx = t.Rx + 512 * t.Ry;
				byte overlay_id = overlayPack[idx];
				if (overlay_id != 0xFF) {
					byte overlay_value = overlayDataPack[idx];
					var ovl = new OverlayObject(overlay_id, overlay_value);
					t.AddObject(ovl);
					_overlayObjects[ovl.Tile.Dx, ovl.Tile.Dy / 2] = ovl;
				}
			}
		}

		/// <summary>Reads the structures.</summary>
		private void ReadStructures() {
			IniSection structsSection = GetSection("Structures");
			_structureObjects = new StructureObject[_fullSize.Width * 2 - 1, _fullSize.Height];
			if (structsSection == null) {
				Logger.Info("Structures section unavailable in {0}", Path.GetFileName(FileName));
				return;
			}
			foreach (var v in structsSection.OrderedEntries) {
				try {
					string[] entries = ((string)v.Value).Split(',');
					string owner = entries[0];
					string name = entries[1];
					short health = short.Parse(entries[2]);
					int rx = int.Parse(entries[3]);
					int ry = int.Parse(entries[4]);
					short direction = short.Parse(entries[5]);
					var s = new StructureObject(owner, name, health, direction);
					s.Tile = _tiles.GetTileR(rx, ry);
					if (s.Tile != null)
						_structureObjects[s.Tile.Dx, s.Tile.Dy / 2] = s;
				}
				catch (IndexOutOfRangeException) {
				} // catch invalid entries
				catch (FormatException) {
				}
			}
			Logger.Trace("Loaded structures ({0})", _structureObjects.Length);
		}

		/// <summary>Reads the infantry. </summary>
		private void ReadInfantry() {
			IniSection infantrySection = GetSection("Infantry");
			_infantryObjects = new List<InfantryObject>[_fullSize.Width * 2 - 1, _fullSize.Height];
			if (infantrySection == null) {
				Logger.Info("Infantry section unavailable in {0}", Path.GetFileName(FileName));
				return;
			}
			int count = 0;
			foreach (var v in infantrySection.OrderedEntries) {
				string[] entries = ((string)v.Value).Split(',');
				string owner = entries[0];
				string name = entries[1];
				short health = short.Parse(entries[2]);
				int rx = int.Parse(entries[3]);
				int ry = int.Parse(entries[4]);
				short direction = short.Parse(entries[7]);
				var i = new InfantryObject(owner, name, health, direction);
				var tile = _tiles.GetTileR(rx, ry);
				if (tile != null) {
					tile.AddObject(i);
					var infantryList = _infantryObjects[i.Tile.Dx, i.Tile.Dy / 2];
					if (infantryList == null)
						_infantryObjects[i.Tile.Dx, i.Tile.Dy / 2] = infantryList = new List<InfantryObject>();
					infantryList.Add(i);
					count++;
				}
			}
			Logger.Trace("Loaded infantry objects ({0})", count);

		}

		/// <summary>Reads the units.</summary>
		private void ReadUnits() {
			IniSection unitsSection = GetSection("Units");
			_unitObjects = new UnitObject[_fullSize.Width * 2 - 1, _fullSize.Height];
			if (unitsSection == null) {
				Logger.Info("Units section unavailable in {0}", Path.GetFileName(FileName));
				return;
			}
			int count = 0;
			foreach (var v in unitsSection.OrderedEntries) {
				string[] entries = ((string)v.Value).Split(',');
				string owner = entries[0];
				string name = entries[1];
				short health = short.Parse(entries[2]);
				int rx = int.Parse(entries[3]);
				int ry = int.Parse(entries[4]);
				short direction = short.Parse(entries[5]);
				var u = new UnitObject(owner, name, health, direction);
				var tile = _tiles.GetTileR(rx, ry);
				if (tile != null) {
					tile.AddObject(u);
					_unitObjects[u.Tile.Dx, u.Tile.Dy / 2] = u;
				}
			}
			Logger.Trace("Loaded units ({0})", _unitObjects.Length);
		}

		/// <summary>Reads the aircraft.</summary>
		private void ReadAircraft() {
			IniSection aircraftSection = GetSection("Aircraft");
			_aircraftObjects = new AircraftObject[_fullSize.Width * 2 - 1, _fullSize.Height];
			if (aircraftSection == null) {
				Logger.Info("Aircraft section unavailable in {0}", Path.GetFileName(FileName));
				return;
			}
			foreach (var v in aircraftSection.OrderedEntries) {
				string[] entries = ((string)v.Value).Split(',');
				string owner = entries[0];
				string name = entries[1];
				short health = short.Parse(entries[2]);
				int rx = int.Parse(entries[3]);
				int ry = int.Parse(entries[4]);
				short direction = short.Parse(entries[5]);
				var a = new AircraftObject(owner, name, health, direction);
				var tile = _tiles.GetTileR(rx, ry);
				if (tile != null) {
					tile.AddObject(a);
					_aircraftObjects[tile.Dx, tile.Dy / 2] = a;
				}
			}
			Logger.Trace("Loaded aircraft ({0})", _aircraftObjects.Length);
		}

		private void RecalculateOreSpread() {
			Logger.Info("Redistributing ore-spread over patches");
			foreach (OverlayObject o in _overlayObjects) {
				if (o == null) continue;
				// The value consists of the sum of all dx's with a little magic offsets
				// plus the sum of all dy's with also a little magic offset, and also
				// everything is calculated modulo 12
				if (o.IsOre()) {
					int x = o.Tile.Dx;
					int y = o.Tile.Dy;
					double yInc = ((((y - 9) / 2) % 12) * (((y - 8) / 2) % 12)) % 12;
					double xInc = ((((x - 13) / 2) % 12) * (((x - 12) / 2) % 12)) % 12;

					// x_inc may be > y_inc so adding a big number outside of cell bounds
					// will surely keep num positive
					var num = (int)(yInc - xInc + 120000);
					num %= 12;

					// replace ore
					o.OverlayID = (byte)(OverlayObject.MinOreID + num);
				}

				else if (o.IsGem()) {
					int x = o.Tile.Dx;
					int y = o.Tile.Dy;
					double yInc = ((((y - 9) / 2) % 12) * (((y - 8) / 2) % 12)) % 12;
					double xInc = ((((x - 13) / 2) % 12) * (((x - 12) / 2) % 12)) % 12;

					// x_inc may be > y_inc so adding a big number outside of cell bounds
					// will surely keep num positive
					var num = (int)(yInc - xInc + 120000);
					num %= 12;

					// replace gems
					o.OverlayID = (byte)(OverlayObject.MinGemsID + num);
				}
			}
		}

		private void LoadLighting() {
			Logger.Info("Loading lighting");
			_lighting = new Lighting(GetSection("Lighting"));
		}

		private void CreateLevelPalettes() {
			Logger.Info("Creating per-height palettes");
			PaletteCollection palettes = _theater.GetPalettes();
			for (int i = 0; i < 19; i++) {
				Palette isoHeight = palettes.isoPalette.Clone();
				isoHeight.ApplyLighting(_lighting, i);
				isoHeight.IsShared = true;
				_palettePerLevel.Add(isoHeight);
				_palettesToBeRecalculated.Add(isoHeight);
			}

			var overlayObjects = _theater.GetCollection(CollectionType.Overlay);
			foreach (MapTile t in _tiles) {
				if (t != null) {
					t.Palette = _palettePerLevel[t.Z];

					// overlay objects inherit per-level iso palettes 
					// (so that for example higher placed rocks look brighter)
					var ovl = _overlayObjects[t.Dx, t.Dy / 2];
					if (ovl == null) continue;

					var drawable = overlayObjects.GetDrawable(ovl);
					if (drawable != null && drawable.IsValid && drawable.UseTilePalette) {
						// bridge tiles get the same lighting as their corresponding tiles, 
						// which are located a bit above their associated tile
						ovl.Palette = _palettePerLevel[ovl.Tile.Z + 4].Clone();

						// regardless of whether or not this palette is affected by any light-source,
						// we'll have to recalculate it
						_palettesToBeRecalculated.Add(ovl.Palette);
					}
				}
			}
		}

		private static readonly string[] LampNames = new[] {
			"REDLAMP", "BLUELAMP", "GRENLAMP", "YELWLAMP", "PURPLAMP", "INORANLAMP", "INGRNLMP", "INREDLMP", "INBLULMP",
			"INGALITE", "GALITE",
			"INYELWLAMP", "INPURPLAMP", "NEGLAMP", "NERGRED", "TEMMORLAMP", "TEMPDAYLAMP", "TEMDAYLAMP", "TEMDUSLAMP",
			"TEMNITLAMP", "SNOMORLAMP",
			"SNODAYLAMP", "SNODUSLAMP", "SNONITLAMP"
		};

		private void LoadLightSources() {
			Logger.Info("Loading light sources");
			var forDeletion = new List<StructureObject>();
			foreach (StructureObject s in _structureObjects) {
				if (s == null) continue;
				if (LampNames.Contains(s.Name)) {
					var ls = new LightSource(_rules.GetSection(s.Name), _lighting);
					ls.Tile = s.Tile;
					_lightSources.Add(ls);
					forDeletion.Add(s);
				}
			}
			// make sure these don't get drawn
			foreach (var s in forDeletion) {
				_structureObjects[s.Tile.Dx, s.Tile.Dy / 2] = null;
			}
		}

		private void ApplyLightSources() {
			int before = _palettesToBeRecalculated.Count;
			foreach (LightSource s in _lightSources) {
				foreach (MapTile t in _tiles) {
					if (t == null || t.Palette == null) continue;

					bool wasShared = t.Palette.IsShared;
					// make sure this tile can only end up in the "to-be-recalculated list" once
					if (LightSource.ApplyLamp(t, t, s))
						// if this lamp caused a new unshared palette to be created
						if (wasShared && !t.Palette.IsShared)
							_palettesToBeRecalculated.Add(t.Palette);
				}
				foreach (var ovl in _overlayObjects) {
					if (ovl == null || ovl.Palette == null) continue;

					LightSource.ApplyLamp(ovl, ovl.Tile, s);
					// this is already added to the PalettesToBeRecalculated list
				}
			}
			Logger.Debug("Determined palettes to be recalculated due to lightsources ({0})",
						 _palettesToBeRecalculated.Count - before);
		}

		private void ApplyRemappables() {
			int before = _palettesToBeRecalculated.Count;
			foreach (var s in _structureObjects) {
				if (s == null) continue;
				s.Palette = _theater.GetPalette(s).Clone();
				s.Palette.Remap(_countryColors[s.Owner]);
				s.Palette.ApplyLighting(s.Tile.Palette.GetLighting());
				_palettesToBeRecalculated.Add(s.Palette);
			}
			foreach (var u in _unitObjects) {
				if (u == null) continue;
				u.Palette = _theater.GetPalette(u).Clone();
				u.Palette.Remap(_countryColors[u.Owner]);
				u.Palette.ApplyLighting(u.Tile.Palette.GetLighting());
				_palettesToBeRecalculated.Add(u.Palette);
			}
			foreach (var a in _aircraftObjects) {
				if (a == null) continue;
				a.Palette = _theater.GetPalette(a).Clone();
				a.Palette.Remap(_countryColors[a.Owner]);
				a.Palette.ApplyLighting(a.Tile.Palette.GetLighting());
				_palettesToBeRecalculated.Add(a.Palette);
			}
			foreach (var il in _infantryObjects) {
				if (il == null) continue;
				foreach (InfantryObject i in il) {
					if (i == null) continue;
					i.Palette = _theater.GetPalette(i).Clone();
					i.Palette.Remap(_countryColors[i.Owner]);
					i.Palette.ApplyLighting(i.Tile.Palette.GetLighting());
					_palettesToBeRecalculated.Add(i.Palette);
				}
			}

			// TS needs tiberium remapped
			if (EngineType == EngineType.TiberianSun || EngineType == EngineType.FireStorm) {
				var collection = _theater.GetCollection(CollectionType.Overlay);
				var tiberiumsSections = _rules.GetSection("Tiberiums");
				var tiberiumRemaps =
					tiberiumsSections.OrderedEntries.Select(v => _rules.GetSection(v.Value).ReadString("Color")).ToList();

				foreach (var ovl in _overlayObjects) {
					if (ovl == null) continue;
					string name = collection.GetName(ovl.OverlayID);
					if (name.StartsWith("TIB")) {
						int tiberiumType;
						if (name.Contains("_")) tiberiumType = name[3] - '0';
						else tiberiumType = 1;
						ovl.Palette = _theater.GetPalette(ovl).Clone();
						ovl.Palette.Remap(_namedColors[tiberiumRemaps[tiberiumType - 1]]);
						_palettesToBeRecalculated.Add(ovl.Palette);
					}
				}
			}
			Logger.Debug("Determined palettes to be recalculated due to remappables ({0})",
						 _palettesToBeRecalculated.Count - before);
		}

		private void RecalculateAllPalettes() {
			Logger.Info("Calculating palette-values for all objects");
			foreach (Palette p in _palettesToBeRecalculated)
				p.Recalculate();
		}

		public void DrawTiledStartPositions() {
			Logger.Info("Marking tiled start positions");
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
						MapTile t = _tiles.GetTileR(x, y);
						if (t != null) {
							t.Palette = Palette.MergePalettes(t.Palette, red, 0.4);
						}
					}
				}
			}
		}

		public void UndrawTiledStartPositions() {
			Logger.Info("Undoing tiled marking of start positions");
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

				// Redraw the 4x4 cell around start pos with original palette;
				// first the tiles, then the objects
				for (int x = wx - 2; x < wx + 2; x++) {
					for (int y = wy - 2; y < wy + 2; y++) {
						MapTile t = _tiles.GetTileR(x, y);
						if (t == null) continue;
						t.Palette = _palettePerLevel[t.Z];
						// redraw tile
						_theater.GetTileCollection().DrawTile(t, _drawingSurface);
					}
				}
				for (int x = wx - 5; x < wx + 5; x++) {
					for (int y = wy - 5; y < wy + 5; y++) {
						MapTile t = _tiles.GetTileR(x, y);
						if (t == null) continue;
						// redraw objects on here
						List<RA2Object> objs = GetObjectsAt(t.Dx, t.Dy / 2);
						foreach (RA2Object o in objs)
							_theater.DrawObject(o, _drawingSurface);
					}
				}
			}
		}

		public unsafe void DrawSquaredStartPositions() {
			Logger.Info("Marking squared start positions");
			IniSection basic = GetSection("Basic");
			if (basic == null || !basic.ReadBool("MultiplayerOnly")) return;
			IniSection waypoints = GetSection("Waypoints");

			foreach (var entry in waypoints.OrderedEntries) {
				if (int.Parse(entry.Key) >= 8)
					continue;
				int pos = int.Parse(entry.Value);
				int wy = pos / 1000;
				int wx = pos - wy * 1000;

				MapTile t = _tiles.GetTileR(wx, wy);
				if (t == null) continue;
				int destX = t.Dx * TileWidth / 2;
				int destY = (t.Dy - t.Z) * TileHeight / 2;

				bool vert = _fullSize.Height * 2 > _fullSize.Width;
				int radius;
				if (vert)
					radius = 10 * _fullSize.Height * TileHeight / 2 / 144;
				else
					radius = 10 * _fullSize.Width * TileWidth / 2 / 133;

				int h = radius, w = radius;
				for (int drawY = destY - h / 2; drawY < destY + h; drawY++) {
					for (int drawX = destX - w / 2; drawX < destX + w; drawX++) {
						byte* p = (byte*)_drawingSurface.bmd.Scan0 + drawY * _drawingSurface.bmd.Stride + 3 * drawX;
						*p++ = 0x00;
						*p++ = 0x00;
						*p++ = 0xFF;
					}
				}
			}
		}


		public int FindCutoffHeight() {
			// searches in 10 rows, starting from the bottom up, for the first fully tiled row
			int y;

			/*// print map:
			var tileTouchGrid = _tiles.GridTouched;
			var sb = new StringBuilder();
			for (y = 0; y < tileTouchGrid.GetLength(1); y++) {
				for (int x = 0; x < tileTouchGrid.GetLength(0); x++) {
					if (tileTouchGrid[x, y] == TileLayer.TouchType.Untouched)
						sb.Append(' ');
					else if ((tileTouchGrid[x, y] & TileLayer.TouchType.ByExtraData) == TileLayer.TouchType.ByExtraData)
						sb.Append('*');
					else
						sb.Append('o');
				}
				sb.AppendLine();
			}
			File.WriteAllText("cutoffmap.txt", sb.ToString());*/

			for (y = _fullSize.Height - 1; y > _fullSize.Height - 10; y--) {
				bool isRowFilled = true;
				for (int x = 1; x < _fullSize.Width - 1; x++) {
					if (_tiles.GridTouched[x, y] == TileLayer.TouchType.Untouched) {
						isRowFilled = false;
						break;
					}
				}
				if (isRowFilled)
					break;
			}
			Logger.Debug("Cutoff-height determined at {0}, cutting off {1} rows", y, _fullSize.Height - y);
			return y;
		}

		public Rectangle GetFullMapSizePixels() {
			int left = TileWidth / 2,
				top = TileHeight / 2;
			int right = (_fullSize.Width - 1) * TileWidth;
			int cutoff = FindCutoffHeight();
			int bottom = cutoff * TileHeight + (1 + (cutoff % 2)) * (TileHeight / 2);
			return Rectangle.FromLTRB(left, top, right, bottom);
		}

		public Rectangle GetLocalSizePixels() {
			int left = Math.Max(_localSize.Left * TileWidth, 0),
				top = Math.Max(_localSize.Top - 3, 0) * TileHeight + TileHeight / 2;
			int right = (_localSize.Left + _localSize.Width) * TileWidth;

			int bottom1 = 2 * (_localSize.Top - 3 + _localSize.Height + 5);
			int cutoff = FindCutoffHeight() * 2;
			int bottom2 = (cutoff + 1 + (cutoff % 2));
			int bottom = Math.Min(bottom1, bottom2) * (TileHeight / 2);
			return Rectangle.FromLTRB(left, top, right, bottom);
		}

		public void MarkOreAndGems() {
			Logger.Info("Marking ore and gems");
			Palette yellow = Palette.MakePalette(Color.Yellow);
			Palette purple = Palette.MakePalette(Color.Purple);
			foreach (var o in _overlayObjects) {
				if (o == null) continue;
				if (o.IsOre())
					o.Tile.Palette = Palette.MergePalettes(o.Tile.Palette, yellow, Math.Min((byte)11, o.OverlayValue) / 11.0 * 0.6 + 0.1);

				else if (o.IsGem())
					o.Tile.Palette = Palette.MergePalettes(o.Tile.Palette, purple, Math.Min((byte)11, o.OverlayValue) / 11.0 * 0.6 + 0.25);
			}
		}

		public void RedrawOreAndGems() {
			var tileCollection = _theater.GetTileCollection();

			// first redraw all required tiles (zigzag method)
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = _fullSize.Width * 2 - 2; x >= 0; x -= 2) {
					if (_overlayObjects[x, y] == null || !_overlayObjects[x, y].IsOreOrGem()) continue;
					tileCollection.DrawTile(_tiles.GetTile(x, y), _drawingSurface);
				}
				for (int x = _fullSize.Width * 2 - 3; x >= 0; x -= 2) {
					if (_overlayObjects[x, y] == null || !_overlayObjects[x, y].IsOreOrGem()) continue;
					tileCollection.DrawTile(_tiles.GetTile(x, y), _drawingSurface);
				}
			}

			// then the objects on these ore positions
			for (int y = 0; y < _fullSize.Height; y++) {
				for (int x = _fullSize.Width * 2 - 2; x >= 0; x -= 2) {
					if (_overlayObjects[x, y] == null || !_overlayObjects[x, y].IsOreOrGem()) continue;
					List<RA2Object> objs = GetObjectsAt(x, y);
					foreach (RA2Object o in objs)
						_theater.DrawObject(o, _drawingSurface);
				}
				for (int x = _fullSize.Width * 2 - 3; x >= 0; x -= 2) {
					if (_overlayObjects[x, y] == null || !_overlayObjects[x, y].IsOreOrGem()) continue;
					List<RA2Object> objs = GetObjectsAt(x, y);
					foreach (RA2Object o in objs)
						_theater.DrawObject(o, _drawingSurface);
				}
			}
		}

		public void DrawMap() {
			Logger.Info("Drawing map");
			_drawingSurface = new DrawingSurface(_fullSize.Width * TileWidth, _fullSize.Height * TileHeight,
												 System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			var tileCollection = _theater.GetTileCollection();

			// zig-zag drawing technique explanation: http://stackoverflow.com/questions/892811/drawing-isometric-game-worlds

			for (int y = 0; y < _fullSize.Height; y++) {
				Logger.Trace("Drawing tiles row {0}", y);
				for (int x = _fullSize.Width * 2 - 2; x >= 0; x -= 2) {
					tileCollection.DrawTile(_tiles.GetTile(x, y), _drawingSurface);
				}
				for (int x = _fullSize.Width * 2 - 3; x >= 0; x -= 2) {
					tileCollection.DrawTile(_tiles.GetTile(x, y), _drawingSurface);
				}
			}

			for (int y = 0; y < _fullSize.Height; y++) {
				Logger.Trace("Drawing objects row {0}", y);
				for (int x = _fullSize.Width * 2 - 2; x >= 0; x -= 2) {
					List<RA2Object> objs = GetObjectsAt(x, y);
					foreach (RA2Object o in objs)
						_theater.DrawObject(o, _drawingSurface);
				}
				for (int x = _fullSize.Width * 2 - 3; x >= 0; x -= 2) {
					List<RA2Object> objs = GetObjectsAt(x, y);
					foreach (RA2Object o in objs)
						_theater.DrawObject(o, _drawingSurface);
				}
			}
		}

		internal void DebugDrawTile(MapTile tile) {
			_theater.GetTileCollection().DrawTile(tile, _drawingSurface);
			foreach (RA2Object o in tile.AllObjects)
				_theater.DrawObject(o, _drawingSurface);
		}


		private List<RA2Object> GetObjectsAt(int dx, int dy) {
			var ret = new List<RA2Object>();

			if (_smudgeObjects[dx, dy] != null)
				ret.Add(_smudgeObjects[dx, dy]);

			var ovl = _overlayObjects[dx, dy];
			Drawable ovlDraw = null;
			if (ovl != null) {
				ovlDraw = _theater.GetCollection(CollectionType.Overlay).GetDrawable(ovl);
				if (ovlDraw != null && !ovlDraw.Overrides)
					ret.Add(_overlayObjects[dx, dy]);
			}

			if (_terrainObjects[dx, dy] != null)
				ret.Add(_terrainObjects[dx, dy]);

			if (_infantryObjects[dx, dy] != null)
				foreach (var r in _infantryObjects[dx, dy])
					ret.Add(r);

			if (_aircraftObjects[dx, dy] != null)
				ret.Add(_aircraftObjects[dx, dy]);

			if (_unitObjects[dx, dy] != null)
				ret.Add(_unitObjects[dx, dy]);

			if (_structureObjects[dx, dy] != null)
				ret.Add(_structureObjects[dx, dy]);

			if (ovlDraw != null && ovlDraw.Overrides)
				ret.Add(ovl);

			return ret;
		}

		public DrawingSurface GetDrawingSurface() {
			return _drawingSurface;
		}
		public TileLayer GetTiles() {
			return _tiles;
		}
		public Theater GetTheater() {
			return _theater;
		}

		public int TileWidth {
			get { return EngineType == EngineType.RedAlert2 || EngineType == EngineType.YurisRevenge ? 60 : 48; }
		}

		public int TileHeight {
			get { return EngineType == EngineType.RedAlert2 || EngineType == EngineType.YurisRevenge ? 30 : 24; }
		}

		internal void FreeUseless() {
			_rules.Dispose();
			_art.Dispose();
			baseStream.Dispose();
			_theater = null;
			_tiles = null;
			_overlayObjects = null;
			_smudgeObjects = null;
			_unitObjects = null;
			_infantryObjects = null;
			_structureObjects = null;
			_unitObjects = null;
			_aircraftObjects = null; ;
			_countryColors.Clear();
			_namedColors.Clear();
			_lightSources.Clear();
			_palettePerLevel.Clear();
			_palettesToBeRecalculated.Clear();
		}
	}
}
