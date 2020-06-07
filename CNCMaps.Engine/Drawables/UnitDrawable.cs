using System.Collections.Generic;
using System.Drawing;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;

namespace CNCMaps.Engine.Drawables {
	internal class UnitDrawable : Drawable {

		public UnitDrawable(VirtualFileSystem vfs, IniFile.IniSection rules, IniFile.IniSection art)
			: base(vfs, rules, art) { }

		public override void LoadFromRules() {
			base.LoadFromArtEssential();

			ShpDrawable shp = null;
			VoxelDrawable vxl = null;

			if (IsVoxel) {
				vxl = new VoxelDrawable(_vfs, Rules, Art);
				vxl.OwnerCollection = OwnerCollection;
				vxl.Props = Props;
				vxl.LoadFromRules();
				vxl.Vxl = _vfs.Open<VxlFile>(vxl.Image + ".vxl");
				vxl.Hva = _vfs.Open<HvaFile>(vxl.Image + ".hva");
				SubDrawables.Add(vxl);
			}
			else {
				shp = new ShpDrawable(_vfs, Rules, Art);
				shp.Props = Props;
				shp.OwnerCollection = OwnerCollection;
				shp.LoadFromRules();
				shp.Shp = _vfs.Open<ShpFile>(shp.GetFilename());
				shp.Props.FrameDecider = FrameDeciders.SHPVehicleFrameDecider(shp.StartStandFrame, shp.StandingFrames, shp.StartWalkFrame, shp.WalkFrames, shp.Facings);
				SubDrawables.Add(shp);
			}

			if (shp != null || vxl != null) {
				if (Rules.ReadBool("Turret")) {

					VoxelDrawable vxlturret = null;
					ShpDrawable shpturret = null;
					var turretVxl = _vfs.Open<VxlFile>(Image + "TUR.vxl");
					var turretHva = _vfs.Open<HvaFile>(Image + "TUR.hva");

					if (turretVxl != null && turretHva != null) {
						vxlturret = new VoxelDrawable(turretVxl, turretHva);
						vxlturret.Props.Offset = Props.Offset;
						vxlturret.Props.Offset += new Size(Rules.ReadInt("TurretAnimX"), Rules.ReadInt("TurretAnimY"));
						vxlturret.Props.TurretVoxelOffset = Art.ReadFloat("TurretOffset");
						vxlturret.Props.Cloakable = Props.Cloakable;
						SubDrawables.Add(vxlturret);
					}

					if (vxlturret == null && shp != null) {
						shpturret = new ShpDrawable(_vfs, Rules, Art);
						shpturret.Props = (DrawProperties)shp.Props.Clone();
						shpturret.OwnerCollection = OwnerCollection;
						shpturret.LoadFromRules();
						shpturret.Shp = _vfs.Open<ShpFile>(shpturret.GetFilename());
						shpturret.Props.FrameDecider = FrameDeciders.SHPVehicleSHPTurretFrameDecider(shpturret.StartWalkFrame, shpturret.WalkFrames, shpturret.Facings);
						shpturret.Props.Cloakable = Props.Cloakable;
						SubDrawables.Add(shpturret);
					}

					var barrelVxl = _vfs.Open<VxlFile>(Image + "BARL.vxl");
					var barrelHva = _vfs.Open<HvaFile>(Image + "BARL.hva");
					if (barrelVxl != null && barrelHva != null) {
						var barrel = new VoxelDrawable(barrelVxl, barrelHva);
						if (vxlturret != null)
							barrel.Props = vxlturret.Props;
						else if (shp != null) {
							barrel.Props.Offset = Props.Offset;
							barrel.Props.Offset += new Size(Rules.ReadInt("TurretAnimX"), Rules.ReadInt("TurretAnimY"));
							barrel.Props.TurretVoxelOffset = Art.ReadFloat("TurretOffset");
						}
						barrel.Props.Cloakable = Props.Cloakable;
						SubDrawables.Add(barrel);
					}
				}
			}
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool shadows = true) {
			Size onBridgeOffset = Size.Empty;
			if (obj is OwnableObject && (obj as OwnableObject).OnBridge)
				onBridgeOffset = new Size(0, -4 * TileHeight / 2);

			foreach (var drawable in SubDrawables) {
				drawable.Props.Offset += onBridgeOffset;
				drawable.Draw(obj, ds, shadows);
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