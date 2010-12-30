// from http://jinxbot.googlecode.com/svn

using System;
using System.Drawing;

namespace CNCMaps.Utility {
	// My HSV considers Hue Sat and Val as values from 0 - 255.
	public class HsvColor {
		private int hue;
		private int sat;
		private int val;
		public HsvColor(int h, int s, int v) {
			hue = h; sat = s; val = v;
		}
		public HsvColor(Color color) {
			hue = 0; sat = 0; val = 0; FromRGB(color);
		}
		public int Hue {
			get { return hue; }
			set { hue = value; }
		}
		public int Saturation {
			get { return sat; }
			set { sat = value; }
		}
		public int Value {
			get { return val; }
			set { val = value; }
		}
		public Color Color {
			get { return ToRGB(); }
			set { FromRGB(value); }
		}
		private void FromRGB(Color color) {
			double min; double max; double delta;
			double r = (double)color.R / 255D;
			double g = (double)color.G / 255D;
			double b = (double)color.B / 255D;
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

			h = ((double)Hue / 255D * 360D) % 360D;
			s = (double)Saturation / 255D;
			v = (double)Value / 255D;

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

				sectorPos = h / 60D;
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
			return Color.FromArgb((int)(r * 255D), (int)(g * 255D), (int)(b * 255D));
		}
		public static bool operator !=(HsvColor left, HsvColor right) {
			return !(left == right);
		}
		public static bool operator ==(HsvColor left, HsvColor right) {
			return (left.Hue == right.Hue && left.Value == right.Value && left.Saturation == right.Saturation);
		}
	}
}
