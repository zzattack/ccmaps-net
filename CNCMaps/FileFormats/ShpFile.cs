using System.IO;
using CNCMaps.VirtualFileSystem;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using CNCMaps.Utility;
using CNCMaps.Encodings;
using System;
using CNCMaps.MapLogic;

namespace CNCMaps.FileFormats {

	class ShpFile : VirtualFile {

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct ShpFileHeader {
			private short zero;
			public short cx;
			public short cy;
			public short c_images;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct ShpImageHeader {
			public short x;
			public short y;
			public short cx;
			public short cy;
			public int compression;
			private int unknown;
			private int zero;
			public int offset;
		}

		struct ShpImage {
			public ShpImageHeader header;
			public byte[] imageData;
		}

		ShpFileHeader header;
		List<ShpImage> images;

		bool initialized = false;

		public ShpFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
		}

		public void Initialize() {
			initialized = true;
			this.header = EzMarshal.ByteArrayToStructure<ShpFileHeader>(Read(Marshal.SizeOf(typeof(ShpFileHeader))));
			images = new List<ShpImage>(this.header.c_images);
			int prevOffset = int.MinValue;
			for (int i = 0; i < this.header.c_images; i++) {
				ShpImage img = new ShpImage();
				img.header = EzMarshal.ByteArrayToStructure<ShpImageHeader>(Read(Marshal.SizeOf(typeof(ShpImageHeader))));
				images.Add(img);

				// if this is a valid image, make sure the offsets are contiguous
				if (img.header.cx * img.header.cy > 0) {
					System.Diagnostics.Debug.Assert(prevOffset < img.header.offset);
					prevOffset = img.header.offset;
				}
			}
		}

		private ShpImage GetImage(int imageIndex) {
			ShpImage img = images[imageIndex];
			// make sure imageData is present/decoded if needed
			if (img.imageData == null) {
				Position = img.header.offset;
				int c_px = img.header.cx * img.header.cy;

				if ((img.header.compression & 2) == 2) {
					img.imageData = new byte[c_px];
					int compressedEnd;
					if (imageIndex < images.Count - 1)
						compressedEnd = images[imageIndex+1].header.offset;
					else
						compressedEnd = (int)this.Length;
					Format3.DecodeInto(Read(compressedEnd - img.header.offset), img.imageData, img.header.cx, img.header.cy);
				}
				else {
					img.imageData = Read(c_px);
				}
			}
			return img;
		}

		public void Draw(int subTileNum, DrawingSurface ds, int x_offset, int y_offset, short height, Palette p) {
			if (!initialized) Initialize();
		}
	}
}