using CNCMaps.VirtualFileSystem;

namespace CNCMaps.MapLogic {
	class DrawableObject<T> : System.IComparable where T : VirtualFile {
		public DrawProperties props;
		public T file;
		int index;
		public DrawableObject(T file, DrawProperties drawProperties, int index) {
			this.file = file;
			props = drawProperties;
			this.index = index;
		}

		public int CompareTo(object obj) {
			var other = obj as DrawableObject<T>;
			if (props.ySort != other.props.ySort)
				return props.ySort - other.props.ySort;
			else
				return index - other.index;
		}
	}
}