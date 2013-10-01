using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using CNCMaps.Shared;

namespace CNCMaps.GUI {
	static class Program {
		static OptionSet _options;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args) {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			try {
				// use command-line parameters if provided (used by auto-update functionality)
				bool showHelp = false;
				bool skipUpdateCheck = false;

				_options = new OptionSet {
			        {"h|help", "Show this short help text", v => showHelp = true},
			        {"k|killpid=", "Kill calling (old) process (to be used by updater)", KillDanglingProcess },
			        {"c|cleanupdate=", "Delete (old) executable (to be used by updater)", RemoveOldExecutable },
					{"s|skip-update-check", "Skip update check)", v => skipUpdateCheck = true },
			    };
				_options.Parse(args);
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
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write("Usage: ");
			Console.WriteLine("");
			var sb = new StringBuilder();
			var sw = new StringWriter(sb);
			_options.WriteOptionDescriptions(sw);
			Console.WriteLine(sb.ToString());
		}
	}
}
