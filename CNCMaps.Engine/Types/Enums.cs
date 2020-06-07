using System;

namespace CNCMaps.Engine.Types {

	public enum MovementZone {
		Amphibious, //  when pathfinding, the unit will consider both ground and water as passable.
		AmphibiousCrusher, // when pathfinding, the unit will consider both ground and water as passible. In addition, it will assume that it is capable of crushing infantry, but is unarmed.1.
		AmphibiousDestroyer, // Same as above, additionally can destroy terrain obstacles. In RA2 this is tied to being an amphibious infantry.
		Crusher, // when pathfinding, only clear ground is considered passable. Also assumes that it can crush infantry, but is not armed.1
		CrusherAll, // Same as above, additionally assumes that it can crush any mobile object, as well as walls. 2
		Destroyer, // when pathfinding, considers ground passable. Can destroy terrain obstacles and crush infantry obstacles.
		Fly, // when pathfinding, it assumes everything passable.
		Infantry, // when pathfinding, only clear ground is considered passable.
		InfantryDestroyer, // Same as above, but can destroy terrain obstacles such as trees.
		Normal, // when pathfinding, considers clear ground passable. Assumes it can destroy terrain obstacles and crush infantry.
		Subterranean, // when pathfinding, considers clear ground passable. When destination is too far away for surface travel, or there is an obstacle in the way, unit will dig underground. Unit cannot dig on pavement. In other cases, when subterranean travel is not possible, or surface travel is quicker, acts like a unit with MovementZone=Crusher or MovementZone=Destroyer.
		Water, // when pathfinding, considers only water as passable.
	}

	public enum PipScale {
		charge, none, passengers, ammo, tiberium, power, mindcontrol
	}

	public enum SpeedType {
		Clear, // Clear ground, no obstacles.
		Road, // Dirt or paved roads.
		Rough, // Rougher terrain such as rocky areas or thick grass. In RA2, this defaults to the same settings as Clear.
		Rock, // rocks, trees, cliffs, anything impassable.
		Tiberium, // areas covered in Ore/Gems/Tiberium.
		Water, // Water as in rivers or ocean areas.
		Railroad, // Train tracks (Works in TS, in RA2 requires Terrain Expansion (aka TX)).
		Tunnel, // Tunnel Entrance/Exit (Works in TS, in RA2 requires Terrain Expansion (aka TX)).
		Beach, // The line where water and ground join.
		Weeds, // Tiberium Veins (Works in TS, in RA2 it is not assigned to anything and can be used for new movement rules).
		Ice, // Water gone cold (Works in TS, in RA2 it is not assigned to anything and can be used for new movement rules).
		Wall, // non-firestorm/non-laserfence walls.
	}

	public enum LandType {
		Clear, // normal clear terrain
		Road, // roads (both dirt and paved)
		Rock, // rocky terrain (including rocks themselves)
		Beach, // where water joins land
		Rough, // rough terrain (debris for example)
		Ice, // ice covering water (although this may be disabled in Red Alert 2)
		Railroad, // railroads. Residual from Tiberian Sun, although it is not known if this logic still works in Red Alert 2
		Tunnel, // tunnels under cliffs etc. The tunnel logic does work in Red Alert 2
		Weeds, // residual from Tiberian Sun, although it is not known if this logic still works in Red Alert 2
	}

	[Flags]
	public enum Abilities : int {
		FASTER, // Applies the effect of VeteranSpeed to Speed
		STRONGER, // Applies the effect of VeteranArmor to damage received
		FIREPOWER, // Applies the effect of VeteranCombat to Damage of all weapons
		SCATTER, // Causes unit to automatically scatter from weapon fire and move away from units that attempt to crush it (if applicable)
		ROF, // Applies the effect of VeteranROF to ROF of all weapons
		SIGHT, // Applies the effect of VeteranSight to Sight
		CLOAK, // Grants the effect of Cloakable=yes
		TIBERIUM_PROOF, // Grants the effect of TiberiumProof=yes
		VEIN_PROOF, // Grants the effect of ImmuneToVeins=yes
		SELF_HEAL, // Grants the effect of SelfHealing=yes
		EXPLODES, // Grants the effect of Explodes=yes
		RADAR_INVISIBLE, // Grants the effect of RadarInvisible=yes
		SENSORS, // Grants the effect of Sensors=yes
		FEARLESS, // Grants the effect of Fearless=yes
		C4, // Grants the effect of C4=yes1
		TIBERIUM_HEAL, // Grants the effect of TiberiumHeal=yes
		GUARD_AREA, // Causes unit to automatically enter a guard mission when idle
		CRUSHER, // Grants the effect of Crusher=yes
	}

	public enum VHPScan {
		None, // To be determined, very most likely ignores the projected health.
		Normal, // Attacks everything in green health. 
		Strong, // Attacks everything alive.
	}

	// This states to which 'category' this object belongs and is used by the AI systems for targeting,
	// team formation and construction purposes. For best results, and to avoid undesirable effects in 
	// the game, it is best to be truthful when defining this object. Placing it in a category to which
	// it would not normally belong will make the AI even more ineffective than it already is. 
	// NOTE: although mentioned in RULES.INI, the Civilian category is obsolete and unused in Red Alert 2.
	// Since Tiberian Sun, objects are defined as being civilian when they have Civilian=yes set. 
	public enum Category {
		AirLift, // Air Transport
		AirPower, // Air Combat Support
		Transport, // Transport Vehicle
		Support, // Miscellaneous Support Vehicle
		LRFS, // Long Range (Indirect) Fire Support
		IFV, // Infantry Fighting Vehicle
		AFV, // Armored Fighting Vehicle
		Recon, // Reconnaisance Vehicle
		VIP, // Important infantry
		Soldier, // Infantry unit
		None = -1,
	}

	public enum BuildCategory {
		Combat, // The structure is used specifically for a combat or defensive purpose
		Infrastructure, // Not known, appears to have no significant effect
		Resource, // The structure provides the player with the ability to store or use money
		Power, // The structure supplies power to the player
		Tech, // The structure provides new construction options
		DontCare, // Strange one, the objects Cameo= displays as if it is partly built
	}

	public enum FactoryType {
		AircraftType,
		BuildingType,
		InfantryType,
		UnitType,
	}

	public enum Layer {
		Ground, // ??
	}

	public enum Behaviour2 {
		Railgun,
		Spark,
		Gas,
		Smoke,
		Fire,
	}

	public enum Action {
		MultiMissile,
		EMPulse,
		Firestorm,
		IonCannon,
		HunterSeeker,
		ChemMissile,
		DropPod,
		IronCurtain,
		LightningStorm,
		ChronoSphere,
		ChronoWarp,
		ParaDrop,
		AmerParaDrop,
		PsychicDominator,
		SpyPlane,
		GeneticConverter,
		ForceShield,
		PsychicReveal,
	}
}
