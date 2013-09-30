using System.Collections.Generic;
using System.Drawing;
using CNCMaps.FileFormats;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.Engine.Game {
	class UnitDrawable : Drawable {
		private List<VoxelDrawable> vxls = new List<VoxelDrawable>();

		public UnitDrawable(IniFile.IniSection rules, IniFile.IniSection art)
			: base(rules, art) {
		}

		public override void LoadFromRules() {
			base.LoadFromRules();

			if (Rules.ReadBool("Turret") && IsVoxel) {
				var turretVxl = VFS.Open<VxlFile>(Image + "TUR.vxl");
				var turretHva = VFS.Open<HvaFile>(Image + "TUR.hva");
				var turret = new VoxelDrawable(Rules, Art, turretVxl, turretHva);
				turret.Props.Offset = new Point(Rules.ReadInt("TurretAnimX"), Rules.ReadInt("TurretAnimY"));
				turret.Props.TurretVoxelOffset = Art.ReadFloat("TurretOffset");
				vxls.Add(turret);

				var barrelVxl = VFS.Open<VxlFile>(Image + "BARL.vxl");
				var barrelHva = VFS.Open<HvaFile>(Image + "BARL.hva");
				if (barrelVxl != null && barrelHva != null) {
					var barrel = new VoxelDrawable(Rules, Art, turretVxl, turretHva);
					vxls.Add(barrel);
				}
			}
		}

	}
}
