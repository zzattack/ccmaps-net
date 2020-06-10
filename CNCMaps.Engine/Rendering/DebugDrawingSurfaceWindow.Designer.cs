namespace CNCMaps.Engine.Rendering {
	partial class DebugDrawingSurfaceWindow {
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
			this.panel1 = new System.Windows.Forms.Panel();
			this.tabs = new System.Windows.Forms.TabControl();
			this.tpMap = new System.Windows.Forms.TabPage();
			this.canvasMap = new CNCMaps.Engine.Rendering.ZoomableCanvas();
			this.tpHeightmap = new System.Windows.Forms.TabPage();
			this.canvasHeight = new CNCMaps.Engine.Rendering.ZoomableCanvas();
			this.tpShadowMap = new System.Windows.Forms.TabPage();
			this.canvasShadows = new CNCMaps.Engine.Rendering.ZoomableCanvas();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.panel1.SuspendLayout();
			this.tabs.SuspendLayout();
			this.tpMap.SuspendLayout();
			this.tpHeightmap.SuspendLayout();
			this.tpShadowMap.SuspendLayout();
			this.statusStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.AutoScroll = true;
			this.panel1.Controls.Add(this.tabs);
			this.panel1.Location = new System.Drawing.Point(9, 9);
			this.panel1.Margin = new System.Windows.Forms.Padding(0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(930, 545);
			this.panel1.TabIndex = 1;
			// 
			// tabs
			// 
			this.tabs.Controls.Add(this.tpMap);
			this.tabs.Controls.Add(this.tpHeightmap);
			this.tabs.Controls.Add(this.tpShadowMap);
			this.tabs.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabs.Location = new System.Drawing.Point(0, 0);
			this.tabs.Name = "tabs";
			this.tabs.SelectedIndex = 0;
			this.tabs.Size = new System.Drawing.Size(930, 545);
			this.tabs.TabIndex = 0;
			this.tabs.SelectedIndexChanged += new System.EventHandler(this.tabs_SelectedIndexChanges);
			// 
			// tpMap
			// 
			this.tpMap.Controls.Add(this.canvasMap);
			this.tpMap.Location = new System.Drawing.Point(4, 22);
			this.tpMap.Name = "tpMap";
			this.tpMap.Padding = new System.Windows.Forms.Padding(3);
			this.tpMap.Size = new System.Drawing.Size(922, 519);
			this.tpMap.TabIndex = 0;
			this.tpMap.Text = "Map";
			this.tpMap.UseVisualStyleBackColor = true;
			// 
			// canvasMap
			// 
			this.canvasMap.Dock = System.Windows.Forms.DockStyle.Fill;
			this.canvasMap.Image = null;
			this.canvasMap.Location = new System.Drawing.Point(3, 3);
			this.canvasMap.Name = "canvasMap";
			this.canvasMap.Size = new System.Drawing.Size(916, 513);
			this.canvasMap.TabIndex = 0;
			this.canvasMap.VirtualMode = false;
			this.canvasMap.VirtualSize = new System.Drawing.Size(0, 0);
			this.canvasMap.MouseDown += new System.Windows.Forms.MouseEventHandler(this.canvas_MouseDown);
			this.canvasMap.MouseMove += new System.Windows.Forms.MouseEventHandler(this.canvas_MouseMove);
			// 
			// tpHeightmap
			// 
			this.tpHeightmap.Controls.Add(this.canvasHeight);
			this.tpHeightmap.Location = new System.Drawing.Point(4, 22);
			this.tpHeightmap.Name = "tpHeightmap";
			this.tpHeightmap.Padding = new System.Windows.Forms.Padding(3);
			this.tpHeightmap.Size = new System.Drawing.Size(922, 519);
			this.tpHeightmap.TabIndex = 1;
			this.tpHeightmap.Text = "Height";
			this.tpHeightmap.UseVisualStyleBackColor = true;
			// 
			// canvasHeight
			// 
			this.canvasHeight.Dock = System.Windows.Forms.DockStyle.Fill;
			this.canvasHeight.Image = null;
			this.canvasHeight.Location = new System.Drawing.Point(3, 3);
			this.canvasHeight.Name = "canvasHeight";
			this.canvasHeight.Size = new System.Drawing.Size(916, 513);
			this.canvasHeight.TabIndex = 1;
			this.canvasHeight.VirtualMode = false;
			this.canvasHeight.VirtualSize = new System.Drawing.Size(0, 0);
			this.canvasHeight.MouseDown += new System.Windows.Forms.MouseEventHandler(this.canvas_MouseDown);
			this.canvasHeight.MouseMove += new System.Windows.Forms.MouseEventHandler(this.canvas_MouseMove);
			// 
			// tpShadowMap
			// 
			this.tpShadowMap.Controls.Add(this.canvasShadows);
			this.tpShadowMap.Location = new System.Drawing.Point(4, 22);
			this.tpShadowMap.Name = "tpShadowMap";
			this.tpShadowMap.Size = new System.Drawing.Size(922, 519);
			this.tpShadowMap.TabIndex = 2;
			this.tpShadowMap.Text = "Shadows";
			this.tpShadowMap.UseVisualStyleBackColor = true;
			// 
			// canvasShadows
			// 
			this.canvasShadows.Dock = System.Windows.Forms.DockStyle.Fill;
			this.canvasShadows.Image = null;
			this.canvasShadows.Location = new System.Drawing.Point(0, 0);
			this.canvasShadows.Name = "canvasShadows";
			this.canvasShadows.Size = new System.Drawing.Size(922, 519);
			this.canvasShadows.TabIndex = 2;
			this.canvasShadows.VirtualMode = false;
			this.canvasShadows.VirtualSize = new System.Drawing.Size(0, 0);
			this.canvasShadows.MouseDown += new System.Windows.Forms.MouseEventHandler(this.canvas_MouseDown);
			this.canvasShadows.MouseMove += new System.Windows.Forms.MouseEventHandler(this.canvas_MouseMove);
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
			this.statusStrip1.Location = new System.Drawing.Point(0, 585);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(948, 22);
			this.statusStrip1.TabIndex = 2;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(118, 17);
			this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// DebugDrawingSurfaceWindow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(948, 607);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.panel1);
			this.KeyPreview = true;
			this.Name = "DebugDrawingSurfaceWindow";
			this.Text = "Preview Window";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.DebugDrawingSurfaceWindow_FormClosed);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.form_KeyDown);
			this.panel1.ResumeLayout(false);
			this.tabs.ResumeLayout(false);
			this.tpMap.ResumeLayout(false);
			this.tpHeightmap.ResumeLayout(false);
			this.tpShadowMap.ResumeLayout(false);
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
		private ZoomableCanvas canvasMap;
		private System.Windows.Forms.TabControl tabs;
		private System.Windows.Forms.TabPage tpHeightmap;
		private System.Windows.Forms.TabPage tpShadowMap;
		private System.Windows.Forms.TabPage tpMap;
		private ZoomableCanvas canvasHeight;
		private ZoomableCanvas canvasShadows;
	}
}
