using CNCMaps.Engine;
using NLog;

namespace CNCMaps {
	class Program {
		public static int Main(string[] args) {
			var engine = new RenderEngine();
			if (engine.ConfigureFromArgs(args)) {
				var result = engine.Execute();
				LogManager.Configuration = null; // required for mono release to flush possible targets
				return (int)result;
			}
			return 0;
		}
	}
}