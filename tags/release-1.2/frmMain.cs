using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.Threading;
using System.IO;
using System.Net;
using wyUpdate.Common;
using wyUpdate.Downloader;
using System.Diagnostics;
using System.Runtime.InteropServices; //launching elevated client

namespace wyUpdate
{
    public partial class frmMain : Form
    {
        #region Private variables

        UpdateEngine update = new UpdateEngine();
        VersionChoice updateFrom;

        UpdateDetails updtDetails;

        FileDownloader downloader;
        InstallUpdate installUpdate;

        readonly ClientLanguage clientLang = new ClientLanguage();

        int frameOn = 0;
        bool isCancelled = false;

        string error = "";

        //The full filename of the update & servers files 
        string updateFilename = "";
        string serverFileLoc = "";

        //client file location
        string clientFileLoc;

        //the base directory (same path as the executable, unless specified)
        string baseDirectory;
        //the extract directory
        string tempDirectory;

        readonly PanelDisplay panelDisplaying = new PanelDisplay(500, 320);

        //does the client need elevation?
        bool needElevation = false;
        bool willSelfUpdate = false;

        //--Uninstalling
        bool uninstalling = false;

        //--Silent updating/uninstalling
        bool isSilent = false;
        int returnCode = 0;
        public int ReturnCode
        {
            get { return returnCode; }
            set { returnCode = value; }
        }

        // Wait Mode (aka API mode)
        UpdateHelper updateHelper;
        System.Windows.Forms.Timer sendGotPreInstallInfo;
        bool isWaitMode = false;
        bool dontDestroyTempFolder = false; //custom temp directory to store downloaded updates

        //-- Self update
        bool selfUpdating = false;
        string selfUpdateFileLoc = null;
        string oldClientLocation = null;
        string clientSFLoc;

        //used after a self update or elevation
        bool continuingUpdate = false; 

        //Pre-RC2 compatability:
        ClientFileType clientFileType;

        bool selfUpdateFromRC1 = false;
        string newClientLocation; //self update from RC1


        #region Threads
        delegate void ShowProgressDelegate(int percentDone, bool statusDone, string extraStatus, Exception ex);
        delegate void UninstallProgressDel(int percentDone, int stepOn, string extraStatus, Exception ex);
        delegate void CheckProcessesDel(FileInfo[] files, bool statusDone);
        #endregion

        #endregion Private variables

        public frmMain(string[] args)
        {
            //sets to SegoeUI on Vista
            Font = SystemFonts.MessageBoxFont;

            InitializeComponent();

            //enable Lazy SSL for all downloads
            FileDownloader.EnableLazySSL();

            //resize the client so its client region = 500x360
            if (ClientRectangle.Width != 500)
                Width = (Width - ClientRectangle.Width) + 500;

            if (ClientRectangle.Height != 360)
                Height = (Height - ClientRectangle.Height) + 360;

            //add the panelDisplaying to form
            panelDisplaying.TabIndex = 0;
            Controls.Add(panelDisplaying);

            //process commandline argument
            Arguments commands = new Arguments(args);
            ProcessArguments(commands);

            try
            {
                //load the self update information
                if (!string.IsNullOrEmpty(selfUpdateFileLoc))
                {
                    LoadSelfUpdateData(selfUpdateFileLoc);

                    //if the loaded file is from RC1, then update self and bail out
                    if (selfUpdateFromRC1)
                    {
                        //install the new client, and relaunch it to continue the update
                        if (needElevation && !IsElevated())
                        {
                            //the user "elevated" as a non-admin user
                            //warn the user of their idiocy
                            error = clientLang.AdminError;

                            //set to false so new client won't be launched in frmMain_Load()
                            selfUpdateFromRC1 = false;
                            
                            ShowFrame(-1);
                        }
                        else
                        {
                            needElevation = false;

                            //Install the new client
                            File.Copy(newClientLocation, oldClientLocation, true);

                            //Relaunch self in OnLoad()
                        }

                        //bail out
                        return;
                    }
                }

                //Load the client information
                if (clientFileType == ClientFileType.PreRC2)
                    //TODO: stop supporting old client files after 1.0 Final.
                    update.OpenObsoleteClientFile(clientFileLoc);
                else
                    update.OpenClientFile(clientFileLoc, clientLang);

                clientLang.SetVariables(update.ProductName, update.InstalledVersion);
            }
            catch (Exception ex)
            {
                clientLang.SetVariables(update.ProductName, update.InstalledVersion);

                error = ex.Message;
                ShowFrame(-1);
                return;
            }

            //sets up Next & Cancel buttons
            SetButtonText();

            //set header alignment, etc.
            panelDisplaying.HeaderImageAlign = update.HeaderImageAlign;

            if (update.HeaderTextIndent >= 0)
                panelDisplaying.HeaderIndent = update.HeaderTextIndent;

            panelDisplaying.HideHeaderDivider = update.HideHeaderDivider;

            try
            {
                if (!string.IsNullOrEmpty(update.HeaderTextColorName))
                    panelDisplaying.HeaderTextColor = Color.FromName(update.HeaderTextColorName);
            }
            catch (Exception) { }

            //load the Side/Top images
            panelDisplaying.TopImage = update.TopImage;
            panelDisplaying.SideImage = update.SideImage;

            if (selfUpdating)
            {
                try
                {
                    //load the self-update server file
                    LoadClientServerFile(null);
                    clientLang.NewVersion = update.NewVersion;
                }
                catch (Exception ex)
                {
                    error = clientLang.ServerError + "\n\n" + ex.Message;
                    ShowFrame(-1);
                    return;
                }

                if (needElevation && !IsElevated())
                {
                    //the user "elevated" as a non-admin user
                    //warn the user of their idiocy
                    error = clientLang.AdminError;

                    ShowFrame(-1);
                }
                else
                {
                    needElevation = false;

                    //begin updating the product
                    ShowFrame(3);
                }
            }
            else if (continuingUpdate) //continuing from elevation or self update (or both)
            {
                try
                {
                    //load the server file (without filling the 'changes' box)
                    LoadServerFile(false);
                }
                catch (Exception ex)
                {
                    error = clientLang.ServerError + "\n\n" + ex.Message;
                    ShowFrame(-1);
                    return;
                }

                if (needElevation && !IsElevated())
                {
                    //the user "elevated" as a non-admin user
                    //warn the user of their idiocy
                    error = clientLang.AdminError;

                    ShowFrame(-1);
                }
                else
                {
                    needElevation = false;

                    //begin updating the product
                    ShowFrame(3);
                }
            }
            else if (uninstalling)
            {
                //do nothing here
            }
            else //no self update nor elevation, just run normally
            {
                //begin check for updates
                ShowFrame(1);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            if (uninstalling)
            {
                ShowFrame(7);
            }
            else if (selfUpdateFromRC1)
            {
                //if the loaded file is from RC1, then update self and bail out

                //Relaunch self
                StartSelfElevated();
            }

            base.OnLoad(e);
        }

        private void ProcessArguments(Arguments commands)
        {
            if (commands["supdf"] != null)
            {
                //the client is in self update mode
                selfUpdateFileLoc = commands["supdf"];
            }
            else
            {
                //client data file
                if (commands["cdata"] != null)
                {
                    clientFileLoc = commands["cdata"];

                    if (clientFileLoc.EndsWith("iuc", StringComparison.InvariantCultureIgnoreCase))
                        clientFileType = ClientFileType.PreRC2;
                    else if (clientFileLoc.EndsWith("iucz", StringComparison.InvariantCultureIgnoreCase))
                        clientFileType = ClientFileType.RC2;
                    else
                        clientFileType = ClientFileType.Final;
                }
                else
                {
                    clientFileLoc = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client.wyc");
                    clientFileType = ClientFileType.Final;

                    //try the RC-2 filename
                    if (!File.Exists(clientFileLoc))
                    {
                        clientFileLoc = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "iuclient.iucz");
                        clientFileType = ClientFileType.RC2;
                    }
                    
                    //try Pre-RC2 filename
                    if (!File.Exists(clientFileLoc))
                    {
                        //if it doesn't exist, try without the 'z'
                        clientFileLoc = clientFileLoc.Substring(0, clientFileLoc.Length - 1);
                        clientFileType = ClientFileType.PreRC2;
                    }
                }

                //set basedirectory as the location of the executable
                baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                if (commands["basedir"] != null)
                {
                    //if the specified directory exists, then set as directory
                    if (Directory.Exists(commands["basedir"]))
                    {
                        baseDirectory = commands["basedir"];
                    }
                }

                if (commands["tempdir"] != null && Directory.Exists(commands["tempdir"]))
                {
                    //set the temp directory
                    tempDirectory = commands["tempdir"];
                }
                else //if the tempDir hasn't been created
                {
                    //create my own "random" temp dir.
                    tempDirectory = Path.Combine(Path.GetTempPath(), @"wyup" + DateTime.Now.ToString("ddMMssfff"));
                    Directory.CreateDirectory(tempDirectory);
                }

                //uninstall any newly created folders, files, or registry
                if (commands["uninstall"] != null)
                    uninstalling = true;

                /*
                //TODO: implement silent API (NOT WORKING YET -- DON'T USE)
                if (commands["wait"] != null)
                {
                    updateHelper = new UpdateHelper(this.Handle);
                    updateHelper.SenderProcessClosed += new EventHandler(UpdateHelper_SenderProcessClosed);
                    updateHelper.RequestReceived += new RequestHandler(UpdateHelper_RequestReceived);

                    sendGotPreInstallInfo = new System.Windows.Forms.Timer();

                    sendGotPreInstallInfo.Enabled = false;
                    sendGotPreInstallInfo.Interval = 1;
                    sendGotPreInstallInfo.Tick += new EventHandler(sendGotPreInstallInfo_Tick);

                    isWaitMode = true;

                    if (commands["tempdir"] != null)
                        dontDestroyTempFolder = true;
                }
                */

                //only allow silent uninstalls 
                //TODO: allow silent checking and updating
                if (uninstalling && commands["s"] != null)
                {
                    isSilent = true;

                    WindowState = FormWindowState.Minimized;
                    ShowInTaskbar = false;
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            //only warn if after the welcome page
            //and not self updating/elevating
            if (needElevation || willSelfUpdate || selfUpdating || isSilent ||
                isCancelled || panelDisplaying.TypeofFrame == FrameType.WelcomeFinish)
            {
                //close the form
                e.Cancel = false;
            }
            else //currently updating
            {
                //stop closing
                e.Cancel = true;

                //prompt the user if they really want to cancel
                CancelUpdate();
            }

            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            //if not self updating, then delete temp files.
            if (!(needElevation || willSelfUpdate || selfUpdating || dontDestroyTempFolder))
            {
                //if the temp directory exists, remove it
                if (Directory.Exists(tempDirectory))
                {
                    try
                    {
                        Directory.Delete(tempDirectory, true);
                    }
                    catch (Exception) { }
                }
            }

            base.OnClosed(e);
        }

        #region Async: Downloading, updating, and checking processes

        // update the label & progress bar when downloading/updating
        private void ShowProgress(int percentDone, bool done, string extraStatus, Exception ex)
        {
            //update progress bar
            if (percentDone >= 0 && percentDone <= 100)
                panelDisplaying.Progress = percentDone;

            //update bottom status
            if (extraStatus != panelDisplaying.ProgressStatus && extraStatus != "")
                panelDisplaying.ProgressStatus = extraStatus;

            if (done && ex == null)
            {
                if (isCancelled)
                    Close(); //close the form
                else if (frameOn == 1)
                {
                    //set the serverfile location
                    serverFileLoc = downloader.DownloadingTo;
                    try
                    {
                        //load the server file into memory
                        LoadServerFile(true);
                    }
                    catch (NoUpdatePathToNewestException)
                    {
                        //there is no update path to the newest version
                        ShowFrame(6);
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
                if (frameOn == 1)
                {
                    error = clientLang.ServerError + "\n\n" + ex.Message;
                }
                else
                {
                    if (update.CurrentlyUpdating == UpdateOn.DownloadingUpdate)
                    {
                        //a download error occurred
                        error = clientLang.DownloadError + "\n\n" + ex.Message;
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
                            ShowFrame(3);

                            return;
                        }

                        error = clientLang.GeneralUpdateError + "\n\n" + ex.Message;
                    }
                }

                ShowFrame(-1);
            }
        }

        private void SelfUpdateProgress(int percentDone, bool done, string extraStatus, Exception ex)
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
                else if (frameOn == 1)
                {
                    clientSFLoc = downloader.DownloadingTo;

                    //client server file downloaded sucessfully
                    DownloadClientSFSuccess();
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
            else if (ex != null)
            {
                //if a new client is *required* to install the update...
                if (UpdateEngine.UpdateNeccessary(UpdateEngine.GetFullVersion(System.Reflection.Assembly.GetExecutingAssembly().Location), update.MinClientVersion))
                {
                    //show an error and bail out
                    error = clientLang.SelfUpdateInstallError + "\n\n" + ex.Message;
                    ShowFrame(-1);
                }
                else //self update isn't necessary, so handle gracefully
                {
                    if (frameOn == 1)
                    {
                        //client server file failed to download, continue as usual:

                        willSelfUpdate = false;

                        //Show update info page
                        ShowFrame(2);
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
                            ShowFrame(3);

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
                //Show the error (rollback has ocurred)
                error = ex.Message;
                ShowFrame(-1);
            }
        }

        private void CheckProcess(FileInfo[] files, bool done)
        {
            if (done)
            {
                if (files != null)//if there are some files needing closing
                {
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

        #endregion Async: Downloading, updating, and checking processes

        #region Downloading methods (not async)

        //downlaod regular update files
        private void BeginDownload(List<string> sites, long adler32, bool relativeProgress)
        {
            ShowProgressDelegate showProgressDel = ShowProgress;

            downloader = new FileDownloader(sites, tempDirectory, this, showProgressDel)
                             {
                                 Adler32 = adler32,
                                 UseRelativeProgress = relativeProgress
                             };

            Thread t = new Thread(downloader.Download)
                           {
                               IsBackground = true
                           };
            t.Start();
        }

        //download self update files (server file or update file)
        private void BeginSelfUpdateDownload(List<string> sites, long adler32)
        {
            //not the different delegate
            ShowProgressDelegate showProgressDel = SelfUpdateProgress;

            downloader = new FileDownloader(sites, tempDirectory, this, showProgressDel)
                             {
                                 Adler32 = adler32
                             };

            Thread t = new Thread(downloader.Download)
                           {
                               IsBackground = true
                           };
            t.Start();
        }

        //client server file downloaded
        private void DownloadClientSFSuccess()
        {
            //load the client server file, and see if a new version is availiable
            UpdateEngine clientSF = new UpdateEngine();

            LoadClientServerFile(clientSF);

            //check if the client is new enough.
            willSelfUpdate = UpdateEngine.UpdateNeccessary(UpdateEngine.GetFullVersion(System.Reflection.Assembly.GetExecutingAssembly().Location), clientSF.NewVersion);

            //Show update info page
            ShowFrame(2);
        }

        private void LoadClientServerFile(UpdateEngine updateEngine)
        {
            //load the client server file
            if (updateEngine == null)
            {
                update.LoadServerDatav2(clientSFLoc);

                //get the current version of the Client
                string currentClientVersion = UpdateEngine.GetFullVersion(System.Reflection.Assembly.GetExecutingAssembly().Location);

                foreach (VersionChoice vChoice in update.VersionChoices)
                {
                    // select the correct delta-patch version choice
                    // using fuzzy equality (i.e. 1.1 == 1.1.0.0)
                    if (UpdateEngine.VersionCompare(vChoice.Version, currentClientVersion) == 0)
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

        //returns True if an update is necessary, otherwise false
        private void LoadServerFile(bool setChangesText)
        {
            //load the server file
            update.LoadServerDatav2(serverFileLoc);

            clientLang.NewVersion = update.NewVersion;

            if (!UpdateEngine.UpdateNeccessary(update.InstalledVersion, update.NewVersion))
            {
                if (isWaitMode)
                {
                    // send reponse that there's no update available
                    updateHelper.SendSuccess();

                    // close this client
                    isCancelled = true;
                    Close();

                    return;
                }
                else
                {
                    // Show "All Finished" page
                    ShowFrame(5);
                    return;
                }
            }

            int i;

            for (i = 0; i < update.VersionChoices.Count; i++)
            {
                // select the correct delta-patch version choice
                if (UpdateEngine.VersionCompare(update.VersionChoices[i].Version, update.InstalledVersion) == 0)
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

            if (setChangesText)
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

                //download the client server file and see if the client is new enough
                BeginSelfUpdateDownload(update.ClientServerSites, 0);
            }
        }

        #endregion End of Downloading methods (not async)

        #region Updating methods (not async)

        private void ShowFrame(int frameNum)
        {
            switch (frameNum)
            {
                case 1: //Update checking screen
                    frameOn = 1;

                    panelDisplaying.ChangePanel(FrameType.Update,
                        clientLang.Checking.Title,
                        clientLang.Checking.SubTitle,
                        clientLang.Checking.Content,
                        "");

                    btnNext.Enabled = false;

                    if (!isWaitMode)
                    {
                        //download the server file
                        BeginDownload(update.ServerFileSites, 0, false);
                    }

                    break;
                case 2: //Update Info Screen
                    frameOn = 2;

                    panelDisplaying.ChangePanel(FrameType.TextInfo,
                        clientLang.UpdateInfo.Title,
                        clientLang.UpdateInfo.SubTitle,
                        clientLang.UpdateInfo.Content,
                        clientLang.UpdateBottom);

                    //check if elevation is needed
                    needElevation = !IsElevated();

                    btnNext.Enabled = true;
                    btnNext.Text = clientLang.UpdateButton;

                    break;
                case 3: //Download and Install Updates
                    frameOn = 3;

                    panelDisplaying.ShowChecklist = true;

                    panelDisplaying.ChangePanel(FrameType.Update,
                        clientLang.DownInstall.Title,
                        clientLang.DownInstall.SubTitle,
                        clientLang.DownInstall.Content,
                        "");

                    if (selfUpdating)
                    {
                        //show status for downloading self
                        SetStepStatus(0, clientLang.DownloadingSelfUpdate);
                    }
                    else
                    {
                        //show status for the downloading update
                        SetStepStatus(0, clientLang.Download);
                    }

                    btnNext.Enabled = false;

                    if (selfUpdating)
                    {
                        //download self update
                        update.CurrentlyUpdating = UpdateOn.DownloadingClientUpdt;
                        BeginSelfUpdateDownload(updateFrom.FileSites, updateFrom.Adler32);
                    }
                    else
                    {
                        //download the update file
                        BeginDownload(updateFrom.FileSites, updateFrom.Adler32, true);
                    }
                    break;
                case 4: //Display Congrats Window
                    frameOn = 4;

                    panelDisplaying.ChangePanel(FrameType.WelcomeFinish,
                        clientLang.SuccessUpdate.Title,
                        clientLang.SuccessUpdate.Content,
                        "",
                        clientLang.FinishBottom);

                    btnNext.Enabled = true;
                    btnCancel.Visible = false;
                    btnNext.Text = clientLang.FinishButton;

                    break;
                case 5: //Your Product is already up to date screen
                    frameOn = 5;

                    panelDisplaying.ChangePanel(FrameType.WelcomeFinish,
                        clientLang.AlreadyLatest.Title,
                        clientLang.AlreadyLatest.Content,
                        "",
                        clientLang.FinishBottom);

                    btnNext.Enabled = true;
                    btnCancel.Visible = false;
                    btnNext.Text = clientLang.FinishButton;

                    break;
                case 6: //No update to the latest version is available
                    frameOn = 6;

                    if (!string.IsNullOrEmpty(update.NoUpdateToLatestLinkText))
                        panelDisplaying.SetNoUpdateAvailableLink(update.NoUpdateToLatestLinkText, update.NoUpdateToLatestLinkURL);

                    panelDisplaying.ChangePanel(FrameType.WelcomeFinish,
                        clientLang.NoUpdateToLatest.Title,
                        clientLang.NoUpdateToLatest.Content,
                        "",
                        clientLang.FinishBottom);

                    btnNext.Enabled = true;
                    btnCancel.Visible = false;
                    btnNext.Text = clientLang.FinishButton;

                    break;
                case 7: //Uninstall screen
                    frameOn = 7;

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
                case -1: //Display error screen
                    
                    //TODO: make the return codes error specific
                    returnCode = 1;

                    frameOn = -1;

                    panelDisplaying.ChangePanel(FrameType.WelcomeFinish,
                        clientLang.UpdateError.Title,
                        error,
                        "",
                        clientLang.FinishBottom);

                    btnNext.Enabled = true;
                    btnCancel.Visible = false;
                    btnNext.Text = clientLang.FinishButton;

                    break;
            }

            //enable the close button on the Finish/Error screens
            if (frameNum == 4 || frameNum == 5 || frameNum == 6 || frameNum == -1)
            {
                // allow the user to forcefuly exit
                BlockLogOff(false);

                EnableCancel();
                
                //allow the user to exit by pressing ESC
                CancelButton = btnNext;
            }

            //if silent & if on one of the user interaction screens, then click next
            if (isSilent && (frameOn == 0 || frameOn == 2 || frameOn == 4 || frameOn == 5 || frameOn == 6 || frameOn == -1))
            {
                btnNext_Click(null, new EventArgs());
            }

            //so the user doesn't accidentally cancel update.
            btnNext.Focus();
        }

        private void InstallUpdates(UpdateOn CurrentlyUpdating)
        {
            ShowProgressDelegate showProgress = ShowProgress;

            Thread asyncThread = null;

            switch (CurrentlyUpdating)
            {
                case UpdateOn.SelfUpdating:
                    SetStepStatus(1, clientLang.SelfUpdate);

                    showProgress = SelfUpdateProgress;

                    installUpdate = new InstallUpdate(Path.Combine(tempDirectory, updateFilename),
                        tempDirectory, this, showProgress)
                                        {
                                            OldIUPClientLoc = oldClientLocation
                                        };

                    //location of old "self" to replace

                    asyncThread = new Thread(installUpdate.RunSelfUpdate);
                    break;
                case UpdateOn.Extracting:
                    SetStepStatus(1, clientLang.Extract);

                    installUpdate = new InstallUpdate(tempDirectory, baseDirectory, showProgress, this)
                                        {
                                            Filename = Path.Combine(tempDirectory, updateFilename),
                                            OutputDirectory = tempDirectory
                                        };

                    asyncThread = new Thread(installUpdate.RunUnzipProcess);
                    break;
                case UpdateOn.ClosingProcesses:
                    SetStepStatus(1, clientLang.Processes);

                    installUpdate = new InstallUpdate(tempDirectory, baseDirectory, 
                        new CheckProcessesDel(CheckProcess), this);

                    asyncThread = new Thread(installUpdate.RunProcessesCheck);
                    break;
                case UpdateOn.PreExecute:

                    // try to stop the user from forcefuly exiting
                    BlockLogOff(true);

                    SetStepStatus(1, clientLang.PreExec);

                    installUpdate = new InstallUpdate(tempDirectory, baseDirectory, showProgress, this)
                                        {
                                            UpdtDetails = updtDetails
                                        };

                    asyncThread = new Thread(installUpdate.RunPreExecute);
                    break;
                case UpdateOn.BackingUp:
                    SetStepStatus(1, clientLang.Files);

                    installUpdate = new InstallUpdate(tempDirectory, baseDirectory, showProgress, this)
                                        {
                                            UpdtDetails = updtDetails
                                        };

                    asyncThread = new Thread(installUpdate.RunUpdateFiles);
                    break;
                case UpdateOn.ModifyReg:
                    SetStepStatus(2, clientLang.Registry);

                    installUpdate = new InstallUpdate(tempDirectory, baseDirectory, showProgress, this)
                                        {
                                            UpdtDetails = updtDetails
                                        };

                    asyncThread = new Thread(installUpdate.RunUpdateRegistry);
                    break;
                case UpdateOn.OptimizeExecute:
                    SetStepStatus(3, clientLang.Optimize);

                    installUpdate = new InstallUpdate(tempDirectory, baseDirectory, showProgress, this)
                                        {
                                            UpdtDetails = updtDetails
                                        };

                    asyncThread = new Thread(installUpdate.RunOptimizeExecute);
                    break;
                case UpdateOn.WriteClientFile:
                    //don't show any status, but begin the thread to being updating the client file
                    
                    //Save the client file with the new version
                    update.InstalledVersion = update.NewVersion;

                    installUpdate = new InstallUpdate(tempDirectory, baseDirectory, showProgress, this)
                                        {
                                            Filename = clientFileLoc,
                                            UpdtDetails = updtDetails,
                                            ClientFileType = clientFileType,
                                            ClientFile = update,
                                            SkipProgressReporting = true
                                        };

                    asyncThread = new Thread(installUpdate.RunUpdateClientDataFile);
                    break;
                case UpdateOn.DeletingTemp:
                    SetStepStatus(3, clientLang.TempFiles);

                    installUpdate = new InstallUpdate(tempDirectory, clientFileLoc, showProgress, this);

                    asyncThread = new Thread(installUpdate.RunDeleteTemporary);
                    break;
                case UpdateOn.Uninstalling:
                    //need to pass: client data file loc, delegate, this
                    installUpdate = new InstallUpdate(clientFileLoc, new UninstallProgressDel(UninstallProgress), this);

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

        private void SetStepStatus(int stepNum, string stepText)
        {
            panelDisplaying.ProgressStatus = "";
            panelDisplaying.UpdateItems[stepNum].Status = UpdateItemStatus.Working;
            panelDisplaying.UpdateItems[stepNum].Text = stepText;
        }

        //update step completed, continue to next
        private void StepCompleted()
        {
            if (frameOn == 3) // update screen
            {
                if (update.CurrentlyUpdating == UpdateOn.DeletingTemp)
                {
                    //successfully deleted temporary files
                    panelDisplaying.UpdateItems[3].Status = UpdateItemStatus.Success;

                    //Successfully Updated
                    ShowFrame(4);

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
                        case UpdateOn.BackingUp: //done backing up & installing files
                            panelDisplaying.UpdateItems[1].Status = UpdateItemStatus.Success;
                            break;
                        case UpdateOn.ModifyReg: //done modifying registry
                            panelDisplaying.UpdateItems[2].Status = UpdateItemStatus.Success;
                            break;
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

        #endregion Updating methods (not async)

        #region UpdateHelper functions (API)

        void UpdateHelper_RequestReceived(object sender, UpdateStep e)
        {
            switch (e)
            {
                case UpdateStep.CheckForUpdate:

                    //download the server file
                    BeginDownload(update.ServerFileSites, 0, false);

                    break;
                case UpdateStep.DownloadUpdate:

                    ShowFrame(3);

                    break;
                case UpdateStep.BeginExtraction:

                    update.CurrentlyUpdating = UpdateOn.Extracting;
                    InstallUpdates(update.CurrentlyUpdating);

                    break;
                case UpdateStep.PreInstallInfo:

                    //TODO: make a note of the pre-install info


                    // send a success signal.
                    sendGotPreInstallInfo.Start();


                    break;
                case UpdateStep.Install:

                    this.TopMost = true;

                    update.CurrentlyUpdating = UpdateOn.ClosingProcesses;
                    InstallUpdates(update.CurrentlyUpdating);

                    this.TopMost = false;

                    break;
            }
        }

        void UpdateHelper_SenderProcessClosed(object sender, EventArgs e)
        {
            //TODO: cleanup what currently doing (cancel any process)


            // exit the client
            if (!updateHelper.PreInstallInfoSent)
                Close();
        }

        void sendGotPreInstallInfo_Tick(object sender, EventArgs e)
        {
            sendGotPreInstallInfo.Enabled = false;

            updateHelper.SendSuccess();
        }

        #endregion UpdateHelper functions (API)


        #region Next, Back, Cancel, Options

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (frameOn == 4 || frameOn == 5 || frameOn == 6 || frameOn == -1)
            {
                Close();
            }
            else
            {
                if (needElevation || willSelfUpdate)
                    StartSelfElevated();
                else
                    ShowFrame(frameOn + 1);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            CancelUpdate();
        }

        
        private void CancelUpdate(bool ForceClose)
        {
            if ((frameOn == 1 || frameOn == 3) && !ForceClose) //if downloading or updating
            {
                DialogResult dResult = MessageBox.Show(clientLang.CancelDialog.Content, clientLang.CancelDialog.Title, 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);

                if (dResult == DialogResult.Yes)
                {
                    //cancel the update
                    isCancelled = true;
                    if (IsDownloading())
                    {
                        downloader.Cancel(); //cancel any downloads

                        //TODO: I should give the 'downloader' a bit of time to clean up partial files

                        //Bail out quickly. Don't hang around for servers to lazily respond.
                        isCancelled = true;
                        this.Close();
                        return;
                    }
                    else if (frameOn == 3 && !IsDownloading())
                        installUpdate.Cancel(); //cancel updates

                    //disable the 'X' button & cancel button
                    DisableCancel();
                } //otherwise, do nothing
            }
            else
            {
                //either force closed, or not download/updating
                isCancelled = true;
                Close();
            }
        }

        private bool IsDownloading()
        {
            //if downloading in anything, return true
            if (frameOn == 1 || frameOn == 3 && downloader != null && 
                (update.CurrentlyUpdating == UpdateOn.DownloadingUpdate 
                    || update.CurrentlyUpdating == UpdateOn.DownloadingClientUpdt))
                return true;
            else
                return false;
        }


        private void CancelUpdate()
        {
            CancelUpdate(false);
        }

        private void DisableCancel()
        {
            if (btnCancel.Enabled)
                SystemMenu.DisableCloseButton(this);

            btnCancel.Enabled = false;
        }

        private void EnableCancel()
        {
            if (!btnCancel.Enabled)
                SystemMenu.EnableCloseButton(this);

            btnCancel.Enabled = true;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            // handle the effect of minimize/restore on 
            // disabling the "close" button & menu item
            if (!btnCancel.Enabled)
                SystemMenu.DisableCloseButton(this);

            base.OnSizeChanged(e);
        }

        private void SetButtonText()
        {
            btnNext.Text = clientLang.NextButton;
            btnCancel.Text = clientLang.CancelButton;
        }

        private void btnCancel_SizeChanged(object sender, EventArgs e)
        {
            btnNext.Left = btnCancel.Left - btnNext.Width - 6;
        }

        #endregion

        #region Self Update

        private void SaveSelfUpdateData(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

            // Write any file-identification data you want to here
            fs.Write(System.Text.Encoding.UTF8.GetBytes("IUSUFV2"), 0, 7);

            //Client data file location
            WriteFiles.WriteString(fs, 0x01, clientFileLoc);

            //Server data file location
            WriteFiles.WriteString(fs, 0x02, serverFileLoc);

            //Client server file
            WriteFiles.WriteString(fs, 0x03, clientSFLoc);

            //Base Directory
            WriteFiles.WriteString(fs, 0x04, baseDirectory);

            //Temporary directory
            WriteFiles.WriteString(fs, 0x05, tempDirectory);

            //Old client file location (self)
            WriteFiles.WriteString(fs, 0x06, System.Reflection.Assembly.GetExecutingAssembly().Location);

            //self update needed
            WriteFiles.WriteBool(fs, 0x07, willSelfUpdate);

            //check if the new client really has been elevated
            WriteFiles.WriteBool(fs, 0x08, needElevation);

            fs.WriteByte(0xFF);
            fs.Close();
        }

        private void LoadSelfUpdateData(string fileName)
        {
            byte[] fileIDBytes = new byte[7];
            string fileID = "";

            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            // Read back the file identification data, if any
            fs.Read(fileIDBytes, 0, 7);
            fileID = System.Text.Encoding.UTF8.GetString(fileIDBytes);
            if (fileID != "IUSUFV2")
            {
                //handle self update from RC1 client
                if (fileID == "IUSUFV1")
                {
                    LoadSelfUpdateRC1Data(fs);
                    return;
                }

                //free up the file so it can be deleted
                fs.Close();
                throw new Exception("Self update fileID is wrong: " + fileID);
            }

            byte bType = (byte)fs.ReadByte();
            while (!ReadFiles.ReachedEndByte(fs, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x01://Read Client data file location
                        clientFileLoc = ReadFiles.ReadString(fs);

                        //TODO: Remove this hackish behavior to cope with pre-RC2 client data files
                        if (clientFileLoc.EndsWith("iuc", StringComparison.InvariantCultureIgnoreCase))
                            clientFileType = ClientFileType.PreRC2;
                        else if (clientFileLoc.EndsWith("iucz", StringComparison.InvariantCultureIgnoreCase))
                            clientFileType = ClientFileType.RC2;
                        else
                            clientFileType = ClientFileType.Final;

                        break;
                    case 0x02: //Read Server data file location
                        serverFileLoc = ReadFiles.ReadString(fs);
                        break;
                    case 0x03: //Client server file location
                        clientSFLoc = ReadFiles.ReadString(fs);
                        break;
                    case 0x04://Read Base Directory
                        baseDirectory = ReadFiles.ReadString(fs);
                        break;
                    case 0x05://Read Temporary directory
                        tempDirectory = ReadFiles.ReadString(fs);
                        break;
                    case 0x06://Read Old client file location
                        oldClientLocation = ReadFiles.ReadString(fs);
                        break;
                    case 0x07: //true=Self Update, false=Continue update
                       
                        if (ReadFiles.ReadBool(fs))
                            selfUpdating = true;
                        else
                            continuingUpdate = true;

                        break;
                    case 0x08: //is elevation required
                        needElevation = ReadFiles.ReadBool(fs);
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            fs.Close();
        }

        //Backwards compatability with 1.0 RC1
        private void LoadSelfUpdateRC1Data(Stream fs)
        {
            selfUpdateFromRC1 = true;

            //RC1 means it's guaranteed to be old-style client data file
            clientFileType = ClientFileType.PreRC2;

            byte bType = (byte)fs.ReadByte();

            while (!ReadFiles.ReachedEndByte(fs, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x01://Read Client data file location
                        clientFileLoc = ReadFiles.ReadString(fs);
                        break;
                    case 0x02: //Read Server data file location
                        serverFileLoc = ReadFiles.ReadString(fs);
                        break;
                    case 0x03://Read Base Directory
                        baseDirectory = ReadFiles.ReadString(fs);
                        break;
                    case 0x04://Read Temporary directory
                        tempDirectory = ReadFiles.ReadString(fs);
                        break;
                    case 0x05://Read Old client file location
                        oldClientLocation = ReadFiles.ReadString(fs);
                        break;
                    case 0x06://Read New client file location
                        newClientLocation = ReadFiles.ReadString(fs);
                        break;
                    case 0x07:
                        selfUpdating = ReadFiles.ReadBool(fs);
                        break;
                    case 0x08:
                        needElevation = ReadFiles.ReadBool(fs);
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            fs.Close();
        }

        #endregion Self Update

        #region User Elevation

        private void StartSelfElevated()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.ErrorDialog = true;
            psi.ErrorDialogParentHandle = this.Handle;

            if (willSelfUpdate)
            {
                //create the filename for the newly copied client
                psi.FileName = Path.Combine(tempDirectory, 
                        Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location));

                //copy self to the temp folder
                File.Copy(System.Reflection.Assembly.GetExecutingAssembly().Location, psi.FileName, true);
            }
            else if (selfUpdating)
            {
                //launch the newly updated self
                psi.FileName = oldClientLocation;
            }
            else
                psi.FileName = System.Reflection.Assembly.GetExecutingAssembly().Location;

            if (needElevation)
                psi.Verb = "runas"; //elevate to administrator

            try
            {
                //write necessary info (base/temp dirs, new client files, etc.) to a file
                SaveSelfUpdateData(Path.Combine(tempDirectory, "selfUpdate.sup"));

                psi.Arguments = "-supdf:\"" + Path.Combine(tempDirectory, "selfUpdate.sup") + "\"";

                Process p = Process.Start(psi);
                this.Close();
            }
            catch (Exception ex)
            {
                //the process couldn't be started, throw an error  
                //Note: this error even occurs when the administrator is using
                // a blank password
                //Note2: Can't run as a Guest account
                error = clientLang.AdminError + "\n\n" + ex.Message;
                ShowFrame(-1);
            }
        }

        private bool IsElevated()
        {
            //no elevation necessary if it's not overwriting important files
            if (updateFrom.InstallingTo == 0 && updateFrom.RegChanges.Count == 0)
                return true;

            try
            {
                //Windows Vista
                if (VistaTools.IsVista())
                {

                    if (VistaTools.IsElevated() ||
                        VistaTools.GetElevationType() == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull)
                        return true;

                    // not already elevated,

                    // if only updating local user files, no elevation is needed
                    if (OnlyUpdatingLocalUser())
                        return true;


                    //set Vista UAC Shield on next button
                    VistaTools.SetButtonShield(btnNext, true);
                    return false;
                }

                //Win 2000, XP, 2003
                if (Environment.OSVersion.Version.Major == 5)
                {
                    // see if we can write files & registry
                    return CanManipFiles() && CanManipReg();
                }

                //windows 98, no elevation needed
                return true;
            }
            catch (Exception)
            {
                // assume error was due to limited access
                return false;
            }
        }

        private bool OnlyUpdatingLocalUser()
        {
            //Vista only check when the client isn't already 
            // running with Admin (and elevated) priviledges

            //Elevation is needed...

            //if updating any registry other than HKEY_CURRENT_USER
            foreach (RegChange reg in updateFrom.RegChanges)
                if (reg.RegBasekey != RegBasekeys.HKEY_CURRENT_USER) return false;

            //if installing to the system folder or one of the common folders
            if (updateFrom.InstallingTo != 0 && (updateFrom.InstallingTo & InstallingTo.BaseDir) == 0)
                return false;

            string userProfileFolder = Environment.GetEnvironmentVariable("userprofile");

            //if the basedirectory isn't in the userprofile folder (C:\Users\UserName)
            if ((updateFrom.InstallingTo & InstallingTo.BaseDir) != 0 && !IsDirInDir(userProfileFolder, baseDirectory))
                return false;

            //if the client data file isn't in the userprofile folder
            if (!IsFileInDirectory(userProfileFolder, clientFileLoc))
                return false;

            //when self-updating, if this client is'nt in the userprofile folder
            if ((willSelfUpdate || selfUpdating) && !IsFileInDirectory(userProfileFolder, System.Reflection.Assembly.GetExecutingAssembly().Location))
                return false;

            //it's not changing anything outside the user profile folder
            return true;
        }


        private bool CanManipReg()
        {
            //test the reg operations
            List<RegChange> rollback = new List<RegChange>();

            try
            {
                foreach (RegChange reg in updateFrom.RegChanges)
                {
                    reg.ExecuteOperation(rollback);

                    //rollback the operation
                    foreach (RegChange regBack in rollback)
                    {
                        regBack.ExecuteOperation();
                    }

                    rollback.Clear();
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool CanManipFiles()
        {
            //test basic file & folder creation
            Random random = new Random();

            List<string> directories = new List<string>(2);

            //Check baseDirectory (where the app is installed)
            if ((updateFrom.InstallingTo & InstallingTo.BaseDir) != 0)
                directories.Add(baseDirectory);

            //Check check dir of client config file (*.iucz)
            if (!IsFileInDirectories(directories, clientFileLoc))
                directories.Add(Path.GetDirectoryName(clientFileLoc));

            //Check client app directory (where THIS is) only if the file needs to be updated
            if ((willSelfUpdate || selfUpdating) && !IsFileInDirectories(directories, System.Reflection.Assembly.GetExecutingAssembly().Location))
            {
                directories.Add(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            }

            //If installing to Win\System32 (or one of the common folder), check permissions
            if (updateFrom.InstallingTo != 0 && (updateFrom.InstallingTo & InstallingTo.BaseDir) == 0)
                directories.Add(Environment.GetFolderPath(Environment.SpecialFolder.System));

            FileStream fs = null;

            string filename = "";
            string directory = "";

            foreach (string dir in directories)
            {
                fs = null;

                try
                {
                    //make sure the new dir doesn't already exists
                    do
                    {
                        directory = Path.Combine(dir, random.Next().ToString());
                    } while (Directory.Exists(directory));

                    Directory.CreateDirectory(directory);

                    //generate a new filename
                    filename = Path.Combine(directory, random.Next().ToString());

                    //create a small file
                    fs = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite);
                    fs.WriteByte(0x01);
                    fs.Close();

                    //now delete the file and directory
                    File.Delete(filename);
                    Directory.Delete(directory, true);
                }
                catch (Exception)
                {
                    if (fs != null)
                        fs.Close();

                    return false;
                }
            }

            return true;
        }


        private static bool IsFileInDirectories(List<string> dirs, string file)
        {
            foreach (string dir in dirs)
            {
                if (IsFileInDirectory(dir, file))
                    return true;
            }

            return false;
        }

        private static bool IsFileInDirectory(string dir, string file)
        {
            StringBuilder strBuild = new StringBuilder(InstallUpdate.MAX_PATH);

            bool bRet = InstallUpdate.PathRelativePathTo(
                strBuild,
                dir, (uint)InstallUpdate.PathAttribute.Directory,
                file, (uint)InstallUpdate.PathAttribute.File
            );

            if (bRet && strBuild.Length >= 2)
            {
                //get the first two characters
                if (strBuild.ToString().Substring(0, 2) == @".\") 
                {
                    //if file is in the directory (or a subfolder)
                    return true;
                }
            }

            return false;
        }

        private static bool IsDirInDir(string dir, string checkDir)
        {
            StringBuilder strBuild = new StringBuilder(InstallUpdate.MAX_PATH);

            bool bRet = InstallUpdate.PathRelativePathTo(
                strBuild,
                dir, (uint)InstallUpdate.PathAttribute.Directory,
                checkDir, (uint)InstallUpdate.PathAttribute.Directory
            );

            if (bRet)
            {
                if (strBuild.Length == 1) //result is "."
                    return true;

                if (strBuild.Length >= 2
                    //get the first two characters
                    && strBuild.ToString().Substring(0, 2) == @".\")
                {
                    //if checkDir is the directory (or a subfolder)
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Logging off & Shutting Down



        [DllImport("user32.dll")]
        public extern static bool ShutdownBlockReasonCreate(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] string pwszReason);

        [DllImport("user32.dll")]
        public extern static bool ShutdownBlockReasonDestroy(IntPtr hWnd);

        private bool logOffBlocked;

        private void BlockLogOff(bool block)
        {
            logOffBlocked = block;

            try
            {
                if (block)
                    ShutdownBlockReasonCreate(Handle, clientLang.LogOffError);
                else
                    ShutdownBlockReasonDestroy(Handle);
            }
            catch { }
        }

        protected override void WndProc(ref Message aMessage)
        {
            //WM_QUERYENDSESSION = 0x0011
            //WM_ENDSESSION = 0x0016
            if (logOffBlocked && (aMessage.Msg == 0x0011 || aMessage.Msg == 0x0016))
                return;

            base.WndProc(ref aMessage);
        }

        #endregion
    }
}
