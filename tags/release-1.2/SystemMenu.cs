using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public static class SystemMenu
{
    // First, we need the GetSystemMenu() function.
    // This function does not have an Unicode counterpart
    [DllImport("USER32", EntryPoint = "GetSystemMenu", SetLastError = true,
               CharSet = CharSet.Unicode, ExactSpelling = true,
               CallingConvention = CallingConvention.Winapi)]
    private static extern IntPtr apiGetSystemMenu(IntPtr WindowHandle,
                                                  int bReset);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int EnableMenuItem(IntPtr menu, int ideEnableItem, int enable); 


    public static void DisableCloseButton(Form form)
    {
        //0xF060 = scClose

        //mfGrayed = 0x00000001
        //mfByCommand = 0x00000000
        IntPtr SystemMenu = apiGetSystemMenu(form.Handle, 0);
        int PreviousState = EnableMenuItem(SystemMenu, 0xF060, 0x00000001);
        if (PreviousState == -1)
            throw new Exception("The close menu does not exist");
    }

    public static void EnableCloseButton(Form form)
    {
        //0xF060 = scClose

        //mfString = 0x00000000
        //mfByCommand = 0x00000000
        IntPtr SystemMenu = apiGetSystemMenu(form.Handle, 0);
        int PreviousState = EnableMenuItem(SystemMenu, 0xF060, 0x00);
        if (PreviousState == -1)
            throw new Exception("The close menu does not exist");
    }
}