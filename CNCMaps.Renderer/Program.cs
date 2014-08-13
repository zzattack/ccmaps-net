using CNCMaps.Engine;
using NLog;

namespace CNCMaps {
	class Program {
		public static int Main(string[] args) { 
			var engineSettings = new EngineSettings();
			engineSettings.ConfigureFromArgs(args);
			int retVal = engineSettings.Execute();
			LogManager.Configuration = null; // required for mono release to flush possible targets
			return retVal;
		}
	}
}