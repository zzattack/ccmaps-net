using System.Drawing;

namespace CNCMaps.Engine.Types {
	public class TerrainType : ObjectType {
		public TerrainType(string ID) : base(ID) { }

		// Rules
		public bool IsVeinhole;
		public bool WaterBound;
		public bool SpawnsTiberium;
		public bool IsFlammable;
		public Color RadarColor;
		public bool IsAnimated;
		public int AnimationRate;
		public float AnimationProbability;
		public int TemperateOccupationBits;
		public int SnowOccupationBits;

		// Art
		public Size Foundation;

		public override void LoadRules(FileFormats.IniFile.IniSection rules) {
			base.LoadRules(rules);

			IsVeinhole = rules.ReadBool("IsVeinhole");
			WaterBound = rules.ReadBool("WaterBound");
			SpawnsTiberium = rules.ReadBool("SpawnsTiberium");
			IsFlammable = rules.ReadBool("IsFlammable");
			RadarColor = rules.ReadColor("RadarColor");
			IsAnimated = rules.ReadBool("IsAnimated");
			AnimationRate = rules.ReadInt("AnimationRate");
			AnimationProbability = rules.ReadFloat("AnimationProbability");
			TemperateOccupationBits = rules.ReadInt("TemperateOccupationBits", 7);
			SnowOccupationBits = rules.ReadInt("SnowOccupationBits", 7);
		}

		public override void LoadArt(FileFormats.IniFile.IniSection art) {
			base.LoadArt(art);

			Foundation = art.ReadSize("Foundation", new Size(1, 1));
		}


	}
}
