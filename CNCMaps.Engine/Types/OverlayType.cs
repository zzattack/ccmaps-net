using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace CNCMaps.Engine.Types {
	public class OverlayType : ObjectType {
		public OverlayType(string ID) : base(ID) { }

		public LandType Land;
		public int Strength;
		public bool Wall;
		public bool Tiberium;
		public bool Crate;
		public bool CrateTrigger;
		public bool Explodes;
		public bool Overrides;
		public Animation CellAnim;
		public int DamageLevels;
		public Color RadarColor;
		public bool NoUseLandTileType;
		public bool IsVeinholeMonster;
		public bool IsVeins;
		public bool ChainReaction;
		public bool DrawFlat;
		public bool IsARock;
		public bool IsRubble;

		public override void LoadRules(FileFormats.IniFile.IniSection rules) {
			base.LoadRules(rules);

			Land = rules.ReadEnum("Land", LandType.Clear);
			Strength = rules.ReadInt("Strength", 1);
			Wall = rules.ReadBool("Wall");
			Tiberium = rules.ReadBool("Tiberium");
			Crate = rules.ReadBool("Crate");
			CrateTrigger = rules.ReadBool("CrateTrigger");
			Explodes = rules.ReadBool("Explodes");
			Overrides = rules.ReadBool("Overrides");
			CellAnim = Get<Animation>(rules.ReadString("CellAnim"));
			DamageLevels = rules.ReadInt("DamageLevels", 1);
			RadarColor = rules.ReadColor("RadarColor");
			NoUseLandTileType = rules.ReadBool("NoUseLandTileType", true);
			IsVeinholeMonster = rules.ReadBool("IsVeinholeMonster");
			IsVeins = rules.ReadBool("IsVeins");
			ChainReaction = rules.ReadBool("ChainReaction");
			DrawFlat = rules.ReadBool("DrawFlat", true);
			IsARock = rules.ReadBool("IsARock");
			IsRubble = rules.ReadBool("IsRubble");
		}


	}
}
