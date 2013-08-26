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
			"ProductionAnim",
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

			string imageFileName;
			if (_collectionType == CollectionType.Building || _collectionType == CollectionType.Overlay)
				imageFileName = artSection.ReadString("Image", artSectionName);
			else
				imageFileName = artSectionName;

			bool paletteChosen = false;
			bool isVoxel = artSection.ReadBool("Voxel");
			bool theaterExtension = artSection.ReadBool("Theater");
			if (isVoxel) imageFileName += ".vxl";
			else if (theaterExtension) {
				imageFileName += TheaterDefaults.GetExtension(_theaterType);
				if (_collectionType != CollectionType.Overlay || _engineType <= EngineType.FireStorm) {
					drawableObject.Palette = _palettes.isoPalette;
					paletteChosen = true;
				}
			}
			else imageFileName += TheaterDefaults.GetExtension(_theaterType, _collectionType);

			// See if a theater-specific image is used
			bool NewTheater = artSection.ReadBool("NewTheater");
			if (NewTheater) {
				// http://modenc.renegadeprojects.com/NewTheater

				ApplyNewTheaterIfNeeded(artSectionName, ref imageFileName);

				// Additionaly, this tag means the unit palette is used to draw this image.
				drawableObject.Palette = (_palettes.unitPalette);
				paletteChosen = true;
			}

			if (_engineType <= EngineType.FireStorm && artSection.ReadBool("Remapable")) {
				drawableObject.Palette = (_palettes.unitPalette);
				paletteChosen = true;
			}

			// Used palet can be overriden
			bool noUseTileLandType = rulesSection.ReadString("NoUseTileLandType") != "";
			if (noUseTileLandType) {
				drawableObject.Palette = _palettes.isoPalette;
				drawableObject.UseTilePalette = true;
				paletteChosen = true;
			}
			else if (artSection.ReadBool("TerrainPalette")) {
				drawableObject.Palette = _palettes.isoPalette;
				paletteChosen = true;
			}
			else if (artSection.ReadBool("AnimPalette")) {
				drawableObject.Palette = _palettes.animPalette;
				paletteChosen = true;
			}
			else if (artSection.ReadBool("AltPalette")) {
				// If AltPalette=yes is set on an animation then that animation will use the unit palette instead of the animation palette. 
				// However, remappable colours are ignored - they will not be remapped. (TODO: make sure this doesn't happen indeed)
				drawableObject.Palette = _palettes.unitPalette;
				paletteChosen = true;
			}
			else if (artSection.ReadString("Palette") == "lib") {
				drawableObject.Palette = _palettes.libPalette;
				paletteChosen = true;
			}

			if (rulesSection.ReadString("AlphaImage") != "") {
				string alphaImageFile = rulesSection.ReadString("AlphaImage") + ".shp";
				if (VFS.Exists(alphaImageFile)) {
					drawableObject.SetAlphaImage(VFS.Open(alphaImageFile) as ShpFile);
				}
			}

			if (!paletteChosen) {
				// Set palette, determined by type of SHP collection
				Palette p = _palettes.GetPalette(TheaterDefaults.GetPaletteType(_collectionType, _engineType));
				drawableObject.Palette = p;
			}

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
				drawableObject.UseTilePalette = true;
			}
			if (rulesSection.ReadString("Land") == "Rock") {
				yOffset = 15;
				drawableObject.UseTilePalette = true;
			}
			else if (rulesSection.ReadString("Land") == "Road") {
				yOffset = 15;
			}
			if (rulesSection.ReadBool("Immune")) {
				// For example on TIBTRE / Ore Poles
				yOffset = -1;
				drawableObject.Palette = _palettes.GetPalette(PaletteType.Unit);
				drawableObject.UseTilePalette = false;
			}
			if (rulesSection.ReadBool("Overrides")) {
				drawableObject.Overrides = true;
			}

			// Find out foundation
			string foundation = artSection.ReadString("Foundation", "1x1");
			int fx = foundation[0] - '0';
			int fy = foundation[2] - '0';
			drawableObject.Foundation = new Size(fx, fy);

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
							extraShadow = extraArtSection.ReadBool("Shadow", false); // additional building need shadows listed explicitly
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
							extraShadow = extraArtDamagedSection.ReadBool("Shadow", false);
							// additional building need shadows listed explicitly
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

				// Add fires
				string df0 = artSection.ReadString("DamageFireOffset0");
				if (df0 != "") {
					int x = int.Parse(df0.Substring(0, df0.IndexOf(',')));
					int y = int.Parse(df0.Substring(df0.IndexOf(',') + 1));
					drawableObject.AddFire(VFS.Open("fire01.shp") as ShpFile, x, y);
				}
				string df1 = artSection.ReadString("DamageFireOffset1");
				if (df1 != "") {
					int x = int.Parse(df1.Substring(0, df1.IndexOf(',')));
					int y = int.Parse(df1.Substring(df1.IndexOf(',') + 1));
					drawableObject.AddFire(VFS.Open("fire02.shp") as ShpFile, x, y);
				}
				string df2 = artSection.ReadString("DamageFireOffset2");
				if (df2 != "") {
					int x = int.Parse(df2.Substring(0, df2.IndexOf(',')));
					int y = int.Parse(df2.Substring(df2.IndexOf(',') + 1));
					drawableObject.AddFire(VFS.Open("fire03.shp") as ShpFile, x, y);
				}

				// Add turrets
				if (rulesSection.ReadBool("Turret")) {
					string img = rulesSection.ReadString("TurretAnim");
					img += rulesSection.ReadBool("TurretAnimIsVoxel") ? ".vxl" : ".shp";
					int m_x = rulesSection.ReadInt("TurretAnimX"),
						m_y = rulesSection.ReadInt("TurretAnimY");
					AddImageToObject(drawableObject, img, m_x, m_y);

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

		private void ApplyNewTheaterIfNeeded(string artName, ref string imageFileName) {
			if (_engineType <= EngineType.FireStorm) {
				// the tag will only work if the ID for the object starts with either G, N or C and its second letter is A (for Arctic/Snow theater) or T (for Temperate theater)
				if (new[] {'G', 'N', 'C'}.Contains(artName[0]) && new[] {'A', 'T'}.Contains(artName[1]))
					ApplyNewTheater(ref imageFileName);
			}
			else if (_engineType == EngineType.RedAlert2) {
				// In RA2, for the tag to work, it must start with either G, N or C, and its second letter must be A, T or U (Urban theater). 
				if (new[] {'G', 'N', 'C'}.Contains(artName[0]) && new[] {'A', 'T', 'U'}.Contains(artName[1]))
					ApplyNewTheater(ref imageFileName);
			}
			else {
				//  In Yuri's Revenge, the ID can also start with Y."
				if (new[] {'G', 'N', 'C', 'Y'}.Contains(artName[0]) && new[] {'A', 'T', 'U'}.Contains(artName[1]))
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

		public Drawable GetDrawable(RA2Object o) {
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

		internal void Draw(RA2Object o, DrawingSurface drawingSurface) {
			Drawable d = GetDrawable(o);
			if (o is OverlayObject)
				d.SetFrame((o as OverlayObject).OverlayValue);

			d.Draw(o, drawingSurface);
		}

		public bool HasObject(RA2Object o) {
			var obj = GetDrawable(o);
			return obj != null && obj.IsValid;
		}

		internal Size GetFoundation(RA2Object v) {
			return GetDrawable(v).Foundation;
		}

		internal string GetName(byte p) {
			return _drawables[p].Name;
		}

	}
}