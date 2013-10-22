using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace CNCMaps.Engine.Types {
	public class ObjectType : AbstractType {
		public ObjectType(string ID) : base(ID) { }

		// Rules entries
		public string Image;
		public string AlphaImage;
		public string CrushSound;
		public string AmbientSound;
		public bool Crushable;
		public bool Bombable;
		public bool NoSpawnAlt;
		public bool AlternateArcticArt;
		public bool RadarInvisible;
		public bool Selectable;
		public bool LegalTarget;

		public string Armor;
		public int Strength;
		public bool Immune;
		public bool Insignificant;
		public bool HasRadialIndicator;
		public Color RadialColor;
		public bool IgnoresFirestorm;

		// Art entries
		public bool UseLineTrail;
		public Color LineTrailColor;
		public int LineTrailColorDecrement;

		public bool Theater;
		public bool NewTheater;
		public bool Voxel;

		public override void LoadRules(FileFormats.IniFile.IniSection rules) {
			base.LoadRules(rules);

			Image = rules.ReadString("Image");
			AlphaImage = rules.ReadString("AlphaImage");
			CrushSound = rules.ReadString("CrushSound");
			AmbientSound = rules.ReadString("AmbientSound");
			Crushable = rules.ReadBool("Crushable");
			Bombable = rules.ReadBool("Bombable");
			NoSpawnAlt = rules.ReadBool("NoSpawnAlt");
			AlternateArcticArt = rules.ReadBool("AlternateArcticArt");
			RadarInvisible = rules.ReadBool("RadarInvisible");
			Selectable = rules.ReadBool("Selectable");
			LegalTarget = rules.ReadBool("LegalTarget");

			Armor = rules.ReadString("Armor");
			Strength = rules.ReadInt("Strength");
			Immune = rules.ReadBool("Immune");
			Insignificant = rules.ReadBool("Insignificant");
			HasRadialIndicator = rules.ReadBool("HasRadialIndicator");
			RadialColor = rules.ReadColor("RadialColor");
			IgnoresFirestorm = rules.ReadBool("IgnoresFirestorm");
		}

		public override void LoadArt(FileFormats.IniFile.IniSection art) {
			base.LoadArt(art);
			UseLineTrail = art.ReadBool("UseLineTrail");
			LineTrailColor= art.ReadColor("LineTrailColor");
			LineTrailColorDecrement= art.ReadInt("LineTrailColorDecrement");
			Theater= art.ReadBool("Theater");
			NewTheater= art.ReadBool("NewTheater");
			Voxel= art.ReadBool("Voxel");
		}

	}


}
