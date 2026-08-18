namespace CNCMaps.Engine.Rendering {
	/// <summary>
	/// Contract for an externally provided interactive preview window. The GUI probes for an
	/// implementation at runtime; <see cref="Show"/> is called after drawing completes, while
	/// the map's theater resources are still loaded so tiles can be re-evaluated.
	/// </summary>
	public interface IMapPreviewWindow {
		void Show(Map.Map map);
	}
}
