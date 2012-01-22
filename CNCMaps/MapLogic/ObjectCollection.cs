using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using CNCMaps.FileFormats;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.MapLogic {
	class ObjectCollection {
		private CollectionType collectionType;
		private TheaterType theaterType;
		private EngineType engineType;
		IniFile rules, art;
		PaletteCollection palettes;
		private List<Drawable> objects = new List<Drawable>();

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
			this.theaterType = theaterType;
			this.engineType = engineType;
			this.collectionType = collectionType;
			this.rules = rules;
			this.art = art;
			this.palettes = palettes;
			foreach (var entry in objectSection.OrderedEntries) {
				logger.Trace("Loading object {0}.{0}", objectSection.Name, entry.Value);
				LoadObject(entry.Value);
			}
		}

		private void LoadObject(string objName) {
			IniFile.IniSection rulesSection = rules.GetSection(objName);
			var drawableObject = new Drawable(objName);
			objects.Add(drawableObject);

			if (rulesSection == null || rulesSection.ReadBool("IsRubble"))
				return;

			string artSectionName = rulesSection.ReadString("Image", objName);
			IniFile.IniSection artSection = art.GetSection(artSectionName);
			if (artSection == null)
				return;

			string imageFileName;
			if (collectionType == CollectionType.Building || collectionType == CollectionType.Overlay)
				imageFileName = artSection.ReadString("Image", artSectionName);
			else
				imageFileName = artSectionName;

			bool paletteChosen = false;
			bool isVoxel = artSection.ReadBool("Voxel");
			bool theaterExtension = artSection.ReadBool("Theater");
			if (isVoxel) imageFileName += ".vxl";
			else if (theaterExtension) {
				imageFileName += TheaterDefaults.GetExtension(theaterType);
				if (collectionType != CollectionType.Overlay) {
					drawableObject.Palette = palettes.isoPalette;
					paletteChosen = true;
				}
			}
			else imageFileName += TheaterDefaults.GetExtension(theaterType, collectionType);

			// See if a theater-specific image is used
			bool NewTheater = artSection.ReadBool("NewTheater");
			if (NewTheater) {
				ApplyNewTheater(ref imageFileName);
				if (engineType == EngineType.RedAlert2 || engineType == EngineType.YurisRevenge) {
					drawableObject.Palette = (palettes.unitPalette);
					paletteChosen = true;
				}
			}

			// Used palet can be overriden
			bool noUseTileLandType = rulesSection.ReadString("NoUseTileLandType") != "";
			if (noUseTileLandType) {
				drawableObject.Palette = palettes.isoPalette;
				paletteChosen = true;
			}
			else if (rulesSection.ReadBool("TerrainPalette")) {
				drawableObject.Palette = palettes.isoPalette;
				paletteChosen = true;
			}
			else if (rulesSection.ReadBool("AnimPalette")) {
				drawableObject.Palette = palettes.animPalette;
				paletteChosen = true;
			}
			else if (rulesSection.ReadBool("AltPalette")) {
				drawableObject.Palette = palettes.unitPalette;
				paletteChosen = true;
			}
			else if (artSection.ReadString("Palette") == "lib") {
				drawableObject.Palette = palettes.libPalette;
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
				Palette p = palettes.GetPalette(TheaterDefaults.GetPaletteType(collectionType, engineType));
				drawableObject.Palette = p;
			}

			bool shadow = TheaterDefaults.GetShadowAssumption(collectionType);
			if (artSection.ReadString("Shadow") != "")
				shadow = artSection.ReadBool("Shadow");

			if (!rulesSection.ReadBool("DrawFlat", true))
				shadow = true;

			int xOffset = 0, yOffset = 0;

			if (rulesSection.ReadBool("BridgeRepairHut")) {
				// xOffset = yOffset = 0;
			}
			if (collectionType == CollectionType.Terrain) {
				yOffset = Drawable.TileHeight / 2; // trees and such are placed in the middle of their tile
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
				yOffset = -16;
				drawableObject.Palette = palettes.GetPalette(PaletteType.Unit);
			}
			if (rulesSection.ReadBool("Overrides")) {
				drawableObject.SetHeightOffset(4);
				drawableObject.SetOverrides(true);
			}

			// Find out foundation
			string foundation = artSection.ReadString("Foundation", "1x1");
			int fx = foundation[0] - '0';
			int fy = foundation[2] - '0';
			drawableObject.Foundation = new Size(fx, fy);

			AddImageToObject(drawableObject, imageFileName, xOffset, yOffset, shadow);

			// Buildings often consist of multiple SHP files
			if (collectionType == CollectionType.Building) {
				drawableObject.AddDamagedShp(VFS.Open(imageFileName) as ShpFile, 0, 0, shadow, 0);

				foreach (string extraImage in ExtraBuildingImages) {
					string extraImageDamaged = extraImage + "Damaged";
					string extraImageSectionName = artSection.ReadString(extraImage);
					string extraImageDamagedSectionName = artSection.ReadString(extraImageDamaged, extraImageSectionName);

					if (extraImageSectionName != "") {
						IniFile.IniSection extraArtSection = art.GetSection(extraImageSectionName);

						int ySort = 0;
						bool extraShadow = false;
						string extraImageFileName = extraImageSectionName;

						if (extraArtSection != null) {
							ySort = extraArtSection.ReadInt("YSort", artSection.ReadInt(extraImage + "YSort"));
							extraShadow = extraArtSection.ReadBool("Shadow", false); // additional building need shadows listed explicitly
							extraImageFileName = extraArtSection.ReadString("Image", extraImageSectionName);
						}
						if (theaterExtension)
							extraImageFileName += TheaterDefaults.GetExtension(theaterType);
						else
							extraImageFileName += TheaterDefaults.GetExtension(theaterType, collectionType);

						if (NewTheater)
							ApplyNewTheater(ref extraImageFileName);

						AddImageToObject(drawableObject, extraImageFileName, 0, 0, extraShadow, ySort);
					}

					if (extraImageDamagedSectionName != "") {
						IniFile.IniSection extraArtDamagedSection = art.GetSection(extraImageDamagedSectionName);

						int ySort = 0;
						bool extraShadow = false;
						string extraImageDamagedFileName = extraImageDamagedSectionName;
						if (extraArtDamagedSection != null) {
							ySort = extraArtDamagedSection.ReadInt("YSort", artSection.ReadInt(extraImage + "YSort"));
							extraShadow = extraArtDamagedSection.ReadBool("Shadow", false); // additional building need shadows listed explicitly
							extraImageDamagedFileName = extraArtDamagedSection.ReadString("Image", extraImageDamagedSectionName);
						}
						if (theaterExtension)
							extraImageDamagedFileName += TheaterDefaults.GetExtension(theaterType);
						else
							extraImageDamagedFileName += TheaterDefaults.GetExtension(theaterType, collectionType);

						if (NewTheater)
							ApplyNewTheater(ref extraImageDamagedFileName);

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
				}
			}

			else if (collectionType == CollectionType.Vehicle) {
				// Add turrets
				if (rulesSection.ReadBool("Turret")) {
					string turretFile = Path.GetFileNameWithoutExtension(imageFileName) + "tur.vxl";
					AddImageToObject(drawableObject, turretFile, rulesSection.ReadInt("TurretAnimX"));
				}
			}

			if (collectionType == CollectionType.Building || collectionType == CollectionType.Vehicle) {
				// try to add barrel
				string barrelFile = Path.GetFileNameWithoutExtension(imageFileName) + "barl.vxl";
				AddImageToObject(drawableObject, barrelFile,
					rulesSection.ReadInt("TurretAnimX"),
					rulesSection.ReadInt("TurretAnimY"));
			}
		}

		private void AddImageToObject(Drawable drawableObject, string fileName, int xOffset = 0, int yOffset = 0, bool hasShadow = false, int ySort = 0) {
			if (fileName.EndsWith(".vxl")) {
				var vxl = VFS.Open<VxlFile>(fileName, FileFormat.Vxl);
				if (vxl != null) {
					string hvaFileName = Path.ChangeExtension(fileName, ".hva");
					var hva = VFS.Open(hvaFileName) as HvaFile;

					if (collectionType == CollectionType.Building) {
						// half tile to the left
						xOffset += 30;
					}
					else if (collectionType == CollectionType.Vehicle) {
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
			sb[1] = TheaterDefaults.GetTheaterPrefix(theaterType);
			if (!VFS.Exists(sb.ToString())) {
				sb[1] = 'G'; // generic
			}
			imageFileName = sb.ToString();
		}


		private int GetObjectIndex(RA2Object o) {
			int idx = -1;

			if (o is NamedObject)
				idx = FindObjectIndex((o as NamedObject).Name);
			else if (o is NumberedObject)
				idx = (o as NumberedObject).Number;
			return idx;
		}

		internal void Draw(RA2Object o, DrawingSurface drawingSurface) {
			int idx = GetObjectIndex(o);
			if (idx == -1) return;

			Drawable d = objects[idx];
			if (o is OverlayObject)
				d.SetFrame((o as OverlayObject).OverlayValue);

			d.Draw(o, drawingSurface);
		}

		internal Palette GetPalette(RA2Object o) {
			return objects[GetObjectIndex(o)].Palette;
		}

		private int FindObjectIndex(string p) {
			for (int i = 0; i < objects.Count; i++) {
				if (objects[i].Name == p)
					return i;
			}
			return -1;
		}


		internal bool HasObject(RA2Object o) {
			return GetObjectIndex(o) != -1;
		}

		internal Size GetFoundation(StructureObject v) {
			return objects[GetObjectIndex(v)].Foundation;
		}

		internal string GetName(byte p) {
			return objects[p].Name;
		}
	}
}