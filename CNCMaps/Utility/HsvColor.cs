// from http://jinxbot.googlecode.com/svn

using System;
using System.Drawing;

namespace CNCMaps.Utility {

	// My HSV considers Hue Sat and Val as values from 0 - 255.
	public class HsvColor {

		public HsvColor(int h, int s, int v) {
			Hue = h; Saturation = s; Value = v;
		}

		public HsvColor(Color color) {
			Hue = 0;
			Saturation = 0;
			Value = 0;
			FromRGB(color);
		}

		public int Hue { get; set; }

		public int Saturation { get; set; }

		public int Value { get; set; }

		public Color Color {
			get { return ToRGB(); }
			set { FromRGB(value); }
		}

		private void FromRGB(Color color) {
			double min; double max; double delta;
			double r = color.R / 255D;
			double g = color.G / 255D;
			double b = color.B / 255D;
			double h; double s; double v;

			min = Math.Min(Math.Min(r, g), b);
			max = Math.Max(Math.Max(r, g), b);
			v = max;
			delta = max - min;
			if (max == 0 || delta == 0) {
				s = 0;
				h = 0;
			}
			else {
				s = delta / max;
				if (r == max) {
					h = (60D * ((g - b) / delta)) % 360D;
				}
				else if (g == max) {
					h = 60D * ((b - r) / delta) + 120D;
				}
				else {
					h = 60D * ((r - g) / delta) + 240D;
				}
			}
			if (h < 0) {
				h += 360D;
			}

			Hue = (int)(h / 360D * 255D);
			Saturation = (int)(s * 255D);
			Value = (int)(v * 255D);
		}

		public Color ToRGB() {
			double h;
			double s;
			double v;
			double r = 0;
			double g = 0;
			double b = 0;

			h = (Hue / 255.0 * 360.0) % 360.0;
			s = Saturation / 255.0;
			v = Value / 255.0;

			if (s == 0) {
				r = v;
				g = v;
				b = v;
			}
			else {
				double p;
				double q;
				double t;

				double fractionalSector;
				int sectorNumber;
				double sectorPos;

				sectorPos = h / 60.0;
				sectorNumber = (int)(Math.Floor(sectorPos));

				fractionalSector = sectorPos - sectorNumber;

				p = v * (1D - s);
				q = v * (1D - (s * fractionalSector));
				t = v * (1D - (s * (1D - fractionalSector)));

				switch (sectorNumber) {
					case 0: r = v; g = t; b = p; break;
					case 1: r = q; g = v; b = p; break;
					case 2: r = p; g = v; b = t; break;
					case 3: r = p; g = q; b = v; break;
					case 4: r = t; g = p; b = v; break;
					case 5: r = v; g = p; b = q; break;
				}
			}
			return Color.FromArgb((int)(r * 255.0), (int)(g * 255.0), (int)(b * 255.0));
		}
	}
}