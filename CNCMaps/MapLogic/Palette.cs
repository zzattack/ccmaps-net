using System;
using System.Drawing;
using System.IO;
using CNCMaps.FileFormats;

namespace CNCMaps.MapLogic {

	public class Palette {
		// Starkku: Name of the palette file, without .pal extension. Mostly for debug purposes, but can also be used to identify certain types of palettes semi-reliably.
		public string Name { get; set; }

		public Color[] colors = new Color[256];
		PalFile originalPalette;
		byte[] origColors;
		public bool IsShared { get; set; }

		double redMult = 1.0,
			greenMult = 1.0,
			blueMult = 1.0,
			ambientMult = 1.0;

		/// <summary>
		/// Starkku: Creates a palette out of already existing palette file.
		/// </summary>
		/// <param name="originalPalette">Original palette file.</param>
		/// <param name="name">An optional custom name for the palette. Defaults to the filename of the palette without file extension.</param>
		public Palette(PalFile originalPalette, string name = "") {
			this.originalPalette = originalPalette;
			if (!string.IsNullOrEmpty(name))
				this.Name = name;
			else
				Name = Path.GetFileNameWithoutExtension(originalPalette.FileName);
		}

		internal Palette Clone() {
			var p = (Palette)MemberwiseClone();
			p.colors = new Color[256];
			p.IsShared = false;
			return p;
		}

		public void ApplyLighting(Lighting l, int level = 0, bool ambientOnly = false) {
			if (!ambientOnly) {
				redMult = l.Red;
				greenMult = l.Green;
				blueMult = l.Blue;
			}
			ambientMult = (l.Ambient - l.Ground) + l.Level * level;
		}

		public void ApplyLamp(LightSource lamp, double lsEffect) {
			ambientMult += lsEffect * lamp.LightIntensity;
			redMult += lsEffect * lamp.LightRedTint;
			greenMult += lsEffect * lamp.LightGreenTint;
			blueMult += lsEffect * lamp.LightBlueTint;
		}


		bool originalColorsLoaded;

		private void LoadOriginalColors() {
			if (originalPalette != null) {
				origColors = originalPalette.GetOriginalColors();
				originalColorsLoaded = true;
			}
		}

		public void Recalculate() {
			if (!originalColorsLoaded) LoadOriginalColors();
			if (!originalColorsLoaded) return;

			const double clipMult = 1.3;
			ambientMult = Math.Min(Math.Max(ambientMult, -clipMult), clipMult);
			redMult = Math.Min(Math.Max(redMult, -clipMult), clipMult);
			greenMult = Math.Min(Math.Max(greenMult, -clipMult), clipMult);
			blueMult = Math.Min(Math.Max(blueMult, -clipMult), clipMult);
			for (int i = 0; i < 256; i++) {
				var r = (byte)Math.Min(255, origColors[i * 3 + 0] * (ambientMult * redMult) / 63.0 * 255.0);
				var g = (byte)Math.Min(255, origColors[i * 3 + 1] * (ambientMult * greenMult) / 63.0 * 255.0);
				var b = (byte)Math.Min(255, origColors[i * 3 + 2] * (ambientMult * blueMult) / 63.0 * 255.0);
				colors[i] = Color.FromArgb(r, g, b);
			}
		}

		public static Palette MakePalette(Color c) {
			// be sure not to call recalculate on this
			var p = new Palette(null);
			for (int i = 0; i < 256; i++)
				p.colors[i] = c;
			p.originalColorsLoaded = true;
			return p;
		}

		public static Palette Merge(Palette A, Palette B, double opacity) {
			// make sure recalculate has been called on A and B,
			// and be sure not to call recalculate on this
			var p = new Palette(null);
			for (int i = 0; i < 256; i++)
				p.colors[i] = Color.FromArgb(
					(int)(A.colors[i].R * opacity + B.colors[i].R * (1.0 - opacity)),
					(int)(A.colors[i].G * opacity + B.colors[i].G * (1.0 - opacity)),
					(int)(A.colors[i].B * opacity + B.colors[i].B * (1.0 - opacity)));
			return p;
		}


		internal void Remap(Color color) {
			if (!originalColorsLoaded)
				LoadOriginalColors();
			double[] mults = { 0xFC >> 2, 0xEC >> 2, 0xDC >> 2, 0xD0 >> 2,
						0xC0 >> 2, 0xB0 >> 2, 0xA4 >> 2, 0x94 >> 2,
						0x84 >> 2, 0x78 >> 2, 0x68 >> 2, 0x58 >> 2,
						0x4C >> 2, 0x3C >> 2, 0x2C >> 2, 0x20 >> 2 };

			for (int i = 16; i < 32; i++) {
				origColors[i * 3 + 0] = (byte)(color.R / 255.0 * mults[i - 16]);
				origColors[i * 3 + 1] = (byte)(color.G / 255.0 * mults[i - 16]);
				origColors[i * 3 + 2] = (byte)(color.B / 255.0 * mults[i - 16]);
			}
		}

		internal Lighting GetLighting(bool ambientOnly = false) {
			if (!ambientOnly)
				return new Lighting {
					Ambient = ambientMult,
					Red = redMult,
					Green = greenMult,
					Blue = blueMult,
					Ground = 0,
					Level = 0,
				};
			return new Lighting {
				Ambient = ambientMult,
				Red = 1.0,
				Green = 1.0,
				Blue = 1.0,
				Ground = 0,
				Level = 0,
			};
		}

	}
}
