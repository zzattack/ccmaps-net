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
		protected readonly Dictionary<string, Drawable> _drawablesDict = new Dictionary<string, Drawable>();

		public GameCollection() { }
		public GameCollection(CollectionType type, TheaterType theater, EngineType engine, IniFile rules, IniFile art) {
			Engine = engine;
			Theater = theater;
			Type = type;
			Rules = rules;
			Art = art;
		}

		public Drawable GetDrawable(GameObject o) {
			if (o is NamedObject) {
				Drawable ret;
				_drawablesDict.TryGetValue((o as NamedObject).Name, out ret);
				return ret;
			}
			else if (o is NumberedObject) {
				int idx = Math.Max(0, (o as NumberedObject).Number);
				if (idx >= 0 && idx < _drawables.Count)
					return _drawables[idx];
				else
					return null;
			}
			throw new ArgumentException();
		}
		public Drawable GetDrawable(string name) {
			Drawable ret;
			_drawablesDict.TryGetValue(name, out ret);
			return ret;
		}
		public bool HasObject(GameObject o) {
			return GetDrawable(o) != null;
		}
	}
}
