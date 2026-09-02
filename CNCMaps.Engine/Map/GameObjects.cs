using System;
using System.Drawing;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.Map;
using CNCMaps.Shared;
using NLog;

namespace CNCMaps.Engine.Map {

	public interface OwnableObject {
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

		public GameCollection Collection { get; set; }

		public Drawable Drawable {
			get {
				if (_drawable == null) _drawable = Collection?.GetDrawable(this);
				return _drawable;
			}
			set { _drawable = value; }
		}

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

		public bool RequiresBoundsInvalidation = true;
		public bool RequiresFrameInvalidation = true;
		private Rectangle cachedBounds = Rectangle.Empty;
		private Drawable _drawable;

		public Rectangle GetBounds() {
			if (RequiresBoundsInvalidation && Drawable != null) {
				cachedBounds = Drawable.GetBounds(this);
				RequiresBoundsInvalidation = false;
			}
			return cachedBounds;
		}

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
		public InfantryObject(string owner, string name, short health, short direction, int subCell, bool onBridge) {
			Owner = owner;
			Name = name;
			Health = health;
			Direction = direction;
			SubCell = subCell;
			OnBridge = onBridge;
		}
		public int SubCell { get; set; }
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

		// The engine lights from the building's centre coordinate, Location + (Foundation - 1)
		// half cells per axis (BuildingClass::GetCoords 0x447ac0), so the GALITE lamps with
		// Foundation=0x0 light from their cell's top corner and reach the 2x2 block up-left of it.
		public double PosX { get; set; }
		public double PosY { get; set; }

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

			// An absent tint defaults to 1000.0 in the game (BuildingTypeClass ctor inits the fields to
			// 1,000,000 per-mille), so a lamp with an intensity but no tints saturates the tint clamp and
			// doubles brightness over its whole radius. Vanilla lamps set all three.
			LightVisibility = lamp.ReadDouble("LightVisibility", 5000.0);
			LightIntensity = lamp.ReadDouble("LightIntensity", 0.0);
			LightRedTint = lamp.ReadDouble("LightRedTint", 1000.0);
			LightGreenTint = lamp.ReadDouble("LightGreenTint", 1000.0);
			LightBlueTint = lamp.ReadDouble("LightBlueTint", 1000.0);
			this.scenario = scenario;
		}

		// A map section for the type makes the engine re-read it (BuildingTypeClass::Read_INI, gamemd
		// 0x460cac; OpenTS builtype.cpp:1206) with the per-mille field / 1000 as the default, and that
		// division is integer. Every light key the map section omits is truncated to whole units,
		// which turns off a lamp with a fractional intensity (xeb2 Sinkhole's INGRNLMP override).
		public void TruncateKeysOmittedBy(IniFile.IniSection mapSection) {
			if (!mapSection.HasKey("LightIntensity")) LightIntensity = Math.Truncate(LightIntensity);
			if (!mapSection.HasKey("LightRedTint")) LightRedTint = Math.Truncate(LightRedTint);
			if (!mapSection.HasKey("LightGreenTint")) LightGreenTint = Math.Truncate(LightGreenTint);
			if (!mapSection.HasKey("LightBlueTint")) LightBlueTint = Math.Truncate(LightBlueTint);
		}

		/// <summary>
		/// Applies a lamp to this object's palette if it's in range
		/// </summary>
		/// <param name="lamp">The lamp to apply</param>
		/// <returns>Whether the palette was replaced, meaning it needs to be recalculated</returns>
		public bool ApplyLamp(GameObject obj, bool ambientOnly = false) {
			var lamp = this;
			// the game only creates a light source for buildings with LightIntensity != 0
			const double TOLERANCE = 0.001;
			if (Math.Abs(lamp.LightIntensity) < TOLERANCE)
				return false;

			var drawLocation = obj.Tile;
			double dx = lamp.PosX - drawLocation.Rx;
			double dy = lamp.PosY - drawLocation.Ry;
			double leptons = 256 * Math.Sqrt(dx * dx + dy * dy);

			// LightSourceClass::Process (0x554af0) truncates the distance and keeps d <= visibility
			if ((0 < lamp.LightVisibility) && (Math.Floor(leptons) <= lamp.LightVisibility)) {
				double lsEffect = (lamp.LightVisibility - leptons) / lamp.LightVisibility;

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

		/// <summary>The drawable of the id the map stored, kept when tiberium swaps in the type's
		/// pooled art, because the shadow still comes from the stored id.</summary>
		public Drawables.Drawable StoredDrawable { get; set; }

		public OverlayObject(byte overlayID, byte overlayValue) {
			OverlayID = overlayID;
			OverlayValue = overlayValue;
		}

		public bool IsGeneratedVeins = false;

		public override string ToString() {
			return string.Format("{0} ({1})", Drawable != null ? Drawable.Name : OverlayID.ToString(), OverlayValue);
		}
	}
	public class SmudgeObject : NamedObject {
		public SmudgeObject(string name) {
			Name = name;
		}
		/// <summary>This copy's cell within the smudge's foundation; (0,0) is the map's own entry.</summary>
		public Point FoundationCell { get; set; }
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
		/// <summary>Handed to a starting player by a game-start trigger, which the engine treats as a capture.</summary>
		public bool PreCaptured { get; set; }

		public int WallBuildingFrame { get; set; }

		// screen row of the building body's drawn bottom, set by BuildingDrawable so
		// attached parts share the body's z anchor
		public int? DrawnBodyAnchorY { get; set; }
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