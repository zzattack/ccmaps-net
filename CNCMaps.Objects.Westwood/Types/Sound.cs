namespace CNCMaps.Engine.Types {
	public class Sound : AbstractType {
		private string filename;
		public Sound(string s) : base(s) {
			filename = s;
		}
	}
}