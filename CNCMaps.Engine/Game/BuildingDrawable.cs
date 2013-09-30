using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.Shared;
using CNCMaps.VirtualFileSystem;

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

		private static readonly Random R = new Random();

		private static readonly string[] AnimImages = {
			// "ProductionAnim",  // you don't want ProductionAnims on map renders, but IdleAnim instead
			"IdleAnim",
			"SuperAnim",
			// "Turret",
			"BibShape",
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

			InvisibleInGame = Rules.ReadBool("InvisibleInGame") ||
                // TS/FS have hardcoded lamps
                (OwnerCollection.Engine <= EngineType.Firestorm && LampNames.Contains(Name.ToUpper()));

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
			Props.SortIndex = Int32.MinValue; // "main" building image always first

			_baseShp = new ShpDrawable(Rules, Art);
			_baseShp.OwnerCollection = this.OwnerCollection;
			_baseShp.LoadFromRules();
			_baseShp.Props = this.Props;
			_baseShp.Shp = VFS.Open<ShpFile>(_baseShp.GetFilename());

			foreach (string extraImage in AnimImages) {
				var extra = LoadExtraImage(extraImage);
				if (extra != null) _anims.Add(extra);

				var extraDmg = LoadExtraImage(extraImage + "Damaged");
				if (extraDmg != null) _animsDamaged.Add(extraDmg);
			}

			// Starkku: New code for adding fire animations to buildings, supports custom-paletted animations.
			if (OwnerCollection.Engine >= EngineType.RedAlert2)
				LoadFireAnimations();

			// Add turrets
			if (Rules.ReadBool("Turret")) {
				IniFile.IniSection turretRules = OwnerCollection.Rules.GetOrCreateSection(Rules.ReadString("TurretAnim"));
				IniFile.IniSection turretArt = OwnerCollection.Art.GetOrCreateSection(Rules.ReadString("TurretAnim"));
				bool voxel = Rules.ReadBool("TurretAnimIsVoxel");
				Drawable turret;
				if (voxel) {
					string img = turretArt.ReadString("Image", turretArt.Name);
					var turretV = new VoxelDrawable(turretRules, turretArt,
						VFS.Open<VxlFile>(img+".vxl"), VFS.Open<HvaFile>(img+".hva"));
					turretV.OwnerCollection = OwnerCollection;
					turretV.LoadFromRules();
					turret = turretV;
				}
				else {
					var turretS = new ShpDrawable(turretRules, turretArt);
					turretS.OwnerCollection = OwnerCollection;
					turretS.LoadFromRules();
					turretS.Shp = VFS.Open<ShpFile>(turretS.GetFilename());
					turret = turretS;
				}
				turret.Props.Offset = Props.Offset + new Size(Rules.ReadInt("TurretAnimX"), Rules.ReadInt("TurretAnimY"));
				turret.Props.HasShadow = true;
				turret.Props.FrameDecider = FrameDeciders.TurretFrameDecider;
				SubDrawables.Add(turret);
				
				if (voxel && turret.Name.ToUpper().Contains("TUR")) {
					string barrelFile = turret.Name.Replace("TUR", "BARL");
					if (VFS.Exists(barrelFile + ".vxl")) {
						var barrel = new VoxelDrawable(null, null,
							VFS.Open<VxlFile>(barrelFile + ".vxl"), VFS.Open<HvaFile>(barrelFile + ".hva"));
						SubDrawables.Add(barrel);
						barrel.Props = turret.Props;
					}
				}
			}

			// Powerup slots, at most 3
			for (int i = 1; i <= 3; i++) {
				if (!Art.HasKey(String.Format("PowerUp{0}LocXX", i))) break;
				_powerupSlots.Add(new PowerupSlot() {
					X = Art.ReadInt(String.Format("PowerUp{0}LocXX", i)),
					Y = Art.ReadInt(String.Format("PowerUp{0}LocYY", i)),
					Z = Art.ReadInt(String.Format("PowerUp{0}LocZZ", i)),
					YSort = Art.ReadInt(String.Format("PowerUp{0}LocYSort", i)),
				});
			}
		}

		private AnimDrawable LoadExtraImage(string extraImage) {
			string animSection = Art.ReadString(extraImage);
			if (animSection == "") return null;

			IniFile.IniSection extraRules = OwnerCollection.Rules.GetOrCreateSection(animSection);
			IniFile.IniSection extraArt = OwnerCollection.Art.GetOrCreateSection(animSection);
			AnimDrawable anim = new AnimDrawable(extraRules, extraArt);
			anim.OwnerCollection = this.OwnerCollection;
			anim.LoadFromRules();

			anim.Props.Offset = Props.Offset;
			anim.NewTheater  =extraArt.ReadBool("NewTheater", NewTheater);
			anim.Props.SortIndex = extraArt.ReadInt("YSort", Art.ReadInt(extraImage + "YSort"));
			anim.Props.HasShadow = extraArt.ReadBool("Shadow", Props.HasShadow);

			anim.Props.FrameDecider = FrameDeciders.LoopFrameDecider(
				extraArt.ReadInt("LoopStart"),
				extraArt.ReadInt("LoopEnd", 1));

			anim.Props.Offset.Offset(Art.ReadInt(extraImage + "X"), Art.ReadInt(extraImage + "Y"));

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
				string fireAnim = OwnerCollection.FireNames[R.Next(OwnerCollection.FireNames.Length)];
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
			if (animation.ReadString("Palette") != "")
				return OwnerCollection.Palettes.GetCustomPalette(animation.ReadString("Palette"));
			else if (animation.ReadString("CustomPalette") != "")
				return OwnerCollection.Palettes.GetCustomPalette(animation.ReadString("CustomPalette"));
			else if (animation.ReadString("AltPalette") != "")
				return OwnerCollection.Palettes.UnitPalette;
			else
				return OwnerCollection.Palettes.AnimPalette;
		}

		public override void Draw(GameObject obj, DrawingSurface ds) {
			var drawList = new List<Drawable> { _baseShp };
			drawList.AddRange(SubDrawables);

			if (obj is StructureObject && (obj as StructureObject).Health < 128) {
				drawList.AddRange(_animsDamaged);
				drawList.AddRange(_fires);
			}
			else
				drawList.AddRange(_anims);

			foreach (var d in drawList.OrderBy(d => d.Props.SortIndex))
				d.Draw(obj, ds);

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
