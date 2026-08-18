using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.CommandLine;
using CNCMaps.Shared;

namespace CNCMaps.GUI {
	static class Program {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args) {
			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			try {
				// use command-line parameters if provided (used by auto-update functionality)
				bool showHelp = false;
				bool skipUpdateCheck = false;

				var optHelp = new Option<bool>("--help", "-h") { Description = "Show this short help text" };
				var optKillPid = new Option<string>("--killpid", "-k") { Description = "Kill calling (old) process (to be used by updater)" };
				var optCleanUpdate = new Option<string>("--cleanupdate", "-c") { Description = "Delete (old) executable (to be used by updater)" };
				var optSkipUpdateCheck = new Option<bool>("--skip-update-check", "-s") { Description = "Skip update check" };
				var rootCommand = new RootCommand { TreatUnmatchedTokensAsErrors = false };
				rootCommand.Options.Add(optHelp);
				rootCommand.Options.Add(optKillPid);
				rootCommand.Options.Add(optCleanUpdate);
				rootCommand.Options.Add(optSkipUpdateCheck);
				var parsed = rootCommand.Parse(args);
				showHelp = parsed.GetValue(optHelp);
				skipUpdateCheck = parsed.GetValue(optSkipUpdateCheck);
				if (parsed.GetValue(optKillPid) is string pid)
					KillDanglingProcess(pid);
				if (parsed.GetValue(optCleanUpdate) is string oldExe)
					RemoveOldExecutable(oldExe);
				if (showHelp) {
					ShowHelp();
					return;
				}

				Application.Run(new MainForm(skipUpdateCheck));
			}

			catch (Exception exc) {
				MessageBox.Show("An error ocurred: " + exc.Message + "\r\n\r\nCallstack: " + exc.StackTrace);
			}
		}

		private static void KillDanglingProcess(string pid) {
			try {
				var proc = Process.GetProcessById(int.Parse(pid));
				string executable = proc.MainModule.FileName.Replace(".vshost", string.Empty);
				proc.CloseMainWindow();
				if (!proc.WaitForExit(100)) proc.Kill();
			}
			catch (FormatException) {
			}
			catch (ArgumentException) {
			}
		}

		private static void RemoveOldExecutable(string path) {
			try {
				Stopwatch sw = Stopwatch.StartNew();
				bool success = false;
				while (sw.ElapsedMilliseconds < 10000) {
					try {
						File.Delete(path);
						success = true;
						break;
					}
					catch (UnauthorizedAccessException) {
						Thread.Sleep(10); // keep trying for a while
					}
				}
				if (!success)
					MessageBox.Show(string.Format("Tried to remove old file {0} but failed. Try to delete it manually.", path));
			}
			catch (FormatException) {
			}
			catch (ArgumentException) {
			}
			catch (Win32Exception) {
			}
		}

		static void ShowHelp() {
			MessageBox.Show(
				"-h, --help               Show this short help text" + Environment.NewLine +
				"-k, --killpid=PID        Kill calling (old) process (to be used by updater)" + Environment.NewLine +
				"-c, --cleanupdate=EXE    Delete (old) executable (to be used by updater)" + Environment.NewLine +
				"-s, --skip-update-check  Skip update check",
				"Command line options");
		}
	}
}
