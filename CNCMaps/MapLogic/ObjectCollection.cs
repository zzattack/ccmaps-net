using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CNCMaps.FileFormats;
using System.Drawing.Imaging;
using CNCMaps.VirtualFileSystem;
using System.IO;
using System.Drawing;

namespace CNCMaps.MapLogic {

	public enum CollectionType {
		Aircraft,
		Building,
		Infantry,
		Overlay,
		Smudge,
		Terrain,
		Vehicle,
	}

	class DrawingSurface {
		BitmapData bmd;
	}

	public class DrawProperties {
		public Point offset;
		public bool hasShadow;
		public int ySort;
		public DrawProperties(Point offset, bool hasShadow, int ySort) {
			this.offset = offset;
			this.hasShadow = hasShadow;
			this.ySort = ySort;
		}
	}

	class DrawableObject {
		public virtual void Draw(RA2Object obj, int x, int y, DrawingSurface s) { }
		Palette palette;
		ShpFile alphaImage = null;
		Point globalOffset = new Point(0, 0);
		Point shadowOffset = new Point(0, 0);

		int heightOffset;
		bool objectOverrides = false;
		Size foundation = new Size(1, 1);
		int direction; // for voxels
		int frame; // for shps
		bool hasShadow;

		List<VxlFile> voxels = new List<VxlFile>();
		List<DrawProperties> voxelProperties = new List<DrawProperties>();

		List<HvaFile> hvas = new List<HvaFile>();
		List<ShpFile> shpFires = new List<ShpFile>();
		List<DrawProperties> shpFireProperties = new List<DrawProperties>();

		List<ShpFile> shpSections = new List<ShpFile>();
		List<DrawProperties> shpProperties = new List<DrawProperties>();

		List<ShpFile> shpDamagedSections = new List<ShpFile>();
		List<DrawProperties> shpDamagedProperties = new List<DrawProperties>();

		public void SetPalette(Palette p) {
			this.palette = p;
		}

		internal void SetAlphaImage(ShpFile shpFile) {
			alphaImage = shpFile;
		}

		internal void SetOffsetShadow(int xOffset, int yOffset) {
			this.shadowOffset.X = xOffset;
			this.shadowOffset.Y = yOffset;
		}

		internal void SetOffset(int xOffset, int yOffset) {
			this.globalOffset.X = xOffset;
			this.globalOffset.Y = yOffset;
		}

		internal void SetHeightOffset(int heightOffset) {
			this.heightOffset = heightOffset;
		}

		internal void SetOverrides(bool overrides) {
			objectOverrides = overrides;
		}

		internal void SetFoundation(int w, int h) {
			this.foundation.Width = w;
			this.foundation.Height = h;
		}

		internal void SetShadow(bool hasShadow) {
			this.hasShadow = hasShadow;
		}

		internal void AddOffset(int extraXOffset, int extraYOffset) {
			globalOffset.X += extraXOffset;
			globalOffset.Y += extraYOffset;
		}

		internal void AddVoxel(VxlFile vxlFile, HvaFile hvaFile, int xOffset = 0, int yOffset = 0, bool hasShadow = false, int ySort = 0) {
			voxels.Add(vxlFile);
			hvas.Add(hvaFile);
			voxelProperties.Add(new DrawProperties(new Point(xOffset, yOffset), hasShadow, ySort));
		}

		internal void AddShp(ShpFile shpFile, int xOffset = 0, int yOffset = 0, bool hasShadow =false, int ySort = 0) {
			shpSections.Add(shpFile);
			shpProperties.Add(new DrawProperties(new Point(xOffset, yOffset), hasShadow, ySort));
		}

		internal void AddDamagedShp(ShpFile shpFile, int xOffset = 0, int yOffset = 0, bool hasShadow = false, int ySort = 0) {
			shpDamagedSections.Add(shpFile);
			shpDamagedProperties.Add(new DrawProperties(new Point(xOffset, yOffset), hasShadow, ySort));
		}

		internal void AddFire(ShpFile shpFile, int xOffset, int yOffset) {
			shpFires.Add(shpFile);
			shpFireProperties.Add(new DrawProperties(new Point(xOffset, yOffset), false, 0));
		}
	}


	class ObjectCollection {
		private CollectionType collectionType;
		private TheaterType theaterType;
		IniFile rules, art;
		PaletteCollection palettes;

		private List<DrawableObject> objects = new List<DrawableObject>();

		static readonly string[] ExtraBuildingImages = {
			"ActiveAnim",	"ActiveAnimTwo",	"ActiveAnimThree",	"ActiveAnimFour",
			"SpecialAnim",	"SpecialAnimTwo",	"SpecialAnimThree",	"SpecialAnimFour",
			"BibShape", "Turret", "SuperAnim"
		};

		public ObjectCollection(IniFile.IniSection objectSection, CollectionType collectionType,
			TheaterType theaterType, IniFile rules, IniFile art, PaletteCollection palettes) {
			this.theaterType = theaterType;
			this.collectionType = collectionType;
			this.rules = rules;
			this.art = art;
			this.palettes = palettes;
			foreach (var entry in objectSection.OrderedEntries) {
				LoadObject(entry.Value);
			}
		}

		private void LoadObject(string objName) {
			IniFile.IniSection rulesSection = rules.GetSection(objName);
			var drawableObject = new DrawableObject();
				this.objects.Add(drawableObject);

			if (rulesSection == null || rulesSection.ReadBool("IsRubble"))
				return;

			string artSectionName = rulesSection.ReadString("Image", objName);
			IniFile.IniSection artSection = art.GetSection(artSectionName);
			if (artSection == null)
				return;

			string imageFileName;
			if (this.collectionType == CollectionType.Building || this.collectionType == CollectionType.Overlay)
				imageFileName = artSection.ReadString("Image", artSectionName);
			else
				imageFileName = artSectionName;

			bool paletteChosen = false;
			bool isVoxel = artSection.ReadBool("IsVoxel");
			bool theaterExtension = artSection.ReadBool("Theater");
			if (isVoxel) imageFileName += ".vxl";
			else if (theaterExtension) {
				imageFileName += TheaterDefaults.GetExtension(theaterType);
				if (collectionType != CollectionType.Overlay) {
					drawableObject.SetPalette(palettes.isoPalette);
					paletteChosen = true;
				}
			}
			else imageFileName += TheaterDefaults.GetExtension(theaterType, collectionType);

			
			// See if a theater-specific image is used
			bool NewTheater = artSection.ReadBool("NewTheater");
			if (NewTheater) {
				ApplyNewTheater(ref imageFileName);
				drawableObject.SetPalette(palettes.unitPalette);
				paletteChosen = true;
			}

			// Used palet can be overriden
			bool noUseTileLandType = rulesSection.ReadString("NoUseTileLandType") != "";
			if (noUseTileLandType) {
				drawableObject.SetPalette(palettes.isoPalette);
				paletteChosen = true;
			}
			else if (rulesSection.ReadBool("TerrainPalette")) {
				drawableObject.SetPalette(palettes.isoPalette);
				paletteChosen = true;
			}
			else if (rulesSection.ReadBool("AnimPalette")) {
				drawableObject.SetPalette(palettes.animPalette);
				paletteChosen = true;
			}
			else if (rulesSection.ReadBool("AltPalette")) {
				drawableObject.SetPalette(palettes.unitPalette);
				paletteChosen = true;
			}
			else if (rulesSection.ReadString("Palette") == "lib") {
				drawableObject.SetPalette(palettes.libPalette);
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
				Palette p = palettes.GetPalette(TheaterDefaults.GetPaletteType(collectionType));
				drawableObject.SetPalette(p);
			}
			
			bool shadow = TheaterDefaults.GetShadowAssumption(collectionType);
			if (rulesSection.ReadString("Shadow") != "")
				drawableObject.SetShadow(rulesSection.ReadBool("Shadow"));

			if (!rulesSection.ReadBool("DrawFlat", true))
				drawableObject.SetShadow(true);

			if (rulesSection.ReadBool("Immune")) {
				// For example on TIBTRE / Ore Poles
				drawableObject.SetOffset(0, -15);
				drawableObject.SetOffsetShadow(0, -15);
				drawableObject.SetPalette(palettes.GetPalette(PaletteType.Unit));
			}

			if (rulesSection.ReadBool("BridgeRepairHut")) {
				drawableObject.SetOffset(0, 0);
				drawableObject.SetOffsetShadow(0, 0);
			}

			if (rulesSection.ReadString("Land") == "Rock") {
				drawableObject.SetOffset(0, 15);
				drawableObject.SetOffsetShadow(0, 15);
			}

			else if (rulesSection.ReadString("Land") == "Road") {
				drawableObject.SetOffset(0, 15);
				drawableObject.SetOffsetShadow(0, 15);
			}

			if (rulesSection.ReadBool("Overrides")) {
				drawableObject.SetHeightOffset(4);
				drawableObject.SetOverrides(true);
			}

			// Find out foundation
			string foundation = rulesSection.ReadString("Foundation", "1x1");
			int fx = foundation[0] - '0';
			int fy = foundation[2] - '0';
			drawableObject.SetFoundation(fx, fy);

			AddImageToObject(drawableObject, imageFileName);

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

		private void AddImageToObject(DrawableObject drawableObject, string fileName, int xOffset = 0, int yOffset = 0, bool hasShadow = false, int ySort = 0) {
			if (fileName.EndsWith(".vxl")) {
				var vxl = VFS.Open(fileName) as VxlFile;
				if (vxl != null) {
					string hvaFileName = Path.ChangeExtension(fileName, ".hva");
					var hva = VFS.Open(hvaFileName) as HvaFile;
					drawableObject.AddVoxel(vxl, hva, xOffset, yOffset, hasShadow, ySort);

					if (collectionType == CollectionType.Building) // half tile to the left, center of 200x200px vxl render
						drawableObject.AddOffset(-70, -100);
					else if (collectionType == CollectionType.Vehicle) // also vertical tile center, plus center of 200x200px vxl render
						drawableObject.AddOffset(-70, -85);
				}
			}
			else {
				var shp = VFS.Open(fileName) as ShpFile;
				if (shp != null)
					drawableObject.AddShp(shp, xOffset, yOffset, hasShadow, ySort);
			}
		}

		private void ApplyNewTheater(ref string imageFileName) {
			StringBuilder sb = new StringBuilder(imageFileName);
			sb[1] = TheaterDefaults.GetTheaterPrefix(theaterType);
			if (VFS.Exists(imageFileName)) {
				sb[1] = 'g';
			}
			imageFileName = sb.ToString();
		}

	}
}
