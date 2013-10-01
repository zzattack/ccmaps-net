using System;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.Map;
using CNCMaps.Shared;
using NLog;

namespace CNCMaps.Engine.Map {
	
	interface OwnableObject {
		string Owner { get; set; }
		short Health { get; set; }
		short Direction { get; set; }
		bool OnBridge { get; set; }
	}

	public class GameObject {
		public GameObject() {
			Id = IdCounter++;
		}
		public virtual MapTile Tile { get; set; }
		public virtual MapTile BottomTile {
			get { return Tile; }
			set { throw new InvalidOperationException("Override this property if you want to use it"); }
		}
		public virtual MapTile TopTile {
			get { return Tile; }
			set { throw new InvalidOperationException("Override this property if you want to use it"); }
		}

		public ObjectCollection Collection { get; set; }
		public Drawable Drawable { get; set; }
		public Palette Palette { get; set; }

		public override string ToString() {
			if (this is NamedObject) return (this as NamedObject).Name;
			else if (this is NumberedObject) return (this as NumberedObject).Number.ToString();
			return GetType().ToString();
		}

		public LightingType Lighting {
			get { return Drawable != null ? Drawable.Props.LightingType : LightingType.Full; }
		}

		public int Id { get; set; }
		private static int IdCounter = 0;
		public int DrawOrderIndex = -1;
	}
	public class NumberedObject : GameObject {
		public virtual int Number { get; protected set; }
	}
	public class NamedObject : GameObject {
		public string Name { get; protected set; }
	}
	public class AircraftObject : NamedObject, OwnableObject {
		public AircraftObject(string owner, string name, short health, short direction, bool onBridge) {
			Owner = owner;
			Name = name;
			Health = health;
			Direction = direction;
			OnBridge = onBridge;
		}
		public override MapTile BottomTile { get; set; }
		public override MapTile TopTile { get; set; }
		public short Health { get; set; }
		public short Direction { get; set; }
		public bool OnBridge { get; set; }
		public string Owner { get; set; }
	}
	public class InfantryObject : NamedObject, OwnableObject {
		public InfantryObject(string owner, string name, short health, short direction, bool onBridge) {
			Owner = owner;
			Name = name;
			Health = health;
			Direction = direction;
			OnBridge = onBridge;
		}
		public short Health { get; set; }
		public short Direction { get; set; }
		public bool OnBridge { get; set; }
		public string Owner { get; set; }
		public override MapTile BottomTile { get; set; }
		public override MapTile TopTile { get; set; }
	}
	public class LightSource : StructureObject {
		public double LightVisibility { get; set; }
		public double LightIntensity { get; set; }
		public double LightRedTint { get; set; }
		public double LightGreenTint { get; set; }
		public double LightBlueTint { get; set; }

		// not yet used
		Lighting scenario;

		static Logger logger = LogManager.GetCurrentClassLogger();

		public LightSource() : base("nobody", "", 0, 0) { }
		public LightSource(IniFile.IniSection lamp, Lighting scenario)
			: base("nobody", lamp.Name, 0, 0) {
			Initialize(lamp, scenario);
		}

		void Initialize(IniFile.IniSection lamp, Lighting scenario) {
			logger.Trace("Loading LightSource {0} at ({1},{2})", lamp.Name, Tile);

			// Read and assume default values
			LightVisibility = lamp.ReadDouble("LightVisibility", 5000.0);
			LightIntensity = lamp.ReadDouble("LightIntensity", 0.0);
			LightRedTint = lamp.ReadDouble("LightRedTint", 1.0);
			LightGreenTint = lamp.ReadDouble("LightGreenTint", 1.0);
			LightBlueTint = lamp.ReadDouble("LightBlueTint", 1.0);
			this.scenario = scenario;
		}

		/// <summary>
		/// Applies a lamp to this object's palette if it's in range
		/// </summary>
		/// <param name="lamp">The lamp to apply</param>
		/// <returns>Whether the palette was replaced, meaning it needs to be recalculated</returns>
		public bool ApplyLamp(GameObject obj, bool ambientOnly = false) {
			var lamp = this;
			const double TOLERANCE = 0.001;
			if (Math.Abs(lamp.LightIntensity) < TOLERANCE)
				return false;

			var drawLocation = obj.Tile;
			double sqX = (lamp.Tile.Rx - drawLocation.Rx) * (lamp.Tile.Rx - drawLocation.Rx);
			double sqY = (lamp.Tile.Ry - (drawLocation.Ry)) * (lamp.Tile.Ry - (drawLocation.Ry));

			double distance = Math.Sqrt(sqX + sqY);

			// checks whether we're in range
			if ((0 < lamp.LightVisibility) && (distance < lamp.LightVisibility / 256)) {
				double lsEffect = (lamp.LightVisibility - 256 * distance) / lamp.LightVisibility;

				// we don't want to apply lamps to shared palettes, so clone first
				if (obj.Palette.IsShared)
					obj.Palette = obj.Palette.Clone();

				obj.Palette.ApplyLamp(lamp, lsEffect, ambientOnly);
				return true;
			}
			else
				return false;
		}
	}
	public class OverlayObject : NumberedObject {
		public byte OverlayID {
			get { return (byte)Number; }
			set { Number = value; }
		}

		public byte OverlayValue { get; set; }
		public override MapTile BottomTile { get; set; }
		public override MapTile TopTile { get; set; }

		public OverlayObject(byte overlayID, byte overlayValue) {
			OverlayID = overlayID;
			OverlayValue = overlayValue;
		}
	}
	public class SmudgeObject : NamedObject {
		public SmudgeObject(string name) {
			Name = name;
		}
		public override MapTile BottomTile { get; set; }
		public override MapTile TopTile { get; set; }
	}
	public class StructureObject : NamedObject, OwnableObject {
		public StructureObject(string owner, string name, short health, short direction) {
			Owner = owner;
			Name = name;
			Health = health;
			Direction = direction;
		}

		public override MapTile BottomTile { get; set; }
		public override MapTile TopTile { get; set; }
		public short Health { get; set; }
		public short Direction { get; set; }
		public bool OnBridge { get; set; }
		public string Owner { get; set; }

		public string Upgrade1 { get; set; }
		public string Upgrade2 { get; set; }
		public string Upgrade3 { get; set; }
	}
	public class TerrainObject : NamedObject {
		public TerrainObject(string name) {
			Name = name;
		}
	}
	public class UnitObject : NamedObject, OwnableObject {
		public UnitObject(string owner, string name, short health, short direction, bool onBridge) {
			Owner = owner;
			Name = name;
			Health = health;
			Direction = direction;
			OnBridge = onBridge;
		}
		public override MapTile BottomTile { get; set; }
		public override MapTile TopTile { get; set; }
		public short Health { get; set; }
		public short Direction { get; set; }
		public bool OnBridge { get; set; }
		public string Owner { get; set; }
	}
	public class AnimationObject : NamedObject {
		public AnimationObject(string name, Drawable drawable) {
			Name = name;
			Drawable = drawable;
		}
		//public override MapTile BottomTile { get; set; }
		//public override MapTile TopTile { get; set; }
	}
}