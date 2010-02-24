using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace wyUpdate
{
    public class VistaTools
    {
        public static bool AtLeastVista()
        {
            return (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 6);
        }

        // check if user is an admin for Windows 2000 and above
        [DllImport("shell32.dll", EntryPoint = "#680", CharSet = CharSet.Unicode)]
        public static extern bool IsUserAnAdmin();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(HandleRef hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        public static void SetButtonShield(Button btn, bool showShield)
        {
            //Note: make sure the button FlatStyle = FlatStyle.System
            // BCM_SETSHIELD = 0x0000160C
            SendMessage(new HandleRef(btn, btn.Handle), 0x160C, IntPtr.Zero, showShield ? new IntPtr(1) : IntPtr.Zero);
        }
    }
}

