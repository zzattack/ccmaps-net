using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using CNCMaps.Encodings;
using CNCMaps.FileFormats;

namespace CNCMaps.Utility {
	class ThumbInjector {
		public static unsafe void InjectThumb(Bitmap preview, IniFile map) {

			preview.Save("C:\\soms.png");
			BitmapData bmd = preview.LockBits(new Rectangle(0, 0, preview.Width, preview.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
			byte[] image = new byte[preview.Width * preview.Height * 3];
			int idx = 0;

			// invert rgb->bgr
			for (int y = 0; y < bmd.Height; y++) {
				byte* row = (byte*)bmd.Scan0 + bmd.Stride * y;
				byte* p = row;
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

			// now overwrite [PreviewPack]
			var section = map.GetOrCreateSection("PreviewPack");
			section.Clear();

			int rowNum = 1;
			for (int i = 0; i < image_base64.Length; i += 70) {
				section.SetValue(rowNum++.ToString(CultureInfo.InvariantCulture), image_base64.Substring(i, Math.Min(70, image_base64.Length - i)));
			}

			map.GetOrCreateSection("Preview").SetValue("Size", string.Format("0,0,{0},{1}", preview.Width, preview.Height));
		}
	}
}
