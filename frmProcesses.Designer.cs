namespace wyUpdate
{
    partial class frmProcesses
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btnCloseProc = new System.Windows.Forms.Button();
            this.btnCloseAll = new System.Windows.Forms.Button();
            this.listProc = new System.Windows.Forms.ListBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.chkProc = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // btnCloseProc
            // 
            this.btnCloseProc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCloseProc.AutoSize = true;
            this.btnCloseProc.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCloseProc.Location = new System.Drawing.Point(12, 214);
            this.btnCloseProc.Name = "btnCloseProc";
            this.btnCloseProc.Size = new System.Drawing.Size(88, 22);
            this.btnCloseProc.TabIndex = 0;
            this.btnCloseProc.Click += new System.EventHandler(this.closeProc_Click);
            // 
            // btnCloseAll
            // 
            this.btnCloseAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCloseAll.AutoSize = true;
            this.btnCloseAll.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCloseAll.Location = new System.Drawing.Point(106, 214);
            this.btnCloseAll.Name = "btnCloseAll";
            this.btnCloseAll.Size = new System.Drawing.Size(113, 22);
            this.btnCloseAll.TabIndex = 1;
            this.btnCloseAll.Click += new System.EventHandler(this.closeAll_Click);
            // 
            // listProc
            // 
            this.listProc.FormattingEnabled = true;
            this.listProc.HorizontalScrollbar = true;
            this.listProc.IntegralHeight = false;
            this.listProc.Location = new System.Drawing.Point(3, 60);
            this.listProc.Name = "listProc";
            this.listProc.Size = new System.Drawing.Size(367, 117);
            this.listProc.TabIndex = 0;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.AutoSize = true;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(279, 214);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(92, 22);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // chkProc
            // 
            this.chkProc.Enabled = true;
            this.chkProc.Interval = 2000;
            this.chkProc.Tick += new System.EventHandler(this.chkProc_Tick);
            // 
            // frmProcesses
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(383, 248);
            this.ControlBox = false;
            this.Controls.Add(this.btnCloseProc);
            this.Controls.Add(this.btnCloseAll);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.listProc);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(349, 178);
            this.Name = "frmProcesses";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = " ";
            this.Resize += new System.EventHandler(this.frmProcesses_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCloseProc;
        private System.Windows.Forms.Button btnCloseAll;
        private System.Windows.Forms.ListBox listProc;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Timer chkProc;
    }
}