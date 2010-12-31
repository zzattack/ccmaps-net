using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CNCMaps.FileFormats;

namespace CNCMaps.MapLogic {
	public class Lighting {
		public double Level { get; private set; }
		public double Ambient { get; private set; }
		public double Red { get; private set; }
		public double Green { get; private set; }
		public double Blue { get; private set; }
		public double Ground { get; private set; }

		public Lighting(IniFile.IniSection iniSection) {
			System.Diagnostics.Debug.Assert(iniSection.Name == "Lighting");
			this.Level = iniSection.ReadDouble("Level", 0.032);
			this.Ambient = iniSection.ReadDouble("Ambient", 1.0);
			this.Red = iniSection.ReadDouble("Red", 1.0);
			this.Green = iniSection.ReadDouble("Green", 1.0);
			this.Blue = iniSection.ReadDouble("Blue", 1.0);
			this.Ground = iniSection.ReadDouble("Ground", 0.0);
		}

	}
}
