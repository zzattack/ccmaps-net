using CNCMaps.FileFormats;
using System.Drawing;
using CNCMaps.VirtualFileSystem;
using System;
namespace CNCMaps.MapLogic {

	public enum TheaterType {
		Temperate, TemperateYR,
		Urban, UrbanYR,
		Snow, SnowYR,
		Lunar,
		Desert,
		NewUrban
	}

	public enum PaletteType {
		Iso,
		Lib,
		Unit,
		Overlay,
		Anim
	}

	struct PaletteCollection {
		public Palette isoPalette, libPalette, ovlPalette, unitPalette, animPalette;

		internal Palette GetPalette(PaletteType paletteType) {
			switch (paletteType) {
				case PaletteType.Anim: return animPalette;
				case PaletteType.Lib: return libPalette;
				case PaletteType.Overlay: return ovlPalette;
				case PaletteType.Unit: return unitPalette;
				case PaletteType.Iso:
				default:
					return isoPalette;
			}
		}
	}

	class Theater {
		TheaterType theaterType;
		EngineType engine;
		IniFile rules;
		IniFile art;

		ObjectCollection infantryTypes;
		ObjectCollection vehicleTypes;
		ObjectCollection aircraftTypes;
		ObjectCollection buildingTypes;
		ObjectCollection overlayTypes;
		ObjectCollection terrainTypes;
		ObjectCollection smudgeTypes;
		TileCollection tileTypes;
		PaletteCollection palettes;

		public Theater(string theaterName, EngineType engine) :
			this(TheaterTypeFromString(theaterName), engine) { }

		public Theater(string theaterName, EngineType engine, IniFile rules, IniFile art) :
			this(TheaterTypeFromString(theaterName), engine, rules, art) { }

		public Theater(TheaterType theaterType, EngineType engine, IniFile rules, IniFile art) {
			this.theaterType = theaterType;
			this.engine = engine;
			this.rules = rules;
			this.art = art;
		}
		public Theater(TheaterType theaterType, EngineType engine) {
			this.theaterType = theaterType;
			this.engine = engine;
			if (engine == EngineType.RedAlert2) {
				rules = VFS.Open("rules.ini") as IniFile;
				art = VFS.Open("art.ini") as IniFile;
			}
			else if (engine == EngineType.YurisRevenge) {
				rules = VFS.Open("rulesmd.ini") as IniFile;
				art = VFS.Open("artmd.ini") as IniFile;
			}
		}

		public void Initialize() {
			// load palettes and additional mix files for this theater
			switch (theaterType) {
				case TheaterType.Temperate:
				case TheaterType.TemperateYR:
					palettes.isoPalette = new Palette(VFS.Open("isotem.pal") as PalFile);
					palettes.libPalette = new Palette(VFS.Open("libtem.pal") as PalFile);
					palettes.ovlPalette = new Palette(VFS.Open("temperat.pal") as PalFile);
					palettes.unitPalette = new Palette(VFS.Open("unittem.pal") as PalFile);
					break;

				case TheaterType.Snow:
				case TheaterType.SnowYR:
					palettes.isoPalette = new Palette(VFS.Open("isosno.pal") as PalFile);
					palettes.libPalette = new Palette(VFS.Open("libsno.pal") as PalFile);
					palettes.ovlPalette = new Palette(VFS.Open("snow.pal") as PalFile);
					palettes.unitPalette = new Palette(VFS.Open("unitsno.pal") as PalFile);
					break;

				case TheaterType.Urban:
				case TheaterType.UrbanYR:
					palettes.isoPalette = new Palette(VFS.Open("isourb.pal") as PalFile);
					palettes.libPalette = new Palette(VFS.Open("liburb.pal") as PalFile);
					palettes.ovlPalette = new Palette(VFS.Open("urban.pal") as PalFile);
					palettes.unitPalette = new Palette(VFS.Open("uniturb.pal") as PalFile);
					break;

				case TheaterType.Desert:
					palettes.isoPalette = new Palette(VFS.Open("isodes.pal") as PalFile);
					palettes.libPalette = new Palette(VFS.Open("libdes.pal") as PalFile);
					palettes.ovlPalette = new Palette(VFS.Open("desert.pal") as PalFile);
					palettes.unitPalette = new Palette(VFS.Open("unitdes.pal") as PalFile);
					break;

				case TheaterType.Lunar:
					palettes.isoPalette = new Palette(VFS.Open("isolun.pal") as PalFile);
					palettes.libPalette = new Palette(VFS.Open("liblun.pal") as PalFile);
					palettes.ovlPalette = new Palette(VFS.Open("lunar.pal") as PalFile);
					palettes.unitPalette = new Palette(VFS.Open("unitlun.pal") as PalFile);
					break;

				case TheaterType.NewUrban:
					palettes.isoPalette = new Palette(VFS.Open("isoubn.pal") as PalFile);
					palettes.libPalette = new Palette(VFS.Open("libubn.pal") as PalFile);
					palettes.ovlPalette = new Palette(VFS.Open("urbann.pal") as PalFile);
					palettes.unitPalette = new Palette(VFS.Open("unitubn.pal") as PalFile);
					break;
			}

			foreach (string mix in TheaterDefaults.GetTheaterMixes(theaterType))
				VFS.Add(mix);

			palettes.animPalette = new Palette(VFS.Open("anim.pal") as PalFile);

			tileTypes = new TileCollection(theaterType);

			buildingTypes = new ObjectCollection(rules.GetSection("BuildingTypes"),
				CollectionType.Building, theaterType, rules, art, palettes);

			aircraftTypes = new ObjectCollection(rules.GetSection("AircraftTypes"),
				CollectionType.Aircraft, theaterType, rules, art, palettes);

			infantryTypes = new ObjectCollection(rules.GetSection("InfantryTypes"),
				CollectionType.Infantry, theaterType, rules, art, palettes);

			overlayTypes = new ObjectCollection(rules.GetSection("OverlayTypes"),
				CollectionType.Overlay, theaterType, rules, art, palettes);

			terrainTypes = new ObjectCollection(rules.GetSection("TerrainTypes"),
				CollectionType.Terrain, theaterType, rules, art, palettes);

			smudgeTypes = new ObjectCollection(rules.GetSection("SmudgeTypes"),
				CollectionType.Smudge, theaterType, rules, art, palettes);

			vehicleTypes = new ObjectCollection(rules.GetSection("VehicleTypes"),
				CollectionType.Vehicle, theaterType, rules, art, palettes);
		}

		static TheaterType TheaterTypeFromString(string theater, bool yr = false) {
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

	}
}