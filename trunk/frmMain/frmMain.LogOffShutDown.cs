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

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [FlagsAttribute]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001

            // Legacy flag, should not be used.
            // ES_USER_PRESENT = 0x00000004
        }

        void BlockLogOff(bool block)
        {
            logOffBlocked = block;

            if (block)
            {
                // prevent shutdown
                if (VistaTools.AtLeastVista())
                    ShutdownBlockReasonCreate(Handle, clientLang.LogOffError);

                // prevent the computer from going to sleep
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED);
            }
            else
            {
                // allow shutdown
                if (VistaTools.AtLeastVista())
                    ShutdownBlockReasonDestroy(Handle);

                // allow the computer to go to sleep
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
            }
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