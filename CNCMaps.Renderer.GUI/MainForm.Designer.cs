using System.Windows.Forms;

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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.gbMiscOptions = new System.Windows.Forms.GroupBox();
			this.lblFixupTilesDesc = new System.Windows.Forms.Label();
			this.ckbFixupTiles = new System.Windows.Forms.CheckBox();
			this.gbSizeMode = new System.Windows.Forms.GroupBox();
			this.rbSizeAuto = new System.Windows.Forms.RadioButton();
			this.rbSizeFullmap = new System.Windows.Forms.RadioButton();
			this.rbSizeLocal = new System.Windows.Forms.RadioButton();
			this.gbThumbs = new System.Windows.Forms.GroupBox();
			this.cbOmitSquareMarkers = new System.Windows.Forms.CheckBox();
			this.cbReplacePreview = new System.Windows.Forms.CheckBox();
			this.gbVoxelsMode = new System.Windows.Forms.GroupBox();
			this.rbPreferSoftwareRendering = new System.Windows.Forms.RadioButton();
			this.rbPreferHardwareRendering = new System.Windows.Forms.RadioButton();
			this.lblTiledSquaredPosDescription = new System.Windows.Forms.Label();
			this.lblSquaredStartPosDescription = new System.Windows.Forms.Label();
			this.lblOreEmphasisDescription = new System.Windows.Forms.Label();
			this.cbSquaredStartPositions = new System.Windows.Forms.CheckBox();
			this.cbTiledStartPositions = new System.Windows.Forms.CheckBox();
			this.cbEmphasizeOre = new System.Windows.Forms.CheckBox();
			this.pnlEngine = new System.Windows.Forms.Panel();
			this.rbEngineFS = new System.Windows.Forms.RadioButton();
			this.rbEngineTS = new System.Windows.Forms.RadioButton();
			this.rbEngineAuto = new System.Windows.Forms.RadioButton();
			this.rbEngineRA2 = new System.Windows.Forms.RadioButton();
			this.rbEngineYR = new System.Windows.Forms.RadioButton();
			this.lbEngine = new System.Windows.Forms.Label();
			this.gbInputOutput = new System.Windows.Forms.GroupBox();
			this.tbCustomOutput = new System.Windows.Forms.TextBox();
			this.rbCustomFilename = new System.Windows.Forms.RadioButton();
			this.rbAutoFilename = new System.Windows.Forms.RadioButton();
			this.lblMarkersType = new System.Windows.Forms.Label();
			this.cbMarkersType = new System.Windows.Forms.ComboBox();
			this.cbPreserveThumbAspect = new System.Windows.Forms.CheckBox();
			this.tbThumbDimensions = new System.Windows.Forms.TextBox();
			this.cbOutputThumbnail = new System.Windows.Forms.CheckBox();
			this.btnModEditor = new System.Windows.Forms.Button();
			this.tbModConfig = new System.Windows.Forms.TextBox();
			this.cbModConfig = new System.Windows.Forms.CheckBox();
			this.tbMixDir = new System.Windows.Forms.TextBox();
			this.lblMixFiles = new System.Windows.Forms.Label();
			this.btnBrowseMixDir = new System.Windows.Forms.Button();
			this.lblQuality = new System.Windows.Forms.Label();
			this.tbInput = new System.Windows.Forms.TextBox();
			this.lblCompressionLevel = new System.Windows.Forms.Label();
			this.lblInputMap = new System.Windows.Forms.Label();
			this.nudEncodingQuality = new System.Windows.Forms.NumericUpDown();
			this.btnBrowseInput = new System.Windows.Forms.Button();
			this.cbOutputJPG = new System.Windows.Forms.CheckBox();
			this.cbOutputPNG = new System.Windows.Forms.CheckBox();
			this.nudCompression = new System.Windows.Forms.NumericUpDown();
			this.btnRenderExecute = new System.Windows.Forms.Button();
			this.tbCommandPreview = new System.Windows.Forms.TextBox();
			this.ofd = new System.Windows.Forms.OpenFileDialog();
			this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
			this.lblFill = new System.Windows.Forms.ToolStripStatusLabel();
			this.pbProgress = new System.Windows.Forms.ToolStripProgressBar();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.tabControl = new System.Windows.Forms.TabControl();
			this.tpMain = new System.Windows.Forms.TabPage();
			this.lblCommand = new System.Windows.Forms.Label();
			this.tpMisc = new System.Windows.Forms.TabPage();
			this.tpLog = new System.Windows.Forms.TabPage();
			this.gbLog = new System.Windows.Forms.GroupBox();
			this.rtbLog = new System.Windows.Forms.RichTextBox();
			this.tpAbout = new System.Windows.Forms.TabPage();
			this.label1 = new System.Windows.Forms.Label();
			this.gbMiscOptions.SuspendLayout();
			this.gbSizeMode.SuspendLayout();
			this.gbThumbs.SuspendLayout();
			this.gbVoxelsMode.SuspendLayout();
			this.pnlEngine.SuspendLayout();
			this.gbInputOutput.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.nudEncodingQuality)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nudCompression)).BeginInit();
			this.statusStrip.SuspendLayout();
			this.tabControl.SuspendLayout();
			this.tpMain.SuspendLayout();
			this.tpMisc.SuspendLayout();
			this.tpLog.SuspendLayout();
			this.gbLog.SuspendLayout();
			this.tpAbout.SuspendLayout();
			this.SuspendLayout();
			// 
			// gbMiscOptions
			// 
			this.gbMiscOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.gbMiscOptions.Controls.Add(this.lblFixupTilesDesc);
			this.gbMiscOptions.Controls.Add(this.ckbFixupTiles);
			this.gbMiscOptions.Controls.Add(this.gbSizeMode);
			this.gbMiscOptions.Controls.Add(this.gbThumbs);
			this.gbMiscOptions.Controls.Add(this.gbVoxelsMode);
			this.gbMiscOptions.Controls.Add(this.lblTiledSquaredPosDescription);
			this.gbMiscOptions.Controls.Add(this.lblSquaredStartPosDescription);
			this.gbMiscOptions.Controls.Add(this.lblOreEmphasisDescription);
			this.gbMiscOptions.Controls.Add(this.cbSquaredStartPositions);
			this.gbMiscOptions.Controls.Add(this.cbTiledStartPositions);
			this.gbMiscOptions.Controls.Add(this.cbEmphasizeOre);
			this.gbMiscOptions.Location = new System.Drawing.Point(6, 6);
			this.gbMiscOptions.Name = "gbMiscOptions";
			this.gbMiscOptions.Size = new System.Drawing.Size(583, 275);
			this.gbMiscOptions.TabIndex = 1;
			this.gbMiscOptions.TabStop = false;
			this.gbMiscOptions.Text = "Misc. Options";
			this.gbMiscOptions.DragDrop += new System.Windows.Forms.DragEventHandler(this.InputDragDrop);
			this.gbMiscOptions.DragEnter += new System.Windows.Forms.DragEventHandler(this.InputDragEnter);
			// 
			// lblFixupTilesDesc
			// 
			this.lblFixupTilesDesc.Location = new System.Drawing.Point(12, 250);
			this.lblFixupTilesDesc.Name = "lblFixupTilesDesc";
			this.lblFixupTilesDesc.Size = new System.Drawing.Size(515, 17);
			this.lblFixupTilesDesc.TabIndex = 22;
			this.lblFixupTilesDesc.Text = "Remove undefined tiles and overwrite IsoMapPack5 section in map";
			// 
			// ckbFixupTiles
			// 
			this.ckbFixupTiles.AutoSize = true;
			this.ckbFixupTiles.Location = new System.Drawing.Point(29, 234);
			this.ckbFixupTiles.Name = "ckbFixupTiles";
			this.ckbFixupTiles.Size = new System.Drawing.Size(72, 17);
			this.ckbFixupTiles.TabIndex = 21;
			this.ckbFixupTiles.Text = "Fixup tiles";
			this.ckbFixupTiles.UseVisualStyleBackColor = true;
			this.ckbFixupTiles.CheckedChanged += new System.EventHandler(this.UIChanged);
			// 
			// gbSizeMode
			// 
			this.gbSizeMode.Controls.Add(this.rbSizeAuto);
			this.gbSizeMode.Controls.Add(this.rbSizeFullmap);
			this.gbSizeMode.Controls.Add(this.rbSizeLocal);
			this.gbSizeMode.Location = new System.Drawing.Point(13, 183);
			this.gbSizeMode.Name = "gbSizeMode";
			this.gbSizeMode.Size = new System.Drawing.Size(496, 26);
			this.gbSizeMode.TabIndex = 18;
			this.gbSizeMode.TabStop = false;
			// 
			// rbSizeAuto
			// 
			this.rbSizeAuto.AutoSize = true;
			this.rbSizeAuto.Checked = true;
			this.rbSizeAuto.Location = new System.Drawing.Point(13, 7);
			this.rbSizeAuto.Name = "rbSizeAuto";
			this.rbSizeAuto.Size = new System.Drawing.Size(68, 17);
			this.rbSizeAuto.TabIndex = 12;
			this.rbSizeAuto.TabStop = true;
			this.rbSizeAuto.Text = "Auto size";
			this.toolTip.SetToolTip(this.rbSizeAuto, "Saves the portion of the map that is visible in game.");
			this.rbSizeAuto.UseVisualStyleBackColor = true;
			// 
			// rbSizeFullmap
			// 
			this.rbSizeFullmap.AutoSize = true;
			this.rbSizeFullmap.Location = new System.Drawing.Point(230, 7);
			this.rbSizeFullmap.Name = "rbSizeFullmap";
			this.rbSizeFullmap.Size = new System.Drawing.Size(175, 17);
			this.rbSizeFullmap.TabIndex = 11;
			this.rbSizeFullmap.Text = "Use full size (useful for missions)";
			this.toolTip.SetToolTip(this.rbSizeFullmap, "Saves the entire map without cutting off the parts outside the LocalSize entry.\r\n" +
        "This is especially useful for campaign maps where the map expands after\r\nachievi" +
        "ng some objective.");
			this.rbSizeFullmap.UseVisualStyleBackColor = true;
			this.rbSizeFullmap.CheckedChanged += new System.EventHandler(this.UIChanged);
			// 
			// rbSizeLocal
			// 
			this.rbSizeLocal.AutoSize = true;
			this.rbSizeLocal.Location = new System.Drawing.Point(114, 7);
			this.rbSizeLocal.Name = "rbSizeLocal";
			this.rbSizeLocal.Size = new System.Drawing.Size(110, 17);
			this.rbSizeLocal.TabIndex = 10;
			this.rbSizeLocal.Text = "Use map localsize";
			this.toolTip.SetToolTip(this.rbSizeLocal, "Saves the portion of the map that is visible in game.");
			this.rbSizeLocal.UseVisualStyleBackColor = true;
			this.rbSizeLocal.CheckedChanged += new System.EventHandler(this.UIChanged);
			// 
			// gbThumbs
			// 
			this.gbThumbs.Controls.Add(this.cbOmitSquareMarkers);
			this.gbThumbs.Controls.Add(this.cbReplacePreview);
			this.gbThumbs.Location = new System.Drawing.Point(13, 140);
			this.gbThumbs.Name = "gbThumbs";
			this.gbThumbs.Size = new System.Drawing.Size(497, 39);
			this.gbThumbs.TabIndex = 20;
			this.gbThumbs.TabStop = false;
			this.gbThumbs.Text = "Thumbnail injection";
			// 
			// cbOmitSquareMarkers
			// 
			this.cbOmitSquareMarkers.AutoSize = true;
			this.cbOmitSquareMarkers.Location = new System.Drawing.Point(299, 15);
			this.cbOmitSquareMarkers.Name = "cbOmitSquareMarkers";
			this.cbOmitSquareMarkers.Size = new System.Drawing.Size(122, 17);
			this.cbOmitSquareMarkers.TabIndex = 17;
			this.cbOmitSquareMarkers.Text = "Omit square markers";
			this.cbOmitSquareMarkers.UseVisualStyleBackColor = true;
			this.cbOmitSquareMarkers.CheckedChanged += new System.EventHandler(this.UIChanged);
			// 
			// cbReplacePreview
			// 
			this.cbReplacePreview.AutoSize = true;
			this.cbReplacePreview.Location = new System.Drawing.Point(9, 15);
			this.cbReplacePreview.Name = "cbReplacePreview";
			this.cbReplacePreview.Size = new System.Drawing.Size(284, 17);
			this.cbReplacePreview.TabIndex = 15;
			this.cbReplacePreview.Text = "Replace map preview with thumbnail of resulting image";
			this.toolTip.SetToolTip(this.cbReplacePreview, resources.GetString("cbReplacePreview.ToolTip"));
			this.cbReplacePreview.UseVisualStyleBackColor = true;
			this.cbReplacePreview.CheckedChanged += new System.EventHandler(this.CbReplacePreviewCheckedChanged);
			// 
			// gbVoxelsMode
			// 
			this.gbVoxelsMode.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.gbVoxelsMode.Controls.Add(this.rbPreferSoftwareRendering);
			this.gbVoxelsMode.Controls.Add(this.rbPreferHardwareRendering);
			this.gbVoxelsMode.Location = new System.Drawing.Point(13, 204);
			this.gbVoxelsMode.Name = "gbVoxelsMode";
			this.gbVoxelsMode.Size = new System.Drawing.Size(496, 26);
			this.gbVoxelsMode.TabIndex = 19;
			this.gbVoxelsMode.TabStop = false;
			// 
			// rbPreferSoftwareRendering
			// 
			this.rbPreferSoftwareRendering.AutoSize = true;
			this.rbPreferSoftwareRendering.Location = new System.Drawing.Point(230, 7);
			this.rbPreferSoftwareRendering.Name = "rbPreferSoftwareRendering";
			this.rbPreferSoftwareRendering.Size = new System.Drawing.Size(209, 17);
			this.rbPreferSoftwareRendering.TabIndex = 11;
			this.rbPreferSoftwareRendering.Text = "Prefer software rendering (compatibility)";
			this.toolTip.SetToolTip(this.rbPreferSoftwareRendering, "Attempts to render voxels using a software implementation. If you have many\r\nvoxe" +
        "ls on your map, this will likely be slower, although no additional startup\r\ntime" +
        " is incurred over hardware rendering. ");
			this.rbPreferSoftwareRendering.UseVisualStyleBackColor = true;
			this.rbPreferSoftwareRendering.CheckedChanged += new System.EventHandler(this.UIChanged);
			// 
			// rbPreferHardwareRendering
			// 
			this.rbPreferHardwareRendering.AutoSize = true;
			this.rbPreferHardwareRendering.Checked = true;
			this.rbPreferHardwareRendering.Location = new System.Drawing.Point(13, 7);
			this.rbPreferHardwareRendering.Name = "rbPreferHardwareRendering";
			this.rbPreferHardwareRendering.Size = new System.Drawing.Size(175, 17);
			this.rbPreferHardwareRendering.TabIndex = 10;
			this.rbPreferHardwareRendering.TabStop = true;
			this.rbPreferHardwareRendering.Text = "Prefer hardware voxel rendering";
			this.toolTip.SetToolTip(this.rbPreferHardwareRendering, "Attempts to render voxels on the hardware graphics card. If you have many\r\nvoxels" +
        " on your map, this will likely be faster, although some additional startup\r\ntime" +
        " is incurred over software rendering. ");
			this.rbPreferHardwareRendering.UseVisualStyleBackColor = true;
			this.rbPreferHardwareRendering.CheckedChanged += new System.EventHandler(this.UIChanged);
			// 
			// lblTiledSquaredPosDescription
			// 
			this.lblTiledSquaredPosDescription.Location = new System.Drawing.Point(12, 118);
			this.lblTiledSquaredPosDescription.Name = "lblTiledSquaredPosDescription";
			this.lblTiledSquaredPosDescription.Size = new System.Drawing.Size(515, 17);
			this.lblTiledSquaredPosDescription.TabIndex = 5;
			this.lblTiledSquaredPosDescription.Text = "Gives a slightly transparent red color to the 4x4 foundation of where MCVs as ini" +
    "tially placed would deploy.";
			// 
			// lblSquaredStartPosDescription
			// 
			this.lblSquaredStartPosDescription.Location = new System.Drawing.Point(12, 83);
			this.lblSquaredStartPosDescription.Name = "lblSquaredStartPosDescription";
			this.lblSquaredStartPosDescription.Size = new System.Drawing.Size(498, 15);
			this.lblSquaredStartPosDescription.TabIndex = 3;
			this.lblSquaredStartPosDescription.Text = "Places a large red square at the starting positions. Looks good when scaling down" +
    " to preview images.";
			// 
			// lblOreEmphasisDescription
			// 
			this.lblOreEmphasisDescription.Location = new System.Drawing.Point(12, 35);
			this.lblOreEmphasisDescription.Name = "lblOreEmphasisDescription";
			this.lblOreEmphasisDescription.Size = new System.Drawing.Size(498, 29);
			this.lblOreEmphasisDescription.TabIndex = 1;
			this.lblOreEmphasisDescription.Text = resources.GetString("lblOreEmphasisDescription.Text");
			// 
			// cbSquaredStartPositions
			// 
			this.cbSquaredStartPositions.AutoSize = true;
			this.cbSquaredStartPositions.Location = new System.Drawing.Point(31, 67);
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
			this.cbTiledStartPositions.Location = new System.Drawing.Point(29, 102);
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
			this.cbEmphasizeOre.Location = new System.Drawing.Point(31, 19);
			this.cbEmphasizeOre.Name = "cbEmphasizeOre";
			this.cbEmphasizeOre.Size = new System.Drawing.Size(125, 17);
			this.cbEmphasizeOre.TabIndex = 0;
			this.cbEmphasizeOre.Text = "Emphasize ore/gems";
			this.cbEmphasizeOre.UseVisualStyleBackColor = true;
			this.cbEmphasizeOre.CheckedChanged += new System.EventHandler(this.UIChanged);
			// 
			// pnlEngine
			// 
			this.pnlEngine.Controls.Add(this.rbEngineFS);
			this.pnlEngine.Controls.Add(this.rbEngineTS);
			this.pnlEngine.Controls.Add(this.rbEngineAuto);
			this.pnlEngine.Controls.Add(this.rbEngineRA2);
			this.pnlEngine.Controls.Add(this.rbEngineYR);
			this.pnlEngine.Controls.Add(this.lbEngine);
			this.pnlEngine.Location = new System.Drawing.Point(6, 142);
			this.pnlEngine.Name = "pnlEngine";
			this.pnlEngine.Size = new System.Drawing.Size(507, 42);
			this.pnlEngine.TabIndex = 13;
			// 
			// rbEngineFS
			// 
			this.rbEngineFS.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.rbEngineFS.AutoSize = true;
			this.rbEngineFS.Location = new System.Drawing.Point(383, 20);
			this.rbEngineFS.Name = "rbEngineFS";
			this.rbEngineFS.Size = new System.Drawing.Size(68, 17);
			this.rbEngineFS.TabIndex = 14;
			this.rbEngineFS.Text = "Force FS";
			this.rbEngineFS.UseVisualStyleBackColor = true;
			this.rbEngineFS.CheckedChanged += new System.EventHandler(this.RbEngineCheckedChanged);
			// 
			// rbEngineTS
			// 
			this.rbEngineTS.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.rbEngineTS.AutoSize = true;
			this.rbEngineTS.Location = new System.Drawing.Point(308, 20);
			this.rbEngineTS.Name = "rbEngineTS";
			this.rbEngineTS.Size = new System.Drawing.Size(69, 17);
			this.rbEngineTS.TabIndex = 13;
			this.rbEngineTS.Text = "Force TS";
			this.rbEngineTS.UseVisualStyleBackColor = true;
			this.rbEngineTS.CheckedChanged += new System.EventHandler(this.RbEngineCheckedChanged);
			// 
			// rbEngineAuto
			// 
			this.rbEngineAuto.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.rbEngineAuto.AutoSize = true;
			this.rbEngineAuto.Checked = true;
			this.rbEngineAuto.Location = new System.Drawing.Point(12, 20);
			this.rbEngineAuto.Name = "rbEngineAuto";
			this.rbEngineAuto.Size = new System.Drawing.Size(132, 17);
			this.rbEngineAuto.TabIndex = 10;
			this.rbEngineAuto.TabStop = true;
			this.rbEngineAuto.Text = "Automatic engine rules";
			this.toolTip.SetToolTip(this.rbEngineAuto, resources.GetString("rbEngineAuto.ToolTip"));
			this.rbEngineAuto.UseVisualStyleBackColor = true;
			this.rbEngineAuto.CheckedChanged += new System.EventHandler(this.RbEngineCheckedChanged);
			// 
			// rbEngineRA2
			// 
			this.rbEngineRA2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.rbEngineRA2.AutoSize = true;
			this.rbEngineRA2.Location = new System.Drawing.Point(226, 20);
			this.rbEngineRA2.Name = "rbEngineRA2";
			this.rbEngineRA2.Size = new System.Drawing.Size(76, 17);
			this.rbEngineRA2.TabIndex = 12;
			this.rbEngineRA2.Text = "Force RA2";
			this.rbEngineRA2.UseVisualStyleBackColor = true;
			this.rbEngineRA2.CheckedChanged += new System.EventHandler(this.RbEngineCheckedChanged);
			// 
			// rbEngineYR
			// 
			this.rbEngineYR.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.rbEngineYR.AutoSize = true;
			this.rbEngineYR.Location = new System.Drawing.Point(146, 20);
			this.rbEngineYR.Name = "rbEngineYR";
			this.rbEngineYR.Size = new System.Drawing.Size(70, 17);
			this.rbEngineYR.TabIndex = 11;
			this.rbEngineYR.Text = "Force YR";
			this.rbEngineYR.UseVisualStyleBackColor = true;
			this.rbEngineYR.CheckedChanged += new System.EventHandler(this.RbEngineCheckedChanged);
			// 
			// lbEngine
			// 
			this.lbEngine.Location = new System.Drawing.Point(12, 2);
			this.lbEngine.Name = "lbEngine";
			this.lbEngine.Size = new System.Drawing.Size(487, 16);
			this.lbEngine.TabIndex = 15;
			this.lbEngine.Text = "This setting tells the program which engine to mimic. Picking an incompatible one" +
    "  will cause crashes.";
			// 
			// gbInputOutput
			// 
			this.gbInputOutput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.gbInputOutput.Controls.Add(this.tbCustomOutput);
			this.gbInputOutput.Controls.Add(this.rbCustomFilename);
			this.gbInputOutput.Controls.Add(this.rbAutoFilename);
			this.gbInputOutput.Controls.Add(this.lblMarkersType);
			this.gbInputOutput.Controls.Add(this.cbMarkersType);
			this.gbInputOutput.Controls.Add(this.cbPreserveThumbAspect);
			this.gbInputOutput.Controls.Add(this.tbThumbDimensions);
			this.gbInputOutput.Controls.Add(this.cbOutputThumbnail);
			this.gbInputOutput.Controls.Add(this.btnModEditor);
			this.gbInputOutput.Controls.Add(this.pnlEngine);
			this.gbInputOutput.Controls.Add(this.tbModConfig);
			this.gbInputOutput.Controls.Add(this.cbModConfig);
			this.gbInputOutput.Controls.Add(this.tbMixDir);
			this.gbInputOutput.Controls.Add(this.lblMixFiles);
			this.gbInputOutput.Controls.Add(this.btnBrowseMixDir);
			this.gbInputOutput.Controls.Add(this.lblQuality);
			this.gbInputOutput.Controls.Add(this.tbInput);
			this.gbInputOutput.Controls.Add(this.lblCompressionLevel);
			this.gbInputOutput.Controls.Add(this.lblInputMap);
			this.gbInputOutput.Controls.Add(this.nudEncodingQuality);
			this.gbInputOutput.Controls.Add(this.btnBrowseInput);
			this.gbInputOutput.Controls.Add(this.cbOutputJPG);
			this.gbInputOutput.Controls.Add(this.cbOutputPNG);
			this.gbInputOutput.Controls.Add(this.nudCompression);
			this.gbInputOutput.Location = new System.Drawing.Point(6, 6);
			this.gbInputOutput.Name = "gbInputOutput";
			this.gbInputOutput.Size = new System.Drawing.Size(581, 221);
			this.gbInputOutput.TabIndex = 0;
			this.gbInputOutput.TabStop = false;
			this.gbInputOutput.Text = "Input && output";
			this.gbInputOutput.DragDrop += new System.Windows.Forms.DragEventHandler(this.InputDragDrop);
			this.gbInputOutput.DragEnter += new System.Windows.Forms.DragEventHandler(this.InputDragEnter);
			// 
			// tbCustomOutput
			// 
			this.tbCustomOutput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbCustomOutput.Location = new System.Drawing.Point(273, 120);
			this.tbCustomOutput.Name = "tbCustomOutput";
			this.tbCustomOutput.Size = new System.Drawing.Size(221, 20);
			this.tbCustomOutput.TabIndex = 10;
			this.tbCustomOutput.Visible = false;
			this.tbCustomOutput.TextChanged += new System.EventHandler(this.UIChanged);
			// 
			// rbCustomFilename
			// 
			this.rbCustomFilename.AutoSize = true;
			this.rbCustomFilename.Location = new System.Drawing.Point(152, 121);
			this.rbCustomFilename.Name = "rbCustomFilename";
			this.rbCustomFilename.Size = new System.Drawing.Size(102, 17);
			this.rbCustomFilename.TabIndex = 9;
			this.rbCustomFilename.Text = "Custom filename";
			this.toolTip.SetToolTip(this.rbCustomFilename, "Overrides the output name instead of using the automatic filename resolution sche" +
        "me.\r\nThe .jpg and .png extensions are automatically added.");
			this.rbCustomFilename.UseVisualStyleBackColor = true;
			this.rbCustomFilename.CheckedChanged += new System.EventHandler(this.OutputNameCheckedChanged);
			// 
			// rbAutoFilename
			// 
			this.rbAutoFilename.AutoSize = true;
			this.rbAutoFilename.Checked = true;
			this.rbAutoFilename.Location = new System.Drawing.Point(19, 121);
			this.rbAutoFilename.Name = "rbAutoFilename";
			this.rbAutoFilename.Size = new System.Drawing.Size(114, 17);
			this.rbAutoFilename.TabIndex = 8;
			this.rbAutoFilename.TabStop = true;
			this.rbAutoFilename.Text = "Automatic filename";
			this.toolTip.SetToolTip(this.rbAutoFilename, resources.GetString("rbAutoFilename.ToolTip"));
			this.rbAutoFilename.UseVisualStyleBackColor = true;
			this.rbAutoFilename.CheckedChanged += new System.EventHandler(this.OutputNameCheckedChanged);
			// 
			// lblMarkersType
			// 
			this.lblMarkersType.AutoSize = true;
			this.lblMarkersType.Location = new System.Drawing.Point(299, 99);
			this.lblMarkersType.Name = "lblMarkersType";
			this.lblMarkersType.Size = new System.Drawing.Size(68, 13);
			this.lblMarkersType.TabIndex = 19;
			this.lblMarkersType.Text = "Markers type";
			this.lblMarkersType.Visible = false;
			// 
			// cbMarkersType
			// 
			this.cbMarkersType.FormattingEnabled = true;
			this.cbMarkersType.Items.AddRange(new object[] {
            "None",
            "Squared",
            "Aro",
            "Bittah"});
			this.cbMarkersType.Location = new System.Drawing.Point(373, 96);
			this.cbMarkersType.Name = "cbMarkersType";
			this.cbMarkersType.Size = new System.Drawing.Size(121, 21);
			this.cbMarkersType.TabIndex = 18;
			this.cbMarkersType.Text = "None";
			this.cbMarkersType.Visible = false;
			this.cbMarkersType.SelectedIndexChanged += new System.EventHandler(this.UIChanged);
			// 
			// cbPreserveThumbAspect
			// 
			this.cbPreserveThumbAspect.AutoSize = true;
			this.cbPreserveThumbAspect.Checked = true;
			this.cbPreserveThumbAspect.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbPreserveThumbAspect.Location = new System.Drawing.Point(449, 76);
			this.cbPreserveThumbAspect.Name = "cbPreserveThumbAspect";
			this.cbPreserveThumbAspect.Size = new System.Drawing.Size(126, 17);
			this.cbPreserveThumbAspect.TabIndex = 17;
			this.cbPreserveThumbAspect.Text = "Preserve aspect ratio";
			this.toolTip.SetToolTip(this.cbPreserveThumbAspect, resources.GetString("cbPreserveThumbAspect.ToolTip"));
			this.cbPreserveThumbAspect.UseVisualStyleBackColor = true;
			this.cbPreserveThumbAspect.Visible = false;
			this.cbPreserveThumbAspect.CheckedChanged += new System.EventHandler(this.UIChanged);
			// 
			// tbThumbDimensions
			// 
			this.tbThumbDimensions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbThumbDimensions.Location = new System.Drawing.Point(390, 74);
			this.tbThumbDimensions.Name = "tbThumbDimensions";
			this.tbThumbDimensions.Size = new System.Drawing.Size(51, 20);
			this.tbThumbDimensions.TabIndex = 16;
			this.tbThumbDimensions.Text = "800,0";
			this.tbThumbDimensions.Visible = false;
			// 
			// cbOutputThumbnail
			// 
			this.cbOutputThumbnail.AutoSize = true;
			this.cbOutputThumbnail.Location = new System.Drawing.Point(284, 76);
			this.cbOutputThumbnail.Name = "cbOutputThumbnail";
			this.cbOutputThumbnail.Size = new System.Drawing.Size(106, 17);
			this.cbOutputThumbnail.TabIndex = 15;
			this.cbOutputThumbnail.Text = "Output thumbnail";
			this.toolTip.SetToolTip(this.cbOutputThumbnail, resources.GetString("cbOutputThumbnail.ToolTip"));
			this.cbOutputThumbnail.UseVisualStyleBackColor = true;
			this.cbOutputThumbnail.CheckedChanged += new System.EventHandler(this.CbOutputThumbnailCheckedChanged);
			// 
			// btnModEditor
			// 
			this.btnModEditor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnModEditor.Location = new System.Drawing.Point(500, 190);
			this.btnModEditor.Name = "btnModEditor";
			this.btnModEditor.Size = new System.Drawing.Size(75, 23);
			this.btnModEditor.TabIndex = 14;
			this.btnModEditor.Text = "Open editor";
			this.btnModEditor.UseVisualStyleBackColor = true;
			this.btnModEditor.Visible = false;
			this.btnModEditor.Click += new System.EventHandler(this.BtnModEditorClick);
			// 
			// tbModConfig
			// 
			this.tbModConfig.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbModConfig.Location = new System.Drawing.Point(165, 192);
			this.tbModConfig.Name = "tbModConfig";
			this.tbModConfig.Size = new System.Drawing.Size(329, 20);
			this.tbModConfig.TabIndex = 13;
			this.tbModConfig.Visible = false;
			this.tbModConfig.TextChanged += new System.EventHandler(this.UIChanged);
			// 
			// cbModConfig
			// 
			this.cbModConfig.AutoSize = true;
			this.cbModConfig.Location = new System.Drawing.Point(18, 194);
			this.cbModConfig.Name = "cbModConfig";
			this.cbModConfig.Size = new System.Drawing.Size(141, 17);
			this.cbModConfig.TabIndex = 12;
			this.cbModConfig.Text = "Load special mod config";
			this.toolTip.SetToolTip(this.cbModConfig, "Special mod configs allow you to specify precisely which extra directories, mixes" +
        "\r\nand theater specific settings should be considered for your mod.");
			this.cbModConfig.UseVisualStyleBackColor = true;
			this.cbModConfig.CheckedChanged += new System.EventHandler(this.cbModConfig_CheckedChanged);
			// 
			// tbMixDir
			// 
			this.tbMixDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbMixDir.Location = new System.Drawing.Point(124, 44);
			this.tbMixDir.Name = "tbMixDir";
			this.tbMixDir.Size = new System.Drawing.Size(370, 20);
			this.tbMixDir.TabIndex = 4;
			this.tbMixDir.TextChanged += new System.EventHandler(this.UIChanged);
			// 
			// lblMixFiles
			// 
			this.lblMixFiles.AutoSize = true;
			this.lblMixFiles.Location = new System.Drawing.Point(16, 45);
			this.lblMixFiles.Name = "lblMixFiles";
			this.lblMixFiles.Size = new System.Drawing.Size(44, 13);
			this.lblMixFiles.TabIndex = 3;
			this.lblMixFiles.Text = "Mix files";
			this.toolTip.SetToolTip(this.lblMixFiles, "Set this to the folder where you have your game mix files stored.\r\nIf possible, t" +
        "his will be determined from information in the registry,\r\nmeaning you can leave " +
        "this empty.");
			// 
			// btnBrowseMixDir
			// 
			this.btnBrowseMixDir.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnBrowseMixDir.Location = new System.Drawing.Point(500, 42);
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
			this.lblQuality.Location = new System.Drawing.Point(121, 76);
			this.lblQuality.Name = "lblQuality";
			this.lblQuality.Size = new System.Drawing.Size(85, 13);
			this.lblQuality.TabIndex = 5;
			this.lblQuality.Text = "Encoding quality";
			this.toolTip.SetToolTip(this.lblQuality, "JPEG encoding quality, between 1-100 with 100 resulting in the largest file in th" +
        "e highest quality.");
			// 
			// tbInput
			// 
			this.tbInput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbInput.Location = new System.Drawing.Point(124, 18);
			this.tbInput.Name = "tbInput";
			this.tbInput.Size = new System.Drawing.Size(370, 20);
			this.tbInput.TabIndex = 1;
			this.tbInput.TextChanged += new System.EventHandler(this.UIChanged);
			// 
			// lblCompressionLevel
			// 
			this.lblCompressionLevel.AutoSize = true;
			this.lblCompressionLevel.Location = new System.Drawing.Point(121, 99);
			this.lblCompressionLevel.Name = "lblCompressionLevel";
			this.lblCompressionLevel.Size = new System.Drawing.Size(92, 13);
			this.lblCompressionLevel.TabIndex = 1;
			this.lblCompressionLevel.Text = "Compression level";
			this.toolTip.SetToolTip(this.lblCompressionLevel, "PNG compression level ranging from 1-9, 9 resulting in the smallest file but also" +
        " in longest loading time.");
			this.lblCompressionLevel.Visible = false;
			// 
			// lblInputMap
			// 
			this.lblInputMap.AutoSize = true;
			this.lblInputMap.Location = new System.Drawing.Point(16, 21);
			this.lblInputMap.Name = "lblInputMap";
			this.lblInputMap.Size = new System.Drawing.Size(54, 13);
			this.lblInputMap.TabIndex = 0;
			this.lblInputMap.Text = "Input map";
			this.toolTip.SetToolTip(this.lblInputMap, "Full path the to input map.\r\nValid filetypes are *.mpr, *.map, *.yrm, *.mmx, *.yr" +
        "o.");
			// 
			// nudEncodingQuality
			// 
			this.nudEncodingQuality.Location = new System.Drawing.Point(224, 75);
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
			this.btnBrowseInput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnBrowseInput.Location = new System.Drawing.Point(500, 16);
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
			this.cbOutputJPG.Location = new System.Drawing.Point(19, 75);
			this.cbOutputJPG.Name = "cbOutputJPG";
			this.cbOutputJPG.Size = new System.Drawing.Size(81, 17);
			this.cbOutputJPG.TabIndex = 4;
			this.cbOutputJPG.Text = "Output JPG";
			this.toolTip.SetToolTip(this.cbOutputJPG, "Specifies whether or not a JPEG compressed image is saved.\r\nImages are always sav" +
        "ed in the same directory as the input map.");
			this.cbOutputJPG.UseVisualStyleBackColor = true;
			this.cbOutputJPG.CheckedChanged += new System.EventHandler(this.JpegOutputCheckedChanged);
			// 
			// cbOutputPNG
			// 
			this.cbOutputPNG.AutoSize = true;
			this.cbOutputPNG.Location = new System.Drawing.Point(19, 98);
			this.cbOutputPNG.Name = "cbOutputPNG";
			this.cbOutputPNG.Size = new System.Drawing.Size(84, 17);
			this.cbOutputPNG.TabIndex = 0;
			this.cbOutputPNG.Text = "Output PNG";
			this.toolTip.SetToolTip(this.cbOutputPNG, "Specifies whether or not a JPEG compressed image is saved.");
			this.cbOutputPNG.UseVisualStyleBackColor = true;
			this.cbOutputPNG.CheckedChanged += new System.EventHandler(this.PngOutputCheckedChanged);
			// 
			// nudCompression
			// 
			this.nudCompression.Location = new System.Drawing.Point(224, 98);
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
			this.btnRenderExecute.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnRenderExecute.Location = new System.Drawing.Point(506, 258);
			this.btnRenderExecute.Name = "btnRenderExecute";
			this.btnRenderExecute.Size = new System.Drawing.Size(75, 23);
			this.btnRenderExecute.TabIndex = 5;
			this.btnRenderExecute.Text = "Render map";
			this.btnRenderExecute.UseVisualStyleBackColor = true;
			this.btnRenderExecute.Click += new System.EventHandler(this.ExecuteCommand);
			// 
			// tbCommandPreview
			// 
			this.tbCommandPreview.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbCommandPreview.BackColor = System.Drawing.SystemColors.ControlLight;
			this.tbCommandPreview.Location = new System.Drawing.Point(77, 260);
			this.tbCommandPreview.Name = "tbCommandPreview";
			this.tbCommandPreview.ReadOnly = true;
			this.tbCommandPreview.Size = new System.Drawing.Size(423, 20);
			this.tbCommandPreview.TabIndex = 4;
			this.toolTip.SetToolTip(this.tbCommandPreview, "For more fine-grained control you can invoke CNCMaps.Renderer.exe manually!");
			// 
			// ofd
			// 
			this.ofd.FileName = "ofd";
			// 
			// statusStrip
			// 
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.lblFill,
            this.pbProgress});
			this.statusStrip.Location = new System.Drawing.Point(0, 328);
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.Size = new System.Drawing.Size(606, 22);
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
			this.lblFill.Size = new System.Drawing.Size(447, 17);
			this.lblFill.Spring = true;
			// 
			// pbProgress
			// 
			this.pbProgress.Name = "pbProgress";
			this.pbProgress.Size = new System.Drawing.Size(100, 16);
			this.pbProgress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			// 
			// tabControl
			// 
			this.tabControl.AllowDrop = true;
			this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl.Controls.Add(this.tpMain);
			this.tabControl.Controls.Add(this.tpMisc);
			this.tabControl.Controls.Add(this.tpLog);
			this.tabControl.Controls.Add(this.tpAbout);
			this.tabControl.Location = new System.Drawing.Point(3, 4);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(603, 310);
			this.tabControl.TabIndex = 8;
			this.tabControl.DragDrop += new System.Windows.Forms.DragEventHandler(this.InputDragDrop);
			this.tabControl.DragEnter += new System.Windows.Forms.DragEventHandler(this.InputDragEnter);
			// 
			// tpMain
			// 
			this.tpMain.Controls.Add(this.lblCommand);
			this.tpMain.Controls.Add(this.gbInputOutput);
			this.tpMain.Controls.Add(this.btnRenderExecute);
			this.tpMain.Controls.Add(this.tbCommandPreview);
			this.tpMain.Location = new System.Drawing.Point(4, 22);
			this.tpMain.Name = "tpMain";
			this.tpMain.Padding = new System.Windows.Forms.Padding(3);
			this.tpMain.Size = new System.Drawing.Size(595, 284);
			this.tpMain.TabIndex = 0;
			this.tpMain.Text = "Main settings";
			this.tpMain.UseVisualStyleBackColor = true;
			// 
			// lblCommand
			// 
			this.lblCommand.Location = new System.Drawing.Point(9, 263);
			this.lblCommand.Name = "lblCommand";
			this.lblCommand.Size = new System.Drawing.Size(62, 17);
			this.lblCommand.TabIndex = 7;
			this.lblCommand.Text = "Command";
			// 
			// tpMisc
			// 
			this.tpMisc.Controls.Add(this.gbMiscOptions);
			this.tpMisc.Location = new System.Drawing.Point(4, 22);
			this.tpMisc.Name = "tpMisc";
			this.tpMisc.Padding = new System.Windows.Forms.Padding(3);
			this.tpMisc.Size = new System.Drawing.Size(595, 284);
			this.tpMisc.TabIndex = 1;
			this.tpMisc.Text = "Misc settings";
			this.tpMisc.UseVisualStyleBackColor = true;
			// 
			// tpLog
			// 
			this.tpLog.Controls.Add(this.gbLog);
			this.tpLog.Location = new System.Drawing.Point(4, 22);
			this.tpLog.Name = "tpLog";
			this.tpLog.Size = new System.Drawing.Size(595, 284);
			this.tpLog.TabIndex = 2;
			this.tpLog.Text = "Log";
			this.tpLog.UseVisualStyleBackColor = true;
			// 
			// gbLog
			// 
			this.gbLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.gbLog.Controls.Add(this.rtbLog);
			this.gbLog.Location = new System.Drawing.Point(6, 6);
			this.gbLog.Name = "gbLog";
			this.gbLog.Size = new System.Drawing.Size(581, 275);
			this.gbLog.TabIndex = 7;
			this.gbLog.TabStop = false;
			this.gbLog.Text = "Log";
			// 
			// rtbLog
			// 
			this.rtbLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.rtbLog.BackColor = System.Drawing.Color.White;
			this.rtbLog.Location = new System.Drawing.Point(6, 18);
			this.rtbLog.Name = "rtbLog";
			this.rtbLog.ReadOnly = true;
			this.rtbLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
			this.rtbLog.Size = new System.Drawing.Size(569, 250);
			this.rtbLog.TabIndex = 0;
			this.rtbLog.Text = "";
			this.rtbLog.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.rtbLog_LinkClicked);
			// 
			// tpAbout
			// 
			this.tpAbout.AllowDrop = true;
			this.tpAbout.Controls.Add(this.label1);
			this.tpAbout.Location = new System.Drawing.Point(4, 22);
			this.tpAbout.Name = "tpAbout";
			this.tpAbout.Padding = new System.Windows.Forms.Padding(3);
			this.tpAbout.Size = new System.Drawing.Size(595, 284);
			this.tpAbout.TabIndex = 3;
			this.tpAbout.Text = "About";
			this.tpAbout.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(20, 24);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(220, 52);
			this.label1.TabIndex = 0;
			this.label1.Text = "Program by Frank Razenberg © 2005-2016\r\n\r\nSpecial thanks to Olaf van der Spek for" +
    " XCC.\r\nSpecial thanks to #renproj for modenc.";
			// 
			// MainForm
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(606, 350);
			this.Controls.Add(this.tabControl);
			this.Controls.Add(this.statusStrip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(582, 372);
			this.Name = "MainForm";
			this.Text = "Red Alert 2 and Tiberian Sun map renderer";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFormClosing);
			this.Load += new System.EventHandler(this.MainFormLoad);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.InputDragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.InputDragEnter);
			this.gbMiscOptions.ResumeLayout(false);
			this.gbMiscOptions.PerformLayout();
			this.gbSizeMode.ResumeLayout(false);
			this.gbSizeMode.PerformLayout();
			this.gbThumbs.ResumeLayout(false);
			this.gbThumbs.PerformLayout();
			this.gbVoxelsMode.ResumeLayout(false);
			this.gbVoxelsMode.PerformLayout();
			this.pnlEngine.ResumeLayout(false);
			this.pnlEngine.PerformLayout();
			this.gbInputOutput.ResumeLayout(false);
			this.gbInputOutput.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.nudEncodingQuality)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nudCompression)).EndInit();
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
			this.tabControl.ResumeLayout(false);
			this.tpMain.ResumeLayout(false);
			this.tpMain.PerformLayout();
			this.tpMisc.ResumeLayout(false);
			this.tpLog.ResumeLayout(false);
			this.gbLog.ResumeLayout(false);
			this.tpAbout.ResumeLayout(false);
			this.tpAbout.PerformLayout();
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
		private System.Windows.Forms.GroupBox gbInputOutput;
		private System.Windows.Forms.Button btnBrowseInput;
		private System.Windows.Forms.TextBox tbMixDir;
		private System.Windows.Forms.Label lblMixFiles;
		private System.Windows.Forms.Button btnBrowseMixDir;
		private System.Windows.Forms.TextBox tbInput;
		private System.Windows.Forms.Label lblInputMap;
		private System.Windows.Forms.Button btnRenderExecute;
		private System.Windows.Forms.TextBox tbCommandPreview;
		private System.Windows.Forms.OpenFileDialog ofd;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
		private System.Windows.Forms.TextBox tbCustomOutput;
		private System.Windows.Forms.RadioButton rbCustomFilename;
		private System.Windows.Forms.RadioButton rbAutoFilename;
		private System.Windows.Forms.Label lblQuality;
		private System.Windows.Forms.Label lblCompressionLevel;
		private System.Windows.Forms.NumericUpDown nudEncodingQuality;
		private System.Windows.Forms.CheckBox cbOutputJPG;
		private System.Windows.Forms.CheckBox cbOutputPNG;
		private System.Windows.Forms.NumericUpDown nudCompression;
		private System.Windows.Forms.RadioButton rbSizeLocal;
		private System.Windows.Forms.RadioButton rbSizeFullmap;
		private System.Windows.Forms.Panel pnlEngine;
		private System.Windows.Forms.RadioButton rbEngineAuto;
		private System.Windows.Forms.RadioButton rbEngineRA2;
		private System.Windows.Forms.RadioButton rbEngineYR;
		private System.Windows.Forms.RadioButton rbEngineFS;
		private System.Windows.Forms.RadioButton rbEngineTS;
		private System.Windows.Forms.CheckBox cbReplacePreview;
		private System.Windows.Forms.RadioButton rbPreferHardwareRendering;
		private System.Windows.Forms.RadioButton rbPreferSoftwareRendering;
		private System.Windows.Forms.CheckBox cbOmitSquareMarkers;
		private System.Windows.Forms.StatusStrip statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel lblFill;
		private System.Windows.Forms.ToolStripProgressBar pbProgress;
		private System.Windows.Forms.ToolStripStatusLabel lblStatus;
		private System.Windows.Forms.TextBox tbModConfig;
		private System.Windows.Forms.CheckBox cbModConfig;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.Button btnModEditor;
		private System.Windows.Forms.TabPage tpMisc;
		private System.Windows.Forms.TabPage tpMain;
		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.RadioButton rbSizeAuto;
		private System.Windows.Forms.Label lbEngine;
		private System.Windows.Forms.Label lblCommand;
		private System.Windows.Forms.GroupBox gbThumbs;
		private System.Windows.Forms.GroupBox gbVoxelsMode;
		private CheckBox cbOutputThumbnail;
		private TextBox tbThumbDimensions;
		private CheckBox cbPreserveThumbAspect;
		private TabPage tpLog;
		private GroupBox gbLog;
		private RichTextBox rtbLog;
		private TabPage tpAbout;
		private ComboBox cbMarkersType;
		private Label lblMarkersType;
		private Label label1;
		private Label lblFixupTilesDesc;
		private CheckBox ckbFixupTiles;
		private GroupBox gbSizeMode;
	}
}

