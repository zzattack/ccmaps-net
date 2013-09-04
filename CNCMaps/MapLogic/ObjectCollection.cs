using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Linq;
using CNCMaps.FileFormats;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.MapLogic {
	public class ObjectCollection {
		private readonly CollectionType _collectionType;
		private readonly TheaterType _theaterType;
		private readonly EngineType _engine;
		private readonly IniFile _rules;
		private readonly IniFile _art;
		private PaletteCollection _palettes;
		private readonly List<Drawable> _drawables = new List<Drawable>();
		private readonly Dictionary<string, Drawable> _drawablesDict = new Dictionary<string, Drawable>();

		#region cached stuff
		private static string[] _fires;
		private static readonly Random R = new Random();
		static readonly string[] ExtraBuildingImages = {
			"IdleAnim", // you don't want ProductionAnims on map renders // Why not? look at NACNST, the crane is missing!
			"SuperAnim",
			// "Turret",
			"BibShape",
			"SpecialAnimFour",
			"SpecialAnimThree",
			"SpecialAnimTwo",
			"SpecialAnim",
			"ActiveAnimFour",
			"ActiveAnimThree",
			"ActiveAnimTwo",
			"ActiveAnim"
		};
		#endregion

		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public ObjectCollection(IniFile.IniSection objectSection, CollectionType collectionType,
			TheaterType theaterType, EngineType engine, IniFile rules, IniFile art, PaletteCollection palettes) {
			this._theaterType = theaterType;
			this._engine = engine;
			this._collectionType = collectionType;
			this._rules = rules;
			this._art = art;
			this._palettes = palettes;

			if (engine >= EngineType.RedAlert2) {
				string fireNames = _rules.ReadString(_engine == EngineType.RedAlert2 ? "AudioVisual" : "General",
					"DamageFireTypes", "FIRE01,FIRE02,FIRE03");
				_fires = fireNames.Split(new[] { ',', '.' }, StringSplitOptions.RemoveEmptyEntries);
			}

			foreach (var entry in objectSection.OrderedEntries) {
				logger.Trace("Loading object {0}.{0}", objectSection.Name, entry.Value);
				LoadObject(entry.Value);
			}
		}

		private void LoadObject(string objName) {
			IniFile.IniSection rulesSection = _rules.GetSection(objName);
			var drawable = new Drawable(objName);
			_drawables.Add(drawable);
			_drawablesDict[objName] = drawable;

			if (rulesSection == null || rulesSection.ReadBool("IsRubble"))
				return;

			drawable.IsValid = true;
			string artSectionName = rulesSection.ReadString("Image", objName);
			IniFile.IniSection artSection = _art.GetSection(artSectionName);
			if (artSection == null)
				artSection = rulesSection;

			drawable.Rules = rulesSection;
			drawable.Art = artSection;

			drawable.PaletteType = Defaults.GetDefaultPalette(_collectionType, _engine);
			drawable.LightingType = Defaults.GetDefaultLighting(_collectionType);
			drawable.IsRemapable = Defaults.GetDefaultRemappability(_collectionType);
			var frameDecider = Defaults.GetDefaultFrameDecider(_collectionType);

			Point offset = new Point();
			Func<GameObject, Point> offsetHack = null;
			Func<GameObject, Point> shadowOffsetHack = null;

			string imageFileName = artSection.ReadString("Image", artSectionName);
			bool isVoxel = artSection.ReadBool("Voxel");
			bool theaterExtension = artSection.ReadBool("Theater");
			if (isVoxel) {
				imageFileName += ".vxl";
				if (_collectionType == CollectionType.Building) {
					// half tile to the left
					offset.X += Drawable.TileWidth / 2;
				}
				else if (_collectionType == CollectionType.Vehicle) {
					// also vertical tile center
					offset.X += Drawable.TileWidth / 2;
					offset.Y += Drawable.TileHeight / 2;
				}
			}
			else if (theaterExtension) {
				imageFileName += Defaults.GetExtension(_theaterType);
				if (_collectionType != CollectionType.Overlay || _engine <= EngineType.Firestorm) {
					drawable.PaletteType = PaletteType.Iso;
				}
			}
			else imageFileName += Defaults.GetExtension(_theaterType, _collectionType);

			// Find out foundation, now with custom foundation support (Ares feature).
			string foundation = artSection.ReadString("Foundation", "1x1");
			if (!foundation.Equals("custom", StringComparison.InvariantCultureIgnoreCase)) {
				int fx = foundation[0] - '0';
				int fy = foundation[2] - '0';
				drawable.Foundation = new Size(fx, fy);
			}
			else {
				int fx = artSection.ReadInt("Foundation.X", 1);
				int fy = artSection.ReadInt("Foundation.Y", 1);
				drawable.Foundation = new Size(fx, fy);
			}

			bool newTheater = artSection.ReadBool("NewTheater");
			// See if a theater-specific image is used
			if (newTheater) {
				// http://modenc.renegadeprojects.com/NewTheater
				ApplyNewTheaterIfNeeded(artSectionName, ref imageFileName);
			}

			if (artSection.ReadString("Remapable") != string.Empty) {
				// does NOT work in RA2
				if (_engine <= EngineType.Firestorm)
					drawable.IsRemapable = artSection.ReadBool("Remapable");
			}

			// Used palet can be overriden
			bool noUseTileLandType = rulesSection.ReadString("NoUseTileLandType") != "";
			if (noUseTileLandType) {
				drawable.PaletteType = PaletteType.Iso;
				drawable.LightingType = LightingType.Full;
			}
			else if (artSection.ReadBool("TerrainPalette")) {
				drawable.PaletteType = PaletteType.Iso;
			}
			else if (artSection.ReadBool("AnimPalette")) {
				drawable.PaletteType = PaletteType.Anim;
				drawable.LightingType = LightingType.None;
			}
			else if (artSection.ReadBool("AltPalette")) {
				// If AltPalette=yes is set on an animation then that animation will use the unit palette instead of the animation palette. 
				// However, remappable colours are ignored - they will not be remapped.
				drawable.PaletteType = PaletteType.Unit;
				drawable.IsRemapable = false;
			}
			else if (artSection.ReadString("Palette") != string.Empty) {
				drawable.PaletteType = PaletteType.Custom;
				drawable.CustomPaletteName = artSection.ReadString("Palette");
			}

			if (rulesSection.ReadString("AlphaImage") != "") {
				string alphaImageFile = rulesSection.ReadString("AlphaImage") + ".shp";
				if (VFS.Exists(alphaImageFile))
					drawable.SetAlphaImage(VFS.Open(alphaImageFile) as ShpFile);
			}

			if (rulesSection.ReadBool("Wall")) {
				drawable.IsWall = true;
				drawable.PaletteType = PaletteType.Unit;
				drawable.LightingType = LightingType.Ambient;
				frameDecider = FrameDeciders.OverlayValueFrameDecider;
			}

			if (rulesSection.ReadBool("Gate")) {
				drawable.IsGate = true;
				drawable.PaletteType = PaletteType.Unit;
				frameDecider = FrameDeciders.NullFrameDecider;
			}

			bool shadow = artSection.ReadBool("Shadow", Defaults.GetShadowAssumption(_collectionType));
			if (!rulesSection.ReadBool("DrawFlat", true))
				shadow = true;

			if (rulesSection.ReadBool("BridgeRepairHut")) {
				// xOffset = yOffset = 0; // TOOD: check we really don't need this
			}
			if (rulesSection.ReadBool("IsVeins")) {
				drawable.LightingType = LightingType.None;
				drawable.PaletteType= PaletteType.Unit;
			}
			if (rulesSection.ReadBool("IsVeinholeMonster")) {
				offset.Y = -48; // why is this needed???
				drawable.LightingType = LightingType.None;
				drawable.PaletteType = PaletteType.Unit;
			}
			if (_collectionType == CollectionType.Terrain) {
				offset.Y += Drawable.TileHeight / 2; // trees and such are placed in the middle of their tile
			}
			if (rulesSection.ReadString("Land") == "Rock") {
				offset.Y += Drawable.TileHeight / 2;
			}
			else if (rulesSection.ReadString("Land") == "Road") {
				offset.Y = Drawable.TileHeight / 2;
				drawable.Foundation = new Size(3, 1); // ensures bridges are drawn a bit lower than where they're stored
			}
			else if (rulesSection.ReadString("Land") == "Railroad") {
				offset.Y = 14;
				drawable.LightingType = LightingType.Full;
			}
			if (rulesSection.ReadBool("SpawnsTiberium")) {
				// For example on TIBTRE / Ore Poles
				offset.Y = -1;
				drawable.LightingType = LightingType.None;
				drawable.PaletteType = PaletteType.Unit;
			}

			if (_collectionType == CollectionType.Overlay) {
				int objIdx = _drawables.Count - 1;
				var ovl = new OverlayObject((byte)objIdx, 0);
				var tibType = SpecialOverlays.GetOverlayTibType(ovl, _engine);

				if (_engine >= EngineType.RedAlert2) {
					if (tibType != OverlayTibType.NotSpecial) {
						drawable.PaletteType = PaletteType.Overlay;
						drawable.LightingType = LightingType.None;
					}
					else if (SpecialOverlays.IsHighBridge(ovl)) {
						offsetHack = OffsetHacks.RA2BridgeOffsets;
						shadowOffsetHack = OffsetHacks.RA2BridgeShadowOffsets;
						drawable.HeightOffset = 1; // bridge has height 4 and roughly foundation 3x1 totaling 4
						//drawable.Foundation = new Size(3, 1);
					}
				}
				else if (_engine <= EngineType.Firestorm) {
					if (tibType != OverlayTibType.NotSpecial) {
						drawable.PaletteType = PaletteType.Unit;
						drawable.LightingType = LightingType.None;
						drawable.IsRemapable = true;
					}
					else if (SpecialOverlays.IsTSRails(ovl))
						offset.Y += 11;
					else if (SpecialOverlays.IsHighBridge(ovl)) {
						offsetHack = OffsetHacks.TSBridgeOffsets;
						shadowOffsetHack = OffsetHacks.TSBridgeShadowOffsets;
						drawable.HeightOffset = 2;
					}
				}
			}

			drawable.Overrides = rulesSection.ReadBool("Overrides", drawable.Overrides);

			AddImageToObject(drawable, imageFileName,
				new DrawProperties {
					Offset = offset,
					HasShadow = shadow,
					FrameDecider = frameDecider,
					OffsetHack = offsetHack,
					ShadowOffsetHack = shadowOffsetHack,
				});

			// Buildings often consist of multiple SHP files
			if (_collectionType == CollectionType.Building) {
				var damagedProps = new DrawProperties {
					HasShadow = shadow,
					FrameDecider = frameDecider, // this is not an animation with loopstart/loopend yet
				};
				drawable.AddDamagedShp(VFS.Open<ShpFile>(imageFileName), damagedProps);

				foreach (string extraImage in ExtraBuildingImages) {
					string extraImageDamaged = extraImage + "Damaged";
					string extraImageSectionName = artSection.ReadString(extraImage);
					string extraImageDamagedSectionName = artSection.ReadString(extraImageDamaged, extraImageSectionName);

					if (extraImageSectionName != "") {
						IniFile.IniSection extraArtSection = _art.GetOrCreateSection(extraImageSectionName);

						int ySort = 0;
						bool extraShadow = false;
						string extraImageFileName = extraImageSectionName;

						if (extraArtSection != null) {
							ySort = extraArtSection.ReadInt("YSort", artSection.ReadInt(extraImage + "YSort"));
							extraShadow = extraArtSection.ReadBool("Shadow", extraShadow);
							extraImageFileName = extraArtSection.ReadString("Image", extraImageSectionName);
							frameDecider = FrameDeciders.LoopFrameDecider(
								extraArtSection.ReadInt("LoopStart"),
								extraArtSection.ReadInt("LoopEnd", 1));
						}
						if (theaterExtension)
							extraImageFileName += Defaults.GetExtension(_theaterType);
						else
							extraImageFileName += Defaults.GetExtension(_theaterType, _collectionType);

						if (newTheater)
							ApplyNewTheaterIfNeeded(artSectionName, ref extraImageFileName);

						var props = new DrawProperties {
							HasShadow = extraShadow,
							Offset = offset,
							ShadowOffset = offset,
							SortIndex = ySort,
							FrameDecider = frameDecider,
							OverridesZbuffer = true,
						};
						AddImageToObject(drawable, extraImageFileName, props);
					}

					if (extraImageDamagedSectionName != "") {
						IniFile.IniSection extraArtDamagedSection = _art.GetSection(extraImageDamagedSectionName);

						int ySort = 0;
						bool extraShadow = false;
						string extraImageDamagedFileName = extraImageDamagedSectionName;
						if (extraArtDamagedSection != null) {
							ySort = extraArtDamagedSection.ReadInt("YSort", artSection.ReadInt(extraImage + "YSort"));
							extraShadow = extraArtDamagedSection.ReadBool("Shadow", extraShadow);
							extraImageDamagedFileName = extraArtDamagedSection.ReadString("Image", extraImageDamagedSectionName);
							frameDecider = FrameDeciders.LoopFrameDecider(
								extraArtDamagedSection.ReadInt("LoopStart"),
								extraArtDamagedSection.ReadInt("LoopEnd", 1));
						}
						if (theaterExtension)
							extraImageDamagedFileName += Defaults.GetExtension(_theaterType);
						else
							extraImageDamagedFileName += Defaults.GetExtension(_theaterType, _collectionType);

						if (newTheater)
							ApplyNewTheaterIfNeeded(artSectionName, ref extraImageDamagedFileName);

						var props = new DrawProperties {
							HasShadow = extraShadow,
							SortIndex = ySort,
							Offset = offset,
							ShadowOffset = offset,
							FrameDecider = frameDecider,
							OverridesZbuffer = true,
						};
						drawable.AddDamagedShp(VFS.Open(extraImageDamagedFileName) as ShpFile, props);
					}
				}

				// Starkku: New code for adding fire animations to buildings, supports custom-paletted animations.
				if (_engine >= EngineType.RedAlert2)
					LoadFireAnimations(artSection, drawable);

				// Add turrets
				if (rulesSection.ReadBool("Turret")) {
					string img = rulesSection.ReadString("TurretAnim");
					IniFile.IniSection turretart = _art.GetSection(img);
					bool voxel = rulesSection.ReadBool("TurretAnimIsVoxel");
					img += voxel ? ".vxl" : ".shp";
					if (turretart != null && turretart.ReadBool("NewTheater") && img.EndsWith(".shp")) {
						ApplyNewTheaterIfNeeded(img, ref img);
					}
					var turretOffset = new Point(rulesSection.ReadInt("TurretAnimX"), rulesSection.ReadInt("TurretAnimY"));
					if (voxel) {
						turretOffset.Offset(Drawable.TileWidth / 2, 0);
						if (_collectionType == CollectionType.Vehicle)
							turretOffset.Y += (int)((turretart.ReadDouble("TurretOffset") * Drawable.TileHeight) / 256);
					}

					var props = new DrawProperties {
						Offset = turretOffset,
						ShadowOffset = turretOffset,
						HasShadow = true,
						FrameDecider = FrameDeciders.TurretFrameDecider,
					};
					AddImageToObject(drawable, img, props);

					string barrelFile = img.Replace("TUR", "BARL");
					if (VFS.Exists(barrelFile)) {
						AddImageToObject(drawable, barrelFile, props);
					}

				}
			}

			else if (_collectionType == CollectionType.Vehicle) {
				// Add turrets
				if (rulesSection.ReadBool("Turret") && artSection.ReadBool("Voxel")) {
					string turretFile = Path.GetFileNameWithoutExtension(imageFileName) + "TUR.vxl";
					Point voxelOffset = new Point(rulesSection.ReadInt("TurretAnimX"), rulesSection.ReadInt("TurretAnimY"));
					voxelOffset.Offset(offset);
					var props = new DrawProperties {
						Offset = voxelOffset,
					};
					AddImageToObject(drawable, turretFile, props);

					string barrelFile = turretFile.Replace("TUR", "BARL");
					if (VFS.Exists(barrelFile))
						AddImageToObject(drawable, barrelFile, props);

				}
			}
		}

		// Adds fire animations to a building. Supports custom-paletted animations.
		private void LoadFireAnimations(IniFile.IniSection artSection, Drawable drawableObject) {
			// http://modenc.renegadeprojects.com/DamageFireTypes
			int f = 0;
			while (true) { // enumerate as many fires as are existing
				string dfo = artSection.ReadString("DamageFireOffset" + f++);
				if (dfo == "")
					break;

				string[] coords = dfo.Split(new[] { ',', '.' }, StringSplitOptions.RemoveEmptyEntries);
				string fireAnim = _fires[R.Next(_fires.Length)];
				IniFile.IniSection fireArt = _art.GetOrCreateSection(fireAnim);

				var props = new DrawProperties {
					PaletteOverride = GetFireAnimPalette(fireArt),
					Offset = new Point(int.Parse(coords[0]), int.Parse(coords[1])),
					FrameDecider = FrameDeciders.RandomFrameDecider,
					OverridesZbuffer = true,
				};
				drawableObject.AddFire(VFS.Open<ShpFile>(fireAnim + ".shp"), props);
			}
		}

		/* Finds out the correct name for an animation palette to use with fire animations.
		 * Reason why this is so complicated is because NPatch & Ares, the YR logic extensions that support custom animation palettes
		 * use different name for the flag declaring the palette. (NPatch uses 'Palette' whilst Ares uses 'CustomPalette' to make it distinct
		 * from the custom object palettes).
		 */
		private Palette GetFireAnimPalette(IniFile.IniSection animation) {
			if (animation.ReadString("Palette") != "")
				return _palettes.GetCustomPalette(animation.ReadString("Palette"));
			else if (animation.ReadString("CustomPalette") != "")
				return _palettes.GetCustomPalette(animation.ReadString("CustomPalette"));
			else if (animation.ReadString("AltPalette") != "")
				return _palettes.UnitPalette;
			else
				return _palettes.AnimPalette;
		}

		private void ApplyNewTheaterIfNeeded(string artName, ref string imageFileName) {
			if (_engine <= EngineType.Firestorm) {
				// the tag will only work if the ID for the object starts with either G, N or C and its second letter is A (for Arctic/Snow theater) or T (for Temperate theater)
				if (new[] { 'G', 'N', 'C' }.Contains(artName[0]) && new[] { 'A', 'T' }.Contains(artName[1]))
					ApplyNewTheater(ref imageFileName);
			}
			else if (_engine == EngineType.RedAlert2) {
				// In RA2, for the tag to work, it must start with either G, N or C, and its second letter must be A, T or U (Urban theater). 
				if (new[] { 'G', 'N', 'C' }.Contains(artName[0]) && new[] { 'A', 'T', 'U' }.Contains(artName[1]))
					ApplyNewTheater(ref imageFileName);
			}
			else {
				//  In Yuri's Revenge, the ID can also start with Y."
				if (new[] { 'G', 'N', 'C', 'Y' }.Contains(artName[0]) && new[] { 'A', 'T', 'U' }.Contains(artName[1]))
					ApplyNewTheater(ref imageFileName);
			}
		}

		private void AddImageToObject(Drawable drawableObject, string fileName, DrawProperties drawProps) {
			if (fileName.EndsWith(".vxl")) {
				var vxl = VFS.Open<VxlFile>(fileName);
				if (vxl != null) {
					string hvaFileName = Path.ChangeExtension(fileName, ".hva");
					var hva = VFS.Open(hvaFileName) as HvaFile;
					drawableObject.AddVoxel(vxl, hva, drawProps);
				}
			}
			else {
				var shp = VFS.Open<ShpFile>(fileName);
				if (shp != null)
					drawableObject.AddShp(shp, drawProps);
			}
		}

		private void ApplyNewTheater(ref string imageFileName) {
			var sb = new StringBuilder(imageFileName);
			sb[1] = Defaults.GetTheaterPrefix(_theaterType);
			if (!VFS.Exists(sb.ToString())) {
				sb[1] = 'G'; // generic
			}
			imageFileName = sb.ToString();
		}

		public Drawable GetDrawable(GameObject o) {
			if (o is NamedObject) {
				Drawable ret;
				_drawablesDict.TryGetValue((o as NamedObject).Name, out ret);
				return ret;
			}
			else if (o is NumberedObject) {
				int idx = (o as NumberedObject).Number;
				if (idx >= 0 && idx < _drawables.Count)
					return _drawables[idx];
				else
					return null;
			}
			throw new ArgumentException();
		}

		public void Draw(GameObject o, DrawingSurface drawingSurface) {
			Drawable d = GetDrawable(o);
			d.Draw(o, drawingSurface);
		}

		public bool HasObject(GameObject o) {
			var obj = GetDrawable(o);
			return obj != null && obj.IsValid;
		}
	}
}