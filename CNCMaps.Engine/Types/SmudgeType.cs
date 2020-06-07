namespace CNCMaps.Engine.Types {
	public class SmudgeType : ObjectType {
		public bool Crater;
		public bool Burn;
		public int Width;
		public int Height;

		public SmudgeType(string ID) : base(ID) { }

		public override void LoadRules(FileFormats.IniFile.IniSection rules) {
			Crater = rules.ReadBool("Crater");
			Burn = rules.ReadBool("Burn");
			Width = rules.ReadInt("Width", 1);
			Height = rules.ReadInt("Height", 1);
		}
	}
}
