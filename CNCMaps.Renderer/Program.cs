using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.Engine.Utility;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.Map;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;
using System;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace CNCMaps {
	class Program {
		static OptionSet _options;
		static Logger Logger;
		public static RenderSettings Settings;

		public static int Main(string[] args) {
			InitLoggerConfig();
			InitSettings(args);

			// DumpTileProperties();

			if (!ValidateSettings())
				return 2;

			try {
				Logger.Info("Initializing virtual filesystem");

				var mapStream = File.Open(Settings.InputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				VirtualFile vmapFile;
				var mixMap = new MixFile(mapStream, Settings.InputFile, 0, mapStream.Length, false, false);
				if (mixMap.IsValid()) { // input max is a mix
					var mapArchive = new MixFile(mapStream, Path.GetFileName(Settings.InputFile), true);
					// grab the largest file in the archive
					var mixEntry = mapArchive.Index.OrderByDescending(me => me.Value.Length).First();
					vmapFile = mapArchive.OpenFile(mixEntry.Key);
				}
				else {
					vmapFile = new VirtualFile(mapStream, Path.GetFileName(Settings.InputFile), true);
				}
				var mapFile = new MapFile(vmapFile, Path.GetFileName(Settings.InputFile));

				if (!string.IsNullOrEmpty(Settings.ModConfig)) {
					if (File.Exists(Settings.ModConfig)) {
						ModConfig cfg;
						try {
							using (FileStream f = File.OpenRead(Settings.ModConfig))
								cfg = ModConfig.Deserialize(f);
							ModConfig.ActiveConfig = cfg;
							if (Settings.Engine != EngineType.AutoDetect) {
								if (Settings.Engine != cfg.Engine)
									Logger.Warn("Provided engine override does not match mod config.");
							}
							else
								Settings.Engine = ModConfig.ActiveConfig.Engine;
						}
						catch (IOException) {
							Logger.Error("IOException while loading mod config");
						}
						catch (XmlException) {
							Logger.Error("XmlException while loading mod config");
						}
						catch (SerializationException) {
							Logger.Error("Serialization exception while loading mod config");
						}
					}
					else {
						Logger.Error("Invalid mod config file specified");
					}
				}

				if (Settings.Engine == EngineType.AutoDetect) {
					Settings.Engine = EngineDetector.DetectEngineType(mapFile);
					Logger.Info("Engine autodetect result: {0}", Settings.Engine);
				}

				// ---------------------------------------------------------------
				// Code to organize moving of maps in a directory for themselves
				/*
				string mapName = DetermineMapName(mapFile, Settings.Engine);
				string ndir = Path.Combine(Path.GetDirectoryName(Settings.InputFile), mapName);
				if (!Directory.Exists(ndir)) Directory.CreateDirectory(ndir);
				mapFile.Close();
				mapFile.Dispose();
				File.Move(Settings.InputFile, Path.Combine(ndir, Path.GetFileName(mapFile.FileName)));
				return 0;*/
				// ---------------------------------------------------------------

				// enginetype is now definitive, load mod config
				if (ModConfig.ActiveConfig == null)
					ModConfig.LoadDefaultConfig(Settings.Engine);

				// first add the dirs, then load the extra mixes, then scan the dirs
				foreach (string modDir in ModConfig.ActiveConfig.Directories)
					VFS.Add(modDir);

				// add mixdir to VFS (if it's not included in the mod config)
				if (!ModConfig.ActiveConfig.Directories.Any()) {
					string mixDir = VFS.DetermineMixDir(Settings.MixFilesDirectory, Settings.Engine);
					VFS.Add(mixDir);
				}
				foreach (string mixFile in ModConfig.ActiveConfig.ExtraMixes)
					VFS.Add(mixFile);
				foreach (var dir in VFS.Instance.AllArchives.OfType<DirArchive>().Select(d => d.Directory).ToList())
					VFS.Instance.ScanMixDir(dir, Settings.Engine);

				var map = new Map {
					IgnoreLighting = Settings.IgnoreLighting,
					StartPosMarking = Settings.StartPositionMarking,
					MarkOreFields = Settings.MarkOreFields
				};

				if (!map.Initialize(mapFile, Settings.Engine)) {
					Logger.Error("Could not successfully load this map. Try specifying the engine type manually.");
					return 1;
				}

				if (!map.LoadTheater()) {
					Logger.Error("Could not successfully load all required components for this map. Aborting.");
					return 1;
				}

				if (Settings.StartPositionMarking == StartPositionMarking.Tiled)
					map.MarkTiledStartPositions();

				if (Settings.MarkOreFields)
					map.MarkOreAndGems();

				map.Draw();

#if DEBUG
				// ====================================================================================
				using (var form = new DebugDrawingSurfaceWindow(map.GetDrawingSurface(), map.GetTiles(), map.GetTheater(), map)) {
					form.RequestTileEvaluate += map.DebugDrawTile; form.ShowDialog();
				}
				// ====================================================================================
#endif

				if (Settings.StartPositionMarking == StartPositionMarking.Squared)
					map.DrawSquaredStartPositions();

				if (Settings.OutputFile == "")
					Settings.OutputFile = DetermineMapName(mapFile, Settings.Engine);

				if (Settings.OutputDir == "")
					Settings.OutputDir = Path.GetDirectoryName(Settings.InputFile);

				// free up as much memory as possible before saving the large images
				Rectangle saveRect = map.GetSizePixels(Settings.SizeMode);
				DrawingSurface ds = map.GetDrawingSurface();
				// if we don't need this data anymore, we can try to save some memory
				if (!Settings.GeneratePreviewPack) {
					ds.FreeNonBitmap();
					map.FreeUseless();
					GC.Collect();
				}

				if (Settings.SaveJPEG)
					ds.SaveJPEG(Path.Combine(Settings.OutputDir, Settings.OutputFile + ".jpg"), Settings.JPEGCompression, saveRect);

				if (Settings.SavePNG)
					ds.SavePNG(Path.Combine(Settings.OutputDir, Settings.OutputFile + ".png"), Settings.PNGQuality, saveRect);

				Regex reThumb = new Regex(@"(\+)?\((\d+),(\d+)\)");
				var match = reThumb.Match(Settings.ThumbnailSettings);
				if (match.Success) {
					Size dimensions = new Size(
						int.Parse(match.Groups[2].Captures[0].Value),
						int.Parse(match.Groups[3].Captures[0].Value));
					var cutRect = map.GetSizePixels(Settings.SizeMode);

					if (match.Groups[1].Captures[0].Value == "+") {
						// + means maintain aspect ratio
						double aspectRatio = cutRect.Width / (double)cutRect.Height;
						if (dimensions.Width / (double)dimensions.Height > aspectRatio) {
							dimensions.Height = (int)(dimensions.Width / aspectRatio);
						}
						else {
							dimensions.Width = (int)(dimensions.Height / aspectRatio);
						}
					}
					Logger.Info("Saving thumbnail with dimensions {0}x{1}", dimensions.Width, dimensions.Height);
					ds.SaveThumb(dimensions, cutRect, Path.Combine(Settings.OutputDir, "thumb_" + Settings.OutputFile + ".jpg"));
				}

				if (Settings.GeneratePreviewPack) {
					if (mapFile.BaseStream is MixFile)
						Logger.Error("Cannot inject thumbnail into an archive (.mmx/.yro/.mix)!");
					else {
						map.GeneratePreviewPack(Settings.OmitPreviewPackMarkers, Settings.SizeMode, mapFile);
						Logger.Info("Saving map");
						mapFile.Save(Settings.InputFile);
					}
				}
			}
			catch (Exception exc) {
				Logger.Error(string.Format("An unknown fatal exception occured: {0}", exc), exc);
#if DEBUG
				throw;
#else
				return 1;
#endif
			}

			LogManager.Configuration = null; // required for mono release to flush possible targets
			return 0;
		}

		/*private static void DumpTileProperties() {
			ModConfig.LoadDefaultConfig(EngineType.YurisRevenge);
			VFS.Instance.ScanMixDir("", EngineType.YurisRevenge);

			foreach (var th in new[] { TheaterType.Temperate, TheaterType.Urban, TheaterType.Snow, TheaterType.Lunar, TheaterType.Desert, TheaterType.NewUrban }) {
				ModConfig.SetActiveTheater(th);
				foreach (var m in ModConfig.ActiveTheater.Mixes) VFS.Add(m);

				var tc = new TileCollection(ModConfig.ActiveTheater, VFS.Open<IniFile>(ModConfig.ActiveTheater.TheaterIni));
				tc.InitTilesets();
				for (int i = 0; i < tc.NumTiles; i++) {
					var mt = new MapTile(0, 0, 0, 0, 0, (short)i, 0, null);
					var td = tc.GetDrawable(mt) as TileDrawable;
					var tf = td.GetTileFile(mt);
					if (tf != null) tf.Initialize(i);
				}

				Debug.WriteLine("_-----------------------------------------");

			}
		}*/


		private static void InitLoggerConfig() {
			// for release, logmanager automatically picks the correct NLog.config file
#if DEBUG
			try {
				LogManager.Configuration = new XmlLoggingConfiguration("NLog.Debug.config");
			}
			catch {
			}
#endif
			if (LogManager.Configuration == null) {
				// init default config
				var target = new ColoredConsoleTarget();
				target.Name = "console";
				target.Layout = "${processtime:format=s\\.ffff} [${level}] ${message}";
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.Magenta,
					Condition = "level = LogLevel.Fatal"
				});
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.Red,
					Condition = "level = LogLevel.Error"
				});
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.Yellow,
					Condition = "level = LogLevel.Warn"
				});
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.Gray,
					Condition = "level = LogLevel.Info"
				});
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.DarkGray,
					Condition = "level = LogLevel.Debug"
				});
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule() {
					ForegroundColor = ConsoleOutputColor.White,
					Condition = "level = LogLevel.Trace"
				});
				LogManager.Configuration = new LoggingConfiguration();
				LogManager.Configuration.AddTarget("console", target);
#if DEBUG
				LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, target));
#else
				LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, target));

#endif
				LogManager.ReconfigExistingLoggers();
			}
			Logger = LogManager.GetCurrentClassLogger();
		}

		private static void InitSettings(string[] args) {
			Settings = RenderSettings.CreateDefaults();
			_options = new OptionSet {
				{"h|help", "Show this short help text", v => Settings.ShowHelp = true},
				{"i|infile=", "Input file", v => Settings.InputFile = v},
				{"o|outfile=", "Output file, without extension, read from map if not specified.", v => Settings.OutputFile = v},
				{"d|outdir=", "Output directiory", v => Settings.OutputDir = v},
				{"y|force-ra2", "Force using the Red Alert 2 engine for rendering", v => Settings.Engine = EngineType.RedAlert2}, 
				{"Y|force-yr", "Force using the Yuri's Revenge engine for rendering", v => Settings.Engine = EngineType.YurisRevenge},
				{"t|force-ts", "Force using the Tiberian Sun engine for rendering", v => Settings.Engine = EngineType.TiberianSun},
				{"T|force-fs", "Force using the Firestorm engine for rendering", v => Settings.Engine = EngineType.Firestorm},
				{"j|output-jpg", "Output JPEG file", v => Settings.SaveJPEG = true},
				{"q|jpeg-quality=", "Set JPEG quality level (0-100)", (int v) => Settings.JPEGCompression = v},
				{"p|output-png", "Output PNG file", v => Settings.SavePNG = true},
				{"c|png-compression=", "Set PNG compression level (1-9)", (int v) => Settings.PNGQuality = v}, 
				{"m|mixdir=", "Specify location of .mix files, read from registry if not specified (win only)",v => Settings.MixFilesDirectory = v},
				{"M|modconfig=", "Filename of a game configuration specific to your mod (create with GUI)",v => Settings.ModConfig = v},
				{"s|start-pos-tiled", "Mark starting positions in a tiled manner",v => Settings.StartPositionMarking = StartPositionMarking.Tiled},
				{"S|start-pos-squared", "Mark starting positions in a squared manner",v => Settings.StartPositionMarking = StartPositionMarking.Squared}, 
				{"r|mark-ore", "Mark ore and gem fields more explicity, looks good when resizing to a preview",v => Settings.MarkOreFields = true},
				{"F|force-fullmap", "Ignore LocalSize definition and just save the full map", v => Settings.SizeMode = SizeMode.Full},
				{"f|force-localsize", "Use localsize for map dimensions (default)", v => Settings.SizeMode = SizeMode.Local}, 
				{"k|replace-preview", "Update the maps [PreviewPack] data with the rendered image",v => Settings.GeneratePreviewPack = true}, 
				{"n|ignore-lighting", "Ignore all lighting and lamps on the map",v => Settings.IgnoreLighting = true}, 
				{"K|replace-preview-nosquares", "Update the maps [PreviewPack] data with the rendered image, without squares",
					v => {
						Settings.GeneratePreviewPack = true;
						Settings.OmitPreviewPackMarkers = true;
					}
				}, 
				// {"G|graphics-winmgr", "Attempt rendering voxels using window manager context first (default)",v => Settings.PreferOSMesa = false},
				{"g|graphics-osmesa", "Attempt rendering voxels using OSMesa context first", v => Settings.PreferOSMesa = true},
				{"z|create-thumbnail=", "Also save a thumbnail along with the fullmap in dimensions (x,y), prefix with + to keep aspect ratio	", v => Settings.ThumbnailSettings = v},
			};

			_options.Parse(args);
		}

		private static bool ValidateSettings() {
			if (Settings.ShowHelp) {
				ShowHelp();
				return false; // not really false :/
			}
			else if (!File.Exists(Settings.InputFile)) {
				Logger.Error("Specified input file does not exist");
				return false;
			}
			else if (!Settings.SaveJPEG && !Settings.SavePNG && !Settings.GeneratePreviewPack) {
				Logger.Error("No output format selected. Either specify -j, -p, -k or a combination");
				return false;
			}
			else if (Settings.OutputDir != "" && !Directory.Exists(Settings.OutputDir)) {
				Logger.Error("Specified output directory does not exist.");
				return false;
			}
			return true;
		}

		private static void ShowHelp() {
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write("Usage: ");
			Console.WriteLine("");
			var sb = new StringBuilder();
			var sw = new StringWriter(sb);
			_options.WriteOptionDescriptions(sw);
			Console.WriteLine(sb.ToString());
		}

		public static bool IsLinux {
			get {
				int p = (int)Environment.OSVersion.Platform;
				return (p == 4) || (p == 6) || (p == 128);
			}
		}

		/// <summary>Gets the determine map name. </summary>
		/// <returns>The filename to save the map as</returns>
		public static string DetermineMapName(MapFile map, EngineType engine) {
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(map.FileName);

			IniFile.IniSection basic = map.GetSection("Basic");
			if (basic.ReadBool("Official") == false)
				return StripPlayersFromName(basic.ReadString("Name", fileNameWithoutExtension));

			string mapExt = Path.GetExtension(Settings.InputFile);
			string missionName = "";
			string mapName = "";
			PktFile.PktMapEntry pktMapEntry = null;
			MissionsFile.MissionEntry missionEntry = null;

			// campaign mission
			if (!basic.ReadBool("MultiplayerOnly") && basic.ReadBool("Official")) {
				string missionsFile;
				switch (engine) {
					case EngineType.TiberianSun:
					case EngineType.RedAlert2:
						missionsFile = "mission.ini";
						break;
					case EngineType.Firestorm:
						missionsFile = "mission1.ini";
						break;
					case EngineType.YurisRevenge:
						missionsFile = "missionmd.ini";
						break;
					default:
						throw new ArgumentOutOfRangeException("engine");
				}
				var mf = VFS.Open<MissionsFile>(missionsFile);
				missionEntry = mf.GetMissionEntry(Path.GetFileName(map.FileName));
				if (missionEntry != null)
					missionName = (engine >= EngineType.RedAlert2) ? missionEntry.UIName : missionEntry.Name;
			}

			else {
				// multiplayer map
				string pktEntryName = fileNameWithoutExtension;
				PktFile pkt = null;

				if (FormatHelper.MixArchiveExtensions.Contains(mapExt)) {
					// this is an 'official' map 'archive' containing a PKT file with its name
					try {
						var mix = new MixFile(File.Open(Settings.InputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
						pkt = mix.OpenFile(fileNameWithoutExtension + ".pkt", FileFormat.Pkt) as PktFile;
						// pkt file is cached by default, so we can close the handle to the file
						mix.Close();

						if (pkt != null && pkt.MapEntries.Count > 0)
							pktEntryName = pkt.MapEntries.First().Key;
					}
					catch (ArgumentException) { }
				}

				else {
					// determine pkt file based on engine
					switch (engine) {
						case EngineType.TiberianSun:
						case EngineType.RedAlert2:
							pkt = VFS.Open<PktFile>("missions.pkt");
							break;
						case EngineType.Firestorm:
							pkt = VFS.Open<PktFile>("multi01.pkt");
							break;
						case EngineType.YurisRevenge:
							pkt = VFS.Open<PktFile>("missionsmd.pkt");
							break;
						default:
							throw new ArgumentOutOfRangeException("engine");
					}
				}


				// fallback for multiplayer maps with, .map extension,
				// no YR objects so assumed to be ra2, but actually meant to be used on yr
				if (mapExt == ".map" && pkt != null && !pkt.MapEntries.ContainsKey(pktEntryName) && engine >= EngineType.RedAlert2) {
					var vfs = new VFS();
					vfs.AddFile(Settings.InputFile);
					pkt = vfs.OpenFile<PktFile>("missionsmd.pkt");
				}

				if (pkt != null && !string.IsNullOrEmpty(pktEntryName))
					pktMapEntry = pkt.GetMapEntry(pktEntryName);
			}

			// now, if we have a map entry from a PKT file, 
			// for TS we are done, but for RA2 we need to look in the CSV file for the translated mapname
			if (engine <= EngineType.Firestorm) {
				if (pktMapEntry != null)
					mapName = pktMapEntry.Description;
				else if (missionEntry != null) {
					if (engine == EngineType.TiberianSun) {
						string campaignSide;
						string missionNumber;

						if (missionEntry.Briefing.Length >= 3) {
							campaignSide = missionEntry.Briefing.Substring(0, 3);
							missionNumber = missionEntry.Briefing.Length > 3 ? missionEntry.Briefing.Substring(3) : "";
							missionName = "";
							mapName = string.Format("{0} {1} - {2}", campaignSide, missionNumber.TrimEnd('A').PadLeft(2, '0'), missionName);
						}
						else if (missionEntry.Name.Length >= 10) {
							mapName = missionEntry.Name;
						}
					}
					else {
						// FS map names are constructed a bit easier
						mapName = missionName.Replace(":", " - ");
					}
				}
				else if (!string.IsNullOrEmpty(basic.ReadString("Name")))
					mapName = basic.ReadString("Name", fileNameWithoutExtension);
			}

				// if this is a RA2/YR mission (csfEntry set) or official map with valid pktMapEntry
			else if (missionEntry != null || pktMapEntry != null) {
				string csfEntryName = missionEntry != null ? missionName : pktMapEntry.Description;

				string csfFile = engine == EngineType.YurisRevenge ? "ra2md.csf" : "ra2.csf";
				Logger.Info("Loading csf file {0}", csfFile);
				var csf = VFS.Open<CsfFile>(csfFile);
				mapName = csf.GetValue(csfEntryName.ToLower());

				if (missionEntry != null) {
					if (mapName.Contains("Operation: ")) {
						string missionMapName = Path.GetFileName(map.FileName);
						if (char.IsDigit(missionMapName[3]) && char.IsDigit(missionMapName[4])) {
							string missionNr = Path.GetFileName(map.FileName).Substring(3, 2);
							mapName = mapName.Substring(0, mapName.IndexOf(":")) + " " + missionNr + " -" +
									  mapName.Substring(mapName.IndexOf(":") + 1);
						}
					}
				}
				else {
					// not standard map
					if ((pktMapEntry.GameModes & PktFile.GameMode.Standard) == 0) {
						if ((pktMapEntry.GameModes & PktFile.GameMode.Megawealth) == PktFile.GameMode.Megawealth)
							mapName += " (Megawealth)";
						if ((pktMapEntry.GameModes & PktFile.GameMode.Duel) == PktFile.GameMode.Duel)
							mapName += " (Land Rush)";
						if ((pktMapEntry.GameModes & PktFile.GameMode.NavalWar) == PktFile.GameMode.NavalWar)
							mapName += " (Naval War)";
					}
				}
			}

			// not really used, likely empty, but if this is filled in it's probably better than guessing
			if (mapName == "" && basic.SortedEntries.ContainsKey("Name"))
				mapName = basic.ReadString("Name");

			if (mapName == "") {
				Logger.Warn("No valid mapname given or found, reverting to default filename {0}", fileNameWithoutExtension);
				mapName = fileNameWithoutExtension;
			}
			else {
				Logger.Info("Mapname found: {0}", mapName);
			}

			mapName = StripPlayersFromName(MakeValidFileName(mapName)).Replace("  ", " ");
			return mapName;
		}

		private static string StripPlayersFromName(string mapName) {
			if (mapName.IndexOf(" (") != -1)
				mapName = mapName.Substring(0, mapName.IndexOf(" ("));
			else if (mapName.IndexOf(" [") != -1)
				mapName = mapName.Substring(0, mapName.IndexOf(" ["));
			return mapName;
		}

		/// <summary>Makes a valid file name.</summary>
		/// <param name="name">The filename to be made valid.</param>
		/// <returns>The valid file name.</returns>
		private static string MakeValidFileName(string name) {
			string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
			string invalidReStr = string.Format(@"[{0}]+", invalidChars);
			return Regex.Replace(name, invalidReStr, "_");
		}

	}
}