using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public static class SystemMenu
{
    [DllImport("user32.dll", EntryPoint = "GetSystemMenu", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
    static extern IntPtr GetSystemMenu(IntPtr WindowHandle, int bReset);

    [DllImport("user32.dll", SetLastError = true)]
    static extern int EnableMenuItem(IntPtr menu, int ideEnableItem, int enable); 


    public static void DisableCloseButton(Form form)
    {
        //0xF060 = scClose

        //mfGrayed = 0x00000001
        //mfByCommand = 0x00000000
        IntPtr SystemMenu = GetSystemMenu(form.Handle, 0);
        EnableMenuItem(SystemMenu, 0xF060, 1);

        //NOTE: don't make this a critical failure if it doesn't work.
        /*
        int PreviousState = EnableMenuItem(SystemMenu, 0xF060, 1);
        if (PreviousState == -1)
            throw new Exception("The close menu does not exist");*/
    }

    public static void EnableCloseButton(Form form)
    {
        //0xF060 = scClose

        //mfString = 0x00000000
        //mfByCommand = 0x00000000
        IntPtr SystemMenu = GetSystemMenu(form.Handle, 0);
        EnableMenuItem(SystemMenu, 0xF060, 0);

        //NOTE: don't make this a critical failure if it doesn't work.
        /*
        int PreviousState = EnableMenuItem(SystemMenu, 0xF060, 0);
        if (PreviousState == -1)
            throw new Exception("The close menu does not exist");*/
    }
}