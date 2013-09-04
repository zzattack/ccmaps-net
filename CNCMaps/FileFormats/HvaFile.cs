using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	/// <summary>Hva file.</summary>
	public class HvaFile : VirtualFile {
		HvaHeader _fileHeader;
		List<Section> _sections;
		int _curSection;
		bool _initialized;
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct HvaHeader {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public byte[] fileName;
			public uint numFrames;
			public uint numSections;
		}

		public class Section {
			public byte[] Name;
			public List<float[]> Matrices;
			public Section(int numMatrices) {
				Matrices = new List<float[]>(numMatrices);
			}
		}

		public HvaFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
		}


		public void Initialize() {
			if (_initialized) return;

			logger.Debug("Loading HVA file {0}", FileName);

			_fileHeader = EzMarshal.ByteArrayToStructure<HvaHeader>(Read(Marshal.SizeOf(typeof(HvaHeader))));
			_sections = new List<Section>((int)_fileHeader.numSections);

			for (int i = 0; i < _fileHeader.numSections; i++) {
				var s = new Section((int)_fileHeader.numFrames);
				s.Name = Read(16);
				_sections.Add(s);
			}

			for (int frame = 0; frame < _fileHeader.numFrames; frame++) {
				for (int section = 0; section < _fileHeader.numSections; section++) {
					_sections[section].Matrices.Add(ReadMatrix());
				}
			}

			logger.Trace("Loaded HVA file {0} with {1} sections", FileName, _sections.Count);

			_initialized = true;
		}

		private float[] ReadMatrix() {
			var ret = new float[12];
			for (int i = 0; i < 12; i++) {
				ret[i] = ReadFloat();
			}
			return ret;
		}

		internal void loadGLMatrix(int frame, out float[] transform) {
			if (!_initialized) Initialize();
			var hvaMatrix = _sections[_curSection].Matrices[frame];
			transform = new float[16];
			transform[0] = hvaMatrix[0];
			transform[1] = hvaMatrix[4];
			transform[2] = hvaMatrix[8];
			transform[3] = 0;
			transform[4] = hvaMatrix[1];
			transform[5] = hvaMatrix[5];
			transform[6] = hvaMatrix[9];
			transform[7] = 0;
			transform[8] = hvaMatrix[2];
			transform[9] = hvaMatrix[6];
			transform[10] = hvaMatrix[10];
			transform[11] = 0;
			transform[12] = hvaMatrix[3];
			transform[13] = hvaMatrix[7];
			transform[14] = hvaMatrix[11];
			transform[15] = 1;
		}
		
		internal void SetSection(int section) {
			_curSection = section;
		}
	}
}