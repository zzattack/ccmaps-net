using System.IO;
using System.Linq;
using CNCMaps.FileFormats.VirtualFileSystem;
using NLog;

namespace CNCMaps.FileFormats {
	/// <summary>Format helper functions.</summary>
	public static class FormatHelper {

		static Logger logger = LogManager.GetCurrentClassLogger();

		public static readonly string[] MixArchiveExtensions = { ".mix", ".yro", ".mmx" };
		public static readonly string[] MapExtensions = { ".map", ".yrm", ".mpr" };

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
			//else if (MapExtensions.Contains(extension))
			//	return FileFormat.Map;
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
			else if (extension == ".vpl")
				return FileFormat.Vpl;
			else if (extension == ".vxl")
				return FileFormat.Vxl;
			return FileFormat.Ukn;
		}

		public static VirtualFile OpenAsFormat(Stream baseStream, string filename, int offset = 0, int length = -1, FileFormat format = FileFormat.None, CacheMethod m = CacheMethod.Default) {
			if (length == -1) length = (int)baseStream.Length;
			if (format == FileFormat.None) {
				format = GuessFormat(filename);
				logger.Debug("Guessed format: {0}", format);
			}

			switch (format) {
				case FileFormat.Csf:
					return new CsfFile(baseStream, filename, offset, length, m != CacheMethod.NoCache); // defaults to cache
				case FileFormat.Hva:
					return new HvaFile(baseStream, filename, offset, length, m != CacheMethod.NoCache); // defaults to not cache
				case FileFormat.Ini:
					return new IniFile(baseStream, filename, offset, length, m != CacheMethod.NoCache);
				case FileFormat.Missions:
					return new MissionsFile(baseStream, filename, offset, length, m != CacheMethod.NoCache);
				case FileFormat.Mix:
					return new MixFile(baseStream, filename, offset, length, m == CacheMethod.Cache);
				case FileFormat.Pal:
					return new PalFile(baseStream, filename, offset, length, m != CacheMethod.NoCache);
				case FileFormat.Pkt:
					return new PktFile(baseStream, filename, offset, length, m != CacheMethod.NoCache);
				case FileFormat.Shp:
					return new ShpFile(baseStream, filename, offset, length, m != CacheMethod.NoCache);
				case FileFormat.Tmp:
					return new TmpFile(baseStream, filename, offset, length, m != CacheMethod.NoCache);
				case FileFormat.Vpl:
					return new VplFile(baseStream, filename, offset, length, m != CacheMethod.NoCache);
				case FileFormat.Vxl:
					return new VxlFile(baseStream, filename, offset, length, m != CacheMethod.NoCache);
				case FileFormat.Ukn:
				default:
					return new VirtualFile(baseStream, filename, offset, length, m != CacheMethod.NoCache);
			}
		}
	}
}