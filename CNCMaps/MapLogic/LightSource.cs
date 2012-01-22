using CNCMaps.FileFormats;

namespace CNCMaps.MapLogic {
	public class LightSource : RA2Object {
		public double LightVisibility { get; set; }
		public double LightIntensity { get; set; }
		public double LightRedTint { get; set; }
		public double LightGreenTint { get; set; }
		public double LightBlueTint { get; set; }

		// not yet used
		Lighting scenario;

		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public LightSource() { }
		public LightSource(IniFile.IniSection lamp, Lighting scenario) {
			Initialize(lamp, scenario);
		}

		void Initialize(IniFile.IniSection lamp, Lighting scenario) {
			logger.Trace("Loading LightSource {0} at ({1},{2})", lamp.Name, Tile);

			// Read and assume default values
			LightVisibility = lamp.ReadDouble("LightVisibility", 5000.0);
			LightIntensity = lamp.ReadDouble("LightIntensity", 0.0);
			LightRedTint = lamp.ReadDouble("LightRedTint", 1.0);
			LightGreenTint = lamp.ReadDouble("LightGreenTint", 1.0);
			LightBlueTint = lamp.ReadDouble("LightBlueTint", 1.0);
			this.scenario = scenario;
		}
	}
}