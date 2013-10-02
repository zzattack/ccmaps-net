using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace CNCMaps.Engine.Types {
	public class VehicleType : TechnoType {
		public VehicleType(string ID) : base(ID) { }

		// art
		public int WalkFrames;
		public int FiringFrames;
		public int StandingFrames;
		public int DeathFrames;
		public int DeathFrameRate;
		public int Facings;
		public int StartStandFrame;
		public int StartWalkFrame;
		public int StartFiringFrame;
		public int StartDeathFrame;
		public int MaxDeathCounter;
		public int FiringSyncFrameX;

		// rules
		public bool DeployToFire;
		public bool IsSimpleDeployer;
		public bool Harvester;
		public bool Weeder;
		public bool IsTilter;
		public bool CarriesCrate;
		public bool TooBigToFitUnderBridge;
		public Vector3 HalfDamageSmokeLocation;
		public bool UseTurretShadow;
		public bool Passive;
		public LandType MovementRestrictedTo;
		public bool CanBeach;
		public bool SmallVisceroid;
		public bool LargeVisceroid;
		public bool NonVehicle;
		public int BurstDelayX;
		public string AltImage;
		public bool CrateGoodie;

		public override void LoadArt(FileFormats.IniFile.IniSection art) {
			base.LoadArt(art);

			WalkFrames = art.ReadInt("WalkFrames", 12);
			FiringFrames = art.ReadInt("FiringFrames");
			StandingFrames = art.ReadInt("StandingFrames");
			DeathFrames = art.ReadInt("DeathFrames");
			DeathFrameRate = art.ReadInt("DeathFrameRate", 1);
			Facings = art.ReadInt("Facings", 8);
			StartStandFrame = art.ReadInt("StartStandFrame", -1);
			StartWalkFrame = art.ReadInt("StartWalkFrame", -1);
			StartFiringFrame = art.ReadInt("StartFiringFrame", -1);
			StartDeathFrame = art.ReadInt("StartDeathFrame", -1);
			MaxDeathCounter = art.ReadInt("MaxDeathCounter", -1);
			FiringSyncFrameX = art.ReadInt("FiringSyncFrameX", -1);
		}

		public override void LoadRules(FileFormats.IniFile.IniSection rules) {
			base.LoadRules(rules);
			DeployToFire = rules.ReadBool("DeployToFire");
			IsSimpleDeployer = rules.ReadBool("IsSimpleDeployer");
			Harvester = rules.ReadBool("Harvester");
			Weeder = rules.ReadBool("Weeder");
			SpeedType = rules.ReadEnum("SpeedType", SpeedType.Clear);
			IsTilter = rules.ReadBool("IsTilter");
			CarriesCrate = rules.ReadBool("CarriesCrate");
			TooBigToFitUnderBridge = rules.ReadBool("TooBigToFitUnderBridge");
			HalfDamageSmokeLocation = rules.ReadXYZ("HalfDamageSmokeLocation");
			UseTurretShadow = rules.ReadBool("UseTurretShadow");
			Passive = rules.ReadBool("Passive");
			MovementRestrictedTo = rules.ReadEnum("MovementRestrictedTo", LandType.Clear);
			CanBeach = rules.ReadBool("CanBeach");
			SmallVisceroid = rules.ReadBool("SmallVisceroid");
			LargeVisceroid = rules.ReadBool("LargeVisceroid");
			NonVehicle = rules.ReadBool("NonVehicle");
			BurstDelayX = rules.ReadInt("BurstDelayX", -1);
			AltImage = rules.ReadString("AltImage");
			CrateGoodie = rules.ReadBool("CrateGoodie");
		}

	}
}
