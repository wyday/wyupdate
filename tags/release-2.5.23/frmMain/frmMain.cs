using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using wyDay.Controls;
using wyUpdate.Common;
using wyUpdate.Downloader;

namespace wyUpdate
{
    public partial class frmMain : Form
    {
        #region Private variables

        public bool IsAdmin;

        public readonly ClientFile update = new ClientFile();
        ServerFile ServerFile;
        VersionChoice updateFrom;

        UpdateDetails updtDetails;

        FileDownloader downloader;
        InstallUpdate installUpdate;

        readonly ClientLanguage clientLang = new ClientLanguage();

        Frame frameOn = Frame.Checking;
        bool isCancelled;

        string error;
        string errorDetails;

        // full filename of the update & servers files
        string updateFilename;
        string serverFileLoc;

        //client file location
        string clientFileLoc;

        // are we using the -server commandline switch?
        string serverOverwrite;

        // custom update path (-updatepath commandline switch)
        string updatePathVar;

        // base directory: same path as the executable, unless specified
        string baseDirectory;
        //the extract directory
        string tempDirectory;

        readonly PanelDisplay panelDisplaying = new PanelDisplay(500, 320);

        // the first step wyUpdate should take
        UpdateStepOn startStep = UpdateStepOn.Nothing;

        // for self update
        public SelfUpdateState SelfUpdateState;

        //does the client need elevation?
        bool needElevation;

        //--Uninstalling
        bool uninstalling;

        //--Silent updating/uninstalling
        bool isSilent;
        public int ReturnCode { get; set; }

        //Pre-RC2 compatability:
        ClientFileType clientFileType;

        // handle hidden form
        bool _isApplicationRun = true;
        
        // start hidden, close if no update, show if update
        bool QuickCheck;
        bool QuickCheckNoErr;
        bool QuickCheckJustCheck;

        #endregion Private variables

        public frmMain(string[] args)
        {
            //sets to SegoeUI on Vista
            Font = SystemFonts.MessageBoxFont;

            // check if user is an admin for windows 2000+
            IsAdmin = VistaTools.IsUserAnAdmin();

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

            try
            {
                //process commandline argument
                Arguments commands = new Arguments(args);
                ProcessArguments(commands);


                // load the self update information
                if (!string.IsNullOrEmpty(selfUpdateFileLoc))
                {
                    //Note: always load the selfupdate data before the automatic update data
                    LoadSelfUpdateData(selfUpdateFileLoc);

                    //TODO: wyUp 3.0: excise this hack
                    //if the loaded file is from RC1, then update self and bail out
                    if (selfUpdateFromRC1)
                    {
                        //install the new client, and relaunch it to continue the update
                        if (needElevation && NeedElevationToUpdate())
                        {
                            //the user "elevated" as a non-admin user
                            //warn the user of their idiocy
                            error = clientLang.AdminError;

                            //set to false so new client won't be launched in frmMain_Load()
                            selfUpdateFromRC1 = false;

                            ShowFrame(Frame.Error);
                        }
                        else
                        {
                            needElevation = false;

                            FileAttributes atr = File.GetAttributes(oldSelfLocation);
                            bool resetAttributes = (atr & FileAttributes.Hidden) != 0 || (atr & FileAttributes.ReadOnly) != 0 || (atr & FileAttributes.System) != 0;

                            // remove the ReadOnly & Hidden atributes temporarily
                            if (resetAttributes)
                                File.SetAttributes(oldSelfLocation, FileAttributes.Normal);

                            //Install the new client
                            File.Copy(newSelfLocation, oldSelfLocation, true);

                            if (resetAttributes)
                                File.SetAttributes(oldSelfLocation, atr);

                            //Relaunch self in OnLoad()
                        }

                        //bail out
                        return;
                    }
                }

                //Load the client information
                if (clientFileType == ClientFileType.PreRC2)
                    //TODO: wyUp 3.0: stop supporting old client files (barely anyone uses RC2).
                    update.OpenObsoleteClientFile(clientFileLoc);
                else
                    update.OpenClientFile(clientFileLoc, clientLang);

                clientLang.SetVariables(update.ProductName, update.InstalledVersion);
            }
            catch (Exception ex)
            {
                clientLang.SetVariables(update.ProductName, update.InstalledVersion);

                error = "Client file failed to load. The client.wyc file might be corrupt.";
                errorDetails = ex.Message;

                ShowFrame(Frame.Error);
                return;
            }

            //sets up Next & Cancel buttons
            SetButtonText();

            //set header alignment, etc.
            panelDisplaying.HeaderImageAlign = update.HeaderImageAlign;

            if (update.HeaderTextIndent >= 0)
                panelDisplaying.HeaderIndent = update.HeaderTextIndent;

            panelDisplaying.HideHeaderDivider = update.HideHeaderDivider;

            // set the 
            if (update.CustomWyUpdateTitle != null)
                Text = update.CustomWyUpdateTitle;

            try
            {
                if (!string.IsNullOrEmpty(update.HeaderTextColorName))
                    panelDisplaying.HeaderTextColor = Color.FromName(update.HeaderTextColorName);
            }
            catch { }

            //load the Side/Top images
            panelDisplaying.TopImage = update.TopImage;
            panelDisplaying.SideImage = update.SideImage;

            if (isAutoUpdateMode)
            {
                // create the temp folder where we'll store the updates long term
                if (tempDirectory == null)
                    tempDirectory = CreateAutoUpdateTempFolder();
                
                try
                {
                    // load the previous auto update state from "autoupdate"
                    LoadAutoUpdateData();
                }
                catch
                {
                    startStep = UpdateStepOn.Checking;
                }
            }
            else if (SelfUpdateState == SelfUpdateState.FullUpdate)
            {
                try
                {
                    // load the server file for MinClient needed details (i.e. failure case)
                    ServerFile = ServerFile.Load(serverFileLoc, updatePathVar);

                    //load the self-update server file
                    LoadClientServerFile();
                    clientLang.NewVersion = SelfServerFile.NewVersion;
                }
                catch (Exception ex)
                {
                    error = clientLang.ServerError;
                    errorDetails = ex.Message;

                    ShowFrame(Frame.Error);
                    return;
                }

                if (needElevation && NeedElevationToUpdate())
                {
                    //the user "elevated" as a non-admin user
                    //warn the user of their idiocy
                    error = clientLang.AdminError;

                    ShowFrame(Frame.Error);
                }
                else
                {
                    needElevation = false;

                    //begin updating the product
                    ShowFrame(Frame.InstallUpdates);
                }
            }
            //continuing from elevation or self update (or both)
            else if (SelfUpdateState == SelfUpdateState.ContinuingRegularUpdate)
            {
                try
                {
                    //load the server file (without filling the 'changes' box & without downloading the wyUpdate Server file)
                    LoadServerFile(false);
                }
                catch (Exception ex)
                {
                    error = clientLang.ServerError;
                    errorDetails = ex.Message;

                    ShowFrame(Frame.Error);
                    return;
                }

                if (needElevation && NeedElevationToUpdate())
                {
                    // the user "elevated" as a non-admin user
                    // warn the user of their idiocy
                    error = clientLang.AdminError;

                    ShowFrame(Frame.Error);
                }
                else
                {
                    needElevation = false;

                    //begin updating the product
                    ShowFrame(Frame.InstallUpdates);
                }
            }
            else if (!uninstalling)
                startStep = UpdateStepOn.Checking;
        }

        protected override void SetVisibleCore(bool value)
        {
            if (_isApplicationRun)
            {
                _isApplicationRun = false;

                base.SetVisibleCore(value);


                if (isAutoUpdateMode)
                {
                    /* SetupAutoupdateMode must happen after the handle is created
                     * (aka. in OnHandleCreated, or after base.SetVisibleCore() is called)
                     * because Control.Invoke() used in UpdateHelper
                     * requires the handle to be created.
                     *
                     * This solves the problem where the AutomaticUpdater control sends a message,
                     * it thinks the message was recieved successfully because there
                     * wasn't an error on the pipe stream, however in reality it never gets past
                     * the try-catch block in 'pipeServer_MessageReceived'. The exception is gobbled up
                     * and there's a stalemate: wyUpdate is waiting for its first message, AutomaticUpdater
                     * is waiting for a progress report.
                     */
                    SetupAutoupdateMode();
                }


                // run the OnLoad code

                if (uninstalling)
                {
                    ShowFrame(Frame.Uninstall);
                }
                else if (selfUpdateFromRC1)
                {
                    //if the loaded file is from RC1, then update self and bail out

                    //Relaunch self
                    StartSelfElevated();
                }
                else if (startStep != UpdateStepOn.Nothing)
                {
                    // either begin checking or load the step from the autoupdate file
                    try
                    {
                        PrepareStepOn(startStep);

                        // selfupdate & post-selfupdate installation
                        if (beginAutoUpdateInstallation)
                            UpdateHelper_RequestReceived(this, UpdateAction.UpdateStep, UpdateStep.Install);
                    }
                    catch (Exception ex)
                    {
                        if (startStep != UpdateStepOn.Checking)
                            startStep = UpdateStepOn.Checking;
                        else
                        {
                            // show the error screen
                            error = "Automatic update state failed to load.";
                            errorDetails = ex.Message;

                            ShowFrame(Frame.Error);
                            return;
                        }

                        try
                        {
                            PrepareStepOn(startStep);
                        }
                        catch (Exception ex2)
                        {
                            // show the error screen
                            error = "Automatic update state failed to load.";
                            errorDetails = ex2.Message;

                            ShowFrame(Frame.Error);
                        }
                    }
                }

                return;
            }

            base.SetVisibleCore(value);
        }


        void ProcessArguments(Arguments commands)
        {
            if (commands["supdf"] != null)
            {
                //the client is in self update mode
                selfUpdateFileLoc = commands["supdf"];
            }
            else
            {
                // automatic update mode
                if (commands["autoupdate"] != null)
                {
                    // the actual pipe will be created when OnHandleCreated is called
                    isAutoUpdateMode = true;

                    // check if this instance is the "new self"
                    if (commands["ns"] != null)
                        IsNewSelf = true;
                }

                if (commands["quickcheck"] != null)
                {
                    WindowState = FormWindowState.Minimized;
                    ShowInTaskbar = false;

                    QuickCheck = true;

                    if (commands["noerr"] != null)
                        QuickCheckNoErr = true;

                    if (commands["justcheck"] != null)
                        QuickCheckJustCheck = true;
                }

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

                if (commands["basedir"] != null && Directory.Exists(commands["basedir"]))
                {
                    //if the specified directory exists, then set as directory
                    // also trim the trailing space
                    baseDirectory = commands["basedir"].TrimEnd();
                }

                // only generate a temp directory if we're not in AutoUpdate mode
                if (!isAutoUpdateMode)
                {
                    // create "random" temp dir.
                    tempDirectory = Path.Combine(Path.GetTempPath(), @"w" + DateTime.Now.ToString("sff"));
                    Directory.CreateDirectory(tempDirectory);
                }

                //uninstall any newly created folders, files, or registry
                if (commands["uninstall"] != null)
                    uninstalling = true;


                // load the passed server argument
                if (commands["server"] != null)
                    serverOverwrite = commands["server"];

                // load the custom updatepatch directory
                if (commands["updatepath"] != null)
                    updatePathVar = commands["updatepath"];

                if (commands["proxy"] != null)
                {
                    FileDownloader.CustomProxy = new WebProxy(commands["proxy"]);
                    
                    if (commands["proxyu"] != null)
                    {
                        FileDownloader.CustomProxy.Credentials = new NetworkCredential(
                            commands["proxyu"],
                            commands["proxyp"],

                            // if the domain is null, use an empty string
                            commands["proxyd"] ?? string.Empty
                            );
                    }
                }

                // only allow silent uninstalls 
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
            if (needElevation || SelfUpdateState == SelfUpdateState.WillUpdate || SelfUpdateState == SelfUpdateState.FullUpdate || isSilent || isAutoUpdateMode ||
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
            if (!(needElevation || SelfUpdateState == SelfUpdateState.WillUpdate || SelfUpdateState == SelfUpdateState.FullUpdate || isAutoUpdateMode))
            {
                RemoveTempDirectory();
            }

            base.OnClosed(e);
        }

        /// <summary>
        /// Remove the temporary directory if it exists.
        /// </summary>
        void RemoveTempDirectory()
        {
            if (!Directory.Exists(tempDirectory))
                return;

            try
            {
                Directory.Delete(tempDirectory, true);
            }
            catch { }
        }
    }
}
