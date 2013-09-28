using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using CNCMaps.Utility;

namespace CNCMaps.GUI {
	public partial class ModConfigEditor : Form {
		public string ModConfigFile { get; private set; }
		private bool _pendingChanges = false;

		public ModConfigEditor() {
			InitializeComponent();
			propertyGrid1.PropertyValueChanged += (o, args) => {
				_pendingChanges = true;
			};
			SelectObject(ModConfig.DefaultsYR.Clone());
		}

		public ModConfigEditor(string modConfigFile)
			: this() {
			this.ModConfigFile = modConfigFile;
			if (!string.IsNullOrEmpty(modConfigFile) && File.Exists(modConfigFile)) {
				try {
					using (var f = File.OpenRead(modConfigFile))
						SelectObject(ModConfig.Deserialize(f));
					_pendingChanges = false;
				}
				catch {
				}
			}
		}
		private void SelectObject(ModConfig modConfig) {
			modConfig.PropertyChanged += (sender, args) => {
				propertyGrid1.Refresh();
				propertyGrid1.ExpandAllGridItems();
			};

			bool expand = propertyGrid1.SelectedObject == null;
			propertyGrid1.SelectedObject = modConfig;
			if (expand) propertyGrid1.ExpandAllGridItems();
		}

		private void loadTSToolStripMenuItem_Click(object sender, EventArgs e) {
			CopyTheaters(ModConfig.DefaultsTS.Clone());
		}

		private void loadRA2ToolStripMenuItem_Click(object sender, EventArgs e) {
			CopyTheaters(ModConfig.DefaultsRA2.Clone());
		}

		private void loadYRToolStripMenuItem_Click(object sender, EventArgs e) {
			CopyTheaters(ModConfig.DefaultsYR.Clone());
		}

		private void newToolStripMenuItem1_Click(object sender, EventArgs e) {
			SelectObject(new ModConfig());
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e) {
			if (openFileDialog1.ShowDialog() == DialogResult.OK) {
				using (var f = openFileDialog1.OpenFile()) {
					SelectObject(ModConfig.Deserialize(f));
				}
				ModConfigFile = openFileDialog1.FileName;
				_pendingChanges = false;
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
			if (saveFileDialog1.ShowDialog() == DialogResult.OK) {
				using (var f = saveFileDialog1.OpenFile())
					(propertyGrid1.SelectedObject as ModConfig).Serialize(f);
				ModConfigFile = Path.GetFullPath(saveFileDialog1.FileName);
				_pendingChanges = false;
			}
		}

		private void CopyTheaters(ModConfig modConfig) {
			(propertyGrid1.SelectedObject as ModConfig).Theaters = modConfig.Theaters;
			TypeDescriptor.Refresh(propertyGrid1.SelectedObject);
		}

		private void ModConfigEditor_FormClosing(object sender, FormClosingEventArgs e) {
			if (_pendingChanges && MessageBox.Show("There are pending changes. Save first?", "Unsaved changes",
				MessageBoxButtons.YesNo) == DialogResult.Yes)
				saveToolStripMenuItem_Click(null, null);
		}

	}
}