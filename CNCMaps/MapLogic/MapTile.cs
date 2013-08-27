using System.Collections.Generic;

namespace CNCMaps.MapLogic {

	public class MapTile : PaletteOwner {

		public ushort Dx { get; private set; }
		public ushort Dy { get; private set; }
		public ushort Rx { get; private set; }
		public ushort Ry { get; private set; }
		public short Z { get; private set; }

		public short TileNum { get; set; }
		public short SetNum { get; set; }
		public ushort SubTile { get; private set; }

		internal TileLayer Layer { get; private set; }
		internal bool ExtraDataAffected { get; set; }
		internal readonly List<GameObject> AllObjects = new List<GameObject>();

		public Palette Palette { get; set; }

		public MapTile(ushort dx, ushort dy, ushort rx, ushort ry, short rz, short tilenum, ushort subtile, TileLayer layer, short setnum = 0) {
			Dx = dx;
			Dy = dy;
			Rx = rx;
			Ry = ry;
			Z = rz;
			TileNum = tilenum;
			SetNum = setnum;
			SubTile = subtile;
			Layer = layer;
		}

		internal void AddObject(GameObject obj) {
			AllObjects.Add(obj);
			obj.Tile = this;
		}

		public override string ToString() {
			return string.Format("d({0},{1}),r({2},{3})", Dx, Dy, Rx, Ry);
		}

	}
}