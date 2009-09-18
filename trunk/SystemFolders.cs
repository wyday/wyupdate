using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace wyUpdate.Common
{
    public static class SystemFolders
    {
        static string m_CommonAppData;
        static string m_CurrentAppData;
        static string m_CommonDesktop;
        static string m_CommonProgramsStartMenu;
        static string m_CommonStartup;

        static string m_System32x86;
        static string m_System32x64;
        static string m_RootDrive;

        public static string GetCommonAppData()
        {
            if (m_CommonAppData == null)
            {
                //read the value from registry
                m_CommonAppData = (string)Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\explorer\Shell Folders",
                    "Common AppData", null);
            }

            return m_CommonAppData;
        }

        public static string GetCurrentUserAppData()
        {
            if(m_CurrentAppData == null)
            {
                m_CurrentAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }

            return m_CurrentAppData;
        }

        public static string GetCommonDesktop()
        {
            if (m_CommonDesktop == null)
            {
                //read the value from registry
                m_CommonDesktop = (string)Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\explorer\Shell Folders",
                    "Common Desktop", null);
            }

            return m_CommonDesktop;
        }

        public static string GetCommonProgramsStartMenu()
        {
            if (m_CommonProgramsStartMenu == null)
            {
                //read the value from registry
                m_CommonProgramsStartMenu = (string)Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\explorer\Shell Folders",
                    "Common Programs", null);
            }

            return m_CommonProgramsStartMenu;
        }

        public static string GetCommonStartup()
        {
            if (m_CommonStartup == null)
            {
                //read the value from registry
                m_CommonStartup = (string)Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\explorer\Shell Folders",
                    "Common Startup", null);
            }

            return m_CommonStartup;
        }

        public static string GetRootDrive()
        {
            if(m_RootDrive == null)
            {
                m_RootDrive = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 3);
            }

            return m_RootDrive;
        }

        [DllImport("shell32.dll")]
        static extern Int32 SHGetFolderPath(IntPtr hwndOwner, Int32 nFolder, IntPtr hToken, UInt32 dwFlags, StringBuilder pszPath);

        public static string GetSystem32x86()
        {
            if(m_System32x86 == null)
            {
                if (Is64Bit())
                {
                    StringBuilder path = new StringBuilder(256);

                    //CSIDL_SYSTEMX86 = 0x29
                    SHGetFolderPath(IntPtr.Zero, 0x29, IntPtr.Zero, 0, path);

                    m_System32x86 = path.ToString();
                }
                else
                    m_System32x86 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            }

            return m_System32x86;
        }

        public static string GetSystem32x64()
        {
            // for x64 systems, return the system32 folder, otherwise throw an exception

            if (m_System32x64 == null)
            {
                if (Is64Bit())
                    m_System32x64 = Environment.GetFolderPath(Environment.SpecialFolder.System);

                else
                    return null;
            }

            return m_System32x64;
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool lpSystemInfo);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern UIntPtr GetProcAddress(IntPtr hModule, string procName);

        private static bool Is64Bit()
        {
            return IntPtr.Size == 8 || (IntPtr.Size == 4 && Is32BitProcessOn64BitProcessor());
        }

        static bool? is32on64;

        private static bool Is32BitProcessOn64BitProcessor()
        {
            if(is32on64 == null)
            {
                UIntPtr proc = GetProcAddress(GetModuleHandle("kernel32.dll"), "IsWow64Process");

                if (proc == UIntPtr.Zero)
                    is32on64 = false;
                else
                {
                    bool retVal;

                    IsWow64Process(Process.GetCurrentProcess().Handle, out retVal);

                    is32on64 = retVal;
                }
            }

            return is32on64.Value;
        }
    }
}