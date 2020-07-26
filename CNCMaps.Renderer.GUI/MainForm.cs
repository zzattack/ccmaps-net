using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using CNCMaps.Engine;
using CNCMaps.Engine.Map;
using CNCMaps.GUI.Properties;
using CNCMaps.Shared;
using Microsoft.Win32;
using NLog;
using NLog.Config;

namespace CNCMaps.GUI {

	public partial class MainForm : Form {
		public const string RendererExe = "CNCMaps.Renderer.exe";
		private readonly bool _skipUpdateCheck;
		// automatically swap between RA2/TS mix dir from registry
		private bool _currentEngineRa2 = true;


		public MainForm() {
			const string GuiConfig = "gui_settings.xml";
			string cfgPath;
			if (File.Exists(GuiConfig))
				cfgPath = GuiConfig;
			else {
				var localAppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CNCMaps");
				cfgPath = Path.Combine(localAppDir, GuiConfig);
			}
			Settings.Default.SettingsKey = cfgPath;
			InitializeComponent();


			ConfigurationItemFactory.Default.Targets.RegisterDefinition("GuiTarget", typeof(GuiTarget));
			if (LogManager.Configuration == null) {
				// init default config
				var target = new GuiTarget();
				target.TargetControl = this.rtbLog;
				target.Name = "rtbLogger";
				target.Layout = "${processtime:format=s\\.ffff} [${level}] ${message}";
				LogManager.Configuration = new LoggingConfiguration();
				LogManager.Configuration.AddTarget("gui", target);
				LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, target));
				LogManager.ReconfigExistingLoggers();
			}
		}

		public MainForm(bool skipUpdateCheck) : this() {
			_skipUpdateCheck = skipUpdateCheck;
		}

		private void MainFormLoad(object sender, EventArgs args) {
			if (Settings.Default.windowlocation != null) {
				int x = 40;
				int y = 40;
				string[] locXY = Settings.Default.windowlocation.Split(',');
				Screen[] displays = Screen.AllScreens;
				if (locXY.Length > 1 && locXY[0] != null && locXY[1] != null)
					if (!int.TryParse(locXY[0], out x) || !int.TryParse(locXY[1], out y) || x < 0 || y < 0 ||
						(displays != null && displays.Length == 1 && (x > Screen.PrimaryScreen.Bounds.Width || y > Screen.PrimaryScreen.Bounds.Height)))
						x = y = 40;
				this.Location = new System.Drawing.Point(x, y);
			}

			Text += " - v" + Assembly.GetEntryAssembly().GetName().Version;

			if (string.IsNullOrEmpty(tbMixDir.Text))
				tbMixDir.Text = FindMixDir(true);

			if (!_skipUpdateCheck && !Settings.Default.skipupdatecheck)
				PerformUpdateCheck();
			else
				UpdateStatus("not checking for newer version", 100);
			tbInput.Text = Settings.Default.input;
			tbMixDir.Text = Settings.Default.mixdir;
			cbOutputJPG.Checked = Settings.Default.outputjpg;
			nudEncodingQuality.Value = Settings.Default.outputjpgq;

			cbOutputPNG.Checked = Settings.Default.outputpng;
			nudCompression.Value = Settings.Default.outputpngq;

			cbOutputThumbnail.Checked = Settings.Default.outputthumb;
			tbThumbDimensions.Text = Settings.Default.thumbdimensions;
			cbPreserveThumbAspect.Checked = Settings.Default.thumbpreserveaspect;
			cbThumbPNG.Checked = Settings.Default.thumbpng;

			rbUseFilename.Checked = Settings.Default.usefilename;
			rbAutoFilename.Checked = Settings.Default.outputauto;
			rbCustomFilename.Checked = Settings.Default.outputcustom;
			tbCustomOutput.Text = Settings.Default.customfilename;

			rbEngineAuto.Checked = Settings.Default.engineauto;
			rbEngineFS.Checked = Settings.Default.enginefs;
			rbEngineRA2.Checked = Settings.Default.enginera2;
			rbEngineTS.Checked = Settings.Default.enginets;
			rbEngineYR.Checked = Settings.Default.engineyr;

			ckbModConfig.Checked = Settings.Default.modconfig;
			tbModConfig.Text = Settings.Default.modconfigfile;

			cbEmphasizeOre.Checked = Settings.Default.emphore;

			cbStartMarkers.Checked = Settings.Default.startmarker;
			cmbStartMarkers.Text = Settings.Default.startmarkertype;
			cbMarkerSize.Text = Settings.Default.startmarkersize;
			cbReplacePreview.Checked = Settings.Default.injectthumb;
			cbMarkersType.Text = Settings.Default.markers;

			rbSizeAuto.Checked = Settings.Default.autosize;
			rbSizeLocal.Checked = Settings.Default.localsize;
			rbSizeFullmap.Checked = Settings.Default.fullsize;

			cbMarkIceGrowth.Checked = Settings.Default.icegrowth;
			cbDiagnosticWindow.Checked = Settings.Default.diagwindow;
			ckbFixupTiles.Checked = Settings.Default.fixuptiles;
			cbBackup.Checked = Settings.Default.backup;
			cbFixOverlay.Checked = Settings.Default.fixoverlays;
			cbCompressTiles.Checked = Settings.Default.compresstiles;
			cbTunnelPaths.Checked = Settings.Default.tunnelpaths;
			cbTunnelPosition.Checked = Settings.Default.tunnelpos;

			tbBatchInput.Lines = Settings.Default.batchinput.Split('\n');

			ckbCheckForUpdates.Checked = !Settings.Default.skipupdatecheck;

			UpdateOptions();
		}

		private void MainFormClosing(object sender, FormClosingEventArgs e) {
			Settings.Default.input = tbInput.Text;
			Settings.Default.mixdir = tbMixDir.Text;
			Settings.Default.outputjpg = cbOutputJPG.Checked;
			Settings.Default.outputjpgq = nudEncodingQuality.Value;

			Settings.Default.outputpng = cbOutputPNG.Checked;
			Settings.Default.outputpngq = nudCompression.Value;

			Settings.Default.outputthumb = cbOutputThumbnail.Checked;
			Settings.Default.thumbdimensions = tbThumbDimensions.Text;
			Settings.Default.thumbpreserveaspect = cbPreserveThumbAspect.Checked;
			Settings.Default.thumbpng = cbThumbPNG.Checked;

			Settings.Default.usefilename = rbUseFilename.Checked;
			Settings.Default.outputauto = rbAutoFilename.Checked;
			Settings.Default.outputcustom = rbCustomFilename.Checked;
			Settings.Default.customfilename = tbCustomOutput.Text;

			Settings.Default.engineauto = rbEngineAuto.Checked;
			Settings.Default.enginefs = rbEngineFS.Checked;
			Settings.Default.enginera2 = rbEngineRA2.Checked;
			Settings.Default.enginets = rbEngineTS.Checked;
			Settings.Default.engineyr = rbEngineYR.Checked;

			Settings.Default.modconfig = ckbModConfig.Checked;
			Settings.Default.modconfigfile = tbModConfig.Text;

			Settings.Default.emphore = cbEmphasizeOre.Checked;

			Settings.Default.startmarker = cbStartMarkers.Checked;
			Settings.Default.startmarkertype = cmbStartMarkers.Text;
			Settings.Default.startmarkersize = cbMarkerSize.Text;
			Settings.Default.injectthumb = cbReplacePreview.Checked;
			Settings.Default.markers = cbMarkersType.Text;

			Settings.Default.autosize = rbSizeAuto.Checked;
			Settings.Default.localsize = rbSizeLocal.Checked;
			Settings.Default.fullsize = rbSizeFullmap.Checked;
			Settings.Default.icegrowth = cbMarkIceGrowth.Checked;
			Settings.Default.diagwindow = cbDiagnosticWindow.Checked;

			Settings.Default.fixuptiles = ckbFixupTiles.Checked;
			Settings.Default.backup = cbBackup.Checked;
			Settings.Default.fixoverlays = cbFixOverlay.Checked;
			Settings.Default.compresstiles = cbCompressTiles.Checked;
			Settings.Default.tunnelpaths = cbTunnelPaths.Checked;
			Settings.Default.tunnelpos = cbTunnelPosition.Checked;

			Settings.Default.batchinput = String.Join("\n", tbBatchInput.Lines);

			Settings.Default.skipupdatecheck = !ckbCheckForUpdates.Checked;

			if (WindowState != FormWindowState.Minimized)
				Settings.Default.windowlocation = this.Location.X + ", " +  this.Location.Y;

			Settings.Default.Save();
		}

		#region registry searching
		private string FindRenderProg() {
			if (File.Exists(Path.Combine(Environment.CurrentDirectory, RendererExe))) return RendererExe;
			else {
				if (!IsLinux) {
					try {
						using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
							return (string)key.OpenSubKey("SOFTWARE\\CNCMaps").GetValue("") + "\\" + RendererExe;
					}
					catch (NullReferenceException) {
					}
					catch (SecurityException) {
					}
				}
				return "";
			}
		}

		public static string FindMixDir(bool RA2) {
			if (IsLinux) // don't expect registry access..
				return Environment.CurrentDirectory;

			try {
				using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)) {
					var subkey = key.OpenSubKey("SOFTWARE\\Westwood\\" + (RA2 ? "Red Alert 2" : "Tiberian Sun"));
					if (subkey != null) return Path.GetDirectoryName((string)subkey.GetValue("InstallPath", string.Empty));
				}
			}
			catch (NullReferenceException) { } // no registry entry
			catch (ArgumentException) { } // invalid path

			// if current directory contains any mix files, try that
			if (Directory.GetFiles(Environment.CurrentDirectory, "*.mix").Any()) return Environment.CurrentDirectory;
			else return string.Empty;
		}

		public static bool IsLinux {
			get {
				int p = (int)Environment.OSVersion.Platform;
				return (p == 4) || (p == 6) || (p == 128);
			}
		}
		#endregion

		#region update checking/performing
		private void PerformUpdateCheck() {
			var uc = new UpdateChecker();
			uc.AlreadyLatest += (o, e) => UpdateStatus("already latest version", 100);
			uc.Connected += (o, e) => UpdateStatus("connected", 10);
			uc.DownloadProgressChanged += (o, e) => { /* care, xml is small anyway */ };
			uc.UpdateCheckFailed += (o, e) => UpdateStatus("update check failed", 100);
			uc.UpdateAvailable += (o, e) => {
				UpdateStatus("update available", 100);

				var dr =
					MessageBox.Show(
						string.Format(
							"An update to version {0} released on {1} is available. Release notes: \r\n\r\n{2}\r\n\r\nUpdate now?",
							e.Version.ToString(), e.ReleaseDate.ToShortDateString(), e.ReleaseNotes), "Update available",
						MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
				if (dr == DialogResult.Yes)
					DownloadAndUpdate(e.DownloadUrl);
			};
			uc.CheckVersion();
		}
		private void DownloadAndUpdate(string url) {
			UpdateStatus("downloading new program version", 0);
			var wc = new WebClient();
			wc.Proxy = null;

			var address = new Uri(UpdateChecker.UpdateCheckHost + url);
			wc.DownloadProgressChanged += (sender, args) => BeginInvoke((Action)delegate {
				UpdateStatus(string.Format("downloading, {0}%", args.ProgressPercentage * 95 / 100), args.ProgressPercentage * 95 / 100);
			});

			wc.DownloadDataCompleted += (sender, args) => {
				UpdateStatus("download complete, running installer", 100);
				string appPath = Path.GetTempPath();
				string dest = Path.Combine(appPath, "CNCMaps_update");

				int suffixNr = 0;
				while (File.Exists(dest + (suffixNr > 0 ? suffixNr.ToString() : "") + ".exe"))
					suffixNr++;

				dest += (suffixNr > 0 ? suffixNr.ToString() : "") + ".exe";
				File.WriteAllBytes(dest, args.Result);
				// invoke 
				var psi = new ProcessStartInfo(dest);
				psi.Arguments = "/Q";
				Process.Start(psi);
				Close();
			};

			// trigger it all
			wc.DownloadDataAsync(address);
		}


		#endregion

		#region ui events
		private void UIChanged(object sender, EventArgs e) {
			UpdateOptions();
		}
		private void UpdateStatus(string text, int progressBarValue) {
			var invokable = new Action(delegate {
				if (progressBarValue < pbProgress.Value && pbProgress.Value != 100) {
                    // probably re-initializing filesystem after map autodetect
                    return;
				}

				lblStatus.Text = "Status: " + text;
				if (progressBarValue < 100)
                    // forces 'instant update'
                    pbProgress.Value = progressBarValue + 1;
				pbProgress.Value = progressBarValue;
			});
			if (InvokeRequired)
				Invoke(invokable);
			else
				invokable();
		}
		private void OutputNameCheckedChanged(object sender, EventArgs e) {
			UpdateOptions();
		}

		private void cbModConfig_CheckedChanged(object sender, EventArgs e) {
			UpdateOptions();
		}

		private void BrowseInput(object sender, EventArgs e) {
			ofd.CheckFileExists = true;
			ofd.Multiselect = false;
			ofd.Filter = "RA2/TS map files (*.map, *.mpr, *.mmx, *.yrm, *.yro)|*.mpr;*.map;*.mmx;*.yrm;*.yro|All files (*.*)|*";

			ofd.FileName = "";
			if (ofd.ShowDialog() == DialogResult.OK) {
				tbInput.Text = ofd.FileName;
				ofd.InitialDirectory = Path.GetDirectoryName(ofd.FileName);
			}
		}
		private void BrowseMixDir(object sender, EventArgs e) {
			folderBrowserDialog.Description = "The directory that contains the mix files.";
			folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;
			folderBrowserDialog.SelectedPath = FindMixDir(rbEngineAuto.Checked || rbEngineRA2.Checked || rbEngineYR.Checked);
			folderBrowserDialog.ShowNewFolderButton = false;
			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
				tbMixDir.Text = folderBrowserDialog.SelectedPath;
		}
		private void InputDragEnter(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effect = DragDropEffects.Move;
		}
		private void InputDragDrop(object sender, DragEventArgs e) {
			var files = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (files.Length == 1 && tabControl.SelectedTab != tpBatch) {
				tbInput.Text = files[0];
				UpdateOptions();
				tabControl.SelectTab(tpMain);
			}
			else if (files.Length > 0) {
				tbBatchInput.Lines = new List<string>().Concat(tbBatchInput.Lines).Concat(files).ToArray();
				tabControl.SelectTab(tpBatch);
			}
		}

		private void RbEngineCheckedChanged(object sender, EventArgs e) {
			bool newEngineRA2 = rbEngineAuto.Checked || rbEngineRA2.Checked || rbEngineYR.Checked;
			if (_currentEngineRa2 && !newEngineRA2 && tbMixDir.Text == FindMixDir(true))
				tbMixDir.Text = FindMixDir(false);
			else if (!_currentEngineRa2 && newEngineRA2 && tbMixDir.Text == FindMixDir(false))
				tbMixDir.Text = FindMixDir(true);
			_currentEngineRa2 = newEngineRA2;
			UpdateOptions();
		}
		private void PngOutputCheckedChanged(object sender, EventArgs e) {
			UpdateOptions();
		}
		private void JpegOutputCheckedChanged(object sender, EventArgs e) {
			UpdateOptions();
		}
		private void cbStartMarkers_CheckedChanged(object sender, EventArgs e) {
			UpdateOptions();
		}
		private void cmbStartMarkers_SelectedIndexChanged(object sender, EventArgs e) {
			UpdateOptions();
		}
		private void cmbMarkerSize_SelectedIndexChanged(object sender, EventArgs e) {
			UpdateOptions();
		}
		private void CbReplacePreviewCheckedChanged(object sender, EventArgs e) {
			UpdateOptions();
		}
		private void BtnModEditorClick(object sender, EventArgs e) {
			var editor = new ModConfigEditor(tbModConfig.Text);
			if (editor.ShowDialog() == DialogResult.OK) {
				tbModConfig.Text = editor.ModConfigFile;
			}
		}
		private void CbOutputThumbnailCheckedChanged(object sender, EventArgs e) {
			UpdateOptions();
		}

		private void rbUseFilename_CheckedChanged(object sender, EventArgs e) {
			UpdateOptions();
		}

		private void cbMarkIceGrowth_CheckedChanged(object sender, EventArgs e) {
			UpdateOptions();
		}

		private void cbDiagnosticWindow_CheckedChanged(object sender, EventArgs e) {
			UpdateOptions();
		}

		private void cbBackup_CheckedChanged(object sender, EventArgs e) {
			UpdateOptions();
		}

		private void cbFixOverlay_CheckedChanged(object sender, EventArgs e) {
			UpdateOptions();
		}

		private void cbCompressTiles_CheckedChanged(object sender, EventArgs e) {
			UpdateOptions();
		}

		private void cbTunnelPaths_CheckedChanged(object sender, EventArgs e) {
			UpdateOptions();
			cbTunnelPosition.Enabled = cbTunnelPaths.Checked;
		}

		private void cbTunnelPosition_CheckedChanged(object sender, EventArgs e) {
			UpdateOptions();
		}

		private void btnClearLog_Click(object sender, EventArgs e) {
			rtbLog.Clear();
			rtbLog.Text = "";
			rtbLog.Update();
		}

		private void BtnAddMaps_Click(object sender, EventArgs e) {
			ofd.CheckFileExists = true;
			ofd.Multiselect = true;
			ofd.Filter = "RA2/TS map files (*.map, *.mpr, *.mmx, *.yrm, *.yro)|*.mpr;*.map;*.mmx;*.yrm;*.yro|All files (*.*)|*";

			ofd.FileName = "";
			if (ofd.ShowDialog() == DialogResult.OK) {
				tbBatchInput.Lines = new List<string>().Concat(tbBatchInput.Lines).Concat(ofd.FileNames).ToArray();
				ofd.InitialDirectory = Path.GetDirectoryName(ofd.FileName);
			}
		}

		private void BtnClearList_Click(object sender, EventArgs e) {
			tbBatchInput.Clear();
		}

		private void BtnBatchRender_Click(object sender, EventArgs e) {
			if (string.IsNullOrWhiteSpace(tbBatchInput.Text)) {
				UpdateStatus("no files provided in batch", 100);
				MessageBox.Show("Add at least one map file for processing!");
				return;
			}
			if (!ValidUIOptions())
				return;

			List<string> mapNames = new List<string>();
			HashSet<string> mapNamesSet = new HashSet<string>();
			List<string> errorMapNames = new List<string>();

			// Filter duplicates
			foreach (string filename in tbBatchInput.Lines) {
				if (!string.IsNullOrWhiteSpace(filename)) {
					filename.Trim();
					if (!mapNamesSet.Contains(filename)) {
						mapNames.Add(filename);
						mapNamesSet.Add(filename);
					}
				}
			}

			tabControl.SelectTab(tpLog);
			tpLog.Update();

			foreach (string mapname in mapNames)
				if (!ExecuteRenderer(mapname))
					errorMapNames.Add(mapname);

			if (errorMapNames.Count() > 0) {
				Log("Batch processing for the following map(s) failed: \r\n------------------------------");
				foreach (string mapFilename in errorMapNames)
					Log(mapFilename);
				Log("------------------------------\r\n");
			}
			else
				Log("Batch processing succeeded.\r\n------------------------------\r\n");
		}

		private void UpdateOptions() {
			nudCompression.Enabled = lblCompressionLevel.Enabled = cbOutputPNG.Checked;
			lblQuality.Enabled = nudEncodingQuality.Enabled = cbOutputJPG.Checked;
			tbThumbDimensions.Enabled = cbPreserveThumbAspect.Enabled = cbThumbPNG.Enabled = lblThumbSize.Enabled = cbOutputThumbnail.Checked;
			cbTunnelPosition.Enabled = cbTunnelPaths.Checked;
			tbCustomOutput.Enabled = rbCustomFilename.Checked;
			tbModConfig.Enabled = btnModEditor.Enabled = ckbModConfig.Checked;
			tbMixDir.Enabled = !ckbModConfig.Checked;

			string cmd = GetCommandLine();
			tbCommandPreview.Text = cmd;
		}
		private string GetCommandLine() {
			string cmd = string.Empty;

			cmd += "-i \"" + tbInput.Text + "\" ";
			if (cbOutputPNG.Checked) {
				cmd += "-p ";
				if (nudCompression.Value != 6)
					cmd += "-c " + nudCompression.Value.ToString(CultureInfo.InvariantCulture) + " ";
			}

			if (cbOutputJPG.Checked) {
				cmd += "-j ";
				if (nudEncodingQuality.Value != 90)
					cmd += "-q " + nudEncodingQuality.Value.ToString(CultureInfo.InvariantCulture) + " ";
			}

			if (rbUseFilename.Checked) cmd += "-o \"" + Path.GetFileNameWithoutExtension(tbInput.Text) + "\" ";
			else if (rbCustomFilename.Checked) cmd += "-o \"" + tbCustomOutput.Text + "\" ";

			if (ckbModConfig.Checked)
				cmd += "-M \"" + tbModConfig.Text + "\" ";
			else if (!string.IsNullOrWhiteSpace(tbMixDir.Text) && tbMixDir.Text != FindMixDir(rbEngineAuto.Checked || rbEngineRA2.Checked || rbEngineYR.Checked))
				cmd += "-m " + "\"" + tbMixDir.Text + "\" ";

			if (cbEmphasizeOre.Checked) cmd += "-r ";

			if (rbEngineRA2.Checked) cmd += "-y ";
			else if (rbEngineYR.Checked) cmd += "-Y ";
			else if (rbEngineTS.Checked) cmd += "-t ";
			else if (rbEngineFS.Checked) cmd += "-T ";

			if (rbSizeLocal.Checked) cmd += "-f ";
			else if (rbSizeFullmap.Checked) cmd += "-F ";

			//if (rbPreferSoftwareRendering.Checked) cmd += "-g ";
			//else if (rbPreferHardwareRendering.Checked) cmd += "-G ";

			if (cbStartMarkers.Checked)
				cmd += "--mark-start-pos ";
			if (cbStartMarkers.Checked || (cbReplacePreview.Checked && cbMarkersType.Text == "SelectedAsAbove")) {
				switch (cmbStartMarkers.Text) {
					case "Squared":
						cmd += "-S ";
						break;
					case "Circled":
						cmd += "--start-pos-circled ";
						break;
					case "Diamond":
						cmd += "--start-pos-diamond ";
						break;
					case "Ellipsed":
						cmd += "--start-pos-ellipsed ";
						break;
					case "Starred":
						cmd += "--start-pos-star ";
						break;
					case "Tiled":
						cmd += "-s ";
						break;
				}
				cmd += "--start-pos-size " + cbMarkerSize.Text + " ";
			}

			if (cbReplacePreview.Checked) {
				if (!cmd.EndsWith(" ")) cmd += " ";
				if (cbMarkersType.Text == "None")
					cmd += "--preview-markers-none ";
				else if (cbMarkersType.Text == "SelectedAsAbove")
					cmd += "--preview-markers-selected ";
				else if (cbMarkersType.Text == "Aro")
					cmd += "--preview-markers-aro ";
				else if (cbMarkersType.Text == "Bittah")
					cmd += "--preview-markers-bittah ";
			}

			if (cbOutputThumbnail.Checked) {
				var wh = tbThumbDimensions.Text.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
				int w, h;
				if (wh.Count == 2 && int.TryParse(wh[0], out w) && int.TryParse(wh[1], out h)) {
					cmd += "-z ";
					if (cbPreserveThumbAspect.Checked)
						cmd += "+";
					cmd += string.Format("({0},{1}) ", w, h);
				}
				if (cbThumbPNG.Checked) {
					cmd += "--thumb-png ";
				}
			}

			if (ckbFixupTiles.Checked) {
				cmd += "--fixup-tiles ";
			}

			if (cbMarkIceGrowth.Checked) {
				cmd += "--icegrowth ";
			}

			if (cbDiagnosticWindow.Checked) {
				cmd += "--diagwindow ";
			}

			if (cbBackup.Checked) {
				cmd += "--bkp ";
			}

			if (cbFixOverlay.Checked) {
				cmd += "--fix-overlays ";
			}

			if (cbCompressTiles.Checked) {
				cmd += "--cmprs-tiles ";
			}

			if (cbTunnelPaths.Checked) {
				cmd += "--tunnels ";
				if (cbTunnelPosition.Checked) {
					cmd += "--tunnelpos ";
				}
			}

			return cmd;
		}

		private RenderSettings GetRenderSettings(string inFilename) {
			var rs = new RenderSettings();
			rs.InputFile = inFilename;

			rs.SavePNG = cbOutputPNG.Checked;
			if (cbOutputPNG.Checked)
				rs.PNGQuality = (int)nudCompression.Value;

			rs.SaveJPEG = cbOutputJPG.Checked;
			if (cbOutputJPG.Checked)
				rs.JPEGCompression = (int)nudEncodingQuality.Value;

			if (rbUseFilename.Checked) rs.OutputFile = Path.GetFileNameWithoutExtension(rs.InputFile);
			else if (rbCustomFilename.Checked) rs.OutputFile = tbCustomOutput.Text;

			if (ckbModConfig.Checked)
				rs.ModConfig = tbModConfig.Text;
			else if (!string.IsNullOrWhiteSpace(tbMixDir.Text) && tbMixDir.Text != FindMixDir(rbEngineAuto.Checked || rbEngineRA2.Checked || rbEngineYR.Checked))
				rs.MixFilesDirectory = tbMixDir.Text;

			rs.MarkOreFields = cbEmphasizeOre.Checked;

			if (rbEngineRA2.Checked) rs.Engine = EngineType.RedAlert2;
			else if (rbEngineYR.Checked) rs.Engine = EngineType.YurisRevenge;
			else if (rbEngineTS.Checked) rs.Engine = EngineType.TiberianSun;
			else if (rbEngineFS.Checked) rs.Engine = EngineType.Firestorm;

			if (rbSizeLocal.Checked) rs.SizeMode = SizeMode.Local;
			else if (rbSizeFullmap.Checked) rs.SizeMode = SizeMode.Full;

			rs.MarkIceGrowth = cbMarkIceGrowth.Checked;
			rs.DiagnosticWindow = cbDiagnosticWindow.Checked;

			if (cbStartMarkers.Checked) rs.MarkStartPos = true;
			if (cbStartMarkers.Checked || (cbReplacePreview.Checked && cbMarkersType.Text == "SelectedAsAbove")) {
				switch (cmbStartMarkers.Text) {
					case "None":
						rs.StartPositionMarking = StartPositionMarking.None;
						break;
					case "Squared":
						rs.StartPositionMarking = StartPositionMarking.Squared;
						break;
					case "Circled":
						rs.StartPositionMarking = StartPositionMarking.Circled;
						break;
					case "Diamond":
						rs.StartPositionMarking = StartPositionMarking.Diamond;
						break;
					case "Ellipsed":
						rs.StartPositionMarking = StartPositionMarking.Ellipsed;
						break;
					case "Starred":
						rs.StartPositionMarking = StartPositionMarking.Starred;
						break;
					case "Tiled":
						rs.StartPositionMarking = StartPositionMarking.Tiled;
						break;
				}

				if (double.TryParse(cbMarkerSize.Text, NumberStyles.Number, CultureInfo.InvariantCulture,
					out double markerSize))
					rs.MarkerStartSize = Math.Abs(markerSize);
				else
					rs.MarkerStartSize = 4.0;
			}

			if (cbReplacePreview.Checked) {
				rs.GeneratePreviewPack = true;
				if (cbMarkersType.Text == "None")
					rs.PreviewMarkers = PreviewMarkersType.None;
				else if (cbMarkersType.Text == "SelectedAsAbove")
					rs.PreviewMarkers = PreviewMarkersType.SelectedAsAbove;
				else if (cbMarkersType.Text == "Aro")
					rs.PreviewMarkers = PreviewMarkersType.Aro;
				else if (cbMarkersType.Text == "Bittah")
					rs.PreviewMarkers = PreviewMarkersType.Bittah;
			}

			if (cbOutputThumbnail.Checked) {
				var wh = tbThumbDimensions.Text.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
				int w, h;
				if (wh.Count == 2 && int.TryParse(wh[0], out w) && int.TryParse(wh[1], out h)) {
					rs.ThumbnailConfig = "";
					if (cbPreserveThumbAspect.Checked)
						rs.ThumbnailConfig += "+";
					rs.ThumbnailConfig += string.Format("({0},{1})", w, h);
				}
				rs.SavePNGThumbnails = cbThumbPNG.Checked;
			}

			rs.FixupTiles = ckbFixupTiles.Checked;
			rs.Backup = cbBackup.Checked;
			rs.FixOverlays = cbFixOverlay.Checked;
			rs.CompressTiles = cbCompressTiles.Checked;
			rs.TunnelPaths = cbTunnelPaths.Checked;
			rs.TunnelPosition = cbTunnelPosition.Checked;

			return rs;
		}
		#endregion

		#region renderer program execution
		private void ExecuteCommand(object sender, EventArgs e) {
			if (File.Exists(tbInput.Text) == false) {
				UpdateStatus("aborted, no input file", 100);
				MessageBox.Show("Input file doesn't exist. Aborting.");
				return;
			}
			if (!ValidUIOptions())
				return;

			tabControl.SelectTab(tpLog);
			tpLog.Update();
			ExecuteRenderer(tbInput.Text);
		}

		private bool ValidUIOptions() {
			if (!cbOutputPNG.Checked && !cbOutputJPG.Checked && !cbOutputThumbnail.Checked &&
				!cbReplacePreview.Checked && !ckbFixupTiles.Checked && !cbFixOverlay.Checked && !cbCompressTiles.Checked &&
				!cbDiagnosticWindow.Checked) {
				UpdateStatus("aborted, no processing", 100);
				MessageBox.Show("Either generate PNG/JPEG/Thumbnail or modify map or use preview window.", "Nothing to do..", MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return false;
			}
			else
				return true;
		}

		private bool ExecuteRenderer(string inputFilename) {
			var engineCfg = GetRenderSettings(inputFilename);
			bool success = false;

			try {
				var engine = new RenderEngine();
				engine.ConfigureFromSettings(engineCfg);
				var result = engine.Execute();

				switch (result) {
					case EngineResult.Exception:
						Log("\r\nUnknown exception.\r\n");
						AskBugReport(null);
						break;
					case EngineResult.RenderedOk:
						Log("\r\nSpecified action(s) completed.\r\n------------------------------\r\n");
						// +" Please send an email to frank@zzattack.org with your map as an attachment.");
						success = true;
						break;
					case EngineResult.LoadTheaterFailed:
						Log("\r\nTheater loading failed. Please make sure the mix directory is correct and that the required expansion packs are installed "
							+ "if they are required for the map you want to render.\r\n");
						AskBugReport(null);
						break;
					case EngineResult.LoadRulesFailed:
						Log("\r\nRules loading failed. Please make sure the mix directory is correct and that the required expansion packs are installed "
							+ "if they are required for the map you want to render.\r\n");
						AskBugReport(null);
						break;
				}
			}
			catch (Exception exc) {
				AskBugReport(exc);
			}
			return success;
		}
		private void AskBugReport(Exception exc) {
			// seems like rendering failed!
			Log("\r\nIt appears an error occurred during image rendering.");
			var form = new SubmitBug();
			form.Email = Settings.Default.email;
			if (form.ShowDialog() == DialogResult.OK) {
				if (!string.IsNullOrWhiteSpace(form.Email))
					Settings.Default.email = form.Email;
				SubmitBugReport(form.Email, exc);
			}
		}

		private void SubmitBugReport(string email, Exception exc) {
			try {
				const string url = UpdateChecker.UpdateCheckHost + "tool/report_bug";
				WebClient wc = new WebClient();
				wc.Proxy = null;
				var data = new NameValueCollection();
				data.Set("renderer_version", typeof(Map).Assembly.GetName().Version.ToString());
				data.Set("exception", exc == null ? "" : exc.ToString());
				data.Set("input_map", File.ReadAllText(tbInput.Text));  // batch passes filename instead of UI main setting input
				data.Set("input_name", Path.GetFileName(tbInput.Text));
				data.Set("commandline", GetCommandLine());
				data.Set("log_text", rtbLog.Text);
				data.Set("email", email);

				wc.OpenWriteCompleted += (o, args) => UpdateStatus("sending bug report.. connected", 15);
				wc.UploadProgressChanged += (o, args) => {
					double pct = 15 + Math.Round(85.0 * (args.TotalBytesToSend / args.BytesSent) / 100.0, 0);
					UpdateStatus("sending bug report.. uploading " + pct + "%", (int)pct);
				};
				wc.UploadValuesCompleted += (o, args) => {
					if (args.Cancelled || args.Error != null)
						BugReportFailed();
					else
						UpdateStatus("bug report sent", 100);
				};

				wc.UploadValuesAsync(new Uri(url), "POST", data);
				UpdateStatus("sending bug report.. ", 5);
			}
			catch {
				BugReportFailed();
			}
		}

		private void BugReportFailed() {
			Log("Submitting bug report failed. Please send a manual bug report to frank@zzattack.org including your map, settings and error log");
			UpdateStatus("bug report failed", 100);
		}

		#endregion

		#region Logging
		private delegate void LogDelegate(string s);

		private string _outputName; // filename of saved jpg
		private void Log(string s) {
			if (InvokeRequired) {
				Invoke(new LogDelegate(Log), s);
				return;
			}

			if (s.Contains("Saving ")) {
				_outputName = s;
				int sIdx = s.IndexOf(" to ") + 4;
				int endIdx = s.IndexOf(", quality");
				if (endIdx == -1) endIdx = s.IndexOf(", compression");
				if (endIdx == -1) return;
				string file = s.Substring(sIdx, endIdx - sIdx);
				rtbLog.AppendText(s.Substring(0, sIdx));
				rtbLog.AppendText("file:///" + Uri.EscapeUriString(file));
				rtbLog.AppendText(s.Substring(endIdx));
			}
			else {
				rtbLog.Text += s + "\r\n";
			}
			rtbLog.SelectionStart = rtbLog.TextLength - 1;
			rtbLog.SelectionLength = 1;
			rtbLog.ScrollToCaret();

			var progressEntry = _progressIndicators.FirstOrDefault(kvp => s.Contains(kvp.Value));
			if (!progressEntry.Equals(default(KeyValuePair<int, string>))) {
				if (s.Contains("Saving")) {
					UpdateStatus("saving: " + progressEntry.Key + "%", progressEntry.Key);
				}
				else {
					UpdateStatus("preparing: " + progressEntry.Key + "%", progressEntry.Key);
				}
			}
			// tiles and objects is progress 27%-90%, program automatically
			// determines ratios for tiles/objects
			if (s.Contains("Drawing tiles") || s.Contains("Drawing objects")) {
				int idx = s.LastIndexOf(" ") + 1;
				double pct = Math.Round(27 + (90.0 - 27.0) * int.Parse(s.Substring(idx, s.Length - idx - 1)) / 100.0, 0);
				UpdateStatus("rendering, " + pct + "%", (int)pct);
			}
		}
		private readonly Dictionary<int, string> _progressIndicators = new Dictionary<int, string>() {
			{5, "Initializing filesystem"},
			{8, "Reading tiles"},
			{10, "Parsing rules.ini"},
			{12, "Parsing art.ini"},
			{14, "Loading houses"},
			{16, "Loading lighting"},
			{18, "Creating per-height palettes"},
			{20, "Loading light sources"},
			{22, "Calculating palette-values for all objects"},
			{90, "Map drawing completed"},
			{92, "Saving"},

		};
		private void rtbLog_LinkClicked(object sender, LinkClickedEventArgs e) {
			Process.Start(e.LinkText);
		}

		#endregion

		private void btnCheckForUpdate_Click(object sender, EventArgs e) {
			PerformUpdateCheck();
		}

		private void lblCopyright_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
			Process.Start(lblCopyright.Text.Substring(e.Link.Start, e.Link.Length));
			lblCopyright.LinkVisited = true;
		}
	}
}
