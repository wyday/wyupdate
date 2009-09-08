using System.Collections.Generic;
using System.IO;
using wyUpdate.Common;
using wyUpdate.Downloader;

namespace wyUpdate
{
    public partial class frmMain
    {
        void DownloadUpdate()
        {
            if (SelfUpdating)
            {
                //download self update
                update.CurrentlyUpdating = UpdateOn.DownloadingClientUpdt;
                BeginSelfUpdateDownload(updateFrom.FileSites, updateFrom.Adler32);
            }
            else
            {
                //download the update file
                update.CurrentlyUpdating = UpdateOn.DownloadingUpdate;
                BeginDownload(updateFrom.FileSites, updateFrom.Adler32, true);
            }
        }

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
            UpdateEngine clientSF = new UpdateEngine();

            LoadClientServerFile(clientSF);

            //check if the client is new enough.
            willSelfUpdate = VersionTools.Compare(VersionTools.FromExecutingAssembly(), clientSF.NewVersion) == -1;

            //Show update info page
            ShowFrame(Frame.UpdateInfo);
        }

        void LoadClientServerFile(UpdateEngine updateEngine)
        {
            //load the client server file
            if (updateEngine == null)
            {
                update.LoadServerDatav2(clientSFLoc);

                //get the current version of the Client
                string currentClientVersion = VersionTools.FromExecutingAssembly();

                foreach (VersionChoice vChoice in update.VersionChoices)
                {
                    // select the correct delta-patch version choice
                    // using fuzzy equality (i.e. 1.1 == 1.1.0.0)
                    if (VersionTools.Compare(vChoice.Version, currentClientVersion) == 0)
                    {
                        updateFrom = vChoice;
                        break;
                    }
                }

                //if no delta-patch update has been selected, use the catch-all update
                if (updateFrom == null)
                    updateFrom = update.VersionChoices[update.VersionChoices.Count - 1];
            }
            else
                updateEngine.LoadServerDatav2(clientSFLoc);
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
            update.LoadServerDatav2(serverFileLoc);

            clientLang.NewVersion = update.NewVersion;

            // if no update is needed...
            if (VersionTools.Compare(update.InstalledVersion, update.NewVersion) > -1)
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

            int i;

            for (i = 0; i < update.VersionChoices.Count; i++)
            {
                // select the correct delta-patch version choice
                if (VersionTools.Compare(update.VersionChoices[i].Version, update.InstalledVersion) == 0)
                {
                    updateFrom = update.VersionChoices[i];
                    break;
                }
            }


            //if no delta-patch update has been selected, use the catch-all update (if it exists)
            if (updateFrom == null && update.VersionChoices[update.VersionChoices.Count - 1].Version == update.NewVersion)
                updateFrom = update.VersionChoices[update.VersionChoices.Count - 1];

            if (updateFrom == null)
                throw new NoUpdatePathToNewestException();

            // set the changes text
            if (setChangesText || isAutoUpdateMode)
            {
                //if there's a catch-all update start with one less than "update.VersionChoices.Count - 1"

                bool catchAllExists = update.VersionChoices[update.VersionChoices.Count - 1].Version == update.NewVersion;


                //build the changes from all previous versions
                for (int j = update.VersionChoices.Count - 1; j >= i; j--)
                {
                    //show the version number for previous updates we may have missed
                    if (j != update.VersionChoices.Count - 1 && (!catchAllExists || catchAllExists && j != update.VersionChoices.Count - 2))
                        panelDisplaying.AppendAndBoldText("\r\n\r\n" + update.VersionChoices[j + 1].Version + ":\r\n\r\n");

                    // append the changes to the total changes list
                    if (!catchAllExists || catchAllExists && j != update.VersionChoices.Count - 2)
                    {
                        if (update.VersionChoices[j].RTFChanges)
                            panelDisplaying.AppendRichText(update.VersionChoices[j].Changes);
                        else
                            panelDisplaying.AppendText(update.VersionChoices[j].Changes);
                    }
                }
            }
        }
    }
}