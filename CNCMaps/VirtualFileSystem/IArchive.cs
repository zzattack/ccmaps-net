using CNCMaps.FileFormats;

namespace CNCMaps.VirtualFileSystem {
	interface IArchive {

		bool ContainsFile(string filename);

		VirtualFile OpenFile(string filename, FileFormat format);
	}
}