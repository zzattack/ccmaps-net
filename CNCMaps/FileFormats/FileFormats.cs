using System.IO;
using System.Linq;
using CNCMaps.FileFormats;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps {
	/// <summary>Values that represent FileFormat.</summary>
	public enum FileFormat {
		/// <summary>Strings file.</summary>
		Csf,
		/// <summary>Voxel transformation file.</summary>
		Hva,
		/// <summary>Ini file.</summary>
		Ini,
		/// <summary>Map file.</summary>
		Map,
		/// <summary>Missions file.</summary>
		Missions,
		/// <summary>Mix file container.</summary>
		Mix,
		/// <summary>Palette file.</summary>
		Pal,
		/// <summary>Pkt file, for listing maps.</summary>
		Pkt,
		/// <summary>SHP file, for buildings and infantry.</summary>
		Shp,
		/// <summary>Tile file used for the map's terrain.</summary>
		Tmp,
		/// <summary>Voxel file used for 3d units.</summary>
		Vxl,
		/// <summary>Unknown file type.</summary>
		Ukn,
		/// <summary>No specific type.</summary>
		None
	}

	/// <summary>Format helper functions.</summary>
	public static partial class FormatHelper {

		static FormatHelper() {
			MixArchiveExtensions = new string[] { ".mix", ".yro", ".mmx" };
			MapExtensions = new string[] { ".map", ".yrm", ".mpr" };
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
					ret = new CsfFile(baseStream, offset, length, true);
					break;
				case FileFormat.Hva:
					ret = new HvaFile(baseStream, offset, length, true);
					break;
				case FileFormat.Ini:
					ret = new IniFile(baseStream, offset, length, true);
					break;
				case FileFormat.Map:
					ret = new MapFile(baseStream, offset, length, true);
					break;
				case FileFormat.Missions:
					ret = new MissionsFile(baseStream, offset, length, true);
					break;
				case FileFormat.Mix:
					ret = new MixFile(baseStream, offset, length, true);
					break;
				case FileFormat.Pal:
					ret = new PalFile(baseStream, offset, length, true);
					break;
				case FileFormat.Pkt:
					ret = new PktFile(baseStream, offset, length, true);
					break;
				case FileFormat.Shp:
					ret = new ShpFile(baseStream, offset, length, true);
					break;
				case FileFormat.Tmp:
					ret = new TmpFile(baseStream, offset, length, true);
					break;
				case FileFormat.Vxl:
					ret = new VxlFile(baseStream, offset, length, true);
					break;
				case FileFormat.Ukn:
				default:
					ret = new VirtualFile(baseStream, offset, length, true);
					break;
			}
			ret.FileName = filename;
			return ret;
		}
	}
}