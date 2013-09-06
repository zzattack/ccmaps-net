using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	public class VplFile : VirtualFile {

		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public VplFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = false)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
		}

		public VplFile(Stream baseStream, string filename = "", bool isBuffered = true)
			: base(baseStream, filename, isBuffered) {
		}

	}
}