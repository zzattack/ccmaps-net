namespace CNCMaps.MapLogic {
	public class StructureObject : NamedObject, OwnableObject {

		public StructureObject(string owner, string name, short health, short direction) {
			Owner = owner;
			Name = name;
			Health = health;
			Direction = direction;
		}

		public override MapTile BaseTile { get; set; }
		public short Health { get; set; }
		public short Direction { get; set; }
		public string Owner { get; set; }
	}
}