using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace CNCMaps.Utility {
	public class DrawingSurface {
		public BitmapData bmd { get; private set; }
		public Bitmap bm { get; private set; }
		public int Width { get; private set; } // prevents repeated (slow) lookups in bm.Width
		public int Height { get; private set; } // prevents repeated (slow) lookups in bm.Width
		
		short[] zBuffer;
		bool[] shadowBuffer;

		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public DrawingSurface(int width, int height, PixelFormat pixelFormat) {
			logger.Debug("Initializing DrawingSurface with dimensions ({0},{1}), pixel format {2}", width, height, pixelFormat.ToString());
			bm = new Bitmap(width, height, pixelFormat);
			Width = width;
			Height = height;
			Lock(bm.PixelFormat);
			zBuffer = new short[width * height];
			shadowBuffer = new bool[width * height];
		}

		public void Lock(PixelFormat pixelFormat) {
			if (bmd == null)
				bmd = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadWrite, pixelFormat);
		}

		public void Unlock() {
			if (bmd != null) {
				bm.UnlockBits(bmd);
				bmd = null;
			}
		}

		public bool IsShadow(int x, int y) {
			return shadowBuffer[x + y * Width];
		}

		public void SetShadow(int x, int y) {
			shadowBuffer[x + y * Width] = true;
		}

		public bool[] GetShadows() {
			return shadowBuffer;
		}

		public void SetZ(int x, int y, short z) {
			zBuffer[x + y * Width] = z;
		}

		public short[] GetZBuffer() {
			return zBuffer;
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

			if (saveRect.Location == Point.Empty && saveRect.Size == bm.Size)
				bm.Save(path, encoder, encoderParams);
			else
				using (var cutRect = bm.Clone(saveRect, bm.PixelFormat))
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

			if (saveRect.Location == Point.Empty && saveRect.Size == bm.Size)
				bm.Save(path, encoder, encoderParams);
			else
				using (var cutRect = bm.Clone(saveRect, bm.PixelFormat))
					cutRect.Save(path, encoder, encoderParams);
		}


		internal void FreeNonBitmap() {
			this.zBuffer = null;
			this.shadowBuffer = null;
		}


		internal void Dispose() {
			Unlock();
			this.zBuffer = null;
			this.shadowBuffer = null;
			bm.Dispose();
		}
	}
}
