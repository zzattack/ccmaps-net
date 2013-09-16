using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using CNCMaps.Game;
using CNCMaps.Rendering;
using CNCMaps.Utility;

namespace CNCMaps.Map {
	class ObjectSorter {
		private TileLayer _map;
		private Theater _t;
		// graph of (v, Adj[v])
		new Dictionary<GameObject, HashSet<GameObject>> _graph = new Dictionary<GameObject, HashSet<GameObject>>();
		// objects with empty all their dependencies
		HashSet<GameObject> _hist = new HashSet<GameObject>();

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
					AddDependency(obj, obj.BottomTile);
					// and on the topmost tile too, but that may be drawn already
					if (obj.TopTile != obj.BottomTile && !_hist.Contains(obj.TopTile))
						AddDependency(obj, obj.TopTile);

					ExamineNeighbourhood(tile, obj);
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
			
			Debug.Assert(_graph.Count == 0); // everything eventually resolved

			return ret;
		}

		private void ExamineNeighbourhood(MapTile tile, GameObject obj) {
			// Debug.WriteLine("Examining neighhourhood of " + tile + " at row " + tile.Dy + " for object " + obj);
			// Debug.Assert(!_hist.Contains(obj), "examining neighbourhood for an object that's already in the draw list");

			Action<MapTile> examine = tile2 => {
				if (tile2 == null) return;

				// Debug.WriteLine("..EXAMINING " + tile2);

				foreach (var obj2 in tile2.AllObjects) {
					if (obj2 == obj) continue;

					var front = GetFrontBlock(obj, obj2);

					if (front == obj && !_hist.Contains(obj2))
						AddDependency(obj, obj2);

					else if (front == obj2) {
						// obj2 is in front of obj, so so obj cannot have been drawn yet
						// Debug.Assert(!_hist.Contains(obj), "obj drawn before all its dependencies were found");
						AddDependency(obj2, obj);
					}
				}
			};

			for (int y = obj.TopTile.Dy; y <= obj.BottomTile.Dy + 4; y++) {
				// we need to start at the correct x, distinction on y
				int x = obj.TopTile.Dx - (obj.TopTile.Dy % 2 == 0 ? 4 : 5);
				for (; x <= obj.BottomTile.Dx + 4; x += 2) {
					examine(_map[x, y]);
				}
				x = obj.TopTile.Dx - (obj.TopTile.Dy % 2 == 1 ? 4 : 5);
				for (; x <= obj.BottomTile.Dx + 4; x += 2) {
					examine(_map[x, y]);
				}
			}
		}

		private void AddDependency(GameObject obj, GameObject dependency) {
			HashSet<GameObject> list;
			if (!_graph.TryGetValue(obj, out list))
				_graph[obj] = list = new HashSet<GameObject> { obj.BottomTile };
			bool added = list.Add(dependency);
			//if (added) Debug.WriteLine("dependency (" + obj + "@" + obj.Tile + "," + dependency + "@" + dependency.Tile + ") added");
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
				foreach (var tuple in _graph) {
					if (tuple.Value.Remove(mark)) {
						// Debug.WriteLine("dependency (" + tuple.Key + "@" + tuple.Key.Tile + "," + mark + "@" + mark.Tile + ") removed");
						if (tuple.Value.Count == 0) {
							prune.Add(tuple.Key);
							// Debug.WriteLine("dependencies satisfied for " + tuple.Key + " @ " + tuple.Key.Tile);
						}
					}
				}
				// prune newly satisfied
				foreach (var obj in prune) {
					_graph.Remove(obj);
					satisfiedQueue.Add(obj);
				}

				yield return mark;
			}
		}

		private object GetFrontBlock(GameObject objA, GameObject objB) {
			var boxA = GetIsoBoundingBox(objA);
			var boxB = GetIsoBoundingBox(objB);
			if (!boxA.IntersectsWith(boxB)) return null;

			var hexA = GetBoundingBox(objA);
			var hexB = GetBoundingBox(objB);
			var sepAxis = Hexagon.GetSeparationAxis(hexA, hexB);
			switch (sepAxis) {
				case Axis.X: return (hexA.xMin > hexB.xMax) ? objA : objB;
				case Axis.Y: return (hexA.yMin > hexB.yMax) ? objA : objB;
				case Axis.Z: return (hexA.zMin > hexB.zMin) ? objA : objB;
				default:
				case Axis.None: {
						// no proper separation is possible, if one of both
						// objects is flat then mark the other one as in front,
						// otherwise use the one with lowest y
						if (objA.Drawable.DrawFlat) return objB;
						else if (objB.Drawable.DrawFlat) return objA;
						else return hexA.yMin < hexB.yMin ? objA : objB;
					}
			}
		}

		public Rectangle GetIsoBoundingBox(GameObject obj) {
			if (obj is MapTile) {
				return _t.GetTileCollection().GetTileFile(obj as MapTile).GetBounds(obj as MapTile);
			}
			else {
				return obj.Drawable.GetBounds(obj);
			}
		}

		public Hexagon GetBoundingBox(GameObject obj) {
			if (obj is MapTile) return new Hexagon {
				xMin = obj.Tile.Rx,
				xMax = obj.Tile.Rx,
				yMin = obj.Tile.Ry,
				yMax = obj.Tile.Ry,
				zMin = obj.Tile.Z,
			};

			else return new Hexagon {
				xMin = obj.Tile.Rx,
				xMax = obj.Tile.Rx + obj.Drawable.Foundation.Width - 1,
				yMin = obj.Tile.Ry,
				yMax = obj.Tile.Ry + obj.Drawable.Foundation.Height - 1,
				zMin = obj.Tile.Z,
			};
		}

	}
}
