using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using CNCMaps.FileFormats;

namespace CNCMaps.MapLogic {

	public class Palette {
		public Color[] colors = new Color[256];
		PalFile originalPalette;
		byte[] origColors;

		bool hasLighting = false;
		double redMult = 1.0,
			greenMult = 1.0,
			blueMult = 1.0,
			ambientMult = 1.0;

		public Palette(PalFile originalPalette) {
			this.originalPalette = originalPalette;
		}

		public void ApplyLighting(Lighting l, int level) {
			redMult = l.Red;
			greenMult = l.Green;
			blueMult = l.Blue;
			ambientMult = (l.Ambient - l.Ground) + l.Level * level;
			hasLighting = true;
		}

		internal Palette Clone() {
			Palette p = (Palette)this.MemberwiseClone();
			p.colors = new Color[256];
			return p;
		}

		internal void ApplyLamp(LightSource lamp, double lsEffect) {
			ambientMult += lsEffect * lamp.LightIntensity;
			redMult += lsEffect * lamp.LightRedTint;
			greenMult += lsEffect * lamp.LightGreenTint;
			blueMult += lsEffect * lamp.LightBlueTint;
		}


		bool originalColorsLoaded = false;

		private void LoadOriginalColors() {
			if (this.originalPalette != null) {
				this.origColors = this.originalPalette.GetOriginalColors();
				originalColorsLoaded = true;
			}
		}

		public void Recalculate() {
			if (!originalColorsLoaded) 
				LoadOriginalColors();
			if (!originalColorsLoaded) return;

			ambientMult = Math.Min(Math.Max(ambientMult, -1.3), 1.3);
			redMult = Math.Min(Math.Max(redMult, -1.3), 1.3);
			greenMult = Math.Min(Math.Max(greenMult, -1.3), 1.3);
			blueMult = Math.Min(Math.Max(blueMult, -1.3), 1.3);

			for (int i = 0; i < 256; i++) {
				byte r = (byte)Math.Min(255, origColors[i * 3 + 0] * (ambientMult * redMult) / 63.0 * 255.0);
				byte g = (byte)Math.Min(255, origColors[i * 3 + 1] * (ambientMult * greenMult) / 63.0 * 255.0);
				byte b = (byte)Math.Min(255, origColors[i * 3 + 2] * (ambientMult * blueMult) / 63.0 * 255.0);
				colors[i] = Color.FromArgb(r, g, b);
			}
		}

		public static Palette MakePalette(Color c) {
			// be sure not to call recalculate on this
			Palette p = new Palette(null);
			for (int i = 0; i < 256; i++)
				p.colors[i] = c;
			p.originalColorsLoaded = true;
			return p;
		}

		public static Palette MergePalettes(Palette A, Palette B, double opacity) {
			// make sure recalculate has been called on A and B,
			// and be sure not to call recalculate on this
			Palette p = new Palette(null);
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
	}
}
