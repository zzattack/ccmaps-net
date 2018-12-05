using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;
using CNCMaps.Shared.Utility;

namespace CNCMaps.Engine.Game {
	class BuildingDrawable : Drawable {

		#region crap
		private static readonly string[] LampNames = {
			"REDLAMP", "BLUELAMP", "GRENLAMP", "YELWLAMP", "PURPLAMP", "INORANLAMP", "INGRNLMP", "INREDLMP", "INBLULMP",
			"INGALITE", "GALITE", "TSTLAMP", 
			"INYELWLAMP", "INPURPLAMP", "NEGLAMP", "NERGRED", "TEMMORLAMP", "TEMPDAYLAMP", "TEMDAYLAMP", "TEMDUSLAMP",
			"TEMNITLAMP", "SNOMORLAMP",
			"SNODAYLAMP", "SNODUSLAMP", "SNONITLAMP"
		};

		private static readonly string[] AnimImages = {
			// "ProductionAnim",  // you don't want ProductionAnims on map renders, but IdleAnim instead
			"IdleAnim",
			"SuperAnim",
			// "Turret",
			//"SpecialAnimFour",
			//"SpecialAnimThree",
			//"SpecialAnimTwo",
			//"SpecialAnim",
			"ActiveAnimFour",
			"ActiveAnimThree",
			"ActiveAnimTwo",
			"ActiveAnim"
		};
		#endregion

		private ShpDrawable _baseShp;
		private readonly List<PowerupSlot> _powerupSlots = new List<PowerupSlot>();
		private readonly List<AnimDrawable> _anims = new List<AnimDrawable>();
		private readonly List<AnimDrawable> _animsDamaged = new List<AnimDrawable>();
		private readonly List<AnimDrawable> _fires = new List<AnimDrawable>();

		public BuildingDrawable(IniFile.IniSection rules, IniFile.IniSection art)
			: base(rules, art) {
		}

		public override void LoadFromRules() {
			base.LoadFromRules();

			IsBuildingPart = true;
			InvisibleInGame = Rules.ReadBool("InvisibleInGame") || LampNames.Contains(Name.ToUpper());
			string foundation = Art.ReadString("Foundation", "1x1");
			if (!foundation.Equals("custom", StringComparison.InvariantCultureIgnoreCase)) {
				int fx = foundation[0] - '0';
				int fy = foundation[2] - '0';
				Foundation = new Size(fx, fy);
			}
			else {
				int fx = Art.ReadInt("Foundation.X", 1);
				int fy = Art.ReadInt("Foundation.Y", 1);
				Foundation = new Size(fx, fy);
			}
			Props.SortIndex = Art.ReadInt("NormalYSort") - Art.ReadInt("NormalZAdjust"); // "main" building image before anims
			Props.ZShapePointMove = Art.ReadPoint("ZShapePointMove");

			_baseShp = new ShpDrawable(Rules, Art);
			_baseShp.OwnerCollection = OwnerCollection;
			_baseShp.LoadFromArtEssential();
			_baseShp.Props = Props;
			_baseShp.Shp = VFS.Open<ShpFile>(_baseShp.GetFilename());

			var extraProps = Props.Clone();
			extraProps.SortIndex = 0;
			foreach (string extraImage in AnimImages) {

				var extra = LoadExtraImage(extraImage, extraProps);
				if (extra != null && extra.Shp != null) {
					_anims.Add(extra);

					var extraDmg = LoadExtraImage(extraImage + "Damaged", extra.Props);
					if (extraDmg != null && extraDmg.Shp != null)
						_animsDamaged.Add(extraDmg);
					else // no damaged anim --> use normal anim also in damaged state
						_animsDamaged.Add(extra);
				}
			}

			// Starkku: New code for adding fire animations to buildings, supports custom-paletted animations.
			if (OwnerCollection.Engine >= EngineType.RedAlert2)
				LoadFireAnimations();

			// Add turrets
			if (Rules.ReadBool("Turret") && Rules.HasKey("TurretAnim")) {
				string turretName = Rules.ReadString("TurretAnim");
                // Starkku: NewTheater/generic image fallback support for turrets.
                string turretNameShp = NewTheater ? OwnerCollection.ApplyNewTheaterIfNeeded(turretName, turretName + ".shp") : turretName + ".shp";
				Drawable turret = Rules.ReadBool("TurretAnimIsVoxel")
					? (Drawable)new VoxelDrawable(VFS.Open<VxlFile>(turretName + ".vxl"), VFS.Open<HvaFile>(turretName + ".hva"))
					: new ShpDrawable(VFS.Open<ShpFile>(turretNameShp));
				turret.Props.Offset = Props.Offset + new Size(Rules.ReadInt("TurretAnimX"), Rules.ReadInt("TurretAnimY"));
				turret.Props.HasShadow = Rules.ReadBool("UseTurretShadow");
				turret.Props.FrameDecider = FrameDeciders.TurretFrameDecider;
				turret.Props.ZAdjust = Rules.ReadInt("TurretAnimZAdjust");
				SubDrawables.Add(turret);

				if (turret is VoxelDrawable && turretName.ToUpper().Contains("TUR")) {
					string barrelName = turretName.Replace("TUR", "BARL");
					if (VFS.Exists(barrelName + ".vxl")) {
						var barrel = new VoxelDrawable(VFS.Open<VxlFile>(barrelName + ".vxl"), VFS.Open<HvaFile>(barrelName + ".hva"));
						SubDrawables.Add(barrel);
						barrel.Props = turret.Props;
					}
				}
			}

			// Bib
			if (Art.HasKey("BibShape")) {
				var bibImg = Art.ReadString("BibShape") + ".shp";
				if (NewTheater)
					bibImg = OwnerCollection.ApplyNewTheaterIfNeeded(bibImg, bibImg);
				var bibShp = VFS.Open<ShpFile>(bibImg);
				if (bibShp != null) {
					var bib = new ShpDrawable(bibShp);
					bib.Props = this.Props.Clone();
					bib.Flat = true;
					SubDrawables.Add(bib);
				}
			}

			// Powerup slots, at most 3
			for (int i = 1; i <= 3; i++) {
				if (!Art.HasKey(String.Format("PowerUp{0}LocXX", i))) break;
				_powerupSlots.Add(new PowerupSlot {
					X = Art.ReadInt(String.Format("PowerUp{0}LocXX", i)),
					Y = Art.ReadInt(String.Format("PowerUp{0}LocYY", i)),
					Z = Art.ReadInt(String.Format("PowerUp{0}LocZZ", i)),
					YSort = Art.ReadInt(String.Format("PowerUp{0}LocYSort", i)),
				});
			}
		}

		private AnimDrawable LoadExtraImage(string extraImage, DrawProperties inheritProps) {
			string animSection = Art.ReadString(extraImage);
			if (animSection == "") return null;

			IniFile.IniSection extraRules = OwnerCollection.Rules.GetOrCreateSection(animSection);
			IniFile.IniSection extraArt = OwnerCollection.Art.GetOrCreateSection(animSection);
			var anim = new AnimDrawable(extraRules, extraArt);
			anim.OwnerCollection = OwnerCollection;
			anim.LoadFromRules();

			anim.NewTheater = this.NewTheater;

			if (extraArt.HasKey("YSortAdjust") || Art.HasKey(extraImage + "YSort") ||
				extraArt.HasKey("ZAdjust") || Art.HasKey(extraImage + "ZAdjust"))
				anim.Props.SortIndex = extraArt.ReadInt("YSortAdjust", Art.ReadInt(extraImage + "YSort"))
					- extraArt.ReadInt("ZAdjust", Art.ReadInt(extraImage + "ZAdjust"));
			else
				anim.Props.SortIndex = inheritProps.SortIndex;
			if (Art.HasKey(extraImage + "X") || Art.HasKey(extraImage + "Y"))
				anim.Props.Offset = this.Props.Offset + new Size(Art.ReadInt(extraImage + "X"), Art.ReadInt(extraImage + "Y"));
			else
				anim.Props.Offset = inheritProps.Offset;
			anim.Props.ZAdjust = Art.ReadInt(extraImage + "ZAdjust");
			anim.IsBuildingPart = true;

			anim.Shp = VFS.Open<ShpFile>(anim.GetFilename());
			return anim;
		}

		// Adds fire animations to a building. Supports custom-paletted animations.
		private void LoadFireAnimations() {
			// http://modenc.renegadeprojects.com/DamageFireTypes
			int f = 0;
			while (true) { // enumerate as many fires as are existing
				string dfo = Art.ReadString("DamageFireOffset" + f++);
				if (dfo == "")
					break;

				string[] coords = dfo.Split(new[] { ',', '.' }, StringSplitOptions.RemoveEmptyEntries);
				string fireAnim = OwnerCollection.FireNames[Rand.Next(OwnerCollection.FireNames.Length)];
				IniFile.IniSection fireArt = OwnerCollection.Art.GetOrCreateSection(fireAnim);

				var fire = new AnimDrawable(Rules, Art, VFS.Open<ShpFile>(fireAnim + ".shp"));
				fire.Props.PaletteOverride = GetFireAnimPalette(fireArt);
				fire.Props.Offset = new Point(Int32.Parse(coords[0]), Int32.Parse(coords[1]));
				fire.Props.FrameDecider = FrameDeciders.RandomFrameDecider;
				_fires.Add(fire);
			}
		}

		/* Finds out the correct name for an animation palette to use with fire animations.
		 * Reason why this is so complicated is because NPatch & Ares, the YR logic extensions that support custom animation palettes
		 * use different name for the flag declaring the palette. (NPatch uses 'Palette' whilst Ares uses 'CustomPalette' to make it distinct
		 * from the custom object palettes).
		 */
		private Palette GetFireAnimPalette(IniFile.IniSection animation) {
            // Starkku: Altered as a part of a fix for crash that happened if custom palette was declared but file wasn't there.
            Palette pal = null;
            if (animation.ReadString("Palette") != "")
            {
                pal = OwnerCollection.Palettes.GetCustomPalette(animation.ReadString("Palette"));
                if (pal == null) pal = OwnerCollection.Palettes.AnimPalette;
            }
            else if (animation.ReadString("CustomPalette") != "")
            {
                pal = OwnerCollection.Palettes.GetCustomPalette(animation.ReadString("CustomPalette"));
                if (pal == null) pal = OwnerCollection.Palettes.AnimPalette;
            }
            else if (animation.ReadString("AltPalette") != "")
                pal = OwnerCollection.Palettes.UnitPalette;
            else
                pal = OwnerCollection.Palettes.AnimPalette;
            return pal;
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool shadows = true) {
			if (InvisibleInGame)
				return;

			var drawList = new List<Drawable>();
			drawList.Add(_baseShp);

			if (obj is StructureObject && (obj as StructureObject).Health < 128) {
				drawList.AddRange(_animsDamaged);
				drawList.AddRange(_fires);
			}
			else
				drawList.AddRange(_anims);

			drawList.AddRange(SubDrawables); // bib
			/* order:
			ActiveAnims+Flat=yes
			BibShape
			ActiveAnims (+ZAdjust=0)
			Building
			ActiveAnims+ZAdjust=-32 */
			drawList = drawList.OrderBy(d => d.Flat ? -1 : 1).ThenBy(d => d.Props.SortIndex).ToList();

			foreach (var d in drawList)
				d.Draw(obj, ds, false);
			if (shadows)
				foreach (var d in drawList)
					d.DrawShadow(obj, ds);

			var strObj = obj as StructureObject;
			if (!strObj.Upgrade1.Equals("None", StringComparison.InvariantCultureIgnoreCase) && _powerupSlots.Count >= 1) {
				var powerup = OwnerCollection.GetDrawable(strObj.Upgrade1);
				DrawPowerup(obj, powerup, 0, ds);
			}

			if (!strObj.Upgrade2.Equals("None", StringComparison.InvariantCultureIgnoreCase) && _powerupSlots.Count >= 2) {
				var powerup = OwnerCollection.GetDrawable(strObj.Upgrade2);
				DrawPowerup(obj, powerup, 1, ds);
			}

			if (!strObj.Upgrade3.Equals("None", StringComparison.InvariantCultureIgnoreCase) && _powerupSlots.Count >= 3) {
				var powerup = OwnerCollection.GetDrawable(strObj.Upgrade3);
				DrawPowerup(obj, powerup, 2, ds);
			}
		}

		public override Rectangle GetBounds(GameObject obj) {
			Rectangle bounds = Rectangle.Empty;
			if (InvisibleInGame)
				return bounds;

			var parts = new List<Drawable>();
			parts.Add(_baseShp);
			//parts.AddRange(_anims);
			//parts.AddRange(SubDrawables);

			foreach (var d in parts.Where(p => !p.InvisibleInGame)) {
				var db = d.GetBounds(obj);
				if (db == Rectangle.Empty) continue;
				if (bounds == Rectangle.Empty) bounds = db;
				else bounds = Rectangle.Union(bounds, db);
			}
			return bounds;
		}

		public override void DrawBoundingBox(GameObject obj, Graphics gfx) {
			base.DrawBoundingBox(obj, gfx);

			var parts = new List<Drawable>();
			parts.Add(_baseShp);
			parts.AddRange(_anims);
			parts.AddRange(SubDrawables);

			foreach (var d in parts)
				d.DrawBoundingBox(obj, gfx);
		}

		public void DrawPowerup(GameObject obj, Drawable powerup, int slot, DrawingSurface ds) {
			if (slot >= _powerupSlots.Count)
				throw new InvalidOperationException("Building does not have requested slot ");

			var powerupOffset = new Point(_powerupSlots[slot].X, _powerupSlots[slot].Y);
			powerup.Props.Offset.Offset(powerupOffset);
			powerup.Draw(obj, ds);
			// undo offset
			powerup.Props.Offset.Offset(-powerupOffset.X, -powerupOffset.Y);
		}
	}


	public class PowerupSlot {
		public int X { get; set; }
		public int Y { get; set; }
		public int Z { get; set; }
		public int YSort { get; set; }
	}

}
