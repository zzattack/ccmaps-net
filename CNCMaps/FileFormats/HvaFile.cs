using System.Collections.Generic;
using System.IO;
using CNCMaps.VirtualFileSystem;
using OpenTK;

namespace CNCMaps.FileFormats {

	/// <summary>Hva file.</summary>
	public class HvaFile : VirtualFile {

		public int NumFrames { get; set; }
		public List<Section> Sections { get; set; }

		bool _initialized;
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
		
		public class Section {
			public string Name;
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
			Seek(0, SeekOrigin.Begin);
			ReadCString(16); // filename
			NumFrames = ReadInt32();
			int numSections = ReadInt32();
			Sections = new List<Section>(numSections);

			for (int i = 0; i < numSections; i++)
				Sections.Add(new Section(NumFrames) {
					Name = ReadCString(16)
				});

			for (int frame = 0; frame < NumFrames; frame++)
				for (int section = 0; section < Sections.Count; section++)
					Sections[section].Matrices.Add(ReadMatrix());

			logger.Trace("Loaded HVA file {0} with {1} sections", FileName, Sections.Count);
			_initialized = true;
		}

		private float[] ReadMatrix() {
			var ret = new float[12];
			for (int i = 0; i < 12; i++) {
				ret[i] = ReadFloat();
			}
			return ret;
		}

		public Matrix4 LoadGLMatrix(string section, int frame = 0) {
			return ToGLMatrix(Sections.Find(s => s.Name == section).Matrices[frame]);
		}

		internal Matrix4 LoadGLMatrix(int section, int frame = 0) {
			Initialize();
			var hvaMatrix = Sections[section].Matrices[frame];
			return ToGLMatrix(hvaMatrix);
		}

		private static Matrix4 ToGLMatrix(float[] hvaMatrix) {
			//return new Matrix4(
			//	hvaMatrix[0], hvaMatrix[1], hvaMatrix[2], 0,
			//	hvaMatrix[3], hvaMatrix[4], hvaMatrix[5], 0,
			//	hvaMatrix[6], hvaMatrix[7], hvaMatrix[8], 0,
			//	hvaMatrix[9], hvaMatrix[10], hvaMatrix[11], 1);

			return new Matrix4(
				hvaMatrix[0], hvaMatrix[4], hvaMatrix[8], 0,
				hvaMatrix[1], hvaMatrix[5], hvaMatrix[9], 0,
				hvaMatrix[2], hvaMatrix[6], hvaMatrix[10], 0,
				hvaMatrix[3], hvaMatrix[7], hvaMatrix[11], 1);
		}
	}
}