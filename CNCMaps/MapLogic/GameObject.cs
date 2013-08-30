namespace CNCMaps.MapLogic {
	public class GameObject : PaletteOwner {
		public virtual MapTile Tile { get; set; }
		public virtual MapTile BaseTile {
			get { return Tile; }
			set { }
		}

		public ObjectCollection Collection { get; set; }
		public Drawable Drawable { get; set; }

		public override string ToString() {
			if (this is NamedObject) return (this as NamedObject).Name;
			else if (this is NumberedObject) return (this as NumberedObject).Number.ToString();
			return GetType().ToString();
		}
		
		public LightingType Lighting {
			get { return Drawable.LightingType; }
		}
	}
}