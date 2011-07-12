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
        static string m_CurrentDesktop;
        static string m_CommonDocuments;
        static string m_CommonProgramsStartMenu;
        static string m_CurrentProgramsStartMenu;
        static string m_CommonStartup;

        static string m_System32x86;
        static string m_System32x64;
        static string m_RootDrive;

        static string m_CommonProgramFilesx86;
        static string m_CommonProgramFilesx64;

        static string m_UserProfile;

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
            return m_CurrentAppData ??
                   (m_CurrentAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
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

        public static string GetCurrentUserDesktop()
        {
            return m_CurrentDesktop ??
                   (m_CurrentDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        }

        public static string GetCommonDocuments()
        {
            if (m_CommonDocuments == null)
            {
                StringBuilder path = new StringBuilder(256);

                //CSIDL_COMMON_DOCUMENTS = 0x002e
                SHGetFolderPath(IntPtr.Zero, 0x2e, IntPtr.Zero, 0, path);

                m_CommonDocuments = path.ToString();
            }

            return m_CommonDocuments;
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

        public static string GetCurrentUserProgramsStartMenu()
        {
            return m_CurrentProgramsStartMenu ??
                   (m_CurrentProgramsStartMenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs));
        }

        public static string GetCommonProgramFilesx86()
        {
            if (m_CommonProgramFilesx86 == null)
            {
                StringBuilder path = new StringBuilder(256);

                // on x64: %ProgramFiles(x86)%\Common Files
                // on x86: %ProgramFiles%\Common Files

                //CSIDL_PROGRAM_FILES_COMMONX86  = 0x002c (Doesn't work on Windows XP x86)
                //CSIDL_PROGRAM_FILES_COMMON = 0x02b
                SHGetFolderPath(IntPtr.Zero, Is64Bit() ? 0x2c : 0x2b, IntPtr.Zero, 0, path);

                m_CommonProgramFilesx86 = path.ToString();
            }

            return m_CommonProgramFilesx86;
        }

        public static string GetCommonProgramFilesx64()
        {
            if (m_CommonProgramFilesx64 == null)
            {
                if (Is64Bit())
                {
                    StringBuilder path = new StringBuilder(256);

                    //CSIDL_PROGRAM_FILES_COMMON = 0x002b
                    SHGetFolderPath(IntPtr.Zero, 0x2b, IntPtr.Zero, 0, path);

                    m_CommonProgramFilesx64 = path.ToString();
                }

                else
                    return null;
            }

            return m_CommonProgramFilesx64;
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
            if (m_RootDrive == null)
            {
                m_RootDrive = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 3);
            }

            return m_RootDrive;
        }

        public static string GetUserProfile()
        {
            if (m_UserProfile == null)
            {
                StringBuilder path = new StringBuilder(256);

                // %userprofile%
                //CSIDL_PROFILE  = 0x0028
                SHGetFolderPath(IntPtr.Zero, 0x28, IntPtr.Zero, 0, path);

                m_UserProfile = path.ToString();
            }

            return m_UserProfile;
        }

        [DllImport("shell32.dll")]
        static extern Int32 SHGetFolderPath(IntPtr hwndOwner, Int32 nFolder, IntPtr hToken, UInt32 dwFlags, StringBuilder pszPath);

        public static string GetSystem32x86()
        {
            if (m_System32x86 == null)
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

        public static bool Is64Bit()
        {
            return IntPtr.Size == 8 || (IntPtr.Size == 4 && Is32BitProcessOn64BitProcessor());
        }

        static bool? is32on64;

        static bool Is32BitProcessOn64BitProcessor()
        {
            if (is32on64 == null)
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


        public enum PathAttribute { File = 0, Directory = 0x10 }
        public const Int32 MAX_PATH = 260;

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        public static extern bool PathRelativePathTo(
             [Out] StringBuilder pszPath,
             [In] string pszFrom,
             [In] uint dwAttrFrom,
             [In] string pszTo,
             [In] uint dwAttrTo
        );

        public static bool IsFileInDirectory(string dir, string file)
        {
            StringBuilder strBuild = new StringBuilder(MAX_PATH);

            bool bRet = PathRelativePathTo(
                strBuild,
                dir, (uint)PathAttribute.Directory,
                file, (uint)PathAttribute.File
            );

            if (bRet && strBuild.Length >= 2)
            {
                //get the first two characters
                if (strBuild.ToString().Substring(0, 2) == @".\")
                {
                    //if file is in the directory (or a subfolder)
                    return true;
                }
            }

            return false;
        }

        public static bool IsDirInDir(string dir, string checkDir)
        {
            StringBuilder strBuild = new StringBuilder(MAX_PATH);

            bool bRet = PathRelativePathTo(
                strBuild,
                dir, (uint)PathAttribute.Directory,
                checkDir, (uint)PathAttribute.Directory
            );

            if (bRet)
            {
                if (strBuild.Length == 1) //result is "."
                    return true;

                if (strBuild.Length >= 2
                    //get the first two characters
                    && strBuild.ToString().Substring(0, 2) == @".\")
                {
                    //if checkDir is the directory (or a subfolder)
                    return true;
                }
            }

            return false;
        }
    }
}