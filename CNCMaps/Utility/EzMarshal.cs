using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace CNCMaps.Utility {
	class EzMarshal {
		public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct {
			GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			T stuff = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(),
				typeof(T));
			handle.Free();
			return stuff;
		}

	}
}
