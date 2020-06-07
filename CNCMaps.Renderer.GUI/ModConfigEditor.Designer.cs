namespace CNCMaps.GUI {
	partial class ModConfigEditor {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ModConfigEditor));
			this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.defaultsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.loadTSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.loadRA2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.loadYRToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
			this.btnAccept = new System.Windows.Forms.Button();
			this.btnLoadRA2Theaters = new System.Windows.Forms.Button();
			this.btnLoadYRTheaters = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnLoadTSTheaters = new System.Windows.Forms.Button();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// propertyGrid1
			// 
			this.propertyGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.propertyGrid1.Location = new System.Drawing.Point(0, 24);
			this.propertyGrid1.Name = "propertyGrid1";
			this.propertyGrid1.PropertySort = System.Windows.Forms.PropertySort.NoSort;
			this.propertyGrid1.Size = new System.Drawing.Size(538, 356);
			this.propertyGrid1.TabIndex = 0;
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.fileToolStripMenuItem,
			this.defaultsToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(538, 24);
			this.menuStrip1.TabIndex = 2;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripMenuItem1,
			this.openToolStripMenuItem,
			this.saveToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(103, 22);
			this.toolStripMenuItem1.Text = "&New";
			this.toolStripMenuItem1.Click += new System.EventHandler(this.newToolStripMenuItem1_Click);
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
			this.openToolStripMenuItem.Text = "&Open";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
			// 
			// saveToolStripMenuItem
			// 
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
			this.saveToolStripMenuItem.Text = "&Save";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
			// 
			// defaultsToolStripMenuItem
			// 
			this.defaultsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.loadTSToolStripMenuItem,
			this.loadRA2ToolStripMenuItem,
			this.loadYRToolStripMenuItem});
			this.defaultsToolStripMenuItem.Name = "defaultsToolStripMenuItem";
			this.defaultsToolStripMenuItem.Size = new System.Drawing.Size(62, 20);
			this.defaultsToolStripMenuItem.Text = "&Defaults";
			// 
			// loadTSToolStripMenuItem
			// 
			this.loadTSToolStripMenuItem.Name = "loadTSToolStripMenuItem";
			this.loadTSToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
			this.loadTSToolStripMenuItem.Text = "Load &TS/FS theaters";
			this.loadTSToolStripMenuItem.Click += new System.EventHandler(this.loadTSToolStripMenuItem_Click);
			// 
			// loadRA2ToolStripMenuItem
			// 
			this.loadRA2ToolStripMenuItem.Name = "loadRA2ToolStripMenuItem";
			this.loadRA2ToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
			this.loadRA2ToolStripMenuItem.Text = "Load &RA2 theaters";
			this.loadRA2ToolStripMenuItem.Click += new System.EventHandler(this.loadRA2ToolStripMenuItem_Click);
			// 
			// loadYRToolStripMenuItem
			// 
			this.loadYRToolStripMenuItem.Name = "loadYRToolStripMenuItem";
			this.loadYRToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
			this.loadYRToolStripMenuItem.Text = "Load &YR theaters";
			this.loadYRToolStripMenuItem.Click += new System.EventHandler(this.loadYRToolStripMenuItem_Click);
			// 
			// openFileDialog1
			// 
			this.openFileDialog1.DefaultExt = "xml";
			this.openFileDialog1.FileName = "modconfig.xml";
			this.openFileDialog1.Filter = "Xml files (*.xml)|*.xml|All files (*.*)|*";
			// 
			// saveFileDialog1
			// 
			this.saveFileDialog1.DefaultExt = "xml";
			this.saveFileDialog1.FileName = "modconfig.xml";
			this.saveFileDialog1.Filter = "Xml files (*.xml)|*.xml|All files (*.*)|*";
			// 
			// btnAccept
			// 
			this.btnAccept.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnAccept.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnAccept.Location = new System.Drawing.Point(471, 386);
			this.btnAccept.Name = "btnAccept";
			this.btnAccept.Size = new System.Drawing.Size(55, 23);
			this.btnAccept.TabIndex = 3;
			this.btnAccept.Text = "Ok";
			this.btnAccept.UseVisualStyleBackColor = true;
			// 
			// btnLoadRA2Theaters
			// 
			this.btnLoadRA2Theaters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnLoadRA2Theaters.Location = new System.Drawing.Point(130, 386);
			this.btnLoadRA2Theaters.Name = "btnLoadRA2Theaters";
			this.btnLoadRA2Theaters.Size = new System.Drawing.Size(120, 23);
			this.btnLoadRA2Theaters.TabIndex = 4;
			this.btnLoadRA2Theaters.Text = "Load RA2 Theaters";
			this.btnLoadRA2Theaters.UseVisualStyleBackColor = true;
			this.btnLoadRA2Theaters.Click += new System.EventHandler(this.btnLoadRA2Theaters_Click);
			// 
			// btnLoadYRTheaters
			// 
			this.btnLoadYRTheaters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnLoadYRTheaters.Location = new System.Drawing.Point(256, 386);
			this.btnLoadYRTheaters.Name = "btnLoadYRTheaters";
			this.btnLoadYRTheaters.Size = new System.Drawing.Size(102, 23);
			this.btnLoadYRTheaters.TabIndex = 5;
			this.btnLoadYRTheaters.Text = "Load YR theaters";
			this.btnLoadYRTheaters.UseVisualStyleBackColor = true;
			this.btnLoadYRTheaters.Click += new System.EventHandler(this.btnLoadYRTheaters_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(402, 386);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(63, 23);
			this.btnCancel.TabIndex = 6;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnLoadTSTheaters
			// 
			this.btnLoadTSTheaters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnLoadTSTheaters.Location = new System.Drawing.Point(12, 386);
			this.btnLoadTSTheaters.Name = "btnLoadTSTheaters";
			this.btnLoadTSTheaters.Size = new System.Drawing.Size(112, 23);
			this.btnLoadTSTheaters.TabIndex = 7;
			this.btnLoadTSTheaters.Text = "Load TS Theaters";
			this.btnLoadTSTheaters.UseVisualStyleBackColor = true;
			this.btnLoadTSTheaters.Click += new System.EventHandler(this.btnLoadTSTheaters_Click);
			// 
			// ModConfigEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(538, 412);
			this.Controls.Add(this.btnLoadTSTheaters);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnLoadYRTheaters);
			this.Controls.Add(this.btnLoadRA2Theaters);
			this.Controls.Add(this.btnAccept);
			this.Controls.Add(this.propertyGrid1);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.MinimumSize = new System.Drawing.Size(554, 450);
			this.Name = "ModConfigEditor";
			this.Text = "Mod configuration editor";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ModConfigEditor_FormClosing);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PropertyGrid propertyGrid1;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem defaultsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem loadTSToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem loadRA2ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem loadYRToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.SaveFileDialog saveFileDialog1;
		private System.Windows.Forms.Button btnAccept;
		private System.Windows.Forms.Button btnLoadRA2Theaters;
		private System.Windows.Forms.Button btnLoadYRTheaters;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnLoadTSTheaters;
	}
}