using System;
using System.IO;
using System.Windows.Forms;
using wyUpdate.Common;

namespace wyDay.Controls
{
    internal class AutoUpdaterInfo
    {
        public DateTime LastCheckedForUpdate { get; set; }
        public UpdateStepOn UpdateStepOn { get; set; }

        public bool UpdateSucceeded { get; set; }
        public string UpdateVersion { get; set; }
        public string ChangesInLatestVersion { get; set; }
        public bool ChangesIsRTF { get; set; }

        public bool UpdateFailed { get; set; }
        public string ErrorTitle { get; set; }
        public string ErrorMessage { get; set; }
        

        string autoUpdateID;

        public AutoUpdaterInfo(string auID)
        {
            autoUpdateID = auID;

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

        private string GetFilename()
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
            WriteFiles.WriteInt(fs, 0x02, (int)UpdateStepOn);

#if CLIENT
            if (UpdateSucceeded)
            {
                if (!string.IsNullOrEmpty(UpdateVersion))
                    WriteFiles.WriteString(fs, 0x03, UpdateVersion);

                if (!string.IsNullOrEmpty(ChangesInLatestVersion))
                    WriteFiles.WriteString(fs, 0x04, ChangesInLatestVersion);

                WriteFiles.WriteBool(fs, 0x05, ChangesIsRTF);
            }

            if (UpdateFailed)
            {
                if (!string.IsNullOrEmpty(ErrorTitle))
                    WriteFiles.WriteString(fs, 0x06, ErrorTitle);

                if (!string.IsNullOrEmpty(ErrorMessage))
                    WriteFiles.WriteString(fs, 0x07, ErrorMessage);
            }
#endif

            fs.WriteByte(0xFF);
            fs.Close();
        }

        private void Load()
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

                    case 0x03: // update succeeded
                        UpdateVersion = ReadFiles.ReadString(fs);
                        UpdateSucceeded = true;
                        break;

                    case 0x04:
                        ChangesInLatestVersion = ReadFiles.ReadString(fs);
                        UpdateSucceeded = true;
                        break;

                    case 0x05:
                        ChangesIsRTF = ReadFiles.ReadBool(fs);
                        UpdateSucceeded = true;
                        break;

                    case 0x06: // update failed
                        ErrorTitle = ReadFiles.ReadString(fs);
                        UpdateFailed = true;
                        break;

                    case 0x07:
                        ErrorMessage = ReadFiles.ReadString(fs);
                        UpdateFailed = true;
                        break;

                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            fs.Close();

            // reset the value so we don't keep seeing the same damn message
            if (UpdateSucceeded || UpdateFailed)
                Save();
        }
    }
}
