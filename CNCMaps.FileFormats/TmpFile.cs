using System;
using System.Collections.Generic;
using System.IO;
using CNCMaps.FileFormats.VirtualFileSystem;
using NLog;

namespace CNCMaps.FileFormats {

	public class TmpFile : VirtualFile {
		bool _isInitialized;
		static Logger logger = LogManager.GetCurrentClassLogger();

		// header stuff
		public int Width;                  // width of blocks
		public int Height;                 // height in blocks
		public int BlockWidth;             // width of each block
		public int BlockHeight;            // height of each block

		public List<TmpImage> Images;

		public class TmpImage {
			// header stuff
			public int X;
			public int Y;
			private int _extraDataOffset;
			private int _zDataOffset;
			private int _extraZDataOffset;
			public int ExtraX;
			public int ExtraY;
			public int ExtraWidth;
			public int ExtraHeight;
			private DataPrecencyFlags _dataPrecencyFlags;
			public byte Height;
			public byte TerrainType;
			public byte RampType;
			public sbyte RadarRedLeft;
			public sbyte RadarGreenLeft;
			public sbyte RadarBlueLeft;
			public sbyte RadarRedRight;
			public sbyte RadarGreenRight;
			public sbyte RadarBlueRight;

			public byte[] TileData; // always available
			public byte[] ExtraData; // available is presency flags says so
			public byte[] ZData; // available is presency flags says so
			public byte[] ExtraZData; // available is presency flags says so

			public void Read(TmpFile f) {
				X = f.ReadInt32(); Y = f.ReadInt32();
				_extraDataOffset = f.ReadInt32();
				_zDataOffset = f.ReadInt32();
				_extraZDataOffset = f.ReadInt32();
				ExtraX = f.ReadInt32();
				ExtraY = f.ReadInt32();
				ExtraWidth = f.ReadInt32();
				ExtraHeight = f.ReadInt32();
				_dataPrecencyFlags = (DataPrecencyFlags)f.ReadUInt32();
				Height = f.ReadByte();
				TerrainType = f.ReadByte();
				RampType = f.ReadByte();
				RadarRedLeft = f.ReadSByte();
				RadarGreenLeft = f.ReadSByte(); ;
				RadarBlueLeft = f.ReadSByte(); ;
				RadarRedRight = f.ReadSByte(); ;
				RadarGreenRight = f.ReadSByte(); ;
				RadarBlueRight = f.ReadSByte(); ;
				f.Read(3); // discard padding

				TileData = f.Read(f.BlockWidth * f.BlockHeight / 2);
				if (HasZData)
					ZData = f.Read(f.BlockWidth * f.BlockHeight / 2);

				if (HasExtraData)
					ExtraData = f.Read(Math.Abs(ExtraWidth * ExtraHeight));

				if (HasZData && HasExtraData && 0 < _extraZDataOffset && _extraZDataOffset < f.Length)
					ExtraZData = f.Read(Math.Abs(ExtraWidth * ExtraHeight));
			}

			[Flags]
			private enum DataPrecencyFlags : uint {
				ExtraData = 0x01,
				ZData = 0x02,
				DamagedData = 0x04,
			}

			public bool HasExtraData {
				get { return (_dataPrecencyFlags & DataPrecencyFlags.ExtraData) == DataPrecencyFlags.ExtraData; }
			}
			public bool HasZData {
				get { return (_dataPrecencyFlags & DataPrecencyFlags.ZData) == DataPrecencyFlags.ZData; }
			}
			public bool HasDamagedData {
				get { return (_dataPrecencyFlags & DataPrecencyFlags.DamagedData) == DataPrecencyFlags.DamagedData; }
			}
		}

		public TmpFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
			// Initialize();
		}

		public void Initialize() {
			if (_isInitialized) return;
			logger.Trace("Initializing TMP data for file {0}", FileName);
			_isInitialized = true;
			Position = 0;

			Width = ReadInt32();
			Height = ReadInt32();
			BlockWidth = ReadInt32();
			BlockHeight = ReadInt32();

			byte[] index = Read(Width * Height * sizeof(int));
			Images = new List<TmpImage>(Width * Height);
			for (int x = 0; x < Width * Height; x++) {
				int imageData = BitConverter.ToInt32(index, x * 4);
				Seek(imageData, SeekOrigin.Begin);
				var img = new TmpImage();
				img.Read(this);
				Images.Add(img);
			}
		}

	}
}