using Microsoft.Win32;

namespace wyUpdate.Common
{
    public static class SystemFolders
    {
        static string m_CommonAppData;
        static string m_CommonDesktop;
        static string m_CommonProgramsStartMenu;
        static string m_CommonStartup;

        public static string CommonAppData
        {
            get
            {
                if (SystemFolders.m_CommonAppData == null)
                {
                    //read the value from registry
                    SystemFolders.m_CommonAppData = (string)Registry.GetValue(
                        @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\explorer\Shell Folders",
                        "Common AppData", null);
                }

                return SystemFolders.m_CommonAppData;
            }
        }

        public static string CommonDesktop
        {
            get
            {
                if (SystemFolders.m_CommonDesktop == null)
                {
                    //read the value from registry
                    SystemFolders.m_CommonDesktop = (string)Registry.GetValue(
                        @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\explorer\Shell Folders",
                        "Common Desktop", null);
                }

                return SystemFolders.m_CommonDesktop;
            }
        }

        public static string CommonProgramsStartMenu
        {
            get
            {
                if (SystemFolders.m_CommonProgramsStartMenu == null)
                {
                    //read the value from registry
                    SystemFolders.m_CommonProgramsStartMenu = (string)Registry.GetValue(
                        @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\explorer\Shell Folders",
                        "Common Programs", null);
                }

                return SystemFolders.m_CommonProgramsStartMenu;
            }
        }

        public static string CommonStartup
        {
            get
            {
                if (SystemFolders.m_CommonStartup == null)
                {
                    //read the value from registry
                    SystemFolders.m_CommonStartup = (string)Registry.GetValue(
                        @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\explorer\Shell Folders",
                        "Common Startup", null);
                }

                return SystemFolders.m_CommonStartup;
            }
        }
    }
}