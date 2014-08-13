using CNCMaps.Engine;

namespace CNCMaps {
	class Program {
		public static int Main(string[] args) { 
			var engineSettings = new EngineSettings();
			engineSettings.ConfigureFromArgs(args);
			return engineSettings.Execute();
		}
	}
}