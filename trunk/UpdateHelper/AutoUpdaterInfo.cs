using System;
using System.IO;
using System.Windows.Forms;
using wyUpdate.Common;

namespace wyDay.Controls
{
    internal enum AutoUpdaterStatus
    {
        Nothing = 0,
        UpdateSucceeded = 1,
        UpdateFailed = 2
    }

    internal class AutoUpdaterInfo
    {
        public DateTime LastCheckedForUpdate { get; set; }
        public UpdateStepOn UpdateStepOn { get; set; }

        public AutoUpdaterStatus AutoUpdaterStatus { get; set; }

        public string UpdateVersion { get; set; }
        public string ChangesInLatestVersion { get; set; }
        public bool ChangesIsRTF { get; set; }


        public string ErrorTitle { get; set; }
        public string ErrorMessage { get; set; }
        

        string autoUpdateID;

        public AutoUpdaterInfo(string auID)
        {
            autoUpdateID = auID;
            AutoUpdaterStatus = AutoUpdaterStatus.Nothing;

            try
            {
                Load();
            }
            catch
            {
                LastCheckedForUpdate = DateTime.MinValue;
                UpdateStepOn = UpdateStepOn.Nothing;
            }
        }

        public string AutoUpdateID
        {
            get
            {
                return string.IsNullOrEmpty(autoUpdateID) 
                    ? Path.GetFileName(Application.ExecutablePath) 
                    : autoUpdateID;
            }
        }

        string GetFilename()
        {
            string filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                           "wyUpdate AU");

            if (!Directory.Exists(filename))
            {
                Directory.CreateDirectory(filename);

                File.SetAttributes(filename, FileAttributes.System | FileAttributes.Hidden);
            }

            filename = Path.Combine(filename, AutoUpdateID + ".autoupdate");

            return filename;
        }


        // not using registry because .NET 2.0 has shit support for x64/x86 access
        public void Save()
        {
            FileStream fs = new FileStream(GetFilename(), FileMode.Create, FileAccess.Write);

            // Write any file-identification data you want to here
            WriteFiles.WriteHeader(fs, "AUIF");

#if CLIENT
            UpdateStepOn = UpdateStepOn.Nothing;
#endif

            // Date last checked for update
            WriteFiles.WriteDateTime(fs, 0x01, LastCheckedForUpdate);

            // update step on
            WriteFiles.WriteInt(fs, 0x02, (int) UpdateStepOn);

#if CLIENT
            // only save the AutoUpdaterStatus when wyUpdate writes the file
            WriteFiles.WriteInt(fs, 0x03, (int)AutoUpdaterStatus);
#endif

            if (!string.IsNullOrEmpty(UpdateVersion))
                WriteFiles.WriteString(fs, 0x04, UpdateVersion);

            if (!string.IsNullOrEmpty(ChangesInLatestVersion))
            {
                WriteFiles.WriteString(fs, 0x05, ChangesInLatestVersion);

                WriteFiles.WriteBool(fs, 0x06, ChangesIsRTF);
            }
            

#if CLIENT
            if (!string.IsNullOrEmpty(ErrorTitle))
                WriteFiles.WriteString(fs, 0x07, ErrorTitle);

            if (!string.IsNullOrEmpty(ErrorMessage))
                WriteFiles.WriteString(fs, 0x08, ErrorMessage);
#endif

            fs.WriteByte(0xFF);
            fs.Close();
        }

        void Load()
        {
            FileStream fs = new FileStream(GetFilename(), FileMode.Open, FileAccess.Read);

            if (!ReadFiles.IsHeaderValid(fs, "AUIF"))
            {
                //free up the file so it can be deleted
                fs.Close();
                throw new Exception("Auto update state file ID is wrong.");
            }

            byte bType = (byte)fs.ReadByte();
            while (!ReadFiles.ReachedEndByte(fs, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x01: // Date last checked for update
                        LastCheckedForUpdate = ReadFiles.ReadDateTime(fs);
                        break;

                    case 0x02: // update step on
                        UpdateStepOn = (UpdateStepOn) ReadFiles.ReadInt(fs);
                        break;

                    case 0x03:
                        AutoUpdaterStatus = (AutoUpdaterStatus) ReadFiles.ReadInt(fs);
                        break;

                    case 0x04: // update succeeded
                        UpdateVersion = ReadFiles.ReadString(fs);
                        break;

                    case 0x05:
                        ChangesInLatestVersion = ReadFiles.ReadString(fs);
                        break;

                    case 0x06:
                        ChangesIsRTF = ReadFiles.ReadBool(fs);
                        break;

                    case 0x07: // update failed
                        ErrorTitle = ReadFiles.ReadString(fs);
                        break;

                    case 0x08:
                        ErrorMessage = ReadFiles.ReadString(fs);
                        break;

                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            fs.Close();
        }

        public void ClearSuccessError()
        {
            AutoUpdaterStatus = AutoUpdaterStatus.Nothing;

            UpdateVersion = null;
            ChangesInLatestVersion = null;
            ChangesIsRTF = false;

            ErrorTitle = null;
            ErrorMessage = null;
        }
    }
}
