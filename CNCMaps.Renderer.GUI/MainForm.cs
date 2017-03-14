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
using CNCMaps.FileFormats.VirtualFileSystem;
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
				var target =  new GuiTarget();
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
			Text += " - v" + Assembly.GetEntryAssembly().GetName().Version;

			if (string.IsNullOrEmpty(tbMixDir.Text))
				tbMixDir.Text = FindMixDir(true);

			if (!_skipUpdateCheck)
				PerformUpdateCheck();
			else
				UpdateStatus("not checking for newer version", 100);

			rbAutoFilename.Checked = Settings.Default.outputauto;
			rbCustomFilename.Checked = Settings.Default.outputcustom;
			rbEngineAuto.Checked = Settings.Default.engineauto;
			rbEngineFS.Checked = Settings.Default.enginefs;
			rbEngineRA2.Checked = Settings.Default.enginera2;
			rbEngineTS.Checked = Settings.Default.enginets;
			rbEngineYR.Checked = Settings.Default.engineyr;
			rbPreferHardwareRendering.Checked = Settings.Default.hwvoxels;
			rbPreferSoftwareRendering.Checked = Settings.Default.swvoxels;
			rbSizeFullmap.Checked = Settings.Default.fullsize;
			rbSizeLocal.Checked = Settings.Default.localsize;
			rbSizeAuto.Checked = Settings.Default.autosize;

			cbEmphasizeOre.Checked = Settings.Default.emphore;
			cbOmitSquareMarkers.Checked = Settings.Default.omitsquarespreview;
            cbMarkersType.Text = Settings.Default.markers;
			cbSquaredStartPositions.Checked = Settings.Default.squaredpos;
			cbTiledStartPositions.Checked = Settings.Default.tiledpos;
			cbOutputJPG.Checked = Settings.Default.outputjpg;
			cbOutputPNG.Checked = Settings.Default.outputpng;
			cbOutputThumbnail.Checked = Settings.Default.outputthumb;
			cbReplacePreview.Checked = Settings.Default.injectthumb;
			cbPreserveThumbAspect.Checked = Settings.Default.thumbpreserveaspect;
			tbCustomOutput.Text = Settings.Default.customfilename;
			cbModConfig.Checked = Settings.Default.modconfig;
			tbModConfig.Text = Settings.Default.modconfigfile;

			UpdateCommandline();
		}

		private void MainFormClosing(object sender, FormClosingEventArgs e) {
            Settings.Default.input = tbInput.Text;
            Settings.Default.mixdir = tbMixDir.Text;
            Settings.Default.outputauto = rbAutoFilename.Checked;
			Settings.Default.outputcustom = rbCustomFilename.Checked;
			Settings.Default.engineauto = rbEngineAuto.Checked;
			Settings.Default.enginefs = rbEngineFS.Checked;
			Settings.Default.enginera2 = rbEngineRA2.Checked;
			Settings.Default.enginets = rbEngineTS.Checked;
			Settings.Default.engineyr = rbEngineYR.Checked;
			Settings.Default.hwvoxels = rbPreferHardwareRendering.Checked; ;
			Settings.Default.swvoxels = rbPreferSoftwareRendering.Checked; ;
			Settings.Default.fullsize = rbSizeFullmap.Checked; ;
			Settings.Default.localsize = rbSizeLocal.Checked;
			Settings.Default.autosize = rbSizeAuto.Checked;
            Settings.Default.markers = cbMarkersType.Text;
			Settings.Default.emphore = cbEmphasizeOre.Checked;
			Settings.Default.omitsquarespreview = cbOmitSquareMarkers.Checked;
			Settings.Default.squaredpos = cbSquaredStartPositions.Checked;
			Settings.Default.tiledpos = cbTiledStartPositions.Checked;
			Settings.Default.outputjpg = cbOutputJPG.Checked;
			Settings.Default.outputpng = cbOutputPNG.Checked;
			Settings.Default.outputthumb = cbOutputThumbnail.Checked;
			Settings.Default.thumbpreserveaspect = cbPreserveThumbAspect.Checked;
			Settings.Default.injectthumb = cbReplacePreview.Checked;
			Settings.Default.customfilename = tbCustomOutput.Text;
			Settings.Default.modconfig = cbModConfig.Checked;
			Settings.Default.modconfigfile = tbModConfig.Text;
			Settings.Default.Save();
		}

		#region registry searching
		private string FindRenderProg() {
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
			return File.Exists(Path.Combine(Environment.CurrentDirectory, RendererExe)) ? RendererExe : "";
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
			UpdateCommandline();
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
			tbCustomOutput.Visible = rbCustomFilename.Checked;
			UpdateCommandline();
		}

		private void cbModConfig_CheckedChanged(object sender, EventArgs e) {
			tbModConfig.Visible = btnModEditor.Visible = cbModConfig.Checked;
			tbMixDir.Enabled = !cbModConfig.Checked;
			UpdateCommandline();
		}

		private void BrowseInput(object sender, EventArgs e) {
			ofd.CheckFileExists = true;
			ofd.Multiselect = false;
			ofd.Filter = "RA2/TS map files (*.map, *.mpr, *.mmx, *.yrm, *.yro)|*.mpr;*.map;*.mmx;*.yrm;*.yro|All files (*.*)|*";
			if (string.IsNullOrEmpty(ofd.InitialDirectory))
				ofd.InitialDirectory = FindMixDir(rbEngineAuto.Checked || rbEngineRA2.Checked || rbEngineYR.Checked);

			ofd.FileName = "";
			if (ofd.ShowDialog() == DialogResult.OK) {
				tbInput.Text = ofd.FileName;
				ofd.InitialDirectory = Path.GetDirectoryName(ofd.FileName);
			}
		}
		private void BrowseMixDir(object sender, EventArgs e) {
			folderBrowserDialog1.Description = "The directory that contains the mix files.";
			folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
			folderBrowserDialog1.SelectedPath = FindMixDir(rbEngineAuto.Checked || rbEngineRA2.Checked || rbEngineYR.Checked);
			folderBrowserDialog1.ShowNewFolderButton = false;
			if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
				tbMixDir.Text = folderBrowserDialog1.SelectedPath;
		}
		private void InputDragEnter(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effect = DragDropEffects.Move;
		}
		private void InputDragDrop(object sender, DragEventArgs e) {
			var files = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (files.Length > 0) {
				tbInput.Text = files[0];
				UpdateCommandline();
			}
		}

		private void RbEngineCheckedChanged(object sender, EventArgs e) {
			bool newEngineRA2 = rbEngineAuto.Checked || rbEngineRA2.Checked || rbEngineYR.Checked;
			if (_currentEngineRa2 && !newEngineRA2 && tbMixDir.Text == FindMixDir(true))
				tbMixDir.Text = FindMixDir(false);
			else if (!_currentEngineRa2 && newEngineRA2 && tbMixDir.Text == FindMixDir(false))
				tbMixDir.Text = FindMixDir(true);
			_currentEngineRa2 = newEngineRA2;
			UpdateCommandline();
		}
		private void PngOutputCheckedChanged(object sender, EventArgs e) {
			nudCompression.Visible = lblCompressionLevel.Visible = cbOutputPNG.Checked;
			UpdateCommandline();
		}
		private void JpegOutputCheckedChanged(object sender, EventArgs e) {
			lblQuality.Visible = nudEncodingQuality.Visible = cbOutputJPG.Checked;
			UpdateCommandline();
		}
		private void SquaredStartPosCheckedChanged(object sender, EventArgs e) {
			if (cbSquaredStartPositions.Checked)
				cbTiledStartPositions.Checked = false;
			UpdateCommandline();
		}
		private void TiledStartPosCheckedChanged(object sender, EventArgs e) {
			if (cbTiledStartPositions.Checked)
				cbSquaredStartPositions.Checked = false;
			UpdateCommandline();
		}
		private void CbReplacePreviewCheckedChanged(object sender, EventArgs e) {
			cbOmitSquareMarkers.Visible = cbReplacePreview.Checked;
			UpdateCommandline();
		}
		private void BtnModEditorClick(object sender, EventArgs e) {
			var editor = new ModConfigEditor(tbModConfig.Text);
			if (editor.ShowDialog() == DialogResult.OK) {
				tbModConfig.Text = editor.ModConfigFile;
			}
		}
		private void CbOutputThumbnailCheckedChanged(object sender, EventArgs e) {
			tbThumbDimensions.Visible = cbPreserveThumbAspect.Visible =
				lblMarkersType.Visible = cbMarkersType.Visible = cbOutputThumbnail.Checked;
			UpdateCommandline();
		}

		private void UpdateCommandline() {
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

			if (rbCustomFilename.Checked) cmd += "-o \"" + tbCustomOutput.Text + "\" ";
			if (cbOutputJPG.Checked) {
				cmd += "-j ";
				if (nudEncodingQuality.Value != 90)
					cmd += "-q " + nudEncodingQuality.Value.ToString(CultureInfo.InvariantCulture) + " ";
			}

			if (cbModConfig.Checked)
				cmd += "-M \"" + tbModConfig.Text + "\" ";
			else if (!string.IsNullOrWhiteSpace(tbMixDir.Text) && tbMixDir.Text != FindMixDir(rbEngineAuto.Checked || rbEngineRA2.Checked || rbEngineYR.Checked))
				cmd += "-m " + "\"" + tbMixDir.Text + "\" ";

			if (cbEmphasizeOre.Checked) cmd += "-r ";
			if (cbTiledStartPositions.Checked) cmd += "-s ";
			if (cbSquaredStartPositions.Checked) cmd += "-S ";

			if (rbEngineRA2.Checked) cmd += "-y ";
			else if (rbEngineYR.Checked) cmd += "-Y ";
			else if (rbEngineTS.Checked) cmd += "-t ";
			else if (rbEngineFS.Checked) cmd += "-T ";

			if (rbSizeLocal.Checked) cmd += "-f ";
			else if (rbSizeFullmap.Checked) cmd += "-F ";

			//if (rbPreferSoftwareRendering.Checked) cmd += "-g ";
			//else if (rbPreferHardwareRendering.Checked) cmd += "-G ";

			if (cbReplacePreview.Checked)
				cmd += cbOmitSquareMarkers.Checked ? "-K" : "-k ";

			if (cbOutputThumbnail.Checked) {
				var wh = tbThumbDimensions.Text.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
				int w, h;
				if (wh.Count == 2 && int.TryParse(wh[0], out w) && int.TryParse(wh[1], out h)) {
					cmd += "-z ";
					if (cbPreserveThumbAspect.Checked)
						cmd += "+";
					cmd += string.Format("({0},{1})", w, h);
				}
				if (!cmd.EndsWith(" ")) cmd += " ";
				if (cbMarkersType.Text == "None")
					cmd += "--preview-markers-none ";
				else if (cbMarkersType.Text == "Squared")
					cmd += "--preview-markers-squared ";
				else if (cbMarkersType.Text == "Aro")
					cmd += "--preview-markers-aro ";
				else if (cbMarkersType.Text == "Bittah")
					cmd += "--preview-markers-bittah ";
			}

			return cmd;
		}
		
		private RenderSettings GetRenderSettings() {
			var rs = new RenderSettings();
			rs.InputFile = tbInput.Text;
			if (cbOutputPNG.Checked) {
				rs.SavePNG=true;
				rs.PNGQuality = (int)nudCompression.Value;
			}

			if (rbCustomFilename.Checked) rs.OutputFile = tbCustomOutput.Text;
			if (cbOutputJPG.Checked) {
				rs.SaveJPEG=true;
				rs.JPEGCompression= (int)nudEncodingQuality.Value;
			}

			if (cbModConfig.Checked)
				rs.ModConfig = tbModConfig.Text;
			else if (!string.IsNullOrWhiteSpace(tbMixDir.Text) && tbMixDir.Text != FindMixDir(rbEngineAuto.Checked || rbEngineRA2.Checked || rbEngineYR.Checked))
				rs.MixFilesDirectory = tbMixDir.Text;

			if (cbEmphasizeOre.Checked) rs.MarkOreFields = true;
			if (cbTiledStartPositions.Checked) rs.StartPositionMarking = StartPositionMarking.Tiled;
			if (cbSquaredStartPositions.Checked) rs.StartPositionMarking = StartPositionMarking.Squared;

			if (rbEngineRA2.Checked) rs.Engine = EngineType.RedAlert2;
			else if (rbEngineYR.Checked) rs.Engine = EngineType.YurisRevenge;
			else if (rbEngineTS.Checked) rs.Engine = EngineType.TiberianSun;
			else if (rbEngineFS.Checked) rs.Engine = EngineType.Firestorm;

			if (rbSizeLocal.Checked) rs.SizeMode = SizeMode.Local;
			else if (rbSizeFullmap.Checked) rs.SizeMode = SizeMode.Full;

			if (rbPreferSoftwareRendering.Checked) rs.PreferOSMesa = true;

			if (cbReplacePreview.Checked) {
				rs.GeneratePreviewPack = true;
				if (cbMarkersType.Text == "None")
					rs.PreviewMarkers = PreviewMarkersType.None;
				else if (cbMarkersType.Text == "Squared")
					rs.PreviewMarkers = PreviewMarkersType.Squared;
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
			}

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
			
			if (!cbOutputPNG.Checked && !cbOutputJPG.Checked && !cbReplacePreview.Checked) {
				UpdateStatus("aborted, no output format picked", 100);
				MessageBox.Show("Either PNG, JPEG or Replace Preview must be checked.", "Nothing to do..", MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return;
			}
			tabControl.SelectTab(tpLog);
			ExecuteRenderer();
		}

		private void ExecuteRenderer() {
			var engineCfg = GetRenderSettings();
			try {
				VFS.Reset();
				var engine = new EngineSettings();
				engine.ConfigureFromSettings(engineCfg);
				var result = engine.Execute();

				switch (result) {
				case EngineResult.Exception:
					Log("\r\nUnknown exception.");
					AskBugReport(null);
					break;
				case EngineResult.RenderedOk:
					// indicates EOF
					Log("\r\nYour map has been rendered. If your image did not appear, something went wrong." +
						" Please send an email to frank@zzattack.org with your map as an attachment.");
					break;
				case EngineResult.LoadTheaterFailed:
						Log("\r\nTheater loading failed. Please make sure the mix directory is correct and that the required expansion packs are installed "
							+ "if they are required for the map you want to render.");
                        AskBugReport(null);
                    break;
				case EngineResult.LoadRulesFailed:
					Log("\r\nRules loading failed. Please make sure the mix directory is correct and that the required expansion packs are installed "
						+ "if they are required for the map you want to render.");
					AskBugReport(null);
					break;
				}
			}
			catch (Exception exc) {
				AskBugReport(exc);
			}
		}
		private void AskBugReport(Exception exc) {
			// seems like rendering failed!
			Log("\r\nIt appears an error ocurred during image rendering.");
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
				data.Set("input_map", File.ReadAllText(tbInput.Text));
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

		private string outputName; // filename of saved jpg
		private void Log(string s) {
			if (InvokeRequired) {
				Invoke(new LogDelegate(Log), s);
				return;
			}

			if (s.Contains("Saving ")) {
				outputName = s;
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


	}
}