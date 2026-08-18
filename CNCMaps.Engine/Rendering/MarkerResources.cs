using System.Collections.Concurrent;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CNCMaps.Engine.Rendering {
	/// <summary>Loads the start-position marker images embedded as raw PNG resources.</summary>
	static class MarkerResources {
		static readonly ConcurrentDictionary<string, Image<Rgba32>> Cache = new ConcurrentDictionary<string, Image<Rgba32>>();

		/// <summary>Returns the marker image for e.g. "aro_marker_1", or null if it does not exist.</summary>
		public static Image<Rgba32> Get(string name) {
			return Cache.GetOrAdd(name, n => {
				var asm = typeof(MarkerResources).Assembly;
				using (var s = asm.GetManifestResourceStream("CNCMaps.Engine.Resources." + n + ".png")) {
					return s == null ? null : Image.Load<Rgba32>(s);
				}
			});
		}
	}
}
