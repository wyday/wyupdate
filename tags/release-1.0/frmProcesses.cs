using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using wyUpdate.Common;

namespace wyUpdate
{
    public partial class frmProcesses : Form
    {
        ClientLanguage clientLang;
        List<Process> runningProcesses = new List<Process>();
        FileInfo[] filenames = new FileInfo[0];

        private const int SidePadding = 12;

        public frmProcesses(FileInfo[] files, ClientLanguage cLang)
        {
            //sets to SegoeUI on Vista
            this.Font = SystemFonts.MessageBoxFont;

            InitializeComponent();

            filenames = files;
            clientLang = cLang;

            //translate the buttons & text
            this.Text = clientLang.ProcessDialog.Title;

            btnCloseProc.Text = clientLang.ClosePrc;
            btnCloseAll.Text = clientLang.CloseAllPrc;
            btnCancel.Text = clientLang.CancelUpdate;

            //begin checking the for the filenames
            CheckProcesses();

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);

            //reposition buttons
            btnCloseAll.Left = btnCloseProc.Right + 10;

            //position all the components
            UpdateSizes();
        }

        Rectangle m_DescripRect;

        private void UpdateSizes()
        {
            m_DescripRect = new Rectangle(new Point(SidePadding, SidePadding),
                TextRenderer.MeasureText(clientLang.ProcessDialog.Content,
                    Font,
                    new Size(ClientRectangle.Width - SidePadding *2, 1),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding));

            listProc.Location = new Point(SidePadding, m_DescripRect.Bottom + 5);
            listProc.Width = ClientRectangle.Width - SidePadding * 2;
            listProc.Height = ClientRectangle.Height - listProc.Top - (ClientRectangle.Height - btnCloseProc.Top) - 5;
        }

        private void closeProc_Click(object sender, EventArgs e)
        {
            try
            {
                runningProcesses[listProc.SelectedIndex].CloseMainWindow();
            }
            catch (Exception) { }



            if (CheckProcesses() == 0)
            {
                //exit, return OK
                this.DialogResult = DialogResult.OK;
            }
        }

        private void closeAll_Click(object sender, EventArgs e)
        {
            foreach (Process proc in runningProcesses)
            {
                proc.CloseMainWindow();
            }

            if (CheckProcesses() == 0)
            {
                //exit, return OK
                this.DialogResult = DialogResult.OK;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            chkProc.Enabled = false;

            DialogResult dResult = MessageBox.Show(clientLang.CancelDialog.Content, clientLang.CancelDialog.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);

            if (dResult == DialogResult.Yes)
                this.DialogResult = DialogResult.Cancel;
            else
            {
                DialogResult = DialogResult.None;
                chkProc.Enabled = true;
            }
        }

        private int CheckProcesses()
        {
            Process[] aProcess = Process.GetProcesses();

            //temporarily storing the running processes
            List<Process> rProcs = new List<Process>();

            //Any processes left running (that need to be shut down)
            bool procLeft = false;


            foreach (Process proc in aProcess)
            {
                foreach (FileInfo filename in filenames)
                {
                    try
                    {
                        if (proc.MainModule.FileName.ToLower() == filename.FullName.ToLower())
                        {
                            //if the running process is not this wyUpdate client
                            if (!InstallUpdate.ProcessIsSelf(proc.MainModule.FileName))
                            {
                                //add the running process to the list
                                rProcs.Add(proc);
                                //there are processes still open
                                procLeft = true;
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }

            //check if list of process is sames as the semi-global one (runningProcesses)
            //if diff, update listbox.

            if (!SameProcs(ref rProcs, ref runningProcesses))
            {
                //update the running processes array
                runningProcesses = rProcs;

                listProc.Items.Clear();
                foreach (Process proc in runningProcesses)
                {
                    listProc.Items.Add(proc.MainWindowTitle);
                }
            }



            //return 0 if there are no processes that need to be closed
            if (procLeft == true)
            {
                return 1;
            }
            else
            {
                return 0;
            }
            
        }

        private bool SameProcs(ref List<Process> procs1, ref List<Process> procs2)
        {
            if (procs1.Count != procs2.Count)
                return false;

            for (int i = 0; i < procs1.Count; i++)
            {
                if (procs1[i].Id != procs2[i].Id || procs1[i].MainWindowTitle != procs2[i].MainWindowTitle)
                {
                    return false;
                }
            }

            return true;
        }

        private void chkProc_Tick(object sender, EventArgs e)
        {
            if (CheckProcesses() == 0)
            {
                this.DialogResult = DialogResult.OK;
                chkProc.Enabled = false;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            TextRenderer.DrawText(e.Graphics, clientLang.ProcessDialog.Content, Font, m_DescripRect, ForeColor,
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);

            base.OnPaint(e);
        }

        private void frmProcesses_Resize(object sender, EventArgs e)
        {
            UpdateSizes();
        }
    }
}