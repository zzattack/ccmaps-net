using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using NLog;

namespace CNCMaps.Engine.Rendering {
	public class DrawingSurface {
		public BitmapData BitmapData { get; private set; }
		public Bitmap Bitmap { get; private set; }
		public int Width { get; private set; } // prevents repeated (slow) lookups in bm.Width
		public int Height { get; private set; } // prevents repeated (slow) lookups in bm.Width
		int[] _heightBuffer;
		bool[] _shadowBuffer;
		short[] zBuffer;
		static Logger logger = LogManager.GetCurrentClassLogger();

		public DrawingSurface(int width, int height, PixelFormat pixelFormat) {
			logger.Debug("Initializing DrawingSurface with dimensions ({0},{1}), pixel format {2}", width, height, pixelFormat.ToString());
			Bitmap = new Bitmap(width, height, pixelFormat);
			Width = width;
			Height = height;
			Lock(Bitmap.PixelFormat);
			zBuffer = new short[width * height];
			_heightBuffer = new int[width * height];
			_shadowBuffer = new bool[width * height];
		}

		public void Lock(PixelFormat pixelFormat = PixelFormat.Format24bppRgb) {
			if (BitmapData == null)
				BitmapData = Bitmap.LockBits(new Rectangle(0, 0, Bitmap.Width, Bitmap.Height), ImageLockMode.ReadWrite, pixelFormat);
		}

		public void Unlock() {
			if (BitmapData != null) {
				Bitmap.UnlockBits(BitmapData);
				BitmapData = null;
			}
		}

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

		public void SavePNG(string path, int compressionLevel, int left, int top, int width, int height) {
			SavePNG(path, compressionLevel, new Rectangle(left, top, width, height));
		}

		public void SavePNG(string path, int compressionLevel, Rectangle saveRect) {
			logger.Info("Saving PNG to {0}, compression level {1}, clip @({2},{3};{4}x{5})",
				path, compressionLevel, saveRect.Left, saveRect.Top, saveRect.Width, saveRect.Height);
			Unlock();
			ImageCodecInfo encoder = ImageCodecInfo.GetImageEncoders().First(e => e.FormatID == ImageFormat.Png.Guid);
			var encoderParams = new EncoderParameters(1);
			encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, compressionLevel);

			if (saveRect.Location == Point.Empty && saveRect.Size == Bitmap.Size)
				Bitmap.Save(path, encoder, encoderParams);
			else
				using (var cutRect = Bitmap.Clone(saveRect, Bitmap.PixelFormat))
					cutRect.Save(path, encoder, encoderParams);
		}

		public void SaveJPEG(string path, int compression, int left, int top, int width, int height) {
			SaveJPEG(path, compression, new Rectangle(left, top, width, height));
		}

		public void SaveJPEG(string path, int quality, Rectangle saveRect) {
			Unlock();
			logger.Info("Saving JPEG to {0}, quality level {1}, clip @({2},{3});{4}x{5})",
				path, quality, saveRect.Left, saveRect.Top, saveRect.Width, saveRect.Height);
			ImageCodecInfo encoder = ImageCodecInfo.GetImageEncoders().First(e => e.FormatID == ImageFormat.Jpeg.Guid);
			var encoderParams = new EncoderParameters(1);
			encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);

			if (saveRect.Location == Point.Empty && saveRect.Size == Bitmap.Size)
				Bitmap.Save(path, encoder, encoderParams);
			else
				using (var cutRect = Bitmap.Clone(saveRect, Bitmap.PixelFormat))
					cutRect.Save(path, encoder, encoderParams);
		}

		public void SaveThumb(Size dimensions, Rectangle cutout, string path) {
			Unlock();

			using (var thumb = new Bitmap(dimensions.Width, dimensions.Height, PixelFormat.Format24bppRgb)) {
				using (Graphics gfx = Graphics.FromImage(thumb)) {
					// use high-quality scaling
					gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
					gfx.SmoothingMode = SmoothingMode.HighQuality;
					gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;
					gfx.CompositingQuality = CompositingQuality.HighQuality;

					var srcRect = cutout;
					var dstRect = new Rectangle(0, 0, thumb.Width, thumb.Height);
					gfx.DrawImage(Bitmap, dstRect, srcRect, GraphicsUnit.Pixel);
				}
				ImageCodecInfo encoder = ImageCodecInfo.GetImageEncoders().First(e => e.FormatID == ImageFormat.Jpeg.Guid);
				var encoderParams = new EncoderParameters(1);
				encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 95L);
				thumb.Save(path, encoder, encoderParams);
			}
		}

		public void FreeNonBitmap() {
			zBuffer = null;
			_shadowBuffer = null;
		}


		internal void Dispose() {
			Unlock();
			zBuffer = null;
			_shadowBuffer = null;
			Bitmap.Dispose();
		}
	}

	public enum SizeMode {
		Local,
		Full,
		Auto,
	}
}
