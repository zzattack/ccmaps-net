using System;
using System.Drawing;
using CNCMaps.FileFormats;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.MapLogic {
	class Theater {
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
		TileCollection _tileTypes;
		PaletteCollection _palettes;

		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public Theater(string theaterName, EngineType engine) :
			this(TheaterTypeFromString(theaterName, engine), engine) { }

		public Theater(string theaterName, EngineType engine, IniFile rules, IniFile art) :
			this(TheaterTypeFromString(theaterName, engine), engine, rules, art) { }

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
				_rules = VFS.Open("rules.ini") as IniFile;
				_art = VFS.Open("art.ini") as IniFile;
			}
			else if (engine == EngineType.YurisRevenge || engine == EngineType.FireStorm) {
				_rules = VFS.Open("rulesmd.ini") as IniFile;
				_art = VFS.Open("artmd.ini") as IniFile;
			}
		}

		public void Initialize() {
			Logger.Info("Initializing theater");
			// load palettes and additional mix files for this theater
			switch (_theaterType) {
				case TheaterType.Temperate:
				case TheaterType.TemperateYR:
					_palettes.isoPalette = new Palette(VFS.Open<PalFile>("isotem.pal"));
					_palettes.libPalette = new Palette(VFS.Open<PalFile>("libtem.pal"));
					_palettes.ovlPalette = new Palette(VFS.Open<PalFile>("temperat.pal"));
					_palettes.unitPalette = new Palette(VFS.Open<PalFile>("unittem.pal"));
					break;

				case TheaterType.Snow:
				case TheaterType.SnowYR:
					_palettes.isoPalette = new Palette(VFS.Open<PalFile>("isosno.pal"));
					_palettes.libPalette = new Palette(VFS.Open<PalFile>("libsno.pal"));
					_palettes.ovlPalette = new Palette(VFS.Open<PalFile>("snow.pal"));
					_palettes.unitPalette = new Palette(VFS.Open<PalFile>("unitsno.pal"));
					break;

				case TheaterType.Urban:
				case TheaterType.UrbanYR:
					_palettes.isoPalette = new Palette(VFS.Open<PalFile>("isourb.pal"));
					_palettes.libPalette = new Palette(VFS.Open<PalFile>("liburb.pal"));
					_palettes.ovlPalette = new Palette(VFS.Open<PalFile>("urban.pal"));
					_palettes.unitPalette = new Palette(VFS.Open<PalFile>("uniturb.pal"));
					break;

				case TheaterType.Desert:
					_palettes.isoPalette = new Palette(VFS.Open<PalFile>("isodes.pal"));
					_palettes.libPalette = new Palette(VFS.Open<PalFile>("libdes.pal"));
					_palettes.ovlPalette = new Palette(VFS.Open<PalFile>("desert.pal"));
					_palettes.unitPalette = new Palette(VFS.Open<PalFile>("unitdes.pal"));
					break;

				case TheaterType.Lunar:
					_palettes.isoPalette = new Palette(VFS.Open<PalFile>("isolun.pal"));
					_palettes.libPalette = new Palette(VFS.Open<PalFile>("liblun.pal"));
					_palettes.ovlPalette = new Palette(VFS.Open<PalFile>("lunar.pal"));
					_palettes.unitPalette = new Palette(VFS.Open<PalFile>("unitlun.pal"));
					break;

				case TheaterType.NewUrban:
					_palettes.isoPalette = new Palette(VFS.Open<PalFile>("isoubn.pal"));
					_palettes.libPalette = new Palette(VFS.Open<PalFile>("libubn.pal"));
					_palettes.ovlPalette = new Palette(VFS.Open<PalFile>("urbann.pal"));
					_palettes.unitPalette = new Palette(VFS.Open<PalFile>("unitubn.pal"));
					break;
			}

			foreach (string mix in TheaterDefaults.GetTheaterMixes(_theaterType))
				VFS.Add(mix);

			_palettes.animPalette = new Palette(VFS.Open<PalFile>("anim.pal"));

			_tileTypes = new TileCollection(_theaterType);

			Drawable.Palettes = _palettes;

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
		}

		static TheaterType TheaterTypeFromString(string theater, EngineType engineType) {
			bool yr = engineType == EngineType.YurisRevenge;
			theater = theater.ToLower();
			if (theater == "lunar") return TheaterType.Lunar;
			else if (theater == "newurban") return TheaterType.NewUrban;
			else if (theater == "desert") return TheaterType.Desert;
			else if (theater == "temperate") return yr ? TheaterType.TemperateYR : TheaterType.Temperate;
			else if (theater == "urban") return yr ? TheaterType.UrbanYR : TheaterType.Urban;
			else if (theater == "snow") return yr ? TheaterType.SnowYR : TheaterType.Snow;
			else throw new InvalidOperationException();
		}

		void DrawObject(RA2Object obj, Bitmap bm) {
			if (obj is SmudgeObject) {
			}
		}

		internal TileCollection GetTileCollection() {
			return _tileTypes;
		}

		internal PaletteCollection GetPalettes() {
			return _palettes;
		}

		ObjectCollection GetObjectCollection(RA2Object o) {
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
			throw new InvalidOperationException("Invalid object");
		}

		internal void DrawObject(RA2Object o, DrawingSurface drawingSurface) {
			GetObjectCollection(o).Draw(o, drawingSurface);
		}

		internal Palette GetPalette(RA2Object o) {
			return GetObjectCollection(o).GetPalette(o);
		}

		internal Size GetFoundation(NamedObject v) {
			if (_buildingTypes.HasObject(v))
				return _buildingTypes.GetFoundation(v);
			else
				return _overlayTypes.GetFoundation(v);
		}

		internal ObjectCollection GetCollection(CollectionType t) {
			if (t == CollectionType.Overlay)
				return _overlayTypes;
			return null;
		}

		internal Size GetFoundation(OverlayObject o) {
			return _overlayTypes.GetFoundation(o);
		}
	}
}