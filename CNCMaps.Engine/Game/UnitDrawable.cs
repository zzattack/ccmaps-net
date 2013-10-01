using System.Collections.Generic;
using System.Drawing;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;

namespace CNCMaps.Engine.Game {
	internal class UnitDrawable : Drawable {

		public UnitDrawable(IniFile.IniSection rules, IniFile.IniSection art)
			: base(rules, art) { }

		public override void LoadFromRules() {
			base.LoadFromRulesEssential();

			if (IsVoxel) {
				var vxl = new VoxelDrawable(Rules, Art);
				vxl.OwnerCollection = OwnerCollection;
				vxl.Props = Props;
				vxl.LoadFromRules();
				vxl.Vxl = VFS.Open<VxlFile>(vxl.Image + ".vxl");
				vxl.Hva = VFS.Open<HvaFile>(vxl.Image + ".hva");
				SubDrawables.Add(vxl);
			}
			else {
				var shp = new ShpDrawable(Rules, Art);
				shp.Props = Props;
				shp.OwnerCollection = OwnerCollection;
				shp.LoadFromRules();
				shp.Shp = VFS.Open<ShpFile>(shp.GetFilename());
				SubDrawables.Add(shp);
			}

			if (Rules.ReadBool("Turret") && IsVoxel) {
				var turretVxl = VFS.Open<VxlFile>(Image + "TUR.vxl");
				var turretHva = VFS.Open<HvaFile>(Image + "TUR.hva");
				var turret = new VoxelDrawable(turretVxl, turretHva);
				turret.Props.Offset = Props.Offset;
				turret.Props.Offset += new Size(Rules.ReadInt("TurretAnimX"), Rules.ReadInt("TurretAnimY"));
				turret.Props.TurretVoxelOffset = Art.ReadFloat("TurretOffset");
				SubDrawables.Add(turret);

				var barrelVxl = VFS.Open<VxlFile>(Image + "BARL.vxl");
				var barrelHva = VFS.Open<HvaFile>(Image + "BARL.hva");
				if (barrelVxl != null && barrelHva != null) {
					var barrel = new VoxelDrawable(barrelVxl, barrelHva);
					barrel.Props = turret.Props;
					SubDrawables.Add(barrel);
				}
			}
		}

		public override void Draw(GameObject obj, DrawingSurface ds) {
			Size onBridgeOffset = Size.Empty;
			if (obj is OwnableObject && (obj as OwnableObject).OnBridge)
				onBridgeOffset = new Size(0, -4 * TileHeight / 2);

			foreach (var drawable in SubDrawables) {
				drawable.Props.Offset += onBridgeOffset;
				drawable.Draw(obj, ds);
				drawable.Props.Offset -= onBridgeOffset;
			}
		}

		public override Rectangle GetBounds(GameObject obj) {
			Rectangle bounds = Rectangle.Empty;
			var parts = new List<Drawable>();
			parts.AddRange(SubDrawables);

			foreach (var d in parts) {
				var db = d.GetBounds(obj);
				if (db == Rectangle.Empty) continue;
				if (bounds == Rectangle.Empty) bounds = db;
				else bounds = Rectangle.Union(bounds, db);
			}

			Point onBridgeOffset = Point.Empty;
			if (obj is OwnableObject && (obj as OwnableObject).OnBridge)
				onBridgeOffset = new Point(0, -4 * TileHeight / 2);
			bounds.Offset(onBridgeOffset);

			return bounds;
		}

	}
}