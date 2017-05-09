using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class DPIAwareForm : Form
{
    #region Native Calls

    // Get handle to monitor that has the largest intersection with a specified window.
    [DllImport("User32.dll", SetLastError = true)]
    internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

    // Get handle to monitor that has the largest intersection with a specified rectangle.
    [DllImport("User32.dll", SetLastError = true)]
    internal static extern IntPtr MonitorFromRect([In] ref RECT lprc, int dwFlags);

    // Get handle to monitor that contains a specified point.
    [DllImport("User32.dll", SetLastError = true)]
    internal static extern IntPtr MonitorFromPoint(POINT pt, int dwFlags);

    internal const int MONITORINFOF_PRIMARY = 0x00000001;
    internal const int MONITOR_DEFAULTTONEAREST = 0x00000002;
    internal const int MONITOR_DEFAULTTONULL = 0x00000000;
    internal const int MONITOR_DEFAULTTOPRIMARY = 0x00000001;

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        internal int x;
        internal int y;

        internal POINT(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        internal int left;
        internal int top;
        internal int right;
        internal int bottom;
    }


    /// <summary>Get DPI from handle to a specified monitor (Windows 8.1 or newer is required).</summary>
    /// <param name="hmonitor"></param>
    /// <param name="dpiType"></param>
    /// <param name="dpiX"></param>
    /// <param name="dpiY"></param>
    /// <returns></returns>
    [DllImport("Shcore.dll", SetLastError = true)]
    internal static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

    internal enum Monitor_DPI_Type : int
    {
        MDT_Effective_DPI = 0,
        MDT_Angular_DPI = 1,
        MDT_Raw_DPI = 2,
        MDT_Default = MDT_Effective_DPI
    }

    // Equivalent to LOWORD macro
    internal static short GetLoWord(int dword)
    {
        return (short)(dword & 0xffff);
    }

    /// <summary>Get device-specific information.</summary>
    /// <param name="hdc"></param>
    /// <param name="nIndex"></param>
    /// <returns></returns>
    [DllImport("Gdi32.dll", SetLastError = true)]
    internal static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    internal const int LOGPIXELSX = 88;

    [DllImport("User32.dll", SetLastError = true)]
    internal static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("User32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

    #endregion

    /// <summary>List of controls to adjust the fonts of</summary>
    public List<List<Control>> ControlGroupsToAdjust = new List<List<Control>>();

    bool isWin81OrNewer = false;

    #region DPI

    // Get DPI of monitor containing this window by GetDpiForMonitor.
    private float GetDpiWindowMonitor()
    {
        // Get handle to this window.
        IntPtr handleWindow = Process.GetCurrentProcess().MainWindowHandle;

        // Get handle to monitor.
        IntPtr handleMonitor = MonitorFromWindow(handleWindow, MONITOR_DEFAULTTOPRIMARY);

        // Get DPI.
        return GetDpiSpecifiedMonitor(handleMonitor);
    }

    // Get DPI of a specified monitor by GetDpiForMonitor.
    private float GetDpiSpecifiedMonitor(IntPtr handleMonitor)
    {
        // Check if GetDpiForMonitor function is available.
        if (!isWin81OrNewer)
            return this.CurrentAutoScaleDimensions.Width;

        // Get DPI.
        uint dpiX = 0;
        uint dpiY = 0;

        int result = GetDpiForMonitor(handleMonitor, Monitor_DPI_Type.MDT_Default, out dpiX, out dpiY);

        if (result != 0) // If not S_OK (= 0)
        {
            throw new Exception("Failed to get DPI of monitor containing this window.");
        }

        return (float)dpiX;
    }

    // Get DPI for all monitors by GetDeviceCaps.
    private float GetDpiDeviceMonitor()
    {
        int dpiX = 0;
        IntPtr screen = IntPtr.Zero;

        try
        {
            screen = GetDC(IntPtr.Zero);
            dpiX = GetDeviceCaps(screen, LOGPIXELSX);
        }
        finally
        {
            if (screen != IntPtr.Zero)
            {
                ReleaseDC(IntPtr.Zero, screen);
            }
        }

        return (float)dpiX;
    }

    #endregion

    // DPI at design time
    private const float dpiAtDesign = 96F;

    // Old (previous) DPI
    private float dpiOld = 0;

    // New (current) DPI
    private float dpiNew = 0;


    public DPIAwareForm(): base()
    {
        // Load whether we're in Windows 8.1 or newer
        // To get this value correctly, it is required to include ID of Windows 8.1 in the manifest file.
        OperatingSystem os = Environment.OSVersion;
        isWin81OrNewer = os.Version.Major > 6 || os.Version.Major == 6 && os.Version.Minor > 2;

        base.AutoScaleMode = AutoScaleMode.Dpi;
    }

    protected override void OnLoad(EventArgs e)
    {
        AdjustWindowInitial();

        base.OnLoad(e);
    }

    /// <summary>0x02E0 from WinUser.h</summary>
    const int WM_DPICHANGED = 0x02e0;

    // Catch window message of DPI change.
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        // Check if Windows 8.1 or newer and if not, ignore message.
        if (!isWin81OrNewer)
            return;

        if (m.Msg == WM_DPICHANGED)
        {
            // wParam
            short lo = GetLoWord(m.WParam.ToInt32());

            // lParam
            RECT r = (RECT)Marshal.PtrToStructure(m.LParam, typeof(RECT));

            // Hold new DPI as target for adjustment.
            dpiNew = lo;

            if (dpiOld != lo)
            {
                MoveWindow();
                AdjustWindow();
            }
        }
    }


    // Get new location of this window after DPI change.
    private void MoveWindow()
    {
        if (dpiOld == 0) return; // Abort.

        float factor = dpiNew / dpiOld;

        // Prepare new rectangles shrinked or expanded sticking four corners.
        int widthDiff = (int)(this.ClientSize.Width * factor) - this.ClientSize.Width;
        int heightDiff = (int)(this.ClientSize.Height * factor) - this.ClientSize.Height;

        List<RECT> rectList = new List<RECT>();

        // Left-Top corner
        rectList.Add(new RECT
        {
            left = this.Bounds.Left,
            top = this.Bounds.Top,
            right = this.Bounds.Right + widthDiff,
            bottom = this.Bounds.Bottom + heightDiff
        });

        // Right-Top corner
        rectList.Add(new RECT
        {
            left = this.Bounds.Left - widthDiff,
            top = this.Bounds.Top,
            right = this.Bounds.Right,
            bottom = this.Bounds.Bottom + heightDiff
        });

        // Left-Bottom corner
        rectList.Add(new RECT
        {
            left = this.Bounds.Left,
            top = this.Bounds.Top - heightDiff,
            right = this.Bounds.Right + widthDiff,
            bottom = this.Bounds.Bottom
        });

        // Right-Bottom corner
        rectList.Add(new RECT
        {
            left = this.Bounds.Left - widthDiff,
            top = this.Bounds.Top - heightDiff,
            right = this.Bounds.Right,
            bottom = this.Bounds.Bottom
        });

        // Get handle to monitor that has the largest intersection with each rectangle.
        for (int i = 0; i <= rectList.Count - 1; i++)
        {
            RECT rectBuf = rectList[i];

            IntPtr handleMonitor = MonitorFromRect(ref rectBuf, MONITOR_DEFAULTTONULL);

            if (handleMonitor != IntPtr.Zero)
            {
                // Check if at least Left-Top corner or Right-Top corner is inside monitors.
                IntPtr handleLeftTop = MonitorFromPoint(new POINT(rectBuf.left, rectBuf.top), MONITOR_DEFAULTTONULL);
                IntPtr handleRightTop = MonitorFromPoint(new POINT(rectBuf.right, rectBuf.top), MONITOR_DEFAULTTONULL);

                if ((handleLeftTop != IntPtr.Zero) || (handleRightTop != IntPtr.Zero))
                {
                    // Check if DPI of the monitor matches.
                    if (GetDpiSpecifiedMonitor(handleMonitor) == dpiNew)
                    {
                        // Move this window.
                        this.Location = new Point(rectBuf.left, rectBuf.top);
                        break;
                    }
                }
            }
        }
    }


    /// <summary>Adjust this window's size and fonts.</summary>
    private void AdjustWindow()
    {
        if ((dpiOld == 0) || (dpiOld == dpiNew)) return; // Abort.

        float factor = dpiNew / dpiOld;

        dpiOld = dpiNew;

        // Adjust location and size of Controls (except location of this window itself).
        this.Scale(new SizeF(factor, factor));

        // Adjust Font size of Controls.
        this.Font = new Font(this.Font.FontFamily,
                             this.Font.Size * factor,
                             this.Font.Style);

        foreach (List<Control> controls in ControlGroupsToAdjust)
        {
            // The font will be identical for all the controls in the control group,
            // so save memory by just creating it once.
            Font f = new Font(controls[0].Font.FontFamily, controls[0].Font.Size * factor, controls[0].Font.Style);

            // Now actually set the font for the controls.
            foreach (Control c in controls)
                c.Font = f;
        }
    }

    /// <summary>Adjust location, size and font size of Controls according to new DPI.</summary>
    private void AdjustWindowInitial()
    {
        // Hold initial DPI used at loading this window.
        dpiOld = this.CurrentAutoScaleDimensions.Width;

        // Check current DPI.
        dpiNew = GetDpiWindowMonitor();

        AdjustWindow();
    }
}
