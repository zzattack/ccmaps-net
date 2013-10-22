using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CNCMaps.Engine.Types;
using CNCMaps.FileFormats;

namespace CNCMaps.Objects.Westwood.Types {
	public class SuperWeaponType : AbstractType {
		
		// rules
		public WeaponType WeaponType;
		public Action Action;
		public bool IsPowered;
		public bool DisableableFromShell;
		public int SidebarFlashTabFrames;
		public bool AIDefendAgainst;
		public bool PreClick;
		public bool PostClick;
		public bool ShowTimer;
		public Sound SpecialSound;
		public Sound StartSound;
		public float Range;
		public int LineMultiplier;
		public Type Type;
		public Type PreDependent;
		public BuildingType AuxBuilding;
		public bool UseChargeDrain;
		public bool ManualControl;
		public float RechargeTime;
		public string SidebarImage;


		public SuperWeaponType(string ID) : base(ID) {}

		public override void LoadRules(IniFile.IniSection rules) {
			base.LoadRules(rules);

			WeaponType = rules.ReadEnum<WeaponType>("WeaponType");
			Action = rules.ReadEnum<Action>("Action", Action.);
			IsPowered = rules.ReadBool("IsPowered", true);
			DisableableFromShell = rules.ReadBool("DisableableFromShell");
			SidebarFlashTabFrames = rules.ReadInt("SidebarFlashTabFrames", -1);
			AIDefendAgainst = rules.ReadBool("AIDefendAgainst");
			PreClick = rules.ReadBool("PreClick");
			PostClick = rules.ReadBool("PostClick");
			ShowTimer = rules.ReadBool("ShowTimer");
			SpecialSound = Get<Sound>(rules.ReadString("SpecialSound"1));
			StartSound = Get<Sound>(rules.ReadString("StartSound"));
			Range = rules.ReadFloat("Range", 0);
			LineMultiplier = rules.ReadInt("LineMultiplier", 0);
			Type = rules.ReadType("Type", -1);
			PreDependent = rules.ReadType("PreDependent", -1);
			AuxBuilding = Get<BuildingType>(rules.ReadString("AuxBuilding"));
			UseChargeDrain = rules.ReadBool("UseChargeDrain");
			ManualControl = rules.ReadBool("ManualControl");
			RechargeTime = rules.ReadFloat("RechargeTime", 5.0f);
			SidebarImage = rules.ReadString("SidebarImage", "");
		
		

		}

	}
}
