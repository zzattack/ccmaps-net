namespace CNCMaps.MapLogic {
	public class StructureObject : NamedObject, DamageableObject {

		public StructureObject(string owner, string name, short health, short direction) {
			Owner = owner;
			Name = name;
			Health = health;
			Direction = direction;
		}
		public MapTile DrawTile { get; set; }
		public short Health { get; set; }

		public short Direction { get; private set; }

		public string Owner { get; set; }

		public Palette Palette { get; set; }
	}
}