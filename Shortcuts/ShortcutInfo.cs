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
        public string Path;
        public string WorkingDirectory;
        public string Arguments;
        public string Description;
        public string IconPath;
        public int IconIndex;
        public WindowStyle WindowStyle = WindowStyle.ShowNormal;
        public string RelativeOuputPath;

        public void SaveToStream(Stream fs, bool saveRelativePath)
        {
            fs.WriteByte(0x8D);

            if (!string.IsNullOrEmpty(Path))
                WriteFiles.WriteDeprecatedString(fs, 0x01, Path);

            if (!string.IsNullOrEmpty(WorkingDirectory))
                WriteFiles.WriteDeprecatedString(fs, 0x02, WorkingDirectory);

            if (!string.IsNullOrEmpty(Arguments))
                WriteFiles.WriteDeprecatedString(fs, 0x03, Arguments);

            if (!string.IsNullOrEmpty(Description))
                WriteFiles.WriteDeprecatedString(fs, 0x04, Description);

            if (!string.IsNullOrEmpty(IconPath))
                WriteFiles.WriteDeprecatedString(fs, 0x05, IconPath);

            WriteFiles.WriteInt(fs, 0x06, IconIndex);
            WriteFiles.WriteInt(fs, 0x07, (int)WindowStyle);

            if (saveRelativePath)
                WriteFiles.WriteDeprecatedString(fs, 0x08, RelativeOuputPath);

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
                        tempInfo.Path = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x02:
                        tempInfo.WorkingDirectory = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x03:
                        tempInfo.Arguments = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x04:
                        tempInfo.Description = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x05:
                        tempInfo.IconPath = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x06:
                        tempInfo.IconIndex = ReadFiles.ReadInt(fs);
                        break;
                    case 0x07:
                        tempInfo.WindowStyle = (WindowStyle)ReadFiles.ReadInt(fs);
                        break;
                    case 0x08:
                        tempInfo.RelativeOuputPath = ReadFiles.ReadDeprecatedString(fs);
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
                           Path = Path,
                           WorkingDirectory = WorkingDirectory,
                           Arguments = Arguments,
                           Description = Description,
                           IconPath = IconPath,
                           IconIndex = IconIndex,
                           WindowStyle = WindowStyle,
                           RelativeOuputPath = RelativeOuputPath
                       };
        }

        #endregion
    }
}