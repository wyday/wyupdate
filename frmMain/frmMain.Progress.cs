using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using wyUpdate.Common;

namespace wyUpdate
{
    public partial class frmMain
    {
        delegate void ShowProgressDelegate(int weightedPercentDone, int percentDone, bool statusDone, string extraStatus, Exception ex);
        delegate void UninstallProgressDel(int percentDone, int stepOn, string extraStatus, Exception ex);
        delegate void CheckProcessesDel(List<FileInfo> files, bool statusDone);

        delegate void ChangeRollbackDelegate(bool rbRegistry);


        // update the label & progress bar when downloading/updating
        private void ShowProgress(int percentDone, int unweightedPercent, bool done, string extraStatus, Exception ex)
        {
            //update progress bar when between 0 and 100
            if (percentDone > -1 && percentDone < 101)
            {
                panelDisplaying.Progress = percentDone;

                // send the progress to the AutoUpdate control
                if (isAutoUpdateMode && updateHelper.UpdateStep != UpdateStep.Install)
                    updateHelper.SendProgress(unweightedPercent);
            }

            //update bottom status
            if (extraStatus != panelDisplaying.ProgressStatus && extraStatus != "")
                panelDisplaying.ProgressStatus = extraStatus;

            if (done && ex == null)
            {
                if (isCancelled)
                    Close(); //close the form
                else if (frameOn == Frame.Checking)
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
                        ex = e; //error occured, show error screen
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


            if (ex != null)
            {
                //Show the error (rollback has already been done)
                if (frameOn == Frame.Checking)
                {
                    error = clientLang.ServerError;
                    errorDetails = ex.Message;
                }
                else
                {
                    if (update.CurrentlyUpdating == UpdateOn.DownloadingUpdate)
                    {
                        //a download error occurred
                        error = clientLang.DownloadError;
                        errorDetails = ex.Message;
                    }
                    else // an update error occurred
                    {
                        // if the exception was PatchApplicationException, then
                        //see if a catch-all update exists (and the catch-all update isn't the one that failed)
                        if (ex.GetType() == typeof(PatchApplicationException) &&
                            updateFrom != update.VersionChoices[update.VersionChoices.Count - 1] &&
                            update.VersionChoices[update.VersionChoices.Count - 1].Version == update.NewVersion)
                        {
                            updateFrom = update.VersionChoices[update.VersionChoices.Count - 1];

                            error = null;

                            panelDisplaying.UpdateItems[1].Status = UpdateItemStatus.Nothing;

                            update.CurrentlyUpdating = UpdateOn.DownloadingUpdate;

                            // download the catch-all update
                            ShowFrame(Frame.InstallUpdates);

                            return;
                        }

                        error = clientLang.GeneralUpdateError;
                        errorDetails = ex.Message;
                    }
                }

                updateHelper.SendFailed(error, errorDetails);

                ShowFrame(Frame.Error);
            }
        }


        private void SelfUpdateProgress(int percentDone, int unweightedProgress, bool done, string extraStatus, Exception ex)
        {
            //update progress bar
            panelDisplaying.Progress = percentDone;

            //update bottom status
            if (extraStatus != panelDisplaying.ProgressStatus && extraStatus != "")
                panelDisplaying.ProgressStatus = extraStatus;

            if (done && ex == null)
            {
                if (isCancelled)
                    Close(); //close the form
                else if (frameOn == Frame.Checking)
                {
                    clientSFLoc = downloader.DownloadingTo;

                    try
                    {
                        // client server file downloaded sucessfully
                        DownloadClientSFSuccess();
                    }
                    catch (Exception e)
                    {
                        ex = e;
                    }
                }
                else
                {
                    if (update.CurrentlyUpdating == UpdateOn.DownloadingClientUpdt)
                    {
                        panelDisplaying.UpdateItems[0].Status = UpdateItemStatus.Success;

                        //set the filename of the downloaded client update file
                        updateFilename = downloader.DownloadingTo;

                        //begin extracting and installing the update
                        update.CurrentlyUpdating = UpdateOn.SelfUpdating;
                        InstallUpdates(update.CurrentlyUpdating);
                    }
                    else if (update.CurrentlyUpdating == UpdateOn.SelfUpdating)
                    {
                        panelDisplaying.UpdateItems[1].Status = UpdateItemStatus.Success;

                        //start the newly installed client and resume "normal" downloading & updating
                        StartSelfElevated();
                    }
                }
            }


            if (ex != null)
            {
                // if a new client is *required* to install the update...
                if (VersionTools.Compare(VersionTools.FromExecutingAssembly(), update.MinClientVersion) == -1)
                {
                    //show an error and bail out
                    error = clientLang.SelfUpdateInstallError;
                    errorDetails = ex.Message;

                    ShowFrame(Frame.Error);
                }
                else //self update isn't necessary, so handle gracefully
                {
                    if (frameOn == Frame.Checking)
                    {
                        //client server file failed to download, continue as usual:

                        willSelfUpdate = false;

                        //Show update info page
                        ShowFrame(Frame.UpdateInfo);
                    }
                    else
                    {
                        // if the exception was PatchApplicationException, then
                        //see if a catch-all update exists (and the catch-all update isn't the one that failed)
                        if (ex.GetType() == typeof(PatchApplicationException) &&
                            updateFrom != update.VersionChoices[update.VersionChoices.Count - 1] &&
                            update.VersionChoices[update.VersionChoices.Count - 1].Version == update.NewVersion)
                        {
                            updateFrom = update.VersionChoices[update.VersionChoices.Count - 1];

                            error = null;

                            panelDisplaying.UpdateItems[1].Status = UpdateItemStatus.Nothing;

                            update.CurrentlyUpdating = UpdateOn.DownloadingUpdate;

                            // download the catch-all update
                            ShowFrame(Frame.InstallUpdates);

                            return;
                        }


                        //self-update failed to download or install
                        //just relaunch old client and continue with update
                        StartSelfElevated();
                    }
                }
            }
        }

        private void UninstallProgress(int percentDone, int step, string extraStatus, Exception ex)
        {
            //update progress bar
            panelDisplaying.Progress = percentDone;

            //update bottom status
            if (extraStatus != panelDisplaying.ProgressStatus && extraStatus != "")
                panelDisplaying.ProgressStatus = extraStatus;

            //step: 0=working, 1=uninstalling registry, 2=done
            if (step == 1)
            {
                panelDisplaying.UpdateItems[0].Status = UpdateItemStatus.Success;
                SetStepStatus(1, clientLang.UninstallRegistry);
            }
            else if (step == 2 && ex == null)
            {
                //just bail out.
                Close();
            }
            else if (ex != null)
            {
                if (isSilent)
                    Close();
                else
                {
                    //Show the error (rollback has ocurred)
                    error = ex.Message;
                    ShowFrame(Frame.Error);
                }
            }
        }

        private void CheckProcess(List<FileInfo> files, bool done)
        {
            if (done)
            {
                if (files != null)//if there are some files needing closing
                {
                    // show myself, make topmost
                    Show();
                    TopMost = true;
                    TopMost = false;

                    // start the close processes form
                    Form proc = new frmProcesses(files, clientLang);
                    DialogResult result = proc.ShowDialog();

                    if (result == DialogResult.Cancel)
                    {
                        //cancel the update process
                        CancelUpdate(true);
                    }
                    else
                    {
                        //processes closed, continue on
                        update.CurrentlyUpdating += 1;
                        InstallUpdates(update.CurrentlyUpdating);
                    }
                }
                else
                {
                    //no processes need to be closed, continue on
                    update.CurrentlyUpdating += 1;
                    InstallUpdates(update.CurrentlyUpdating);
                }
            }
        }

        private void ChangeRollback(bool rbRegistry)
        {
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