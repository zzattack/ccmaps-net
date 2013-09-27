using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using CNCMaps.GUI;
using CNCMaps.GUI.Annotations;
using DynamicTypeDescriptor;

namespace CNCMaps.Utility {

	[Editor(typeof(StandardValueEditor), typeof(UITypeEditor))]
	public enum EngineType {
		[StandardValue("Auto Detect", Visible = false)]
		AutoDetect = 0,

		[StandardValue("Tiberian Sun")]
		TiberianSun = 1,

		[StandardValue("Firestorm")]
		Firestorm = 2,

		[StandardValue("Red Alert 2")]
		RedAlert2 = 3,

		[StandardValue("Yuri's Revenge")]
		YurisRevenge = 4,
	}

	[Editor(typeof(StandardValueEditor), typeof(UITypeEditor))]
	public enum TheaterType {
		Temperate,
		Urban,
		Snow,
		Lunar,
		Desert,
		NewUrban
	}

	[Serializable]
	public class ModConfig : INotifyPropertyChanged {

		[NonSerialized]
		private DynamicCustomTypeDescriptor _dctd = null;

		public static ModConfig ActiveConfig { get; set; }
		public static TheaterSettings ActiveTheater { get; private set; }

		internal static void SetActiveConfig(EngineType engine, string modConfigFile) {
			if (!string.IsNullOrEmpty(modConfigFile)) {
				var fs = File.OpenRead(modConfigFile);
				ActiveConfig = Deserialize(fs);
				fs.Close();
			}
			else if (engine == EngineType.TiberianSun)
				ActiveConfig = DefaultsTS;
			else if (engine == EngineType.Firestorm)
				ActiveConfig = DefaultsFS;
			else if (engine == EngineType.RedAlert2)
				ActiveConfig = DefaultsRA2;
			else if (engine == EngineType.YurisRevenge)
				ActiveConfig = DefaultsYR;
		}

		public static void SetActiveTheater(TheaterType theater) {
			ActiveTheater = ActiveConfig.Theaters.First(t => t.Type == theater);
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

		[Id(5, 1)]
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

		public ModConfig() {
			Name = "Custom mod config";
			Theaters = new BindingList<TheaterSettings>();
			Directories = new List<string> { };
			ExtraMixes = new List<string>();
			Engine = EngineType.YurisRevenge;
			InstallTypeDescriptor();
		}

		private void InstallTypeDescriptor() {
			_dctd = ProviderInstaller.Install(this);
			_dctd.PropertySortOrder = CustomSortOrder.AscendingById;
		}
		
		public ModConfig Clone() {
			var ret = (ModConfig)this.MemberwiseClone();
			ret.ExtraMixes = new List<string>();
			ret.Directories = new List<string>();
			ret.Theaters = new BindingList<TheaterSettings>();
			foreach (var t in Theaters)
				ret.Theaters.Add(t.Clone());
			ret.InstallTypeDescriptor();
			return ret;
		}


		#region Defaults per game

		internal static readonly ModConfig DefaultsTS = new ModConfig {
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

		internal static readonly ModConfig DefaultsFS = DefaultsTS;

		internal static readonly ModConfig DefaultsRA2 = new ModConfig {
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


		internal static readonly ModConfig DefaultsYR = new ModConfig {
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

		internal TheaterSettings GetTheater(TheaterType th) {
			return Theaters.First(t => t.Type == th);
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
			var ret = (TheaterSettings)this.MemberwiseClone();
			ret.InstallTypeDescriptor();
			return ret;
		}
	}

	public class CsvConverter : TypeConverter {
		// Overrides the ConvertTo method of TypeConverter.
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
			var v = value as IEnumerable<String>;
			if (destinationType == typeof(string)) {
				return string.Join(", ", v.Select(AddQuotes).ToArray());
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}

		private string AddQuotes(string s) {
			if (string.IsNullOrWhiteSpace(s)) return "";

			var sb = new StringBuilder();
			if (!s.StartsWith("\""))
				sb.Append('"');
			sb.Append(s);
			if (!s.EndsWith("\""))
				sb.Append('"');
			return sb.ToString();
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
			if (sourceType == typeof(string)) {
				return true;
			}
			return base.CanConvertFrom(context, sourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type sourceType) {
			if (sourceType == typeof(IList<string>)) {
				return true;
			}
			return base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
			if (value is string) {
				var vs = ((string)value).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
				return vs.Select(v => v.Trim('"')).ToList();
			}
			return base.ConvertFrom(context, culture, value);
		}

		public override bool IsValid(ITypeDescriptorContext context, object value) {
			return true; // basically anything is valid
		}
	}

}
