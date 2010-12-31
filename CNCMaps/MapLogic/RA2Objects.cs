using System.Collections.Generic;
using CNCMaps.FileFormats;
using System;
using CNCMaps.Utility;

namespace CNCMaps.MapLogic {

	public class MapTile {

		public ushort Dx { get; private set; }

		public ushort Dy { get; private set; }

		public ushort Rx { get; private set; }

		public ushort Ry { get; private set; }

		public short Z { get; private set; }

		public short TileNum { get; set; }

		public short SetNum { get; set; }

		public ushort SubTile { get; private set; }

		List<RA2Object> AllObjects = new List<RA2Object>();

		public Palette Palette { get; set; }

		public bool PaletteIsOriginal { get; set; }

		public MapTile(ushort dx, ushort dy, ushort rx, ushort ry, short rz, short tilenum, ushort subtile, short setnum = 0) {
			this.Dx = dx;
			this.Dy = dy;
			this.Rx = rx;
			this.Ry = ry;
			this.Z = rz;
			this.TileNum = tilenum;
			this.SetNum = setnum;
			this.SubTile = subtile;
		}

		internal void AddObject(RA2Object obj) {
			AllObjects.Add(obj);
			obj.Tile = this;
		}

		/// <summary>
		/// Applies a lamp to this object's palette if it's in range
		/// </summary>
		/// <param name="lamp">The lamp to apply</param>
		/// <returns>Whether the palette was replaced, meaning it needs to be recalculated</returns>
		public virtual bool ApplyLamp(LightSource lamp) {
			if (lamp.LightIntensity == 0.0)
				return false;

			double sqX = (lamp.Tile.Rx - Rx) * (lamp.Tile.Rx - Rx);
			double sqY = (lamp.Tile.Ry - (Ry)) * (lamp.Tile.Ry - (Ry));

			double distance = Math.Sqrt(sqX + sqY);

			// checks whether we're in range
			if ((0 < lamp.LightVisibility) && (distance < lamp.LightVisibility / 256)) {
				double lsEffect = (lamp.LightVisibility - 256 * distance) / lamp.LightVisibility;
				// make sure we copy the palette only once
				if (this.PaletteIsOriginal) {
					this.Palette = this.Palette.Clone();
					PaletteIsOriginal = false;
				}
				Palette.ApplyLamp(lamp, lsEffect);
				return true;
			}
			else
				return false;
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

		public byte OverlayID { get; set; }
		public byte OverlayValue { get; set; }

		public OverlayObject(byte overlayID, byte overlayValue) {
			this.OverlayID = overlayID;
			this.OverlayValue = overlayValue;
		}

		public const byte MaxOreID = 127;
		public const byte MinOreID = 102;
		public const byte MaxGemsID = 38;
		public const byte MinGemsID = 27;
		public bool IsOre() {
			return OverlayID >= MinOreID && OverlayID <= MaxOreID;
		}

		public bool IsGem() {
			return OverlayID >= MinGemsID && OverlayID <= MaxGemsID;
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