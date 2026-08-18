using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CNCMaps.Engine.Rendering;
using NLog;

namespace CNCMaps.GUI {
	/// <summary>
	/// Probes the application directory for an optional preview window implementation
	/// (CNCMaps.Preview.dll, distributed separately) and instantiates it on demand.
	/// </summary>
	static class PreviewPlugin {
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
		private static bool _probed;
		private static Type _windowType;

		public static bool IsAvailable => File.Exists(Path.Combine(AppContext.BaseDirectory, "CNCMaps.Preview.dll"));

		public static IMapPreviewWindow TryCreate() {
			if (!_probed) {
				_probed = true;
				string path = Path.Combine(AppContext.BaseDirectory, "CNCMaps.Preview.dll");
				if (File.Exists(path)) {
					try {
						var asm = Assembly.LoadFrom(path);
						_windowType = asm.GetTypes().FirstOrDefault(t =>
							!t.IsAbstract && typeof(IMapPreviewWindow).IsAssignableFrom(t));
						if (_windowType == null)
							_logger.Warn("CNCMaps.Preview.dll contains no IMapPreviewWindow implementation");
					}
					catch (Exception exc) {
						// a broken plugin must never take the renderer down with it
						_logger.Error(exc, "Failed to load preview plugin");
					}
				}
			}

			if (_windowType == null)
				return null;
			try {
				return (IMapPreviewWindow)Activator.CreateInstance(_windowType);
			}
			catch (Exception exc) {
				_logger.Error(exc, "Failed to instantiate preview plugin");
				return null;
			}
		}
	}
}
