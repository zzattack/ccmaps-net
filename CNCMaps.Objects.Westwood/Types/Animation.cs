using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CNCMaps.Engine.Types {
	public class Animation : ObjectType {
		// ONLY ART STUFF
		public bool Shadow;
		public Layer Layer;
		public bool AltPalette;
		public bool DoubleThick;
		public bool Flat;
		public bool Flamer;
		public bool Normalized;
		public bool Translucent;
		public bool Scorch;
		public bool Crater;
		public bool ForceBigCraters;
		public bool Sticky;
		public bool PingPong;
		public bool Reverse;
		public bool PsiWarning;
		public bool TiberiumChainReaction;
		public int Rate;
		public float Damage;
		public int Start;
		public int End;
		public int LoopStart;
		public int LoopEnd;
		public int LoopCount;
		public Animation Next;
		public int DetailLevel;
		public int TranslucencyDetailLevel;
		public Tuple<int, int> RandomLoopDelay;
		public int Translucency;
		public bool IsTiberium;
		public bool HideIfNoOre;
		public int YSortAdjust;
		public float Elasticity;
		public float MaxXYVel;
		public float MinZVel;
		public int MakeInfantry;
		public Animation Spawns;
		public int SpawnCount;
		public bool IsMeteor;
		public bool IsVeins;
		public int TiberiumSpreadRadius;
		public OverlayType TiberiumSpawnType;
		public bool IsAnimatedTiberium;
		public bool ShouldFogRemove;
		public bool IsFlamingGuy;
		public int RunningFrames;
		public int YDrawOffset;
		public int ZAdjust;
		public Sound StartSound;
		public Sound Report;
		public Sound StopSound;
		public Animation BounceAnim;
		public Animation ExpireAnim;
		public Animation TrailerAnim;
		public int TrailerSeperation;
		public int DamageRadius;
		public WarheadType Warhead;
		public bool Bouncer;
		public bool Tiled;
		public bool ShouldUseCellDrawer;
		public bool UseNormalLight;
		public ParticleType SpawnsParticle;
		public int NumParticles;
		public Tuple<int, int> RandomRate;
		
		public Animation(string ID) : base(ID) {}

		public override void LoadArt(FileFormats.IniFile.IniSection art) {
			base.LoadArt(art);
					
			Shadow = art.ReadBool("Shadow");
			Layer = art.ReadEnum("Layer", Layer.Ground);
			AltPalette = art.ReadBool("AltPalette");
			DoubleThick = art.ReadBool("DoubleThick");
			Flat = art.ReadBool("Flat");
			Flamer = art.ReadBool("Flamer");
			Normalized = art.ReadBool("Normalized");
			Translucent = art.ReadBool("Translucent");
			Scorch = art.ReadBool("Scorch");
			Crater = art.ReadBool("Crater");
			ForceBigCraters = art.ReadBool("ForceBigCraters");
			Sticky = art.ReadBool("Sticky");
			PingPong = art.ReadBool("PingPong");
			Reverse = art.ReadBool("Reverse");
			PsiWarning = art.ReadBool("PsiWarning");
			TiberiumChainReaction = art.ReadBool("TiberiumChainReaction");
			Rate = art.ReadInt("Rate", 1);
			Damage = art.ReadFloat("Damage");
			Start = art.ReadInt("Start");
			End = art.ReadInt("End");
			LoopStart = art.ReadInt("LoopStart");
			LoopEnd = art.ReadInt("LoopEnd");
			LoopCount = art.ReadInt("LoopCount");
			Next = Get<Animation>(art.ReadString("Next"));
			DetailLevel = art.ReadInt("DetailLevel");
			TranslucencyDetailLevel = art.ReadInt("TranslucencyDetailLevel");
			RandomLoopDelay = art.ReadXY("RandomLoopDelay");
			Translucency = art.ReadInt("Translucency");
			IsTiberium = art.ReadBool("IsTiberium");
			HideIfNoOre = art.ReadBool("HideIfNoOre");
			YSortAdjust = art.ReadInt("YSortAdjust");
			Elasticity = art.ReadFloat("Elasticity", 0.8f);
			MaxXYVel = art.ReadFloat("MaxXYVel", 2.71875f);
			MinZVel = art.ReadFloat("MinZVel", 2.1875f);
			MakeInfantry = art.ReadInt("MakeInfantry", -1);
			Spawns = Get<Animation>(art.ReadString("Spawns"));
			SpawnCount = art.ReadInt("SpawnCount");
			IsMeteor = art.ReadBool("IsMeteor");
			IsVeins = art.ReadBool("IsVeins");
			TiberiumSpreadRadius = art.ReadInt("TiberiumSpreadRadius");
			TiberiumSpawnType = Get<OverlayType>(art.ReadString("TiberiumSpawnType"));
			IsAnimatedTiberium = art.ReadBool("IsAnimatedTiberium");
			ShouldFogRemove = art.ReadBool("ShouldFogRemove", true);
			IsFlamingGuy = art.ReadBool("IsFlamingGuy");
			RunningFrames = art.ReadInt("RunningFrames");
			YDrawOffset = art.ReadInt("YDrawOffset");
			ZAdjust = art.ReadInt("ZAdjust");
			StartSound = Get<Sound>(art.ReadString("StartSound"));
			Report = Get<Sound>(art.ReadString("Report"));
			StopSound = Get<Sound>(art.ReadString("StopSound"));
			BounceAnim = Get<Animation>(art.ReadString("BounceAnim"));
			ExpireAnim = Get<Animation>(art.ReadString("ExpireAnim"));
			TrailerAnim = Get<Animation>(art.ReadString("TrailerAnim"));
			TrailerSeperation = art.ReadInt("TrailerSeperation", 0);
			DamageRadius = art.ReadInt("DamageRadius", 0);
			Warhead = Get<WarheadType>(art.ReadString("Warhead"));
			Bouncer = art.ReadBool("Bouncer");
			Tiled = art.ReadBool("Tiled");
			ShouldUseCellDrawer = art.ReadBool("ShouldUseCellDrawer", true);
			UseNormalLight = art.ReadBool("UseNormalLight");
			SpawnsParticle =Get<ParticleType>(art.ReadString("SpawnsParticle"));
			NumParticles = art.ReadInt("NumParticles");
			RandomRate = art.ReadXY("RandomRate");
		

		}

	}
}
