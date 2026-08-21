using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Globalization;
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
	public class RenderEngine {
		static Logger _logger = LogManager.GetCurrentClassLogger();
		private RenderSettings _settings = new RenderSettings();

		/// <summary>
		/// Optional interactive preview, shown after drawing but before the output files are
		/// saved, while game resources are still loaded (required for tile re-evaluation).
		/// Takes precedence over the built-in diagnostic window.
		/// </summary>
		public Rendering.IMapPreviewWindow PreviewWindow { get; set; }

		public bool ConfigureFromArgs(string[] args) {
			InitLoggerConfig();
			_settings.ConfigureFromArgs(args);

			if (_settings.Debug && !Debugger.IsAttached)
				Debugger.Launch();

			return ValidateSettings();
		}

		public bool ConfigureFromSettings(RenderSettings settings) {
			_settings = settings;
			return ValidateSettings();
		}

		public EngineResult Execute() {
			try {
				// make each render deterministic regardless of how many renders ran
				// earlier in this process
				CNCMaps.Shared.Utility.Rand.Reset();

				_logger.Info("Initializing virtual filesystem");

				var mapStream = File.Open(_settings.InputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				VirtualFile vmapFile;
				var mixMap = new MixFile(mapStream, _settings.InputFile, 0, mapStream.Length, false, false);
				if (mixMap.IsValid()) { // input max is a mix
					var mapArchive = new MixFile(mapStream, Path.GetFileName(_settings.InputFile), true);
					// grab the largest file in the archive
					var mixEntry = mapArchive.Index.OrderByDescending(me => me.Value.Length).First();
					vmapFile = mapArchive.OpenFile(mixEntry.Key);
				}
				else {
					vmapFile = new VirtualFile(mapStream, Path.GetFileName(_settings.InputFile), true);
					vmapFile = ResolveIniInheritance(vmapFile,
						Path.GetDirectoryName(Path.GetFullPath(_settings.InputFile)));
				}
				var mapFile = new MapFile(vmapFile, Path.GetFileName(_settings.InputFile));

				ModConfig modConfig = null;
				if (!string.IsNullOrEmpty(_settings.ModConfig)) {
					if (File.Exists(_settings.ModConfig)) {
						try {
							using (FileStream f = File.OpenRead(_settings.ModConfig))
								modConfig = ModConfig.Deserialize(f);

							// relative directories resolve against the config file, so a config can ship
							// inside a mod folder and work from any working directory
							string configDir = Path.GetDirectoryName(Path.GetFullPath(_settings.ModConfig));
							for (int i = 0; i < modConfig.Directories.Count; i++) {
								if (!Path.IsPathRooted(modConfig.Directories[i]))
									modConfig.Directories[i] = Path.GetFullPath(Path.Combine(configDir, modConfig.Directories[i]));
								if (!Directory.Exists(modConfig.Directories[i]))
									_logger.Warn("Mod config directory {0} does not exist", modConfig.Directories[i]);
							}
							for (int i = 0; i < modConfig.ExtraMixes.Count; i++) {
								if (Path.IsPathRooted(modConfig.ExtraMixes[i])) continue;
								string resolved = Path.GetFullPath(Path.Combine(configDir, modConfig.ExtraMixes[i]));
								// a name that is no file beside the config resolves inside the VFS search order
								if (File.Exists(resolved))
									modConfig.ExtraMixes[i] = resolved;
							}
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

				if (_settings.Engine == EngineType.AutoDetect) {
					_settings.Engine = EngineDetector.DetectEngineType(mapFile, _settings.MixFilesDirectories, _settings.InputFile);
					_logger.Info("Engine autodetect result: {0}", _settings.Engine);
				}

				// Engine type is now definitive, load mod config
				if (modConfig == null)
					modConfig = ModConfig.GetDefaultConfig(_settings.Engine);

				var map = new Map.Map {
					IgnoreLighting = _settings.IgnoreLighting,
					StartPosMarking = _settings.StartPositionMarking,
					StartMarkerSize = _settings.MarkerStartSize,
					MarkOreFields = _settings.MarkOreFields
				};

				string resolvedName = Path.GetFileNameWithoutExtension(_settings.InputFile);
				using (var vfs = new VirtualFileSystem()) {
					// first add the dirs, then load the extra mixes, then scan the dirs
					foreach (string modDir in modConfig.Directories)
						vfs.Add(modDir);

					// add the mixdirs to VFS (if they're not included in the mod config)
					if (!modConfig.Directories.Any()) {
						if (_settings.MixFilesDirectories.Any())
							foreach (string mixDir in _settings.MixFilesDirectories)
								vfs.Add(mixDir);
						else
							vfs.Add(VirtualFileSystem.DetermineMixDir(null, _settings.Engine));
					}

					foreach (string mixFile in modConfig.ExtraMixes)
						vfs.Add(mixFile);

					vfs.LoadMixes(_settings.Engine);

					if (!map.Initialize(mapFile, modConfig, vfs)) {
						_logger.Error("Could not successfully load this map. Try specifying the engine type manually.");
						return EngineResult.LoadRulesFailed;
					}

					if (!map.LoadTheater()) {
						_logger.Error("Could not successfully load all required components for this map. Aborting.");
						return EngineResult.LoadTheaterFailed;
					}

					if (_settings.MarkStartPos && _settings.StartPositionMarking == StartPositionMarking.Tiled)
						map.MarkTiledStartPositions();

					if (_settings.MarkOreFields)
						map.MarkOreAndGems();

					if ((_settings.GeneratePreviewPack || _settings.FixupTiles || _settings.FixOverlays ||
						 _settings.CompressTiles) && _settings.Backup) {
						if (mapFile.BaseStream is MixFile)
							_logger.Error("Cannot generate a map file backup into an archive (.mmx/.yro/.mix)!");
						else {
							try {
								string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
								string fileInput = Path.Combine(Path.GetDirectoryName(_settings.InputFile),
									Path.GetFileName(_settings.InputFile));
								fileInput = fileInput.TrimEnd(Path.DirectorySeparatorChar,
									Path.AltDirectorySeparatorChar);
								string fileInputNoExtn = Path.Combine(Path.GetDirectoryName(_settings.InputFile),
									Path.GetFileNameWithoutExtension(_settings.InputFile));
								fileInputNoExtn = fileInputNoExtn.TrimEnd(Path.DirectorySeparatorChar,
									Path.AltDirectorySeparatorChar);
								string fileBackup = fileInputNoExtn + "_" + timestamp + ".bkp";
								File.Copy(fileInput, fileBackup, true);
								_logger.Info("Creating map backup: " + fileBackup);
							}
							catch (Exception) {
								_logger.Error("Unable to generate a map file backup!");
							}
						}
					}

					if (_settings.FixupTiles)
						map.FixupTileLayer();

					map.Draw();

					if (_settings.MarkIceGrowth)
						map.MarkIceGrowth();

					if (_settings.TunnelPaths)
						map.PlotTunnels(_settings.TunnelPosition);

					if (_settings.MarkStartPos && (_settings.StartPositionMarking == StartPositionMarking.Squared ||
												   _settings.StartPositionMarking == StartPositionMarking.Circled ||
												   _settings.StartPositionMarking == StartPositionMarking.Diamond ||
												   _settings.StartPositionMarking == StartPositionMarking.Ellipsed ||
												   _settings.StartPositionMarking == StartPositionMarking.Starred))
						map.DrawStartPositions();

					// always resolve the map's proper name (pkt/csf lookup for official maps);
					// it is logged as "Mapname found:" and exported via --meta-json
					try {
						resolvedName = DetermineMapName(mapFile, _settings.Engine, vfs);
					}
					catch (Exception exc) {
						_logger.Warn("Could not determine map name: {0}", exc.Message);
						resolvedName = Path.GetFileNameWithoutExtension(_settings.InputFile);
					}
					if (_settings.OutputFile == "")
						_settings.OutputFile = StripPlayersFromName(MakeValidFileName(resolvedName)).Replace("  ", " ");

					if (_settings.OutputDir == "")
						_settings.OutputDir = Path.GetDirectoryName(_settings.InputFile);

					if (PreviewWindow != null)
						PreviewWindow.Show(map);
				} // VFS resources can now be released

				// free up as much memory as possible before saving the large images
				Rectangle saveRect = map.GetSizePixels(_settings.SizeMode);
				DrawingSurface ds = map.GetDrawingSurface();
				saveRect.Intersect(new Rectangle(0, 0, ds.Width, ds.Height));
				// if we don't need this data anymore, we can try to save some memory
				if (!_settings.GeneratePreviewPack) {
					ds.FreeNonBitmap();
					map.FreeUseless();
					GC.Collect();
				}

				if (_settings.SaveJPEG)
					ds.SaveJPEG(Path.Combine(_settings.OutputDir, _settings.OutputFile + ".jpg"),
						_settings.JPEGCompression, saveRect);

				if (_settings.SavePNG)
					ds.SavePNG(Path.Combine(_settings.OutputDir, _settings.OutputFile + ".png"),
						_settings.PNGQuality, saveRect);

				Regex reThumb = new Regex(@"(\+|)?\((\d+),(\d+)\)");
				var match = reThumb.Match(_settings.ThumbnailConfig);
				if (match.Success) {
					Size dimensions = new Size(
							int.Parse(match.Groups[2].Captures[0].Value),
							int.Parse(match.Groups[3].Captures[0].Value));
					var cutRect = map.GetSizePixels(_settings.SizeMode);

					if (match.Groups[1].Captures[0].Value == "+") {
						// + means maintain aspect ratio

						if (dimensions.Width > 0 && dimensions.Height > 0) {
							float scaleHeight = (float)dimensions.Height / (float)cutRect.Height;
							float scaleWidth = (float)dimensions.Width / (float)cutRect.Width;
							float scale = Math.Min(scaleHeight, scaleWidth);
							dimensions.Width = Math.Max((int)(cutRect.Width * scale), 1);
							dimensions.Height = Math.Max((int)(cutRect.Height * scale), 1);
						}
						else {
							double aspectRatio = cutRect.Width / (double)cutRect.Height;
							if (dimensions.Width / (double)dimensions.Height > aspectRatio) {
								dimensions.Height = (int)(dimensions.Width / aspectRatio);
							}
							else {
								dimensions.Width = (int)(dimensions.Height * aspectRatio);
							}
						}
					}

					_logger.Info("Saving thumbnail with dimensions {0}x{1}", dimensions.Width, dimensions.Height);

					if (!_settings.SavePNGThumbnails) {
						ds.SaveThumb(dimensions, cutRect,
							Path.Combine(_settings.OutputDir, "thumb_" + _settings.OutputFile + ".jpg"));
					}
					else {
						ds.SaveThumb(dimensions, cutRect,
							Path.Combine(_settings.OutputDir, "thumb_" + _settings.OutputFile + ".png"), true);
					}
				}

				if (!string.IsNullOrEmpty(_settings.MetadataOutFile))
					WriteMetadataJson(_settings.MetadataOutFile, resolvedName, mapFile, map, saveRect);

				if (_settings.GeneratePreviewPack || _settings.FixupTiles || _settings.FixOverlays ||
					_settings.CompressTiles) {
					if (mapFile.BaseStream is MixFile)
						_logger.Error(
							"Cannot fix tile layer or inject thumbnail into an archive (.mmx/.yro/.mix)!");
					else {
						if (_settings.GeneratePreviewPack)
							map.GeneratePreviewPack(_settings.PreviewMarkers, _settings.SizeMode, mapFile,
								_settings.FixPreviewDimensions);

						if (_settings.FixOverlays)
							map.FixupOverlays(); // fixing is done earlier, it now creates overlay and its data pack

						// Keep this last in tiles manipulation
						if (_settings.CompressTiles)
							map.CompressIsoMapPack5();

						_logger.Info("Saving map to " + _settings.InputFile);
						mapFile.Save(_settings.InputFile);
					}
				}
			}
			catch (Exception exc) {
				_logger.Error(string.Format("An unknown fatal exception occurred: {0}", exc), exc);
#if DEBUG
				throw;
#endif
				return EngineResult.Exception;
			}
			return EngineResult.RenderedOk;
		}

		/// <summary>
		/// Resolves the CnCNet client's [INISystem]BasedOn map inheritance (used by e.g. DTA and
		/// TI for difficulty variants): the referenced base map, resolved from the same directory,
		/// is loaded first and the derived map's sections are merged on top of it.
		/// </summary>
		private VirtualFile ResolveIniInheritance(VirtualFile vmapFile, string mapDir) {
			var ini = new IniFile(vmapFile, vmapFile.FileName, 0, vmapFile.Length);
			if (ini.GetSection("INISystem")?.ReadString("BasedOn") is not { Length: > 0 }) {
				vmapFile.Position = 0;
				return vmapFile;
			}

			IniFile Resolve(IniFile derived, int depth) {
				string basedOn = derived.GetSection("INISystem")?.ReadString("BasedOn");
				if (string.IsNullOrEmpty(basedOn) || depth > 8)
					return derived;
				string basePath = Path.Combine(mapDir ?? "", basedOn);
				if (!File.Exists(basePath)) {
					_logger.Warn("Map is based on \"{0}\" but that file was not found next to it", basedOn);
					return derived;
				}
				_logger.Info("Merging map with its base map {0}", basedOn);
				var baseStream = File.Open(basePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var baseIni = new IniFile(baseStream, Path.GetFileName(basePath), 0, baseStream.Length);
				baseIni = Resolve(baseIni, depth + 1);
				baseIni.GetOrCreateSection("INISystem").Clear();
				baseIni.MergeWith(derived);
				return baseIni;
			}

			var merged = Resolve(ini, 0);
			var ms = new MemoryStream();
			merged.Save(ms);
			ms.Position = 0;
			return new VirtualFile(ms, vmapFile.FileName, 0, ms.Length, true);
		}

		private static void InitLoggerConfig() {
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
		private bool ValidateSettings() {
			if (_settings.ShowHelp) {
				ShowHelp();
				return false; // not really false :/
			}
			else if (!File.Exists(_settings.InputFile)) {
				_logger.Error("Specified input file does not exist");
				return false;
			}
			else if (!_settings.SaveJPEG && !_settings.SavePNG && !_settings.SavePNGThumbnails  &&
				!_settings.GeneratePreviewPack && !_settings.FixupTiles && !_settings.FixOverlays && !_settings.CompressTiles &&
				PreviewWindow == null) {
				_logger.Error("No action to perform. Either generate PNG/JPEG/Thumbnail or modify map.");
				return false;
			}
			else if (_settings.OutputDir != "" && !Directory.Exists(_settings.OutputDir)) {
				_logger.Error("Specified output directory does not exist.");
				return false;
			}
			return true;
		}

		private void ShowHelp() {
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write("Usage: ");
			Console.WriteLine("");
			Console.WriteLine(_settings.GetHelpText());
		}

		public static bool IsLinux {
			get {
				int p = (int)Environment.OSVersion.Platform;
				return (p == 4) || (p == 6) || (p == 128);
			}
		}

		/// <summary>
		/// Writes the authoritatively resolved map properties as JSON, for consumers
		/// like the web portal that would otherwise have to re-parse the map INI.
		/// </summary>
		private void WriteMetadataJson(string path, string resolvedName, MapFile mapFile, Map.Map map, Rectangle saveRect) {
			try {
				var basic = mapFile.GetSection("Basic");
				var meta = new {
					name = StripPlayersFromName(resolvedName).Replace("  ", " ").Trim(),
					rawName = resolvedName,
					basicName = basic?.ReadString("Name") ?? "",
					official = basic?.ReadBool("Official") ?? false,
					multiplayerOnly = basic?.ReadBool("MultiplayerOnly") ?? false,
					engine = _settings.Engine.ToString(),
					theater = map.TheaterType.ToString(),
					fullSize = new { x = mapFile.FullSize.X, y = mapFile.FullSize.Y, width = mapFile.FullSize.Width, height = mapFile.FullSize.Height },
					localSize = new { x = mapFile.LocalSize.X, y = mapFile.LocalSize.Y, width = mapFile.LocalSize.Width, height = mapFile.LocalSize.Height },
					startPositions = mapFile.Waypoints.Count(w => w.Number < 8),
					renderedWidth = saveRect.Width,
					renderedHeight = saveRect.Height,
				};
				File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(meta,
					new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
				_logger.Info("Wrote map metadata to {0}", path);
			}
			catch (Exception exc) {
				_logger.Error("Failed writing metadata JSON: {0}", exc.Message);
			}
		}

		/// <summary>Determines the map's proper name (raw, unsanitized).</summary>
		/// <returns>The resolved map name</returns>
		public string DetermineMapName(MapFile map, EngineType engine, VirtualFileSystem vfs) {
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(map.FileName);

			IniFile.IniSection basic = map.GetSection("Basic");
			if (basic == null)
				return fileNameWithoutExtension;
			if (basic.ReadBool("Official") == false)
				return basic.ReadString("Name", fileNameWithoutExtension);

			string mapExt = Path.GetExtension(_settings.InputFile);
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
				var mf = vfs.Open<MissionsFile>(missionsFile);
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
						var mix = new MixFile(File.Open(_settings.InputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
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
							pkt = vfs.Open<PktFile>("missions.pkt");
							break;
						case EngineType.Firestorm:
							pkt = vfs.Open<PktFile>("multi01.pkt");
							break;
						case EngineType.YurisRevenge:
							pkt = vfs.Open<PktFile>("missionsmd.pkt");
							break;
						default:
							throw new ArgumentOutOfRangeException("engine");
					}
				}


				// fallback for multiplayer maps with, .map extension,
				// no YR objects so assumed to be ra2, but actually meant to be used on yr
				if (mapExt == ".map" && pkt != null && !pkt.MapEntries.ContainsKey(pktEntryName) && engine >= EngineType.RedAlert2) {
					// this pkt (if any) comes from a private VFS over the map file itself,
					// so it can be disposed here; the pkt opened from the main vfs must NOT be
					// disposed as that would close the mix stream other files are read from
					using (var mapVfs = new VirtualFileSystem()) {
						pkt = null;
						if (mapVfs.AddItem(_settings.InputFile))
							pkt = mapVfs.OpenFile<PktFile>("missionsmd.pkt");
						if (pkt != null && !string.IsNullOrEmpty(pktEntryName))
							pktMapEntry = pkt.GetMapEntry(pktEntryName);
					}
				}
				else if (pkt != null && !string.IsNullOrEmpty(pktEntryName))
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
				var csf = vfs.Open<CsfFile>(csfFile);
				if (csf != null && csfEntryName != null)
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
