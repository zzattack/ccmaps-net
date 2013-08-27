namespace CNCMaps.MapLogic {
	public class TerrainObject : NamedObject {
		public TerrainObject(string name) {
			Name = name;
		}

        // Starkku: They have a palette as well.
        public Palette Palette { get; set; }
	}
}