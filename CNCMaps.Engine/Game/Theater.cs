using System;
using System.Drawing;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;

namespace CNCMaps.Engine.Game {
	public class Theater {
		readonly TheaterType _theaterType;
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

		public Theater(TheaterType theaterType, EngineType engine, IniFile rules, IniFile art) {
			this._theaterType = theaterType;
			this._engine = engine;
			this._rules = rules;
			this._art = art;
		}

		public Theater(TheaterType theaterType, EngineType engine) {
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
			
			_animations = new ObjectCollection(CollectionType.Animation, _theaterType, _engine, _rules, _art,
				_rules.GetSection("Animations"), _palettes);

			_tileTypes = new TileCollection(CollectionType.Tiles, _theaterType, _engine, _rules, _art, ModConfig.ActiveTheater);

			_buildingTypes = new ObjectCollection(CollectionType.Building, _theaterType, _engine, _rules, _art,
				_rules.GetSection("BuildingTypes"), _palettes);

			_aircraftTypes = new ObjectCollection(CollectionType.Aircraft, _theaterType, _engine, _rules, _art,
				_rules.GetSection("AircraftTypes"), _palettes);

			_infantryTypes = new ObjectCollection(CollectionType.Infantry, _theaterType, _engine, _rules, _art, 
				_rules.GetSection("InfantryTypes"), _palettes);

			_overlayTypes = new ObjectCollection(CollectionType.Overlay, _theaterType, _engine, _rules, _art, 
				_rules.GetSection("OverlayTypes"), _palettes);

			_terrainTypes = new ObjectCollection(CollectionType.Terrain, _theaterType, _engine, _rules, _art, 
				_rules.GetSection("TerrainTypes"), _palettes);

			_smudgeTypes = new ObjectCollection(CollectionType.Smudge, _theaterType, _engine, _rules, _art, 
				_rules.GetSection("SmudgeTypes"), _palettes);

			_vehicleTypes = new ObjectCollection(CollectionType.Vehicle, _theaterType, _engine, _rules, _art,
				_rules.GetSection("VehicleTypes"), _palettes);

			_tileTypes.InitTilesets();
			_tileTypes.InitAnimations(_animations);

			return true;
		}

		public static TheaterType TheaterTypeFromString(string theater) {
			theater = theater.ToLower();
			if (theater == "lunar") return TheaterType.Lunar;
			else if (theater == "newurban") return TheaterType.NewUrban;
			else if (theater == "desert") return TheaterType.Desert;
			else if (theater == "temperate") return TheaterType.Temperate;
			else if (theater == "urban") return TheaterType.Urban;
			else if (theater == "snow") return TheaterType.Snow;
			else throw new InvalidOperationException();
		}

		public TileCollection GetTileCollection() {
			return _tileTypes;
		}

		internal PaletteCollection GetPalettes() {
			return _palettes;
		}

		internal Palette GetPalette(Drawable drawable) {
			if (drawable.Props.PaletteType == PaletteType.Custom)
				return _palettes.GetCustomPalette(drawable.Props.CustomPaletteName);
			else
				return _palettes.GetPalette(drawable.Props.PaletteType);
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

		internal GameCollection GetCollection(CollectionType t) {
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
				case CollectionType.Tiles:
					return _tileTypes;
				default:
					throw new ArgumentOutOfRangeException("t");
			}
		}


		internal void Draw(GameObject obj, DrawingSurface ds) {
			Logger.Trace("Drawing object {0} @ {1}", obj, obj.Tile);
			obj.Drawable.Draw(obj, ds);
		}

	}
}