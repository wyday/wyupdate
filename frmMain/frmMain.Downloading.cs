using System.Collections.Generic;
using System.IO;
using wyUpdate.Common;
using wyUpdate.Downloader;

namespace wyUpdate
{
    public partial class frmMain
    {
        void CheckForUpdate()
        {
            if (!string.IsNullOrEmpty(serverOverwrite))
            {
                // overrite server file
                BeginDownload(new List<string> { serverOverwrite }, 0, null, false, false);
            }
            else
            {
                // download the server file
                BeginDownload(update.ServerFileSites, 0, null, false, false);
            }
        }

        void DownloadUpdate()
        {
            if (SelfUpdateState == SelfUpdateState.FullUpdate || isAutoUpdateMode && SelfUpdateState == SelfUpdateState.WillUpdate)
            {
                //download self update
                update.CurrentlyUpdating = UpdateOn.DownloadingSelfUpdate;
                BeginSelfUpdateDownload(updateFrom.FileSites, updateFrom.Adler32);
            }
            else
            {
                //download the update file
                update.CurrentlyUpdating = UpdateOn.DownloadingUpdate;
                BeginDownload(updateFrom.FileSites, updateFrom.Adler32, updateFrom.SignedSHA1Hash, true, true);
            }
        }

        //download regular update files
        void BeginDownload(List<string> sites, long adler32, byte[] signedSHA1Hash, bool relativeProgress, bool checkSigning)
        {
            if (downloader != null)
            {
                downloader.ProgressChanged -= ShowProgress;
                downloader.ProgressChanged -= SelfUpdateProgress;
            }

            downloader = new FileDownloader(sites, tempDirectory)
                             {
                                 Adler32 = adler32,
                                 UseRelativeProgress = relativeProgress,
                                 SignedSHA1Hash = signedSHA1Hash,
                                 PublicSignKey = checkSigning ? update.PublicSignKey : null
                             };

            downloader.ProgressChanged += ShowProgress;
            downloader.Download();
        }

        //download self update files (server file or update file)
        void BeginSelfUpdateDownload(List<string> sites, long adler32)
        {
            if (downloader != null)
            {
                downloader.ProgressChanged -= ShowProgress;
                downloader.ProgressChanged -= SelfUpdateProgress;
            }

            string suFolder = Path.Combine(tempDirectory, "su");

            if (!Directory.Exists(suFolder))
                Directory.CreateDirectory(suFolder);

            downloader = new FileDownloader(sites, suFolder)
                             {
                                 Adler32 = adler32
                             };

            downloader.ProgressChanged += SelfUpdateProgress;
            downloader.Download();
        }

        /// <summary>
        /// Called when the self-update server files has downloaded successfully.
        /// </summary>
        void DownloadClientSFSuccess()
        {
            // load the wyUpdate server file, and see if a new version is availiable
            ServerFile clientSF = ServerFile.Load(clientSFLoc, null, null);

            // check if the wyUpdate is new enough.
            if (VersionTools.Compare(VersionTools.FromExecutingAssembly(), clientSF.NewVersion) < 0)
            {
                SelfUpdateState = SelfUpdateState.WillUpdate;

                // autoupdate will need this SF
                if (isAutoUpdateMode)
                {
                    SelfServerFile = clientSF;
                    LoadClientServerFile();
                }
            }

            // Show update info page (or just start downloading & installing)
            if (SkipUpdateInfo)
            {
                // check if elevation is needed
                needElevation = NeedElevationToUpdate();

                if (needElevation || SelfUpdateState == SelfUpdateState.WillUpdate)
                    StartSelfElevated();
                else
                    ShowFrame(Frame.InstallUpdates);
            }
            else
                ShowFrame(Frame.UpdateInfo);
        }

        void LoadClientServerFile()
        {
            // load the self server file if it doesn't already exist
            if (SelfServerFile == null)
                SelfServerFile = ServerFile.Load(clientSFLoc, null, null);

            updateFrom = SelfServerFile.GetVersionChoice(VersionTools.FromExecutingAssembly());
        }

        void ServerDownloadedSuccessfully()
        {
            //load the server file into memory
            LoadServerFile(true);

            // if we went to the finish page, bail out
            if (frameOn != Frame.Checking)
                return;

            if (isAutoUpdateMode)
            {
                // set the autoupdate filename
                autoUpdateStateFile = Path.Combine(tempDirectory, "autoupdate");
            }

            //download the client server file and see if the client is new enough
            BeginSelfUpdateDownload(update.ClientServerSites, 0);
        }

        //returns True if an update is necessary, otherwise false
        void LoadServerFile(bool setChangesText)
        {
            //load the server file
            ServerFile = ServerFile.Load(serverFileLoc, updatePathVar, customUrlArgs);

            clientLang.NewVersion = ServerFile.NewVersion;

            // if no update is needed...
            if (VersionTools.Compare(update.InstalledVersion, ServerFile.NewVersion) >= 0)
            {
                if (isAutoUpdateMode)
                {
                    // send reponse that there's no update available
                    updateHelper.SendSuccess(null, null, true);

                    // close this client
                    isCancelled = true;

                    // let wyUpdate cleanup the files
                    isAutoUpdateMode = false;

                    // let ServerDownloadedSuccessfully() exit early
                    frameOn = Frame.AlreadyUpToDate;

                    Close();

                    return;
                }

                // Show "All Finished" page
                ShowFrame(Frame.AlreadyUpToDate);
                return;
            }

            // get the correct update file to download
            updateFrom = ServerFile.GetVersionChoice(update.InstalledVersion);

            // if the update install the x64 system32 folder on an x86 machine we need to throw an error
            if ((updateFrom.InstallingTo & InstallingTo.SysDirx64) == InstallingTo.SysDirx64 && !SystemFolders.Is64Bit())
            {
                error = "Update available, but can't install 64-bit files on a 32-bit machine.";
                errorDetails = "There's an update available (version " + ServerFile.NewVersion + "). However, this update will install files to the x64 (64-bit) system32 folder. And because this machine is an x86 (32-bit), there isn't an x64 system32 folder.";

                ShowFrame(Frame.Error);
                return;
            }

            // if the update install the x64 system32 folder on an x86 machine we need to throw an error
            if ((updateFrom.InstallingTo & InstallingTo.CommonFilesx64) == InstallingTo.CommonFilesx64 && !SystemFolders.Is64Bit())
            {
                error = "Update available, but can't install 64-bit files on a 32-bit machine.";
                errorDetails = "There's an update available (version " + ServerFile.NewVersion + "). However, this update will install files to the x64 (64-bit) \"Program File\\Common Files\" folder. And because this machine is an x86 (32-bit), there isn't an x64 \"Program File\\Common Files\" folder.";

                ShowFrame(Frame.Error);
                return;
            }

            // set the changes text
            if (setChangesText || isAutoUpdateMode)
            {
                int i = ServerFile.VersionChoices.IndexOf(updateFrom);

                //if there's a catch-all update start with one less than "update.VersionChoices.Count - 1"

                //build the changes from all previous versions
                for (int j = ServerFile.VersionChoices.Count - 1; j >= i; j--)
                {
                    //show the version number for previous updates we may have missed
                    if (j != ServerFile.VersionChoices.Count - 1 && (!ServerFile.CatchAllUpdateExists || ServerFile.CatchAllUpdateExists && j != ServerFile.VersionChoices.Count - 2))
                        panelDisplaying.AppendAndBoldText("\r\n\r\n" + ServerFile.VersionChoices[j + 1].Version + ":\r\n\r\n");

                    // append the changes to the total changes list
                    if (!ServerFile.CatchAllUpdateExists || ServerFile.CatchAllUpdateExists && j != ServerFile.VersionChoices.Count - 2)
                    {
                        if (ServerFile.VersionChoices[j].RTFChanges)
                            panelDisplaying.AppendRichText(ServerFile.VersionChoices[j].Changes);
                        else
                            panelDisplaying.AppendText(ServerFile.VersionChoices[j].Changes);
                    }
                }
            }
        }
    }
}