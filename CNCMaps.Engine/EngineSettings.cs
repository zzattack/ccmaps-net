using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.Map;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace CNCMaps.Engine {
	public class EngineSettings {
		static Logger _logger = LogManager.GetCurrentClassLogger();
		public static RenderSettings Settings;

		public bool ConfigureFromArgs(string[] args) {
			InitLoggerConfig();
			Settings = new RenderSettings();
			Settings.ConfigureFromArgs(args);

			if (Settings.Debug && !Debugger.IsAttached) 
				Debugger.Launch();

			return ValidateSettings();
		}

		public bool ConfigureFromSettings(RenderSettings settings) { 
			Settings = settings;
			return ValidateSettings();
		}
		
		public int Execute() { 
			try {
				_logger.Info("Initializing virtual filesystem");

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
									_logger.Warn("Provided engine override does not match mod config.");
							}
							else
								Settings.Engine = ModConfig.ActiveConfig.Engine;
						}
						catch (IOException) {
							_logger.Fatal("IOException while loading mod config");
						}
						catch (XmlException) {
							_logger.Fatal("XmlException while loading mod config");
						}
						catch (SerializationException) {
							_logger.Fatal("Serialization exception while loading mod config");
						}
					}
					else {
						_logger.Fatal("Invalid mod config file specified");
					}
				}

				if (Settings.Engine == EngineType.AutoDetect) {
					Settings.Engine = EngineDetector.DetectEngineType(mapFile);
					_logger.Info("Engine autodetect result: {0}", Settings.Engine);
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

				VFS.Instance.LoadMixes(Settings.Engine);

				var map = new Map.Map {
					IgnoreLighting = Settings.IgnoreLighting,
					StartPosMarking = Settings.StartPositionMarking,
					MarkOreFields = Settings.MarkOreFields
				};

				if (!map.Initialize(mapFile, Settings.Engine, ModConfig.ActiveConfig.CustomRulesIniFiles, ModConfig.ActiveConfig.CustomArtIniFiles)) {
					_logger.Error("Could not successfully load this map. Try specifying the engine type manually.");
					return 1;
				}

				if (!map.LoadTheater()) {
					_logger.Error("Could not successfully load all required components for this map. Aborting.");
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

				Regex reThumb = new Regex(@"(\+|)?\((\d+),(\d+)\)");
				var match = reThumb.Match(Settings.ThumbnailConfig);
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
					_logger.Info("Saving thumbnail with dimensions {0}x{1}", dimensions.Width, dimensions.Height);
					ds.SaveThumb(dimensions, cutRect, Path.Combine(Settings.OutputDir, "thumb_" + Settings.OutputFile + ".jpg"));
				}

				if (Settings.GeneratePreviewPack) {
					if (mapFile.BaseStream is MixFile)
						_logger.Error("Cannot inject thumbnail into an archive (.mmx/.yro/.mix)!");
					else {
						map.GeneratePreviewPack(Settings.PreviewMarkers, Settings.SizeMode, mapFile, Settings.FixPreviewDimensions);
						_logger.Info("Saving map");
						mapFile.Save(Settings.InputFile);
					}
				}
			}
			catch (Exception exc) {
				_logger.Error(string.Format("An unknown fatal exception occured: {0}", exc), exc);
				throw;
			}
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
				if (File.Exists("NLog.Debug.config")) {
					LogManager.Configuration = new XmlLoggingConfiguration("NLog.Debug.config");
				}
			}
			catch (XmlException) { }
			catch (NLogConfigurationException) { }
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
			_logger = LogManager.GetCurrentClassLogger();
		}
		private static bool ValidateSettings() {
			if (Settings.ShowHelp) {
				ShowHelp();
				return false; // not really false :/
			}
			else if (!File.Exists(Settings.InputFile)) {
				_logger.Error("Specified input file does not exist");
				return false;
			}
			else if (!Settings.SaveJPEG && !Settings.SavePNG && !Settings.GeneratePreviewPack) {
				_logger.Error("No output format selected. Either specify -j, -p, -k or a combination");
				return false;
			}
			else if (Settings.OutputDir != "" && !Directory.Exists(Settings.OutputDir)) {
				_logger.Error("Specified output directory does not exist.");
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
			Settings.GetOptions().WriteOptionDescriptions(sw);
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
				return StripPlayersFromName(MakeValidFileName(basic.ReadString("Name", fileNameWithoutExtension)));

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
				if (mf != null)
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
					vfs.AddItem(Settings.InputFile);
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
				_logger.Info("Loading csf file {0}", csfFile);
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
				_logger.Warn("No valid mapname given or found, reverting to default filename {0}", fileNameWithoutExtension);
				mapName = fileNameWithoutExtension;
			}
			else {
				_logger.Info("Mapname found: {0}", mapName);
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
