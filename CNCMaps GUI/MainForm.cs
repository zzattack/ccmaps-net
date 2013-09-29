using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CNCMaps.GUI {

	public partial class MainForm : Form {
		public const string RendererExe = "CNCMaps.exe";
		private readonly bool _skipUpdateCheck;
		// automatically swap between RA2/TS mix dir from registry
		private bool _currentEngineRa2 = true;


		public MainForm() {
			InitializeComponent();
		}
		public MainForm(bool skipUpdateCheck)
			: this() {
			_skipUpdateCheck = skipUpdateCheck;
		}

		private void MainFormLoad(object sender, EventArgs args) {
			Text += " - v" + Assembly.GetEntryAssembly().GetName().Version;

			if (string.IsNullOrEmpty(tbRenderProg.Text))
				tbRenderProg.Text = FindRenderProg();

			if (string.IsNullOrEmpty(tbMixDir.Text))
				tbMixDir.Text = FindMixDir(true);

			if (!_skipUpdateCheck)
				PerformUpdateCheck();
			else
				UpdateStatus("not checking for newer version", 100);

			rbAutoFilename.Checked = Properties.Settings.Default.outputauto;
			rbCustomFilename.Checked = Properties.Settings.Default.outputcustom;
			rbEngineAuto.Checked = Properties.Settings.Default.engineauto;
			rbEngineFS.Checked = Properties.Settings.Default.enginefs;
			rbEngineRA2.Checked = Properties.Settings.Default.enginera2;
			rbEngineTS.Checked = Properties.Settings.Default.enginets;
			rbEngineYR.Checked = Properties.Settings.Default.engineyr;
			rbPreferHardwareRendering.Checked = Properties.Settings.Default.hwvoxels;
			rbPreferSoftwareRendering.Checked = Properties.Settings.Default.swvoxels;
			rbSizeFullmap.Checked = Properties.Settings.Default.fullsize;
			rbSizeLocal.Checked = Properties.Settings.Default.localsize;
			rbSizeAuto.Checked = Properties.Settings.Default.autosize;

			cbEmphasizeOre.Checked = Properties.Settings.Default.emphore;
			cbModConfig.Checked = Properties.Settings.Default.modconfig;
			cbOmitSquareMarkers.Checked = Properties.Settings.Default.omitsquarespreview;
			cbSquaredStartPositions.Checked = Properties.Settings.Default.squaredpos;
			cbTiledStartPositions.Checked = Properties.Settings.Default.tiledpos;
			cbOutputJPG.Checked = Properties.Settings.Default.outputjpg;
			cbOutputPNG.Checked = Properties.Settings.Default.outputpng;
			cbOutputThumbnail.Checked = Properties.Settings.Default.outputthumb;
			cbReplacePreview.Checked = Properties.Settings.Default.injectthumb;
			cbPreserveThumbAspect.Checked = Properties.Settings.Default.thumbpreserveaspect;

			UpdateCommandline();
		}

		private void MainFormClosing(object sender, FormClosingEventArgs e) {
			Properties.Settings.Default.outputauto = rbAutoFilename.Checked;
			Properties.Settings.Default.outputcustom = rbCustomFilename.Checked;
			Properties.Settings.Default.engineauto = rbEngineAuto.Checked;
			Properties.Settings.Default.enginefs = rbEngineFS.Checked;
			Properties.Settings.Default.enginera2 = rbEngineRA2.Checked;
			Properties.Settings.Default.enginets = rbEngineTS.Checked;
			Properties.Settings.Default.engineyr = rbEngineYR.Checked;
			Properties.Settings.Default.hwvoxels = rbPreferHardwareRendering.Checked; ;
			Properties.Settings.Default.swvoxels = rbPreferSoftwareRendering.Checked; ;
			Properties.Settings.Default.fullsize = rbSizeFullmap.Checked; ;
			Properties.Settings.Default.localsize = rbSizeLocal.Checked;
			Properties.Settings.Default.autosize = rbSizeAuto.Checked;

			Properties.Settings.Default.emphore = cbEmphasizeOre.Checked;
			Properties.Settings.Default.modconfig = cbModConfig.Checked;
			Properties.Settings.Default.omitsquarespreview = cbOmitSquareMarkers.Checked;
			Properties.Settings.Default.squaredpos = cbSquaredStartPositions.Checked;
			Properties.Settings.Default.tiledpos = cbTiledStartPositions.Checked;
			Properties.Settings.Default.outputjpg = cbOutputJPG.Checked;
			Properties.Settings.Default.outputpng = cbOutputPNG.Checked;
			Properties.Settings.Default.outputthumb = cbOutputThumbnail.Checked;
			Properties.Settings.Default.thumbpreserveaspect = cbPreserveThumbAspect.Checked;
			Properties.Settings.Default.injectthumb = cbReplacePreview.Checked;

			Properties.Settings.Default.Save();
		}

		#region registry searching
		private string FindRenderProg() {
			if (!IsLinux) {
				try {
					using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
						return (string)key.OpenSubKey("SOFTWARE\\CNC Map Render").GetValue("") + "\\" + RendererExe;
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
				using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
					return Path.GetDirectoryName((string)key.OpenSubKey("SOFTWARE\\Westwood\\" + (RA2 ? "Red Alert 2" : "Tiberian Sun")).GetValue("InstallPath", string.Empty));
			}
			catch (NullReferenceException) { } // no registry entry
			catch (ArgumentException) { } // invalid path

			return Environment.CurrentDirectory;
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
				string appPath = Path.GetDirectoryName(Application.ExecutablePath);
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
		private void BrowseRenderer(object sender, EventArgs e) {
			ofd.CheckFileExists = true;
			ofd.Multiselect = false;
			ofd.Filter = "Executable (*.exe)|*.exe";
			ofd.InitialDirectory = Directory.GetCurrentDirectory();
			ofd.FileName = RendererExe;
			if (ofd.ShowDialog() == DialogResult.OK) {
				tbRenderProg.Text = ofd.FileName.StartsWith(Directory.GetCurrentDirectory())
					? ofd.FileName.Substring(Directory.GetCurrentDirectory().Length + 1)
					: ofd.FileName;
			}
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
			tbThumbDimensions.Visible = cbPreserveThumbAspect.Visible = cbOutputThumbnail.Checked;
			UpdateCommandline();
		}

		private void UpdateCommandline() {
			string cmd = GetCommandline();
			string file = tbRenderProg.Text;
			if (file.Contains("\\"))
				file = file.Substring(file.LastIndexOf('\\') + 1);
			tbCommandPreview.Text = file + " " + cmd;
		}
		private string GetCommandline() {
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
			else if (tbMixDir.Text != FindMixDir(rbEngineAuto.Checked || rbEngineRA2.Checked || rbEngineYR.Checked))
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

			if (rbPreferSoftwareRendering.Checked) cmd += "-g ";
			else if (rbPreferHardwareRendering.Checked) cmd += "-G ";

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
			}

			return cmd;
		}
		#endregion

		#region renderer program execution
		private void ExecuteCommand(object sender, EventArgs e) {
			if (File.Exists(tbInput.Text) == false) {
				UpdateStatus("aborted, no input file", 100);
				MessageBox.Show("Input file doesn't exist. Aborting.");
				return;
			}

			string exePath = GetRendererExePath();
			if (!File.Exists(exePath)) {
				UpdateStatus("aborted, no renderer exe", 100);
				MessageBox.Show("File " + RendererExe + " not found. Aborting.");
				return;
			}

			if (!cbOutputPNG.Checked && !cbOutputJPG.Checked && !cbReplacePreview.Checked) {
				UpdateStatus("aborted, no output format picked", 100);
				MessageBox.Show("Either PNG, JPEG or Replace Preview must be checked.", "Nothing to do..", MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return;
			}
			tabControl.SelectTab(tpLog);
			MakeLog();
			ProcessCmd(exePath);
		}

		public string GetRendererExePath() {
			string exepath = tbRenderProg.Text;
			if (!File.Exists(exepath)) {
				exepath = Application.ExecutablePath;
				if (exepath.Contains("\\"))
					exepath = exepath.Substring(0, exepath.LastIndexOf('\\') + 1);
				exepath += RendererExe;
			}
			return exepath;
		}

		private void ProcessCmd(string exepath) {
			ThreadPool.QueueUserWorkItem(delegate(object state) {
				try {
					var p = new Process { StartInfo = { FileName = exepath, Arguments = GetCommandline() } };

					p.OutputDataReceived += ConsoleDataReceived;
					p.StartInfo.CreateNoWindow = true;
					p.StartInfo.RedirectStandardOutput = true;
					p.StartInfo.UseShellExecute = false;
					p.Start();
					p.BeginOutputReadLine();

					p.WaitForExit();

					if (p.ExitCode == 0)
						// indicates EOF
						Log("\r\nYour map has been rendered. If your image did not appear, something went wrong." +
							" Please sent an email to frank@zzattack.org with your map as an attachment.");
					else
						BeginInvoke((MethodInvoker)AskBugReport);
				}
				catch (InvalidOperationException) {
				}
				catch (Win32Exception) {
				}
			});
		}
		private void AskBugReport() {
			// seems like rendering failed!
			Log("\r\nIt appears an error ocurred during image rendering.");
			var dr = MessageBox.Show(
				"Rendering appears to have failed. Would you like to transmit a bug report containing the error log and map to frank@zzattack.org?",
				"Failed, submit report", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
			if (dr == DialogResult.Yes)
				SubmitBugReport();
		}

		private void SubmitBugReport() {
			try {
				const string url = UpdateChecker.UpdateCheckHost + "tool/report_bug";
				WebClient wc = new WebClient();
				wc.Proxy = null;
				var data = new NameValueCollection();
				data.Set("renderer_version", Assembly.LoadFile(GetRendererExePath()).GetName().Version.ToString());
				data.Set("input_map", File.ReadAllText(tbInput.Text));
				data.Set("input_name", Path.GetFileName(tbInput.Text));
				data.Set("commandline", GetCommandline());
				data.Set("log_text", rtbLog.Text);

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
		private void ConsoleDataReceived(object sender, DataReceivedEventArgs e) {
			if (e.Data == null) // finished
				UpdateStatus("rendering complete", 100);
			else
				Log(e.Data);
		}
		private bool _showlog;
		private void MakeLog() {
			if (_showlog)
				return;

			Height += gbLog.Height + 10;
			gbLog.Visible = true;
			_showlog = true;
		}
		private delegate void LogDelegate(string s);
		private void Log(string s) {
			if (InvokeRequired) {
				Invoke(new LogDelegate(Log), s);
				return;
			}
			rtbLog.Text += s + "\r\n";
			rtbLog.SelectionStart = rtbLog.TextLength - 1;
			rtbLog.SelectionLength = 1;
			rtbLog.ScrollToCaret();

			var progressEntry = progressIndicators.FirstOrDefault(kvp => s.Contains(kvp.Value));
			if (!progressEntry.Equals(default(KeyValuePair<int, string>))) {
				UpdateStatus("rendering: " + progressEntry.Key + "%", progressEntry.Key);
			}
			if (s.Contains("Drawing map...")) {
				int idx = s.LastIndexOf(" ") + 1;
				double pct = Math.Round(27 + (90.0 - 27.0) * int.Parse(s.Substring(idx, s.Length - idx - 1)) / 100.0, 0);
				UpdateStatus("drawing, " + pct + "%", (int)pct);
			}
		}
		private Dictionary<int, string> progressIndicators = new Dictionary<int, string>() {
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
		};
		#endregion


	}
}