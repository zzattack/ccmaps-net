using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Text;
using CNCMaps.Utility;

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
			var sb = new System.Text.StringBuilder();
			var sw = new StringWriter(sb);
			_options.WriteOptionDescriptions(sw);
			Console.WriteLine(sb.ToString());
		}

#if DEBUG

		public static void BuildDatabase() {
			var customerSPAA05 = new Dictionary<string, HashSet<LicenseEntry>>();
			var customerPDA = new Dictionary<string, HashSet<KeyValuePair<string, string>>>();
			var customerAccessories = new Dictionary<string, HashSet<LicenseEntry>>();

			foreach (var info in RecursiveMovieFolderScan("M:\\customer license files").Where(i => !i.IsDirectory)) {
				if (info.Path.ToLower().EndsWith("license_features.dat"))
					continue;
				try {
					string customer = info.Path.Split('\\', '/')[2];

					if (!customerSPAA05.ContainsKey(customer)) {
						customerSPAA05[customer] = new HashSet<LicenseEntry>();
						customerPDA[customer] = new HashSet<KeyValuePair<string, string>>();
						customerAccessories[customer] = new HashSet<LicenseEntry>();
					}

					RAMISLicense rl = RAMISLicense.LoadFrom(info.Path);
					if (!customerPDA[customer].Any(t => t.Key == rl.PDADeviceID && t.Value == rl.PinCode.ToString()))
						customerPDA[customer].Add(new KeyValuePair<string, string>(rl.PDADeviceID, rl.PinCode.ToString()));

					if (info.Path.ToLower().Contains("license_accessory")) {
						foreach (var dev in rl.Devices)
							if (!customerAccessories[customer].Any(l => l.Spaa05Serial == dev.Spaa05Serial))
								customerAccessories[customer].Add(dev);
					}
					else {
						foreach (var dev in rl.Devices)
							if (!customerSPAA05[customer].Any(l => l.Spaa05Serial == dev.Spaa05Serial))
								customerSPAA05[customer].Add(dev);
					}
				}
				catch {
				}
			}

			StringBuilder sb = new StringBuilder();
			foreach (string customer in customerSPAA05.Keys) {
				sb.AppendFormat("newCust, created = Customer.objects.get_or_create(name=\"{0}\", location=\"unknown\")\n", customer);
				sb.AppendLine("newCust.save()");
				foreach (var spaa05 in customerSPAA05[customer]) {
					sb.AppendFormat("newSPAA05, created = SPAA05.objects.get_or_create(spaa05_id={0}, owner=newCust, service_date=\"{1}\")\n",
						spaa05.Spaa05Serial, spaa05.ServiceDate.ToString("yyyy-MM-dd"));
					sb.AppendLine("newSPAA05.save()");
				}
				foreach (var acc in customerAccessories[customer]) {
					sb.AppendFormat("newAccessory, created = Accessory.objects.get_or_create(accessory_id={0}, owner=newCust, service_date=\"{1}\")\n",
						acc.Spaa05Serial, acc.ServiceDate.ToString("yyyy-MM-dd"));
					sb.AppendLine("newAccessory.save()");
				}
				foreach (var pda in customerPDA[customer]) {
					sb.AppendFormat("newPDA, created = PDADevice.objects.get_or_create(pda_id=\"{0}\", owner=newCust, pin_code={1})\n",
						pda.Key, pda.Value);
					sb.AppendLine("newPDA.save()");
				}
				sb.AppendLine();
			}
			var sw = new StreamWriter("license_pythondb.txt");
			sw.Write(sb.ToString());
			sw.Flush();
			sw.Close();

			sw = new StreamWriter("license_db.txt");
			sw.WriteLine("SPAA05");
			foreach (var customer in customerSPAA05.Keys) {
				sw.Write(customer);
				sw.Write("\t");
				foreach (var spaa05 in customerSPAA05[customer].Select(c => c.Spaa05Serial.ToString()).OrderBy(int.Parse)) {
					sw.Write(spaa05);
					sw.Write("\t");
				}
				sw.WriteLine();
			}

			sw.WriteLine();
			sw.WriteLine("PDA");
			foreach (var customer in customerPDA.Keys) {
				sw.Write(customer);
				sw.Write("\t");
				foreach (var pda in customerPDA[customer].OrderBy(s => s)) {
					sw.Write(pda);
					sw.Write("\t");
				}
				sw.WriteLine();
			}

			sw.WriteLine();
			sw.WriteLine("ACCESSORIES");
			foreach (var customer in customerAccessories.Keys) {
				sw.Write(customer);
				sw.Write("\t");
				foreach (var accessory in customerAccessories[customer].Select(a => a.Spaa05Serial.ToString()).OrderBy(int.Parse)) {
					sw.Write(accessory);
					sw.Write("\t");
				}
				sw.WriteLine();
			}
			sw.Flush();
			sw.Close();
		}

		public static void ObtainPincodes() {
			Dictionary<string, int> PDAPincodes = new Dictionary<string, int>();

			foreach (var info in RecursiveMovieFolderScan("M:\\customer license files").Where(i => !i.IsDirectory)) {
				if (info.Path.ToLower().EndsWith("license_features.dat"))
					continue;
				try {
					RAMISLicense rl = RAMISLicense.LoadFrom(info.Path);
					PDAPincodes[rl.PDADeviceID] = rl.PinCode;
				}
				catch {
				}
			}

			StreamWriter sw = new StreamWriter("pda_pincodes.txt");
			foreach (var entry in PDAPincodes) {
				sw.WriteLine(string.Format("pda = PDADevice.objects.get(pda_id='{0}')", entry.Key));
				sw.WriteLine(string.Format("pda.pin_code = {0}", entry.Value));
				sw.WriteLine("pda.save()");
				sw.WriteLine();
			}

			sw.Flush();
			sw.Close();
		}

		public static void ObtainServiceDates() {
			Dictionary<string, DateTime> dateTimes = new Dictionary<string, DateTime>();

			foreach (var info in RecursiveMovieFolderScan("M:\\customer license files").Where(i => !i.IsDirectory)) {
				if (info.Path.ToLower().EndsWith("license_features.dat"))
					continue;
				try {
					RAMISLicense rl = RAMISLicense.LoadFrom(info.Path);
					foreach (var spaa05 in rl.Devices) {
						if (dateTimes.ContainsKey(spaa05.Spaa05Serial.ToString())) dateTimes[spaa05.ToString()] =
							dateTimes[spaa05.Spaa05Serial.ToString()] > spaa05.ServiceDate ? dateTimes[spaa05.ToString()] : spaa05.ServiceDate;
						else
							dateTimes[spaa05.Spaa05Serial.ToString()] = spaa05.ServiceDate;
					}
				}
				catch {
				}
			}

			StreamWriter sw = new StreamWriter("spaa05_servicedates.txt");
			foreach (var entry in dateTimes) {
				sw.WriteLine(string.Format("spaa05 = SPAA05.objects.get(spaa05_id='{0}')", entry.Key));
				sw.WriteLine(string.Format("spaa05.service_date = datetime.date({0},{1},{2})", entry.Value.Year, entry.Value.Month, entry.Value.Day));
				sw.WriteLine("spaa05.save()");
				sw.WriteLine();
			}

			sw.Flush();
			sw.Close();
		}


		public static void ObtainAccessoryServiceDates() {
			Dictionary<string, DateTime> dateTimes = new Dictionary<string, DateTime>();

			foreach (var info in RecursiveMovieFolderScan("M:\\customer license files").Where(i => !i.IsDirectory)) {
				if (!info.Path.ToLower().EndsWith("license_accessory.dat"))
					continue;
				try {
					RAMISLicense rl = RAMISLicense.LoadFrom(info.Path);
					foreach (var spaa05 in rl.Devices) {
						if (dateTimes.ContainsKey(spaa05.Spaa05Serial.ToString())) dateTimes[spaa05.ToString()] =
							dateTimes[spaa05.Spaa05Serial.ToString()] > spaa05.ServiceDate ? dateTimes[spaa05.ToString()] : spaa05.ServiceDate;
						else
							dateTimes[spaa05.Spaa05Serial.ToString()] = spaa05.ServiceDate;
					}
				}
				catch {
				}
			}

			StreamWriter sw = new StreamWriter("accessory_servicedates.txt");
			foreach (var entry in dateTimes) {
				sw.WriteLine(string.Format("acc = Accessory.objects.get(accessory_id='{0}')", entry.Key));
				sw.WriteLine(string.Format("acc.service_date = datetime.date({0},{1},{2})", entry.Value.Year, entry.Value.Month, entry.Value.Day));
				sw.WriteLine("acc.save()");
				sw.WriteLine();
			}

			sw.Flush();
			sw.Close();
		}

		class Info {
			public bool IsDirectory;
			public string Path;
			public DateTime ModifiedDate;
			public DateTime CreatedDate;
		}

		static List<Info> RecursiveMovieFolderScan(string path) {
			var info = new List<Info>();
			var dirInfo = new DirectoryInfo(path);
			foreach (var dir in dirInfo.GetDirectories()) {
				info.Add(new Info() {
					IsDirectory = true,
					CreatedDate = dir.CreationTimeUtc,
					ModifiedDate = dir.LastWriteTimeUtc,
					Path = dir.FullName
				});

				info.AddRange(RecursiveMovieFolderScan(dir.FullName));
			}

			foreach (var file in dirInfo.GetFiles("license_*.dat")) {
				info.Add(new Info() {
					IsDirectory = false,
					CreatedDate = file.CreationTimeUtc,
					ModifiedDate = file.LastWriteTimeUtc,
					Path = file.FullName
				});
			}

			return info;
		}

		private static void SearchDatabaseBySPAA05(int spaa05Id) {
			Dictionary<string, HashSet<LicenseEntry>> customerSPAA05 = new Dictionary<string, HashSet<LicenseEntry>>();
			Dictionary<string, HashSet<KeyValuePair<string, string>>> customerPDA = new Dictionary<string, HashSet<KeyValuePair<string, string>>>();
			Dictionary<string, HashSet<LicenseEntry>> customerAccessories = new Dictionary<string, HashSet<LicenseEntry>>();

			foreach (var info in RecursiveMovieFolderScan("M:\\customer license files").Where(i => !i.IsDirectory)) {
				if (info.Path.ToLower().EndsWith("license_features.dat"))
					continue;
				try {
					string customer = info.Path.Split('\\', '/')[2];

					if (!customerSPAA05.ContainsKey(customer)) {
						customerSPAA05[customer] = new HashSet<LicenseEntry>();
						customerPDA[customer] = new HashSet<KeyValuePair<string, string>>();
						customerAccessories[customer] = new HashSet<LicenseEntry>();
					}

					RAMISLicense rl = RAMISLicense.LoadFrom(info.Path);

					if (rl.Devices.Any(d => d.Spaa05Serial == spaa05Id)) {
						MessageBox.Show("Found spaa05 in file " + info.Path);
					}

					if (!customerPDA[customer].Any(t => t.Key == rl.PDADeviceID && t.Value == rl.PinCode.ToString()))
						customerPDA[customer].Add(new KeyValuePair<string, string>(rl.PDADeviceID, rl.PinCode.ToString()));

					if (info.Path.ToLower().Contains("license_accessory")) {
						foreach (var dev in rl.Devices)
							if (!customerAccessories[customer].Any(l => l.Spaa05Serial == dev.Spaa05Serial))
								customerAccessories[customer].Add(dev);
					}
					else {
						foreach (var dev in rl.Devices)
							if (!customerSPAA05[customer].Any(l => l.Spaa05Serial == dev.Spaa05Serial))
								customerSPAA05[customer].Add(dev);
					}
				}
				catch {
				}
			}

		}

#endif
	}
}
