using System;
using System.Collections.Generic;
using System.IO;
using wyDay.Controls;
using wyUpdate.Common;

namespace wyUpdate
{
    public partial class frmMain
    {
        // Automatic Update Mode (aka API mode)
        UpdateHelper updateHelper;
        bool isAutoUpdateMode;
        string autoUpdateStateFile;


        void SetupAutoupdateMode()
        {
            isAutoUpdateMode = true;

            updateHelper = new UpdateHelper(this);
            updateHelper.SenderProcessClosed += UpdateHelper_SenderProcessClosed;
            updateHelper.RequestReceived += UpdateHelper_RequestReceived;
        }

        void UpdateHelper_RequestReceived(object sender, Action a, UpdateStep s)
        {
            if (a == Action.Cancel)
            {
                CancelUpdate(true);
                return;
            }

            switch (s)
            {
                case UpdateStep.CheckForUpdate:

                    if (!string.IsNullOrEmpty(serverOverwrite))
                    {
                        // overrite server file
                        List<string> overwriteServer = new List<string> { serverOverwrite };
                        BeginDownload(overwriteServer, 0, false);
                    }
                    else
                    {
                        //download the server file
                        BeginDownload(update.ServerFileSites, 0, false);
                    }

                    break;
                case UpdateStep.DownloadUpdate:

                    ShowFrame(Frame.InstallUpdates);

                    break;
                case UpdateStep.BeginExtraction:

                    update.CurrentlyUpdating = UpdateOn.Extracting;
                    InstallUpdates(update.CurrentlyUpdating);

                    break;
                case UpdateStep.RestartInfo:

                    // send a success signal.
                    updateHelper.SendSuccess();

                    break;
                case UpdateStep.Install:

                    TopMost = true;

                    update.CurrentlyUpdating = UpdateOn.ClosingProcesses;
                    InstallUpdates(update.CurrentlyUpdating);

                    TopMost = false;

                    break;
            }
        }

        void UpdateHelper_SenderProcessClosed(object sender, EventArgs e)
        {
            // close wyUpdate if we're not installing an update
            if (isAutoUpdateMode && !updateHelper.Installing)
                CancelUpdate(true);
        }



        private void SaveAutoUpdateData(UpdateStepOn updateStepOn)
        {
            FileStream fs = new FileStream(autoUpdateStateFile, FileMode.Create, FileAccess.Write);

            // Write any file-identification data you want to here
            WriteFiles.WriteHeader(fs, "IUAUFV1");

            // Step on {Checked = 2, Downloaded = 4, Extracted = 6}
            WriteFiles.WriteInt(fs, 0x01, (int)updateStepOn);

            // DateTime when the last step was taken.
            WriteFiles.WriteLong(fs, 0x02, DateTime.Now.ToBinary());

            // file to execute
            if (updateHelper.FileToExecuteAfterUpdate != null)
                WriteFiles.WriteString(fs, 0x03, updateHelper.FileToExecuteAfterUpdate);

            if (updateHelper.AutoUpdateID != null)
                WriteFiles.WriteString(fs, 0x04, updateHelper.AutoUpdateID);

            // Server data file location
            if (!string.IsNullOrEmpty(serverFileLoc))
                WriteFiles.WriteString(fs, 0x05, serverFileLoc);

            // Client's server file location (self update server file)
            if (!string.IsNullOrEmpty(clientSFLoc))
                WriteFiles.WriteString(fs, 0x06, clientSFLoc);

            // temp directory
            if (!string.IsNullOrEmpty(tempDirectory))
                WriteFiles.WriteString(fs, 0x07, tempDirectory);

            fs.WriteByte(0xFF);
            fs.Close();
        }

        // Note: the server file (client or regular) might not exist. If they don't, redownload them.
        private void LoadAutoUpdateData()
        {
            FileStream fs = new FileStream(autoUpdateStateFile, FileMode.Open, FileAccess.Read);

            if (!ReadFiles.IsHeaderValid(fs, "IUAUFV1"))
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
                    case 0x01:


                        break;
                    case 0x02:

                        break;
                    case 0x03: // file to execute
                        updateHelper.FileToExecuteAfterUpdate = ReadFiles.ReadString(fs);
                        break;

                    case 0x04: // autoupdate ID
                        updateHelper.AutoUpdateID = ReadFiles.ReadString(fs);
                        break;

                    case 0x05: // Server data file location
                        serverFileLoc = ReadFiles.ReadString(fs);
                        break;

                    case 0x06: // Client's server file location (self update server file)
                        clientSFLoc = ReadFiles.ReadString(fs);
                        break;

                    case 0x07: // Temp directory
                        tempDirectory = ReadFiles.ReadString(fs);
                        break;

                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            fs.Close();
        }

    }
}