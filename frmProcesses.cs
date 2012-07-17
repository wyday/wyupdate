using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace wyUpdate
{
    public partial class frmProcesses : Form
    {
        readonly ClientLanguage clientLang;
        readonly List<FileInfo> filenames;
        List<Process> runningProcesses;

        const int SidePadding = 12;

        readonly BackgroundWorker bw = new BackgroundWorker();

        public frmProcesses(List<FileInfo> files, List<Process> rProcesses, ClientLanguage cLang)
        {
            runningProcesses = rProcesses;

            //sets to SegoeUI on Vista
            Font = SystemFonts.MessageBoxFont;

            InitializeComponent();

            filenames = files;
            clientLang = cLang;

            //translate the buttons & text
            Text = clientLang.ProcessDialog.Title;

            btnCloseProc.Text = clientLang.ClosePrc;
            btnCloseAll.Text = clientLang.CloseAllPrc;
            btnCancel.Text = clientLang.CancelUpdate;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);

            //reposition buttons
            btnCloseAll.Left = btnCloseProc.Right + 10;

            //position all the components
            UpdateSizes();

            // update the list with the running proccesses
            UpdateList();

            bw.WorkerSupportsCancellation = true;
            bw.DoWork += bw_DoWork;
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;

            //begin checking the for the filenames
            bw.RunWorkerAsync();
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            // temp storing the running processes
            List<Process> rProcs = new List<Process>();

            foreach (FileInfo filename in filenames)
            {
                Process[] aProcess = Process.GetProcessesByName(filename.Name.Replace(filename.Extension, ""));

                foreach (Process proc in aProcess)
                {
                    try
                    {
                        //are one of the exe's in baseDir running?
                        if (proc.MainModule != null && string.Equals(proc.MainModule.FileName, filename.FullName, StringComparison.OrdinalIgnoreCase)

                            //if the running process is not this wyUpdate instance
                            && !InstallUpdate.ProcessIsSelf(proc.MainModule.FileName))
                        {
                            rProcs.Add(proc);
                        }
                    }
                    catch { }
                }
            }

            // remove any closed processes
            for (int i = 0; i < rProcs.Count; i++)
            {
                if (rProcs[i].HasExited)
                {
                    rProcs.RemoveAt(i);
                    i--;
                }
            }

            e.Result = rProcs.Count > 0 ? rProcs : null;
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result == null)
            {
                // we're done, close the form
                DialogResult = DialogResult.OK;
                chkProc.Enabled = false;
                return;
            }

            List<Process> rProcs = (List<Process>)e.Result;

            //check if list of process is sames as the semi-global one (runningProcesses)
            //if diff, update listbox.
            if (!SameProcs(rProcs, runningProcesses))
            {
                //update the running processes array
                runningProcesses = rProcs;
                UpdateList();
            }
        }

        void UpdateList()
        {
            listProc.Items.Clear();

            foreach (Process proc in runningProcesses)
            {
                listProc.Items.Add(proc.MainWindowTitle + " (" + proc.ProcessName + ".exe)");
            }

            listProc.SelectedIndex = 0;
        }

        Rectangle m_DescripRect;

        void UpdateSizes()
        {
            int newWidth = Width - ClientRectangle.Width + btnCloseAll.Right + 35 + btnCancel.Width;
            MinimumSize = new Size(newWidth, 178);

            if (Width < newWidth)
                Width = newWidth;

            m_DescripRect = new Rectangle(new Point(SidePadding, SidePadding),
                TextRenderer.MeasureText(clientLang.ProcessDialog.Content,
                    Font,
                    new Size(ClientRectangle.Width - SidePadding *2, 1),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding));

            listProc.Location = new Point(SidePadding, m_DescripRect.Bottom + 5);
            listProc.Width = ClientRectangle.Width - SidePadding * 2;
            listProc.Height = ClientRectangle.Height - listProc.Top - (ClientRectangle.Height - btnCloseProc.Top) - 5;
        }

        void closeProc_Click(object sender, EventArgs e)
        {
            try
            {
                // if there's no window handle, just kill the process
                if (runningProcesses[listProc.SelectedIndex].MainWindowHandle == IntPtr.Zero)
                    runningProcesses[listProc.SelectedIndex].Kill();
                else
                    runningProcesses[listProc.SelectedIndex].CloseMainWindow();

                string procDets = (string)listProc.Items[listProc.SelectedIndex];

                if (!procDets.StartsWith("[closing]"))
                    procDets = "[closing] " + procDets;

                listProc.Items[listProc.SelectedIndex] = procDets;
            }
            catch { }

            if (!bw.IsBusy)
                bw.RunWorkerAsync();
        }

        void closeAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < runningProcesses.Count; i++)
            {
                try
                {
                    // if there's no window handle, just kill the process
                    if (runningProcesses[i].MainWindowHandle == IntPtr.Zero)
                        runningProcesses[i].Kill();
                    else
                        runningProcesses[i].CloseMainWindow();

                    string procDets = (string)listProc.Items[i];

                    if (!procDets.StartsWith("[closing]"))
                        procDets = "[closing] " + procDets;

                    listProc.Items[i] = procDets;
                }
                catch { }
            }

            if (!bw.IsBusy)
                bw.RunWorkerAsync();
        }

        void btnCancel_Click(object sender, EventArgs e)
        {
            chkProc.Enabled = false;

            DialogResult dResult = MessageBox.Show(clientLang.CancelDialog.Content, clientLang.CancelDialog.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);

            if (dResult == DialogResult.Yes)
                DialogResult = DialogResult.Cancel;
            else
            {
                DialogResult = DialogResult.None;
                chkProc.Enabled = true;
            }
        }

        public static bool SameProcs(List<Process> procs1, List<Process> procs2)
        {
            if (procs1.Count != procs2.Count)
                return false;

            for (int i = 0; i < procs1.Count; i++)
            {
                if (procs1[i].Id != procs2[i].Id || procs1[i].MainWindowTitle != procs2[i].MainWindowTitle)
                    return false;
            }

            return true;
        }

        void chkProc_Tick(object sender, EventArgs e)
        {
            if (!bw.IsBusy)
                bw.RunWorkerAsync();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            TextRenderer.DrawText(e.Graphics, clientLang.ProcessDialog.Content, Font, m_DescripRect, ForeColor,
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);

            base.OnPaint(e);
        }

        void frmProcesses_Resize(object sender, EventArgs e)
        {
            UpdateSizes();
        }
    }
}