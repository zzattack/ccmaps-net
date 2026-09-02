using System;
using System.Drawing;
using System.IO;
using CNCMaps.Engine.Map;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.Map;
using CNCMaps.Engine.Utility;

namespace CNCMaps.Engine.Rendering {

	public class Palette {
		public string Name { get; set; }

		public Color[] Colors = new Color[256];
		readonly PalFile _originalPalette;
		bool _originalColorsLoaded;
		bool _isObjectPalette; // Necessary to distinguish between object and theater/animation palettes when recalculating values.
		byte[] _origColors;
		public bool IsShared { get; set; }

		/// <summary>Quantize lighting to the 63 intensity steps the engine draws through.
		/// Always on; kept as a switch so a render can be compared against the continuous maths.</summary>
		public static bool QuantizeIntensity { get; set; }

		byte[] _bgr; // per-pixel loops read colors as raw bytes; Color's property accessors are too slow there

		double _redMult = 1.0,
			_greenMult = 1.0,
			_blueMult = 1.0,
			_ambientMult = 1.0;

		public Palette() {
			Name = "";
		}

		public Palette(PalFile originalPalette, string name = "", bool objectPalette = false) {
			try {
				_originalPalette = originalPalette;
				_isObjectPalette = objectPalette;
				if (!string.IsNullOrEmpty(name))
					Name = name;
				else
					Name = Path.GetFileNameWithoutExtension(originalPalette.FileName);
			}
			catch (Exception) {

				throw;
			}
		}

		public Palette(byte[] colors, string name) {
			_origColors = colors;
			Name = name;
		}

		internal Palette Clone() {
			var p = (Palette)MemberwiseClone();
			p.Colors = new Color[256];
			p._bgr = null;
			p.IsShared = false;
			return p;
		}

		/// <summary>Colors as B,G,R triplets, rebuilt after every Recalculate.</summary>
		public byte[] GetBgrBytes() {
			if (_bgr == null) {
				_bgr = new byte[768];
				for (int i = 0; i < 256; i++) {
					_bgr[i * 3 + 0] = Colors[i].B;
					_bgr[i * 3 + 1] = Colors[i].G;
					_bgr[i * 3 + 2] = Colors[i].R;
				}
			}
			return _bgr;
		}

		public void ApplyLighting(Lighting l, int level = 0, bool applyTints = true) {
			_ambientMult = l.Ambient - l.Ground + l.Level * level;
			if (applyTints) {
				_redMult = l.Red;
				_greenMult = l.Green;
				_blueMult = l.Blue;
			}
		}

		// The engine does not scale colors by the light level. It draws every shape through a
		// LightConvertClass whose table holds 63 intensity steps from black to double brightness, and
		// the cell's brightness picks one of them, so a cell's lighting always lands on a multiple of
		// 1/31. The chain below is the game's own: Draw_Tile hands the brightness to
		// AlphaLightingRemapClass::Get_Table, whose row index is (261*brightness)>>11, and the table
		// entry is (alpha * shade * 62) / 32258 with alpha at its neutral 127. RA2 and YR use the same
		// machinery.
		private static double QuantizeTsIntensity(double intensity) {
			int brightness = (int)(intensity * 1000);
			if (brightness < 0) brightness = 0;
			if (brightness > 2000) brightness = 2000;
			int shade = Math.Min(254, (261 * brightness) >> 11);
			int level = Math.Min(62, 127 * shade * 62 / 32258);
			return level / 31.0;
		}

		/// <summary>Adds a flat intensity, in the game's per-mille units divided by 1000.</summary>
		public void AddLight(double intensity) {
			_ambientMult += intensity;
		}

		public void ApplyLamp(LightSource lamp, double lsEffect, bool ambientOnly = false) {
			_ambientMult += lsEffect * lamp.LightIntensity;
			if (!ambientOnly) {
				_redMult += lsEffect * lamp.LightRedTint;
				_greenMult += lsEffect * lamp.LightGreenTint;
				_blueMult += lsEffect * lamp.LightBlueTint;
			}
		}


		private void LoadOriginalColors() {
			if (!_originalColorsLoaded && _originalPalette != null) {
				_origColors = _originalPalette.GetOriginalColors();
				_originalColorsLoaded = true;
			}
		}

		public void Recalculate() {
			if (!_originalColorsLoaded) LoadOriginalColors();
			if (!_originalColorsLoaded) return;

			// gamemd (CellClass::ComputeLighting 0x484180, normalize 0x5558E0): the ambient sum and each
			// tint sum clamp to [0,2]; the tint triple is normalized so its max channel becomes 1, the max
			// goes into the intensity, and that clamps to [0,2] again. Without a binding clamp this is the
			// plain per-channel product; the clamps cap the dominant channel's gain at 2x, which keeps
			// stacked or negative lamps from discoloring.
			double amb = Math.Min(Math.Max(_ambientMult, 0), 2.0);
			double tr = Math.Min(Math.Max(_redMult, 0), 2.0);
			double tg = Math.Min(Math.Max(_greenMult, 0), 2.0);
			double tb = Math.Min(Math.Max(_blueMult, 0), 2.0);
			double m = Math.Max(tr, Math.Max(tg, tb));
			double rmult, gmult, bmult;
			if (m < 0.001) {
				rmult = gmult = bmult = 0;
			}
			else {
				double intensity = Math.Min(amb * m, 2.0);
				if (QuantizeIntensity)
					intensity = QuantizeTsIntensity(intensity);
				rmult = intensity * (tr / m);
				gmult = intensity * (tg / m);
				bmult = intensity * (tb / m);
			}
			for (int i = 0; i < 256; i++) {
				double rm = rmult, gm = gmult, bm = bmult;
				// For object palettes colors 240-254 do not get any lighting applied on them.
				if (i >= 240 && i <= 254 && _isObjectPalette) {
					rm = gm = bm = 1.0;
				}
				var r = (byte)Math.Min(255, _origColors[i * 3 + 0] * rm / 63.0 * 255.0);
				var g = (byte)Math.Min(255, _origColors[i * 3 + 1] * gm / 63.0 * 255.0);
				var b = (byte)Math.Min(255, _origColors[i * 3 + 2] * bm / 63.0 * 255.0);
				Colors[i] = Color.FromArgb(r, g, b);
			}
			_bgr = null;
		}

		public static Palette MakePalette(Color c) {
			// be sure not to call recalculate on this
			var p = new Palette();
			for (int i = 0; i < 256; i++)
				p.Colors[i] = c;
			p._originalColorsLoaded = true;
			return p;
		}

		/// <param name="opacity">how much to retain of the first palette (range 0-1)</param>
		public static Palette Merge(Palette A, Palette B, double opacity) {
			// make sure recalculate has been called on A and B,
			// and be sure not to call recalculate on this
			var p = new Palette();
			for (int i = 0; i < 256; i++)
				p.Colors[i] = Color.FromArgb(
					(int)(A.Colors[i].R * opacity + B.Colors[i].R * (1.0 - opacity)),
					(int)(A.Colors[i].G * opacity + B.Colors[i].G * (1.0 - opacity)),
					(int)(A.Colors[i].B * opacity + B.Colors[i].B * (1.0 - opacity)));
			return p;
		}


		// The 16 remap shades a house colour gets, in palette indices 16-31. gamemd builds them at
		// 0x0068C3B0: the colour's hue is kept, its saturation swept up a sine and its value swept
		// down a cosine, so a shade grows more saturated as it darkens. Both sweeps end at pi/2, so
		// shade 15 is black. The angles are the binary's own doubles; in degrees they run 20 + 14i/3
		// for the value, overridden to 11.25 at i=0, and 50 + 8i/3 for the saturation.
		internal void Remap(HsvColor color) {
			if (!_originalColorsLoaded)
				LoadOriginalColors();

			for (int i = 0; i < 16; i++) {
				double value = i == 0 ? 0.19634954084936207
					: i * 0.08144869842640204 + 0.3490658503988659;
				double saturation = i * 0.046542113386515455 + 0.8726646259971648;
				var shade = EngineHsvToRgb(color.Hue,
					(int)(Math.Sin(saturation) * color.Saturation),
					(int)(Math.Cos(value) * color.Value));
				// The palette is six bit; the engine's conversion hands back eight.
				_origColors[(16 + i) * 3 + 0] = (byte)(shade.R * 63 / 255);
				_origColors[(16 + i) * 3 + 1] = (byte)(shade.G * 63 / 255);
				_origColors[(16 + i) * 3 + 2] = (byte)(shade.B * 63 / 255);
			}
		}

		// The engine's own HSV conversion (0x00517440), not the floating point one on HsvColor: it
		// splits the hue on 255 rather than 256 or 360 and truncates every intermediate, which moves
		// a shade a unit or two against a textbook conversion.
		private static Color EngineHsvToRgb(int h, int s, int v) {
			int sector = h * 6 / 255, frac = h * 6 % 255;
			int p = (255 - s) * v / 255;
			int q = (255 - frac * s / 255) * v / 255;
			int t = (255 - (255 - frac) * s / 255) * v / 255;
			switch (sector) {
				case 1: return Color.FromArgb(q, v, p);
				case 2: return Color.FromArgb(p, v, t);
				case 3: return Color.FromArgb(p, q, v);
				case 4: return Color.FromArgb(t, p, v);
				case 5: return Color.FromArgb(v, p, q);
				default: return Color.FromArgb(v, t, p); // 0, and 6 when the hue is 255
			}
		}

		internal Lighting GetLighting(bool ambientOnly = false) {
			if (!ambientOnly)
				return new Lighting {
					Ambient = _ambientMult,
					Red = _redMult,
					Green = _greenMult,
					Blue = _blueMult,
					Ground = 0,
					Level = 0,
				};
			return new Lighting {
				Ambient = _ambientMult,
				Red = 1.0,
				Green = 1.0,
				Blue = 1.0,
				Ground = 0,
				Level = 0,
			};
		}

	}
}
