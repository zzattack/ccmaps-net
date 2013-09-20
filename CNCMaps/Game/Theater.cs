using System;
using System.Drawing;
using CNCMaps.FileFormats;
using CNCMaps.Map;
using CNCMaps.Rendering;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.Game {
	public enum TheaterType {
		Temperate, TemperateYR,
		Urban, UrbanYR,
		Snow, SnowYR,
		Lunar,
		Desert,
		NewUrban
	}

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
			else if (engine == EngineType.YurisRevenge) {
				_rules = VFS.Open("rulesmd.ini") as IniFile;
				_art = VFS.Open("artmd.ini") as IniFile;
			}
			else if (engine == EngineType.Firestorm) {
				_rules = VFS.Open("rules.ini") as IniFile;
				var fsRules = VFS.Open<IniFile>("firestrm.ini");
				Logger.Info("Merging Firestorm rules with TS rules");
				_rules.MergeWith(fsRules);
				_art = VFS.Open("artmd.ini") as IniFile;
			}
		}

		/* Starkku: Statue of Liberty does not need special palette handling anymore, so commented those lines out.
		 * Also, game only uses temperat.pal for ore overlays - snow.pal, urban.pal etc. are UNUSED - some code below changed to match this.
		 */
		public void Initialize() {
			Logger.Info("Initializing theater");
			// load palettes and additional mix files for this theater
			switch (_theaterType) {
				case TheaterType.Temperate:
				case TheaterType.TemperateYR:
					_palettes = new PaletteCollection(_theaterType);
					_palettes.IsoPalette = new Palette(VFS.Open<PalFile>("isotem.pal"));
					_palettes.OvlPalette = new Palette(VFS.Open<PalFile>("temperat.pal"));
					_palettes.UnitPalette = new Palette(VFS.Open<PalFile>("unittem.pal"));
					break;

				case TheaterType.Snow:
				case TheaterType.SnowYR:
					_palettes = new PaletteCollection(_theaterType);
					_palettes.IsoPalette = new Palette(VFS.Open<PalFile>("isosno.pal"));
					_palettes.OvlPalette = new Palette(VFS.Open<PalFile>("temperat.pal"));
					_palettes.UnitPalette = new Palette(VFS.Open<PalFile>("unitsno.pal"));
					break;

				case TheaterType.Urban:
				case TheaterType.UrbanYR:
					_palettes = new PaletteCollection(_theaterType);
					_palettes.IsoPalette = new Palette(VFS.Open<PalFile>("isourb.pal"));
					_palettes.OvlPalette = new Palette(VFS.Open<PalFile>("temperat.pal"));
					_palettes.UnitPalette = new Palette(VFS.Open<PalFile>("uniturb.pal"));
					break;

				case TheaterType.Desert:
					_palettes = new PaletteCollection(_theaterType);
					_palettes.IsoPalette = new Palette(VFS.Open<PalFile>("isodes.pal"));
					_palettes.OvlPalette = new Palette(VFS.Open<PalFile>("temperat.pal"));
					_palettes.UnitPalette = new Palette(VFS.Open<PalFile>("unitdes.pal"));
					break;

				case TheaterType.Lunar:
					_palettes = new PaletteCollection(_theaterType);
					_palettes.IsoPalette = new Palette(VFS.Open<PalFile>("isolun.pal"));
					_palettes.OvlPalette = new Palette(VFS.Open<PalFile>("temperat.pal"));
					_palettes.UnitPalette = new Palette(VFS.Open<PalFile>("unitlun.pal"));
					break;

				case TheaterType.NewUrban:
					_palettes = new PaletteCollection(_theaterType);
					_palettes.IsoPalette = new Palette(VFS.Open<PalFile>("isoubn.pal"));
					_palettes.OvlPalette = new Palette(VFS.Open<PalFile>("temperat.pal"));
					_palettes.UnitPalette = new Palette(VFS.Open<PalFile>("unitubn.pal"));
					break;
			}

			foreach (string mix in Defaults.GetTheaterMixes(_theaterType))
				VFS.Add(mix, CacheMethod.Cache); // we wish for these to be cached as they're gonna be hit often

			_palettes.AnimPalette = new Palette(VFS.Open<PalFile>("anim.pal"));

			_tileTypes = new TileCollection(_theaterType, _engine, _art);

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