using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using CNCMaps.FileFormats;
using CNCMaps.VirtualFileSystem;
using CNCMaps.Utility;

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

	class DrawableObject<T> : System.IComparable where T : VirtualFile {
		public DrawProperties props;
		public T file;
		int index;
		public DrawableObject(T file, DrawProperties drawProperties, int index) {
			this.file = file;
			this.props = drawProperties;
			this.index = index;
		}

		public int CompareTo(object obj) {
			DrawableObject<T> other = obj as DrawableObject<T>;
			if (this.props.ySort != other.props.ySort)
				return this.props.ySort - other.props.ySort;
			else
				return this.index - other.index;
		}
	}

	class DrawableObject {
		public DrawableObject(string name) {
			this.Name = name;
			this.Foundation = new Size(1, 1);
		}

		bool sorted = false;
		void Sort() {
			fires.Sort();
			shps.Sort();
			damagedShps.Sort();
			voxels.Sort();
			sorted = true;
		}

		public virtual void Draw(RA2Object obj, DrawingSurface ds) {
			if (!sorted) Sort();

			if (obj is DamageableObject && (obj as DamageableObject).Health < 128) {
				foreach (var v in damagedShps)
					DrawFile(obj, ds, v.file, v.props);

				foreach (var v in fires)
					DrawFile(obj, ds, v.file, v.props, palettes.animPalette);
			}
			else {
				foreach (var v in shps)
					DrawFile(obj, ds, v.file, v.props);
			}
			
			if (alphaImage != null) {
				int dx = obj.Tile.Dx * TileWidth / 2;
				int dy = (obj.Tile.Dy - obj.Tile.Z) * TileHeight / 2;
				dx += globalOffset.X;
				dy += globalOffset.Y;
				alphaImage.DrawAlpha(0, ds, dx, dy);
			}

			for (int i = 0; i < voxels.Count; i++) {
				Palette p = null;
				if (obj is RemappableObject) p = (obj as RemappableObject).Palette;
				if (obj is UnitObject) direction = (obj as UnitObject).Direction;
				DrawingSurface vxl_ds = voxelrenderer.Render(voxels[i].file, hvas[i], -(double)direction / 256.0 * 360 + 45, p ?? this.Palette);

				// rows inverted!
				int dx = obj.Tile.Dx * TileWidth / 2;
				int dy = (obj.Tile.Dy - obj.Tile.Z) * TileHeight / 2;
				dx += globalOffset.X;
				dy += globalOffset.Y;
				var props = this.voxels[i].props;
				dx += props.offset.X;
				dy += props.offset.Y;
				dx -= vxl_ds.bmd.Width / 2;
				dy -= vxl_ds.bmd.Height / 2;

				// vxl_ds.SavePNG("C:\\soms.jpg", 100, 0, 0, 200, 200);

				unsafe {
					byte* w_low = (byte*)ds.bmd.Scan0;
					byte* w_high = w_low + ds.bmd.Stride * ds.bmd.Height;

					for (int y = 0; y < vxl_ds.Height; y++) {
						byte* src_row = (byte*)vxl_ds.bmd.Scan0 + vxl_ds.bmd.Stride * (vxl_ds.Height - y - 1);
						byte* dst_row = ((byte*)ds.bmd.Scan0 + (dy + y) * ds.bmd.Stride + dx * 3);
						if (dst_row < w_low || dst_row >= w_high) continue;
						
						for (int x = 0; x < vxl_ds.Width; x++) {
							// only non-transparent pixels
							if (*(src_row + x * 4 + 3) > 0) {
								*(dst_row + x * 3) = *(src_row + x * 4);
								*(dst_row + x * 3 + 1) = *(src_row + x * 4 + 1);
								*(dst_row + x * 3 + 2) = *(src_row + x * 4 + 2);							
							}
						}

					}
				}

			}

		}

		static VoxelRenderer voxelrenderer = new VoxelRenderer();

		private void DrawFile(RA2Object obj, DrawingSurface ds, ShpFile file, DrawProperties props, Palette p = null) {
			int dx = obj.Tile.Dx * TileWidth / 2;
			int dy = (obj.Tile.Dy - obj.Tile.Z) * TileHeight / 2;
			dx += globalOffset.X;
			dy += globalOffset.Y;
			dx += props.offset.X;
			dy += props.offset.Y;
			if (p == null && obj is RemappableObject)
				p = (obj as RemappableObject).Palette;

			if (objectOverrides && obj is OverlayObject) {
				OverlayObject o = obj as OverlayObject;
				if (TileWidth == 60) {
					if (o.OverlayID == 24 || o.OverlayID == 25 || o.OverlayID == 238 || o.OverlayID == 237)
						dy += o.OverlayValue > 8 ? 16 : 1;
				}
				else {
					dx += o.OverlayValue > 8 ? -7 : -6;
					dy += o.OverlayValue > 8 ? -13 : -1;
				}
			}

			file.Draw(frame, ds, dx, dy, 0, p ?? this.Palette);
			if (props.hasShadow)
				file.DrawShadow(frame, ds, dx, dy);	
		}

		public static PaletteCollection palettes { get; set; }
		public Palette Palette { get; set; }
		ShpFile alphaImage = null;
		Point globalOffset = new Point(0, 0);

		int heightOffset;
		bool objectOverrides = false;
		public Size Foundation { get; set; }
		int direction; // for voxels
		int frame; // for shps
		public string Name { get; private set; }

		List<DrawableObject<VxlFile>> voxels = new List<DrawableObject<VxlFile>>();
		List<HvaFile> hvas = new List<HvaFile>();

		List<DrawableObject<ShpFile>> shps = new List<DrawableObject<ShpFile>>();
		List<DrawableObject<ShpFile>> fires = new List<DrawableObject<ShpFile>>();
		List<DrawableObject<ShpFile>> damagedShps = new List<DrawableObject<ShpFile>>();

		internal void SetAlphaImage(ShpFile shpFile) {
			alphaImage = shpFile;
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
			Foundation = new Size(w, h);
		}

		internal void AddOffset(int extraXOffset, int extraYOffset) {
			globalOffset.X += extraXOffset;
			globalOffset.Y += extraYOffset;
		}

		internal void AddVoxel(VxlFile vxlFile, HvaFile hvaFile, int xOffset = 0, int yOffset = 0, bool hasShadow = false, int ySort = 0) {
			voxels.Add(new DrawableObject<VxlFile>(vxlFile, new DrawProperties(new Point(xOffset, yOffset), hasShadow, ySort), voxels.Count));
			hvas.Add(hvaFile);
		}

		internal void AddShp(ShpFile shpFile, int xOffset = 0, int yOffset = 0, bool hasShadow = false, int ySort = 0) {
			shps.Add(new DrawableObject<ShpFile>(shpFile, new DrawProperties(new Point(xOffset, yOffset), hasShadow, ySort), shps.Count));
		}

		internal void AddDamagedShp(ShpFile shpFile, int xOffset = 0, int yOffset = 0, bool hasShadow = false, int ySort = 0) {
			damagedShps.Add(new DrawableObject<ShpFile>(shpFile, new DrawProperties(new Point(xOffset, yOffset), hasShadow, ySort), damagedShps.Count));
		}

		internal void AddFire(ShpFile shpFile, int xOffset, int yOffset) {
			fires.Add(new DrawableObject<ShpFile>(shpFile, new DrawProperties(new Point(xOffset, yOffset), false, 0), fires.Count));
		}

		internal void SetFrame(int frameNum) {
			this.frame = frameNum;
		}

		public static ushort TileWidth { get; set; }
		public static ushort TileHeight { get; set; }
	}

	class ObjectCollection {
		private CollectionType collectionType;
		private TheaterType theaterType;
		private EngineType engineType;
		IniFile rules, art;
		PaletteCollection palettes;

		private List<DrawableObject> objects = new List<DrawableObject>();

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

		public ObjectCollection(IniFile.IniSection objectSection, CollectionType collectionType,
			TheaterType theaterType, EngineType engineType, IniFile rules, IniFile art, PaletteCollection palettes) {
			this.theaterType = theaterType;
			this.engineType = engineType;
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
			var drawableObject = new DrawableObject(objName);
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
			else if (rulesSection.ReadString("Palette") == "lib") {
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
				Palette p = palettes.GetPalette(TheaterDefaults.GetPaletteType(collectionType, this.engineType));
				drawableObject.Palette = p;
			}

			bool shadow = TheaterDefaults.GetShadowAssumption(collectionType);
			if (artSection.ReadString("Shadow") != "")
				shadow = artSection.ReadBool("Shadow");

			if (!rulesSection.ReadBool("DrawFlat", true))
				shadow = true;

			int xOffset = 0, yOffset = 0;
			if (rulesSection.ReadBool("Immune")) {
				// For example on TIBTRE / Ore Poles
				yOffset = -1;
				drawableObject.Palette = palettes.GetPalette(PaletteType.Unit);
			}
			if (rulesSection.ReadBool("BridgeRepairHut")) {
				// xOffset = yOffset = 0;
			}

			if (rulesSection.ReadString("Land") == "Rock") {
				yOffset = 15;
			}

			else if (rulesSection.ReadString("Land") == "Road") {
				yOffset = 15;
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

		private void AddImageToObject(DrawableObject drawableObject, string fileName, int xOffset = 0, int yOffset = 0, bool hasShadow = false, int ySort = 0) {
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
			StringBuilder sb = new StringBuilder(imageFileName);
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

			DrawableObject d = objects[idx];
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