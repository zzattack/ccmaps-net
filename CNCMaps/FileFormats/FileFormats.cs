using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CNCMaps.VirtualFileSystem;
using CNCMaps.FileFormats;

namespace CNCMaps {
	public enum FileFormat {
		Csf, Hva,
		Ini, Map,
		Missions, Mix,
		Pal, Pkt,
		Shp, Tmp,
		Vxl, Ukn,
		None
	}

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
					ret =  new CsfFile(baseStream, offset, length, true);
					break;
				case FileFormat.Hva:
					ret =  new HvaFile(baseStream, offset, length, true);
					break;
				case FileFormat.Ini:
					ret =  new IniFile(baseStream, offset, length, true);
					break;
				case FileFormat.Map:
					ret =  new MapFile(baseStream, offset, length, true);
					break;
				case FileFormat.Missions:
					ret =  new MissionsFile(baseStream, offset, length, true);
					break;
				case FileFormat.Mix:
					ret =  new MixFile(baseStream, offset, length, true);
					break;
				case FileFormat.Pal:
					ret =  new PalFile(baseStream, offset, length, true);
					break;
				case FileFormat.Pkt:
					ret =  new PktFile(baseStream, offset, length, true);
					break;
				case FileFormat.Shp:
					ret =  new ShpFile(baseStream, offset, length, true);
					break;
				case FileFormat.Tmp:
					ret =  new TmpFile(baseStream, offset, length, true);
					break;
				case FileFormat.Vxl:
					ret =  new VxlFile(baseStream, offset, length, true);
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
