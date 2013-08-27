namespace CNCMaps.MapLogic {
	public enum PaletteSettings {
		None, // implies unchosen
		Iso, // for tiles and stuff
		Unit, // for units and stuff
		Overlay, // for overlays...
		Anim, // for animation
		Custom // give by a custom string (officially only used for Statue of Liberty)
	}
	
	public enum LightingType {
		None, // No special lighting (ore/gems).
		Global, // Global would get the lighting of the map as specified in [Lighting] but nothing else. NOT USED.
		Level, // as above, adjusts per z-level of the map
		AmbientOnly, // as above, + affected by lamps, but only the ambient color
		Full, // as above, + affected by lamps including r/g/b tints
	};

}