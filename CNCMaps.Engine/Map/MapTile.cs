using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CNCMaps.Engine.Game;
using CNCMaps.FileFormats;
using NLog;

namespace CNCMaps.Engine.Map {

	public class MapTile : NumberedObject { // inherit from gameobject to receive palette properties and such

		public ushort Dx { get; private set; }
		public ushort Dy { get; private set; }
		public ushort Rx { get; private set; }
		public ushort Ry { get; private set; }
		public short Z { get; private set; }

		public short TileNum { get; set; }
		public override int Number {
			get { return TileNum; }
			protected set { TileNum = (short)value; }
		}

		public short SetNum { get; set; }
		public ushort SubTile { get; set; }

		internal TileLayer Layer { get; private set; }
		internal bool ExtraDataAffected { get; set; }

		public ReadOnlyCollection<GameObject> AllObjects {
			get { return new ReadOnlyCollection<GameObject>(_allObjects); }
		}
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private readonly List<GameObject> _allObjects = new List<GameObject>();

		public MapTile(ushort dx, ushort dy, ushort rx, ushort ry, short rz, short tilenum, ushort subtile, TileLayer layer, short setnum = 0) {
			Dx = dx;
			Dy = dy;
			Rx = rx;
			Ry = ry;
			Z = rz;
			TileNum = tilenum;
			SetNum = setnum;
			SubTile = subtile;
			Layer = layer;
		}

		public void AddObject(GameObject obj) {
			_allObjects.Add(obj);
			obj.Tile = this;
		}
		public void RemoveObject(GameObject obj, bool silent = false) {
			if (!silent) Logger.Warn("Removing unknown object {0} from tile {1}", obj, this);
			bool removed = _allObjects.Remove(obj);
			if (!removed) Logger.Warn("Failed to reomve objects {0} from tile {1}", obj, this);
		}

		public override string ToString() {
			return string.Format("d({0},{1}),r({2},{3},{4})", Dx, Dy, Rx, Ry, Z);
		}

		public override MapTile Tile {
			get { return this; }
			set { throw new InvalidOperationException("lol wat u tryin bra"); }
		}

		public TmpFile GetTileFile() {
			return (Drawable as TileDrawable).GetTileFile(this);
		}

		public TmpFile.TmpImage GetTileImage() {
			return (Drawable as TileDrawable).GetTileImage(this);
		}
	}
}