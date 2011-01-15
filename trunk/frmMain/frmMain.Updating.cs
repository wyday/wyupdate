using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;
using wyDay.Controls;
using wyUpdate.Common;

namespace wyUpdate
{
    public partial class frmMain
    {
        void ShowFrame(Frame frameNum)
        {
            frameOn = frameNum;

            switch (frameNum)
            {
                case Frame.Checking: //Update checking screen
                    panelDisplaying.ChangePanel(FrameType.Update,
                        clientLang.Checking.Title,
                        clientLang.Checking.SubTitle,
                        clientLang.Checking.Content,
                        "");

                    btnNext.Enabled = false;

                    if (!isAutoUpdateMode)
                    {
                        CheckForUpdate();
                    }

                    break;
                case Frame.UpdateInfo: //Update Info Screen
                    panelDisplaying.ChangePanel(FrameType.TextInfo,
                        clientLang.UpdateInfo.Title,
                        clientLang.UpdateInfo.SubTitle,
                        clientLang.UpdateInfo.Content,
                        clientLang.UpdateBottom);

                    //check if elevation is needed
                    needElevation = NeedElevationToUpdate();

                    btnNext.Enabled = true;
                    btnNext.Text = clientLang.UpdateButton;

                    if (QuickCheck)
                    {
                        // return 2 if we're just checking
                        if (QuickCheckJustCheck)
                        {
                            ReturnCode = 2;
                            Close();
                            return;
                        }

                        ShowInTaskbar = true;
                        WindowState = FormWindowState.Normal;

                        TopMost = true;
                        TopMost = false;

                        QuickCheck = false;
                    }
                    else if (isAutoUpdateMode)
                    {
                        // save the automatic updater file
                        SaveAutoUpdateData(UpdateStepOn.UpdateAvailable);

                        updateHelper.SendSuccess(ServerFile.NewVersion, panelDisplaying.GetChangesRTF(), true);
                    }

                    break;
                case Frame.InstallUpdates: //Download and Install Updates
                    panelDisplaying.ShowChecklist = true;

                    panelDisplaying.ChangePanel(FrameType.Update,
                        clientLang.DownInstall.Title,
                        clientLang.DownInstall.SubTitle,
                        clientLang.DownInstall.Content,
                        "");

                    if (SelfUpdateState == SelfUpdateState.FullUpdate)
                    {
                        //show status for downloading self
                        SetStepStatus(0, clientLang.DownloadingSelfUpdate);
                    }
                    else
                    {
                        //show status for the downloading update
                        SetStepStatus(0, clientLang.Download);
                    }

                    if (!isAutoUpdateMode)
                        DownloadUpdate();

                    btnNext.Enabled = false;

                    break;
                case Frame.UpdatedSuccessfully: //Display Congrats Window
                    panelDisplaying.ChangePanel(FrameType.WelcomeFinish,
                        clientLang.SuccessUpdate.Title,
                        clientLang.SuccessUpdate.Content,
                        "",
                        clientLang.FinishBottom);

                    btnNext.Enabled = true;
                    btnCancel.Visible = false;
                    btnNext.Text = clientLang.FinishButton;

                    break;
                case Frame.AlreadyUpToDate: //Your Product is already up to date screen
                    panelDisplaying.ChangePanel(FrameType.WelcomeFinish,
                        clientLang.AlreadyLatest.Title,
                        clientLang.AlreadyLatest.Content,
                        "",
                        clientLang.FinishBottom);

                    btnNext.Enabled = true;
                    btnCancel.Visible = false;
                    btnNext.Text = clientLang.FinishButton;

                    break;
                case Frame.NoUpdatePathAvailable: //No update to the latest version is available
                    if (!string.IsNullOrEmpty(ServerFile.NoUpdateToLatestLinkText))
                        panelDisplaying.SetNoUpdateAvailableLink(ServerFile.NoUpdateToLatestLinkText, ServerFile.NoUpdateToLatestLinkURL);

                    panelDisplaying.ChangePanel(FrameType.WelcomeFinish,
                        clientLang.NoUpdateToLatest.Title,
                        clientLang.NoUpdateToLatest.Content,
                        "",
                        clientLang.FinishBottom);

                    btnNext.Enabled = true;
                    btnCancel.Visible = false;
                    btnNext.Text = clientLang.FinishButton;

                    break;
                case Frame.Uninstall: //Uninstall screen
                    panelDisplaying.ShowChecklist = true;

                    panelDisplaying.ChangePanel(FrameType.Update,
                        clientLang.Uninstall.Title,
                        clientLang.Uninstall.SubTitle,
                        clientLang.Uninstall.Content,
                        "");


                    //Show uninstalling status
                    SetStepStatus(0, clientLang.UninstallFiles);

                    btnNext.Enabled = false;

                    InstallUpdates(UpdateOn.Uninstalling);

                    break;
                case Frame.Error: //Display error screen

                    //TODO: make the return codes error specific
                    ReturnCode = 1;

                    // show details button to hide all the complex crap from users
                    panelDisplaying.ErrorDetails = errorDetails;
                    panelDisplaying.SetUpErrorDetails(clientLang.ShowDetails);

                    panelDisplaying.ChangePanel(FrameType.WelcomeFinish,
                        clientLang.UpdateError.Title,
                        error,
                        "",
                        clientLang.FinishBottom);

                    btnNext.Enabled = true;
                    btnCancel.Visible = false;
                    btnNext.Text = clientLang.FinishButton;

                    // show wyUpdate if 
                    if (QuickCheck && !QuickCheckNoErr)
                    {
                        ShowInTaskbar = true;
                        WindowState = FormWindowState.Normal;

                        TopMost = true;
                        TopMost = false;

                        QuickCheck = false;
                    }

                    break;
            }

            //enable the close button on the Finish/Error screens
            if (FrameIs.ErrorFinish(frameNum))
            {
                // allow the user to forcefuly exit
                BlockLogOff(false);

                EnableCancel();

                //allow the user to exit by pressing ESC
                CancelButton = btnNext;

                //set the error return code (1) or success (0)
                ReturnCode = frameNum == Frame.Error ? 1 : 0;

                if (QuickCheck)
                {
                    if (frameNum == Frame.Error && !QuickCheckNoErr)
                    {
                        Visible = true;
                        TopMost = true;
                        TopMost = false;
                    }
                    else
                    {
                        WindowState = FormWindowState.Minimized;
                        ShowInTaskbar = false;
                        Visible = true;
                        Close();
                        return;
                    }
                }
                else if (isAutoUpdateMode)
                {
                    if (frameNum == Frame.UpdatedSuccessfully || frameNum == Frame.Error)
                    {
                        // save whether an update succeeded or failed
                        AutoUpdaterInfo auInfo;

                        if (frameNum == Frame.Error)
                        {
                            auInfo = new AutoUpdaterInfo(updateHelper.AutoUpdateID, oldAUTempFolder)
                                         {
                                             AutoUpdaterStatus = AutoUpdaterStatus.UpdateFailed,
                                             ErrorTitle = error,
                                             ErrorMessage = errorDetails
                                         };
                        }
                        else
                        {
                            auInfo = new AutoUpdaterInfo(updateHelper.AutoUpdateID, oldAUTempFolder)
                                         {
                                             AutoUpdaterStatus = AutoUpdaterStatus.UpdateSucceeded,
                                             UpdateVersion = ServerFile.NewVersion,
                                             ChangesInLatestVersion = panelDisplaying.GetChangesRTF(),
                                             ChangesIsRTF = true
                                         };
                        }

                        auInfo.Save();

                        try
                        {
                            if (updateHelper.IsAService)
                            {
                                if (updateHelper.ExecutionArguments != null)
                                {
                                    string[] args = CmdLineToArgvW.SplitArgs(updateHelper.ExecutionArguments);

                                    // start the windows service
                                    new ServiceController(updateHelper.FileOrServiceToExecuteAfterUpdate).Start(args);
                                }
                                else // start the windows service (without args)
                                    new ServiceController(updateHelper.FileOrServiceToExecuteAfterUpdate).Start();
                            }
                            else
                            {
                                // start the updated program as a limited user
                                LimitedProcess.Start(updateHelper.FileOrServiceToExecuteAfterUpdate,
                                                     updateHelper.ExecutionArguments);
                            }
                        }
                        catch { }
                    }

                    // we're no longer in autoupdate mode - cleanup temp files on close
                    isAutoUpdateMode = false;

                    Close();
                    return;
                }
                else if (frameNum == Frame.UpdatedSuccessfully && update.CloseOnSuccess)
                {
                    // if close on finish - then close
                    Close();
                    return;
                }
            }

            try
            {
                // so the user doesn't accidentally cancel update.
                btnNext.Focus();
            }
            catch { }

            // if silent & if on one of the user interaction screens, then click next
            if (isSilent && (FrameIs.Interaction(frameOn)))
            {
                btnNext_Click(null, EventArgs.Empty);
                return;
            }
        }

        void InstallUpdates(UpdateOn CurrentlyUpdating)
        {
            ShowProgressDelegate showProgress = ShowProgress;

            Thread asyncThread = null;

            switch (CurrentlyUpdating)
            {
                case UpdateOn.FullSelfUpdate:
                    SetStepStatus(1, clientLang.SelfUpdate);

                    showProgress = SelfUpdateProgress;

                    installUpdate = new InstallUpdate(this, showProgress)
                                        {
                                            //location of old "self" to replace
                                            OldSelfLoc = oldSelfLocation,
                                            Filename = Path.Combine(tempDirectory, updateFilename),
                                            OutputDirectory = tempDirectory
                                        };

                    asyncThread = new Thread(installUpdate.RunSelfUpdate);
                    break;
                case UpdateOn.ExtractSelfUpdate:
                    showProgress = SelfUpdateProgress;

                    oldSelfLocation = Application.ExecutablePath;

                    installUpdate = new InstallUpdate(this, showProgress)
                                        {
                                            // old self is needed for patching
                                            OldSelfLoc = oldSelfLocation,
                                            Filename = Path.Combine(tempDirectory, updateFilename),
                                            OutputDirectory = Path.Combine(tempDirectory, "selfupdate")
                                        };

                    asyncThread = new Thread(installUpdate.JustExtractSelfUpdate);
                    break;
                case UpdateOn.InstallSelfUpdate:
                    showProgress = SelfUpdateProgress;

                    installUpdate = new InstallUpdate(this, showProgress)
                                        {
                                            //location of old "self" to replace
                                            OldSelfLoc = oldSelfLocation,
                                            NewSelfLoc = newSelfLocation,
                                            Filename = Path.Combine(tempDirectory, updateFilename),
                                            OutputDirectory = Path.Combine(tempDirectory, "selfupdate")
                                        };

                    asyncThread = new Thread(installUpdate.JustInstallSelfUpdate);
                    break;
                case UpdateOn.Extracting:

                    // set for auto-updates
                    currentlyExtracting = true;

                    SetStepStatus(1, clientLang.Extract);

                    installUpdate = new InstallUpdate(this, showProgress)
                                        {
                                            Filename = Path.Combine(tempDirectory, updateFilename),
                                            OutputDirectory = tempDirectory,
                                            TempDirectory = tempDirectory,
                                            ProgramDirectory = baseDirectory
                                        };

                    asyncThread = new Thread(installUpdate.RunUnzipProcess);
                    break;
                case UpdateOn.ClosingProcesses:
                    SetStepStatus(1, clientLang.Processes);

                    installUpdate = new InstallUpdate(this, new CheckProcessesDel(CheckProcess))
                                        {
                                            UpdtDetails = updtDetails,
                                            RollbackDelegate = (ChangeRollbackDelegate)ChangeRollback,
                                            TempDirectory = tempDirectory,
                                            ProgramDirectory = baseDirectory
                                        };

                    asyncThread = new Thread(installUpdate.RunProcessesCheck);
                    break;
                case UpdateOn.PreExecute:

                    // try to stop the user from forcefuly exiting
                    BlockLogOff(true);

                    SetStepStatus(1, clientLang.PreExec);

                    installUpdate = new InstallUpdate(this, showProgress)
                                        {
                                            UpdtDetails = updtDetails,
                                            RollbackDelegate = (ChangeRollbackDelegate)ChangeRollback,
                                            TempDirectory = tempDirectory,
                                            ProgramDirectory = baseDirectory
                                        };

                    asyncThread = new Thread(installUpdate.RunPreExecute);
                    break;
                case UpdateOn.BackUpInstalling:
                    SetStepStatus(1, clientLang.Files);

                    installUpdate = new InstallUpdate(this, showProgress)
                                        {
                                            UpdtDetails = updtDetails,
                                            RollbackDelegate = (ChangeRollbackDelegate) ChangeRollback,
                                            TempDirectory = tempDirectory,
                                            ProgramDirectory = baseDirectory,
                                            IsAdmin = IsAdmin
                                        };

                    asyncThread = new Thread(installUpdate.RunUpdateFiles);
                    break;
                case UpdateOn.ModifyReg:
                    SetStepStatus(2, clientLang.Registry);

                    installUpdate = new InstallUpdate(this, showProgress)
                                        {
                                            UpdtDetails = updtDetails,
                                            RollbackDelegate = (ChangeRollbackDelegate) ChangeRollback,
                                            TempDirectory = tempDirectory,
                                            ProgramDirectory = baseDirectory
                                        };

                    asyncThread = new Thread(installUpdate.RunUpdateRegistry);
                    break;
                case UpdateOn.OptimizeExecute:
                    SetStepStatus(3, clientLang.Optimize);

                    installUpdate = new InstallUpdate(this, showProgress)
                                        {
                                            UpdtDetails = updtDetails,
                                            RollbackDelegate = (ChangeRollbackDelegate)ChangeRollback,
                                            TempDirectory = tempDirectory,
                                            ProgramDirectory = baseDirectory
                                        };

                    asyncThread = new Thread(installUpdate.RunOptimizeExecute);
                    break;
                case UpdateOn.WriteClientFile:
                    //don't show any status, but begin the thread to being updating the client file

                    //Save the client file with the new version
                    update.InstalledVersion = ServerFile.NewVersion;

                    installUpdate = new InstallUpdate(this, showProgress)
                                        {
                                            Filename = clientFileLoc,
                                            UpdtDetails = updtDetails,
                                            ClientFileType = clientFileType,
                                            ClientFile = update,
                                            SkipProgressReporting = true,
                                            TempDirectory = tempDirectory,
                                            ProgramDirectory = baseDirectory
                                        };

                    asyncThread = new Thread(installUpdate.RunUpdateClientDataFile);
                    break;
                case UpdateOn.DeletingTemp:
                    SetStepStatus(3, clientLang.TempFiles);

                    installUpdate = new InstallUpdate(this, showProgress)
                                        {
                                            TempDirectory = tempDirectory
                                        };

                    asyncThread = new Thread(installUpdate.RunDeleteTemporary);
                    break;
                case UpdateOn.Uninstalling:
                    //need to pass: client data file loc, delegate, this
                    installUpdate = new InstallUpdate(this, new UninstallProgressDel(UninstallProgress))
                                        {
                                            Filename = clientFileLoc
                                        };

                    asyncThread = new Thread(installUpdate.RunUninstall);
                    break;
                default:
                    break;
            }

            if (asyncThread != null)
            {
                //make them a daemon - prevent thread callback issues
                asyncThread.IsBackground = true;
                asyncThread.Start();
            }
        }

        void SetStepStatus(int stepNum, string stepText)
        {
            panelDisplaying.ProgressStatus = "";
            panelDisplaying.UpdateItems[stepNum].Status = UpdateItemStatus.Working;
            panelDisplaying.UpdateItems[stepNum].Text = stepText;
        }

        //update step completed, continue to next
        void StepCompleted()
        {
            if (update.CurrentlyUpdating == UpdateOn.DeletingTemp)
            {
                //successfully deleted temporary files
                panelDisplaying.UpdateItems[3].Status = UpdateItemStatus.Success;

                //Successfully Updated
                ShowFrame(Frame.UpdatedSuccessfully);

                btnNext.Enabled = true;
            }
            else
            {
                //show check mark to completed
                switch (update.CurrentlyUpdating)
                {
                    case UpdateOn.DownloadingUpdate: //done downloading
                        panelDisplaying.UpdateItems[0].Status = UpdateItemStatus.Success;
                        break;
                    case UpdateOn.Extracting:
                        //done extracting, load update details
                        updtDetails = installUpdate.UpdtDetails;
                        break;
                    case UpdateOn.BackUpInstalling: //done backing up & installing files
                        panelDisplaying.UpdateItems[1].Status = UpdateItemStatus.Success;
                        break;
                    case UpdateOn.ModifyReg: //done modifying registry
                        panelDisplaying.UpdateItems[2].Status = UpdateItemStatus.Success;
                        break;
                }

                // if we're in wait mode, then don't continue to the next step
                if (isAutoUpdateMode && (update.CurrentlyUpdating == UpdateOn.DownloadingUpdate || update.CurrentlyUpdating == UpdateOn.Extracting))
                {
                    // save the autoupdate state details
                    SaveAutoUpdateData(update.CurrentlyUpdating == UpdateOn.DownloadingUpdate
                                            ? UpdateStepOn.UpdateDownloaded
                                            : UpdateStepOn.UpdateReadyToInstall);

                    updateHelper.SendSuccess(autoUpdateStepProcessing);

                    //Go to the next step
                    update.CurrentlyUpdating += 1;

                    // set for auto-updates
                    currentlyExtracting = false;

                    return;
                }

                //Go to the next step
                update.CurrentlyUpdating += 1;

                //if there isn't an updateDetails file
                if (updtDetails == null &&
                    (update.CurrentlyUpdating == UpdateOn.PreExecute ||
                     update.CurrentlyUpdating == UpdateOn.OptimizeExecute ||
                     update.CurrentlyUpdating == UpdateOn.ModifyReg))
                {
                    update.CurrentlyUpdating += 1;

                    if (update.CurrentlyUpdating == UpdateOn.ModifyReg)
                        update.CurrentlyUpdating += 1;
                }

                InstallUpdates(update.CurrentlyUpdating);
            }
        }
    }
}