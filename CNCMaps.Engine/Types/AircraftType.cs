namespace CNCMaps.Engine.Types {
	public class AircraftType : TechnoType {
		// RULES
		public bool Landable;
		public bool AirportBound;
		public bool Fighter;
		public bool Carryall;
		public bool FlyBy;
		public bool FlyBack;

		// ART 
		public bool Rotors;
		public bool CustomRotor;
		public Animation Trailer;
		public int SpawnDelay;

		public AircraftType(string ID) : base(ID) {}

		public override void LoadRules(FileFormats.IniFile.IniSection rules) {
			base.LoadRules(rules);
			Landable = rules.ReadBool("Landable");
			AirportBound= rules.ReadBool("AirportBound");
			Fighter= rules.ReadBool("Fighter");
			Carryall= rules.ReadBool("Carryall");
			FlyBy= rules.ReadBool("FlyBy");
			FlyBack= rules.ReadBool("FlyBack");
		}

		public override void LoadArt(FileFormats.IniFile.IniSection art) {
			base.LoadArt(art);
			Rotors = art.ReadBool("Rotors");
			CustomRotor = art.ReadBool("CustomRotor");
			Trailer = TypesRepository.GetAnimType(art.ReadString("Trailer"));
			SpawnDelay = art.ReadInt("SpawnDelay", 3);
		}

	}
}
