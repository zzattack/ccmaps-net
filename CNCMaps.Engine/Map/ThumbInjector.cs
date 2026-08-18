using System;
using System.Globalization;
using System.Runtime.InteropServices;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.Encodings;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CNCMaps.Engine.Map {
	class ThumbInjector {
		public static void InjectThumb(Image<Bgr24> preview, IniFile map) {
			byte[] image = new byte[preview.Width * preview.Height * 3];

			// the game stores the preview as RGB
			preview.ProcessPixelRows(accessor => {
				int idx = 0;
				for (int y = 0; y < accessor.Height; y++) {
					var row = MemoryMarshal.AsBytes(accessor.GetRowSpan(y));
					for (int x = 0; x < accessor.Width; x++) {
						image[idx++] = row[x * 3 + 2]; // r
						image[idx++] = row[x * 3 + 1]; // g
						image[idx++] = row[x * 3 + 0]; // b
					}
				}
			});

			// encode
			byte[] image_compressed = Format5.Encode(image, 5);

			// base64 encode
			string image_base64 = Convert.ToBase64String(image_compressed, Base64FormattingOptions.None);

			// now overwrite [Preview] and [PreviewPack], inserting them directly after [Basic] if not yet existing
			map.GetOrCreateSection("Preview").SetValue("Size", string.Format("0,0,{0},{1}", preview.Width, preview.Height));

			var section = map.GetOrCreateSection("PreviewPack", "Preview");
			section.Clear();
			section.Index = 0;

			int rowNum = 1;
			for (int i = 0; i < image_base64.Length; i += 70) {
				section.SetValue(rowNum++.ToString(CultureInfo.InvariantCulture), image_base64.Substring(i, Math.Min(70, image_base64.Length - i)));
			}

		}
	}
}
