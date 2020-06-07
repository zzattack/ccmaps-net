namespace CNCMaps.Engine.Types {
	public class InfantryType : TechnoType {
		// ART
		public bool Crawls;
		public int FireUp;
		public int FireProne;
		public int SecondaryFire;
		public int SecondaryProne;
		public string SequenceName;

		public InfantryType(string ID) : base(ID) { }

		public override void LoadArt(FileFormats.IniFile.IniSection art) {
			base.LoadArt(art);

			Crawls = art.ReadBool("Crawls", true);
			FireUp= art.ReadInt("FireUp");
			FireProne= art.ReadInt("FireProne");
			SecondaryFire= art.ReadInt("SecondaryFire");
			SecondaryProne= art.ReadInt("SecondaryProne");
			SequenceName= art.ReadString("Sequence");
		}
	}
}
