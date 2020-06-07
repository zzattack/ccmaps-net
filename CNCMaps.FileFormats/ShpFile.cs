using System.Collections.Generic;
using System.IO;
using CNCMaps.FileFormats.Encodings;
using CNCMaps.FileFormats.VirtualFileSystem;
using NLog;

namespace CNCMaps.FileFormats {

	public class ShpFile : VirtualFile {
		static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		bool _isInitialized;

		public ShpFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
		}

		private short Zero;
		public short Width;
		public short Height;
		public short NumImages;
		public List<ShpImage> Images = new List<ShpImage>();

		public class ShpImage {
			ShpFile _f;
			int _frameIndex;

			public short X;
			public short Y;
			public short Width;
			public short Height;
			public byte CompressionType;
			public byte Unknown1;
			public byte Unknown2;
			public byte Unknown3;
			private int Unknown4;
			private int Zero;
			public int ImgDataOffset;

			private byte[] _decompressedImage;

			public void Read(ShpFile f, int frameIndex) {
				_f = f;
				_frameIndex = frameIndex;

				X = f.ReadInt16();
				Y = f.ReadInt16();
				Width = f.ReadInt16();
				Height = f.ReadInt16();
				CompressionType = f.ReadByte();
				Unknown1 = f.ReadByte();
				Unknown2 = f.ReadByte();
				Unknown3 = f.ReadByte();
				Unknown4 = f.ReadInt32();
				Zero = f.ReadInt32();
				ImgDataOffset = f.ReadInt32();
			}

			public byte[] GetImageData() {
				// make sure RawImageData is present/decoded if needed
				if (_decompressedImage == null) {
					_f.Seek(ImgDataOffset, SeekOrigin.Begin);
					int c_px = Width * Height;

					// img.Header.compression &= 0x03;
					if (CompressionType <= 1) {
						// Raw 8 bits-per-pixel image data
						_decompressedImage = _f.Read(c_px);
					}
					else if (CompressionType == 2) {
						// Image data divided into scanlines {
						// -- Length of scanline (ImageHeader.width + 2) : uint16
						// -- Raw 8 bits-per-pixel image data : uint8[ImageHeader.width]
						_decompressedImage = new byte[c_px];
						int lineOffset = 0;
						for (int y = 0; y < Height; y++) {
							ushort scanlineLength = (ushort)(_f.ReadUInt16() - sizeof(ushort));
							_f.Read(_decompressedImage, lineOffset, scanlineLength);
							lineOffset += scanlineLength;
						}
					}
					else if (CompressionType == 3) {
						_decompressedImage = new byte[c_px];
						var compressedEnd = (int)_f.Length;
						if (_frameIndex < _f.Images.Count - 1)
							compressedEnd = _f.Images[_frameIndex + 1].ImgDataOffset;
						if (compressedEnd < ImgDataOffset)
							compressedEnd = (int)_f.Length;
						Format3.DecodeInto(_f.Read(compressedEnd - ImgDataOffset), _decompressedImage, Width, Height);
					}
					else {
						Logger.Debug("SHP image {0} frame {1} has unknown compression!", _f.FileName, _frameIndex);
					}
				}

				return _decompressedImage;
			}
		}

		public void Initialize() {
			if (_isInitialized) return;

			Logger.Trace("Initializing SHP data for file {0}", FileName);
			Zero = ReadInt16();
			Width = ReadInt16();
			Height = ReadInt16();
			NumImages = ReadInt16();

			Images = new List<ShpImage>(NumImages);
			for (int i = 0; i < NumImages; i++) {
				var img = new ShpImage();
				img.Read(this, i);
				Images.Add(img);
			}
			_isInitialized = true;
		}

		public ShpImage GetImage(int imageIndex) {
			if (imageIndex >= Images.Count) return new ShpImage();
			return Images[imageIndex];
		}
	}
}