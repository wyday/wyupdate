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
                List<string> overwriteServer = new List<string> { serverOverwrite };
                BeginDownload(overwriteServer, 0, false);
            }
            else
            {
                //download the server file
                BeginDownload(update.ServerFileSites, 0, false);
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
                BeginDownload(updateFrom.FileSites, updateFrom.Adler32, true);
            }
        }

        //download regular update files
        void BeginDownload(List<string> sites, long adler32, bool relativeProgress)
        {
            if (downloader != null)
            {
                downloader.ProgressChanged -= ShowProgress;
                downloader.ProgressChanged -= SelfUpdateProgress;
            }

            downloader = new FileDownloader(sites, tempDirectory)
                             {
                                 Adler32 = adler32,
                                 UseRelativeProgress = relativeProgress
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

            downloader = new FileDownloader(sites, tempDirectory)
                             {
                                 Adler32 = adler32
                             };

            downloader.ProgressChanged += SelfUpdateProgress;
            downloader.Download();
        }

        //client server file downloaded
        void DownloadClientSFSuccess()
        {
            //load the client server file, and see if a new version is availiable
            ServerFile clientSF = ServerFile.Load(clientSFLoc);

            //check if the client is new enough.
            if (VersionTools.Compare(VersionTools.FromExecutingAssembly(), clientSF.NewVersion) == -1)
            {
                SelfUpdateState = SelfUpdateState.WillUpdate;

                // autoupdate will need this SF
                if (isAutoUpdateMode)
                {
                    SelfServerFile = clientSF;
                    LoadClientServerFile();
                }
            }

            //Show update info page
            ShowFrame(Frame.UpdateInfo);
        }

        void LoadClientServerFile()
        {
            // load the self server file if it doesn't already exist
            if (SelfServerFile == null)
                SelfServerFile = ServerFile.Load(clientSFLoc);

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
                //TODO: create a new folder to store the downloaded & extracted folder
                try
                {
                    //TODO: delete existing update folders


                    // TODO: set the autoupdate filename
                    autoUpdateStateFile = Path.Combine(tempDirectory, "autoupdate");
                }
                catch { }
            }

            //download the client server file and see if the client is new enough
            BeginSelfUpdateDownload(update.ClientServerSites, 0);
        }

        //returns True if an update is necessary, otherwise false
        void LoadServerFile(bool setChangesText)
        {
            //load the server file
            ServerFile = ServerFile.Load(serverFileLoc);

            clientLang.NewVersion = ServerFile.NewVersion;

            // if no update is needed...
            if (VersionTools.Compare(update.InstalledVersion, ServerFile.NewVersion) > -1)
            {
                if (isAutoUpdateMode)
                {
                    // send reponse that there's no update available
                    updateHelper.SendSuccess(null, null, true, null);

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