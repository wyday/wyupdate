using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace wyUpdate
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            Application.EnableVisualStyles();

            frmMain mainForm = new frmMain(args);

            StringBuilder mutexName = new StringBuilder("Local\\wyUpdate-" + mainForm.update.GUID);

            if (mainForm.IsAdmin)
                mutexName.Append('a');

            if (mainForm.SelfUpdateState == SelfUpdateState.FullUpdate)
                mutexName.Append('s');

            Mutex mutex = new Mutex(true, mutexName.ToString());

            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                Application.Run(mainForm);

                mutex.ReleaseMutex();
            }
            else
            {
                FocusOtherProcess();
            }

            return mainForm.ReturnCode;
        }



        [DllImport("user32")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32")]
        static extern int ShowWindow(IntPtr hWnd, int swCommand);
        [DllImport("user32")]
        static extern bool IsIconic(IntPtr hWnd);

        public static void FocusOtherProcess()
        {
            Process proc = Process.GetCurrentProcess();

            // Using Process.ProcessName does not function properly when
            // the actual name exceeds 15 characters. Using the assembly 
            // name takes care of this quirk and is more accruate than 
            // other work arounds.

            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;

            foreach (Process otherProc in Process.GetProcessesByName(assemblyName))
            {
                //ignore "this" process, and ignore wyUpdate with a different filename

                if (proc.Id != otherProc.Id 
                        && otherProc.MainModule != null && proc.MainModule != null 
                        && proc.MainModule.FileName == otherProc.MainModule.FileName)
                {
                    // Found a "same named process".
                    // Assume it is the one we want brought to the foreground.
                    // Use the Win32 API to bring it to the foreground.

                    IntPtr hWnd = otherProc.MainWindowHandle;

                    if (IsIconic(hWnd))
                        ShowWindow(hWnd, 9); //SW_RESTORE

                    SetForegroundWindow(hWnd);
                    break;
                }
            }
        }
    }
}