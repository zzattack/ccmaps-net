// Compiled only for the cross-platform (non-Windows) target, where the WinForms
// based property-grid editors are excluded. These stubs satisfy the [Editor]
// attribute references in ModConfig/Enums; they are inert metadata at runtime.
namespace System.Drawing.Design {
	public class UITypeEditor {
	}
}

namespace CNCMaps.Shared.DynamicTypeDescription {
	public class StandardValueEditor : System.Drawing.Design.UITypeEditor {
	}
}
