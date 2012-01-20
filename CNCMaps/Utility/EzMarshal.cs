using System.Runtime.InteropServices;

namespace CNCMaps.Utility {

	class EzMarshal {

		public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct {
			GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			var stuff = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(),
				typeof(T));
			handle.Free();
			return stuff;
		}
	}
}