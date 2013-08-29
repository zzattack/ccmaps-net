using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using CNCMaps.Encodings;
using CNCMaps.FileFormats;

namespace CNCMaps.Utility {
	class ThumbInjector {
		public static unsafe void InjectThumb(Bitmap preview, IniFile map) {
			BitmapData bmd = preview.LockBits(new Rectangle(0, 0, preview.Width, preview.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
			byte[] image = new byte[preview.Width * preview.Height * 3];
			int idx = 0;

			// invert rgb->bgr
			for (int y = 0; y < bmd.Height; y++) {
				byte* p = (byte*)bmd.Scan0 + bmd.Stride * y;
				for (int x = 0; x < bmd.Width; x++) {
					byte r = *p++;
					byte g = *p++;
					byte b = *p++;

					image[idx++] = b;
					image[idx++] = g;
					image[idx++] = r;
				}
			}

			// encode
			byte[] image_compressed = Format5.Encode(image, 5);
			
			// base64 encode
			string image_base64 = Convert.ToBase64String(image_compressed, Base64FormattingOptions.None);

			// now overwrite [Preview] and [PreviewPack], inserting them directly after [Basic] if not yet existing
			map.GetOrCreateSection("Preview").SetValue("Size", string.Format("0,0,{0},{1}", preview.Width, preview.Height));

			var section = map.GetOrCreateSection("PreviewPack", "Preview");
			section.Clear();

			int rowNum = 1;
			for (int i = 0; i < image_base64.Length; i += 70) {
				section.SetValue(rowNum++.ToString(CultureInfo.InvariantCulture), image_base64.Substring(i, Math.Min(70, image_base64.Length - i)));
			}

		}

		public static unsafe Bitmap ExtractThumb(IniFile map) {
			var prevSection = map.GetSection("Preview");
			var size = prevSection.ReadString("Size").Split(',');
			var previewSize = new Rectangle(int.Parse(size[0]), int.Parse(size[1]), int.Parse(size[2]), int.Parse(size[3]));
			var preview = new Bitmap(previewSize.Width, previewSize.Height, PixelFormat.Format24bppRgb);

			byte[] image = new byte[preview.Width * preview.Height * 3];
			var prevDataSection = map.GetSection("PreviewPack");
			var image_compressed = Convert.FromBase64String(prevDataSection.ConcatenatedValues());
			Format5.DecodeInto(image_compressed, image, 5);

			// invert rgb->bgr
			BitmapData bmd = preview.LockBits(new Rectangle(0, 0, preview.Width, preview.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
			int idx = 0;
			for (int y = 0; y < bmd.Height; y++) {
				byte* row = (byte*)bmd.Scan0 + bmd.Stride * y;
				byte* p = row;
				for (int x = 0; x < bmd.Width; x++) {
					byte b = image[idx++];
					byte g = image[idx++];
					byte r = image[idx++];
					*p++ = r;
					*p++ = g;
					*p++ = b;
				}
			}

			preview.UnlockBits(bmd);
			return preview;
		}
	}
}
