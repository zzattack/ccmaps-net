using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		public LightSource() { }
		public LightSource(IniFile.IniSection lamp, Lighting scenario) {
			Initialize(lamp, scenario);
		}

		void Initialize(IniFile.IniSection lamp, Lighting scenario) {
			// Read and assume default values
			LightVisibility = lamp.ReadDouble("LightVisibility", 5000.0);
			LightIntensity = lamp.ReadDouble("LightIntensity", 0.0);
			LightRedTint = lamp.ReadDouble("LightRedtint", 1.0);
			LightGreenTint = lamp.ReadDouble("LightGreentint", 1.0);
			LightBlueTint = lamp.ReadDouble("LightBluetint", 1.0);
			this.scenario = scenario;
		}
	}
}