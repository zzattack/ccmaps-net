using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using CNCMaps.Shared.DynamicTypeDescription;

namespace CNCMaps.Shared {

	[Serializable]
	public class ModConfig : INotifyPropertyChanged {

		[NonSerialized]
		private DynamicCustomTypeDescriptor _dctd = null;

		public static ModConfig ActiveConfig { get; set; }
		public static TheaterSettings ActiveTheater { get; private set; }

		public static void LoadDefaultConfig(EngineType engine) {
			if (engine == EngineType.TiberianSun)
				ActiveConfig = DefaultsTS;
			else if (engine == EngineType.Firestorm)
				ActiveConfig = DefaultsFS;
			else if (engine == EngineType.RedAlert2)
				ActiveConfig = DefaultsRA2;
			else if (engine == EngineType.YurisRevenge)
				ActiveConfig = DefaultsYR;
		}

		public static bool SetActiveTheater(TheaterType theater) {
			ActiveTheater = ActiveConfig.Theaters.First(t => t.Type == theater);
			return ActiveTheater != null;
		}

		public static ModConfig Deserialize(Stream s) {
			var xs = new XmlSerializer(typeof(ModConfig));
			var ret = (ModConfig)xs.Deserialize(s);
			return ret;
		}

		public void Serialize(Stream s) {
			var xs = new XmlSerializer(typeof(ModConfig));
			xs.Serialize(s, this);
		}

		[Id(1, 1)]
		[Description("An identifying name for this configuration file")]
		public string Name { get; set; }

		[Id(2, 1)]
		[Description("Specify the engine type used for your mod.")]
		public EngineType Engine { get; set; }

		[Id(3, 1)]
		[Description("Directories in which your mod stores assets, mixes, or configuration files.\r\nCan be entered as a comma-separated list.")]
		[PropertyStateFlags((PropertyFlags.Default | PropertyFlags.ExpandIEnumerable) & ~PropertyFlags.SupportStandardValues)]
		[Editor(@"System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
		[TypeConverter(typeof(CsvConverter))]
		public List<string> Directories { get; set; }

		[Id(4, 1)]
		[Description("Extra mix files that should be loaded specific to your mod.\r\nCan be entered as a comma-separated list.")]
		[PropertyStateFlags((PropertyFlags.Default | PropertyFlags.ExpandIEnumerable) & ~PropertyFlags.SupportStandardValues)]
		[Editor(@"System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
		[TypeConverter(typeof(CsvConverter))]
		public List<string> ExtraMixes { get; set; }

        // Starkku: Custom INI filename support that certain mods alas require to function correctly with the renderer.
        [Id(5, 1)]
        [Description("Custom rules.ini filenames that should be loaded specific to your mod. First one listed is primary one, overriding standard game file. Contents of the rest get appended to it. Can be entered as a comma-separated list.")]
        [PropertyStateFlags((PropertyFlags.Default | PropertyFlags.ExpandIEnumerable) & ~PropertyFlags.SupportStandardValues)]
        [Editor(@"System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
        [TypeConverter(typeof(CsvConverter))]
        public List<string> CustomRulesIniFiles { get; set; }

        [Id(6, 1)]
        [Description("Custom art.ini filenames that should be used with your mod. First one listed is primary one, overriding standard game file. Contents of the rest get appended to it. Can be entered as a comma-separated list.")]
        [PropertyStateFlags((PropertyFlags.Default | PropertyFlags.ExpandIEnumerable) & ~PropertyFlags.SupportStandardValues)]
        [Editor(@"System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
        [TypeConverter(typeof(CsvConverter))]
        public List<string> CustomArtIniFiles { get; set; }

		[Id(7, 1)]
		[Description("The theater specific settings for each available theater")]
		[PropertyStateFlags(PropertyFlags.ExpandIEnumerable)]
		public BindingList<TheaterSettings> Theaters {
			get { return _theaters; }
			set {
				_theaters = value;
				_theaters.ListChanged += (sender, args) => OnPropertyChanged("Theaters");
			}
		}
		private BindingList<TheaterSettings> _theaters;

		[Id(8, 1)]
		[Description("Object specific overrides")]
		[PropertyStateFlags(PropertyFlags.ExpandIEnumerable)]
		public BindingList<ObjectOverride> ObjectOverrides {
			get { return _objectOverrides; }
			set {
				_objectOverrides = value;
				_objectOverrides.ListChanged += (sender, args) => OnPropertyChanged("ObjectOverrides");
			}
		}
		private BindingList<ObjectOverride> _objectOverrides;

		public ModConfig() {
			Name = "Custom mod config";
			Theaters = new BindingList<TheaterSettings>();
			ObjectOverrides = new BindingList<ObjectOverride>();
			Directories = new List<string> { };
			ExtraMixes = new List<string>();
            CustomRulesIniFiles = new List<string>();
            CustomArtIniFiles = new List<string>();
			Engine = EngineType.YurisRevenge;
			InstallTypeDescriptor();
		}

		private void InstallTypeDescriptor() {
			_dctd = ProviderInstaller.Install(this);
			_dctd.PropertySortOrder = CustomSortOrder.AscendingById;
		}

		public ModConfig Clone() {
			var ret = (ModConfig)MemberwiseClone();
			ret.ExtraMixes = new List<string>();
            ret.CustomRulesIniFiles = new List<string>();
            ret.CustomArtIniFiles = new List<string>();
			ret.Directories = new List<string>();

			ret.Theaters = new BindingList<TheaterSettings>();
			foreach (var t in Theaters)
				ret.Theaters.Add(t.Clone());

			ret.ObjectOverrides = new BindingList<ObjectOverride>();
			foreach (var t in ObjectOverrides)
				ret.ObjectOverrides.Add(t.Clone());

			ret.InstallTypeDescriptor();
			return ret;
		}


		#region Defaults per game

		public static readonly ModConfig DefaultsTS = new ModConfig {
			Name = "TS Defaults",
			ExtraMixes = new List<string>(),
			Theaters = new BindingList<TheaterSettings> {
				new TheaterSettings {
					Type = TheaterType.Temperate,
					TheaterIni = "temperat.ini",
					Mixes = new List<string> {
						"isotemp.mix",
						"temperat.mix",
						"tem.mix",
					},
					Extension = ".tem",
					NewTheaterChar = 'T',
					IsoPaletteName = "isotem.pal",
					UnitPaletteName = "unittem.pal",
					OverlayPaletteName = "temperat.pal",
				},
				new TheaterSettings {
					Type=TheaterType.Snow,
					TheaterIni = "snow.ini",
					Mixes = new List<string> {
						"isosnow.mix",
						"snow.mix",
						"sno.mix",
					},
					Extension = ".sno",
					NewTheaterChar = 'A',
					IsoPaletteName = "isosno.pal",
					UnitPaletteName = "unitsno.pal",
					OverlayPaletteName = "snow.pal",
				},
			}
		};

		public static readonly ModConfig DefaultsFS = DefaultsTS;

		public static readonly ModConfig DefaultsRA2 = new ModConfig {
			Name = "RA2 Defaults",
			ExtraMixes = new List<string>(),
			Theaters = new BindingList<TheaterSettings> {
				new TheaterSettings {
					Type = TheaterType.Temperate,
					TheaterIni = "temperat.ini",
					Mixes = new List<string> {
						"isotemp.mix",
						"temperat.mix",
						"tem.mix",
					},
					Extension = ".tem",
					NewTheaterChar = 'T',
					IsoPaletteName = "isotem.pal",
					UnitPaletteName = "unittem.pal",
					OverlayPaletteName = "temperat.pal",
				},
				new TheaterSettings {
					Type=TheaterType.Snow,
					TheaterIni = "snow.ini",
					Mixes = new List<string> {
						"isosnow.mix",
						"snow.mix",
						"sno.mix",
					},
					Extension = ".sno",
					NewTheaterChar = 'A',
					IsoPaletteName = "isosno.pal",
					UnitPaletteName = "unitsno.pal",
					OverlayPaletteName = "snow.pal",
				},
				new TheaterSettings {
					Type=TheaterType.Urban,
					TheaterIni = "urban.ini",
					Mixes = new List<string> {
						"isourb.mix",
						"urb.mix",
						"urban.mix",
					},
					Extension = ".urb",
					NewTheaterChar = 'U',
					IsoPaletteName = "isourb.pal",
					UnitPaletteName = "uniturb.pal",
					OverlayPaletteName = "urban.pal",
				},
			}
		};


		public static readonly ModConfig DefaultsYR = new ModConfig {
			Name = "YR Defaults",
			ExtraMixes = new List<string>(),
			Theaters = new BindingList<TheaterSettings> {
				new TheaterSettings {
					Type = TheaterType.Temperate,
					TheaterIni = "temperatmd.ini",
					Mixes = new List<string> {
						"isotemp.mix",
						"isotemmd.mix",
						"temperat.mix",
						"tem.mix",
					},
					Extension = ".tem",
					NewTheaterChar = 'T',
					IsoPaletteName = "isotem.pal",
					UnitPaletteName = "unittem.pal",
					OverlayPaletteName = "temperat.pal",
				},
				new TheaterSettings {
					Type=TheaterType.Snow,
					TheaterIni = "snowmd.ini",
					Mixes = new List<string> {
						"isosnomd.mix",
						"snowmd.mix",
						"isosnow.mix",
						"snow.mix",
						"sno.mix",
					},
					Extension = ".sno",
					NewTheaterChar = 'A',
					IsoPaletteName = "isosno.pal",
					UnitPaletteName = "unitsno.pal",
					OverlayPaletteName = "snow.pal",
				},
				new TheaterSettings {
					Type=TheaterType.Urban,
					TheaterIni = "urbanmd.ini",
					Mixes = new List<string> {
						"isourbmd.mix",
						"isourb.mix",
						"urb.mix",
						"urban.mix",
					},
					Extension = ".urb",
					NewTheaterChar = 'U',
					IsoPaletteName = "isourb.pal",
					UnitPaletteName = "uniturb.pal",
					OverlayPaletteName = "urban.pal",
				},
				new TheaterSettings {
					Type=TheaterType.NewUrban,
					TheaterIni = "urbannmd.ini",
					Mixes = new List<string> {
						"isoubnmd.mix",
						"isoubn.mix",
						"ubn.mix",
						"urbann.mix",
					},
					Extension = ".ubn",
					NewTheaterChar = 'N',
					IsoPaletteName = "isoubn.pal",
					UnitPaletteName = "unitubn.pal",
					OverlayPaletteName = "urbann.pal",
				},
				new TheaterSettings {
					Type=TheaterType.Desert,
					TheaterIni = "desertmd.ini",
					Mixes = new List<string> {
						"isodesmd.mix",
						"desert.mix",
						"des.mix",
						"isodes.mix",
					},
					Extension = ".des",
					NewTheaterChar = 'D',
					IsoPaletteName = "isodes.pal",
					UnitPaletteName = "unitdes.pal",
					OverlayPaletteName = "desert.pal",
				},
				new TheaterSettings {
					Type=TheaterType.Lunar,
					TheaterIni = "lunarmd.ini",
					Mixes = new List<string> {
						"isolunmd.mix",
						"isolun.mix",
						"lun.mix",
						"lunar.mix",
					},
					Extension = ".lun",
					NewTheaterChar = 'L',
					IsoPaletteName = "isolun.pal",
					UnitPaletteName = "unitlun.pal",
					OverlayPaletteName = "lunar.pal",
				},
			}
		};

		#endregion

		public TheaterSettings GetTheater(TheaterType th) {
			return Theaters.FirstOrDefault(t => t.Type.Equals(th));
		}

		public event PropertyChangedEventHandler PropertyChanged;
		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged(string propertyName) {
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	[Serializable]
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public class TheaterSettings {
		[NonSerialized]
		private DynamicCustomTypeDescriptor _dctd = null;

		public TheaterSettings() {
			Mixes = new List<string>();
			InstallTypeDescriptor();
		}

		internal void InstallTypeDescriptor() {
			_dctd = ProviderInstaller.Install(this);
			_dctd.PropertySortOrder = CustomSortOrder.AscendingById;
		}

		[Id(1, 1)]
		public TheaterType Type { get; set; }

		[Id(2, 1)]
		[Description("Mix files that should be loaded specific to this theater.")]
		[Editor(@"System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
		[TypeConverter(typeof(CsvConverter))]
		public List<string> Mixes { get; set; }

		[Id(3, 1)]
		public string TheaterIni { get; set; }

		[Id(4, 1)]
		public string Extension { get; set; }

		[Id(5, 1)]
		public char NewTheaterChar { get; set; }

		[Id(6, 1)]
		public string IsoPaletteName { get; set; }

		[Id(7, 1)]
		public string UnitPaletteName { get; set; }

		[Id(8, 1)]
		public string OverlayPaletteName { get; set; }

		public override string ToString() {
			return Type.ToString();
		}

		internal TheaterSettings Clone() {
			var ret = (TheaterSettings)MemberwiseClone();
			ret.InstallTypeDescriptor();
			return ret;
		}
	}

	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Serializable]
	public class ObjectOverride {
		[NonSerialized]
		private DynamicCustomTypeDescriptor _dctd = null;

		public ObjectOverride() {
			InstallTypeDescriptor();
			CollectionTypes = CollectionType.All;
			TheaterTypes = TheaterType.All;
			ObjRegex = "";
			Palette = PaletteType.Default;
			CustomPaletteFile = "";
			Lighting = LightingType.Default;
		}

		internal void InstallTypeDescriptor() {
			_dctd = ProviderInstaller.Install(this);
			_dctd.PropertySortOrder = CustomSortOrder.AscendingById;
		}

		[Id(1, 1)]
		[Description(".")]
		public CollectionType CollectionTypes { get; set; }

		[Id(2, 1)]
		[Description(".")]
		public TheaterType TheaterTypes { get; set; }

		[Id(3, 1)]
		[Description("Regex matching the name of objects that this override should be applied to (case insensitive).")]
		public string ObjRegex { get; set; }

		[Id(4, 1)]
		[Description("Palette type that should be used for these objects.")]
		public PaletteType Palette { get; set; }

		[Id(5, 1)]
		[Description("Filename of the custom palette, only used if PaletteType is set to custom.")]
		public string CustomPaletteFile { get; set; }

		[Id(6, 1)]
		[Description("The kind of lighting applied to these objects..")]
		public LightingType Lighting { get; set; }

		[Id(7, 1)]
		[Description("Compilable piece of C# code to determine a GameObjects frame")]
		[EditorAttribute("System.ComponentModel.Design.MultilineStringEditor, System.Design", "System.Drawing.Design.UITypeEditor")]
		public string FrameDeciderCode { get; set; }

		[Id(8, 1)]
		[Description("Priority of this override entry. Lower values indicate higher priority.")]
		public int Priority { get; set; }

		internal ObjectOverride Clone() {
			var ret = (ObjectOverride)MemberwiseClone();
			ret.InstallTypeDescriptor();
			return ret;
		}

		public override string ToString() {
			return string.Format("Override for \"{0}\"", !string.IsNullOrEmpty(ObjRegex) ? ObjRegex : "<empty>");
		}
	}
}
