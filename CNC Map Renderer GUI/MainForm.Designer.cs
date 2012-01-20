namespace RA2Maps_GUI {
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
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.panel2 = new System.Windows.Forms.Panel();
			this.radioButton6 = new System.Windows.Forms.RadioButton();
			this.radioButton8 = new System.Windows.Forms.RadioButton();
			this.panel1 = new System.Windows.Forms.Panel();
			this.rbForceFS = new System.Windows.Forms.RadioButton();
			this.rbForceTS = new System.Windows.Forms.RadioButton();
			this.radioButton4 = new System.Windows.Forms.RadioButton();
			this.rbForceRA2 = new System.Windows.Forms.RadioButton();
			this.radioButton3 = new System.Windows.Forms.RadioButton();
			this.label7 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.checkBox4 = new System.Windows.Forms.CheckBox();
			this.checkBox2 = new System.Windows.Forms.CheckBox();
			this.checkBox3 = new System.Windows.Forms.CheckBox();
			this.gbInput = new System.Windows.Forms.GroupBox();
			this.tbCustomOutput = new System.Windows.Forms.TextBox();
			this.tbRenderProg = new System.Windows.Forms.TextBox();
			this.label11 = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.radioButton2 = new System.Windows.Forms.RadioButton();
			this.btnBrowseRenderer = new System.Windows.Forms.Button();
			this.radioButton1 = new System.Windows.Forms.RadioButton();
			this.textBox2 = new System.Windows.Forms.TextBox();
			this.label9 = new System.Windows.Forms.Label();
			this.btnBrowseMixDir = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.numericUpDown2 = new System.Windows.Forms.NumericUpDown();
			this.btnBrowseInput = new System.Windows.Forms.Button();
			this.checkBox1 = new System.Windows.Forms.CheckBox();
			this.PNG = new System.Windows.Forms.CheckBox();
			this.nudCompression = new System.Windows.Forms.NumericUpDown();
			this.button4 = new System.Windows.Forms.Button();
			this.textBox5 = new System.Windows.Forms.TextBox();
			this.label12 = new System.Windows.Forms.Label();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.textBox6 = new System.Windows.Forms.TextBox();
			this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
			this.groupBox2.SuspendLayout();
			this.panel2.SuspendLayout();
			this.panel1.SuspendLayout();
			this.gbInput.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nudCompression)).BeginInit();
			this.groupBox4.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.panel2);
			this.groupBox2.Controls.Add(this.panel1);
			this.groupBox2.Controls.Add(this.label7);
			this.groupBox2.Controls.Add(this.label6);
			this.groupBox2.Controls.Add(this.label5);
			this.groupBox2.Controls.Add(this.checkBox4);
			this.groupBox2.Controls.Add(this.checkBox2);
			this.groupBox2.Controls.Add(this.checkBox3);
			this.groupBox2.Location = new System.Drawing.Point(12, 209);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(541, 225);
			this.groupBox2.TabIndex = 1;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Misc. Options";
			this.groupBox2.DragDrop += new System.Windows.Forms.DragEventHandler(this.gbInput_DragDrop);
			this.groupBox2.DragEnter += new System.Windows.Forms.DragEventHandler(this.gbInput_DragEnter);
			// 
			// panel2
			// 
			this.panel2.Controls.Add(this.radioButton6);
			this.panel2.Controls.Add(this.radioButton8);
			this.panel2.Location = new System.Drawing.Point(17, 199);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(461, 26);
			this.panel2.TabIndex = 14;
			// 
			// radioButton6
			// 
			this.radioButton6.AutoSize = true;
			this.radioButton6.Checked = true;
			this.radioButton6.Location = new System.Drawing.Point(12, 4);
			this.radioButton6.Name = "radioButton6";
			this.radioButton6.Size = new System.Drawing.Size(110, 17);
			this.radioButton6.TabIndex = 10;
			this.radioButton6.TabStop = true;
			this.radioButton6.Text = "Use map localsize";
			this.radioButton6.UseVisualStyleBackColor = true;
			this.radioButton6.CheckedChanged += new System.EventHandler(this.rbsEngine_CheckedChanged);
			// 
			// radioButton8
			// 
			this.radioButton8.AutoSize = true;
			this.radioButton8.Location = new System.Drawing.Point(154, 4);
			this.radioButton8.Name = "radioButton8";
			this.radioButton8.Size = new System.Drawing.Size(175, 17);
			this.radioButton8.TabIndex = 11;
			this.radioButton8.Text = "Use full size (useful for missions)";
			this.radioButton8.UseVisualStyleBackColor = true;
			this.radioButton8.CheckedChanged += new System.EventHandler(this.rbsEngine_CheckedChanged);
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.rbForceFS);
			this.panel1.Controls.Add(this.rbForceTS);
			this.panel1.Controls.Add(this.radioButton4);
			this.panel1.Controls.Add(this.rbForceRA2);
			this.panel1.Controls.Add(this.radioButton3);
			this.panel1.Location = new System.Drawing.Point(17, 167);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(461, 26);
			this.panel1.TabIndex = 13;
			// 
			// rbForceFS
			// 
			this.rbForceFS.AutoSize = true;
			this.rbForceFS.Location = new System.Drawing.Point(383, 4);
			this.rbForceFS.Name = "rbForceFS";
			this.rbForceFS.Size = new System.Drawing.Size(68, 17);
			this.rbForceFS.TabIndex = 14;
			this.rbForceFS.Text = "Force FS";
			this.rbForceFS.UseVisualStyleBackColor = true;
			// 
			// rbForceTS
			// 
			this.rbForceTS.AutoSize = true;
			this.rbForceTS.Location = new System.Drawing.Point(308, 3);
			this.rbForceTS.Name = "rbForceTS";
			this.rbForceTS.Size = new System.Drawing.Size(69, 17);
			this.rbForceTS.TabIndex = 13;
			this.rbForceTS.Text = "Force TS";
			this.rbForceTS.UseVisualStyleBackColor = true;
			// 
			// radioButton4
			// 
			this.radioButton4.AutoSize = true;
			this.radioButton4.Checked = true;
			this.radioButton4.Location = new System.Drawing.Point(12, 4);
			this.radioButton4.Name = "radioButton4";
			this.radioButton4.Size = new System.Drawing.Size(132, 17);
			this.radioButton4.TabIndex = 10;
			this.radioButton4.TabStop = true;
			this.radioButton4.Text = "Automatic engine rules";
			this.radioButton4.UseVisualStyleBackColor = true;
			this.radioButton4.CheckedChanged += new System.EventHandler(this.rbsEngine_CheckedChanged);
			// 
			// rbForceRA2
			// 
			this.rbForceRA2.AutoSize = true;
			this.rbForceRA2.Location = new System.Drawing.Point(226, 4);
			this.rbForceRA2.Name = "rbForceRA2";
			this.rbForceRA2.Size = new System.Drawing.Size(76, 17);
			this.rbForceRA2.TabIndex = 12;
			this.rbForceRA2.Text = "Force RA2";
			this.rbForceRA2.UseVisualStyleBackColor = true;
			this.rbForceRA2.CheckedChanged += new System.EventHandler(this.rbsEngine_CheckedChanged);
			// 
			// radioButton3
			// 
			this.radioButton3.AutoSize = true;
			this.radioButton3.Location = new System.Drawing.Point(150, 3);
			this.radioButton3.Name = "radioButton3";
			this.radioButton3.Size = new System.Drawing.Size(70, 17);
			this.radioButton3.TabIndex = 11;
			this.radioButton3.Text = "Force YR";
			this.radioButton3.UseVisualStyleBackColor = true;
			this.radioButton3.CheckedChanged += new System.EventHandler(this.rbsEngine_CheckedChanged);
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(12, 147);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(515, 17);
			this.label7.TabIndex = 5;
			this.label7.Text = "Gives a slightly transparent red color to the 4x4 foundation of where MCVs as ini" +
	"tially placed would deploy.";
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(12, 100);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(498, 15);
			this.label6.TabIndex = 3;
			this.label6.Text = "Places a large red square at the starting positions. Looks good when scaling down" +
	" to preview images.";
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(12, 39);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(498, 32);
			this.label5.TabIndex = 1;
			this.label5.Text = "Setting this will result in a yellow glow to be placed behind ore, and a purple glow behind gems. When scaling images down, for example to use them as small preview image of a map, this looks pretty good.";
			// 
			// checkBox4
			// 
			this.checkBox4.AutoSize = true;
			this.checkBox4.Location = new System.Drawing.Point(31, 80);
			this.checkBox4.Name = "checkBox4";
			this.checkBox4.Size = new System.Drawing.Size(133, 17);
			this.checkBox4.TabIndex = 2;
			this.checkBox4.Text = "Squared start positions";
			this.checkBox4.UseVisualStyleBackColor = true;
			this.checkBox4.CheckedChanged += new System.EventHandler(this.checkBox4_CheckedChanged_1);
			// 
			// checkBox2
			// 
			this.checkBox2.AutoSize = true;
			this.checkBox2.Location = new System.Drawing.Point(31, 127);
			this.checkBox2.Name = "checkBox2";
			this.checkBox2.Size = new System.Drawing.Size(116, 17);
			this.checkBox2.TabIndex = 4;
			this.checkBox2.Text = "Tiled start positions";
			this.checkBox2.UseVisualStyleBackColor = true;
			this.checkBox2.CheckedChanged += new System.EventHandler(this.checkBox2_CheckedChanged);
			// 
			// checkBox3
			// 
			this.checkBox3.AutoSize = true;
			this.checkBox3.Location = new System.Drawing.Point(31, 19);
			this.checkBox3.Name = "checkBox3";
			this.checkBox3.Size = new System.Drawing.Size(125, 17);
			this.checkBox3.TabIndex = 0;
			this.checkBox3.Text = "Emphasize ore/gems";
			this.checkBox3.UseVisualStyleBackColor = true;
			this.checkBox3.CheckedChanged += new System.EventHandler(this.checkBox3_CheckedChanged);
			// 
			// gbInput
			// 
			this.gbInput.Controls.Add(this.tbCustomOutput);
			this.gbInput.Controls.Add(this.tbRenderProg);
			this.gbInput.Controls.Add(this.label11);
			this.gbInput.Controls.Add(this.label10);
			this.gbInput.Controls.Add(this.radioButton2);
			this.gbInput.Controls.Add(this.btnBrowseRenderer);
			this.gbInput.Controls.Add(this.radioButton1);
			this.gbInput.Controls.Add(this.textBox2);
			this.gbInput.Controls.Add(this.label9);
			this.gbInput.Controls.Add(this.btnBrowseMixDir);
			this.gbInput.Controls.Add(this.label2);
			this.gbInput.Controls.Add(this.textBox1);
			this.gbInput.Controls.Add(this.label1);
			this.gbInput.Controls.Add(this.label8);
			this.gbInput.Controls.Add(this.numericUpDown2);
			this.gbInput.Controls.Add(this.btnBrowseInput);
			this.gbInput.Controls.Add(this.checkBox1);
			this.gbInput.Controls.Add(this.PNG);
			this.gbInput.Controls.Add(this.nudCompression);
			this.gbInput.Location = new System.Drawing.Point(12, 9);
			this.gbInput.Name = "gbInput";
			this.gbInput.Size = new System.Drawing.Size(544, 194);
			this.gbInput.TabIndex = 0;
			this.gbInput.TabStop = false;
			this.gbInput.Text = "Input";
			this.gbInput.DragDrop += new System.Windows.Forms.DragEventHandler(this.gbInput_DragDrop);
			this.gbInput.DragEnter += new System.Windows.Forms.DragEventHandler(this.gbInput_DragEnter);
			// 
			// tbCustomOutput
			// 
			this.tbCustomOutput.Location = new System.Drawing.Point(302, 142);
			this.tbCustomOutput.Name = "tbCustomOutput";
			this.tbCustomOutput.Size = new System.Drawing.Size(219, 20);
			this.tbCustomOutput.TabIndex = 10;
			this.tbCustomOutput.Visible = false;
			this.tbCustomOutput.TextChanged += new System.EventHandler(this.textBox4_TextChanged);
			// 
			// tbRenderProg
			// 
			this.tbRenderProg.Location = new System.Drawing.Point(131, 67);
			this.tbRenderProg.Name = "tbRenderProg";
			this.tbRenderProg.Size = new System.Drawing.Size(298, 20);
			this.tbRenderProg.TabIndex = 7;
			this.tbRenderProg.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
			// 
			// label11
			// 
			this.label11.Location = new System.Drawing.Point(14, 165);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(515, 17);
			this.label11.TabIndex = 11;
			this.label11.Text = "Automatic filename resolution uses CSF, missions.ini or [Basic]/Name if possible." +
	"\r\n";
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(28, 68);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(102, 13);
			this.label10.TabIndex = 6;
			this.label10.Text = "Map render program";
			// 
			// radioButton2
			// 
			this.radioButton2.AutoSize = true;
			this.radioButton2.Location = new System.Drawing.Point(173, 143);
			this.radioButton2.Name = "radioButton2";
			this.radioButton2.Size = new System.Drawing.Size(102, 17);
			this.radioButton2.TabIndex = 9;
			this.radioButton2.Text = "Custom filename";
			this.radioButton2.UseVisualStyleBackColor = true;
			this.radioButton2.CheckedChanged += new System.EventHandler(this.rbCustomOutput_CheckedChanged);
			// 
			// btnBrowseRenderer
			// 
			this.btnBrowseRenderer.Location = new System.Drawing.Point(435, 67);
			this.btnBrowseRenderer.Name = "btnBrowseRenderer";
			this.btnBrowseRenderer.Size = new System.Drawing.Size(75, 23);
			this.btnBrowseRenderer.TabIndex = 8;
			this.btnBrowseRenderer.Text = "Browse";
			this.btnBrowseRenderer.UseVisualStyleBackColor = true;
			this.btnBrowseRenderer.Click += new System.EventHandler(this.btnBrowseRenderer_Click);
			// 
			// radioButton1
			// 
			this.radioButton1.AutoSize = true;
			this.radioButton1.Checked = true;
			this.radioButton1.Location = new System.Drawing.Point(31, 143);
			this.radioButton1.Name = "radioButton1";
			this.radioButton1.Size = new System.Drawing.Size(114, 17);
			this.radioButton1.TabIndex = 8;
			this.radioButton1.TabStop = true;
			this.radioButton1.Text = "Automatic filename";
			this.radioButton1.UseVisualStyleBackColor = true;
			this.radioButton1.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
			// 
			// textBox2
			// 
			this.textBox2.Location = new System.Drawing.Point(131, 41);
			this.textBox2.Name = "textBox2";
			this.textBox2.Size = new System.Drawing.Size(298, 20);
			this.textBox2.TabIndex = 4;
			this.textBox2.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(28, 42);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(44, 13);
			this.label9.TabIndex = 3;
			this.label9.Text = "Mix files";
			// 
			// btnBrowseMixDir
			// 
			this.btnBrowseMixDir.Location = new System.Drawing.Point(435, 41);
			this.btnBrowseMixDir.Name = "btnBrowseMixDir";
			this.btnBrowseMixDir.Size = new System.Drawing.Size(75, 23);
			this.btnBrowseMixDir.TabIndex = 5;
			this.btnBrowseMixDir.Text = "Browse";
			this.btnBrowseMixDir.UseVisualStyleBackColor = true;
			this.btnBrowseMixDir.Click += new System.EventHandler(this.btnBrowseMixDir_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(132, 120);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(85, 13);
			this.label2.TabIndex = 5;
			this.label2.Text = "Encoding quality";
			this.label2.Visible = false;
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(131, 15);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(298, 20);
			this.textBox1.TabIndex = 1;
			this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(132, 98);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(92, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Compression level";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(28, 18);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(54, 13);
			this.label8.TabIndex = 0;
			this.label8.Text = "Input map";
			// 
			// numericUpDown2
			// 
			this.numericUpDown2.Location = new System.Drawing.Point(230, 119);
			this.numericUpDown2.Name = "numericUpDown2";
			this.numericUpDown2.Size = new System.Drawing.Size(43, 20);
			this.numericUpDown2.TabIndex = 6;
			this.numericUpDown2.Value = new decimal(new int[] {
            90,
            0,
            0,
            0});
			this.numericUpDown2.Visible = false;
			this.numericUpDown2.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
			// 
			// btnBrowseInput
			// 
			this.btnBrowseInput.Location = new System.Drawing.Point(435, 15);
			this.btnBrowseInput.Name = "btnBrowseInput";
			this.btnBrowseInput.Size = new System.Drawing.Size(75, 23);
			this.btnBrowseInput.TabIndex = 2;
			this.btnBrowseInput.Text = "Browse";
			this.btnBrowseInput.UseVisualStyleBackColor = true;
			this.btnBrowseInput.Click += new System.EventHandler(this.button1_Click);
			// 
			// checkBox1
			// 
			this.checkBox1.AutoSize = true;
			this.checkBox1.Checked = true;
			this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBox1.Location = new System.Drawing.Point(42, 119);
			this.checkBox1.Name = "checkBox1";
			this.checkBox1.Size = new System.Drawing.Size(81, 17);
			this.checkBox1.TabIndex = 4;
			this.checkBox1.Text = "Output JPG";
			this.checkBox1.UseVisualStyleBackColor = true;
			this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
			// 
			// PNG
			// 
			this.PNG.AutoSize = true;
			this.PNG.Location = new System.Drawing.Point(42, 97);
			this.PNG.Name = "PNG";
			this.PNG.Size = new System.Drawing.Size(84, 17);
			this.PNG.TabIndex = 0;
			this.PNG.Text = "Output PNG";
			this.PNG.UseVisualStyleBackColor = true;
			this.PNG.CheckedChanged += new System.EventHandler(this.PNG_CheckedChanged);
			// 
			// nudCompression
			// 
			this.nudCompression.Location = new System.Drawing.Point(230, 97);
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
			this.nudCompression.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
			// 
			// button4
			// 
			this.button4.Location = new System.Drawing.Point(458, 440);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(75, 23);
			this.button4.TabIndex = 5;
			this.button4.Text = "Render map";
			this.button4.UseVisualStyleBackColor = true;
			this.button4.Click += new System.EventHandler(this.button4_Click);
			// 
			// textBox5
			// 
			this.textBox5.Location = new System.Drawing.Point(80, 440);
			this.textBox5.Name = "textBox5";
			this.textBox5.Size = new System.Drawing.Size(361, 20);
			this.textBox5.TabIndex = 4;
			// 
			// label12
			// 
			this.label12.Location = new System.Drawing.Point(18, 443);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(62, 17);
			this.label12.TabIndex = 3;
			this.label12.Text = "Command";
			// 
			// openFileDialog1
			// 
			this.openFileDialog1.FileName = "openFileDialog1";
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.textBox6);
			this.groupBox4.Location = new System.Drawing.Point(12, 480);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(532, 176);
			this.groupBox4.TabIndex = 6;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Log";
			this.groupBox4.Visible = false;
			// 
			// textBox6
			// 
			this.textBox6.Location = new System.Drawing.Point(9, 19);
			this.textBox6.Multiline = true;
			this.textBox6.Name = "textBox6";
			this.textBox6.ReadOnly = true;
			this.textBox6.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textBox6.Size = new System.Drawing.Size(518, 151);
			this.textBox6.TabIndex = 0;
			// 
			// MainForm
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(576, 657);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.label12);
			this.Controls.Add(this.textBox5);
			this.Controls.Add(this.button4);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.gbInput);
			this.Name = "MainForm";
			this.Text = "RA2/YR Map Renderer by Frank Razenberg";
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.gbInput_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.gbInput_DragEnter);
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.gbInput.ResumeLayout(false);
			this.gbInput.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nudCompression)).EndInit();
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.CheckBox checkBox2;
		private System.Windows.Forms.CheckBox checkBox3;
		private System.Windows.Forms.CheckBox checkBox4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.GroupBox gbInput;
		private System.Windows.Forms.Button btnBrowseInput;
		private System.Windows.Forms.TextBox tbRenderProg;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Button btnBrowseRenderer;
		private System.Windows.Forms.TextBox textBox2;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Button btnBrowseMixDir;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Button button4;
		private System.Windows.Forms.TextBox textBox5;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.TextBox textBox6;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
		private System.Windows.Forms.TextBox tbCustomOutput;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.RadioButton radioButton2;
		private System.Windows.Forms.RadioButton radioButton1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown numericUpDown2;
		private System.Windows.Forms.CheckBox checkBox1;
		private System.Windows.Forms.CheckBox PNG;
		private System.Windows.Forms.NumericUpDown nudCompression;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.RadioButton radioButton6;
		private System.Windows.Forms.RadioButton radioButton8;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.RadioButton radioButton4;
		private System.Windows.Forms.RadioButton rbForceRA2;
		private System.Windows.Forms.RadioButton radioButton3;
		private System.Windows.Forms.RadioButton rbForceFS;
		private System.Windows.Forms.RadioButton rbForceTS;

	}
}

