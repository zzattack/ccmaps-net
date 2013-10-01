using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.Engine.Utility;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.Map;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;
using NLog;
using TileLayer = CNCMaps.Engine.Map.TileLayer;

namespace CNCMaps.Engine.Game {
	public class Map {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		public EngineType Engine { get; private set; }
		public TheaterType TheaterType { get; private set; }
		public bool IgnoreLighting { get; set; }
		public StartPositionMarking StartPosMarking { get; set; }
		public bool MarkOreFields { get; set; }

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
		private readonly List<Waypoint> _wayPoints = new List<Waypoint>();

		private readonly Dictionary<string, Color> _countryColors = new Dictionary<string, Color>();
		private readonly Dictionary<string, Color> _namedColors = new Dictionary<string, Color>();

		private Lighting _lighting;
		private readonly List<LightSource> _lightSources = new List<LightSource>();
		private readonly List<Palette> _palettePerLevel = new List<Palette>(19);
		private readonly HashSet<Palette> _palettesToBeRecalculated = new HashSet<Palette>();

		private DrawingSurface _drawingSurface;

		public bool Initialize(MapFile mf, EngineType et) {
			if (et == EngineType.AutoDetect) {
				Logger.Fatal("Engine type needs to be known by now!");
				return false;
			}
			Engine = et;
			TheaterType = Theater.TheaterTypeFromString(mf.ReadString("Map", "Theater"));
			FullSize = mf.FullSize;
			LocalSize = mf.LocalSize;

			_tiles = new TileLayer(FullSize.Size);

			LoadAllObjects(mf);

			if (!IgnoreLighting)
				_lighting = mf.Lighting;
			else
				_lighting = new Lighting { Level = 0.0 };

			_wayPoints.AddRange(mf.Waypoints);

			if (!LoadInis()) {
				Logger.Fatal("Ini files couldn't be loaded");
				return false;
			}

			Logger.Info("Overriding rules.ini with map INI entries");
			_rules.MergeWith(mf);

			return true;
		}

		/// <summary>Reads all objects. </summary>
		private void LoadAllObjects(MapFile mf) {

			// import tiles
			foreach (var iso in mf.Tiles)
				_tiles[iso.Dx, iso.Dy/2] = new MapTile(iso.Dx, iso.Dy, iso.Rx, iso.Ry, iso.Z, iso.TileNum, iso.SubTile, _tiles);

			// import terrain
			foreach (var terr in mf.Terrains) {
				var t = new TerrainObject(terr.Name);
				_terrainObjects.Add(t);
				_tiles.GetTile(terr.Tile).AddObject(t);
			}

			// import smudges
			foreach (var sm in mf.Smudges) {
				var s = new SmudgeObject(sm.Name);
				_tiles.GetTile(sm.Tile).AddObject(s);
				_smudgeObjects.Add(s);
			}

			// import overlays
			foreach (var o in mf.Overlays) {
				var ovl = new OverlayObject(o.OverlayID, o.OverlayValue);
				_tiles.GetTile(o.Tile).AddObject(ovl);
				_overlayObjects.Add(ovl);
			}

			// import infantry
			foreach (var i in mf.Infantries) {
				var inf = new InfantryObject(i.Owner, i.Name, i.Health, i.Direction, i.OnBridge);
				_tiles.GetTile(i.Tile).AddObject(inf);
				_infantryObjects.Add(inf);
			}

			foreach (var u in mf.Units) {
				var un = new UnitObject(u.Owner, u.Name, u.Health, u.Direction, u.OnBridge);
				_tiles.GetTile(u.Tile).AddObject(un);
				_unitObjects.Add(un);
			}

			foreach (var a in mf.Aircrafts) {
				var ac = new AircraftObject(a.Owner, a.Name, a.Health, a.Direction, a.OnBridge);
				_tiles.GetTile(a.Tile).AddObject(ac);
				_aircraftObjects.Add(ac);
			}

			foreach (var s in mf.Structures) {
				var str = new StructureObject(s.Owner, s.Name, s.Health, s.Direction);
				str.Upgrade1 = s.Upgrade1;
				str.Upgrade2 = s.Upgrade2;
				str.Upgrade3 = s.Upgrade3;
				_tiles.GetTile(s.Tile).AddObject(str);
				_structureObjects.Add(str);
			}
		}

		public bool LoadInis() {
			if (Engine == EngineType.YurisRevenge) {
				_rules = VFS.Open<IniFile>("rulesmd.ini");
				_art = VFS.Open<IniFile>("artmd.ini");
			}
			else if (Engine == EngineType.Firestorm) {
				_rules = VFS.Open<IniFile>("rules.ini");
				_art = VFS.Open<IniFile>("art.ini");

				Logger.Info("Merging Firestorm rules with TS rules");
				_rules.MergeWith(VFS.Open<IniFile>("firestrm.ini"));
				_art.MergeWith(VFS.Open<IniFile>("artfs.ini"));
			}
			else {
				_rules = VFS.Open<IniFile>("rules.ini");
				_art = VFS.Open<IniFile>("art.ini");
			}

			if (_rules == null || _art == null) {
				Logger.Fatal("Rules or art config file could not be loaded! You cannot render a YR/FS map" +
							 " without the expansion installed");
				return false;
			}
			return true;
		}

		// between LoadMap and LoadTheater, the VFS should be initialized
		public bool LoadTheater() {
			Drawable.TileWidth = (ushort)TileWidth;
			Drawable.TileHeight = (ushort)TileHeight;

			_theater = new Theater(TheaterType, Engine, _rules, _art);
			if (!_theater.Initialize())
				return false;

			// needs to be done before drawables are set
			Operations.RecalculateOreSpread(_overlayObjects, Engine);

			RemoveUnknownObjects();
			SetDrawables();

			LoadColors();
			if (Engine >= EngineType.RedAlert2)
				LoadCountries();
			LoadHouses();

			if (Engine >= EngineType.RedAlert2)
				Operations.RecalculateTileSystem(_tiles, _theater.GetTileCollection());
			else if (Engine <= EngineType.Firestorm)
				Operations.RecalculateVeinsSpread(_overlayObjects, _tiles);

			CreateLevelPalettes();
			LoadPalettes();
			ApplyRemappables();
			if (!IgnoreLighting) {
				LoadLightSources();
				ApplyLightSources();
			}

			SetBaseTiles(); // requires .AnimationDrawable set on objects

			// first preparing all palettes as above, and only now recalculating them 
			// could save a large amount of work in total
			RecalculatePalettes();

			return true;
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
				var bounds = obj.GetBounds();
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

		private void RemoveUnknownObjects() {
			var c = _theater.GetCollection(CollectionType.Terrain);
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
				tile.Drawable = _theater.GetCollection(CollectionType.Tiles).GetDrawable(tile);
				foreach (var obj in tile.AllObjects) {
					obj.Collection = _theater.GetObjectCollection(obj);
					obj.Drawable = obj.Collection.GetDrawable(obj);
				}
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
				/*var tse = _theater.GetTileCollection().GetTileSetEntry(tile);
				if (tse != null && tse.AnimationSubtile == tile.SubTile) {
					var anim = new AnimationObject(tse.AnimationDrawable.Name, tse.AnimationDrawable);
					tile.AddObject(anim);
					_animationObjects.Add(anim);
				}*/

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
						pt = obj.Drawable.Props.PaletteType;
						lt = obj.Drawable.Props.LightingType;
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
			var red = Palette.MakePalette(Color.Red);

			foreach (var w in _wayPoints.Where(w => w.Number < 8)) {
				// Draw 4x4 cell around start pos
				for (int x = w.Tile.Rx - 1; x < w.Tile.Rx + 3; x++) {
					for (int y = w.Tile.Ry - 1; y < w.Tile.Ry + 3; y++) {
						MapTile t = _tiles.GetTileR(x, y);
						if (t != null) {
							t.Palette = Palette.Merge(t.Palette, red, 0.4);
							//foreach (var o in t.AllObjects.OfType<SmudgeObject>().Cast<GameObject>().Union(t.AllObjects.OfType<OverlayObject>()))
							//	o.Palette = Palette.Merge(o.Palette, red, 0.4);
						}
					}
				}
			}
		}

		public void UndrawTiledStartPositions() {
			Palette red = Palette.MakePalette(Color.Red);

			foreach (var w in _wayPoints) {

				// Redraw the 4x4 cell around start pos with original palette;
				// first the tiles, then the objects
				for (int x = w.Tile.Rx - 2; x < w.Tile.Rx + 2; x++) {
					for (int y = w.Tile.Ry - 2; y < w.Tile.Ry + 2; y++) {
						MapTile t = _tiles.GetTileR(x, y);
						if (t == null) continue;
						t.Palette = _palettePerLevel[t.Z];
						// redraw tile
						_theater.Draw(t, _drawingSurface);
					}
				}
				for (int x = w.Tile.Rx - 5; x < w.Tile.Rx + 5; x++) {
					for (int y = w.Tile.Ry - 5; y < w.Tile.Ry + 5; y++) {
						MapTile t = _tiles.GetTileR(x, y);
						if (t == null) continue;
						// redraw objects on here
						List<GameObject> objs = GetObjectsAt(t.Dx, t.Dy / 2);
						foreach (GameObject o in objs)
							_theater.Draw(o, _drawingSurface);
					}
				}
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
			IniFile.IniSection housesSection = _rules.GetOrCreateSection("Houses");
			foreach (var v in housesSection.OrderedEntries) {
				var houseSection = _rules.GetSection(v.Value);
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

		/// <summary>Loads the countries. </summary>
		private void LoadCountries() {
			Logger.Info("Loading countries");

			var countriesSection = _rules.GetSection(Engine >= EngineType.RedAlert2 ? "Countries" : "Houses");
			foreach (var entry in countriesSection.OrderedEntries) {
				IniFile.IniSection countrySection = _rules.GetSection(entry.Value);
				if (countrySection == null) continue;
				Color c;
				if (!_namedColors.TryGetValue(countrySection.ReadString("Color"), out c))
					c = _namedColors.Values.First();
				_countryColors[entry.Value] = c;
			}
		}

		public unsafe void DrawSquaredStartPositions() {
			Logger.Info("Marking squared start positions");
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

		public Rectangle GetSizePixels(SizeMode sizeMode) {
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
						_theater.Draw(_tiles.GetTile(x, y), _drawingSurface);
				}
				for (int x = FullSize.Width * 2 - 3; x >= 0; x -= 2) {
					if (_tiles[x, y].AllObjects.OfType<OverlayObject>().Any(checkFunc))
						_theater.Draw(_tiles.GetTile(x, y), _drawingSurface);
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

		public void Draw() {
			Logger.Info("Sorting objects map");
			var sorter = new ObjectSorter(_theater, _tiles);
			var orderedObjs = sorter.GetOrderedObjects().ToList();
			_drawingSurface = new DrawingSurface(FullSize.Width * TileWidth, FullSize.Height * TileHeight, PixelFormat.Format24bppRgb);

			double lastReported = 0.0;
			Logger.Info("Drawing map... 0%");
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

		public void GeneratePreviewPack(bool omitPreviewMarkers, SizeMode sizeMode, IniFile map) {
			Logger.Info("Generating PreviewPack data");
			// we will have to re-lock the BitmapData

			_drawingSurface.Lock(_drawingSurface.Bitmap.PixelFormat);
			if (MarkOreFields == false) {
				Logger.Trace("Marking ore and gems areas");
				MarkOreAndGems();
				Logger.Debug("Redrawing ore and gems areas");
				RedrawOreAndGems();
			}
			if (StartPosMarking != StartPositionMarking.Squared) {
				// undo tiled, if needed
				if (StartPosMarking == StartPositionMarking.Tiled)
					UndrawTiledStartPositions();

				if (!omitPreviewMarkers)
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

					var srcRect = GetSizePixels(sizeMode);
					var dstRect = new Rectangle(0, 0, preview.Width, preview.Height);
					gfx.DrawImage(_drawingSurface.Bitmap, dstRect, srcRect, GraphicsUnit.Pixel);
				}

				Logger.Info("Injecting thumbnail into map");
				ThumbInjector.InjectThumb(preview, map);

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
		}

		public void DebugDrawTile(MapTile tile) {
			_theater.Draw(tile, _drawingSurface);
			foreach (GameObject o in GetObjectsAt(tile.Dx, tile.Dy / 2))
				_theater.Draw(o, _drawingSurface);
			 Operations.CountNeighbouringVeins(tile, Operations.IsVeins);
		}

		public List<GameObject> GetObjectsAt(int dx, int dy) {
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

		public void FreeUseless() {
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
