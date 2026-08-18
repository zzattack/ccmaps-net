using System;
using System.IO;
using CNCMaps.Engine.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace CNCMaps.Tests {

	public class PngWriterTests {

		static byte[] MakeBgrTestData(int width, int height, int bpp, int seed) {
			var rnd = new Random(seed);
			var data = new byte[width * height * bpp];
			rnd.NextBytes(data);
			// include some compressible runs like real map content
			Array.Fill(data, (byte)0x55, 0, Math.Min(data.Length, width * bpp * 3));
			return data;
		}

		static void AssertRoundtrip(int width, int height, int bpp, System.Drawing.Rectangle rect, int level) {
			var data = MakeBgrTestData(width, height, bpp, 1234);
			string path = Path.Combine(Path.GetTempPath(), $"pngwriter-test-{Guid.NewGuid():N}.png");
			try {
				PngWriter.Save(path, data, width, bpp, rect, level);
				using var img = SixLabors.ImageSharp.Image.Load<Rgb24>(path);
				Assert.Equal(rect.Width, img.Width);
				Assert.Equal(rect.Height, img.Height);
				img.ProcessPixelRows(accessor => {
					for (int y = 0; y < rect.Height; y++) {
						var row = accessor.GetRowSpan(y);
						for (int x = 0; x < rect.Width; x++) {
							int src = ((rect.Top + y) * width + rect.Left + x) * bpp;
							Assert.True(row[x].B == data[src] && row[x].G == data[src + 1] && row[x].R == data[src + 2],
								$"pixel mismatch at {x},{y}");
						}
					}
				});
			}
			finally {
				File.Delete(path);
			}
		}

		[Theory]
		[InlineData(1)]
		[InlineData(4)]
		[InlineData(9)]
		public void FullImage_RoundtripsAtAllLevels(int level) {
			AssertRoundtrip(320, 200, 3, new System.Drawing.Rectangle(0, 0, 320, 200), level);
		}

		[Fact]
		public void CroppedRegion_Roundtrips() {
			AssertRoundtrip(320, 200, 3, new System.Drawing.Rectangle(17, 23, 111, 97), 4);
		}

		[Fact]
		public void BgraSource_DropsAlphaAndRoundtrips() {
			AssertRoundtrip(64, 64, 4, new System.Drawing.Rectangle(0, 0, 64, 64), 4);
		}

		[Fact]
		public void LargeImage_UsesMultipleChunksAndRoundtrips() {
			// large enough that the parallel writer splits into multiple deflate blocks
			AssertRoundtrip(2000, 1400, 3, new System.Drawing.Rectangle(0, 0, 2000, 1400), 4);
		}

		[Fact]
		public void TinyImage_Roundtrips() {
			AssertRoundtrip(1, 1, 3, new System.Drawing.Rectangle(0, 0, 1, 1), 4);
		}
	}
}
