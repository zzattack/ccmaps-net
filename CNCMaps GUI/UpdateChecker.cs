using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Xml;

namespace CNCMaps.GUI {
	class UpdateChecker : EventArgs {
		public event EventHandler Connected;
		public event EventHandler AlreadyLatest;
		public event EventHandler<UpdateAvailableArgs> UpdateAvailable;
		public event EventHandler UpdateCheckFailed;
		public event DownloadProgressChangedEventHandler DownloadProgressChanged;

		private const string UpdateCheckHost = "http://cncmaps.zzattack.org/tool/version_check";

		public UpdateChecker() {
		}

		public void CheckVersion() {
			WebClient wc = new WebClient();
			wc.Proxy = null;
			wc.DownloadStringAsync(new Uri(UpdateCheckHost));
			wc.DownloadProgressChanged += (sender, args) => DownloadProgressChanged(this, args);
			wc.DownloadStringCompleted += (sender, args) => {
				if (args.Cancelled || args.Error != null)
					UpdateCheckFailed(this, EventArgs.Empty);
				else {
					try {
						XmlDocument xd = new XmlDocument();
						xd.LoadXml(args.Result);
						var versionNode = xd["version"];
						var version = Version.Parse(versionNode["version_string"].InnerText);
						var releaseDate = DateTime.ParseExact(versionNode["release_date"].InnerText.Trim(), "yyyy'-'MM'-'dd", null);
						string releaseNotes = versionNode["release_notes"].InnerText;
						string url = versionNode["url"].InnerText;
						UpdateAvailable(this, new UpdateAvailableArgs {
							DownloadUrl = url,
							ReleaseDate = releaseDate,
							ReleaseNotes = releaseNotes,
							Version = version,
						});
					}
					catch {
						UpdateCheckFailed(this, EventArgs.Empty);
					}
				}
			};
		}

	}

	class UpdateAvailableArgs : EventArgs {
		public Version Version { get; set; }
		public string ReleaseNotes { get; set; }
		public DateTime ReleaseDate { get; set; }
		public string DownloadUrl { get; set; }
	}
}
