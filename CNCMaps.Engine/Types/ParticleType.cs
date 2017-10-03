using System.Collections.Generic;
using System.Drawing;
using OpenTK;

namespace CNCMaps.Engine.Types {
	public class ParticleType : ObjectType {

		// RULES only
		public List<Color> ColorList;
		public int MaxDC;
		public int MaxEC;
		public int Damage;
		public WarheadType Warhead;
		public int StartFrame;
		public int NumLoopFrames;
		public int Translucency;
		public int WindEffect;
		public float Velocity;
		public float Deacc;
		public int Radius;
		public bool DeleteOnStateLimit;
		public int EndStateAI;
		public int StartStateAI;
		public int StateAIAdvance;
		public int Translucent50State;
		public int Translucent25State;
		public bool Normalized;
		public float ColorSpeed;
		public int XVelocity;
		public int YVelocity;
		public int MinZVelocity;
		public int ZVelocityRange;
		public Vector3 NextParticleOffset;
		public Color StartColor1;
		public Color StartColor2;
		public int FinalDamageState;
		public ParticleType NextParticle;
		public Behaviour2 BehavesLike;

		public ParticleType(string ID) : base(ID) {}

		public override void LoadRules(FileFormats.IniFile.IniSection rules) {
			base.LoadRules(rules);
		}

	}

}
