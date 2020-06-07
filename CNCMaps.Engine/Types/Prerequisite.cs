namespace CNCMaps.Engine.Types {
	public class Prerequisite : AbstractType {
		public enum PrequisiteValues {
			POWER, // (corresponds to PrerequisitePower)
			PROC, // (corresponds to PrerequisiteProc and PrerequisiteProcAlternateYro.png)
			BARRACKS, // (corresponds to PrerequisiteBarracks)
			FACTORY, // (corresponds to PrerequisiteFactory)
			RADAR, // (corresponds to PrerequisiteRadar)
			TECH, // (corresponds to PrerequisiteTech)
			GDIFACTORY, // (corresponds to PrerequisiteGDIFactory)
			NODFACTORY, // (corresponds to PrerequisiteNodFactory)
		}
		public Prerequisite(string s) : base(s) {
			// todo
		}
	}
}