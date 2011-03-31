using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace wyUpdate
{
    public partial class frmMain
    {
        [DllImport("user32.dll")]
        extern static bool ShutdownBlockReasonCreate(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] string pwszReason);

        [DllImport("user32.dll")]
        extern static bool ShutdownBlockReasonDestroy(IntPtr hWnd);

        bool logOffBlocked;

        void BlockLogOff(bool block)
        {
            logOffBlocked = block;

            // These API calls don't exist on pre-Vista Windows, but are
            // *required* for Vista+ systems.
            // Hence the try{}catch{} block. We could detect the version
            // of Windows, but that can be spoofed by stupid users.

            // Thus it's better to just try to blindly call the function
            // and let it silently fail if it doesn't exist.
            try
            {
                if (block)
                    ShutdownBlockReasonCreate(Handle, clientLang.LogOffError);
                else
                    ShutdownBlockReasonDestroy(Handle);
            }
            catch { }
        }

        protected override void WndProc(ref Message aMessage)
        {
            //WM_QUERYENDSESSION = 0x0011
            //WM_ENDSESSION = 0x0016
            if (logOffBlocked && (aMessage.Msg == 0x0011 || aMessage.Msg == 0x0016))
            {
                return;
            }

            base.WndProc(ref aMessage);
        }
    }
}