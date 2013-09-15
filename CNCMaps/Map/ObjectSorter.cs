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
		new Dictionary<GameObject, HashSet<GameObject>> dependencies = new Dictionary<GameObject, HashSet<GameObject>>();
		HashSet<GameObject> dependenciesFullfilled = new HashSet<GameObject>();

		public ObjectSorter(Theater t, TileLayer map) {
			this._map = map;
			this._t = t;
		}

		internal IEnumerable<GameObject> GetOrderedObjects() {
			var ret = new List<GameObject>();

			Action<MapTile> processTile = tile => {
				foreach (var obj in tile.AllObjects) {
					// find all dependencies for this object
					// all objects on this tile at least depend on their basetile
					AddDependency(obj, obj.BaseTile);
					// look around a bit
					ExamineNeighbourhood(tile, obj);
				}

				// satisfied objects can now be drawn
				var satisfied = MarkDependencies(tile);
				ret.AddRange(satisfied);
			};

			for (int y = 0; y < _map.Height; y++) {
				for (int x = _map.Width * 2 - 2; x >= 0; x -= 2)
					processTile(_map[x, y]);
				for (int x = _map.Width * 2 - 3; x >= 0; x -= 2)
					processTile(_map[x, y]);
			}

			return ret;
		}

		private void ExamineNeighbourhood(MapTile tile, GameObject obj) {
			Action<MapTile> examine = tile2 => {
				if (tile2 == null || tile == tile2) return;
				
				foreach (var obj2 in tile2.AllObjects) {
					var front = GetFrontBlock(obj, obj2);
					if (front == obj && !dependenciesFullfilled.Contains(obj2))
						AddDependency(obj, obj2);
					else if (front == obj2) {
						// if we already have drawn obj2.. well then we shouldnt have
						Debug.Assert(!dependenciesFullfilled.Contains(obj2));
						AddDependency(obj2, obj);
					}
				}

			};

			for (int y = obj.Tile.Dy - 8; y <= obj.BaseTile.Dy; y++) {
				if ((obj.Tile.Dy) % 2 == 1) {
					for (int x = obj.Tile.Dx - 7; x < obj.Tile.Dx + 6; x += 2) {
						Debug.Assert(_map[x, y] == null || _map[x + 1, y] == null || _map[x, y].Dy < _map[x + 1, y].Dy);
						examine(_map[x, y]);
					}
					for (int x = obj.Tile.Dx - 6; x < obj.Tile.Dx + 6; x += 2) {
						Debug.Assert(_map[x, y] == null || _map[x + 1, y] == null || _map[x, y].Dy > _map[x + 1, y].Dy);
						examine(_map[x, y]);
					}
				}
				else {
					for (int x = obj.Tile.Dx - 6; x < obj.Tile.Dx + 6; x += 2) {
						Debug.Assert(_map[x, y] == null || _map[x + 1, y] == null || _map[x, y].Dy < _map[x + 1, y].Dy);
						examine(_map[x, y]);
					}
					for (int x = obj.Tile.Dx - 7; x < obj.Tile.Dx + 6; x += 2) {
						Debug.Assert(_map[x, y] == null || _map[x + 1, y] == null || _map[x, y].Dy > _map[x + 1, y].Dy);
						examine(_map[x, y]);
					}
				}
			}



			for (int dy = obj.Tile.Dy - 8; dy <= obj.BaseTile.Dy; dy++) {
			}
		}

		private void AddDependency(GameObject obj, GameObject dependency) {
			HashSet<GameObject> list;
			if (!dependencies.TryGetValue(obj, out list))
				dependencies[obj] = list = new HashSet<GameObject>() { obj.BaseTile };
			list.Add(dependency);
		}

		private IEnumerable<GameObject> MarkDependencies(GameObject nowSatisfied) {
			var satisfiedQueue = new List<GameObject> { nowSatisfied };
			while (satisfiedQueue.Count > 0) {
				var mark = satisfiedQueue.Last();
				satisfiedQueue.RemoveAt(satisfiedQueue.Count - 1);

				List<GameObject> prune = new List<GameObject>();
				foreach (var tuple in dependencies) {
					tuple.Value.Remove(mark);
					if (tuple.Value.Count == 0) {
						prune.Add(tuple.Key);
						// now also remove this as dependency from those who depended on it
					}
				}
				// prune newly satisfied
				foreach (var obj in prune) {
					dependencies.Remove(obj);
					dependenciesFullfilled.Add(obj);
					// Debug.WriteLine("fulfilled at row " + obj.Tile.Dy);
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
				default: throw new Exception("blocks must be non-intersecting");
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
