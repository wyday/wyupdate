using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using wyUpdate.Common;

namespace wyUpdate
{
    public partial class frmMain
    {
        frmFilesInUse inUseForm;

        // update the label & progress bar when downloading/updating
        void ShowProgress(int percentDone, int unweightedPercent, string extraStatus, ProgressStatus status, Object payload)
        {
            if (IsDisposed)
                return;

            //update progress bar when between 0 and 100
            if (percentDone > -1 && percentDone < 101)
            {
                panelDisplaying.Progress = percentDone;

                // send the progress to the AutoUpdate control
                if (isAutoUpdateMode && autoUpdateStepProcessing != UpdateStep.Install)
                    updateHelper.SendProgress(unweightedPercent, autoUpdateStepProcessing);
            }

            // update bottom status
            if (!string.IsNullOrEmpty(extraStatus) && extraStatus != panelDisplaying.ProgressStatus)
                panelDisplaying.ProgressStatus = extraStatus;

            if (status == ProgressStatus.SharingViolation)
            {
                // show the dialog showing which file is in-use by another process
                if (inUseForm == null)
                {
                    inUseForm = new frmFilesInUse(clientLang, (string)payload);

                    frmFilesInUse inuf = inUseForm;

                    inuf.ShowDialog(this);

                    //cancel the update process
                    if (inuf.CancelUpdate)
                        CancelUpdate(true);
                }

                return;
            }

            if (inUseForm != null)
            {
                // close the InUse form
                inUseForm.Close();
                inUseForm.Visible = false;
                inUseForm = null;
            }

            if (installUpdate != null && (status == ProgressStatus.Success || status == ProgressStatus.Failure))
            {
                installUpdate.Rollback -= ChangeRollback;
                installUpdate.ProgressChanged -= ShowProgress;
            }

            if (status == ProgressStatus.Success)
            {
                if (frameOn == Frame.Checking)
                {
                    //set the serverfile location
                    serverFileLoc = downloader.DownloadingTo;
                    try
                    {
                        ServerDownloadedSuccessfully();
                    }
                    catch (NoUpdatePathToNewestException)
                    {
                        //there is no update path to the newest version
                        ShowFrame(Frame.NoUpdatePathAvailable);
                        return;
                    }
                    catch (Exception e)
                    {
                        //error occured, show error screen
                        status = ProgressStatus.Failure;
                        payload = e;
                    }
                }
                else
                {
                    if (update.CurrentlyUpdating == UpdateOn.DownloadingUpdate)
                        updateFilename = downloader.DownloadingTo;

                    // continue on to next step
                    StepCompleted();
                }
            }


            if (status == ProgressStatus.Failure)
            {
                // Show the error (rollback has already been done)
                if (isCancelled)
                {
                    Close();
                    return;
                }
                if (frameOn == Frame.Checking)
                {
                    error = clientLang.ServerError;
                    errorDetails = ((Exception)payload).Message;
                }
                else
                {
                    if (update.CurrentlyUpdating == UpdateOn.DownloadingUpdate)
                    {
                        //a download error occurred
                        error = clientLang.DownloadError;
                        errorDetails = ((Exception)payload).Message;

                        //TODO: retry downloads in AutoUpdateMode
                        /*
                        if (isAutoUpdateMode)
                        {
                            TimeSpan span = DateTime.Now - File.GetCreationTime(serverFileLoc);

                            if (span.Days > 0 || span.Hours > 2)
                            {
                                
                            }
                            //TODO: if we're in AutoUpdate mode, and the
                            //download failure was an update file, and the
                            //server file is older than 1 hour, redownload the server and see if there's a new download site

                            //TODO: ditto for self-update failures (clientSFLoc)
                        }*/
                    }
                    else // an update error occurred
                    {
                        // if the exception was PatchApplicationException, then
                        //see if a catch-all update exists (and the catch-all update isn't the one that failed)
                        if (payload is PatchApplicationException &&
                            updateFrom != ServerFile.VersionChoices[ServerFile.VersionChoices.Count - 1] &&
                            ServerFile.VersionChoices[ServerFile.VersionChoices.Count - 1].Version == ServerFile.NewVersion)
                        {
                            updateFrom = ServerFile.VersionChoices[ServerFile.VersionChoices.Count - 1];

                            error = null;

                            panelDisplaying.UpdateItems[1].Status = UpdateItemStatus.Nothing;

                            // we're no longer extracting
                            if (isAutoUpdateMode)
                                autoUpdateStepProcessing = UpdateStep.DownloadUpdate;

                            // download the catch-all update
                            DownloadUpdate();

                            // set for auto-updates
                            currentlyExtracting = false;

                            return;
                        }

                        error = clientLang.GeneralUpdateError;
                        errorDetails = ((Exception)payload).Message;
                    }
                }

                if (isAutoUpdateMode)
                    updateHelper.SendFailed(error, errorDetails, autoUpdateStepProcessing);

                ShowFrame(Frame.Error);
            }
        }

        void SelfUpdateProgress(int percentDone, int unweightedPercent, string extraStatus, ProgressStatus status, Object payload)
        {
            if (IsDisposed)
                return;

            //update progress bar
            if (percentDone > -1 && percentDone < 101)
                panelDisplaying.Progress = percentDone;

            //update bottom status
            if (!string.IsNullOrEmpty(extraStatus) && extraStatus != panelDisplaying.ProgressStatus)
                panelDisplaying.ProgressStatus = extraStatus;

            if (installUpdate != null && (status == ProgressStatus.Success || status == ProgressStatus.Failure))
                installUpdate.ProgressChanged -= SelfUpdateProgress;

            if (status == ProgressStatus.Success)
            {
                if (frameOn == Frame.Checking)
                {
                    clientSFLoc = downloader.DownloadingTo;

                    try
                    {
                        // client server file downloaded sucessfully
                        DownloadClientSFSuccess();
                    }
                    catch (Exception e)
                    {
                        //error occured, show error screen
                        status = ProgressStatus.Failure;
                        payload = e;
                    }
                }
                else
                {
                    switch (update.CurrentlyUpdating)
                    {
                        case UpdateOn.DownloadingSelfUpdate:

                            //set the filename of the downloaded client update file
                            updateFilename = downloader.DownloadingTo;

                            if (isAutoUpdateMode)
                            {
                                SelfUpdateState = SelfUpdateState.Downloaded;

                                // save autoupdate file (new selfupdate state is saved)
                                SaveAutoUpdateData(wyDay.Controls.UpdateStepOn.UpdateAvailable);

                                // begin extracting self
                                update.CurrentlyUpdating = UpdateOn.ExtractSelfUpdate;
                                InstallUpdates(update.CurrentlyUpdating);
                            }
                            else // regular self update mode
                            {
                                panelDisplaying.UpdateItems[0].Status = UpdateItemStatus.Success;

                                //begin extracting and installing the update
                                update.CurrentlyUpdating = UpdateOn.FullSelfUpdate;
                                InstallUpdates(update.CurrentlyUpdating);
                            }

                            break;

                        case UpdateOn.FullSelfUpdate:

                            panelDisplaying.UpdateItems[1].Status = UpdateItemStatus.Success;

                            //start the newly installed client and resume "normal" downloading & updating
                            StartSelfElevated();
                            break;

                        case UpdateOn.ExtractSelfUpdate:

                            SelfUpdateState = SelfUpdateState.Extracted;

                            // oldSelfLocation already set in the InstallUpdates(ExtractSelfUpdate)
                            newSelfLocation = installUpdate.NewSelfLoc;

                            // save autoupdate file (new selfupdate state is saved)
                            SaveAutoUpdateData(wyDay.Controls.UpdateStepOn.UpdateAvailable);

                            // start the new client
                            StartNewSelfAndClose();
                            
                            return;

                        case UpdateOn.InstallSelfUpdate:

                            SelfUpdateState = SelfUpdateState.None;

                            // save autoupdate file (new selfupdate state is saved)
                            SaveAutoUpdateData(wyDay.Controls.UpdateStepOn.UpdateReadyToInstall);

                            // we must set new self to false because it's used in StartSelfElevated()
                            // to set the /ns argument for the newly launched wyUpdate
                            IsNewSelf = false;

                            // relaunch newly installed self to do regular update
                            StartSelfElevated();

                            return;
                    }
                }
            }


            if (status == ProgressStatus.Failure)
            {
                bool selfUpdateRequired =
                    VersionTools.Compare(VersionTools.FromExecutingAssembly(), ServerFile.MinClientVersion) == -1;

                bool canTryCatchAllUpdate = frameOn != Frame.Checking

                                            // patch failed
                                            && payload.GetType() == typeof(PatchApplicationException)

                                            // if the catch-all update isn't the one that failed
                                            && updateFrom != SelfServerFile.VersionChoices[SelfServerFile.VersionChoices.Count - 1]

                                            // and there is a catch-all update
                                            && SelfServerFile.VersionChoices[SelfServerFile.VersionChoices.Count - 1].Version == SelfServerFile.NewVersion;


                // if a new client is *required* to install the update...
                if (selfUpdateRequired && !canTryCatchAllUpdate)
                {
                    //show an error and bail out
                    error = clientLang.SelfUpdateInstallError;
                    errorDetails = ((Exception)payload).Message;

                    // report error back to the app
                    if (isAutoUpdateMode)
                        updateHelper.SendFailed(error, errorDetails, autoUpdateStepProcessing);

                    ShowFrame(Frame.Error);
                }
                else if (frameOn == Frame.Checking)
                {
                    // client server file failed to download, continue as usual:
                    SelfUpdateState = SelfUpdateState.None;

                    // Show update info page (or just start downloading & installing)
                    ShowFrame(SkipUpdateInfo ? Frame.InstallUpdates : Frame.UpdateInfo);
                }
                else
                {
                    if (canTryCatchAllUpdate)
                    {
                        // select the catch all update
                        updateFrom = SelfServerFile.VersionChoices[SelfServerFile.VersionChoices.Count - 1];

                        // clear errors
                        error = null;
                        errorDetails = null;

                        panelDisplaying.UpdateItems[1].Status = UpdateItemStatus.Nothing;

                        if (isAutoUpdateMode)
                        {
                            // change update state from Downloaded to WillUpdate (just autoupdate)
                            SelfUpdateState = SelfUpdateState.WillUpdate;

                            // save the fact that there's no longer an update file
                            SaveAutoUpdateData(wyDay.Controls.UpdateStepOn.UpdateAvailable);
                        }

                        // download the catch-all update
                        DownloadUpdate();

                        return;
                    }


                    if (isAutoUpdateMode)
                    {
                        SelfUpdateState = SelfUpdateState.None;

                        if (update.CurrentlyUpdating == UpdateOn.InstallSelfUpdate)
                        {
                            // update has already been downloaded & extracted
                            SaveAutoUpdateData(wyDay.Controls.UpdateStepOn.UpdateReadyToInstall);

                            UpdateHelper_RequestReceived(this, UpdateAction.UpdateStep, UpdateStep.Install);
                        }
                        else
                        {
                            // update hasn't been downloaded yet
                            SaveAutoUpdateData(wyDay.Controls.UpdateStepOn.UpdateAvailable);

                            UpdateHelper_RequestReceived(this, UpdateAction.UpdateStep, UpdateStep.DownloadUpdate);
                        }
                    }
                    else
                    {
                        //self-update failed to download or install
                        //just relaunch old client and continue with update
                        StartSelfElevated();
                    }
                }
            }
        }

        void UninstallProgress(int percentDone, int unweightedPercent, string extraStatus, ProgressStatus status, Object payload)
        {
            if (IsDisposed)
                return;

            // update progress bar
            if (percentDone > -1 && percentDone < 101)
                panelDisplaying.Progress = percentDone;

            //update bottom status
            if (!string.IsNullOrEmpty(extraStatus) && extraStatus != panelDisplaying.ProgressStatus)
                panelDisplaying.ProgressStatus = extraStatus;

            if (status == ProgressStatus.Success || status == ProgressStatus.Failure)
                installUpdate.ProgressChanged -= UninstallProgress;

            switch (status)
            {
                case ProgressStatus.None:
                    panelDisplaying.UpdateItems[0].Status = UpdateItemStatus.Success;
                    SetStepStatus(1, clientLang.UninstallRegistry);
                    break;
                case ProgressStatus.Success:
                    //just bail out.
                    Close();
                    break;
                case ProgressStatus.Failure:
                    if (isSilent)
                        Close();
                    else
                    {
                        // Show the error (rollback has ocurred)
                        error = clientLang.GeneralUpdateError;
                        errorDetails = ((Exception)payload).Message;
                        ShowFrame(Frame.Error);
                    }
                    break;
            }
        }

        void CheckProcess(int percentDone, int unweightedPercent, string extraStatus, ProgressStatus status, Object payload)
        {
            if (IsDisposed)
                return;

            // update bottom status
            if (!string.IsNullOrEmpty(extraStatus) && extraStatus != panelDisplaying.ProgressStatus)
                panelDisplaying.ProgressStatus = extraStatus;

            if (status == ProgressStatus.Success || status == ProgressStatus.Failure)
            {
                installUpdate.Rollback -= ChangeRollback;
                installUpdate.ProgressChanged -= CheckProcess;
            }

            if (status == ProgressStatus.Success)
            {
                List<FileInfo> files = null;
                List<Process> rProcesses = null;

                if (payload is object[])
                {
                    files = (List<FileInfo>)((object[])payload)[0];
                    rProcesses = (List<Process>)((object[])payload)[1];
                }

                if (rProcesses != null) //if there are some processes need closing
                {
                    // remove processes that have exited since last checked
                    for (int i = 0; i < rProcesses.Count; i++)
                    {
                        try
                        {
                            if (rProcesses[i].HasExited)
                                rProcesses.RemoveAt(i);
                        }
                        catch { }
                    }

                    // only continue if processes are still running
                    if (rProcesses.Count > 0)
                    {
                        // show myself, make topmost
                        Show();
                        TopMost = true;
                        TopMost = false;

                        // start the close processes form
                        Form proc = new frmProcesses(files, rProcesses, clientLang);

                        if (proc.ShowDialog() == DialogResult.Cancel)
                        {
                            //cancel the update process
                            CancelUpdate(true);
                            return;
                        }
                    }
                }

                // processes closed, continue on
                update.CurrentlyUpdating += 1;
                InstallUpdates(update.CurrentlyUpdating);
            }

            if (status == ProgressStatus.Failure)
            {
                if (isCancelled)
                {
                    Close();
                    return;
                }

                error = clientLang.GeneralUpdateError;
                errorDetails = ((Exception)payload).Message;

                ShowFrame(Frame.Error);
            }
        }

        void ChangeRollback(bool rbRegistry)
        {
            if (IsDisposed)
                return;

            DisableCancel();

            // set the error icon to current progress item
            if (rbRegistry)
            {
                // error updating the registry
                panelDisplaying.UpdateItems[2].Status = UpdateItemStatus.Error;

                SetStepStatus(3, clientLang.RollingBackRegistry);
            }
            else if (panelDisplaying.UpdateItems[2].Status != UpdateItemStatus.Error)
            {
                // error updating the files
                panelDisplaying.UpdateItems[1].Status = UpdateItemStatus.Error;

                SetStepStatus(2, clientLang.RollingBackFiles);
            }
            else
            {
                SetStepStatus(3, clientLang.RollingBackFiles);
            }
        }
    }
}