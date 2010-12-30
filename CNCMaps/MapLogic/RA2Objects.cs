using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CNCMaps.FileFormats;

namespace CNCMaps.MapLogic {

	public class MapTile {
		public ushort Dx { get; private set; }
		public ushort Dy { get; private set; }
		public ushort Rx { get; private set; }
		public ushort Ry { get; private set; }
		public short TileNum { get; private set; }
		public ushort SubTile { get; private set; }
		List<RA2Object> AllObjects = new List<RA2Object>();

		public MapTile(ushort dx, ushort dy, ushort rx, ushort ry, short tilenum, ushort subtile) {
			this.Dx = dx;
			this.Dy = dy;
			this.Rx = rx;
			this.Ry = ry;
			this.TileNum = tilenum;
			this.SubTile = subtile;
		}

		internal void AddObject(RA2Object obj) {
			AllObjects.Add(obj);
			obj.Tile = this;
		}
	}

	public class RA2Object {
		public MapTile Tile { get; set; }
		public Palette Palette { get; set; }
	}

	public class SmudgeObject : RA2Object {
		public string Name { get; private set; }

		public SmudgeObject(string name) {
			this.Name = name;
		}

	}

	public class TerrainObject : RA2Object {
		public string Name { get; private set; }

		public TerrainObject(string name) {
			this.Name = name;
		}

	}

	public class OverlayObject : RA2Object {
		public byte OverlayID { get; private set; }
		public byte OverlayValue { get; private set; }

		public OverlayObject(byte overlayID, byte overlayValue) {
			this.OverlayID = overlayID;
			this.OverlayValue = overlayValue;
		}
	}

	public class StructureObject : RA2Object {
		public StructureObject(string owner, string name, short health, short direction) {
			this.Owner = owner;
			this.Name = name;
			this.Health = health;
			this.Direction = direction;
		}
		public string Name { get; private set; }
		public short Health { get; private set; }
		public short Direction { get; private set; }
		public string Owner { get; set; }
	}

	public class InfantryObject : RA2Object {
		public InfantryObject(string owner, string name, short health, short direction) {
			this.Owner = owner;
			this.Name = name;
			this.Health = health;
			this.Direction = direction;
		}
		public string Name { get; private set; }
		public short Health { get; private set; }
		public short Direction { get; private set; }
		public string Owner { get; set; }
	}

	public class AircraftObject : RA2Object {
		public AircraftObject(string owner, string name, short health, short direction) {
			this.Owner = owner;
			this.Name = name;
			this.Health = health;
			this.Direction = direction;
		}
		public string Name { get; private set; }
		public short Health { get; private set; }
		public short Direction { get; private set; }
		public string Owner { get; set; }
	}

	public class UnitObject : RA2Object {
		public UnitObject(string owner, string name, short health, short direction) {
			this.Owner = owner;
			this.Name = name;
			this.Health = health;
			this.Direction = direction;
		}
		public string Name { get; private set; }
		public short Health { get; private set; }
		public short Direction { get; private set; }
		public string Owner { get; set; }
	}

}
