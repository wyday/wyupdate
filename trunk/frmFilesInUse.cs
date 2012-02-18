using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace wyUpdate
{
    public partial class frmFilesInUse : Form
    {
        [StructLayout(LayoutKind.Sequential)]
        struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public FILETIME ProcessStartTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;

            [MarshalAs(UnmanagedType.ByValTStr,
                SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            public string strAppName;

            [MarshalAs(UnmanagedType.ByValTStr,
                SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            public string strServiceShortName;

            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }

        enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll")]
        static extern int RmEndSession(uint pSessionHandle);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        static extern int RmRegisterResources(uint pSessionHandle,
                                              UInt32 nFiles,
                                              string[] rgsFilenames,
                                              UInt32 nApplications,
                                              [In] RM_UNIQUE_PROCESS[] rgApplications,
                                              UInt32 nServices,
                                              string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll")]
        static extern int RmGetList(uint dwSessionHandle,
                                    out uint pnProcInfoNeeded,
                                    ref uint pnProcInfo,
                                    [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
                                    ref uint lpdwRebootReasons);

        const int RmRebootReasonNone = 0;
        const int CCH_RM_MAX_APP_NAME = 255;
        const int CCH_RM_MAX_SVC_NAME = 63;


        private const int SidePadding = 12;
        readonly ClientLanguage clientLang;
        public bool CancelUpdate;

        bool showingProcesses;

        public frmFilesInUse(ClientLanguage cLang, string filename)
        {
            InitializeComponent();

            clientLang = cLang;

            // translate the buttons & text
            Text = clientLang.FilesInUseDialog.Title;
            btnCancel.Text = clientLang.CancelUpdate;

            txtFile.Text = filename;

            if (VistaTools.AtLeastVista())
            {
                // get the list of processes using the file
                List<Process> processes = null;
                try
                {
                    processes = GetProcessesUsingFiles(new[] {filename});
                }
                catch { }

                if (processes != null && processes.Count > 0)
                {
                    listProc.Items.Clear();

                    foreach (Process proc in processes)
                    {
                        listProc.Items.Add(proc.MainWindowTitle + " (" + proc.ProcessName + ".exe)");
                    }

                    // translate the lblProc
                    lblProc.Text = clientLang.FilesInUseDialog.SubTitle;

                    // show the list box of the running processes
                    lblProc.Visible = true;
                    listProc.Visible = true;

                    showingProcesses = true;
                }
            }

            SetStyle(ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.UserPaint, true);

            // position all the components
            UpdateSizes();
        }

        // http://blogs.msdn.com/b/oldnewthing/archive/2012/02/17/10268840.aspx (How do I find out which process has a file open?)
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa373682(v=vs.85).aspx  (Using Restart Manager with a Secondary Installer )
        // http://msdn.microsoft.com/en-us/magazine/cc163450.aspx ( Restart Manager -- C# example)
        public static List<Process> GetProcessesUsingFiles(IList<string> filePaths)
        {
            uint sessionHandle;
            List<Process> processes = null;

            // Create a restart manager session
            int rv = RmStartSession(out sessionHandle, 0, Guid.NewGuid().ToString("N"));

            if (rv != 0)
                throw new Win32Exception();

            try
            {
                // Let the restart manager know what files we’re interested in
                string[] pathStrings = new string[filePaths.Count];
                filePaths.CopyTo(pathStrings, 0);
                rv = RmRegisterResources(sessionHandle, (uint)pathStrings.Length, pathStrings, 0, null, 0, null);

                if (rv != 0)
                    throw new Win32Exception();

                // Ask the restart manager what other applications 
                // are using those files
                const int ERROR_MORE_DATA = 234;
                uint pnProcInfoNeeded = 0,
                     pnProcInfo = 0,
                     lpdwRebootReasons = RmRebootReasonNone;

                //Note: there's a race condition here -- the first call to RmGetList() returns
                //      the total number of process. However, when we call RmGetList() again to get
                //      the actual processes this number may have increased.
                rv = RmGetList(sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

                if (rv == ERROR_MORE_DATA)
                {
                    // Create an array to store the process results
                    RM_PROCESS_INFO[] processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
                    pnProcInfo = pnProcInfoNeeded;

                    // Get the list
                    rv = RmGetList(sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
                    if (rv == 0)
                    {
                        processes = new List<Process>((int)pnProcInfo);

                        // Enumerate all of the results and add them to the 
                        // list to be returned
                        for (int i = 0; i < pnProcInfo; i++)
                        {
                            try
                            {
                                processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId));
                            }
                            // catch the error -- in case the process is no longer running
                            catch (ArgumentException) { }
                        }
                    }
                    else
                        throw new Win32Exception();
                }
                else if (rv != 0)
                    throw new Win32Exception();
            }
            finally
            {
                // Close the resource manager
                RmEndSession(sessionHandle);
            }

            return processes;
        }

        void btnCancel_Click(object sender, EventArgs e)
        {
            CancelUpdate = DialogResult.Yes ==
                               MessageBox.Show(clientLang.CancelDialog.Content, clientLang.CancelDialog.Title,
                                               MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation,
                                               MessageBoxDefaultButton.Button2);

            // prevent closing this window if the user isn't canceling
            if (!CancelUpdate)
                DialogResult = DialogResult.None;
        }

        Rectangle m_DescripRect;

        void UpdateSizes()
        {
            if (txtFile == null || clientLang == null)
                return;

            m_DescripRect = new Rectangle(new Point(SidePadding, SidePadding),
                TextRenderer.MeasureText(clientLang.FilesInUseDialog.Content,
                    Font,
                    new Size(ClientRectangle.Width - SidePadding * 2, 1),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding));

            txtFile.Location = new Point(SidePadding, m_DescripRect.Bottom + 5);
            txtFile.Width = ClientRectangle.Width - SidePadding * 2;

            int txtHeight = ClientRectangle.Height - txtFile.Top - (ClientRectangle.Height - btnCancel.Top) - 5;

            if (showingProcesses)
            {
                txtFile.Height = (txtHeight / 2) - 5 - lblProc.Height;

                lblProc.Location = new Point(SidePadding, txtFile.Bottom + 12);

                listProc.Height = txtFile.Height;
                listProc.Location = new Point(SidePadding, lblProc.Bottom + 5);
                listProc.Width = ClientRectangle.Width - SidePadding * 2;
            }
            else
                txtFile.Height = txtHeight;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            UpdateSizes();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            TextRenderer.DrawText(e.Graphics, clientLang.FilesInUseDialog.Content, Font, m_DescripRect, ForeColor,
                                  TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);

            base.OnPaint(e);
        }
    }
}
