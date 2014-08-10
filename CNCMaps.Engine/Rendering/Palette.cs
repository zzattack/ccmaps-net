using System;
using System.Drawing;
using System.IO;
using CNCMaps.Engine.Map;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.Map;

namespace CNCMaps.Engine.Rendering {

	public class Palette {
		public string Name { get; set; }

		public Color[] Colors = new Color[256];
		readonly PalFile _originalPalette;
		bool _originalColorsLoaded;
        // Starkku: Necessary to distinguish between object and theater/animation palettes when recalculating values.
        bool _objectPalette;
		byte[] _origColors;
		public bool IsShared { get; set; }

		double _redMult = 1.0,
			_greenMult = 1.0,
			_blueMult = 1.0,
			_ambientMult = 1.0;

		public Palette() {
			Name = "";
		}

		public Palette(PalFile originalPalette, string name = "", bool objectPalette = false) {
			_originalPalette = originalPalette;
            _objectPalette = objectPalette;
			if (!string.IsNullOrEmpty(name))
				Name = name;
			else
				Name = Path.GetFileNameWithoutExtension(originalPalette.FileName);
		}

		public Palette(byte[] colors, string name) {
			_origColors = colors;
			Name = name;
		}

		internal Palette Clone() {
			var p = (Palette)MemberwiseClone();
			p.Colors = new Color[256];
			p.IsShared = false;
			return p;
		}

		public void ApplyLighting(Lighting l, int level = 0, bool applyTints = true) {
			_ambientMult = (l.Ambient + l.Ground) + l.Level * level;
			if (applyTints) {
				_redMult = l.Red;
				_greenMult = l.Green;
				_blueMult = l.Blue;
			}
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

            // Starkku: What is the purpose of this? Can cause weird discoloration issues when you hit this ceiling when recalculating palettes f.ex
            // from light sources, something that does not happen in the game (it lightens stuff up until it's near white and so on.
            const double clipMult = Double.MaxValue; //1.3;
			_ambientMult = Math.Min(Math.Max(_ambientMult, 0), clipMult);
			_redMult = Math.Min(Math.Max(_redMult, 0), clipMult);
			_greenMult = Math.Min(Math.Max(_greenMult, 0), clipMult);
			_blueMult = Math.Min(Math.Max(_blueMult, 0), clipMult);
            double rmult, gmult, bmult;
			for (int i = 0; i < 256; i++) {
                rmult = _ambientMult * _redMult;
                gmult = _ambientMult * _greenMult;
                bmult = _ambientMult * _blueMult;
                // Starkku: For object palettes colors 240-254 do not get any lighting applied on them.
                if (i >= 240 && i <= 254 && _objectPalette) 
                {
                    rmult = gmult = bmult = 1.0;
                }
				var r = (byte)Math.Min(255, _origColors[i * 3 + 0] * (rmult) / 63.0 * 255.0);
                var g = (byte)Math.Min(255, _origColors[i * 3 + 1] * (gmult) / 63.0 * 255.0);
                var b = (byte)Math.Min(255, _origColors[i * 3 + 2] * (bmult) / 63.0 * 255.0);
				Colors[i] = Color.FromArgb(r, g, b);
			}
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


		internal void Remap(Color color) {
			if (!_originalColorsLoaded)
				LoadOriginalColors();
			double[] mults = { 0xFC >> 2, 0xEC >> 2, 0xDC >> 2, 0xD0 >> 2,
						0xC0 >> 2, 0xB0 >> 2, 0xA4 >> 2, 0x94 >> 2,
						0x84 >> 2, 0x78 >> 2, 0x68 >> 2, 0x58 >> 2,
						0x4C >> 2, 0x3C >> 2, 0x2C >> 2, 0x20 >> 2 };

			for (int i = 16; i < 32; i++) {
				_origColors[i * 3 + 0] = (byte)(color.R / 255.0 * mults[i - 16]);
				_origColors[i * 3 + 1] = (byte)(color.G / 255.0 * mults[i - 16]);
				_origColors[i * 3 + 2] = (byte)(color.B / 255.0 * mults[i - 16]);
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
