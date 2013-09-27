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

			propertyGrid1.SelectedObject = new ModConfig();
			_pendingChanges = true;

			propertyGrid1.PropertyValueChanged += (o, args) => _pendingChanges = true;
		}

		public ModConfigEditor(string modConfigFile)
			: this() {
			this.ModConfigFile = modConfigFile;
			try {
				using (var f = File.OpenRead(modConfigFile))
					propertyGrid1.SelectedObject = ModConfig.Deserialize(f);
				_pendingChanges = false;
			}
			catch {
			}
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
			propertyGrid1.SelectedObject = new ModConfig();
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e) {
			if (openFileDialog1.ShowDialog() == DialogResult.OK) {
				using (var f = openFileDialog1.OpenFile())
					propertyGrid1.SelectedObject = ModConfig.Deserialize(f);
				ModConfigFile = saveFileDialog1.FileName;
				_pendingChanges = false;
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
			if (saveFileDialog1.ShowDialog() == DialogResult.OK) {
				using (var f = saveFileDialog1.OpenFile())
					(propertyGrid1.SelectedObject as ModConfig).Serialize(f);
				ModConfigFile = saveFileDialog1.FileName;
				_pendingChanges = false;
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
			Close();
		}

		private void CopyTheaters(ModConfig modConfig) {
			(propertyGrid1.SelectedObject as ModConfig).Theaters = modConfig.Theaters;
			TypeDescriptor.Refresh(propertyGrid1.SelectedObject);
			propertyGrid1.ExpandAllGridItems();
		}

		private void ModConfigEditor_FormClosing(object sender, FormClosingEventArgs e) {
			if (_pendingChanges && MessageBox.Show("There are pending changes. Save first?", "Unsaved changes",
				MessageBoxButtons.YesNo) == DialogResult.Yes)
				saveToolStripMenuItem_Click(null, null);

		}
	}
}
