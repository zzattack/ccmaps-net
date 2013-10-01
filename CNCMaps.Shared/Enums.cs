using System.ComponentModel;
using System.Drawing.Design;
using CNCMaps.Shared.DynamicTypeDescription;

namespace CNCMaps.Shared {
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
		Custom
	};


	public enum StartPositionMarking {
		None,
		Tiled,
		Squared
	}
}