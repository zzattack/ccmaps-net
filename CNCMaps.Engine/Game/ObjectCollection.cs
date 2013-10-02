using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;
using NLog;

namespace CNCMaps.Engine.Game {

	public class ObjectCollection : GameCollection {
		public readonly string[] FireNames;
		public readonly PaletteCollection Palettes;

		static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public ObjectCollection(CollectionType type, TheaterType theater, EngineType engine, IniFile rules, IniFile art,
			IniFile.IniSection objectsList, PaletteCollection palettes)
			: base(type, theater, engine, rules, art) {

			Palettes = palettes;
			if (engine >= EngineType.RedAlert2) {
				string fireNames = Rules.ReadString(Engine == EngineType.RedAlert2 ? "AudioVisual" : "General",
					"DamageFireTypes", "FIRE01,FIRE02,FIRE03");
				FireNames = fireNames.Split(new[] { ',', '.' }, StringSplitOptions.RemoveEmptyEntries);
			}

			foreach (var entry in objectsList.OrderedEntries) {
				if (!string.IsNullOrEmpty(entry.Value)) {
					Logger.Trace("Loading object {0}.{1}", objectsList.Name, entry.Value);
					LoadObject(entry.Value);
				}
			}
		}

		private void LoadObject(string objName) {
			Drawable drawable;

			var rulesSection = Rules.GetOrCreateSection(objName);
			string artSectionName = rulesSection.ReadString("Image", objName);
			var artSection = Art.GetOrCreateSection(artSectionName);

			switch (Type) {
				case CollectionType.Aircraft:
				case CollectionType.Vehicle:
					drawable = InitUnitDrawable(rulesSection, artSection);
					break;
				case CollectionType.Building:
					drawable = InitBuildingDrawable(rulesSection, artSection);
					break;
				case CollectionType.Infantry:
				case CollectionType.Overlay:
				case CollectionType.Smudge:
				case CollectionType.Terrain:
					drawable = InitSimpleDrawable(rulesSection, artSection);
					break;
				case CollectionType.Animation:
					drawable = InitAnimDrawable(rulesSection, artSection);
					break;
				default:
					throw new InvalidEnumArgumentException();
			}

			// overrides from the modconfig
			var cfgOverrides = ModConfig.ActiveConfig.ObjectOverrides.Where(ovr =>
				// matches collection
				(ovr.CollectionTypes & Type) == Type &&
				// matches collection
				(ovr.TheaterTypes & Theater) == Theater &&
				// matches object regex
				Regex.IsMatch(objName, ovr.ObjRegex, RegexOptions.IgnoreCase))
				.OrderByDescending(o => o.Priority);

			foreach (var cfgOverride in cfgOverrides) {
				Logger.Debug("Object {0} receives overrides from regex {1}", objName, cfgOverride.ObjRegex);

				if (cfgOverride.Lighting != LightingType.Default)
					drawable.Props.LightingType = cfgOverride.Lighting;

				if (cfgOverride.Palette != PaletteType.Default) {
					drawable.Props.PaletteType = cfgOverride.Palette;
					drawable.Props.CustomPaletteName = cfgOverride.CustomPaletteFile;
				}
				if (!string.IsNullOrWhiteSpace(cfgOverride.FrameDeciderCode) && !cannotCompile) {
					try {
						var fdc = FrameDeciders.GetOverrideFrameDecider(cfgOverride);
						if (fdc != null)
							drawable.Props.FrameDecider = fdc;
					}
					catch (TypeLoadException exc) {
						cannotCompile = true;
						Logger.Error("Custom framedecider could not be compiled. You need .NET 4.5 for this."
							+ " Functionality will be unavaiable.\r\n{0}", exc);
					}
				}
			}
		}

		private static bool cannotCompile = false;

		private Drawable InitAnimDrawable(IniFile.IniSection rules, IniFile.IniSection art) {
			var anim = new AnimDrawable(rules, art);
			InitDrawableDefaults(anim, art);
			anim.LoadFromRules();
			anim.Shp = VFS.Open<ShpFile>(anim.Image + ".shp");
			return anim;
		}

		private void InitDrawableDefaults(Drawable drawable, IniFile.IniSection artSection) {
			drawable.OwnerCollection = this;
			_drawables.Add(drawable);
			_drawablesDict[drawable.Name] = drawable;

			drawable.Props.PaletteType = Defaults.GetDefaultPalette(Type, Engine);
			drawable.Props.LightingType = Defaults.GetDefaultLighting(Type);
			drawable.IsRemapable = Defaults.GetDefaultRemappability(Type, Engine);
			drawable.Props.FrameDecider = Defaults.GetDefaultFrameDecider(Type);

			// apply collection-specific offsets
			switch (Type) {
				case CollectionType.Building:
				case CollectionType.Overlay:
				case CollectionType.Smudge:
					drawable.Props.Offset.Offset(Drawable.TileWidth / 2, 0);
					break;
				case CollectionType.Terrain:
				case CollectionType.Vehicle:
				case CollectionType.Infantry:
				case CollectionType.Aircraft:
				case CollectionType.Animation:
					drawable.Props.Offset.Offset(Drawable.TileWidth / 2, Drawable.TileHeight / 2);
					break;
			}
		}

		private Drawable InitSimpleDrawable(IniFile.IniSection rulesSection, IniFile.IniSection artSection) {
			var drawable = new ShpDrawable(rulesSection, artSection);
			InitDrawableDefaults(drawable, artSection);
			drawable.LoadFromRules();

			string shpFile = drawable.GetFilename();
			drawable.Shp = VFS.Open<ShpFile>(shpFile);

			if (Type == CollectionType.Smudge)
				drawable.Foundation = new Size(rulesSection.ReadInt("Width", 1), rulesSection.ReadInt("Height", 1));

			if (Type == CollectionType.Overlay)
				LoadOverlayDrawable(drawable);

			return drawable;
		}

		private Drawable InitBuildingDrawable(IniFile.IniSection rulesSection, IniFile.IniSection artSection) {
			var drawable = new BuildingDrawable(rulesSection, artSection);
			InitDrawableDefaults(drawable, artSection);
			drawable.LoadFromRules();
			return drawable;
		}

		private Drawable InitUnitDrawable(IniFile.IniSection rulesSection, IniFile.IniSection artSection) {
			var drawable = new UnitDrawable(rulesSection, artSection);
			InitDrawableDefaults(drawable, artSection);

			drawable.LoadFromRules();
			return drawable;
		}

		private void LoadOverlayDrawable(Drawable drawable) {
			int objIdx = _drawables.Count - 1;
			var ovl = new OverlayObject((byte)objIdx, 0);
			var tibType = SpecialOverlays.GetOverlayTibType(ovl, Engine);
			DrawProperties props = drawable.Props;

			if (Engine >= EngineType.RedAlert2) {
				if (tibType != OverlayTibType.NotSpecial) {
					props.FrameDecider = FrameDeciders.OverlayValueFrameDecider;
					props.PaletteType = PaletteType.Overlay;
					props.LightingType = LightingType.None;
				}
				else if (SpecialOverlays.IsHighBridge(ovl)) {
					props.OffsetHack = OffsetHacks.RA2BridgeOffsets;
					props.ShadowOffsetHack = OffsetHacks.RA2BridgeShadowOffsets;
					drawable.TileElevation = 4; // for lighting
					drawable.Foundation = new Size(3, 1); // ensures they're drawn later --> fixes overlap
				}
			}
			else if (Engine <= EngineType.Firestorm) {
				if (tibType != OverlayTibType.NotSpecial) {
					props.FrameDecider = FrameDeciders.OverlayValueFrameDecider;
					props.PaletteType = PaletteType.Unit;
					props.LightingType = LightingType.None;
					drawable.IsRemapable = true;
				}
				else if (SpecialOverlays.IsHighBridge(ovl) || SpecialOverlays.IsTSHighRailsBridge(ovl)) {
					props.OffsetHack = OffsetHacks.TSBridgeOffsets;
					props.ShadowOffsetHack = OffsetHacks.TSBridgeShadowOffsets;
					drawable.TileElevation = 4; // for lighting
					//drawable.Foundation = new Size(3, 1); // ensures they're drawn later --> fixes overlap
				}
			}
		}

		public string ApplyNewTheaterIfNeeded(string artName, string imageFileName) {
			if (Engine <= EngineType.Firestorm) {
				// the tag will only work if the ID for the object starts with either G, N or C and its second letter is A (for Arctic/Snow theater) or T (for Temperate theater)
				if (new[] { 'G', 'N', 'C' }.Contains(artName[0]) && new[] { 'A', 'T' }.Contains(artName[1]))
					ApplyNewTheater(ref imageFileName);
			}
			else if (Engine == EngineType.RedAlert2) {
				// In RA2, for the tag to work, it must start with either G, N or C, and its second letter must be A, T or U (Urban theater). 
				if (new[] { 'G', 'N', 'C' }.Contains(artName[0]) && new[] { 'A', 'T', 'U' }.Contains(artName[1]))
					ApplyNewTheater(ref imageFileName);
			}
			else {
				//  In Yuri's Revenge, the ID can also start with Y."
				if (new[] { 'G', 'N', 'C', 'Y' }.Contains(artName[0]) && new[] { 'A', 'T', 'U' }.Contains(artName[1]))
					ApplyNewTheater(ref imageFileName);
			}
			return imageFileName;
		}

		private void ApplyNewTheater(ref string imageFileName) {
			var sb = new StringBuilder(imageFileName);

			sb[1] = ModConfig.ActiveTheater.NewTheaterChar;
			if (!VFS.Exists(sb.ToString())) {
				sb[1] = 'G'; // generic
				if (!VFS.Exists(sb.ToString()))
					sb[1] = imageFileName[1]; // fallback to original
			}
			imageFileName = sb.ToString();
		}

	}
}