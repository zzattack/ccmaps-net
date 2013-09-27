using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.Encodings;
using CNCMaps.Game;
using CNCMaps.Rendering;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace CNCMaps.Map {

	/// <summary>Map file.</summary>
	public class MapFile : IniFile {
		public EngineType Engine { get; private set; }
		public Rectangle FullSize { get; private set; }
		public Rectangle LocalSize { get; private set; }
		private Theater _theater;
		private IniFile _rules;
		private IniFile _art;

		private TileLayer _tiles;
		private readonly List<OverlayObject> _overlayObjects = new List<OverlayObject>();
		private readonly List<SmudgeObject> _smudgeObjects = new List<SmudgeObject>();
		private readonly List<TerrainObject> _terrainObjects = new List<TerrainObject>();
		private readonly List<StructureObject> _structureObjects = new List<StructureObject>();
		private readonly List<InfantryObject> _infantryObjects = new List<InfantryObject>();
		private readonly List<UnitObject> _unitObjects = new List<UnitObject>();
		private readonly List<AircraftObject> _aircraftObjects = new List<AircraftObject>();
		private readonly List<AnimationObject> _animationObjects = new List<AnimationObject>();

		private readonly Dictionary<string, Color> _countryColors = new Dictionary<string, Color>();
		private readonly Dictionary<string, Color> _namedColors = new Dictionary<string, Color>();

		private Lighting _lighting;
		private readonly List<LightSource> _lightSources = new List<LightSource>();
		private readonly List<Palette> _palettePerLevel = new List<Palette>(19);
		private readonly HashSet<Palette> _palettesToBeRecalculated = new HashSet<Palette>();

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

		private double PercentageObjectsKnown(VFS vfs, IniFile rules, TheaterSettings ths) {
			var theaterIni = vfs.OpenFile<IniFile>(ths.TheaterIni);
			if (rules == null || theaterIni == null) return 0.0;

			Func<GameObject, IniSection, bool> objectKnown = (obj, section) => {
				if (obj is NamedObject) {
					string name = (obj as NamedObject).Name;
					return section.OrderedEntries.Any(kvp => kvp.Value.ToString().Equals(name, StringComparison.InvariantCultureIgnoreCase));
				}
				else if (obj is NumberedObject) {
					int number = (obj as NumberedObject).Number;
					return section.HasKey(number.ToString());
				}
				return false; // should not happen
			};

			int known = 0;
			int total = 0;

			var tiles = _tiles.Where(t => t != null).DistinctBy(t => t.TileNum);
			var tilesCollection = new TileCollection(ths, vfs.OpenFile<IniFile>(ths.TheaterIni));
			tilesCollection.InitTilesets();
			known += tiles.Count(o => o.TileNum <= tilesCollection.NumTiles);
			total += tiles.Count();

			var infs = _infantryObjects.DistinctBy(o => o.Name);
			known += infs.Count(o => objectKnown(o, rules.GetSection("InfantryTypes")));
			total += infs.Count();

			var terrains = _terrainObjects.DistinctBy(o => o.Name);
			known += terrains.Count(o => objectKnown(o, rules.GetSection("TerrainTypes")));
			total += terrains.Count();

			var units = _unitObjects.DistinctBy(o => o.Name);
			known += units.Count(o => objectKnown(o, rules.GetSection("VehicleTypes")));
			total += units.Count();

			var aircrafts = _aircraftObjects.DistinctBy(o => o.Name);
			known += aircrafts.Count(o => objectKnown(o, rules.GetSection("AircraftTypes")));
			total += aircrafts.Count();

			var smudges = _smudgeObjects.DistinctBy(o => o.Name);
			known += smudges.Count(o => objectKnown(o, rules.GetSection("SmudgeTypes")));
			total += smudges.Count();

			var structures = _structureObjects.DistinctBy(o => o.Name);
			known += structures.Count(o => objectKnown(o, rules.GetSection("BuildingTypes"))
				|| objectKnown(o, rules.GetSection("OverlayTypes")));
			total += structures.Count();

			var overlays = _overlayObjects.DistinctBy(o => o.Number);
			known += overlays.Count(o => objectKnown(o, rules.GetSection("OverlayTypes")));
			total += overlays.Count();


			return known / (double)total;
		}

		private void RemoveUnknownObjects() {
			ObjectCollection c = _theater.GetCollection(CollectionType.Terrain);
			foreach (var obj in _terrainObjects.Where(obj => !c.HasObject(obj)).ToList()) {
				_terrainObjects.Remove(obj);
				obj.Tile.RemoveObject(obj);
			}

			c = _theater.GetCollection(CollectionType.Infantry);
			foreach (var obj in _infantryObjects.Where(obj => !c.HasObject(obj)).ToList()) {
				obj.Tile.RemoveObject(obj);
				_infantryObjects.Remove(obj);

			}

			c = _theater.GetCollection(CollectionType.Vehicle);
			foreach (var obj in _unitObjects.Where(obj => !c.HasObject(obj)).ToList()) {
				obj.Tile.RemoveObject(obj);
				_unitObjects.Remove(obj);
			}

			c = _theater.GetCollection(CollectionType.Aircraft);
			foreach (var obj in _aircraftObjects.Where(obj => !c.HasObject(obj)).ToList()) {
				obj.Tile.RemoveObject(obj);
				_aircraftObjects.Remove(obj);
			}

			c = _theater.GetCollection(CollectionType.Smudge);
			foreach (var obj in _smudgeObjects.Where(obj => !c.HasObject(obj)).ToList()) {
				obj.Tile.RemoveObject(obj);
				_smudgeObjects.Remove(obj);
			}


			c = _theater.GetCollection(CollectionType.Building);
			var cAlt = _theater.GetCollection(CollectionType.Overlay);
			foreach (var obj in _structureObjects.Where(obj => !c.HasObject(obj) && !cAlt.HasObject(obj)).ToList()) {
				obj.Tile.RemoveObject(obj);
				_structureObjects.Remove(obj);
			}
		}

		private void SetDrawables() {
			foreach (var tile in _tiles) {
				if (tile == null) continue;
				tile.Drawable = new Drawable(tile.ToString()) { DrawFlat = true };

				foreach (var obj in tile.AllObjects) {
					obj.Collection = _theater.GetObjectCollection(obj);
					obj.Drawable = obj.Collection.GetDrawable(obj);
				}
			}
		}

		/// <summary>Detect map type.</summary>
		/// <param name="rules">The rules.ini file to be used.</param>
		/// <returns>The engine to be used to render this map.</returns>
		private void DetectEngineType() {
			var vfsTS = new VFS();
			var vfsFS = new VFS();
			var vfsRA2 = new VFS();
			var vfsYR = new VFS();

			if (Directory.Exists(VFS.TSInstallDir)) {
				vfsTS.ScanMixDir(VFS.TSInstallDir, EngineType.TiberianSun);
				vfsFS.ScanMixDir(VFS.TSInstallDir, EngineType.Firestorm);
			}

			if (Directory.Exists(VFS.RA2InstallDir)) {
				vfsRA2.ScanMixDir(VFS.RA2InstallDir, EngineType.RedAlert2);
				vfsYR.ScanMixDir(VFS.RA2InstallDir, EngineType.YurisRevenge);
			}

			IniFile rulesTS = vfsTS.OpenFile<IniFile>("rules.ini");
			IniFile rulesFS = vfsFS.OpenFile<IniFile>("rules.ini");
			if (rulesFS != null)
				rulesFS.MergeWith(vfsFS.OpenFile<IniFile>("firestrm.ini"));

			IniFile rulesRA2 = vfsRA2.OpenFile<IniFile>("rules.ini");
			IniFile rulesYR = vfsYR.OpenFile<IniFile>("rulesmd.ini");

			string theater = ReadString("Map", "Theater");
			TheaterType thType = Theater.TheaterTypeFromString(theater);
			TheaterSettings thsTS = ModConfig.DefaultsTS.GetTheater(thType);
			TheaterSettings thsFS = ModConfig.DefaultsFS.GetTheater(thType);
			TheaterSettings thsRA2 = ModConfig.DefaultsRA2.GetTheater(thType);
			TheaterSettings thsYR = ModConfig.DefaultsYR.GetTheater(thType);

			foreach (var f in thsTS.Mixes)
				vfsTS.AddFile(f);
			foreach (var f in thsFS.Mixes)
				vfsFS.AddFile(f);
			foreach (var f in thsRA2.Mixes)
				vfsRA2.AddFile(f);
			foreach (var f in thsYR.Mixes)
				vfsYR.AddFile(f);

			Engine = DetectEngineFromRules(rulesTS, rulesFS, rulesRA2, rulesYR, thsTS, thsFS, thsRA2, thsYR, vfsTS, vfsFS, vfsRA2, vfsYR);
			Logger.Debug("Engine type detected as {0}", Engine);
		}

		private EngineType DetectEngineFromRules(
			IniFile rulesTS, IniFile rulesFS, IniFile rulesRA2, IniFile rulesYR,
			TheaterSettings theaterTS, TheaterSettings theaterFS, TheaterSettings theaterRA2, TheaterSettings theaterYR,
			VFS vfsTS, VFS vfsFS, VFS vfsRA2, VFS vfsYR) {
			
			double tsScore = PercentageObjectsKnown(vfsTS, rulesTS, theaterTS);
			double fsScore = PercentageObjectsKnown(vfsFS, rulesFS, theaterFS);
			double ra2Score = PercentageObjectsKnown(vfsRA2, rulesRA2, theaterRA2);
			double yrScore = PercentageObjectsKnown(vfsYR, rulesYR, theaterYR);

			double maxScore = Math.Max(Math.Max(Math.Max(tsScore, fsScore), ra2Score), yrScore);
			if (maxScore == ra2Score) return EngineType.RedAlert2;
			else if (maxScore == yrScore) return EngineType.YurisRevenge;
			else if (maxScore == tsScore) return EngineType.TiberianSun;
			else if (maxScore == fsScore) return EngineType.Firestorm;
			return EngineType.YurisRevenge; // default
		}

		private EngineType DetectRA2orYR() {
			Logger.Info("Determining map type");

			if (ReadBool("Basic", "RequiredAddon"))
				return EngineType.YurisRevenge;

			string theater = ReadString("Map", "Theater").ToLower();

			// decision based on theatre
			if (theater == "lunar" || theater == "newurban" || theater == "desert")
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
			else if (new[] { ".yrm", ".yro" }.Contains(Path.GetExtension(FileName))) return EngineType.YurisRevenge;
			else return EngineType.RedAlert2;
		}

		/// <summary>Loads a map. </summary>
		/// <param name="et">The engine type to be forced, or autodetect.</param>
		public bool LoadMap(EngineType et = EngineType.AutoDetect) {
			var map = GetSection("Map");
			string[] size = map.ReadString("Size").Split(',');
			FullSize = new Rectangle(int.Parse(size[0]), int.Parse(size[1]), int.Parse(size[2]), int.Parse(size[3]));
			size = map.ReadString("LocalSize").Split(',');
			LocalSize = new Rectangle(int.Parse(size[0]), int.Parse(size[1]), int.Parse(size[2]), int.Parse(size[3]));
			Engine = et;
			ReadAllObjects();

			// if we have to autodetect, we need to load rules.ini,
			// and we don't want to parse it again when constructing the theater
			try {
				if (et == EngineType.AutoDetect)
					DetectEngineType();
				return true;
			}
			catch {
				return false;
			}
		}

		// between LoadMap and LoadTheater, the VFS should be initialized
		public bool LoadTheater() {
			if (Engine == EngineType.YurisRevenge) {
				_rules = VFS.Open("rulesmd.ini") as IniFile;
				_art = VFS.Open("artmd.ini") as IniFile;
			}
			else if (Engine == EngineType.Firestorm) {
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

			Logger.Info("Overriding rules.ini with map INI entries");
			_rules.MergeWith(this);

			_theater = new Theater(ReadString("Map", "Theater"), Engine, _rules, _art);
			_theater.Initialize();
			RemoveUnknownObjects();
			SetDrawables();

			LoadColors();
			if (Engine >= EngineType.RedAlert2)
				LoadCountries();
			LoadHouses();

			//if (EngineType >= EngineType.RedAlert2)
			//	_theater.GetTileCollection().RecalculateTileSystem(_tiles);


			RecalculateOreSpread(); // is this really used on TS?

			LoadLighting();
			CreateLevelPalettes();
			LoadPalettes();
			ApplyRemappables();
			if (!Program.Settings.IgnoreLighting) {
				LoadLightSources();
				ApplyLightSources();
			}

			SetBaseTiles(); // requires .AnimationDrawable set on objects

			// first preparing all palettes as above, and only now recalculating them 
			// could save a large amount of work in total
			RecalculatePalettes();

			return true;
		}

		/// <summary>Loads the countries. </summary>
		private void LoadCountries() {
			Logger.Info("Loading countries");

			var countriesSection = _rules.GetSection(Engine >= EngineType.RedAlert2 ? "Countries" : "Houses");
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
									 int.Parse(colorComponents[1]),
									 int.Parse(colorComponents[2]));
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

		private void SetBaseTiles() {
			// we need foundations from the theater to place the structures at the correct tile,
			foreach (GameObject obj in _structureObjects.Cast<GameObject>().Union(_overlayObjects).Union(_smudgeObjects)) {
				if (obj.BottomTile == null) {
					// s.BottomTile set means we've already moved it
					var foundation = obj.Drawable.Foundation;
					var bottom = _tiles.GetTileR(obj.Tile.Rx + foundation.Width - 1, obj.Tile.Ry + foundation.Height - 1);
					obj.BottomTile = bottom ?? obj.Tile;
					obj.TopTile = obj.Tile;
				}
			}

			foreach (GameObject obj in _unitObjects.Cast<GameObject>().Union(_aircraftObjects).Union(_infantryObjects)) {
				var bounds = obj.Drawable.GetBounds(obj);
				// bounds to foundation
				Size occupy = new Size(
					(int)Math.Max(1, Math.Ceiling(bounds.Width / (double)Drawable.TileWidth)),
					(int)Math.Max(1, Math.Ceiling(bounds.Height / (double)Drawable.TileHeight)));

				int bridge = (obj as OwnableObject).OnBridge ? -2 : 0;
				var top = _tiles.GetTileR(obj.Tile.Rx + bridge - 1 + occupy.Width, obj.Tile.Ry + bridge - 1 + occupy.Height);
				var bottom = _tiles.GetTileR(obj.Tile.Rx, obj.Tile.Ry);
				obj.BottomTile = bottom ?? obj.Tile;
				obj.TopTile = top ?? obj.Tile;
			}

			foreach (OverlayObject obj in _overlayObjects.Where(SpecialOverlays.IsHighBridge)) {
				var bottom = _tiles.GetTileR(obj.Tile.Rx + 2, obj.Tile.Ry + 2);
				obj.BottomTile = bottom ?? obj.Tile;
				obj.TopTile = obj.BottomTile;
			}
		}

		/// <summary>Reads all objects. </summary>
		private void ReadAllObjects() {
			Logger.Info("Reading tiles");
			ReadTiles();

			Logger.Info("Reading map overlay");
			ReadOverlay();

			Logger.Info("Reading map terrain objects");
			ReadTerrain();

			Logger.Info("Reading map smudge objects");
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
			int cells = (FullSize.Width * 2 - 1) * FullSize.Height;
			int lzoPackSize = cells * 11 + 4; // last 4 bytes contains a lzo pack header saying no more data is left

			var isoMapPack = new byte[lzoPackSize];
			uint totalDecompressSize = Format5.DecodeInto(lzoData, isoMapPack);

			_tiles = new TileLayer(FullSize.Size);
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
				if (dx >= 0 && dx < 2 * _tiles.Width &&
					dy >= 0 && dy < 2 * _tiles.Height) {
					var tile = new MapTile((ushort)dx, (ushort)dy, rx, ry, z, tilenum, subtile, _tiles);
					_tiles[(ushort)dx, (ushort)dy / 2] = tile;
				}
			}

			// fix null tiles to blank
			for (ushort y = 0; y < FullSize.Height; y++) {
				for (ushort x = 0; x <= FullSize.Width * 2 - 2; x++) {
					ushort dx = (ushort)(x);
					ushort dy = (ushort)(y * 2 + x % 2);
					ushort rx = (ushort)((dx + dy) / 2 + 1);
					ushort ry = (ushort)(dy - rx + FullSize.Width + 1);
					if (_tiles[x, y] == null) _tiles[x, y] = new MapTile(dx, dy, rx, ry, 0, 0, 0, _tiles);
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
					var t = new TerrainObject(name);
					var tile = _tiles.GetTileR(rx, ry);
					if (tile != null) {
						tile.AddObject(t);
						_terrainObjects.Add(t);
					}
				}
			}
			Logger.Debug("Read {0} terrain objects", _terrainObjects.Count);
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
					var s = new SmudgeObject(name);
					var tile = _tiles.GetTileR(rx, ry);
					if (tile != null) {
						tile.AddObject(s);
						_smudgeObjects.Add(s);
					}
				}
				catch (FormatException) {
				}
			}
			Logger.Debug("Read {0} smudges", _smudgeObjects.Count);
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

			foreach (MapTile t in _tiles) {
				if (t == null) continue;
				int idx = t.Rx + 512 * t.Ry;
				byte overlay_id = overlayPack[idx];
				if (overlay_id != 0xFF) {
					byte overlay_value = overlayDataPack[idx];
					var ovl = new OverlayObject(overlay_id, overlay_value);
					t.AddObject(ovl);
					_overlayObjects.Add(ovl);
				}
			}
			Logger.Debug("Read {0} overlay types", _overlayObjects.Count);
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
					var s = new StructureObject(owner, name, health, direction);
					s.Tile = _tiles.GetTileR(rx, ry);
					s.Upgrade1 = entries[12];
					s.Upgrade2 = entries[13];
					s.Upgrade3 = entries[14];
					if (s.Tile != null) {
						_structureObjects.Add(s);
						s.Tile.AddObject(s);
					}
				}
				catch (IndexOutOfRangeException) {
				} // catch invalid entries
				catch (FormatException) {
				}
			}
			Logger.Trace("Read {0} structures", _structureObjects.Count);
		}

		/// <summary>Reads the infantry. </summary>
		private void ReadInfantry() {
			IniSection infantrySection = GetSection("Infantry");
			if (infantrySection == null) {
				Logger.Info("Infantry section unavailable in {0}", Path.GetFileName(FileName));
				return;
			}
			int count = 0;
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
					var i = new InfantryObject(owner, name, health, direction, onBridge);
					var tile = _tiles.GetTileR(rx, ry);
					if (tile != null) {
						tile.AddObject(i);
						_infantryObjects.Add(i);
						count++;
					}
				}
				catch (IndexOutOfRangeException) {
				}
				catch (FormatException) {
				}
			}
			Logger.Trace("Read {0} infantry objects", count);

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
					var u = new UnitObject(owner, name, health, direction, onBridge);
					var tile = _tiles.GetTileR(rx, ry);
					if (tile != null) {
						tile.AddObject(u);
						_unitObjects.Add(u);
					}
				}
				catch (FormatException) {
				}
				catch (IndexOutOfRangeException) {
				}
			}
			Logger.Trace("Read {0} units", _unitObjects.Count);
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
					var a = new AircraftObject(owner, name, health, direction, onBridge);
					var tile = _tiles.GetTileR(rx, ry);
					if (tile != null) {
						tile.AddObject(a);
						_aircraftObjects.Add(a);
					}
				}
				catch (FormatException) {
				}
				catch (IndexOutOfRangeException) {
				}
			}
			Logger.Trace("Read {0} aircraft objects", _aircraftObjects.Count);
		}

		private void RecalculateOreSpread() {
			Logger.Info("Redistributing ore-spread over patches");
			foreach (OverlayObject o in _overlayObjects) {
				if (o == null) continue;
				// The value consists of the sum of all dx's with a little magic offsets
				// plus the sum of all dy's with also a little magic offset, and also
				// everything is calculated modulo 12
				var type = SpecialOverlays.GetOverlayTibType(o, Engine);

				if (type == OverlayTibType.Ore) {
					int x = o.Tile.Dx;
					int y = o.Tile.Dy;
					double yInc = ((((y - 9) / 2) % 12) * (((y - 8) / 2) % 12)) % 12;
					double xInc = ((((x - 13) / 2) % 12) * (((x - 12) / 2) % 12)) % 12;

					// x_inc may be > y_inc so adding a big number outside of cell bounds
					// will surely keep num positive
					var num = (int)(yInc - xInc + 120000);
					num %= 12;

					// replace ore
					o.OverlayID = (byte)(SpecialOverlays.Ra2MinIdRiparius + num);
				}

				else if (type == OverlayTibType.Gems) {
					int x = o.Tile.Dx;
					int y = o.Tile.Dy;
					double yInc = ((((y - 9) / 2) % 12) * (((y - 8) / 2) % 12)) % 12;
					double xInc = ((((x - 13) / 2) % 12) * (((x - 12) / 2) % 12)) % 12;

					// x_inc may be > y_inc so adding a big number outside of cell bounds
					// will surely keep num positive
					var num = (int)(yInc - xInc + 120000);
					num %= 12;

					// replace gems
					o.OverlayID = (byte)(SpecialOverlays.Ra2MinIdCruentus + num);
				}

			}
		}

		private void LoadLighting() {
			Logger.Info("Loading lighting");
			if (!Program.Settings.IgnoreLighting)
				_lighting = new Lighting(GetOrCreateSection("Lighting"));
			else {
				_lighting = new Lighting(new IniSection(""));
				_lighting.Level = 0.0;
			}
		}

		// Large-degree changes to make the lighting better mimic the way it is in the game.
		private void CreateLevelPalettes() {
			Logger.Info("Creating per-height palettes");
			PaletteCollection palettes = _theater.GetPalettes();
			for (int i = 0; i < 19; i++) {
				Palette isoHeight = palettes.IsoPalette.Clone();
				isoHeight.ApplyLighting(_lighting, i);
				isoHeight.IsShared = true;
				isoHeight.Name = string.Format("{0} lvl.{1}", isoHeight.Name, i);
				_palettePerLevel.Add(isoHeight);
				_palettesToBeRecalculated.Add(isoHeight);
			}
		}

		private void LoadPalettes() {
			int before = _palettesToBeRecalculated.Count;

			// get the default palettes
			var pc = _theater.GetPalettes();
			foreach (var p in pc) _palettesToBeRecalculated.Add(p);

			foreach (var tile in _tiles) {
				if (tile == null) continue;

				// TODO: move this to a more sensible place
				var tse = _theater.GetTileCollection().GetTileSetEntry(tile);
				if (tse != null && tse.AnimationSubtile == tile.SubTile) {
					var anim = new AnimationObject(tse.AnimationDrawable.Name, tse.AnimationDrawable);
					tile.AddObject(anim);
					_animationObjects.Add(anim);
				}

				foreach (var obj in tile.AllObjects.Union(new[] { tile }).ToList()) {
					if (obj == null) continue;

					Palette p;
					LightingType lt;
					PaletteType pt;

					if (obj is MapTile) {
						lt = LightingType.Full;
						pt = PaletteType.Iso;
					}
					else {
						obj.Collection = _theater.GetObjectCollection(obj);
						pt = obj.Drawable.PaletteType;
						lt = obj.Drawable.LightingType;
					}

					// level, ambient and full benefit from sharing
					if (lt == LightingType.Full && pt == PaletteType.Iso) {
						// bridges are attached to a low tile, but their height-offset should be taken into account 
						int z = obj.Tile.Z + (obj.Drawable != null ? obj.Drawable.TileElevation : 0);
						p = _palettePerLevel[z];
					}
					else if (lt >= LightingType.Level) {
						// when applying lighting to its palette
						p = _theater.GetPalette(obj.Drawable).Clone();
						int z = obj.Tile.Z + (obj.Drawable != null ? obj.Drawable.TileElevation : 0);
						p.ApplyLighting(_lighting, z, lt == LightingType.Full);
					}
					else {
						p = _theater.GetPalette(obj.Drawable).Clone();
					}
					_palettesToBeRecalculated.Add(p);
					obj.Palette = p;
				}
			}
			Logger.Debug("Loaded {0} different palettes", _palettesToBeRecalculated.Count - before);
		}

		private void ApplyRemappables() {
			int before = _palettesToBeRecalculated.Count;

			foreach (OwnableObject obj in _structureObjects.Cast<OwnableObject>().Union(_unitObjects).Union(_aircraftObjects).Union(_infantryObjects)) {
				var g = obj as GameObject;
				if (g != null & g.Drawable != null && g.Drawable.IsRemapable) {
					if (g.Palette.IsShared) // don't wanna touch somebody elses palette..
						g.Palette = g.Palette.Clone();
					var color = _countryColors.ContainsKey(obj.Owner) ? _countryColors[obj.Owner] : _countryColors.First().Value;
					g.Palette.Remap(color);
					_palettesToBeRecalculated.Add(g.Palette);
				}
			}

			// TS needs tiberium remapped
			if (Engine <= EngineType.Firestorm) {
				var tiberiums = _rules.GetSection("Tiberiums").OrderedEntries.Select(tib => tib.Value.ToString());
				var remaps = tiberiums.Select(tib => _rules.GetOrCreateSection(tib).ReadString("Color"));
				var tibRemaps = tiberiums.Zip(remaps, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);

				foreach (var ovl in _overlayObjects) {
					if (ovl == null) continue;
					var tibType = SpecialOverlays.GetOverlayTibType(ovl, Engine);
					if (tibType != OverlayTibType.NotSpecial) {
						ovl.Palette = ovl.Palette.Clone();
						string tibName = SpecialOverlays.GetTibName(ovl, Engine);
						if (tibRemaps.ContainsKey(tibName) && _namedColors.ContainsKey(tibRemaps[tibName]))
							ovl.Palette.Remap(_namedColors[tibRemaps[tibName]]);
						_palettesToBeRecalculated.Add(ovl.Palette);
					}
				}
			}
			Logger.Debug("Determined palettes to be recalculated due to remappables ({0})",
						 _palettesToBeRecalculated.Count - before);
		}

		private void LoadLightSources() {
			Logger.Info("Loading light sources");
			foreach (StructureObject s in _structureObjects.ToList()) {
				var section = _rules.GetSection(s.Name);
				if (section != null && section.HasKey("LightVisibility")) {
					var ls = new LightSource(_rules.GetSection(s.Name), _lighting);
					ls.Tile = s.Tile;
					_lightSources.Add(ls);
					s.Tile.RemoveObject(s, true);
					_structureObjects.Remove(s); // arent drawn anyway
				}
			}
		}

		private void ApplyLightSources() {
			int before = _palettesToBeRecalculated.Count;
			foreach (LightSource lamp in _lightSources) {
				foreach (MapTile t in _tiles) {
					if (t == null) continue;

					bool wasShared = t.Palette.IsShared;
					// make sure this tile can only end up in the "to-be-recalculated list" once
					if (!lamp.ApplyLamp(t)) continue;

					// this lamp caused a new unshared palette to be created
					if (wasShared && !t.Palette.IsShared)
						_palettesToBeRecalculated.Add(t.Palette);

					foreach (var obj in t.AllObjects.Where(o => o.Lighting == LightingType.Full || o.Lighting == LightingType.Ambient)) {
						wasShared = obj.Palette.IsShared;
						lamp.ApplyLamp(obj, obj.Lighting == LightingType.Ambient);
						_palettesToBeRecalculated.Add(obj.Palette);
						if (wasShared && !obj.Palette.IsShared)
							_palettesToBeRecalculated.Add(obj.Palette);
					}
				}
			}
			Logger.Debug("Determined palettes to be recalculated due to lightsources ({0})",
						 _palettesToBeRecalculated.Count - before);
		}

		private void RecalculatePalettes() {
			Logger.Info("Calculating palette-values for all objects");
			foreach (Palette p in _palettesToBeRecalculated)
				p.Recalculate();
		}

		public void MarkTiledStartPositions() {
			Logger.Info("Marking tiled start positions");
			IniSection basic = GetSection("Basic");
			if (basic == null || !basic.ReadBool("MultiplayerOnly")) return;
			IniSection waypoints = GetSection("Waypoints");
			Palette red = Palette.MakePalette(Color.Red);

			foreach (var entry in waypoints.OrderedEntries) {
				try {
					if (int.Parse(entry.Key) >= 8)
						continue;
					int pos = int.Parse(entry.Value);
					int wy = pos / 1000;
					int wx = pos - wy * 1000;

					// Draw 4x4 cell around start pos
					for (int x = wx - 1; x < wx + 3; x++) {
						for (int y = wy - 1; y < wy + 3; y++) {
							MapTile t = _tiles.GetTileR(x, y);
							if (t != null) {
								t.Palette = Palette.Merge(t.Palette, red, 0.4);
								//foreach (var o in t.AllObjects.OfType<SmudgeObject>().Cast<GameObject>().Union(t.AllObjects.OfType<OverlayObject>()))
								//	o.Palette = Palette.Merge(o.Palette, red, 0.4);
							}
						}
					}
				}
				catch (FormatException) {
				}
				catch (IndexOutOfRangeException) {
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
				try {
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
							List<GameObject> objs = GetObjectsAt(t.Dx, t.Dy / 2);
							foreach (GameObject o in objs)
								_theater.Draw(o, _drawingSurface);
						}
					}
				}
				catch (IndexOutOfRangeException) {
				}
				catch (FormatException) {
				}
			}
		}

		public unsafe void DrawSquaredStartPositions() {
			Logger.Info("Marking squared start positions");
			IniSection basic = GetSection("Basic");
			if (basic == null || !basic.ReadBool("MultiplayerOnly")) return;
			IniSection waypoints = GetSection("Waypoints");

			foreach (var entry in waypoints.OrderedEntries) {
				try {
					if (int.Parse(entry.Key) >= 8)
						continue;
					int pos = int.Parse(entry.Value);
					int wy = pos / 1000;
					int wx = pos - wy * 1000;

					MapTile t = _tiles.GetTileR(wx, wy);
					if (t == null) continue;
					int destX = t.Dx * TileWidth / 2;
					int destY = (t.Dy - t.Z) * TileHeight / 2;

					bool vert = FullSize.Height * 2 > FullSize.Width;
					int radius;
					if (vert)
						radius = 10 * FullSize.Height * TileHeight / 2 / 144;
					else
						radius = 10 * FullSize.Width * TileWidth / 2 / 133;

					int h = radius, w = radius;
					for (int drawY = destY - h / 2; drawY < destY + h; drawY++) {
						for (int drawX = destX - w / 2; drawX < destX + w; drawX++) {
							byte* p = (byte*)_drawingSurface.BitmapData.Scan0 + drawY * _drawingSurface.BitmapData.Stride + 3 * drawX;
							*p++ = 0x00;
							*p++ = 0x00;
							*p++ = 0xFF;
						}
					}
				}
				catch (FormatException) {
				}
				catch (IndexOutOfRangeException) {
				}
			}
		}

		public int FindCutoffHeight() {
			// searches in 10 rows, starting from the bottom up, for the first fully tiled row
			int y;

#if DEBUG
			// print map:
			var tileTouchGrid = _tiles.GridTouched;
			var sb = new System.Text.StringBuilder();
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
			// File.WriteAllText("cutoffmap.txt", sb.ToString());
#endif

			for (y = FullSize.Height - 1; y > FullSize.Height - 10; y--) {
				bool isRowFilled = true;
				for (int x = 1; x < FullSize.Width * 2 - 3; x++) {
					if (_tiles.GridTouched[x, y] == TileLayer.TouchType.Untouched) {
						isRowFilled = false;
						break;
					}
				}
				if (isRowFilled)
					break;
			}
			Logger.Debug("Cutoff-height determined at {0}, cutting off {1} rows", y, FullSize.Height - y);
			return y;
		}

		internal Rectangle GetSizePixels(SizeMode sizeMode) {
			switch (sizeMode) {
				case SizeMode.Local: return GetLocalSizePixels();
				case SizeMode.Full: return GetFullMapSizePixels();
				case SizeMode.Auto: return GetAutoSizePixels();
			}
			return Rectangle.Empty;
		}

		public Rectangle GetAutoSizePixels() {
			// uses full map, but if it's too close to localsize reverts to local
			var full = GetFullMapSizePixels();
			var local = GetLocalSizePixels();
			const double delta = 0.15;
			if (Math.Abs(full.Left - local.Left) / (double)full.Width < delta
				&& Math.Abs(full.Width - local.Width) / (double)full.Width < delta
				&& Math.Abs(full.Top - local.Top) / (double)full.Height < delta
				&& Math.Abs(full.Bottom - local.Bottom) / (double)full.Height < delta)
				return local;
			else return full;
		}

		public Rectangle GetFullMapSizePixels() {
			int left = TileWidth / 2,
				top = TileHeight / 2;
			int right = (FullSize.Width - 1) * TileWidth;
			int cutoff = FindCutoffHeight();
			int bottom = cutoff * TileHeight + (1 + (cutoff % 2)) * (TileHeight / 2);
			return Rectangle.FromLTRB(left, top, right, bottom);
		}

		public Rectangle GetLocalSizePixels() {
			int left = Math.Max(LocalSize.Left * TileWidth, 0),
				top = Math.Max(LocalSize.Top - 3, 0) * TileHeight + TileHeight / 2;
			int right = (LocalSize.Left + LocalSize.Width) * TileWidth;

			int bottom1 = 2 * (LocalSize.Top - 3 + LocalSize.Height + 5);
			int cutoff = FindCutoffHeight() * 2;
			int bottom2 = (cutoff + 1 + (cutoff % 2));
			int bottom = Math.Min(bottom1, bottom2) * (TileHeight / 2);
			return Rectangle.FromLTRB(left, top, right, bottom);
		}

		public void MarkOreAndGems() {
			Logger.Info("Marking ore and gems");
			var markerPalettes = new Dictionary<OverlayTibType, Palette>();

			// init sensible defaults
			if (Engine >= EngineType.RedAlert2) {
				markerPalettes[OverlayTibType.Ore] = Palette.MakePalette(Color.Yellow);
				markerPalettes[OverlayTibType.Gems] = Palette.MakePalette(Color.Purple);
			}

			// read [Tiberiums] for names or different tiberiums, and the [Color] entry (for TS)
			// for their corresponding "remapped" color. In RA2 this isn't actually used,
			// but made available for the renderer's preview functionality through a key "MapRendererColor"
			var tiberiums = _rules.GetOrCreateSection("Tiberiums").OrderedEntries.Select(kvp => kvp.Value).ToList();
			var remaps = tiberiums.Select(tib => _rules.GetOrCreateSection(tib)
				.ReadString(Engine >= EngineType.RedAlert2 ? "MapRendererColor" : "Color")).ToList();

			// override defaults if specified in rules
			for (int i = 0; i < tiberiums.Count; i++) {
				OverlayTibType type = (OverlayTibType)Enum.Parse(typeof(OverlayTibType), tiberiums[i]);
				string namedColor = remaps[i];
				if (_namedColors.ContainsKey(namedColor))
					markerPalettes[type] = Palette.MakePalette(_namedColors[namedColor]);
			}

			// apply the 'marking' by replacing the tile containing ore by a partly
			// transparent version of its original, merged with a palette of solely
			// the color of the tiberium kind stored on it
			foreach (var o in _overlayObjects) {
				if (o == null) continue;
				var ovlType = SpecialOverlays.GetOverlayTibType(o, Engine);
				if (!markerPalettes.ContainsKey(ovlType)) continue;

				double opacityBase = ovlType == OverlayTibType.Ore && Engine == EngineType.RedAlert2 ? 0.3 : 0.15;
				double opacity = Math.Max(0, 12 - o.OverlayValue) / 11.0 * 0.5 + opacityBase;
				o.Tile.Palette = Palette.Merge(o.Tile.Palette, markerPalettes[ovlType], opacity);
				o.Palette = Palette.Merge(o.Palette, markerPalettes[ovlType], opacity);
			}
		}

		public void RedrawOreAndGems() {
			var tileCollection = _theater.GetTileCollection();
			var checkFunc = new Func<OverlayObject, bool>(delegate(OverlayObject ovl) {
				return SpecialOverlays.GetOverlayTibType(ovl, Engine) != OverlayTibType.NotSpecial;
			});

			// first redraw all required tiles (zigzag method)
			for (int y = 0; y < FullSize.Height; y++) {
				for (int x = FullSize.Width * 2 - 2; x >= 0; x -= 2) {
					if (_tiles[x, y].AllObjects.OfType<OverlayObject>().Any(checkFunc))
						tileCollection.DrawTile(_tiles.GetTile(x, y), _drawingSurface);
				}
				for (int x = FullSize.Width * 2 - 3; x >= 0; x -= 2) {
					if (_tiles[x, y].AllObjects.OfType<OverlayObject>().Any(checkFunc))
						tileCollection.DrawTile(_tiles.GetTile(x, y), _drawingSurface);
				}
			}
			for (int y = 0; y < FullSize.Height; y++) {
				for (int x = FullSize.Width * 2 - 2; x >= 0; x -= 2) {
					if (_tiles[x, y].AllObjects.OfType<OverlayObject>().Any(checkFunc)) {
						List<GameObject> objs = GetObjectsAt(x, y);
						foreach (GameObject o in objs)
							_theater.Draw(o, _drawingSurface);
					}
				}
				for (int x = FullSize.Width * 2 - 3; x >= 0; x -= 2) {
					if (_tiles[x, y].AllObjects.OfType<OverlayObject>().Any(checkFunc)) {
						List<GameObject> objs = GetObjectsAt(x, y);
						foreach (GameObject o in objs)
							_theater.Draw(o, _drawingSurface);
					}
				}
			}
		}

		public void DrawMap() {
			Logger.Info("Drawing map");
			_drawingSurface = new DrawingSurface(FullSize.Width * TileWidth, FullSize.Height * TileHeight, PixelFormat.Format24bppRgb);
			var sorter = new ObjectSorter(_theater, _tiles);
			var orderedObjs = sorter.GetOrderedObjects().ToList();
			double lastReported = 0.0;
			for (int i = 0; i < orderedObjs.Count; i++) {
				var obj = orderedObjs[i];
				_theater.Draw(obj, _drawingSurface);
				double pct = 100.0 * i / orderedObjs.Count;
				if (pct > lastReported + 5) {
					Logger.Info("Drawing map... {0}%", Math.Round(pct, 0));
					lastReported = pct;
				}
			}

#if DEBUG

			// test that my bounds make some kind of sense
			/*_drawingSurface.Unlock();
			using (Graphics gfx = Graphics.FromImage(_drawingSurface.Bitmap)) {
				foreach (var obj in orderedObjs)
					if (obj.Drawable != null)
						obj.Drawable.DrawBoundingBox(obj, gfx);
			}*/
#endif
			/*
			var tileCollection = _theater.GetTileCollection();
			// zig-zag drawing technique explanation: http://stackoverflow.com/questions/892811/drawing-isometric-game-worlds
			double lastReported = 0.0;
			for (int y = 0; y < FullSize.Height; y++) {
				Logger.Trace("Drawing tiles row {0}", y);
				for (int x = FullSize.Width * 2 - 2; x >= 0; x -= 2) {
					tileCollection.DrawTile(_tiles.GetTile(x, y), _drawingSurface);
				}
				for (int x = FullSize.Width * 2 - 3; x >= 0; x -= 2) {
					tileCollection.DrawTile(_tiles.GetTile(x, y), _drawingSurface);
				}

				double pct = 50.0 * y / FullSize.Height;
				if (pct > lastReported + 5) {
					Logger.Info("Drawing tiles... {0}%", Math.Round(pct, 0));
					lastReported = pct;
				}
			}

			Logger.Info("Tiles drawn");

			for (int y = 0; y < FullSize.Height; y++) {
				Logger.Trace("Drawing objects row {0}", y);
				for (int x = FullSize.Width * 2 - 2; x >= 0; x -= 2) {
					List<GameObject> objs = GetObjectsAt(x, y);
					foreach (GameObject o in objs)
						_theater.Draw(o, _drawingSurface);
				}
				for (int x = FullSize.Width * 2 - 3; x >= 0; x -= 2) {
					List<GameObject> objs = GetObjectsAt(x, y);
					foreach (GameObject o in objs)
						_theater.Draw(o, _drawingSurface);
				}

				double pct = 50 + 50.0 * y / FullSize.Height;
				if (pct > lastReported + 5) {
					Logger.Info("Drawing objects... {0}%", Math.Round(pct, 0));
					lastReported = pct;
				}
			}
			*/
			Logger.Info("Map drawing completed");
		}

		public void GeneratePreviewPack(bool omitMarkers) {
			Logger.Info("Generating PreviewPack data");
			// we will have to re-lock the BitmapData

			_drawingSurface.Lock(_drawingSurface.Bitmap.PixelFormat);
			if (Program.Settings.MarkOreFields == false) {
				Logger.Trace("Marking ore and gems areas");
				MarkOreAndGems();
				Logger.Debug("Redrawing ore and gems areas");
				RedrawOreAndGems();
			}
			if (Program.Settings.StartPositionMarking != StartPositionMarking.Squared) {
				// undo tiled, if needed
				if (Program.Settings.StartPositionMarking == StartPositionMarking.Tiled)
					UndrawTiledStartPositions();

				if (!omitMarkers)
					DrawSquaredStartPositions();
			}

			_drawingSurface.Unlock();

			// Number magic explained: http://modenc.renegadeprojects.com/Maps/PreviewPack
			int pw, ph;
			switch (Engine) {
				case EngineType.TiberianSun:
					pw = (int)Math.Ceiling(1.975 * FullSize.Width);
					ph = (int)Math.Ceiling(0.995 * FullSize.Height);
					break;
				case EngineType.Firestorm:
					pw = (int)Math.Ceiling(1.975 * FullSize.Width);
					ph = (int)Math.Ceiling(0.995 * FullSize.Height);
					break;
				case EngineType.RedAlert2:
					pw = (int)Math.Ceiling(1.975 * FullSize.Width);
					ph = (int)Math.Ceiling(0.995 * FullSize.Height);
					break;
				case EngineType.YurisRevenge:
					pw = (int)Math.Ceiling(1.975 * LocalSize.Width);
					ph = (int)Math.Ceiling(1.00 * LocalSize.Height);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			using (var preview = new Bitmap(pw, ph, PixelFormat.Format24bppRgb)) {

				using (Graphics gfx = Graphics.FromImage(preview)) {
					// use high-quality scaling
					gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
					gfx.SmoothingMode = SmoothingMode.HighQuality;
					gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;
					gfx.CompositingQuality = CompositingQuality.HighQuality;

					var srcRect = GetSizePixels(Program.Settings.SizeMode);
					var dstRect = new Rectangle(0, 0, preview.Width, preview.Height);
					gfx.DrawImage(_drawingSurface.Bitmap, dstRect, srcRect, GraphicsUnit.Pixel);
				}

				Logger.Info("Injecting thumbnail into map");
				ThumbInjector.InjectThumb(preview, this);

				// debug thing to dump original previewpack dimensions
				// preview.Save("C:\\thumbs\\" + Program.Settings.OutputFile + ".png");
				// var originalPreview = ThumbInjector.ExtractThumb(this);
				// originalPreview.Save("C:\\soms.png");
				/*var prev = GetSection("Preview");
				if (prev != null) {
					var name = DetermineMapName(this.EngineType);
					var size = GetSection("Preview").ReadString("Size").Split(',');
					var previewSize = new Rectangle(int.Parse(size[0]), int.Parse(size[1]), int.Parse(size[2]), int.Parse(size[3]));
					
					File.AppendAllText("C:\\thumbs\\map_preview_dimensions.txt",
										string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\n", name,
										previewSize.Width, previewSize.Height, LocalSize.Width, LocalSize.Height, FullSize.Width, FullSize.Height));
				}*/
			}

			Logger.Info("Saving map");
			this.Save(Program.Settings.InputFile);
		}

		/// <summary>Gets the determine map name. </summary>
		/// <returns>The filename to save the map as</returns>
		public string DetermineMapName() {
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(FileName);

			IniSection basic = GetSection("Basic");
			if (basic.ReadBool("Official") == false)
				return StripPlayersFromName(basic.ReadString("Name", fileNameWithoutExtension));

			string mapExt = Path.GetExtension(Program.Settings.InputFile);
			string missionName = "";
			string mapName = "";
			PktFile.PktMapEntry pktMapEntry = null;
			MissionsFile.MissionEntry missionEntry = null;

			// campaign mission
			if (!basic.ReadBool("MultiplayerOnly") && basic.ReadBool("Official")) {
				string missionsFile;
				switch (Engine) {
					case EngineType.TiberianSun:
					case EngineType.RedAlert2:
						missionsFile = "mission.ini";
						break;
					case EngineType.Firestorm:
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
				if (missionEntry != null)
					missionName = (Engine >= EngineType.RedAlert2) ? missionEntry.UIName : missionEntry.Name;
			}

			else {
				// multiplayer map
				string pktEntryName = fileNameWithoutExtension;
				PktFile pkt = null;

				if (FormatHelper.MixArchiveExtensions.Contains(mapExt)) {
					// this is an 'official' map 'archive' containing a PKT file with its name
					try {
						var mix = new MixFile(File.Open(Program.Settings.InputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
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
					switch (Engine) {
						case EngineType.TiberianSun:
						case EngineType.RedAlert2:
							pkt = VFS.Open<PktFile>("missions.pkt");
							break;
						case EngineType.Firestorm:
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
				if (mapExt == ".map" && pkt != null && !pkt.MapEntries.ContainsKey(pktEntryName) && Engine >= EngineType.RedAlert2) {
					VFS.GetInstance().ScanMixDir(Program.Settings.MixFilesDirectory, EngineType.YurisRevenge);
					pkt = VFS.Open<PktFile>("missionsmd.pkt");
				}

				if (pkt != null && !string.IsNullOrEmpty(pktEntryName))
					pktMapEntry = pkt.GetMapEntry(pktEntryName);
			}

			// now, if we have a map entry from a PKT file, 
			// for TS we are done, but for RA2 we need to look in the CSV file for the translated mapname
			if (Engine <= EngineType.Firestorm) {
				if (pktMapEntry != null)
					mapName = pktMapEntry.Description;
				else if (missionEntry != null) {
					if (Engine == EngineType.TiberianSun) {
						string campaignSide;
						string missionNumber;

						if (missionEntry.Briefing.Length >= 3) {
							campaignSide = missionEntry.Briefing.Substring(0, 3);
							missionNumber = missionEntry.Briefing.Length > 3 ? missionEntry.Briefing.Substring(3) : "";
							missionName = "";
							mapName = string.Format("{0} {1} - {2}", campaignSide, missionNumber.TrimEnd('A').PadLeft(2, '0'), missionName);
						}
						else if (missionEntry.Name.Length >= 10) {
							mapName = missionEntry.Name;
						}
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

				string csfFile = Engine == EngineType.YurisRevenge ? "ra2md.csf" : "ra2.csf";
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
			else if (mapName.IndexOf(" [") != -1)
				mapName = mapName.Substring(0, mapName.IndexOf(" ["));
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

		internal void DebugDrawTile(MapTile tile) {
			_theater.GetTileCollection().DrawTile(tile, _drawingSurface);
			foreach (GameObject o in GetObjectsAt(tile.Dx, tile.Dy / 2))
				_theater.Draw(o, _drawingSurface);
		}


		internal List<GameObject> GetObjectsAt(int dx, int dy) {
			var tile = _tiles[dx, dy];
			var ret = new List<GameObject>();
			ret.AddRange(tile.AllObjects.OfType<SmudgeObject>());
			ret.AddRange(tile.AllObjects.OfType<OverlayObject>().Where(o => o.Drawable == null || !o.Drawable.Overrides));
			ret.AddRange(tile.AllObjects.OfType<TerrainObject>());
			ret.AddRange(tile.AllObjects.OfType<InfantryObject>());
			ret.AddRange(tile.AllObjects.OfType<UnitObject>());
			ret.AddRange(tile.AllObjects.OfType<StructureObject>());
			ret.AddRange(tile.AllObjects.OfType<AircraftObject>());
			ret.AddRange(tile.AllObjects.OfType<OverlayObject>().Where(o => o.Drawable != null && o.Drawable.Overrides));
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
			get { return Engine == EngineType.RedAlert2 || Engine == EngineType.YurisRevenge ? 60 : 48; }
		}

		public int TileHeight {
			get { return Engine == EngineType.RedAlert2 || Engine == EngineType.YurisRevenge ? 30 : 24; }
		}

		internal void FreeUseless() {
			_rules.Dispose();
			_art.Dispose();
			_countryColors.Clear();
			_namedColors.Clear();
			_lightSources.Clear();
			_palettePerLevel.Clear();
			_palettesToBeRecalculated.Clear();
		}

	}
}
