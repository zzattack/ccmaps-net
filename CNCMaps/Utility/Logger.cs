using System;
using System.Diagnostics;

namespace CNCMaps.Utility {
	public static class Logger {
		static Stopwatch sw;

		static Logger() {
			sw = new Stopwatch();
			sw.Start();
		}


		public static void WriteLine(string format, params object[] args) {
			Console.Write("{0} - ", sw.ElapsedMilliseconds);
			Console.WriteLine(string.Format(format, args));
		}
	}
}
