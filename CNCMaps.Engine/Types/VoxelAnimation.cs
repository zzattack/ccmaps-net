using CNCMaps.FileFormats;

namespace CNCMaps.Engine.Types {
	public class VoxelAnimation : Animation {
		public float MinAngularVelocity;
		public float MaxAngularVelocity;
		public int Duration;
		public float MaxZVel;
		public bool ShareBodyData;
		public bool ShareTurretData;
		public bool ShareBarrelData;
		public int VoxelIndex;
		public ParticleSystem AttachedSystem;
		public TechnoType ShareSource;

		public VoxelAnimation(string ID) : base(ID) { }

		public override void LoadRules(IniFile.IniSection rules) {
			base.LoadRules(rules);

			Normalized = rules.ReadBool("Normalized");
			Translucent = rules.ReadBool("Translucent");
			IsTiberium = rules.ReadBool("IsTiberium");
			IsMeteor = rules.ReadBool("IsMeteor");
			Elasticity = rules.ReadFloat("Elasticity", 0.8f);
			MinAngularVelocity = rules.ReadFloat("MinAngularVelocity");
			MaxAngularVelocity = rules.ReadFloat("MaxAngularVelocity", 0.174528f);
			Duration = rules.ReadInt("Duration", 30);
			MinZVel = rules.ReadFloat("MinZVel", 3.5f);
			MaxZVel = rules.ReadFloat("MaxZVel", 5f);
			MaxXYVel = rules.ReadFloat("MaxXYVel", 15f);
			Spawns = Get<VoxelAnimation>(rules.ReadString("Spawns"));
			SpawnCount = rules.ReadInt("SpawnCount");
			ShareBodyData = rules.ReadBool("ShareBodyData");
			ShareTurretData = rules.ReadBool("ShareTurretData");
			ShareBarrelData = rules.ReadBool("ShareBarrelData");
			VoxelIndex = rules.ReadInt("VoxelIndex");
			StartSound = Get<Sound>(rules.ReadString("StartSound"));
			StopSound = Get<Sound>(rules.ReadString("StopSound"));
			BounceAnim = Get<Animation>(rules.ReadString("BounceAnim"));
			ExpireAnim = Get<Animation>(rules.ReadString("ExpireAnim"));
			TrailerAnim = Get<Animation>(rules.ReadString("TrailerAnim"));
			Damage = rules.ReadInt("Damage");
			DamageRadius = rules.ReadInt("DamageRadius");
			Warhead = Get<WarheadType>(rules.ReadString("Warhead"));
			AttachedSystem = Get<ParticleSystem>(rules.ReadString("AttachedSystem"));
			ShareSource = Get<TechnoType>(rules.ReadString("ShareSource"));
		}
	}
}
