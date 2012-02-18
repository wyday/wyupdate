namespace wyUpdate
{
    partial class frmFilesInUse
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
            this.btnCancel = new System.Windows.Forms.Button();
            this.txtFile = new System.Windows.Forms.TextBox();
            this.listProc = new System.Windows.Forms.ListBox();
            this.lblProc = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.AutoSize = true;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(279, 302);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(92, 22);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // txtFile
            // 
            this.txtFile.Location = new System.Drawing.Point(12, 62);
            this.txtFile.Multiline = true;
            this.txtFile.Name = "txtFile";
            this.txtFile.ReadOnly = true;
            this.txtFile.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtFile.Size = new System.Drawing.Size(346, 100);
            this.txtFile.TabIndex = 4;
            // 
            // listProc
            // 
            this.listProc.FormattingEnabled = true;
            this.listProc.HorizontalScrollbar = true;
            this.listProc.IntegralHeight = false;
            this.listProc.Location = new System.Drawing.Point(9, 203);
            this.listProc.Name = "listProc";
            this.listProc.Size = new System.Drawing.Size(367, 93);
            this.listProc.TabIndex = 5;
            this.listProc.Visible = false;
            // 
            // lblProc
            // 
            this.lblProc.AutoSize = true;
            this.lblProc.Location = new System.Drawing.Point(12, 187);
            this.lblProc.Name = "lblProc";
            this.lblProc.Size = new System.Drawing.Size(0, 13);
            this.lblProc.TabIndex = 6;
            this.lblProc.Visible = false;
            // 
            // frmFilesInUse
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(383, 336);
            this.ControlBox = false;
            this.Controls.Add(this.lblProc);
            this.Controls.Add(this.listProc);
            this.Controls.Add(this.txtFile);
            this.Controls.Add(this.btnCancel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(350, 350);
            this.Name = "frmFilesInUse";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = " ";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox txtFile;
        private System.Windows.Forms.ListBox listProc;
        private System.Windows.Forms.Label lblProc;

    }
}