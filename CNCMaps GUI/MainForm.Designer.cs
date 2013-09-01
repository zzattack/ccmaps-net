namespace CNCMaps.GUI {
	partial class MainForm {
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.gbMiscOptions = new System.Windows.Forms.GroupBox();
			this.cbOmitSquareMarkers = new System.Windows.Forms.CheckBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.rbPreferHardwareRendering = new System.Windows.Forms.RadioButton();
			this.rbPreferSoftwareRendering = new System.Windows.Forms.RadioButton();
			this.cbReplacePreview = new System.Windows.Forms.CheckBox();
			this.pnlMapSize = new System.Windows.Forms.Panel();
			this.rbSizeLocal = new System.Windows.Forms.RadioButton();
			this.rbSizeFullmap = new System.Windows.Forms.RadioButton();
			this.pnlEngine = new System.Windows.Forms.Panel();
			this.rbEngineFS = new System.Windows.Forms.RadioButton();
			this.rbEngineTS = new System.Windows.Forms.RadioButton();
			this.rbEngineAuto = new System.Windows.Forms.RadioButton();
			this.rbEngineRA2 = new System.Windows.Forms.RadioButton();
			this.rbEngineYR = new System.Windows.Forms.RadioButton();
			this.lblTiledSquaredPosDescription = new System.Windows.Forms.Label();
			this.lblSquaredStartPosDescription = new System.Windows.Forms.Label();
			this.lblOreEmphasisDescription = new System.Windows.Forms.Label();
			this.cbSquaredStartPositions = new System.Windows.Forms.CheckBox();
			this.cbTiledStartPositions = new System.Windows.Forms.CheckBox();
			this.cbEmphasizeOre = new System.Windows.Forms.CheckBox();
			this.lblCompression = new System.Windows.Forms.GroupBox();
			this.tbCustomOutput = new System.Windows.Forms.TextBox();
			this.tbRenderProg = new System.Windows.Forms.TextBox();
			this.lblAutoFilenameDescription = new System.Windows.Forms.Label();
			this.lblMapRenderer = new System.Windows.Forms.Label();
			this.rbCustomFilename = new System.Windows.Forms.RadioButton();
			this.btnBrowseRenderer = new System.Windows.Forms.Button();
			this.rbAutoFilename = new System.Windows.Forms.RadioButton();
			this.tbMixDir = new System.Windows.Forms.TextBox();
			this.lblMixFiles = new System.Windows.Forms.Label();
			this.btnBrowseMixDir = new System.Windows.Forms.Button();
			this.lblQuality = new System.Windows.Forms.Label();
			this.tbInput = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.lblInputMap = new System.Windows.Forms.Label();
			this.nudEncodingQuality = new System.Windows.Forms.NumericUpDown();
			this.btnBrowseInput = new System.Windows.Forms.Button();
			this.cbOutputJPG = new System.Windows.Forms.CheckBox();
			this.cbOutputPNG = new System.Windows.Forms.CheckBox();
			this.nudCompression = new System.Windows.Forms.NumericUpDown();
			this.btnRenderExecute = new System.Windows.Forms.Button();
			this.tbCommandPreview = new System.Windows.Forms.TextBox();
			this.lblCommand = new System.Windows.Forms.Label();
			this.ofd = new System.Windows.Forms.OpenFileDialog();
			this.cbLog = new System.Windows.Forms.GroupBox();
			this.rtbLog = new System.Windows.Forms.RichTextBox();
			this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
			this.lblFill = new System.Windows.Forms.ToolStripStatusLabel();
			this.pbProgress = new System.Windows.Forms.ToolStripProgressBar();
			this.gbMiscOptions.SuspendLayout();
			this.panel1.SuspendLayout();
			this.pnlMapSize.SuspendLayout();
			this.pnlEngine.SuspendLayout();
			this.lblCompression.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.nudEncodingQuality)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nudCompression)).BeginInit();
			this.cbLog.SuspendLayout();
			this.statusStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// gbMiscOptions
			// 
			this.gbMiscOptions.Controls.Add(this.cbOmitSquareMarkers);
			this.gbMiscOptions.Controls.Add(this.panel1);
			this.gbMiscOptions.Controls.Add(this.cbReplacePreview);
			this.gbMiscOptions.Controls.Add(this.pnlMapSize);
			this.gbMiscOptions.Controls.Add(this.pnlEngine);
			this.gbMiscOptions.Controls.Add(this.lblTiledSquaredPosDescription);
			this.gbMiscOptions.Controls.Add(this.lblSquaredStartPosDescription);
			this.gbMiscOptions.Controls.Add(this.lblOreEmphasisDescription);
			this.gbMiscOptions.Controls.Add(this.cbSquaredStartPositions);
			this.gbMiscOptions.Controls.Add(this.cbTiledStartPositions);
			this.gbMiscOptions.Controls.Add(this.cbEmphasizeOre);
			this.gbMiscOptions.Location = new System.Drawing.Point(12, 209);
			this.gbMiscOptions.Name = "gbMiscOptions";
			this.gbMiscOptions.Size = new System.Drawing.Size(541, 267);
			this.gbMiscOptions.TabIndex = 1;
			this.gbMiscOptions.TabStop = false;
			this.gbMiscOptions.Text = "Misc. Options";
			this.gbMiscOptions.DragDrop += new System.Windows.Forms.DragEventHandler(this.InputDragDrop);
			this.gbMiscOptions.DragEnter += new System.Windows.Forms.DragEventHandler(this.InputDragEnter);
			// 
			// cbOmitSquareMarkers
			// 
			this.cbOmitSquareMarkers.AutoSize = true;
			this.cbOmitSquareMarkers.Location = new System.Drawing.Point(319, 206);
			this.cbOmitSquareMarkers.Name = "cbOmitSquareMarkers";
			this.cbOmitSquareMarkers.Size = new System.Drawing.Size(122, 17);
			this.cbOmitSquareMarkers.TabIndex = 17;
			this.cbOmitSquareMarkers.Text = "Omit square markers";
			this.cbOmitSquareMarkers.UseVisualStyleBackColor = true;
			this.cbOmitSquareMarkers.CheckedChanged += new System.EventHandler(this.UIChanged);
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.rbPreferHardwareRendering);
			this.panel1.Controls.Add(this.rbPreferSoftwareRendering);
			this.panel1.Location = new System.Drawing.Point(17, 226);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(461, 25);
			this.panel1.TabIndex = 16;
			// 
			// rbPreferHardwareRendering
			// 
			this.rbPreferHardwareRendering.AutoSize = true;
			this.rbPreferHardwareRendering.Checked = true;
			this.rbPreferHardwareRendering.Location = new System.Drawing.Point(12, 4);
			this.rbPreferHardwareRendering.Name = "rbPreferHardwareRendering";
			this.rbPreferHardwareRendering.Size = new System.Drawing.Size(175, 17);
			this.rbPreferHardwareRendering.TabIndex = 10;
			this.rbPreferHardwareRendering.TabStop = true;
			this.rbPreferHardwareRendering.Text = "Prefer hardware voxel rendering";
			this.rbPreferHardwareRendering.UseVisualStyleBackColor = true;
			this.rbPreferHardwareRendering.CheckedChanged += new System.EventHandler(this.UIChanged);
			// 
			// rbPreferSoftwareRendering
			// 
			this.rbPreferSoftwareRendering.AutoSize = true;
			this.rbPreferSoftwareRendering.Location = new System.Drawing.Point(226, 3);
			this.rbPreferSoftwareRendering.Name = "rbPreferSoftwareRendering";
			this.rbPreferSoftwareRendering.Size = new System.Drawing.Size(209, 17);
			this.rbPreferSoftwareRendering.TabIndex = 11;
			this.rbPreferSoftwareRendering.Text = "Prefer software rendering (compatibility)";
			this.rbPreferSoftwareRendering.UseVisualStyleBackColor = true;
			this.rbPreferSoftwareRendering.CheckedChanged += new System.EventHandler(this.UIChanged);
			// 
			// cbReplacePreview
			// 
			this.cbReplacePreview.AutoSize = true;
			this.cbReplacePreview.Location = new System.Drawing.Point(29, 206);
			this.cbReplacePreview.Name = "cbReplacePreview";
			this.cbReplacePreview.Size = new System.Drawing.Size(284, 17);
			this.cbReplacePreview.TabIndex = 15;
			this.cbReplacePreview.Text = "Replace map preview with thumbnail of resulting image";
			this.cbReplacePreview.UseVisualStyleBackColor = true;
			this.cbReplacePreview.CheckedChanged += new System.EventHandler(this.CbReplacePreviewCheckedChanged);
			// 
			// pnlMapSize
			// 
			this.pnlMapSize.Controls.Add(this.rbSizeLocal);
			this.pnlMapSize.Controls.Add(this.rbSizeFullmap);
			this.pnlMapSize.Location = new System.Drawing.Point(17, 175);
			this.pnlMapSize.Name = "pnlMapSize";
			this.pnlMapSize.Size = new System.Drawing.Size(461, 26);
			this.pnlMapSize.TabIndex = 14;
			// 
			// rbSizeLocal
			// 
			this.rbSizeLocal.AutoSize = true;
			this.rbSizeLocal.Checked = true;
			this.rbSizeLocal.Location = new System.Drawing.Point(12, 4);
			this.rbSizeLocal.Name = "rbSizeLocal";
			this.rbSizeLocal.Size = new System.Drawing.Size(110, 17);
			this.rbSizeLocal.TabIndex = 10;
			this.rbSizeLocal.TabStop = true;
			this.rbSizeLocal.Text = "Use map localsize";
			this.rbSizeLocal.UseVisualStyleBackColor = true;
			this.rbSizeLocal.CheckedChanged += new System.EventHandler(this.UIChanged);
			// 
			// rbSizeFullmap
			// 
			this.rbSizeFullmap.AutoSize = true;
			this.rbSizeFullmap.Location = new System.Drawing.Point(154, 4);
			this.rbSizeFullmap.Name = "rbSizeFullmap";
			this.rbSizeFullmap.Size = new System.Drawing.Size(175, 17);
			this.rbSizeFullmap.TabIndex = 11;
			this.rbSizeFullmap.Text = "Use full size (useful for missions)";
			this.rbSizeFullmap.UseVisualStyleBackColor = true;
			this.rbSizeFullmap.CheckedChanged += new System.EventHandler(this.UIChanged);
			// 
			// pnlEngine
			// 
			this.pnlEngine.Controls.Add(this.rbEngineFS);
			this.pnlEngine.Controls.Add(this.rbEngineTS);
			this.pnlEngine.Controls.Add(this.rbEngineAuto);
			this.pnlEngine.Controls.Add(this.rbEngineRA2);
			this.pnlEngine.Controls.Add(this.rbEngineYR);
			this.pnlEngine.Location = new System.Drawing.Point(17, 145);
			this.pnlEngine.Name = "pnlEngine";
			this.pnlEngine.Size = new System.Drawing.Size(461, 25);
			this.pnlEngine.TabIndex = 13;
			// 
			// rbEngineFS
			// 
			this.rbEngineFS.AutoSize = true;
			this.rbEngineFS.Location = new System.Drawing.Point(383, 4);
			this.rbEngineFS.Name = "rbEngineFS";
			this.rbEngineFS.Size = new System.Drawing.Size(68, 17);
			this.rbEngineFS.TabIndex = 14;
			this.rbEngineFS.Text = "Force FS";
			this.rbEngineFS.UseVisualStyleBackColor = true;
			this.rbEngineFS.CheckedChanged += new System.EventHandler(this.RbEngineCheckedChanged);
			// 
			// rbEngineTS
			// 
			this.rbEngineTS.AutoSize = true;
			this.rbEngineTS.Location = new System.Drawing.Point(308, 3);
			this.rbEngineTS.Name = "rbEngineTS";
			this.rbEngineTS.Size = new System.Drawing.Size(69, 17);
			this.rbEngineTS.TabIndex = 13;
			this.rbEngineTS.Text = "Force TS";
			this.rbEngineTS.UseVisualStyleBackColor = true;
			this.rbEngineTS.CheckedChanged += new System.EventHandler(this.RbEngineCheckedChanged);
			// 
			// rbEngineAuto
			// 
			this.rbEngineAuto.AutoSize = true;
			this.rbEngineAuto.Checked = true;
			this.rbEngineAuto.Location = new System.Drawing.Point(12, 4);
			this.rbEngineAuto.Name = "rbEngineAuto";
			this.rbEngineAuto.Size = new System.Drawing.Size(132, 17);
			this.rbEngineAuto.TabIndex = 10;
			this.rbEngineAuto.TabStop = true;
			this.rbEngineAuto.Text = "Automatic engine rules";
			this.rbEngineAuto.UseVisualStyleBackColor = true;
			this.rbEngineAuto.CheckedChanged += new System.EventHandler(this.RbEngineCheckedChanged);
			// 
			// rbEngineRA2
			// 
			this.rbEngineRA2.AutoSize = true;
			this.rbEngineRA2.Location = new System.Drawing.Point(226, 4);
			this.rbEngineRA2.Name = "rbEngineRA2";
			this.rbEngineRA2.Size = new System.Drawing.Size(76, 17);
			this.rbEngineRA2.TabIndex = 12;
			this.rbEngineRA2.Text = "Force RA2";
			this.rbEngineRA2.UseVisualStyleBackColor = true;
			this.rbEngineRA2.CheckedChanged += new System.EventHandler(this.RbEngineCheckedChanged);
			// 
			// rbEngineYR
			// 
			this.rbEngineYR.AutoSize = true;
			this.rbEngineYR.Location = new System.Drawing.Point(150, 3);
			this.rbEngineYR.Name = "rbEngineYR";
			this.rbEngineYR.Size = new System.Drawing.Size(70, 17);
			this.rbEngineYR.TabIndex = 11;
			this.rbEngineYR.Text = "Force YR";
			this.rbEngineYR.UseVisualStyleBackColor = true;
			this.rbEngineYR.CheckedChanged += new System.EventHandler(this.RbEngineCheckedChanged);
			// 
			// lblTiledSquaredPosDescription
			// 
			this.lblTiledSquaredPosDescription.Location = new System.Drawing.Point(12, 127);
			this.lblTiledSquaredPosDescription.Name = "lblTiledSquaredPosDescription";
			this.lblTiledSquaredPosDescription.Size = new System.Drawing.Size(515, 17);
			this.lblTiledSquaredPosDescription.TabIndex = 5;
			this.lblTiledSquaredPosDescription.Text = "Gives a slightly transparent red color to the 4x4 foundation of where MCVs as ini" +
    "tially placed would deploy.";
			// 
			// lblSquaredStartPosDescription
			// 
			this.lblSquaredStartPosDescription.Location = new System.Drawing.Point(12, 89);
			this.lblSquaredStartPosDescription.Name = "lblSquaredStartPosDescription";
			this.lblSquaredStartPosDescription.Size = new System.Drawing.Size(498, 15);
			this.lblSquaredStartPosDescription.TabIndex = 3;
			this.lblSquaredStartPosDescription.Text = "Places a large red square at the starting positions. Looks good when scaling down" +
    " to preview images.";
			// 
			// lblOreEmphasisDescription
			// 
			this.lblOreEmphasisDescription.Location = new System.Drawing.Point(12, 37);
			this.lblOreEmphasisDescription.Name = "lblOreEmphasisDescription";
			this.lblOreEmphasisDescription.Size = new System.Drawing.Size(498, 32);
			this.lblOreEmphasisDescription.TabIndex = 1;
			this.lblOreEmphasisDescription.Text = resources.GetString("lblOreEmphasisDescription.Text");
			// 
			// cbSquaredStartPositions
			// 
			this.cbSquaredStartPositions.AutoSize = true;
			this.cbSquaredStartPositions.Location = new System.Drawing.Point(31, 69);
			this.cbSquaredStartPositions.Name = "cbSquaredStartPositions";
			this.cbSquaredStartPositions.Size = new System.Drawing.Size(133, 17);
			this.cbSquaredStartPositions.TabIndex = 2;
			this.cbSquaredStartPositions.Text = "Squared start positions";
			this.cbSquaredStartPositions.UseVisualStyleBackColor = true;
			this.cbSquaredStartPositions.CheckedChanged += new System.EventHandler(this.SquaredStartPosCheckedChanged);
			// 
			// cbTiledStartPositions
			// 
			this.cbTiledStartPositions.AutoSize = true;
			this.cbTiledStartPositions.Location = new System.Drawing.Point(31, 107);
			this.cbTiledStartPositions.Name = "cbTiledStartPositions";
			this.cbTiledStartPositions.Size = new System.Drawing.Size(116, 17);
			this.cbTiledStartPositions.TabIndex = 4;
			this.cbTiledStartPositions.Text = "Tiled start positions";
			this.cbTiledStartPositions.UseVisualStyleBackColor = true;
			this.cbTiledStartPositions.CheckedChanged += new System.EventHandler(this.TiledStartPosCheckedChanged);
			// 
			// cbEmphasizeOre
			// 
			this.cbEmphasizeOre.AutoSize = true;
			this.cbEmphasizeOre.Location = new System.Drawing.Point(31, 17);
			this.cbEmphasizeOre.Name = "cbEmphasizeOre";
			this.cbEmphasizeOre.Size = new System.Drawing.Size(125, 17);
			this.cbEmphasizeOre.TabIndex = 0;
			this.cbEmphasizeOre.Text = "Emphasize ore/gems";
			this.cbEmphasizeOre.UseVisualStyleBackColor = true;
			this.cbEmphasizeOre.CheckedChanged += new System.EventHandler(this.UIChanged);
			// 
			// lblCompression
			// 
			this.lblCompression.Controls.Add(this.tbCustomOutput);
			this.lblCompression.Controls.Add(this.tbRenderProg);
			this.lblCompression.Controls.Add(this.lblAutoFilenameDescription);
			this.lblCompression.Controls.Add(this.lblMapRenderer);
			this.lblCompression.Controls.Add(this.rbCustomFilename);
			this.lblCompression.Controls.Add(this.btnBrowseRenderer);
			this.lblCompression.Controls.Add(this.rbAutoFilename);
			this.lblCompression.Controls.Add(this.tbMixDir);
			this.lblCompression.Controls.Add(this.lblMixFiles);
			this.lblCompression.Controls.Add(this.btnBrowseMixDir);
			this.lblCompression.Controls.Add(this.lblQuality);
			this.lblCompression.Controls.Add(this.tbInput);
			this.lblCompression.Controls.Add(this.label1);
			this.lblCompression.Controls.Add(this.lblInputMap);
			this.lblCompression.Controls.Add(this.nudEncodingQuality);
			this.lblCompression.Controls.Add(this.btnBrowseInput);
			this.lblCompression.Controls.Add(this.cbOutputJPG);
			this.lblCompression.Controls.Add(this.cbOutputPNG);
			this.lblCompression.Controls.Add(this.nudCompression);
			this.lblCompression.Location = new System.Drawing.Point(12, 9);
			this.lblCompression.Name = "lblCompression";
			this.lblCompression.Size = new System.Drawing.Size(544, 194);
			this.lblCompression.TabIndex = 0;
			this.lblCompression.TabStop = false;
			this.lblCompression.Text = "Input";
			this.lblCompression.DragDrop += new System.Windows.Forms.DragEventHandler(this.InputDragDrop);
			this.lblCompression.DragEnter += new System.Windows.Forms.DragEventHandler(this.InputDragEnter);
			// 
			// tbCustomOutput
			// 
			this.tbCustomOutput.Location = new System.Drawing.Point(302, 142);
			this.tbCustomOutput.Name = "tbCustomOutput";
			this.tbCustomOutput.Size = new System.Drawing.Size(219, 20);
			this.tbCustomOutput.TabIndex = 10;
			this.tbCustomOutput.Visible = false;
			this.tbCustomOutput.TextChanged += new System.EventHandler(this.UIChanged);
			// 
			// tbRenderProg
			// 
			this.tbRenderProg.Location = new System.Drawing.Point(131, 67);
			this.tbRenderProg.Name = "tbRenderProg";
			this.tbRenderProg.Size = new System.Drawing.Size(298, 20);
			this.tbRenderProg.TabIndex = 7;
			this.tbRenderProg.TextChanged += new System.EventHandler(this.UIChanged);
			// 
			// lblAutoFilenameDescription
			// 
			this.lblAutoFilenameDescription.Location = new System.Drawing.Point(14, 165);
			this.lblAutoFilenameDescription.Name = "lblAutoFilenameDescription";
			this.lblAutoFilenameDescription.Size = new System.Drawing.Size(515, 17);
			this.lblAutoFilenameDescription.TabIndex = 11;
			this.lblAutoFilenameDescription.Text = "Automatic filename resolution uses CSF, missions.ini or [Basic]/Name if possible." +
    "\r\n";
			// 
			// lblMapRenderer
			// 
			this.lblMapRenderer.AutoSize = true;
			this.lblMapRenderer.Location = new System.Drawing.Point(28, 68);
			this.lblMapRenderer.Name = "lblMapRenderer";
			this.lblMapRenderer.Size = new System.Drawing.Size(102, 13);
			this.lblMapRenderer.TabIndex = 6;
			this.lblMapRenderer.Text = "Map render program";
			// 
			// rbCustomFilename
			// 
			this.rbCustomFilename.AutoSize = true;
			this.rbCustomFilename.Location = new System.Drawing.Point(173, 143);
			this.rbCustomFilename.Name = "rbCustomFilename";
			this.rbCustomFilename.Size = new System.Drawing.Size(102, 17);
			this.rbCustomFilename.TabIndex = 9;
			this.rbCustomFilename.Text = "Custom filename";
			this.rbCustomFilename.UseVisualStyleBackColor = true;
			this.rbCustomFilename.CheckedChanged += new System.EventHandler(this.OutputNameCheckedChanged);
			// 
			// btnBrowseRenderer
			// 
			this.btnBrowseRenderer.Location = new System.Drawing.Point(435, 67);
			this.btnBrowseRenderer.Name = "btnBrowseRenderer";
			this.btnBrowseRenderer.Size = new System.Drawing.Size(75, 23);
			this.btnBrowseRenderer.TabIndex = 8;
			this.btnBrowseRenderer.Text = "Browse";
			this.btnBrowseRenderer.UseVisualStyleBackColor = true;
			this.btnBrowseRenderer.Click += new System.EventHandler(this.BrowseRenderer);
			// 
			// rbAutoFilename
			// 
			this.rbAutoFilename.AutoSize = true;
			this.rbAutoFilename.Checked = true;
			this.rbAutoFilename.Location = new System.Drawing.Point(31, 143);
			this.rbAutoFilename.Name = "rbAutoFilename";
			this.rbAutoFilename.Size = new System.Drawing.Size(114, 17);
			this.rbAutoFilename.TabIndex = 8;
			this.rbAutoFilename.TabStop = true;
			this.rbAutoFilename.Text = "Automatic filename";
			this.rbAutoFilename.UseVisualStyleBackColor = true;
			this.rbAutoFilename.CheckedChanged += new System.EventHandler(this.OutputNameCheckedChanged);
			// 
			// tbMixDir
			// 
			this.tbMixDir.Location = new System.Drawing.Point(131, 41);
			this.tbMixDir.Name = "tbMixDir";
			this.tbMixDir.Size = new System.Drawing.Size(298, 20);
			this.tbMixDir.TabIndex = 4;
			this.tbMixDir.TextChanged += new System.EventHandler(this.UIChanged);
			// 
			// lblMixFiles
			// 
			this.lblMixFiles.AutoSize = true;
			this.lblMixFiles.Location = new System.Drawing.Point(28, 42);
			this.lblMixFiles.Name = "lblMixFiles";
			this.lblMixFiles.Size = new System.Drawing.Size(44, 13);
			this.lblMixFiles.TabIndex = 3;
			this.lblMixFiles.Text = "Mix files";
			// 
			// btnBrowseMixDir
			// 
			this.btnBrowseMixDir.Location = new System.Drawing.Point(435, 41);
			this.btnBrowseMixDir.Name = "btnBrowseMixDir";
			this.btnBrowseMixDir.Size = new System.Drawing.Size(75, 23);
			this.btnBrowseMixDir.TabIndex = 5;
			this.btnBrowseMixDir.Text = "Browse";
			this.btnBrowseMixDir.UseVisualStyleBackColor = true;
			this.btnBrowseMixDir.Click += new System.EventHandler(this.BrowseMixDir);
			// 
			// lblQuality
			// 
			this.lblQuality.AutoSize = true;
			this.lblQuality.Location = new System.Drawing.Point(132, 95);
			this.lblQuality.Name = "lblQuality";
			this.lblQuality.Size = new System.Drawing.Size(85, 13);
			this.lblQuality.TabIndex = 5;
			this.lblQuality.Text = "Encoding quality";
			// 
			// tbInput
			// 
			this.tbInput.Location = new System.Drawing.Point(131, 15);
			this.tbInput.Name = "tbInput";
			this.tbInput.Size = new System.Drawing.Size(298, 20);
			this.tbInput.TabIndex = 1;
			this.tbInput.TextChanged += new System.EventHandler(this.UIChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(132, 118);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(92, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Compression level";
			this.label1.Visible = false;
			// 
			// lblInputMap
			// 
			this.lblInputMap.AutoSize = true;
			this.lblInputMap.Location = new System.Drawing.Point(28, 18);
			this.lblInputMap.Name = "lblInputMap";
			this.lblInputMap.Size = new System.Drawing.Size(54, 13);
			this.lblInputMap.TabIndex = 0;
			this.lblInputMap.Text = "Input map";
			// 
			// nudEncodingQuality
			// 
			this.nudEncodingQuality.Location = new System.Drawing.Point(230, 94);
			this.nudEncodingQuality.Name = "nudEncodingQuality";
			this.nudEncodingQuality.Size = new System.Drawing.Size(43, 20);
			this.nudEncodingQuality.TabIndex = 6;
			this.nudEncodingQuality.Value = new decimal(new int[] {
            90,
            0,
            0,
            0});
			this.nudEncodingQuality.ValueChanged += new System.EventHandler(this.UIChanged);
			// 
			// btnBrowseInput
			// 
			this.btnBrowseInput.Location = new System.Drawing.Point(435, 15);
			this.btnBrowseInput.Name = "btnBrowseInput";
			this.btnBrowseInput.Size = new System.Drawing.Size(75, 23);
			this.btnBrowseInput.TabIndex = 2;
			this.btnBrowseInput.Text = "Browse";
			this.btnBrowseInput.UseVisualStyleBackColor = true;
			this.btnBrowseInput.Click += new System.EventHandler(this.BrowseInput);
			// 
			// cbOutputJPG
			// 
			this.cbOutputJPG.AutoSize = true;
			this.cbOutputJPG.Checked = true;
			this.cbOutputJPG.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbOutputJPG.Location = new System.Drawing.Point(42, 94);
			this.cbOutputJPG.Name = "cbOutputJPG";
			this.cbOutputJPG.Size = new System.Drawing.Size(81, 17);
			this.cbOutputJPG.TabIndex = 4;
			this.cbOutputJPG.Text = "Output JPG";
			this.cbOutputJPG.UseVisualStyleBackColor = true;
			this.cbOutputJPG.CheckedChanged += new System.EventHandler(this.JpegOutputCheckedChanged);
			// 
			// cbOutputPNG
			// 
			this.cbOutputPNG.AutoSize = true;
			this.cbOutputPNG.Location = new System.Drawing.Point(42, 117);
			this.cbOutputPNG.Name = "cbOutputPNG";
			this.cbOutputPNG.Size = new System.Drawing.Size(84, 17);
			this.cbOutputPNG.TabIndex = 0;
			this.cbOutputPNG.Text = "Output PNG";
			this.cbOutputPNG.UseVisualStyleBackColor = true;
			this.cbOutputPNG.CheckedChanged += new System.EventHandler(this.PngOutputCheckedChanged);
			// 
			// nudCompression
			// 
			this.nudCompression.Location = new System.Drawing.Point(230, 117);
			this.nudCompression.Maximum = new decimal(new int[] {
            9,
            0,
            0,
            0});
			this.nudCompression.Name = "nudCompression";
			this.nudCompression.Size = new System.Drawing.Size(43, 20);
			this.nudCompression.TabIndex = 2;
			this.nudCompression.Value = new decimal(new int[] {
            6,
            0,
            0,
            0});
			this.nudCompression.Visible = false;
			this.nudCompression.ValueChanged += new System.EventHandler(this.UIChanged);
			// 
			// btnRenderExecute
			// 
			this.btnRenderExecute.Location = new System.Drawing.Point(462, 482);
			this.btnRenderExecute.Name = "btnRenderExecute";
			this.btnRenderExecute.Size = new System.Drawing.Size(75, 23);
			this.btnRenderExecute.TabIndex = 5;
			this.btnRenderExecute.Text = "Render map";
			this.btnRenderExecute.UseVisualStyleBackColor = true;
			this.btnRenderExecute.Click += new System.EventHandler(this.ExecuteCommand);
			// 
			// tbCommandPreview
			// 
			this.tbCommandPreview.Location = new System.Drawing.Point(84, 482);
			this.tbCommandPreview.Name = "tbCommandPreview";
			this.tbCommandPreview.Size = new System.Drawing.Size(361, 20);
			this.tbCommandPreview.TabIndex = 4;
			// 
			// lblCommand
			// 
			this.lblCommand.Location = new System.Drawing.Point(22, 485);
			this.lblCommand.Name = "lblCommand";
			this.lblCommand.Size = new System.Drawing.Size(62, 17);
			this.lblCommand.TabIndex = 3;
			this.lblCommand.Text = "Command";
			// 
			// ofd
			// 
			this.ofd.FileName = "ofd";
			// 
			// cbLog
			// 
			this.cbLog.Controls.Add(this.rtbLog);
			this.cbLog.Location = new System.Drawing.Point(16, 506);
			this.cbLog.Name = "cbLog";
			this.cbLog.Size = new System.Drawing.Size(542, 176);
			this.cbLog.TabIndex = 6;
			this.cbLog.TabStop = false;
			this.cbLog.Text = "Log";
			this.cbLog.Visible = false;
			// 
			// rtbLog
			// 
			this.rtbLog.Location = new System.Drawing.Point(9, 19);
			this.rtbLog.Name = "rtbLog";
			this.rtbLog.ReadOnly = true;
			this.rtbLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
			this.rtbLog.Size = new System.Drawing.Size(527, 151);
			this.rtbLog.TabIndex = 0;
			this.rtbLog.Text = "";
			// 
			// statusStrip
			// 
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.lblFill,
            this.pbProgress});
			this.statusStrip.Location = new System.Drawing.Point(0, 684);
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.Size = new System.Drawing.Size(566, 22);
			this.statusStrip.TabIndex = 7;
			this.statusStrip.Text = "statusStrip1";
			// 
			// lblStatus
			// 
			this.lblStatus.Name = "lblStatus";
			this.lblStatus.Size = new System.Drawing.Size(42, 17);
			this.lblStatus.Text = "Status:";
			// 
			// lblFill
			// 
			this.lblFill.Name = "lblFill";
			this.lblFill.Size = new System.Drawing.Size(376, 17);
			this.lblFill.Spring = true;
			// 
			// pbProgress
			// 
			this.pbProgress.Name = "pbProgress";
			this.pbProgress.Size = new System.Drawing.Size(100, 16);
			// 
			// MainForm
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(566, 706);
			this.Controls.Add(this.statusStrip);
			this.Controls.Add(this.cbLog);
			this.Controls.Add(this.lblCommand);
			this.Controls.Add(this.tbCommandPreview);
			this.Controls.Add(this.btnRenderExecute);
			this.Controls.Add(this.gbMiscOptions);
			this.Controls.Add(this.lblCompression);
			this.Name = "MainForm";
			this.Text = "Red Alert 2 and Tiberian Sun map renderer";
			this.Load += new System.EventHandler(this.MainFormLoad);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.InputDragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.InputDragEnter);
			this.gbMiscOptions.ResumeLayout(false);
			this.gbMiscOptions.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.pnlMapSize.ResumeLayout(false);
			this.pnlMapSize.PerformLayout();
			this.pnlEngine.ResumeLayout(false);
			this.pnlEngine.PerformLayout();
			this.lblCompression.ResumeLayout(false);
			this.lblCompression.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.nudEncodingQuality)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nudCompression)).EndInit();
			this.cbLog.ResumeLayout(false);
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox gbMiscOptions;
		private System.Windows.Forms.CheckBox cbTiledStartPositions;
		private System.Windows.Forms.CheckBox cbEmphasizeOre;
		private System.Windows.Forms.CheckBox cbSquaredStartPositions;
		private System.Windows.Forms.Label lblOreEmphasisDescription;
		private System.Windows.Forms.Label lblTiledSquaredPosDescription;
		private System.Windows.Forms.Label lblSquaredStartPosDescription;
		private System.Windows.Forms.GroupBox lblCompression;
		private System.Windows.Forms.Button btnBrowseInput;
		private System.Windows.Forms.TextBox tbRenderProg;
		private System.Windows.Forms.Label lblMapRenderer;
		private System.Windows.Forms.Button btnBrowseRenderer;
		private System.Windows.Forms.TextBox tbMixDir;
		private System.Windows.Forms.Label lblMixFiles;
		private System.Windows.Forms.Button btnBrowseMixDir;
		private System.Windows.Forms.TextBox tbInput;
		private System.Windows.Forms.Label lblInputMap;
		private System.Windows.Forms.Button btnRenderExecute;
		private System.Windows.Forms.TextBox tbCommandPreview;
		private System.Windows.Forms.Label lblCommand;
		private System.Windows.Forms.OpenFileDialog ofd;
		private System.Windows.Forms.GroupBox cbLog;
		private System.Windows.Forms.RichTextBox rtbLog;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
		private System.Windows.Forms.TextBox tbCustomOutput;
		private System.Windows.Forms.Label lblAutoFilenameDescription;
		private System.Windows.Forms.RadioButton rbCustomFilename;
		private System.Windows.Forms.RadioButton rbAutoFilename;
		private System.Windows.Forms.Label lblQuality;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown nudEncodingQuality;
		private System.Windows.Forms.CheckBox cbOutputJPG;
		private System.Windows.Forms.CheckBox cbOutputPNG;
		private System.Windows.Forms.NumericUpDown nudCompression;
		private System.Windows.Forms.Panel pnlMapSize;
		private System.Windows.Forms.RadioButton rbSizeLocal;
		private System.Windows.Forms.RadioButton rbSizeFullmap;
		private System.Windows.Forms.Panel pnlEngine;
		private System.Windows.Forms.RadioButton rbEngineAuto;
		private System.Windows.Forms.RadioButton rbEngineRA2;
		private System.Windows.Forms.RadioButton rbEngineYR;
		private System.Windows.Forms.RadioButton rbEngineFS;
		private System.Windows.Forms.RadioButton rbEngineTS;
		private System.Windows.Forms.CheckBox cbReplacePreview;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.RadioButton rbPreferHardwareRendering;
		private System.Windows.Forms.RadioButton rbPreferSoftwareRendering;
		private System.Windows.Forms.CheckBox cbOmitSquareMarkers;
		private System.Windows.Forms.StatusStrip statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel lblFill;
		private System.Windows.Forms.ToolStripProgressBar pbProgress;
		private System.Windows.Forms.ToolStripStatusLabel lblStatus;

	}
}

