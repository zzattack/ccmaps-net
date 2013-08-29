using System;
using CNCMaps.FileFormats;

namespace CNCMaps.MapLogic {
	public class LightSource : GameObject {
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

		/// <summary>
		/// Applies a lamp to this object's palette if it's in range
		/// </summary>
		/// <param name="lamp">The lamp to apply</param>
		/// <returns>Whether the palette was replaced, meaning it needs to be recalculated</returns>
		public bool ApplyLamp(GameObject obj, bool ambientOnly = false) {
			var lamp = this;
			if (lamp.LightIntensity == 0.0)
				return false;

			var drawLocation = obj.Tile;
			double sqX = (lamp.Tile.Rx - drawLocation.Rx) * (lamp.Tile.Rx - drawLocation.Rx);
			double sqY = (lamp.Tile.Ry - (drawLocation.Ry)) * (lamp.Tile.Ry - (drawLocation.Ry));

			double distance = Math.Sqrt(sqX + sqY);

			// checks whether we're in range
			if ((0 < lamp.LightVisibility) && (distance < lamp.LightVisibility / 256)) {
				double lsEffect = (lamp.LightVisibility - 256 * distance) / lamp.LightVisibility;

				// we don't want to apply lamps to shared palettes, so clone first
				if (obj.Palette.IsShared)
					obj.Palette = obj.Palette.Clone();

				obj.Palette.ApplyLamp(lamp, lsEffect, ambientOnly);
				return true;
			}
			else
				return false;
		}
	}
}