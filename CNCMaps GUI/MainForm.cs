using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CNCMaps.GUI {

	public partial class MainForm : Form {
		public const string RendererExe = "CNCMaps.exe";
		private bool _skipUpdateCheck;

		public MainForm() {
			InitializeComponent();
		}
		public MainForm(bool skipUpdateCheck)
			: this() {
			_skipUpdateCheck = skipUpdateCheck;
		}

		private void MainFormLoad(object sender, EventArgs args) {
			tbRenderProg.Text = FindRenderProg();
			tbMixDir.Text = FindMixDir(true);
			UpdateCommandline();
			Height -= 180;

			if (!_skipUpdateCheck) {
				var uc = new UpdateChecker();
				uc.AlreadyLatest += (o, e) => { lblStatus.Text = "Status: already latest version"; };
				uc.Connected += (o, e) => { pbProgress.Value = 10; };
				uc.UpdateCheckFailed += (o, e) => {
					pbProgress.Value = 100;
					lblStatus.Text = "Status: update check failed";
				};
				uc.UpdateAvailable += (o, e) => {
					pbProgress.Value = 100;
					lblStatus.Text = "Status: update available";
					var dr = MessageBox.Show(string.Format("An update to version {0} released on {1} is available. Release notes: \r\n\r\n{2}\r\n\r\nUpdate now?", 
							e.Version.ToString(), e.ReleaseDate, e.ReleaseNotes), "Update available", 
							MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
					if (dr == DialogResult.Yes)
						DownloadAndUpdate(e.DownloadUrl);
				};
				uc.CheckVersion();
			}
			else {
				lblStatus.Text = "Status: not checking for newer version";
			}
		}

		private string FindRenderProg() {
			if (!IsLinux) {
				try {
					using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
						return (string)key.OpenSubKey("SOFTWARE\\CNC Map Render").GetValue("") + "\\" + RendererExe;
				}
				catch (NullReferenceException) { }
				catch (SecurityException) { }
			}
			return File.Exists(Path.Combine(Environment.CurrentDirectory, RendererExe)) ? RendererExe : "";
		}

		private static string FindMixDir(bool RA2) {
			if (IsLinux) // don't expect registry access..
				return Environment.CurrentDirectory;

			try {
				using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
					return Path.GetDirectoryName(
						(string)key.OpenSubKey("SOFTWARE\\Westwood\\" + (RA2 ? "Red Alert 2" : "Tiberian Sun")).GetValue("InstallPath", string.Empty));
			}
			catch (NullReferenceException) { }

			return Environment.CurrentDirectory;
		}

		public static bool IsLinux {
			get {
				int p = (int)Environment.OSVersion.Platform;
				return (p == 4) || (p == 6) || (p == 128);
			}
		}

		private void OutputNameCheckedChanged(object sender, EventArgs e) {
			tbCustomOutput.Visible = rbCustomFilename.Checked;
			UpdateCommandline();
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
				tbRenderProg.Text = ofd.FileName.StartsWith(Directory.GetCurrentDirectory()) ?
					ofd.FileName.Substring(Directory.GetCurrentDirectory().Length + 1) :
					ofd.FileName;
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

			if (tbMixDir.Text != FindMixDir(rbEngineAuto.Checked || rbEngineRA2.Checked || rbEngineYR.Checked)) cmd += "-m " + "\"" + tbMixDir.Text + "\" ";
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

			return cmd;
		}

		private void UIChanged(object sender, EventArgs e) { UpdateCommandline(); }

		private void PngOutputCheckedChanged(object sender, EventArgs e) {
			nudCompression.Visible = label1.Visible = cbOutputPNG.Checked;
			UpdateCommandline();
		}

		private void JpegOutputCheckedChanged(object sender, EventArgs e) {
			lblQuality.Visible = nudEncodingQuality.Visible = cbOutputJPG.Checked;
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

		private void ExecuteCommand(object sender, EventArgs e) {
			if (File.Exists(tbInput.Text) == false) {
				MessageBox.Show("Input file doesn't exist. Aborting.");
				return;
			}

			string exepath = tbRenderProg.Text;
			if (File.Exists(exepath) == false) {
				exepath = Application.ExecutablePath;
				if (exepath.Contains("\\"))
					exepath = exepath.Substring(0, exepath.LastIndexOf('\\') + 1);
				exepath += RendererExe;
				if (File.Exists(exepath) == false) {
					MessageBox.Show("File " + RendererExe + " not found. Aborting.");
					return;
				}
			}

			if (!cbOutputPNG.Checked && !cbOutputJPG.Checked && !cbReplacePreview.Checked) {
				MessageBox.Show("Either PNG, JPEG or Replace Preview must be checked.", "Nothing to do..", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			MakeLog();
			ProcessCmd(exepath);
		}

		private void ProcessCmd(string exepath) {
			try {
				var p = new Process { StartInfo = { FileName = exepath, Arguments = GetCommandline() } };

				p.OutputDataReceived += ConsoleDataReceived;
				p.StartInfo.CreateNoWindow = true;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.UseShellExecute = false;
				p.Start();
				p.BeginOutputReadLine();
			}
			catch (InvalidOperationException) { }
			catch (Win32Exception) { }
		}

		#region Logging

		private void ConsoleDataReceived(object sender, DataReceivedEventArgs e) {
			if (e.Data == null) {
				// indicates EOF
				Log("\r\nYour map has been rendered. If your image did not appear, something went wrong." +
					" Please sent an email to frank@zzattack.org with your map as an attachment.");
			}
			else {
				Log(e.Data);
			}
		}

		bool _showlog;
		private void MakeLog() {
			if (_showlog)
				return;

			Height += 180;
			cbLog.Visible = true;
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
		}

		#endregion

		// automatically swap between RA2/TS mix dir from registry
		private bool currentEngineRA2 = true;
		private void RbEngineCheckedChanged(object sender, EventArgs e) {
			bool newEngineRA2 = rbEngineAuto.Checked || rbEngineRA2.Checked || rbEngineYR.Checked;
			if (currentEngineRA2 && !newEngineRA2 && tbMixDir.Text == FindMixDir(true))
				tbMixDir.Text = FindMixDir(false);
			else if (!currentEngineRA2 && newEngineRA2 && tbMixDir.Text == FindMixDir(false))
				tbMixDir.Text = FindMixDir(true);
			currentEngineRA2 = newEngineRA2;
		}


		private void DownloadAndUpdate(string url) {
			lblFill.Text = "Status: downloading new program version";
			var pbf = new ProgressBarForm();
			pbf.StartPosition = FormStartPosition.Manual;
			pbf.Location = new Point(this.Left + this.Width / 2 - pbf.Width / 2, this.Top + this.Height / 2 - pbf.Height / 2);
			pbf.Text = "Updating program";
			pbf.lblStatus.Text = "Status: contacting download server";
			pbf.progressBar.Value = 4;
			pbf.Show();

			var wc = new WebClient();
			var address = new Uri(url);

			wc.DownloadProgressChanged += (sender, args) =>
				BeginInvoke((Action)delegate {
					pbf.progressBar.Value = args.ProgressPercentage;
					pbf.lblStatus.Text = string.Format("Status: downloading, {0}%", args.ProgressPercentage);
				});

			wc.DownloadDataCompleted += (sender, args) => {
				string appPath = Application.ExecutablePath;
				string dest = appPath + ".tmp";
				string suffix = "";
				int suffixNr = 0;
				while (File.Exists(dest + suffix)) {
					suffixNr++;
					suffix = suffixNr.ToString(CultureInfo.InvariantCulture);
				}
				dest = dest + suffix;
				File.Move(appPath, dest);
				File.WriteAllBytes(appPath, args.Result);
				// invoke 
				var psi = new ProcessStartInfo(appPath);
				psi.Arguments = string.Format("--killpid {0} --cleanupdate {1} --skip-update-check", Process.GetCurrentProcess().Id, dest);
				Process.Start(psi);
				pbf.Close();
			};

			// trigger it all
			wc.DownloadDataAsync(address);
		}
	}
}