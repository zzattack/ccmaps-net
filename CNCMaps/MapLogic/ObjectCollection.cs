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
	class ObjectCollection {
		private readonly CollectionType _collectionType;
		private readonly TheaterType _theaterType;
		private readonly EngineType _engineType;
		private readonly IniFile _rules;
		private readonly IniFile _art;
		private PaletteCollection _palettes;
		private readonly List<Drawable> _drawables = new List<Drawable>();
		private readonly Dictionary<string, Drawable> _drawablesDict = new Dictionary<string, Drawable>();

		static readonly string[] ExtraBuildingImages = {
			// "ProductionAnim", // you don't want ProductionAnims on map renders
			"SuperAnim",
			"Turret",
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

		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public ObjectCollection(IniFile.IniSection objectSection, CollectionType collectionType,
			TheaterType theaterType, EngineType engineType, IniFile rules, IniFile art, PaletteCollection palettes) {
			this._theaterType = theaterType;
			this._engineType = engineType;
			this._collectionType = collectionType;
			this._rules = rules;
			this._art = art;
			this._palettes = palettes;
			foreach (var entry in objectSection.OrderedEntries) {
				logger.Trace("Loading object {0}.{0}", objectSection.Name, entry.Value);
				LoadObject(entry.Value);
			}
		}

		private void LoadObject(string objName) {
			IniFile.IniSection rulesSection = _rules.GetSection(objName);
			var drawableObject = new Drawable(objName);
			_drawables.Add(drawableObject);
			_drawablesDict[objName] = drawableObject;

			if (rulesSection == null || rulesSection.ReadBool("IsRubble"))
				return;

			drawableObject.IsValid = true;
			string artSectionName = rulesSection.ReadString("Image", objName);
			IniFile.IniSection artSection = _art.GetSection(artSectionName);
			if (artSection == null)
				artSection = rulesSection;

			drawableObject.Rules = rulesSection;
			drawableObject.Art = artSection;

			string imageFileName;
			if (_collectionType == CollectionType.Building || _collectionType == CollectionType.Overlay)
				imageFileName = artSection.ReadString("Image", artSectionName);
			else
				imageFileName = artSectionName;

			bool isVoxel = artSection.ReadBool("Voxel");
			bool theaterExtension = artSection.ReadBool("Theater");
			if (isVoxel) imageFileName += ".vxl";
			else if (theaterExtension) {
				imageFileName += TheaterDefaults.GetExtension(_theaterType);
				if (_collectionType != CollectionType.Overlay || _engineType <= EngineType.FireStorm) {
					drawableObject.PaletteType = PaletteSettings.Iso;
				}
			}
			else imageFileName += TheaterDefaults.GetExtension(_theaterType, _collectionType);

			// See if a theater-specific image is used
			bool NewTheater = artSection.ReadBool("NewTheater");
			if (NewTheater) {
				// http://modenc.renegadeprojects.com/NewTheater

				ApplyNewTheaterIfNeeded(artSectionName, ref imageFileName);

				// Additionaly, this tag means the unit palette is used to draw this image.
				drawableObject.PaletteType = PaletteSettings.Unit;
			}

			if (_engineType <= EngineType.FireStorm && artSection.ReadBool("Remapable")) {
				drawableObject.PaletteType = PaletteSettings.Unit;
			}

			// Used palet can be overriden
			bool noUseTileLandType = rulesSection.ReadString("NoUseTileLandType") != "";
			if (noUseTileLandType) {
				drawableObject.PaletteType = PaletteSettings.Iso;
				drawableObject.LightingType = LightingType.Full;
			}
			else if (artSection.ReadBool("TerrainPalette")) {
				drawableObject.PaletteType = PaletteSettings.Iso;
			}
			else if (artSection.ReadBool("AnimPalette")) {
				drawableObject.PaletteType = PaletteSettings.Anim;
			}
			else if (rulesSection.ReadBool("Wall")) {
				drawableObject.PaletteType = PaletteSettings.Unit;
			}
			else if (artSection.ReadBool("AltPalette")) {
				// If AltPalette=yes is set on an animation then that animation will use the unit palette instead of the animation palette. 
				// However, remappable colours are ignored - they will not be remapped. (TODO: make sure this doesn't happen indeed)
				drawableObject.PaletteType = PaletteSettings.Unit;
			}
			else if (artSection.ReadString("Palette") != string.Empty) {
				drawableObject.PaletteType = PaletteSettings.Custom;
				drawableObject.CustomPaletteName = artSection.ReadString("Palette");
			}

			if (rulesSection.ReadString("AlphaImage") != "") {
				string alphaImageFile = rulesSection.ReadString("AlphaImage") + ".shp";
				if (VFS.Exists(alphaImageFile)) {
					drawableObject.SetAlphaImage(VFS.Open(alphaImageFile) as ShpFile);
				}
			}

			if (drawableObject.PaletteType == PaletteSettings.None)
				// Set palette, determined by type of SHP collection
				drawableObject.PaletteType = TheaterDefaults.GetPaletteType(_collectionType, _engineType);

			bool shadow = TheaterDefaults.GetShadowAssumption(_collectionType);
			if (artSection.ReadString("Shadow") != "")
				shadow = artSection.ReadBool("Shadow");

			if (!rulesSection.ReadBool("DrawFlat", true))
				shadow = true;

			int xOffset = 0, yOffset = 0;

			if (rulesSection.ReadBool("BridgeRepairHut")) {
				// xOffset = yOffset = 0;
			}
			if (_collectionType == CollectionType.Terrain) {
				yOffset = Drawable.TileHeight / 2; // trees and such are placed in the middle of their tile
				drawableObject.LightingType = LightingType.Full;
			}

			if (rulesSection.ReadBool("IsVeins")) {
				yOffset = -36;
				drawableObject.LightingType = LightingType.None;
			}

			if (rulesSection.ReadString("Land") == "Rock") {
				yOffset = Drawable.TileHeight / 2;
			}
			else if (rulesSection.ReadString("Land") == "Road") {
				yOffset = Drawable.TileHeight / 2;
			}
			// Starkku: Railroad track fixes.
			else if (rulesSection.ReadString("Land") == "Railroad") {
				yOffset = 14;
				drawableObject.LightingType = LightingType.Full;
			}
			// Starkku: Use of flag 'Immune' to distinguish ore spawner objects from rest of the things was stupid when SpawnsTiberium exists.
			if (rulesSection.ReadBool("SpawnsTiberium")) {
				// For example on TIBTRE / Ore Poles
				yOffset = -1;
				drawableObject.LightingType = LightingType.None;
				drawableObject.PaletteType = PaletteSettings.Unit;
			}

			drawableObject.Overrides = rulesSection.ReadBool("Overrides");

			// Find out foundation
			// Starkku: Now with custom foundation support (Ares feature).
			string foundation = artSection.ReadString("Foundation", "1x1");
			if (!foundation.Equals("custom", System.StringComparison.InvariantCultureIgnoreCase)) {
				int fx = foundation[0] - '0';
				int fy = foundation[2] - '0';
				drawableObject.Foundation = new Size(fx, fy);
			}
			else {
				int fx = artSection.ReadInt("Foundation.X", 1);
				int fy = artSection.ReadInt("Foundation.Y", 1);
				drawableObject.Foundation = new Size(fx, fy);
			}

			AddImageToObject(drawableObject, imageFileName, xOffset, yOffset, shadow);

			// Buildings often consist of multiple SHP files
			if (_collectionType == CollectionType.Building) {
				drawableObject.AddDamagedShp(VFS.Open(imageFileName) as ShpFile, 0, 0, shadow, 0);

				foreach (string extraImage in ExtraBuildingImages) {
					string extraImageDamaged = extraImage + "Damaged";
					string extraImageSectionName = artSection.ReadString(extraImage);
					string extraImageDamagedSectionName = artSection.ReadString(extraImageDamaged, extraImageSectionName);

					if (extraImageSectionName != "") {
						IniFile.IniSection extraArtSection = _art.GetSection(extraImageSectionName);

						int ySort = 0;
						bool extraShadow = false;
						string extraImageFileName = extraImageSectionName;

						if (extraArtSection != null) {
							ySort = extraArtSection.ReadInt("YSort", artSection.ReadInt(extraImage + "YSort"));
							// Starkku: Art.ini flag 'Shadow' defaults to true for building animations. Changed code below to match that.
							extraShadow = extraArtSection.ReadBool("Shadow", true); // additional building need shadows listed explicitly
							extraImageFileName = extraArtSection.ReadString("Image", extraImageSectionName);
						}
						if (theaterExtension)
							extraImageFileName += TheaterDefaults.GetExtension(_theaterType);
						else
							extraImageFileName += TheaterDefaults.GetExtension(_theaterType, _collectionType);

						if (NewTheater)
							ApplyNewTheaterIfNeeded(artSectionName, ref extraImageFileName);

						AddImageToObject(drawableObject, extraImageFileName, 0, 0, extraShadow, ySort);
					}

					if (extraImageDamagedSectionName != "") {
						IniFile.IniSection extraArtDamagedSection = _art.GetSection(extraImageDamagedSectionName);

						int ySort = 0;
						bool extraShadow = false;
						string extraImageDamagedFileName = extraImageDamagedSectionName;
						if (extraArtDamagedSection != null) {
							ySort = extraArtDamagedSection.ReadInt("YSort", artSection.ReadInt(extraImage + "YSort"));
							// Starkku: Art.ini flag 'Shadow' defaults to true for building animations. Changed code below to match that.
							extraShadow = extraArtDamagedSection.ReadBool("Shadow", true); // additional building need shadows listed explicitly
							extraImageDamagedFileName = extraArtDamagedSection.ReadString("Image", extraImageDamagedSectionName);
						}
						if (theaterExtension)
							extraImageDamagedFileName += TheaterDefaults.GetExtension(_theaterType);
						else
							extraImageDamagedFileName += TheaterDefaults.GetExtension(_theaterType, _collectionType);

						if (NewTheater)
							ApplyNewTheaterIfNeeded(artSectionName, ref extraImageDamagedFileName);

						drawableObject.AddDamagedShp(VFS.Open(extraImageDamagedFileName) as ShpFile, 0, 0, extraShadow, ySort);
					}
				}

				// Starkku: New code for adding fire animations to buildings, supports custom-paletted animations.
				addFireAnimations(artSection, _art, drawableObject);

				// Add turrets
				// Starkku: Added better support for building SHP turrets.
				if (rulesSection.ReadBool("Turret")) {
					string img = rulesSection.ReadString("TurretAnim");
					IniFile.IniSection turretart = _art.GetSection(img);
					img += rulesSection.ReadBool("TurretAnimIsVoxel") ? ".vxl" : ".shp";
					if (turretart != null && turretart.ReadBool("NewTheater") && img.EndsWith(".shp")) {
						ApplyNewTheaterIfNeeded(img, ref img);
					}
					int m_x = rulesSection.ReadInt("TurretAnimX"),
						m_y = rulesSection.ReadInt("TurretAnimY");
					AddImageToObject(drawableObject, img, m_x, m_y, true, 0);

					string barrelFile = img.Replace("TUR", "BARL");
					if (img.EndsWith("TUR.vxl") && VFS.Exists(barrelFile))
						AddImageToObject(drawableObject, barrelFile, m_x, m_y);
				}
			}

			else if (_collectionType == CollectionType.Vehicle) {
				// Add turrets
				if (rulesSection.ReadBool("Turret") && artSection.ReadBool("Voxel")) {
					string turretFile = Path.GetFileNameWithoutExtension(imageFileName) + "TUR.vxl";
					int m_x = rulesSection.ReadInt("TurretAnimX"),
						m_y = rulesSection.ReadInt("TurretAnimY");
					AddImageToObject(drawableObject, turretFile, m_x, m_y);

					string barrelFile = turretFile.Replace("TUR", "BARL");
					if (VFS.Exists(barrelFile))
						AddImageToObject(drawableObject, barrelFile, m_x, m_y);
				}
			}
		}

		// Starkku: Adds fire animations to a building. Supports custom-paletted animations.
		private void addFireAnimations(IniFile.IniSection art, IniFile artfile, Drawable drawableObject) {
			List<string> fireoffsets = new List<string>();
			string[] fireanims = { "FIRE01", "FIRE02", "FIRE03" };
			int count = 0;
			string dfo = null;
			while (true) {
				dfo = art.ReadString("DamageFireOffset" + count.ToString());
				if (dfo == null || dfo == "") break;
				fireoffsets.Add(dfo);
				count++;
			}
			string fanim;
			if (fireanims.Length > 0) {
				for (int i = 0; i < count; i++) {
					dfo = fireoffsets[i];
					int x = int.Parse(dfo.Substring(0, dfo.IndexOf(',')));
					int y = int.Parse(dfo.Substring(dfo.IndexOf(',') + 1));
					try {
						fanim = fireanims[i % 3];
					}
					catch (Exception) {
						fanim = fireanims[0];
					}
					IniFile.IniSection fireart = artfile.GetSection(fanim);
					Palette firepalette;
					firepalette = _palettes.GetCustomPalette(getAnimPalName(fireart), false);
					if (firepalette == null) firepalette = _palettes.AnimPalette;
					
					drawableObject.AddFire(VFS.Open(fanim + ".shp") as ShpFile, x, y, firepalette);
				}
			}
		}

		/* Starkku: Finds out the correct name for an animation palette to use with fire animations.
		 * Reason why this is so complicated is because NPatch & Ares, the YR logic extensions that support custom animation palettes
		 * use different name for the flag declaring the palette. (NPatch uses 'Palette' whilst Ares uses 'CustomPalette' to make it distinct
		 * from the custom object palettes).
		 */
		private string getAnimPalName(IniFile.IniSection animation) {
			String palname = null;
			if (animation != null && (animation.ReadString("Palette") != "" || animation.ReadString("CustomPalette") != "")) {
				palname = animation.ReadString("Palette");
				if (palname == null || palname == "") palname = animation.ReadString("CustomPalette");
				palname = palname.Substring(0, palname.LastIndexOf('.'));
			}
			else if (animation != null && animation.ReadBool("AltPalette") == true) return _palettes.UnitPalette.Name;
			if (palname == null || palname == "") return _palettes.AnimPalette.Name;
			return palname;
		}

		private void ApplyNewTheaterIfNeeded(string artName, ref string imageFileName) {
			if (_engineType <= EngineType.FireStorm) {
				// the tag will only work if the ID for the object starts with either G, N or C and its second letter is A (for Arctic/Snow theater) or T (for Temperate theater)
				if (new[] { 'G', 'N', 'C' }.Contains(artName[0]) && new[] { 'A', 'T' }.Contains(artName[1]))
					ApplyNewTheater(ref imageFileName);
			}
			else if (_engineType == EngineType.RedAlert2) {
				// In RA2, for the tag to work, it must start with either G, N or C, and its second letter must be A, T or U (Urban theater). 
				if (new[] { 'G', 'N', 'C' }.Contains(artName[0]) && new[] { 'A', 'T', 'U' }.Contains(artName[1]))
					ApplyNewTheater(ref imageFileName);
			}
			else {
				//  In Yuri's Revenge, the ID can also start with Y."
				// Starkku: And the theater ID can be N, D or L as well.
				if (new[] { 'G', 'N', 'C', 'Y' }.Contains(artName[0]) && new[] { 'A', 'T', 'U', 'D', 'L', 'N' }.Contains(artName[1]))
					ApplyNewTheater(ref imageFileName);
			}
		}

		private void AddImageToObject(Drawable drawableObject, string fileName, int xOffset = 0, int yOffset = 0, bool hasShadow = false, int ySort = 0) {

			if (fileName.EndsWith(".vxl")) {
				var vxl = VFS.Open<VxlFile>(fileName, FileFormat.Vxl);
				if (vxl != null) {
					string hvaFileName = Path.ChangeExtension(fileName, ".hva");
					var hva = VFS.Open(hvaFileName) as HvaFile;

					if (_collectionType == CollectionType.Building) {
						// half tile to the left
						xOffset += 30;
					}
					else if (_collectionType == CollectionType.Vehicle) {
						// also vertical tile center
						xOffset += 30;
						yOffset += 15;
					}
					drawableObject.AddVoxel(vxl, hva, xOffset, yOffset, hasShadow, ySort);
				}
			}
			else {
				var shp = VFS.Open<ShpFile>(fileName, FileFormat.Shp);
				if (shp != null)
					drawableObject.AddShp(shp, xOffset, yOffset, hasShadow, ySort);
			}
		}

		private void ApplyNewTheater(ref string imageFileName) {
			var sb = new StringBuilder(imageFileName);
			sb[1] = TheaterDefaults.GetTheaterPrefix(_theaterType);
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

		internal void Draw(GameObject o, DrawingSurface drawingSurface) {

			// Starkku: Don't draw objects that have zero hitpoints left.
			if (o is DamageableObject) {
				int str = (o as DamageableObject).Health;
				if (str < 1) return;
			}

			Drawable d = GetDrawable(o);
			// Starkku: Frame to display for infantry depends on their direction.
			if (o is InfantryObject)
				d.SetFrame((o as InfantryObject).Direction / 32);
			if (o is OverlayObject)
				d.SetFrame((o as OverlayObject).OverlayValue);
			d.Draw(o, drawingSurface);
		}

		public bool HasObject(GameObject o) {
			var obj = GetDrawable(o);
			return obj != null && obj.IsValid;
		}

		internal Size GetFoundation(GameObject v) {
			return GetDrawable(v).Foundation;
		}

		internal string GetName(byte p) {
			return _drawables[p].Name;
		}

	}
}