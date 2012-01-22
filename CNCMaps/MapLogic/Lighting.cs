using CNCMaps.FileFormats;
using CNCMaps.Utility;

namespace CNCMaps.MapLogic {
	public class Lighting {
		public double Level { get; private set; }
		public double Ambient { get; private set; }
		public double Red { get; private set; }
		public double Green { get; private set; }
		public double Blue { get; private set; }
		public double Ground { get; private set; }

		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public Lighting(IniFile.IniSection iniSection) {
			System.Diagnostics.Debug.Assert(iniSection.Name == "Lighting");
			Level = iniSection.ReadDouble("Level", 0.032);
			Ambient = iniSection.ReadDouble("Ambient", 1.0);
			Red = iniSection.ReadDouble("Red", 1.0);
			Green = iniSection.ReadDouble("Green", 1.0);
			Blue = iniSection.ReadDouble("Blue", 1.0);
			Ground = iniSection.ReadDouble("Ground", 0.0);

			logger.Trace("Lighting loaded: level: {0}, ambient: {1}, red: {2}, green: {3}, blue: {4}, ground: {5}",
				Level, Ambient, Red, Green, Blue, Ground);
		}

	}
}
