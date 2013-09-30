using System.ComponentModel;
using System.Drawing.Design;
using CNCMaps.Shared.DynamicTypeDescription;

namespace CNCMaps.Shared {

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
}