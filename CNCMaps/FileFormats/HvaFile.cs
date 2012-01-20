using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	/// <summary>Hva file.</summary>
	public class HvaFile : VirtualFile {
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct HvaHeader {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public byte[] fileName;
			public uint numFrames;
			public uint numSections;
		}

		public class Section {
			public byte[] name;
			public List<float[]> matrices;

			public Section(int numMatrices) {
				matrices = new List<float[]>(numMatrices);
			}
		}

		HvaHeader fileHeader;
		List<Section> sections;

		public HvaFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
		}

		bool initialized;

		public void Initialize() {
			if (initialized) return;
			fileHeader = EzMarshal.ByteArrayToStructure<HvaHeader>(Read(Marshal.SizeOf(typeof(HvaHeader))));
			sections = new List<Section>((int)fileHeader.numSections);

			for (int i = 0; i < fileHeader.numSections; i++) {
				var s = new Section((int)fileHeader.numFrames);
				s.name = Read(16);
				sections.Add(s);
			}

			for (int frame = 0; frame < fileHeader.numFrames; frame++) {
				for (int section = 0; section < fileHeader.numSections; section++) {
					sections[section].matrices.Add(ReadMatrix());
				}
			}
			initialized = true;
		}

		private float[] ReadMatrix() {
			var ret = new float[12];
			for (int i = 0; i < 12; i++) {
				ret[i] = ReadFloat();
			}
			return ret;
		}

		internal void loadGLMatrix(int frame, out float[] transform) {
			if (!initialized) Initialize();
			var hvaMatrix = sections[curSection].matrices[frame];
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

		int curSection;

		internal void SetSection(int section) {
			curSection = section;
		}
	}
}