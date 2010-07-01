using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace wyUpdate
{
    public partial class frmFilesInUse : Form
    {
        readonly ClientLanguage clientLang;

        const int SidePadding = 12;

        public bool CancelUpdate;

        public frmFilesInUse(ClientLanguage cLang, string filename)
        {
            InitializeComponent();

            clientLang = cLang;

            // translate the buttons & text
            Text = clientLang.FilesInUseDialog.Title;
            btnCancel.Text = clientLang.CancelUpdate;

            txtFile.Text = filename;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);

            //position all the components
            UpdateSizes();
        }

        void btnCancel_Click(object sender, EventArgs e)
        {
            CancelUpdate = DialogResult.Yes ==
                               MessageBox.Show(clientLang.CancelDialog.Content, clientLang.CancelDialog.Title,
                                               MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation,
                                               MessageBoxDefaultButton.Button2);
        }

        Rectangle m_DescripRect;

        void UpdateSizes()
        {
            m_DescripRect = new Rectangle(new Point(SidePadding, SidePadding),
                TextRenderer.MeasureText(clientLang.FilesInUseDialog.Content,
                    Font,
                    new Size(ClientRectangle.Width - SidePadding * 2, 1),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding));

            txtFile.Location = new Point(SidePadding, m_DescripRect.Bottom + 5);
            txtFile.Width = ClientRectangle.Width - SidePadding * 2;
            txtFile.Height = ClientRectangle.Height - txtFile.Top - (ClientRectangle.Height - btnCancel.Top) - 5;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            TextRenderer.DrawText(e.Graphics, clientLang.FilesInUseDialog.Content, Font, m_DescripRect, ForeColor,
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);

            base.OnPaint(e);
        }

        void frmFilesInUse_Resize(object sender, EventArgs e)
        {
            UpdateSizes();
        }
    }
}
