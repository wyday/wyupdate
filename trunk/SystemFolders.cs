using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace wyUpdate.Common
{
    public static class SystemFolders
    {
        [DllImport("shell32.dll")]
        static extern Int32 SHGetFolderPath(IntPtr hwndOwner, Int32 nFolder, IntPtr hToken, UInt32 dwFlags, StringBuilder pszPath);

        static string m_CommonAppData;
        static string m_CurrentAppData;
        static string m_CurrentLocalAppData;
        static string m_CommonDesktop;
        static string m_CurrentDesktop;
        static string m_CommonDocuments;
        static string m_CommonProgramsStartMenu;
        static string m_CurrentProgramsStartMenu;
        static string m_CommonStartup;

        static string m_System32x86;
        static string m_System32x64;
        static string m_RootDrive;

        //static string m_FontsFolder;

        static string m_CommonProgramFilesx86;
        static string m_CommonProgramFilesx64;

        static string m_UserProfile;

        public static string GetCommonAppData()
        {
            if (m_CommonAppData == null)
            {
                StringBuilder path = new StringBuilder(256);

                //CSIDL_COMMON_APPDATA = 0x0023
                SHGetFolderPath(IntPtr.Zero, 0x23, IntPtr.Zero, 0, path);

                m_CommonAppData = path.ToString();
            }

            return m_CommonAppData;
        }

        public static string GetCurrentUserAppData()
        {
            return m_CurrentAppData ??
                   (m_CurrentAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        }

        public static string GetCurrentUserLocalAppData()
        {
            return m_CurrentLocalAppData ??
                   (m_CurrentLocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        }

        public static string GetCommonDesktop()
        {
            if (m_CommonDesktop == null)
            {
                StringBuilder path = new StringBuilder(256);

                //CSIDL_COMMON_DESKTOPDIRECTORY = 0x0019
                SHGetFolderPath(IntPtr.Zero, 0x19, IntPtr.Zero, 0, path);

                m_CommonDesktop = path.ToString();
            }

            return m_CommonDesktop;
        }

        public static string GetCurrentUserDesktop()
        {
            // Current user's desktop: CSIDL_DESKTOPDIRECTORY, 0x0010
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
                StringBuilder path = new StringBuilder(256);

                //CSIDL_COMMON_PROGRAMS = 0x0017
                SHGetFolderPath(IntPtr.Zero, 0x17, IntPtr.Zero, 0, path);

                m_CommonProgramsStartMenu = path.ToString();
            }

            return m_CommonProgramsStartMenu;
        }

        public static string GetCurrentUserProgramsStartMenu()
        {
            // Current user's start menu: CSIDL_STARTMENU, 0xB
            return m_CurrentProgramsStartMenu ??
                   (m_CurrentProgramsStartMenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs));
        }

        /*
        public static string GetFontsFolder()
        {
            if (m_FontsFolder == null)
            {
                StringBuilder path = new StringBuilder(256);

                //CSIDL_FONTS = 0x14
                SHGetFolderPath(IntPtr.Zero, 0x14, IntPtr.Zero, 0, path);

                m_FontsFolder = path.ToString();
            }

            return m_FontsFolder;
        }*/

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
                StringBuilder path = new StringBuilder(256);

                //CSIDL_COMMON_STARTUP = 0x0018
                SHGetFolderPath(IntPtr.Zero, 0x18, IntPtr.Zero, 0, path);

                m_CommonStartup = path.ToString();
            }

            return m_CommonStartup;
        }

        public static string GetRootDrive()
        {
            return m_RootDrive ??
                   (m_RootDrive = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 3));
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

                // fucking Windows XP screws this up when CSIDL_PROFILE
                // is used in Windows Services. Hell if I know why. For some
                // reason the "userprofile" environment variable is populated.
                // See: http://wyday.com/forum/viewtopic.php?f=1&t=3524
                if (string.IsNullOrEmpty(m_UserProfile))
                    m_UserProfile = Environment.GetEnvironmentVariable("userprofile");
            }

            return m_UserProfile;
        }

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

        public const int FILE_ATTRIBUTE_DIRECTORY = 0x10;
        public const int FILE_ATTRIBUTE_FILE = 0;
        public const int MAX_PATH = 260;

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
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
                dir, FILE_ATTRIBUTE_DIRECTORY,
                file, FILE_ATTRIBUTE_FILE
            );

            // return if file is in the directory (or a subfolder)
            return bRet && strBuild.Length >= 2 && strBuild.ToString().Substring(0, 2) == @".\";
        }

        public static bool IsDirInDir(string dir, string checkDir)
        {
            StringBuilder strBuild = new StringBuilder(MAX_PATH);

            bool bRet = PathRelativePathTo(
                strBuild,
                dir, FILE_ATTRIBUTE_DIRECTORY,
                checkDir, FILE_ATTRIBUTE_DIRECTORY
            );

            // if strBuild.Length == 1, result is "."
            return bRet && (strBuild.Length == 1 || (strBuild.Length >= 2 && strBuild.ToString().Substring(0, 2) == @".\"));
        }
    }
}