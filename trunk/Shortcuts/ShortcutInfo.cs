using System;
using System.IO;

namespace wyUpdate.Common
{
    public enum WindowStyle
    {
        ShowNormal = 1, //SW_SHOWNORMAL
        ShowMinimized = 2, //SW_SHOWMINIMIZED
        ShowMaximized = 3, //SW_SHOWMAXIMIZED
    }

    public class ShortcutInfo : ICloneable
    {
        private string m_Path;
        private string m_WorkingDirectory;
        private string m_Arguments;
        private string m_Description;
        private string m_IconPath;
        private int m_IconIndex;
        private WindowStyle m_WindowStyle = WindowStyle.ShowNormal;

        //used on the updating side of things
        private string m_RelativeOuputPath;

        #region Properties

        public string Path
        {
            get { return m_Path; }
            set { m_Path = value; }
        }

        public string WorkingDirectory
        {
            get { return m_WorkingDirectory; }
            set { m_WorkingDirectory = value; }
        }

        public string Arguments
        {
            get { return m_Arguments; }
            set { m_Arguments = value; }
        }

        public string Description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }

        public string IconPath
        {
            get { return m_IconPath; }
            set { m_IconPath = value; }
        }

        public int IconIndex
        {
            get { return m_IconIndex; }
            set { m_IconIndex = value; }
        }

        public WindowStyle WindowStyle
        {
            get { return m_WindowStyle; }
            set { m_WindowStyle = value; }
        }

        public string RelativeOuputPath
        {
            get { return m_RelativeOuputPath; }
            set { m_RelativeOuputPath = value; }
        }

        #endregion Properties

        public void SaveToStream(Stream fs, bool saveRelativePath)
        {
            fs.WriteByte(0x8D);

            if (!string.IsNullOrEmpty(m_Path))
                WriteFiles.WriteString(fs, 0x01, m_Path);

            if (!string.IsNullOrEmpty(m_WorkingDirectory))
                WriteFiles.WriteString(fs, 0x02, m_WorkingDirectory);

            if (!string.IsNullOrEmpty(m_Arguments))
                WriteFiles.WriteString(fs, 0x03, m_Arguments);

            if (!string.IsNullOrEmpty(m_Description))
                WriteFiles.WriteString(fs, 0x04, m_Description);

            if (!string.IsNullOrEmpty(m_IconPath))
                WriteFiles.WriteString(fs, 0x05, m_IconPath);

            WriteFiles.WriteInt(fs, 0x06, m_IconIndex);
            WriteFiles.WriteInt(fs, 0x07, (int)m_WindowStyle);

            if (saveRelativePath)
                WriteFiles.WriteString(fs, 0x08, m_RelativeOuputPath);

            fs.WriteByte(0x9A);
        }

        public static ShortcutInfo LoadFromStream(Stream fs)
        {
            ShortcutInfo tempInfo = new ShortcutInfo();

            byte bType = (byte)fs.ReadByte();
            while (!ReadFiles.ReachedEndByte(fs, bType, 0x9A))//if this byte is detected then bail
            {
                switch (bType)
                {
                    case 0x01:
                        tempInfo.m_Path = ReadFiles.ReadString(fs);
                        break;
                    case 0x02:
                        tempInfo.m_WorkingDirectory = ReadFiles.ReadString(fs);
                        break;
                    case 0x03:
                        tempInfo.m_Arguments = ReadFiles.ReadString(fs);
                        break;
                    case 0x04:
                        tempInfo.m_Description = ReadFiles.ReadString(fs);
                        break;
                    case 0x05:
                        tempInfo.m_IconPath = ReadFiles.ReadString(fs);
                        break;
                    case 0x06:
                        tempInfo.m_IconIndex = ReadFiles.ReadInt(fs);
                        break;
                    case 0x07:
                        tempInfo.m_WindowStyle = (WindowStyle)ReadFiles.ReadInt(fs);
                        break;
                    case 0x08:
                        tempInfo.m_RelativeOuputPath = ReadFiles.ReadString(fs);
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            return tempInfo;
        }

        #region ICloneable Members

        public object Clone()
        {
            return new ShortcutInfo
                                  {
                                      m_Path = m_Path,
                                      m_WorkingDirectory = m_WorkingDirectory,
                                      m_Arguments = m_Arguments,
                                      m_Description = m_Description,
                                      m_IconPath = m_IconPath,
                                      m_IconIndex = m_IconIndex,
                                      m_WindowStyle = m_WindowStyle,
                                      m_RelativeOuputPath = m_RelativeOuputPath
                                  };
        }

        #endregion
    }
}