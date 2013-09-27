using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace CNCMaps.Utility {

	public enum EngineType {
		AutoDetect = 0,
		TiberianSun = 1,
		Firestorm = 2,
		RedAlert2 = 3,
		YurisRevenge = 4,
	}

	public enum TheaterType {
		Temperate,
		Urban,
		Snow,
		Lunar,
		Desert,
		NewUrban
	}

	[Serializable]
	public class ModConfig {
		public static ModConfig ActiveConfig { get; set; }
		public static TheaterSettings ActiveTheater { get; private set; }

		internal static void LoadDefaultConfig(EngineType engine) {
			if (engine == EngineType.TiberianSun)
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
			return (ModConfig)xs.Deserialize(s);
		}

		public void Serialize(Stream s) {
			var xs = new XmlSerializer(typeof(ModConfig));
			xs.Serialize(s, this);
		}

		public string Name { get; set; }
		public EngineType Engine { get; set; }
		public List<string> ExtraDirectories { get; set; }
		public List<string> ExtraMixes { get; set; }
		public List<TheaterSettings> Theaters { get; set; }

		public ModConfig() {
			Name = "Custom mod config";
			Theaters = new List<TheaterSettings>();
			ExtraDirectories = new List<string>();
			ExtraMixes = new List<string>();
			Engine = EngineType.YurisRevenge;
		}

		#region Defaults per game

		internal static readonly ModConfig DefaultsTS = new ModConfig {
			Name = "TS Defaults",
			ExtraMixes = new List<string>(),
			Theaters = new List<TheaterSettings> {
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
			Theaters = new List<TheaterSettings> {
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
			Theaters = new List<TheaterSettings> {
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
						"isosnowmd.mix",
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
	}

	[Serializable]
	public class TheaterSettings {
		public TheaterSettings() {
			Mixes = new List<string>();
		}
		public TheaterType Type { get; set; }
		public List<string> Mixes { get; set; }
		public string TheaterIni { get; set; }
		public string Extension { get; set; }
		public char NewTheaterChar { get; set; }
		public string IsoPaletteName { get; set; }
		public string UnitPaletteName { get; set; }
		public string OverlayPaletteName { get; set; }

		public override string ToString() {
			return Type.ToString();
		}

	}

}
