using System.IO;
using System.Linq;
using CNCMaps.MapLogic;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {
	/// <summary>Format helper functions.</summary>
	public static class FormatHelper {

		static FormatHelper() {
			MixArchiveExtensions = new[] { ".mix", ".yro", ".mmx" };
			MapExtensions = new[] { ".map", ".yrm", ".mpr" };
		}

		public static readonly string[] MixArchiveExtensions;
		public static readonly string[] MapExtensions;

		public static FileFormat GuessFormat(string filename) {
			string extension = Path.GetExtension(filename).ToLower();
			if (extension == ".csf") return FileFormat.Csf;
			else if (extension == ".hva") return FileFormat.Hva;
			else if (extension == ".ini") {
				if (filename.StartsWith("mission"))
					return FileFormat.Missions;
				else
					return FileFormat.Ini;
			}
			else if (MapExtensions.Contains(extension))
				return FileFormat.Map;
			else if (MixArchiveExtensions.Contains(extension))
				return FileFormat.Mix;
			else if (extension == ".pal")
				return FileFormat.Pal;
			else if (extension == ".pkt")
				return FileFormat.Pkt;
			else if (extension == ".shp")
				return FileFormat.Shp;
			else if (extension == ".tmp")
				return FileFormat.Tmp;
			else if (extension == ".vxl")
				return FileFormat.Vxl;
			return FileFormat.Ukn;
		}

		public static VirtualFile OpenAsFormat(Stream baseStream, string filename, int offset = 0, int length = -1, FileFormat format = FileFormat.None) {
			if (length == -1) length = (int)baseStream.Length;
			if (format == FileFormat.None) format = GuessFormat(filename);
			VirtualFile ret;
			switch (format) {
				case FileFormat.Csf:
					ret = new CsfFile(baseStream, filename, offset, length, true);
					break;
				case FileFormat.Hva:
					ret = new HvaFile(baseStream, filename, offset, length, true);
					break;
				case FileFormat.Ini:
					ret = new IniFile(baseStream, filename, offset, length, true);
					break;
				case FileFormat.Map:
					ret = new MapFile(baseStream, filename, offset, length, true);
					break;
				case FileFormat.Missions:
					ret = new MissionsFile(baseStream, filename, offset, length, true);
					break;
				case FileFormat.Mix:
					ret = new MixFile(baseStream, filename, offset, length, true);
					break;
				case FileFormat.Pal:
					ret = new PalFile(baseStream, filename, offset, length, true);
					break;
				case FileFormat.Pkt:
					ret = new PktFile(baseStream, filename, offset, length, true);
					break;
				case FileFormat.Shp:
					ret = new ShpFile(baseStream, filename, offset, length, true);
					break;
				case FileFormat.Tmp:
					ret = new TmpFile(baseStream, filename, offset, length, true);
					break;
				case FileFormat.Vxl:
					ret = new VxlFile(baseStream, filename, offset, length, true);
					break;
				case FileFormat.Ukn:
				default:
					ret = new VirtualFile(baseStream, filename, offset, length, true);
					break;
			}
			return ret;
		}
	}
}