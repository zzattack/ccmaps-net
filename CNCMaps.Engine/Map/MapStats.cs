using System.Collections.Generic;

namespace CNCMaps.Engine.Map {
	/// <summary>Statistics derived from the fully parsed map, exported via --meta-json.</summary>
	public class MapStats {
		public int HeightMin;
		public int HeightMax;
		public int TotalTiles;
		public int WaterTiles;
		public int ShoreTiles;
		public int CliffTiles;
		public int RampTiles;

		public int OreCells;
		public int GemCells;
		public long TotalCredits;
		public int OreSpawners;

		public int Structures;
		public int TechStructures;
		public int GarrisonableStructures;
		public int TerrainObjects;
		public int Units;
		public int Infantry;
		public int Aircraft;
		public int Smudges;
		public bool HasBridges;

		public SortedDictionary<string, int> TechStructureTypes = new SortedDictionary<string, int>();
	}

	/// <summary>A start waypoint in both cell and drawing-surface pixel coordinates, exported via
	/// --meta-json so a render can be aligned against an engine capture of the same map.</summary>
	public class StartPositionPixel {
		public int Number;
		public int Rx;
		public int Ry;
		public int Z;
		public int X;
		public int Y;
	}
}
