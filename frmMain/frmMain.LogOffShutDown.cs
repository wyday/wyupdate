using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace wyUpdate
{
    public partial class frmMain
    {
        [DllImport("user32.dll")]
        public extern static bool ShutdownBlockReasonCreate(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] string pwszReason);

        [DllImport("user32.dll")]
        public extern static bool ShutdownBlockReasonDestroy(IntPtr hWnd);

        private bool logOffBlocked;

        private void BlockLogOff(bool block)
        {
            logOffBlocked = block;

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
                //TODO: show window, bring to front
                return;
            }


            base.WndProc(ref aMessage);
        }
    }
}