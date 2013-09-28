using System;
using System.Drawing;
using CNCMaps.FileFormats;
using CNCMaps.Map;
using CNCMaps.Rendering;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.Game {
	public class Theater {
		readonly string _theaterType;
		readonly EngineType _engine;
		readonly IniFile _rules;
		readonly IniFile _art;

		ObjectCollection _infantryTypes;
		ObjectCollection _vehicleTypes;
		ObjectCollection _aircraftTypes;
		ObjectCollection _buildingTypes;
		ObjectCollection _overlayTypes;
		ObjectCollection _terrainTypes;
		ObjectCollection _smudgeTypes;
		ObjectCollection _animations;
		TileCollection _tileTypes;
		PaletteCollection _palettes;

		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public Theater(string theaterType, EngineType engine, IniFile rules, IniFile art) {
			this._theaterType = theaterType;
			this._engine = engine;
			this._rules = rules;
			this._art = art;
		}

		public Theater(string theaterType, EngineType engine) {
			this._theaterType = theaterType;
			this._engine = engine;
			if (engine == EngineType.RedAlert2 || engine == EngineType.TiberianSun) {
				_rules = VFS.Open<IniFile>("rules.ini") ;
				_art = VFS.Open<IniFile>("art.ini");
			}
			else if (engine == EngineType.YurisRevenge) {
				_rules = VFS.Open<IniFile>("rulesmd.ini");
				_art = VFS.Open<IniFile>("artmd.ini");
			}
			else if (engine == EngineType.Firestorm) {
				_rules = VFS.Open<IniFile>("rules.ini");
				var fsRules = VFS.Open<IniFile>("firestrm.ini");
				Logger.Info("Merging Firestorm rules with TS rules");
				_rules.MergeWith(fsRules);
				_art = VFS.Open<IniFile>("artmd.ini");
			}
		}

		public bool Initialize() {
			Logger.Info("Initializing theater of type {0}", _theaterType);

			if (!ModConfig.SetActiveTheater(_theaterType))
				return false;

			// load palettes and additional mix files for this theater
			_palettes = new PaletteCollection();
			_palettes.IsoPalette = new Palette(VFS.Open<PalFile>(ModConfig.ActiveTheater.IsoPaletteName));
			_palettes.OvlPalette = new Palette(VFS.Open<PalFile>(ModConfig.ActiveTheater.OverlayPaletteName));
			_palettes.UnitPalette = new Palette(VFS.Open<PalFile>(ModConfig.ActiveTheater.UnitPaletteName));

			foreach (string mix in ModConfig.ActiveTheater.Mixes)
				VFS.Add(mix, CacheMethod.Cache); // we wish for these to be cached as they're gonna be hit often

			_palettes.AnimPalette = new Palette(VFS.Open<PalFile>("anim.pal"));

			_tileTypes = new TileCollection(ModConfig.ActiveTheater);
			_buildingTypes = new ObjectCollection(_rules.GetSection("BuildingTypes"),
				CollectionType.Building, _theaterType, _engine, _rules, _art, _palettes);

			_aircraftTypes = new ObjectCollection(_rules.GetSection("AircraftTypes"),
				CollectionType.Aircraft, _theaterType, _engine, _rules, _art, _palettes);

			_infantryTypes = new ObjectCollection(_rules.GetSection("InfantryTypes"),
				CollectionType.Infantry, _theaterType, _engine, _rules, _art, _palettes);

			_overlayTypes = new ObjectCollection(_rules.GetSection("OverlayTypes"),
				CollectionType.Overlay, _theaterType, _engine, _rules, _art, _palettes);

			_terrainTypes = new ObjectCollection(_rules.GetSection("TerrainTypes"),
				CollectionType.Terrain, _theaterType, _engine, _rules, _art, _palettes);

			_smudgeTypes = new ObjectCollection(_rules.GetSection("SmudgeTypes"),
				CollectionType.Smudge, _theaterType, _engine, _rules, _art, _palettes);

			_vehicleTypes = new ObjectCollection(_rules.GetSection("VehicleTypes"),
				CollectionType.Vehicle, _theaterType, _engine, _rules, _art, _palettes);

			_animations = new ObjectCollection(_rules.GetSection("Animations"),
				CollectionType.Animation, _theaterType, _engine, _rules, _art, _palettes);

			_tileTypes.InitTilesets();
			_tileTypes.InitAnimations(_animations);

			return true;
		}
		
		internal TileCollection GetTileCollection() {
			return _tileTypes;
		}

		internal PaletteCollection GetPalettes() {
			return _palettes;
		}

		internal Palette GetPalette(Drawable drawable) {
			if (drawable.PaletteType == PaletteType.Custom)
				return _palettes.GetCustomPalette(drawable.CustomPaletteName);
			else
				return _palettes.GetPalette(drawable.PaletteType);
		}

		public ObjectCollection GetObjectCollection(GameObject o) {
			if (o is InfantryObject) return _infantryTypes;
			else if (o is UnitObject) return _vehicleTypes;
			else if (o is AircraftObject) return _aircraftTypes;
			else if (o is StructureObject) {
				if (_buildingTypes.HasObject(o))
					return _buildingTypes;
				else
					return _overlayTypes;
			}
			else if (o is OverlayObject) return _overlayTypes;
			else if (o is TerrainObject) return _terrainTypes;
			else if (o is SmudgeObject) return _smudgeTypes;
			else if (o is AnimationObject) return _animations;
			else return null;
		}

		internal Size GetFoundation(NamedObject v) {
			if (_buildingTypes.HasObject(v))
				return _buildingTypes.GetDrawable(v).Foundation;
			else
				return _overlayTypes.GetDrawable(v).Foundation;
		}

		internal ObjectCollection GetCollection(CollectionType t) {
			switch (t) {
				case CollectionType.Aircraft:
					return _aircraftTypes;
				case CollectionType.Building:
					return _buildingTypes;
				case CollectionType.Infantry:
					return _infantryTypes;
				case CollectionType.Overlay:
					return _overlayTypes;
				case CollectionType.Smudge:
					return _smudgeTypes;
				case CollectionType.Terrain:
					return _terrainTypes;
				case CollectionType.Vehicle:
					return _vehicleTypes;
				default:
					throw new ArgumentOutOfRangeException("t");
			}
		}


		internal void Draw(GameObject obj, DrawingSurface ds) {
			Logger.Trace("Drawing object {0} @ {1}", obj, obj.Tile);

			if (obj is MapTile)
				_tileTypes.DrawTile(obj as MapTile, ds);
			else {
				obj.Drawable.Draw(obj, ds);
				// only structure objects should also draw their powerups
				if (!(obj is StructureObject)) return;

				var strObj = obj as StructureObject;
				if (!strObj.Upgrade1.Equals("None", StringComparison.InvariantCultureIgnoreCase) && obj.Drawable.PowerupSlots.Count >= 1) {
					var powerup = _buildingTypes.GetDrawable(strObj.Upgrade1);
					obj.Drawable.DrawPowerup(obj, powerup, 0, ds);
				}

				if (!strObj.Upgrade2.Equals("None", StringComparison.InvariantCultureIgnoreCase) && obj.Drawable.PowerupSlots.Count >= 2) {
					var powerup = _buildingTypes.GetDrawable(strObj.Upgrade2);
					obj.Drawable.DrawPowerup(obj, powerup, 1, ds);
				}

				if (!strObj.Upgrade3.Equals("None", StringComparison.InvariantCultureIgnoreCase) && obj.Drawable.PowerupSlots.Count >= 3) {
					var powerup = _buildingTypes.GetDrawable(strObj.Upgrade3);
					obj.Drawable.DrawPowerup(obj, powerup, 2, ds);
				}
			}
		}
	}
}