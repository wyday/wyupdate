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
            this.btnCancelUpdate = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnCancelUpdate
            // 
            this.btnCancelUpdate.Location = new System.Drawing.Point(267, 197);
            this.btnCancelUpdate.Name = "btnCancelUpdate";
            this.btnCancelUpdate.Size = new System.Drawing.Size(91, 28);
            this.btnCancelUpdate.TabIndex = 0;
            this.btnCancelUpdate.Text = "Cancel Update";
            this.btnCancelUpdate.UseVisualStyleBackColor = true;
            // 
            // frmFilesInUse
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(370, 237);
            this.ControlBox = false;
            this.Controls.Add(this.btnCancelUpdate);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmFilesInUse";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "frmFilesInUse";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnCancelUpdate;
    }
}