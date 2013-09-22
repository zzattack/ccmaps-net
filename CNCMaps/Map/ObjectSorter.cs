using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using CNCMaps.Game;
using CNCMaps.Rendering;

namespace CNCMaps.Map {
	class ObjectSorter {
		private readonly TileLayer _map;
		private readonly Theater _t;
		// graph of (v, Adj[v])
		private readonly Dictionary<GameObject, HashSet<GameObject>> _graph = new Dictionary<GameObject, HashSet<GameObject>>();
		// objects with empty all their dependencies
		private readonly HashSet<GameObject> _hist = new HashSet<GameObject>();

		public ObjectSorter(Theater t, TileLayer map) {
			this._map = map;
			this._t = t;
		}

		internal IEnumerable<GameObject> GetOrderedObjects() {
			var ret = new List<GameObject>();

			Action<MapTile> processTile = tile => {
				if (tile == null) return;
				foreach (var obj in tile.AllObjects) {
					// every object depends on its bottom-most host tile at least
					AddDependency(obj, obj.TopTile, "top tile");
					ExamineNeighbourhood(obj);
				}

				// if this tile has no dependencies, then mark it and remove it
				// as dependency from all other objects
				if (!_graph.ContainsKey(tile) || _graph[tile].Count == 0) {
					var satisfied = MarkDependencies(tile);
					ret.AddRange(satisfied);
				}
			};

			for (int y = 0; y < _map.Height; y++) {
				for (int x = _map.Width * 2 - 2; x >= 0; x -= 2)
					processTile(_map[x, y]);
				for (int x = _map.Width * 2 - 3; x >= 0; x -= 2)
					processTile(_map[x, y]);
			}


			// assert no cyclics (one level deep)
			foreach (var entry in _graph) {
				foreach (var dep in entry.Value) {
					Debug.Assert(!_graph.ContainsKey(dep) || !_graph[dep].Contains(entry.Key), "", "cyclic dependency found between {0} and {1}", entry.Key, dep);
					// GetFrontBlock(dep, entry.Key);
					// GetFrontBlock(entry.Key, dep);
				}
			}

			// in a world where everything is perfectly drawable this should hold
			// Debug.Assert(_graph.Count == 0);
			while (_graph.Count != 0) {
				var leastDy = _graph.OrderBy(pair => pair.Key.TopTile.Dy).First().Key;
				ret.AddRange(MarkDependencies(leastDy));
			}

			for (int i = 0; i < ret.Count; i++) {
				var r = ret[i];
				r.DrawOrderIndex = i;
			}

			return ret;
		}

		internal void ExamineNeighbourhood(GameObject obj) {
			// Debug.WriteLine("Examining neighhourhood of " + obj);
			Debug.Assert(!_hist.Contains(obj), "examining neighbourhood for an object that's already in the draw list");

			Action<MapTile> examine = tile2 => {
				if (tile2 == null) return;
				// Debug.WriteLine("neighhourhood tile " + tile2 + " of obj " + obj + " at " + obj.Tile);

				// Debug.WriteLine("..EXAMINING " + obj);
				foreach (var obj2 in tile2.AllObjects) {
					if (obj2 == obj) continue;

					var front = GetFrontBlock(obj, obj2);

					if (front == obj && !_hist.Contains(obj2))
						AddDependency(obj, obj2, "obj in front");

					else if (front == obj2) {
						// obj2 is in front of obj, so so obj cannot have been drawn yet
						Debug.Assert(!_hist.Contains(obj2), "obj drawn before all its dependencies were found");
						AddDependency(obj2, obj, "obj2 in front");
					}
				}
			};

			for (int y = obj.TopTile.Dy - 2; y <= obj.BottomTile.Dy + 4; y++) {
				for (int x = obj.TopTile.Dx - 4; x <= obj.TopTile.Dx + 4; x += 2) {
					if (x >= 0 && y >= 0)
						examine(_map[x + (y + obj.TopTile.Dy) % 2, y / 2]);
				}
			}

		}

		private void AddDependency(GameObject obj, GameObject dependency, string reason = "") {
			HashSet<GameObject> list;
			if (!_graph.TryGetValue(obj, out list))
				_graph[obj] = list = new HashSet<GameObject> { obj.BottomTile, obj.TopTile };
			bool added = list.Add(dependency);
			if (added) Debug.WriteLine("dependency (" + obj + "@" + obj.Tile + "," + dependency + "@" + dependency.Tile + ") added because " + reason);
		}

		private IEnumerable<GameObject> MarkDependencies(GameObject nowSatisfied) {
			var satisfiedQueue = new List<GameObject> { nowSatisfied };
			while (satisfiedQueue.Count > 0) {
				var mark = satisfiedQueue.Last();
				satisfiedQueue.RemoveAt(satisfiedQueue.Count - 1);

				// move the tile we're marking from the graph to the history
				_graph.Remove(mark);
				_hist.Add(mark);
				// Debug.WriteLine("Inserting object " + mark + "@" + mark.Tile + " to hist");

				var prune = new List<GameObject> { };
				prune.AddRange(from tuple in _graph where tuple.Value.Remove(mark) where tuple.Value.Count == 0 select tuple.Key);
				// prune newly satisfied
				foreach (var obj in prune) {
					_graph.Remove(obj);
					satisfiedQueue.Add(obj);
				}

				yield return mark;
			}
		}

		private object GetFrontBlock(GameObject objA, GameObject objB) {
			// magic, don't touch.
			// any kind of randomness or antisymmetry in this function
			// will lead to cyclic dependencies in the drawing order graph, 
			// resulting in neither object every being drawn.

			var boxA = GetBoundingBox(objA);
			var boxB = GetBoundingBox(objB);
			if (!boxA.IntersectsWith(boxB)) return null;

			var hexA = GetIsoBoundingBox(objA);
			var hexB = GetIsoBoundingBox(objB);
			var sepAxis = Hexagon.GetSeparationAxis(hexA, hexB);
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
					if (hexA.zMin > hexB.zMin) return objA;
					else if (hexB.zMin > hexA.zMin) return objB;
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
				{ typeof(MapTile), 0 },
				{ typeof(SmudgeObject), 1 },
				{ typeof(OverlayObject), 2 },
				{ typeof(TerrainObject), 3 },
				{ typeof(StructureObject), 3 },
				{ typeof(AnimationObject), 4 },
				{ typeof(UnitObject), 5 },
				{ typeof(InfantryObject), 5 },
				{ typeof(AircraftObject), 6 },
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
			if (obj is MapTile) {
				return _t.GetTileCollection().GetTileFile(obj as MapTile).GetBounds(obj as MapTile);
			}
			else {
				return obj.Drawable.GetBounds(obj);
			}
		}

		public Hexagon GetIsoBoundingBox(GameObject obj) {
			if (obj is MapTile) return new Hexagon {
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
					zMin = obj.Tile.Z,
					zMax = obj.Tile.Z + (oObj.OnBridge ? 4 : 0),
				};
			}
			else
				return new Hexagon {
					xMin = obj.TopTile.Rx,
					xMax = obj.BottomTile.Rx,
					yMin = obj.TopTile.Ry,
					yMax = obj.BottomTile.Ry,
					zMin = obj.Tile.Z,
					zMax = obj.Tile.Z + obj.Drawable.TileElevation,
				};
		}

	}
}
