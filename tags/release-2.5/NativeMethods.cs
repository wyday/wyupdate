using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;

namespace InstantUpdate.Common
{
    // this class just wraps some Win32 stuffthat we're going to use
    internal class NativeMethods
    {
        [DllImport("user32")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32")]
        private static extern int ShowWindow(IntPtr hWnd, int swCommand);
        [DllImport("user32")]
        private static extern bool IsIconic(IntPtr hWnd);

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
                //ignore "this" process

                if (proc.Id != otherProc.Id)
                {
                    // Found a "same named process".
                    // Assume it is the one we want brought to the foreground.
                    // Use the Win32 API to bring it to the foreground.
 
                    IntPtr hWnd = otherProc.MainWindowHandle;

                    if (IsIconic(hWnd))
                    {
                        ShowWindow(hWnd, 9); //SW_RESTORE
                    }
                    SetForegroundWindow(hWnd);
                    break;
                }
            }
        }
    }
}
