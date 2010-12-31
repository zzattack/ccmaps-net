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

		bool hasLighting = false;
		double redMult = 1.0,
			greenMult,
			blueMult,
			ambientMult;

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

		internal void Recalculate() {
			// read originalPalette
			originalPalette.Position = 0;
			byte[] origColors = originalPalette.Read(256*3);
			
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

	}
}
