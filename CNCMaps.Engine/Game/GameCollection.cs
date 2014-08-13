using System;
using System.Collections.Generic;
using CNCMaps.Engine.Map;
using CNCMaps.FileFormats;
using CNCMaps.Shared;

namespace CNCMaps.Engine.Game {
	public class GameCollection {
		public readonly CollectionType Type;
		public readonly TheaterType Theater;
		public readonly EngineType Engine;
		public readonly IniFile Rules;
		public readonly IniFile Art;
		protected readonly List<Drawable> _drawables = new List<Drawable>();
		protected readonly Dictionary<int, string> _drawableIndexNameMap = new Dictionary<int, string>();
		protected readonly Dictionary<string, Drawable> _drawablesDict = new Dictionary<string, Drawable>();

		public GameCollection() { }
		public GameCollection(CollectionType type, TheaterType theater, EngineType engine, IniFile rules, IniFile art) {
			Engine = engine;
			Theater = theater;
			Type = type;
			Rules = rules;
			Art = art;
		}

		protected void AddObject(string objName) {
			_drawableIndexNameMap[_drawables.Count] = objName;
			_drawables.Add(null);
			_drawablesDict[objName] = null;
		}

		public Drawable GetDrawable(GameObject o) {
			string name = null;
			if (o is NamedObject)
				name = (o as NamedObject).Name;
			else if (o is NumberedObject) {
				int idx = Math.Max(0, (o as NumberedObject).Number);
				if (idx >= 0 && idx < _drawables.Count)
					return _drawables[idx] ?? LoadObject(idx);
			}
			if (name == null)
				throw new ArgumentException();
			return GetDrawable(name);
		}
		public Drawable GetDrawable(string name) {
			Drawable ret;
			_drawablesDict.TryGetValue(name, out ret);
			return ret ?? (ret = LoadObject(name));
		}
		public bool HasObject(GameObject o) {
			return GetDrawable(o) != null;
		}

		protected virtual Drawable LoadObject(string objName) { return null; }
		protected virtual Drawable LoadObject(int idx) {
			string name = _drawableIndexNameMap[idx];
			return LoadObject(name);
		}
	}
}
