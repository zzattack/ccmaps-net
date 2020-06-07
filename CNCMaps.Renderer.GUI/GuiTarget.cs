using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NLog;
using NLog.Targets;

namespace CNCMaps.GUI {
	internal class GuiTarget : TargetWithLayout {
		public RichTextBox TargetControl { get; set; }

		protected override void Write(LogEventInfo log) {
			Color c = Color.Black;

			switch (log.Level.Name) {
				case "Debug":
					c = Color.DarkGreen;
					break;
				case "Error":
					c = Color.Red;
					break;
				case "Fatal":
					c = Color.DarkRed;
					break;
				case "Info":
					c = Color.Blue;
					break;
				case "Trace":
					c = Color.DimGray;
					break;
				case "Warn":
					c = Color.YellowGreen;
					break;
			}

			SafeBeginInvoke(new Action(() => {
				TargetControl.SelectionStart = TargetControl.TextLength;
				TargetControl.SelectionLength = 0;
				TargetControl.SelectionColor = c;
				TargetControl.AppendText("[" + log.Level + "] " + log.FormattedMessage + "\r\n");
				SendMessage(TargetControl.Handle, WM_VSCROLL, (IntPtr)SB_BOTTOM, IntPtr.Zero);
			}));
		}

		private const int WM_VSCROLL = 0x115;
		private const int SB_BOTTOM = 7;
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

		public void SafeBeginInvoke(Delegate del) {
			if (TargetControl.InvokeRequired) TargetControl.BeginInvoke(del);
			else del.DynamicInvoke();
		}
	}
}