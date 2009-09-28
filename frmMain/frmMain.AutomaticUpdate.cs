using System;
using System.Diagnostics;
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

        UpdateStep autoUpdateStepProcessing;


        bool currentlyExtracting;

        // is this instance of wyUpdate the New Self
        public bool IsNewSelf;

        bool beginAutoUpdateInstallation;


        void SetupAutoupdateMode()
        {
            isAutoUpdateMode = true;

            updateHelper = new UpdateHelper(this);
            updateHelper.SenderProcessClosed += UpdateHelper_SenderProcessClosed;
            updateHelper.RequestReceived += UpdateHelper_RequestReceived;
        }

        void StartNewSelfAndClose()
        {
            Process clientProcess = new Process
                                        {
                                            StartInfo =
                                                {
                                                    FileName = newSelfLocation,

                                                    // start the client in automatic update mode (a.k.a. wait mode)
                                                    Arguments =
                                                        "-cdata:\"" + clientFileLoc + "\" -basedir:\"" + baseDirectory +
                                                        "\" /autoupdate /ns",

                                                    WindowStyle = ProcessWindowStyle.Hidden
                                                }
                                        };

            clientProcess.Start();

            clientProcess.WaitForInputIdle();

            // tell all the clients that there's a new wyUpdate
            updateHelper.SendNewWyUpdate(UpdateHelperData.PipenameFromFilename(newSelfLocation), clientProcess.Id);

            CancelUpdate(true);
        }

        void UpdateHelper_RequestReceived(object sender, Action a, UpdateStep s)
        {
            if (a == Action.Cancel)
            {
                CancelUpdate(true);
                return;
            }

            // filter out out-of-order requests (never assume the step 's' is coming in the correct order)
            if (FilterBadRequest(s))
                return;

            autoUpdateStepProcessing = s;

            switch (s)
            {
                case UpdateStep.CheckForUpdate:

                    CheckForUpdate();

                    break;
                case UpdateStep.DownloadUpdate:

                    ShowFrame(Frame.InstallUpdates);
                    DownloadUpdate();

                    break;
                case UpdateStep.BeginExtraction:

                    update.CurrentlyUpdating = UpdateOn.Extracting;
                    InstallUpdates(update.CurrentlyUpdating);

                    break;
                case UpdateStep.RestartInfo:

                    // send a success signal.
                    updateHelper.SendSuccess(autoUpdateStepProcessing);

                    break;
                case UpdateStep.Install:

                    // show self & make topmost
                    Visible = true;
                    TopMost = true;
                    TopMost = false;

                    if (needElevation)
                    {
                        // save the RestartInfo details (file to launch, where to save the update success details)
                        SaveAutoUpdateData(wyDay.Controls.UpdateStepOn.UpdateReadyToInstall);

                        StartSelfElevated();
                        return;
                    }

                    if (SelfUpdateState == SelfUpdateState.Extracted)
                    {
                        // install the self update
                        update.CurrentlyUpdating = UpdateOn.InstallSelfUpdate;
                        InstallUpdates(update.CurrentlyUpdating);
                    }
                    else
                    {
                        // install the regular update
                        update.CurrentlyUpdating = UpdateOn.ClosingProcesses;
                        InstallUpdates(update.CurrentlyUpdating);
                    }

                    break;
            }
        }

        void UpdateHelper_SenderProcessClosed(object sender, EventArgs e)
        {
            // close wyUpdate if we're not installing an update
            if (isAutoUpdateMode && !updateHelper.Installing)
                CancelUpdate(true);
        }

        /// <summary>
        /// Filters bad request by responding with the required info.
        /// </summary>
        /// <param name="s">The requested step.</param>
        /// <returns>True if a bad request has been filtered, false otherwise</returns>
        bool FilterBadRequest(UpdateStep s)
        {
            // if the selfupdate has been downloaded but not extracted
            // it means wyUpdate was closed before extraction could take place
            if(SelfUpdateState == SelfUpdateState.Downloaded
                && update.CurrentlyUpdating != UpdateOn.ExtractSelfUpdate
                && s != UpdateStep.CheckForUpdate)
            {
                // report we're downloading
                updateHelper.SendProgress(0, UpdateStep.DownloadUpdate);

                // begin extracting self
                update.CurrentlyUpdating = UpdateOn.ExtractSelfUpdate;
                InstallUpdates(update.CurrentlyUpdating);

                return true;
            }

            switch (s)
            {
                case UpdateStep.CheckForUpdate:

                    // if already checking ...
                    if (frameOn == Frame.Checking && downloader != null)
                    {
                        // report progress of 0%
                        updateHelper.SendProgress(0, UpdateStep.CheckForUpdate);
                        return true;
                    }

                    // if on another step ...
                    if (frameOn != Frame.Checking)
                    {
                        // report UpdateAvailable, with changes
                        updateHelper.SendSuccess(ServerFile.NewVersion, panelDisplaying.GetChangesRTF(), true, null);

                        return true;
                    }

                    break;

                case UpdateStep.DownloadUpdate:

                    if(frameOn == Frame.Checking)
                    {
                        // waiting to be told to check for updates...
                        if(downloader == null)
                        {
                            // report 0% and begin checking
                            updateHelper.SendProgress(0, UpdateStep.CheckForUpdate);
                            CheckForUpdate();
                        }
                        else // already checking ...
                        {
                            // report 0% progress
                            updateHelper.SendProgress(0, UpdateStep.CheckForUpdate);
                        }

                        return true;
                    }
                    
                    if(frameOn == Frame.InstallUpdates)
                    {
                        // if already downloading ...
                        if(update.CurrentlyUpdating == UpdateOn.DownloadingUpdate)
                        {
                            // report 0%
                            updateHelper.SendProgress(0, UpdateStep.DownloadUpdate);
                        }
                        else // on another step (extracting, etc.) ...
                        {
                            // report UpdateDownloaded
                            updateHelper.SendSuccess(UpdateStep.DownloadUpdate);
                        }

                        return true;
                    }

                    break;

                case UpdateStep.BeginExtraction:

                    if (frameOn == Frame.Checking)
                    {
                        // waiting to be told to check for updates...
                        if (downloader == null)
                        {
                            // report 0% and begin checking
                            updateHelper.SendProgress(0, UpdateStep.CheckForUpdate);
                            CheckForUpdate();
                        }
                        else // already checking ...
                        {
                            // report 0% progress
                            updateHelper.SendProgress(0, UpdateStep.CheckForUpdate);
                        }

                        return true;
                    }

                    // if we haven't downloaded yet...
                    if (frameOn == Frame.UpdateInfo)
                    {
                        ShowFrame(Frame.InstallUpdates);

                        // report 0% progress & download
                        updateHelper.SendProgress(0, UpdateStep.DownloadUpdate);
                        DownloadUpdate();
                    }

                    if (frameOn == Frame.InstallUpdates)
                    {
                        // if already downloading ...
                        if (update.CurrentlyUpdating == UpdateOn.DownloadingUpdate)
                        {
                            // report 0%
                            updateHelper.SendProgress(0, UpdateStep.DownloadUpdate);
                            return true;
                        }

                        // if done extracting...
                        if(updtDetails != null)
                        {
                            // report extraction completed successfully
                            updateHelper.SendSuccess(UpdateStep.BeginExtraction);
                            return true;
                        }

                        if (currentlyExtracting)
                        {
                            // report extraction has begun
                            updateHelper.SendProgress(0, UpdateStep.BeginExtraction);
                            return true;
                        }
                    }


                    break;

                case UpdateStep.RestartInfo:
                case UpdateStep.Install:

                    if (frameOn == Frame.Checking)
                    {
                        // waiting to be told to check for updates...
                        if (downloader == null)
                        {
                            // report 0% and begin checking
                            updateHelper.SendProgress(0, UpdateStep.CheckForUpdate);
                            CheckForUpdate();
                        }
                        else // already checking ...
                        {
                            // report 0% progress
                            updateHelper.SendProgress(0, UpdateStep.CheckForUpdate);
                        }

                        return true;
                    }

                    // if we haven't downloaded yet...
                    if (frameOn == Frame.UpdateInfo)
                    {
                        ShowFrame(Frame.InstallUpdates);

                        // report 0% progress & download
                        updateHelper.SendProgress(0, UpdateStep.DownloadUpdate);
                        DownloadUpdate();
                    }

                    if (frameOn == Frame.InstallUpdates)
                    {
                        // if already downloading ...
                        if (update.CurrentlyUpdating == UpdateOn.DownloadingUpdate)
                        {
                            // report 0%
                            updateHelper.SendProgress(0, UpdateStep.DownloadUpdate);
                            return true;
                        }

                        if (currentlyExtracting)
                        {
                            // report extraction has begun
                            updateHelper.SendProgress(0, UpdateStep.BeginExtraction);
                            return true;
                        }
                    }

                    break;
            }

            // no bad request found - continue processing as usual
            return false;
        }


        string CreateAutoUpdateTempFolder()
        {
            string temp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                       "wyUpdate AU");

            // if the folder temp folder doesn't exist, create the folder with hiden attributes
            if(!Directory.Exists(temp))
            {
                Directory.CreateDirectory(temp);

                File.SetAttributes(temp, FileAttributes.System | FileAttributes.Hidden);
            }

            temp = Path.Combine(temp, "cache\\" + update.GUID);

            Directory.CreateDirectory(temp);

            return temp;
        }


        void PrepareStepOn(UpdateStepOn step)
        {
            switch (step)
            {
                case UpdateStepOn.Checking:

                    ShowFrame(Frame.Checking);

                    break;

                case UpdateStepOn.UpdateAvailable:

                    ShowFrame(Frame.UpdateInfo);

                    break;

                case UpdateStepOn.UpdateDownloaded:

                    // set the update step pending (extracting)
                    update.CurrentlyUpdating = UpdateOn.Extracting;

                    needElevation = NeedElevationToUpdate();

                    // show frame InstallUpdate
                    ShowFrame(Frame.InstallUpdates);

                    // put a checkmark next to downloaded
                    panelDisplaying.UpdateItems[0].Status = UpdateItemStatus.Success;

                    break;

                case UpdateStepOn.UpdateReadyToInstall:

                    string updtDetailsFilename = Path.Combine(tempDirectory, "updtdetails.udt");

                    // Try to load the update details file

                    if (File.Exists(updtDetailsFilename))
                    {
                        updtDetails = UpdateDetails.Load(updtDetailsFilename);
                    }
                    else
                        throw new Exception("Update details file does not exist.");

                    // set the update step pending (closing processes & installing files, etc.)
                    update.CurrentlyUpdating = UpdateOn.ClosingProcesses;

                    needElevation = NeedElevationToUpdate();

                    // show frame InstallUpdate
                    ShowFrame(Frame.InstallUpdates);

                    // put a checkmark next to downloaded
                    panelDisplaying.UpdateItems[0].Status = UpdateItemStatus.Success;

                    // set the "Extracting" text
                    SetStepStatus(1, clientLang.Extract);

                    break;

                default:
                    throw new Exception("Can't restore from this automatic update state: " + step);
            }
        }


        void SaveAutoUpdateData(UpdateStepOn updateStepOn)
        {
            FileStream fs = new FileStream(autoUpdateStateFile, FileMode.Create, FileAccess.Write);

            // Write any file-identification data you want to here
            WriteFiles.WriteHeader(fs, "IUAUFV1");

            // Step on {Checked = 2, Downloaded = 4, Extracted = 6}
            WriteFiles.WriteInt(fs, 0x01, (int)updateStepOn);

            // file to execute
            if (updateHelper.FileToExecuteAfterUpdate != null)
                WriteFiles.WriteString(fs, 0x02, updateHelper.FileToExecuteAfterUpdate);

            if (updateHelper.AutoUpdateID != null)
                WriteFiles.WriteString(fs, 0x03, updateHelper.AutoUpdateID);

            // Server data file location
            if (!string.IsNullOrEmpty(serverFileLoc))
                WriteFiles.WriteString(fs, 0x04, serverFileLoc);

            // Client's server file location (self update server file)
            if (!string.IsNullOrEmpty(clientSFLoc))
                WriteFiles.WriteString(fs, 0x05, clientSFLoc);

            // temp directory
            if (!string.IsNullOrEmpty(tempDirectory))
                WriteFiles.WriteString(fs, 0x06, tempDirectory);

            // the update filename
            if (!string.IsNullOrEmpty(updateFilename))
                WriteFiles.WriteString(fs, 0x07, updateFilename);

            if(SelfUpdateState != SelfUpdateState.None)
            {
                WriteFiles.WriteInt(fs, 0x08, (int) SelfUpdateState);

                if (SelfUpdateState == SelfUpdateState.Downloaded)
                    WriteFiles.WriteString(fs, 0x09, updateFilename);
                else if (SelfUpdateState == SelfUpdateState.Extracted)
                {
                    WriteFiles.WriteString(fs, 0x09, newSelfLocation);

                    WriteFiles.WriteString(fs, 0x0A, oldSelfLocation);
                }
            }

            fs.WriteByte(0xFF);
            fs.Close();
        }

        void LoadAutoUpdateData()
        {
            autoUpdateStateFile = Path.Combine(tempDirectory, "autoupdate");

            using (FileStream fs = new FileStream(autoUpdateStateFile, FileMode.Open, FileAccess.Read))
            {
                if (!ReadFiles.IsHeaderValid(fs, "IUAUFV1"))
                {
                    throw new Exception("Auto update state file ID is wrong.");
                }

                byte bType = (byte) fs.ReadByte();
                while (!ReadFiles.ReachedEndByte(fs, bType, 0xFF))
                {
                    switch (bType)
                    {
                        case 0x01:

                            startStep = (UpdateStepOn) ReadFiles.ReadInt(fs);

                            break;
                        case 0x02: // file to execute
                            updateHelper.FileToExecuteAfterUpdate = ReadFiles.ReadString(fs);
                            break;

                        case 0x03: // autoupdate ID
                            updateHelper.AutoUpdateID = ReadFiles.ReadString(fs);
                            break;

                        case 0x04: // Server data file location
                            serverFileLoc = ReadFiles.ReadString(fs);

                            if (!File.Exists(serverFileLoc))
                                serverFileLoc = null;

                            break;

                        case 0x05: // Client's server file location (self update server file)
                            clientSFLoc = ReadFiles.ReadString(fs);

                            if (!File.Exists(clientSFLoc))
                                clientSFLoc = null;
                            break;

                        case 0x06: // Temp directory
                            tempDirectory = ReadFiles.ReadString(fs);
                            break;

                        case 0x07: // update filename
                            updateFilename = ReadFiles.ReadString(fs);
                            break;

                        case 0x08:
                            SelfUpdateState = (SelfUpdateState) ReadFiles.ReadInt(fs);
                            break;

                        case 0x09:
                            if (SelfUpdateState == SelfUpdateState.Downloaded)
                                updateFilename = ReadFiles.ReadString(fs);
                            else
                                newSelfLocation = ReadFiles.ReadString(fs);

                            break;

                        case 0x0A:
                            oldSelfLocation = ReadFiles.ReadString(fs);
                            break;

                        default:
                            ReadFiles.SkipField(fs, bType);
                            break;
                    }

                    bType = (byte) fs.ReadByte();
                }
            }

            // if the server file doesn't exist we need to download a new one
            if (serverFileLoc == null)
                startStep = UpdateStepOn.Checking;
            else
            {
                // load the server file
                LoadServerFile(true);

                if(SelfUpdateState == SelfUpdateState.Extracted && !IsNewSelf)
                {
                    // launch new wyUpdate
                    StartNewSelfAndClose();
                }
            }
        }
    }
}