namespace CNCMaps.MapLogic {
	public class RA2Object {
		public MapTile Tile { get; set; }
		public override string ToString() {
			if (this is NamedObject) return (this as NamedObject).Name;
			else if (this is NumberedObject) return (this as NumberedObject).Number.ToString();
			return GetType().ToString();
		}
	}
}