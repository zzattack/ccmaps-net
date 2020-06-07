using System;
using System.Windows.Forms;

namespace CNCMaps.GUI {
	public class SubmitBug : Form {
		private string _email;
		public string Email {
			get { return _email; }
			set {
				_email = value;
				tbEmail.Text = _email;
			}
		}
		public SubmitBug() {
			InitializeComponent();
		}

		private void btnOK_Click(object sender, EventArgs e) {
			Ok();
		}

		private void btnCancel_Click(object sender, EventArgs e) {
			Cancel();
		}

		private void GlobalKeyDown(object sender, KeyEventArgs e) {
			if (e.KeyCode == Keys.Escape) Cancel();
			else if (e.KeyCode == Keys.Enter) Ok();
		}

		private void tbEmail_TextChanged(object sender, EventArgs e) {
			_email = tbEmail.Text;
		}

		private void Ok() {
			DialogResult = DialogResult.OK;
			Close();
		}

		private void Cancel() {
			DialogResult = DialogResult.Cancel;
			Close();

		}
		#region Windows Form Designer generated code

		private Button btnOK;
		private Button btnCancel;
		private Label label1;
		private Label label2;
		private TextBox tbEmail;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SubmitBug));
			this.tbEmail = new System.Windows.Forms.TextBox();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// tbEmail
			// 
			this.tbEmail.Location = new System.Drawing.Point(27, 80);
			this.tbEmail.Name = "tbEmail";
			this.tbEmail.Size = new System.Drawing.Size(228, 20);
			this.tbEmail.TabIndex = 0;
			this.tbEmail.TextChanged += new System.EventHandler(this.tbEmail_TextChanged);
			this.tbEmail.KeyDown += new System.Windows.Forms.KeyEventHandler(this.GlobalKeyDown);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(277, 77);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(64, 24);
			this.btnCancel.TabIndex = 2;
			this.btnCancel.Text = "&Cancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			this.btnCancel.KeyDown += new System.Windows.Forms.KeyEventHandler(this.GlobalKeyDown);
			// 
			// btnOK
			// 
			this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(277, 47);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(64, 24);
			this.btnOK.TabIndex = 1;
			this.btnOK.Text = "&OK";
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			this.btnOK.KeyDown += new System.Windows.Forms.KeyEventHandler(this.GlobalKeyDown);
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(18, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(321, 33);
			this.label1.TabIndex = 3;
			this.label1.Text = "Rendering appears to have failed. Would you like to transmit a bug report contain" +
	"ing the error log and map to frank@zzattack.org?";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(27, 63);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(220, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "Email (optional, allows author to contact you):";
			// 
			// SubmitBug
			// 
			this.ClientSize = new System.Drawing.Size(353, 115);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.tbEmail);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Location = new System.Drawing.Point(22, 29);
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size(369, 154);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(369, 154);
			this.Name = "SubmitBug";
			this.Text = "Submit bug report?";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion Windows Form Designer generated code

	}

}