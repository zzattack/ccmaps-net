using System;
using System.ComponentModel;
using System.Drawing.Design;
using CNCMaps.Shared.DynamicTypeDescription;

namespace CNCMaps.Shared {

	[Editor(typeof(StandardValueEditor), typeof(UITypeEditor))]
	[Flags]
	public enum OverlayTibType {
		NotSpecial = 0,
		Riparius = 1, // note: don't change the indices of 0-3! they're hardcoded in the game too!
		Cruentus = 2,
		Vinifera = 4,
		Aboreus = 8,
		Ore = 1, // ts: rip
		Gems = 2, // ts: cru
		All = 15,
	};

	[Editor(typeof(StandardValueEditor), typeof(UITypeEditor))]
	public enum EngineType {
		[StandardValue("Auto Detect", Visible = false)]
		AutoDetect = 0,
		TiberianSun = 1,
		Firestorm = 2,
		RedAlert2 = 3,
		YurisRevenge = 4,
	}

	[Editor(typeof(StandardValueEditor), typeof(UITypeEditor))]
	[Flags]
	public enum TheaterType {
		None = 0,
		Temperate = 1,
		Urban = 2,
		Snow = 4,
		Lunar = 8,
		Desert = 16,
		NewUrban = 32,
		All = 63,
	}

	[Editor(typeof(StandardValueEditor), typeof(UITypeEditor))]
	[Flags]
	public enum CollectionType {
		None = 0,
		Aircraft = 1,
		Building = 2,
		Infantry = 4,
		Overlay = 8,
		Smudge = 16,
		Terrain = 32 ,
		Vehicle = 64,
		Animation = 128,
		Tiles = 256,
		All = 511
	}

	[Editor(typeof(StandardValueEditor), typeof(UITypeEditor))]
	public enum LightingType {
		[StandardValue("No special lighting (default for ore/gems)")]
		None,
		[StandardValue("Global receives the lighting of the map as specified in [Lighting] but nothing else ")]
		Global,
		[StandardValue("Same as global, but with z-level adjustments ")]
		Level,
		[StandardValue("Same as level, but with additional lighting affected by only the ambient color of lamps")]
		Ambient,
		[StandardValue("Full lighting, including r/g/b tints from lamps")]
		Full,
		[StandardValue("No change")]
		Default,
	};


	[Editor(typeof(StandardValueEditor), typeof(UITypeEditor))]
	public enum PaletteType {
		[StandardValue("No palette", Visible = false)]
		None,
		[StandardValue("The iso palette, for tiles etc.")]
		Iso,
		[StandardValue("Unit palette, primarily for units")]
		Unit,
		[StandardValue("Overlay palette")]
		Overlay,
		[StandardValue("Animations palette")]
		Anim,
		[StandardValue("Custom palette (officially only used for Statue of Liberty)")]
		Custom,
		[StandardValue("No change")]
		Default,
	};


	public enum StartPositionMarking {
		None,
		Squared,
		Circled,
		Diamond,
		Ellipsed,
		Tiled,
	}

	public enum PreviewMarkersType {
		None,
		SelectedAsAbove,
		Bittah,
		Aro,
	}
}