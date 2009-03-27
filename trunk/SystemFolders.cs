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
                if (m_CommonAppData == null)
                {
                    //read the value from registry
                    m_CommonAppData = (string)Registry.GetValue(
                        @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\explorer\Shell Folders",
                        "Common AppData", null);
                }

                return m_CommonAppData;
            }
        }

        public static string CommonDesktop
        {
            get
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
        }

        public static string CommonProgramsStartMenu
        {
            get
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
        }

        public static string CommonStartup
        {
            get
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
        }
    }
}