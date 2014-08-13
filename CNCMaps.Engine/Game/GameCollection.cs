using System;
using System.Collections.Generic;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Map;
using CNCMaps.FileFormats;
using CNCMaps.Shared;

namespace CNCMaps.Engine.Game {
	public abstract class GameCollection {
		public readonly CollectionType Type;
		public readonly TheaterType Theater;
		public readonly EngineType Engine;
		public readonly IniFile Rules;
		public readonly IniFile Art;
		protected readonly List<Drawable> _drawables = new List<Drawable>();
		protected readonly Dictionary<int, string> _drawableIndexNameMap = new Dictionary<int, string>();
		private readonly Dictionary<string, Drawable> _drawablesDict = new Dictionary<string, Drawable>();
		private readonly Dictionary<Drawable, bool> _drawableLoaded = new Dictionary<Drawable, bool>();

		protected GameCollection() { }

		protected GameCollection(CollectionType type, TheaterType theater, EngineType engine, IniFile rules, IniFile art) {
			Engine = engine;
			Theater = theater;
			Type = type;
			Rules = rules;
			Art = art;
		}

		protected Drawable AddObject(string objName) {
			Drawable sub = MakeDrawable(objName);
			sub.OwnerCollection = this as ObjectCollection;
			sub.Index = _drawables.Count;
			_drawableIndexNameMap[_drawables.Count] = objName;
			_drawables.Add(sub);
			_drawablesDict[objName] = sub;
			_drawableLoaded[sub] = false;
			return sub;
		}

		public Drawable GetDrawable(GameObject o) {
			if (o is NamedObject)
				return GetDrawable((o as NamedObject).Name);
			if (o is NumberedObject) {
				int idx = Math.Max(0, (o as NumberedObject).Number);
				if (idx >= 0 && idx < _drawables.Count)
					return GetDrawable(idx);
			}
			return null;
		}

		public Drawable GetDrawable(string name) {
			Drawable ret = _drawablesDict[name];
			if (!_drawableLoaded[ret]) {
				_drawableLoaded[ret] = true;
				LoadDrawable(ret);
			}
			return ret;
		}

		public Drawable GetDrawable(int index) {
			Drawable ret = _drawables[index];
			if (!_drawableLoaded[ret]) {
				_drawableLoaded[ret] = true;
				LoadDrawable(ret);
			}
			return ret;
		}

		public bool HasObject(GameObject o) {
			return GetDrawable(o) != null;
		}

		protected abstract Drawable MakeDrawable(string objName);
		protected virtual void LoadDrawable(Drawable d) { 	}
	}
}
