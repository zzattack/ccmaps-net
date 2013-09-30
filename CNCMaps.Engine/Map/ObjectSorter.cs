using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Utility;

namespace CNCMaps.Engine.Map {
	internal class ObjectSorter {
		private readonly TileLayer _map;
		private readonly Theater _t;
		// graph of (v, Adj[v])
		private readonly Dictionary<GameObject, HashSet<GameObject>> _graph = new Dictionary<GameObject, HashSet<GameObject>>();
		// objects with empty all their dependencies
		private readonly HashSet<GameObject> _hist = new HashSet<GameObject>();
		private readonly List<GameObject> _histOrdered = new List<GameObject>();

		public ObjectSorter(Theater t, TileLayer map) {
			this._map = map;
			this._t = t;
		}


		internal IEnumerable<GameObject> GetOrderedObjects() {
			for (int y = 0; y < _map.Height; y++) {
				for (int x = _map.Width*2 - 2; x >= 0; x -= 2)
					ProcessTile(_map[x, y]);
				for (int x = _map.Width*2 - 3; x >= 0; x -= 2)
					ProcessTile(_map[x, y]);
			}

			// in a world where everything is perfectly drawable this should hold
			// Debug.Assert(_graph.Count == 0);
			while (_graph.Count != 0) {
				var leastDy = _graph.OrderBy(pair => pair.Key.TopTile.Dy).First().Key;
				MarkDependencies(leastDy);
			}

			//for (int i = 0; i < ret.Count; i++) {
			//	var r = ret[i];
			//	r.DrawOrderIndex = i;
			//}

			return _histOrdered;
		}

		private void ProcessTile(MapTile tile) {
			// "lock" this tile at least until we've examined its neighbourhood
			AddDependency(tile, null);
			ExamineNeighbourhood(tile);
			foreach (var obj in tile.AllObjects) {
				// every object depends on its bottom-most host tile at least
				AddDependency(obj, null);
				ExamineNeighbourhood(obj);
			}

			_graph[tile].Remove(tile);
			if (_graph[tile].Count == 0)
				// mark this tile and remove it as dependency from all other objects
				MarkDependencies(tile);

			// Debug.WriteLine("----------------------- processed " + tile);
		}

		internal void ExamineNeighbourhood(GameObject obj) {
			// Debug.WriteLine("Examining neighhourhood of " + obj);
			// Debug.Assert(!_hist.Contains(obj), "examining neighbourhood for an object that's already in the draw list");
			var objBB = GetBoundingBox(obj);
			var tileTL = _map.GetTileScreen(objBB.Location);
			var tileBR = _map.GetTileScreen(objBB.Location + objBB.Size);
			if (tileTL == null) tileTL = _map.GetTile(0, 0);
			if (tileBR == null) tileBR = _map.GetTile(_map.Width*2 - 2, _map.Height - 1);

			for (int y = tileTL.Dy - 1; y <= tileBR.Dy + 1; y++) {
				for (int x = tileTL.Dx - 1; x <= tileBR.Dx + 1; x += 2) {
					if ((x + (y + obj.TopTile.Dy)) < 0 || y < 0) continue;
					var tile2 = _map[x + (y + obj.TopTile.Dy)%2, y/2];
					if (tile2 == null) continue;

					// Debug.WriteLine("neighhourhood tile " + tile2 + " of obj " + obj + " at " + obj.Tile);
					ExamineObjects(obj, tile2);
					foreach (var obj2 in tile2.AllObjects)
						ExamineObjects(obj, obj2);
				}
			}
		}

		private void ExamineObjects(GameObject obj, GameObject obj2) {
			if (obj == obj2) return;

			var front = GetFrontBlock(obj, obj2);
			if (front == obj && !_hist.Contains(obj2))
				AddDependency(obj, obj2, "obj in front");

			else if (front == obj2) {
				// obj2 is in front of obj, so so obj cannot have been drawn yet
				// Debug.Assert(!_hist.Contains(obj2), "obj drawn before all its dependencies were found");
				AddDependency(obj2, obj, "obj2 in front");
			}
		}

		private void AddDependency(GameObject obj, GameObject dependency, string reason = "") {
			HashSet<GameObject> list;
			if (!_graph.TryGetValue(obj, out list))
				_graph[obj] = list = new HashSet<GameObject> { obj.BottomTile, obj.TopTile };

			if (dependency != null) {
				bool added = list.Add(dependency);
				if (added)
					Debug.WriteLine("dependency (" + obj + "@" + obj.Tile + " --> " + dependency + "@" + dependency.Tile + ") added because " + reason);
			}
		}


		private void MarkDependencies(GameObject nowSatisfied) {
			var satisfiedQueue = new List<GameObject> { nowSatisfied };
			while (satisfiedQueue.Count > 0) {
				var mark = satisfiedQueue.Last();
				satisfiedQueue.RemoveAt(satisfiedQueue.Count - 1);

				// move the tile we're marking from the graph to the history
				_graph.Remove(mark);
				_hist.Add(mark);
				_histOrdered.Add(mark);
				// Debug.WriteLine("Inserting object " + mark + "@" + mark.Tile + " to hist");

				var prune = from entry in _graph where entry.Value.Remove(mark) && entry.Value.Count == 0 select entry.Key;
				// prune newly satisfied
				foreach (var obj in prune.ToList()) {
					_graph.Remove(obj);
					satisfiedQueue.Add(obj);
				}
			}
		}

		private object GetFrontBlock(GameObject objA, GameObject objB) {
			// magic, don't touch.
			// any kind of randomness or antisymmetry in this function
			// will lead to cyclic dependencies in the drawing order graph, 
			// resulting in neither object every being drawn.

			// tiles never overlap
			if (objA is MapTile && objB is MapTile) return null;

			var boxA = GetBoundingBox(objA);
			var boxB = GetBoundingBox(objB);
			if (!boxA.IntersectsWith(boxB)) return null;

			var hexA = GetIsoBoundingBox(objA);
			var hexB = GetIsoBoundingBox(objB);

			var sepAxis = Hexagon.GetSeparationAxis(hexA, hexB);

			// tiles can only be in front based on z-axis separation
			if ((objA is MapTile) ^ (objB is MapTile) && sepAxis != Axis.Z)
				return (objA is MapTile) ? objB : objA;

			// flat stuff always loses
			if (objA.Drawable.DrawFlat ^ objB.Drawable.DrawFlat) {
				if (sepAxis != Axis.Z)
					return (objA.Drawable.DrawFlat) ? objB : objA;
			}

			switch (sepAxis) {
				case Axis.X:
					if (hexA.xMin > hexB.xMax) return objA;
					else if (hexB.xMin > hexA.xMax) return objB;
					break;
				case Axis.Y:
					if (hexA.yMin > hexB.yMax) return objA;
					else if (hexB.yMin > hexA.yMax) return objB;
					break;
				case Axis.Z:
					if (hexA.zMin > hexB.zMax) return objA;
					else if (hexB.zMin > hexA.zMax) return objB;
					break;
			}

			// units on bridges can only be drawn after the bridge
			if (objA is OverlayObject && SpecialOverlays.IsHighBridge(objA as OverlayObject)
                && objB is OwnableObject && (objB as OwnableObject).OnBridge) return objB;
			else if (objB is OverlayObject && SpecialOverlays.IsHighBridge(objB as OverlayObject)
                     && objA is OwnableObject && (objA as OwnableObject).OnBridge) return objA;

			// no proper separation is possible, if one of both
			// objects is flat then mark the other one as in front,
			// otherwise use the one with lowest y
			if (objA.Drawable.DrawFlat && !objB.Drawable.DrawFlat) return objB;
			else if (objB.Drawable.DrawFlat && !objA.Drawable.DrawFlat) return objA;

			// try to make distinction based on object type
			// tile, smudge, overlay, terrain, unit/building, aircraft
			var priorities = new Dictionary<Type, int> {
                {typeof(MapTile), 0},
                {typeof(SmudgeObject), 1},
                {typeof(OverlayObject), 2},
                {typeof(TerrainObject), 3},
                {typeof(StructureObject), 3},
                {typeof(AnimationObject), 3},
                {typeof(UnitObject), 3},
                {typeof(InfantryObject), 3},
                {typeof(AircraftObject), 4},
            };
			int prioA = priorities[objA.GetType()];
			int prioB = priorities[objB.GetType()];

			if (prioA > prioB) return objA;
			else if (prioA < prioB) return objB;

			// finally try the minimal y coordinate
			if (boxA.Bottom > boxB.Bottom) return objA;
			else if (boxA.Bottom < boxB.Bottom) return objB;

			// finally if nothing worked up to here, which is very unlikely,
			// we'll use a tie-breaker that is at least guaranteed to yield
			// the same result regardless of argument order
			return objA.Id < objB.Id ? objA : objB;
		}

		public Rectangle GetBoundingBox(GameObject obj) {
			return obj.Drawable.GetBounds(obj);
		}

		public Hexagon GetIsoBoundingBox(GameObject obj) {
			if (obj is MapTile)
				return new Hexagon {
					xMin = obj.Tile.Rx,
					xMax = obj.Tile.Rx,
					yMin = obj.Tile.Ry,
					yMax = obj.Tile.Ry,
					zMin = obj.Tile.Z,
					zMax = obj.Tile.Z,
				};
			else if (obj is OwnableObject) {
				var oObj = obj as OwnableObject;
				return new Hexagon {
					xMin = obj.TopTile.Rx,
					xMax = obj.BottomTile.Rx,
					yMin = obj.TopTile.Ry,
					yMax = obj.BottomTile.Ry,
					zMin = obj.Tile.Z + (oObj.OnBridge ? 4 : 0),
					zMax = obj.Tile.Z + (oObj.OnBridge ? 4 : 0),
				};
			}
			else
				return new Hexagon {
					xMin = obj.TopTile.Rx,
					xMax = obj.BottomTile.Rx,
					yMin = obj.TopTile.Ry,
					yMax = obj.BottomTile.Ry,
					zMin = obj.Tile.Z + obj.Drawable.TileElevation,
					zMax = obj.Tile.Z + obj.Drawable.TileElevation,
				};
		}

	}
}