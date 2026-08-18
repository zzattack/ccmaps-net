using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using NLog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;

namespace CNCMaps.Engine.Rendering {

	public enum SurfaceFormat {
		Bgr24,
		Bgra32,
	}

	/// <summary>
	/// Mirrors the shape of System.Drawing.Imaging.BitmapData so the pointer-based
	/// renderers can keep addressing pixels through Scan0/Stride unchanged.
	/// </summary>
	public class BitmapData {
		public IntPtr Scan0 { get; internal set; }
		public int Stride { get; internal set; }
		public int Width { get; internal set; }
		public int Height { get; internal set; }
	}

	/// <summary>
	/// A raw pixel surface: a pinned managed buffer the renderers write into directly,
	/// with ImageSharp used for encoding and scaling. No GDI+ involved.
	/// </summary>
	public class DrawingSurface {
		public BitmapData BitmapData { get; private set; }
		public SurfaceFormat Format { get; }
		public int Width { get; private set; }
		public int Height { get; private set; }
		public int BytesPerPixel => Format == SurfaceFormat.Bgr24 ? 3 : 4;

		byte[] _data;
		int[] _heightBuffer;
		bool[] _shadowBuffer;
		short[] zBuffer;
		static Logger logger = LogManager.GetCurrentClassLogger();

		public DrawingSurface(int width, int height, SurfaceFormat format = SurfaceFormat.Bgr24) {
			logger.Debug("Initializing DrawingSurface with dimensions ({0},{1}), format {2}", width, height, format);
			Format = format;
			Width = width;
			Height = height;
			// allocated on the pinned object heap: the address is stable for the
			// pointer-based renderers, yet the array is garbage-collected normally
			// once the surface is unreferenced (no handle to leak)
			_data = GC.AllocateArray<byte>(width * height * BytesPerPixel, pinned: true);
			BitmapData = new BitmapData {
				Scan0 = Marshal.UnsafeAddrOfPinnedArrayElement(_data, 0),
				Stride = width * BytesPerPixel,
				Width = width,
				Height = height,
			};
			zBuffer = new short[width * height];
			_heightBuffer = new int[width * height];
			_shadowBuffer = new bool[width * height];
		}

		// The surface is always directly addressable now; kept for call-site compatibility.
		public void Lock() { }
		public void Unlock() { }

		public bool IsShadow(int x, int y) {
			return _shadowBuffer[x + y * Width];
		}

		public void SetShadow(int x, int y) {
			_shadowBuffer[x + y * Width] = true;
		}

		public bool[] GetShadows() {
			return _shadowBuffer;
		}

		public short[] GetZBuffer() {
			return zBuffer;
		}

		public int[] GetHeightBuffer() {
			return _heightBuffer;
		}

		/// <summary>
		/// An ImageSharp view over this surface's pixel buffer; mutations write directly
		/// into the surface. Only valid for Bgr24 surfaces. Do not dispose the surface
		/// while the view is in use.
		/// </summary>
		public Image<Bgr24> GetImageView() {
			if (Format != SurfaceFormat.Bgr24)
				throw new InvalidOperationException("image view is only supported on Bgr24 surfaces");
			return SixLabors.ImageSharp.Image.WrapMemory<Bgr24>(_data.AsMemory(), Width, Height);
		}

		/// <summary>Copies a region of the surface into a standalone ImageSharp image.</summary>
		public Image<Bgr24> CopyRegion(Rectangle rect) {
			var img = new Image<Bgr24>(rect.Width, rect.Height);
			int bpp = BytesPerPixel;
			var data = _data;
			int surfaceWidth = Width;
			img.ProcessPixelRows(accessor => {
				for (int y = 0; y < rect.Height; y++) {
					var dstRow = MemoryMarshal.AsBytes(accessor.GetRowSpan(y));
					var srcRow = data.AsSpan(((rect.Top + y) * surfaceWidth + rect.Left) * bpp, rect.Width * bpp);
					if (bpp == 3)
						srcRow.CopyTo(dstRow);
					else {
						// drop the alpha channel
						for (int x = 0; x < rect.Width; x++)
							srcRow.Slice(x * 4, 3).CopyTo(dstRow.Slice(x * 3, 3));
					}
				}
			});
			return img;
		}

		public void SavePNG(string path, int compressionLevel, int left, int top, int width, int height) {
			SavePNG(path, compressionLevel, new Rectangle(left, top, width, height));
		}

		public void SavePNG(string path, int compressionLevel, Rectangle saveRect) {
			logger.Info("Saving PNG to {0}, compression level {1}, clip @({2},{3};{4}x{5})",
				path, compressionLevel, saveRect.Left, saveRect.Top, saveRect.Width, saveRect.Height);
			saveRect.Intersect(new Rectangle(0, 0, Width, Height));
			PngWriter.Save(path, _data, Width, BytesPerPixel, saveRect, compressionLevel);
		}

		public void SaveJPEG(string path, int compression, int left, int top, int width, int height) {
			SaveJPEG(path, compression, new Rectangle(left, top, width, height));
		}

		public void SaveJPEG(string path, int quality, Rectangle saveRect) {
			logger.Info("Saving JPEG to {0}, quality level {1}, clip @({2},{3});{4}x{5})",
				path, quality, saveRect.Left, saveRect.Top, saveRect.Width, saveRect.Height);
			Save(path, saveRect, new JpegEncoder { Quality = quality });
		}

		private void Save(string path, Rectangle saveRect, SixLabors.ImageSharp.Formats.IImageEncoder encoder) {
			saveRect.Intersect(new Rectangle(0, 0, Width, Height));
			if (saveRect.Location == System.Drawing.Point.Empty && saveRect.Size == new Size(Width, Height) && Format == SurfaceFormat.Bgr24) {
				using (var img = GetImageView())
					img.Save(path, encoder);
			}
			else {
				using (var img = CopyRegion(saveRect))
					img.Save(path, encoder);
			}
		}

		public void SaveThumb(Size dimensions, Rectangle cutout, string path, bool saveAsPng = false) {
			using (var thumb = CopyRegion(cutout)) {
				thumb.Mutate(x => x.Resize(dimensions.Width, dimensions.Height, KnownResamplers.Bicubic));
				if (saveAsPng)
					thumb.Save(path, new PngEncoder { CompressionLevel = PngCompressionLevel.Level6, ColorType = PngColorType.Rgb });
				else
					thumb.Save(path, new JpegEncoder { Quality = 95 });
			}
		}

		public void FreeNonBitmap() {
			zBuffer = null;
			_shadowBuffer = null;
		}

		internal void Dispose() {
			zBuffer = null;
			_shadowBuffer = null;
			_data = null;
			BitmapData = null;
		}
	}

}
