using CNCMaps.FileFormats.FileFormats;

namespace CNCMaps.FileFormats.VirtualFileSystem {
	public interface IArchive {
		bool ContainsFile(string filename);
		VirtualFile OpenFile(string filename, FileFormat format, CacheMethod m = CacheMethod.Default);

		void Close();
	}
}