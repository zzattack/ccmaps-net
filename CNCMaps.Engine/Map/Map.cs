using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Rendering;
using CNCMaps.Engine.Utility;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.Map;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.FileFormats.Encodings;
using CNCMaps.Shared;
using NLog;

namespace CNCMaps.Engine.Map {
	public class Map {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		public EngineType Engine { get; private set; }
		public TheaterType TheaterType { get; private set; }
		public bool IgnoreLighting { get; set; }
		public StartPositionMarking StartPosMarking { get; set; }
		public double StartMarkerSize;
		public bool MarkOreFields { get; set; }

		public Rectangle FullSize { get; private set; }
		public Rectangle LocalSize { get; private set; }
		private VirtualFileSystem _vfs;
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
		private MapFile _mapFile;

		private bool _overlaysAltered;

		public bool Initialize(MapFile mf, EngineType et, VirtualFileSystem vfs, List<string> customRulesININames, List<string> customArtININames) {
			if (et == EngineType.AutoDetect) {
				Logger.Fatal("Engine type needs to be known by now!");
				return false;
			}
			this._mapFile = mf;
			Engine = et;
			_vfs = vfs;
			TheaterType = Theater.TheaterTypeFromString(mf.ReadString("Map", "Theater"));
			FullSize = mf.FullSize;
			LocalSize = mf.LocalSize;

			_tiles = new TileLayer(FullSize.Size);

			LoadAllObjects(mf);

			if (!IgnoreLighting) {
				_lighting = mf.Lighting;
				if (ModConfig.ActiveConfig.ExtraOptions.FirstOrDefault() != null) {
					double ambient = 0;
					double red = 0;
					double green = 0;
					double blue = 0;
					string argb = ModConfig.ActiveConfig.ExtraOptions.FirstOrDefault().LightingAmbientRGBDelta;
					string[] ambientParts = argb.Split(',');
					if (ambientParts.Length > 0 && ambientParts[0] != null)
						double.TryParse(ambientParts[0], NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-US"), out ambient);
					if (ambientParts.Length > 1 && ambientParts[1] != null)
						double.TryParse(ambientParts[1], NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-US"), out red);
					if (ambientParts.Length > 2 && ambientParts[2] != null)
						double.TryParse(ambientParts[2], NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-US"), out green);
					if (ambientParts.Length > 3 && ambientParts[3] != null)
						double.TryParse(ambientParts[3], NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-US"), out blue);
					if (ambient <= 1 && ambient >= -1) {
						_lighting.Ambient += ambient;
						_lighting.Ambient = _lighting.Ambient < 0 ? 0 : _lighting.Ambient;
					}
					if (red <= 1 && red >= -1) {
						_lighting.Red += red;
						_lighting.Red = _lighting.Red < 0 ? 0 : _lighting.Red;
					}
					if (green <= 1 && green >= -1) {
						_lighting.Green += green;
						_lighting.Green = _lighting.Green < 0 ? 0 : _lighting.Green;
					}
					if (blue <= 1 && blue >= -1) {
						_lighting.Blue += blue;
						_lighting.Blue = _lighting.Blue < 0 ? 0 : _lighting.Blue;
					}
				}
			}
			else
				_lighting = new Lighting();

			_wayPoints.AddRange(mf.Waypoints);

			if (!LoadInis(customRulesININames, customArtININames)) {
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
				_tiles[iso.Dx, iso.Dy / 2] = new MapTile(iso.Dx, iso.Dy, iso.Rx, iso.Ry, iso.Z, iso.TileNum, iso.SubTile, iso.IceGrowth, _tiles);

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

		public bool LoadInis(List<string> customRulesIniFiles, List<string> customArtIniFiles) {

			if (customRulesIniFiles.Count < 1) {
				if (Engine == EngineType.YurisRevenge) {
					_rules = _vfs.Open<IniFile>("rulesmd.ini");
				}
				else if (Engine == EngineType.Firestorm) {
					_rules = _vfs.Open<IniFile>("rules.ini");
					Logger.Info("Merging Firestorm rules with TS rules");
					_rules.MergeWith(_vfs.Open<IniFile>("firestrm.ini"));
				}
				else {
					_rules = _vfs.Open<IniFile>("rules.ini");
				}
			}
			else {
				_rules = LoadCustomInis(customRulesIniFiles);

			}

			if (customArtIniFiles.Count < 1) {
				if (Engine == EngineType.YurisRevenge) {
					_art = _vfs.Open<IniFile>("artmd.ini");
				}
				else if (Engine == EngineType.Firestorm) {
					_art = _vfs.Open<IniFile>("art.ini");
					Logger.Info("Merging Firestorm art with TS art");
					_art.MergeWith(_vfs.Open<IniFile>("artfs.ini"));
				}
				else {
					_art = _vfs.Open<IniFile>("art.ini");
				}
			}
			else {
				_art = LoadCustomInis(customArtIniFiles);
			}

			if (_rules == null || _art == null) {
				Logger.Fatal("Rules or art config file could not be loaded! You cannot render a YR/FS map" +
							" without the expansion installed");
				return false;
			}
			return true;
		}

		private IniFile LoadCustomInis(List<string> fileNames) {
			IniFile ini = _vfs.Open<IniFile>(fileNames[0]);
			for (int i = 1; i < fileNames.Count; i++) {
				Logger.Info("Merging " + fileNames[i] + " with " + fileNames[0]);
				ini.MergeWith(_vfs.Open<IniFile>(fileNames[i]));
			}
			return ini;
		}

		// between LoadMap and LoadTheater, the VFS should be initialized
		public bool LoadTheater() {
			Drawable.TileWidth = (ushort)TileWidth;
			Drawable.TileHeight = (ushort)TileHeight;

			_theater = new Theater(TheaterType, Engine, _vfs, _rules, _art);
			if (!_theater.Initialize())
				return false;

			// needs to be done before drawables are set
			bool disableOreRandomizing = false;
			if (ModConfig.ActiveConfig.ExtraOptions.FirstOrDefault() != null)
				disableOreRandomizing = ModConfig.ActiveConfig.ExtraOptions.FirstOrDefault().DisableOreRandomization;
			if (!disableOreRandomizing)
				Operations.RecalculateOreSpread(_overlayObjects, Engine);

			RemoveUnknownObjects();
			SetDrawables();

			LoadColors();
			if (Engine >= EngineType.RedAlert2)
				LoadCountries();
			LoadHouses();

			Operations.FixTiles(_tiles, _theater.GetTileCollection());
			if (Engine <= EngineType.Firestorm)
				Operations.RecalculateVeinsSpread(_overlayObjects, _tiles);

			RevisitWallBuildings();

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

		// For walls given as buildings in the map instead of overlays, recalculate its SHP frame 
		// so that those connect with its adjacent objects
		private void RevisitWallBuildings() {
			var generalSection = _rules.GetSection("General");
			string EWGate1 = generalSection.ReadString("GDIGateOne","");
			string NSGate1 = generalSection.ReadString("GDIGateTwo","");
			string EWGate2 = generalSection.ReadString("NodGateOne","");
			string NSGate2 = generalSection.ReadString("NodGateTwo","");
			string WallTower = generalSection.ReadString("WallTower","");

			foreach (StructureObject obj in _structureObjects) {
				if (obj.Drawable.IsActualWall) {
					int frame = 0;
					MapTile t = _tiles.GetTileR(obj.Tile.Rx, obj.Tile.Ry);
					var ne = t.Layer.GetNeighbourTile(t, TileLayer.TileDirection.TopRight);
					var se = t.Layer.GetNeighbourTile(t, TileLayer.TileDirection.BottomRight);
					var sw = t.Layer.GetNeighbourTile(t, TileLayer.TileDirection.BottomLeft);
					var nw = t.Layer.GetNeighbourTile(t, TileLayer.TileDirection.TopLeft);

					if (ne != null && (ne.AllObjects.OfType<StructureObject>().Any(o => o.Drawable != null &&
						 ((o.Drawable.IsActualWall && obj.Name == o.Name) || o.Name == WallTower))
						|| (ne.AllObjects.OfType<OverlayObject>().Any(o => o.Drawable != null && o.Drawable.Name == obj.Name))))
						frame |= 1;
					if (se != null && (se.AllObjects.OfType<StructureObject>().Any(o => o.Drawable != null &&
						 ((o.Drawable.IsActualWall && obj.Name == o.Name) || o.Drawable.IsGate || o.Name == WallTower))
						|| (se.AllObjects.OfType<OverlayObject>().Any(o => o.Drawable != null && o.Drawable.Name == obj.Name))))
						frame |= 2;
					if (sw != null && (sw.AllObjects.OfType<StructureObject>().Any(o => o.Drawable != null &&
						 ((o.Drawable.IsActualWall && obj.Name == o.Name) || o.Drawable.IsGate || o.Name == WallTower))
						|| (sw.AllObjects.OfType<OverlayObject>().Any(o => o.Drawable != null && o.Drawable.Name == obj.Name))))
						frame |= 4;
					if (nw != null && (nw.AllObjects.OfType<StructureObject>().Any(o => o.Drawable != null &&
						 ((o.Drawable.IsActualWall && obj.Name == o.Name) || o.Name == WallTower))
						|| (nw.AllObjects.OfType<OverlayObject>().Any(o => o.Drawable != null && o.Drawable.Name == obj.Name))))
						frame |= 8;

					if (ne != null) {
						var ne2 = ne.Layer.GetNeighbourTile(ne, TileLayer.TileDirection.TopRight);
						if (ne2 != null) {
							var ne3 = ne2.Layer.GetNeighbourTile(ne2, TileLayer.TileDirection.TopRight);
							if (ne3 != null && ne3.AllObjects.OfType<StructureObject>().Any(o => o.Drawable != null &&
								o.Drawable.IsGate && (o.Name == NSGate1 || o.Name == NSGate2)))
								frame |= 1;
						}
					}
					if (nw != null) {
						var nw2 = nw.Layer.GetNeighbourTile(nw, TileLayer.TileDirection.TopLeft);
						if (nw2 != null) {
							var nw3 = nw2.Layer.GetNeighbourTile(nw2, TileLayer.TileDirection.TopLeft);
							if (nw3 != null && nw3.AllObjects.OfType<StructureObject>().Any(o => o.Drawable != null &&
								o.Drawable.IsGate && (o.Name == EWGate1 || o.Name == EWGate2)))
								frame |= 8;
						}
					}
					obj.WallBuildingFrame = frame;
				}
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

			// Overlay check
			_overlaysAltered = false;
			c = _theater.GetCollection(CollectionType.Overlay);
			foreach (OverlayObject obj in _overlayObjects.ToList()) {
				if (!c.HasObject(obj)) {
					obj.Tile.RemoveObject(obj);
					_overlayObjects.Remove(obj);
					_overlaysAltered = true;
				}
				else {
					ShpDrawable drawable = (ShpDrawable)c.GetDrawable(obj);
					if (drawable.Shp == null) {
						obj.Tile.RemoveObject(obj);
						_overlayObjects.Remove(obj);
						_overlaysAltered = true;
					}
					else {
						drawable.Shp.Initialize();
						if ((drawable.Shp.NumImages - 1) < obj.OverlayValue) {
							obj.Tile.RemoveObject(obj);
							_overlayObjects.Remove(obj);
							_overlaysAltered = true;
						}
					}
				}
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

				// TODO: Tunnel top for TS. Attempt : Anim is read from the file but its anim offsets are incorrect
				/*
				var tileCol =  _theater.GetTileCollection();
				var tDrawable = tileCol.GetDrawable(tile) as TileDrawable;
				var tileSetEntry = tDrawable.GetTileSetEntry();

				if (tileSetEntry != null && tileSetEntry.AnimationSubtile == tile.SubTile && (tileSetEntry.MemberOfSet.TileSetNum == tileCol.DirtTrackTunnels || 
					tileSetEntry.MemberOfSet.TileSetNum == tileCol.DirtTunnels || tileSetEntry.MemberOfSet.TileSetNum == tileCol.TrackTunnels ||
					tileSetEntry.MemberOfSet.TileSetNum == tileCol.Tunnels)) {
					var tunnelTopDrawable = (AnimDrawable)tileSetEntry.AnimationDrawable;
					if (tunnelTopDrawable.Shp == null) {
						tunnelTopDrawable.Shp = VFS.Open<ShpFile>(tunnelTopDrawable.GetFilename());
						if(tunnelTopDrawable.Shp != null)
							tunnelTopDrawable.Shp.Initialize();
					}
					var tunnelAnim = new AnimationObject(tileSetEntry.AnimationDrawable.Name, tunnelTopDrawable);
					tile.AddObject(tunnelAnim);
					// _tileAnimObjects.Add(tunnelAnim);
				}
				*/

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

			// Original TS needs tiberium remapped
			bool disableTibRemapping = false;
			if (ModConfig.ActiveConfig.ExtraOptions.FirstOrDefault() != null)
				disableTibRemapping = ModConfig.ActiveConfig.ExtraOptions.FirstOrDefault().DisableTibRemap;
			if (Engine <= EngineType.Firestorm && !disableTibRemapping) {
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

		public unsafe void MarkIceGrowth() {
			Logger.Info("Marking ice growth cells");
			foreach (var tile in _tiles.Where(t => t != null).ToList()) {
				var t = _mapFile.Tiles.GetTileR(tile.Rx, tile.Ry);

				try {
					if (t != null && tile != null && t.IceGrowth > 0) {
						int destX = tile.Dx * TileWidth / 2;
						int destY = (tile.Dy - tile.Z) * TileHeight / 2;
						bool vert = FullSize.Height * 2 > FullSize.Width;

						int radius;
						if (vert)
							radius = FullSize.Height * TileHeight / 2 / 144 / 3;
						else
							radius = FullSize.Width * TileWidth / 2 / 133 / 3;

						int h = radius, w = radius;
						for (int drawY = destY - h / 2; drawY < destY + h; drawY++) {
							for (int drawX = destX - w / 2; drawX < destX + w; drawX++) {
								byte* p = (byte*)_drawingSurface.BitmapData.Scan0 + drawY * _drawingSurface.BitmapData.Stride + 3 * drawX;
								*p++ = 0x88;
								*p++ = 0xFF;
								*p++ = 0x00;
							}
						}
					}
				}
				catch (FormatException) { }
				catch (IndexOutOfRangeException) { }
			}
		}

		public void MarkTiledStartPositions() {
			var red = Palette.MakePalette(Color.Red);
			int markSize = (int)StartMarkerSize;
			int delta1, delta2;
			switch (markSize) {
				case 2:
					delta1 = -1; delta2 = 1;
					break;
				case 3:
					delta1 = -1; delta2 = 2;
					break;
				case 4:
					delta1 = -1; delta2 = 3;
					break;
				case 5:
					delta1 = -2; delta2 = 3;
					break;
				case 6:
					delta1 = -2; delta2 = 4;
					break;
				default:
					delta1 = -1; delta2 = 3;
					break;
			}
			foreach (var w in _wayPoints.Where(w => w.Number < 8)) {
				for (int x = w.Tile.Rx + delta1; x < w.Tile.Rx + delta2; x++) {
					for (int y = w.Tile.Ry + delta1; y < w.Tile.Ry + delta2; y++) {
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

		public void RedrawTiledStartPositions(bool reset = false) {
			foreach (var w in _wayPoints.Where(w => (w.Tile != null && w.Number < 8))) {
				// Redraw the cells around start pos with original palette if needed;
				// first the tiles, then the objects
				for (int x = w.Tile.Rx - 5; x < w.Tile.Rx + 5; x++) {
					for (int y = w.Tile.Ry - 5; y < w.Tile.Ry + 5; y++) {
						MapTile t = _tiles.GetTileR(x, y);
						if (t == null) continue;
						if (reset) t.Palette = _palettePerLevel[t.Z];
						// redraw tile
						_theater.Draw(t, _drawingSurface);
					}
				}
				for (int x = w.Tile.Rx - 7; x < w.Tile.Rx + 7; x++) {
					for (int y = w.Tile.Ry - 7; y < w.Tile.Ry + 7; y++) {
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

		private void DrawStartMarkersBittah(Graphics gfx, Rectangle fullImage, Rectangle previewImage) {
			foreach (var w in _wayPoints.Where(w => w.Number < 8)) {
				var t = _tiles.GetTile(w.Tile);
				var center = new Point(t.Dx * Drawable.TileWidth / 2, (t.Dy - t.Z) * Drawable.TileHeight / 2);
				// project to preview dimensions
				double pctFullX = (center.X - fullImage.Left) / (double)fullImage.Width;
				double pctFullY = (center.Y - fullImage.Top) / (double)fullImage.Height;
				Point dest = new Point((int)(pctFullX * previewImage.Width), (int)(pctFullY * previewImage.Height));
				var img = Resources.ResourceManager.GetObject("bittah_marker_" + (w.Number + 1)) as Image;
				if (img != null) {
					// center marker img
					dest.Offset(-img.Width / 2, -img.Height / 2);
					// draw it
					gfx.DrawImage(img, dest);
				}
			}
		}

		private void DrawStartMarkersAro(Graphics gfx, Rectangle fullImage, Rectangle previewImage) {
			foreach (var w in _wayPoints.Where(w => w.Number < 8)) {
				var t = _tiles.GetTile(w.Tile);
				var center = new Point(t.Dx * Drawable.TileWidth / 2, (t.Dy - t.Z) * Drawable.TileHeight / 2);// TileLayer.GetTilePixelCenter(w.Tile);
																											  // project to preview dimensions
				double pctFullX = (center.X - fullImage.Left) / (double)fullImage.Width;
				double pctFullY = (center.Y - fullImage.Top) / (double)fullImage.Height;
				Point dest = new Point((int)(pctFullX * previewImage.Width), (int)(pctFullY * previewImage.Height));
				var img = Resources.ResourceManager.GetObject("aro_marker_" + (w.Number + 1)) as Image;
				// center marker img
				dest.Offset(-img.Width / 2, -img.Height / 2);
				// draw it
				gfx.DrawImage(img, dest);
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
				if (!string.IsNullOrEmpty(color) && !string.IsNullOrEmpty(v.Value)) {
					if (_namedColors.ContainsKey(color))
						_countryColors[v.Value] = _namedColors[color];
					else
						_countryColors[v.Value] = _namedColors["LightGrey"];

				}
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

		public unsafe void DrawStartPositions() {
			Logger.Info("Marking start positions");
			double markerSize = StartMarkerSize;
			_drawingSurface.Unlock();
			using (Graphics g = Graphics.FromImage(_drawingSurface.Bitmap)) {
				foreach (var entry in _wayPoints) {
					if (entry.Number < 8) {
						try {
							MapTile t = _tiles.GetTile(entry.Tile);
							if (t == null) continue;
							int centerX = (t.Dx + 1) * TileWidth / 2;
							int centerY = (t.Dy - t.Z + 1) * TileHeight / 2;
							int halfWidth = (int)((double)TileWidth * (markerSize / 2.0));
							int halfHeight = (int)((double)TileHeight * (markerSize / 2.0));
							int opacity = 155 + (int)((7.2 - StartMarkerSize) * 18);
							if (opacity < 145) opacity = 145;
							if (opacity > 255) opacity = 255;

							if (StartPosMarking == StartPositionMarking.Squared || StartPosMarking == StartPositionMarking.Ellipsed ||
								StartPosMarking == StartPositionMarking.Circled) {
								int startX = centerX - halfWidth;
								int startY = centerY - halfHeight;
								int width = (int)((double)TileWidth * markerSize);
								int height = (int)((double)TileHeight * markerSize);

								if (StartPosMarking == StartPositionMarking.Ellipsed)
									g.FillEllipse(new SolidBrush(Color.FromArgb(opacity, Color.Red)), startX, startY, width, height);
								else {
									width /= 2;
									startX = centerX - halfWidth / 2;
									if (StartPosMarking == StartPositionMarking.Squared)
										g.FillRectangle(new SolidBrush(Color.FromArgb(opacity, Color.Red)), startX, startY, width, height);
									else
										g.FillEllipse(new SolidBrush(Color.FromArgb(opacity, Color.Red)), startX, startY, width, height);
								}
							}
							else if (StartPosMarking == StartPositionMarking.Diamond) {
								Point[] rhombus = new Point[] {
									new Point(centerX, centerY - halfHeight),
									new Point(centerX + halfWidth, centerY),
									new Point(centerX, centerY + halfHeight),
									new Point(centerX - halfWidth, centerY)
								};
								g.FillPolygon(new SolidBrush(Color.FromArgb(opacity, Color.Red)), rhombus);
							}
							else if (StartPosMarking == StartPositionMarking.Starred) {
								Point[] star = new Point[10];
								double angle = Math.PI / 5;
								double shorter = (halfWidth + halfHeight) / 4.0;
								double longer = shorter * 2.3;
								for (int i = 0; i < 10; i += 2) {
									star[i].X = centerX + (int)(longer * Math.Cos((i - 0.5) * angle));
									star[i].Y = centerY + (int)(longer * Math.Sin((i - 0.5) * angle));
									star[i + 1].X = centerX + (int)(shorter * Math.Cos((i + 0.5) * angle));
									star[i + 1].Y = centerY + (int)(shorter * Math.Sin((i + 0.5) * angle));
								}
								g.FillPolygon(new SolidBrush(Color.FromArgb(opacity, Color.Red)), star);
							}
						}
						catch (Exception) {
						}
					}
				}
			}
			_drawingSurface.Lock();
		}

		public int FindCutoffHeight() {
			// searches in 10 rows, starting from the bottom up, for the first fully tiled row
			int y;

#if DEBUG && FALSE
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
				case SizeMode.Local:
					return GetLocalSizePixels();
				case SizeMode.Full:
					return GetFullMapSizePixels();
				case SizeMode.Auto:
					return GetAutoSizePixels();
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
			if (ModConfig.ActiveConfig.ExtraOptions.FirstOrDefault() != null) {
				int bottomCrop = 0;
				if (int.TryParse(ModConfig.ActiveConfig.ExtraOptions.FirstOrDefault().MapLocalSizeBottomCropValue, out bottomCrop)) {
					bottomCrop = Math.Abs(bottomCrop);
					if (bottom1 > bottomCrop && bottomCrop >= 0 && bottomCrop < 17)
						bottom1 -= bottomCrop;
				}
			}
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
				markerPalettes[OverlayTibType.Ore2] = Palette.MakePalette(Color.Yellow);
				markerPalettes[OverlayTibType.Ore3] = Palette.MakePalette(Color.Yellow);
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

				double opacityBase = ((ovlType == OverlayTibType.Ore || ovlType == OverlayTibType.Ore2 || ovlType == OverlayTibType.Ore3) && Engine == EngineType.RedAlert2) ? 0.3 : 0.15;
				double opacity = Math.Max(0, 12 - o.OverlayValue) / 11.0 * 0.5 + opacityBase;
				o.Tile.Palette = Palette.Merge(o.Tile.Palette, markerPalettes[ovlType], opacity);
				o.Palette = Palette.Merge(o.Palette, markerPalettes[ovlType], opacity);
			}
		}

		public void RedrawOreAndGems() {
			var tileCollection = _theater.GetTileCollection();
			var checkFunc = new Func<OverlayObject, bool>(delegate (OverlayObject ovl) { return SpecialOverlays.GetOverlayTibType(ovl, Engine) != OverlayTibType.NotSpecial; });

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
			_drawingSurface = new DrawingSurface(FullSize.Width * TileWidth, FullSize.Height * TileHeight, PixelFormat.Format24bppRgb);

#if SORT
			Logger.Info("Sorting objects map");
			var sorter = new ObjectSorter(_theater, _tiles);
			var orderedObjs = sorter.GetOrderedObjects().ToList();

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
#else
			double lastReported = 0.0;
			for (int y = 0; y < FullSize.Height; y++) {
				Logger.Trace("Drawing tiles row {0}", y);
				for (int x = FullSize.Width * 2 - 2; x >= 0; x -= 2)
					_theater.Draw(_tiles.GetTile(x, y), _drawingSurface);
				for (int x = FullSize.Width * 2 - 3; x >= 0; x -= 2)
					_theater.Draw(_tiles.GetTile(x, y), _drawingSurface);

				double pct = 50.0 * y / FullSize.Height;
				if (pct > lastReported + 5) {
					Logger.Info("Drawing tiles... {0}%", Math.Round(pct, 0));
					lastReported = pct;
				}
			}
			Logger.Info("Tiles drawn");

			for (int y = 0; y < FullSize.Height; y++) {
				Logger.Trace("Drawing objects row {0}", y);
				for (int x = FullSize.Width * 2 - 2; x >= 0; x -= 2)
					foreach (GameObject o in GetObjectsAt(x, y))
						_theater.Draw(o, _drawingSurface);

				for (int x = FullSize.Width * 2 - 3; x >= 0; x -= 2)
					foreach (GameObject o in GetObjectsAt(x, y))
						_theater.Draw(o, _drawingSurface);

				double pct = 50 + 50.0 * y / FullSize.Height;
				if (pct > lastReported + 5) {
					Logger.Info("Drawing objects... {0}%", Math.Round(pct, 0));
					lastReported = pct;
				}
			}
#endif


#if DEBUG && FALSE
			// test that my bounds make some kind of sense
			_drawingSurface.Unlock();
			using (Graphics gfx = Graphics.FromImage(_drawingSurface.Bitmap)) {
				foreach (var obj in _tiles.SelectMany(t=>t.AllObjects))
					if (obj.Drawable != null)
						obj.Drawable.DrawBoundingBox(obj, gfx);
			}
#endif

			Logger.Info("Map drawing completed");
		}

		public void GeneratePreviewPack(PreviewMarkersType previewMarkers, SizeMode sizeMode, IniFile map, bool fixDimensions) {
			Logger.Info("Generating PreviewPack data");

			// we will have to re-lock the BitmapData
			_drawingSurface.Lock(_drawingSurface.Bitmap.PixelFormat);
			if (MarkOreFields == false) {
				Logger.Trace("Marking ore and gems areas");
				MarkOreAndGems();
				Logger.Debug("Redrawing ore and gems areas");
				RedrawOreAndGems();
			}

			switch (previewMarkers) {
				case PreviewMarkersType.None:
					RedrawTiledStartPositions(true);
					break;
				case PreviewMarkersType.SelectedAsAbove:
					if (StartPosMarking == StartPositionMarking.Tiled) {
						RedrawTiledStartPositions(true);
						MarkTiledStartPositions();
						RedrawTiledStartPositions(false);
					}
					else if (StartPosMarking == StartPositionMarking.Squared || StartPosMarking == StartPositionMarking.Ellipsed ||
						StartPosMarking == StartPositionMarking.Diamond || StartPosMarking == StartPositionMarking.Circled || StartPosMarking == StartPositionMarking.Starred) {
						RedrawTiledStartPositions(true);
						DrawStartPositions();
					}
					break;
				case PreviewMarkersType.Bittah:
				case PreviewMarkersType.Aro:
					RedrawTiledStartPositions(true);
					// is being injected later
					break;
			}
			_drawingSurface.Unlock();

			// Number magic explained: http://modenc.renegadeprojects.com/Maps/PreviewPack
			int pw, ph;
			switch (Engine) {
				case EngineType.TiberianSun:
					pw = (int)Math.Ceiling((fixDimensions ? 1.975 : 2.000) * FullSize.Width);
					ph = (int)Math.Ceiling((fixDimensions ? 0.995 : 1.000) * FullSize.Height);
					break;
				case EngineType.Firestorm:
					pw = (int)Math.Ceiling((fixDimensions ? 1.975 : 2.000) * FullSize.Width);
					ph = (int)Math.Ceiling((fixDimensions ? 0.995 : 1.000) * FullSize.Height);
					break;
				case EngineType.RedAlert2:
					pw = (int)Math.Ceiling((fixDimensions ? 1.975 : 2.000) * FullSize.Width);
					ph = (int)Math.Ceiling((fixDimensions ? 0.995 : 1.000) * FullSize.Height);
					break;
				case EngineType.YurisRevenge:
					pw = (int)Math.Ceiling((fixDimensions ? 1.975 : 2.000) * LocalSize.Width);
					ph = (int)Math.Ceiling((fixDimensions ? 1.000 : 1.000) * LocalSize.Height);
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

					switch (previewMarkers) {
						case PreviewMarkersType.None:
						case PreviewMarkersType.SelectedAsAbove:
							break;
						case PreviewMarkersType.Bittah:
							DrawStartMarkersBittah(gfx, srcRect, dstRect);
							break;
						case PreviewMarkersType.Aro:
							DrawStartMarkersAro(gfx, srcRect, dstRect);
							break;
					}
				}


				Logger.Info("Injecting thumbnail into map");
				ThumbInjector.InjectThumb(preview, map);
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


		public void FixupTileLayer() {
			Logger.Info("Locating undefined tiles on map");
			var coll = _theater.GetTileCollection();
			int brokenTiles = 0;
			foreach (var tile in _tiles.Where(t => t != null).ToList()) {
				if (tile.TileNum >= coll.NumTiles) {
					Logger.Warn("Removing tile at ({0},{1}) with tilenum {2} because it is not valid in this theather's tileset", tile.Rx, tile.Ry, tile.TileNum);
					ChangeTileToClear(coll, tile);
					brokenTiles++;
					continue;
				}
				var drawable = coll.GetDrawable(tile) as TileDrawable;
				if (drawable == null) {
					Logger.Warn("Removing tile at ({0},{1}) with tilenum {2} because no definition for it was found", tile.Rx, tile.Ry, tile.TileNum);
					ChangeTileToClear(coll, tile);
					brokenTiles++;
					continue;
				}

				var tmp = drawable.GetTileFile(tile);
				if (tmp == null) {
					Logger.Warn(string.Format("Removing tile #{2}@({0},{1}) because no tmp file for it was found; set {3} ({4}), expected filename {5}xx{6}",
						tile.Rx, tile.Ry, tile.TileNum, drawable.Name, drawable.TsEntry?.MemberOfSet?.SetName ?? "", drawable.TsEntry?.MemberOfSet?.FileName ?? "", ModConfig.ActiveTheater.Extension));
					brokenTiles++;
					ChangeTileToClear(coll, tile);
				}
				else {
					try {
						if (!drawable.DoesSubTileExist(tile)) {
							Logger.Warn(string.Format("Removing tile-subtile,count #{2}-{7},{8}@({0},{1}) because subtile for it was not found; set {3} ({4}), expected filename {5}xx{6}",
								tile.Rx, tile.Ry, tile.TileNum, drawable.Name, drawable.TsEntry?.MemberOfSet?.SetName ?? "", drawable.TsEntry?.MemberOfSet?.FileName ?? "",
								ModConfig.ActiveTheater.Extension, tile.SubTile, drawable.TsEntry?.GetTmpFile(tile).Images.Count));
							brokenTiles++;
							ChangeTileToClear(coll, tile);
						}
					}
					catch (Exception) { }
				}
			}
			if (brokenTiles == 0) {
				Logger.Info("No undefined/broken tiles found, not altering IsoMapPack5 section");
			}
			else {
				Logger.Info($"Fixing IsoMapPack5 section with {brokenTiles} broken tiles");
				_mapFile.Tiles.SerializeIsoMapPack5(_mapFile.GetSection("IsoMapPack5"));
			}

		}

		private void ChangeTileToClear(TileCollection coll, MapTile tile) {
			tile.TileNum = 0;
			tile.SubTile = 0;
			tile.Drawable = coll.GetDrawable(0);

			var t = _mapFile.Tiles.GetTileR(tile.Rx, tile.Ry);
			t.TileNum = 0;
			t.SubTile = 0;
		}

		public void FixupOverlays() {
			// Byte arrays of 256KB fixed size
			byte[] overlayPack = new byte[1 << 18];
			byte[] overlayDataPack = new byte[1 << 18];

			if (_overlaysAltered) {
				// Fill with no overlays
				for (int x = 0; x < 262144; x++) {
					overlayPack[x] = 255;
					overlayDataPack[x] = 0;
				}

				// Missing removed during RemoveUnknownObjects
				foreach (OverlayObject obj in _overlayObjects.ToList()) {
					int location = (obj.Tile.Ry * 512) + obj.Tile.Rx;
					overlayPack[location] = obj.OverlayID;
					overlayDataPack[location] = obj.OverlayValue;
				}

				// Encode the byte arrays into LCW/Format80 and then into Base64
				string oPackEncoded = Convert.ToBase64String(Format5.Encode(overlayPack, 80), Base64FormattingOptions.None);
				string oDataPackEncoded = Convert.ToBase64String(Format5.Encode(overlayDataPack, 80), Base64FormattingOptions.None);

				var overlayPackSection = _mapFile.GetSection("OverlayPack");
				overlayPackSection.Clear();

				// Split the string into lines of 70 chars and build the sections
				int rowNum = 1;
				for (int i = 0; i < oPackEncoded.Length; i += 70) {
					overlayPackSection.SetValue(rowNum++.ToString(CultureInfo.InvariantCulture), oPackEncoded.Substring(i, Math.Min(70, oPackEncoded.Length - i)));
				}

				var overlayDataPackSection = _mapFile.GetSection("OverlayDataPack");
				overlayDataPackSection.Clear();

				rowNum = 1;
				for (int i = 0; i < oDataPackEncoded.Length; i += 70) {
					overlayDataPackSection.SetValue(rowNum++.ToString(CultureInfo.InvariantCulture), oDataPackEncoded.Substring(i, Math.Min(70, oDataPackEncoded.Length - i)));
				}
			}
		}

		public void CompressIsoMapPack5() {
			Logger.Info("Generating compressed IsoMapPack5 section. Please wait ...");
			_mapFile.Tiles.SerializeIsoMapPack5(_mapFile.GetSection("IsoMapPack5"), true);
		}

		public void PlotTunnels(bool adjustPosition = true) {
			Logger.Info("Plotting Tunnel path");
			_drawingSurface.Unlock();
			using (Graphics g = Graphics.FromImage(_drawingSurface.Bitmap)) {
				Pen linePen = new Pen(Color.FromArgb(148,255,0,0), 3);
				Pen dashlinePen = new Pen(Color.FromArgb(180,0,255,205), 3);
				float[] dashValues = {2, 1};
				dashlinePen.DashPattern = dashValues;

				HashSet<int> endCells = new HashSet<int>();

				foreach (TunnelLine tunnelLine in _mapFile.TunnelEntries) {
					int deltaFromCenterY = 1;
					int deltaFromCenterX = 0;
					List<Point> linePoints = new List<Point>();

					MapTile startTile = _tiles.GetTileR(tunnelLine.StartX, tunnelLine.StartY);
					MapTile endTile = _tiles.GetTileR(tunnelLine.EndX, tunnelLine.EndY);

					// Current adjustment makes it look correct but the back facing tunnel coordinates are shown inaccurately
					Point startTileCenter = new Point((startTile.Dx + 1) * Drawable.TileWidth / 2, (startTile.Dy - startTile.Z + 1 - (adjustPosition ? 4 : 0)) * Drawable.TileHeight / 2);
					Point endTileCenter = new Point((endTile.Dx + 1) * Drawable.TileWidth / 2, (endTile.Dy - endTile.Z + 1 - (adjustPosition ? 4 : 0)) * Drawable.TileHeight / 2);

					endCells.Add(tunnelLine.EndX + 1000 * tunnelLine.EndY);
					if (endCells.Contains(tunnelLine.StartX + 1000 * tunnelLine.StartY)) {
						linePen.Color = Color.FromArgb(148, 255, 30, 255);
						dashlinePen.Color = Color.FromArgb(180, 0, 0, 255);
						deltaFromCenterY = 2;
					}
					else {
						linePen.Color = Color.FromArgb(148, 255, 0, 0);
						dashlinePen.Color = Color.FromArgb(180, 0, 255, 205);
						deltaFromCenterY = -2;
					}

					MapTile currentTile = startTile;
					MapTile nextTile = currentTile;
					Point currentPoint = new Point(startTileCenter.X, startTileCenter.Y + deltaFromCenterY);
					int addWidth = 0;
					int addHeight = 0;

					linePoints.Add(currentPoint);

					if (tunnelLine.Direction != null) {
						foreach (int d in tunnelLine.Direction) {
							deltaFromCenterX = 0;
							switch (d) {
								case 0:
									nextTile = currentTile.Layer.GetNeighbourTile(currentTile, TileLayer.TileDirection.TopRight);
									addWidth = Drawable.TileWidth / 2;
									addHeight = -Drawable.TileHeight / 2;
									break;
								case 1:
									nextTile = currentTile.Layer.GetNeighbourTile(currentTile, TileLayer.TileDirection.Right);
									addWidth = Drawable.TileWidth;
									addHeight = 0;
									break;
								case 2:
									nextTile = currentTile.Layer.GetNeighbourTile(currentTile, TileLayer.TileDirection.BottomRight);
									addWidth = Drawable.TileWidth / 2;
									addHeight = Drawable.TileHeight / 2;
									break;
								case 3:
									nextTile = currentTile.Layer.GetNeighbourTile(currentTile, TileLayer.TileDirection.Bottom);
									addWidth = 0;
									addHeight = Drawable.TileHeight;
									deltaFromCenterX = deltaFromCenterY - 6;
									break;
								case 4:
									nextTile = currentTile.Layer.GetNeighbourTile(currentTile, TileLayer.TileDirection.BottomLeft);
									addWidth = -Drawable.TileWidth / 2;
									addHeight = Drawable.TileHeight / 2;
									break;
								case 5:
									nextTile = currentTile.Layer.GetNeighbourTile(currentTile, TileLayer.TileDirection.Left);
									addWidth = -Drawable.TileWidth;
									addHeight = 0;
									break;
								case 6:
									nextTile = currentTile.Layer.GetNeighbourTile(currentTile, TileLayer.TileDirection.TopLeft);
									addWidth = -Drawable.TileWidth / 2;
									addHeight = -Drawable.TileHeight / 2;
									break;
								case 7:
									nextTile = currentTile.Layer.GetNeighbourTile(currentTile, TileLayer.TileDirection.Top);
									addWidth = 0;
									addHeight = -Drawable.TileHeight;
									deltaFromCenterX = deltaFromCenterY + 6;
									break;
							}
							if (nextTile != null) {
								currentTile = nextTile;
								currentPoint.X += addWidth;
								currentPoint.Y += addHeight;
								linePoints.Add(new Point(currentPoint.X + deltaFromCenterX, currentPoint.Y + deltaFromCenterY));
							}
						}
						if (linePoints.Count > 1) {
							g.DrawLines(linePen, linePoints.ToArray());
						}
					}

					if (endTile.Rx != currentTile.Rx || endTile.Ry != currentTile.Ry) {
						Point dashlineStart = new Point(currentPoint.X, currentPoint.Y + deltaFromCenterY);
						Point dashlineEnd = new Point(endTileCenter.X, endTileCenter.Y + deltaFromCenterY);
						g.DrawLine(dashlinePen, dashlineStart, dashlineEnd);
					}

					g.FillEllipse(new SolidBrush(Color.FromArgb(138, Color.Red)), startTileCenter.X - 10, startTileCenter.Y - 5, 20, 10);
					g.FillEllipse(new SolidBrush(Color.FromArgb(138, Color.Red)), endTileCenter.X - 10, endTileCenter.Y - 5, 20, 10);
				}

				linePen.Dispose();
				dashlinePen.Dispose();
			}
			_drawingSurface.Lock();
		}

	}
}