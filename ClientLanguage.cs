using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace wyUpdate
{
    public class LanguageCulture
    {
        public string Culture;
        public string Filename;

        public LanguageCulture (string culture)
        {
            Culture = culture;
        }
    }

#if !WBCMD

    public class ScreenDialog
    {
        public string Title;
        public string SubTitle;
        public string Content;

        public bool IsEmpty
        {
            get
            {
                //if all the fields are empty then the screenDialog is empty
                return (string.IsNullOrEmpty(Title)
                    && string.IsNullOrEmpty(SubTitle)
                    && string.IsNullOrEmpty(Content));
            }
        }

        public ScreenDialog(string title, string subtitle, string content)
        {
            Title = title;
            SubTitle = subtitle;
            Content = content;
        }

        public void Clear()
        {
            Title = SubTitle = Content = null;
        }
    }

    public class ClientLanguage
    {
#if CLIENT
        // Buttons
        string m_NextButton = "Next";
        public string NextButton
        {
            get { return ParseText(m_NextButton); }
            set { m_NextButton = value; }
        }

        string m_UpdateButton = "Update";
        public string UpdateButton
        {
            get { return ParseText(m_UpdateButton); }
            set { m_UpdateButton = value; }
        }

        string m_FinishButton = "Finish";
        public string FinishButton
        {
            get { return ParseText(m_FinishButton); }
            set { m_FinishButton = value; }
        }

        string m_CancelButton = "Cancel";
        public string CancelButton
        {
            get { return ParseText(m_CancelButton); }
            set { m_CancelButton = value; }
        }

        string m_ShowDetails = "Show details";
        public string ShowDetails
        {
            get { return ParseText(m_ShowDetails); }
            set { m_ShowDetails = value; }
        }

        // Dialogs
        ScreenDialog m_ProcessDialog = new ScreenDialog("Close processes...",
                                               null,
                                               "The following processes need to be closed before updating can continue. Select a process and click Close Process.");

        public ScreenDialog ProcessDialog
        {
            get { return ParseScreenDialog(m_ProcessDialog); }
            set { m_ProcessDialog = value; }
        }


        ScreenDialog m_FilesInUseDialog = new ScreenDialog("Files in use...",
                                                           "These files are used by the following processes:",
                                                           "The following files are in use. These files must be closed before the update can continue.");
        public ScreenDialog FilesInUseDialog
        {
            get { return ParseScreenDialog(m_FilesInUseDialog); }
            set { m_FilesInUseDialog = value; }
        }

        ScreenDialog m_CancelDialog = new ScreenDialog("Cancel update?",
                                                       null,
                                                       "Are you sure you want to exit before the update is complete?");

        public ScreenDialog CancelDialog
        {
            get { return ParseScreenDialog(m_CancelDialog); }
            set { m_CancelDialog = value; }
        }

        string m_ClosePrc = "Close Process";
        public string ClosePrc
        {
            get { return ParseText(m_ClosePrc); }
            set { m_ClosePrc = value; }
        }

        string m_CloseAllPrc = "Close All Processes";
        public string CloseAllPrc
        {
            get { return ParseText(m_CloseAllPrc); }
            set { m_CloseAllPrc = value; }
        }

        string m_CancelUpdate = "Cancel Update";
        public string CancelUpdate
        {
            get { return ParseText(m_CancelUpdate); }
            set { m_CancelUpdate = value; }
        }


        // Errors

        string m_ServerError = "Unable to check for updates, the server file failed to load.";
        public string ServerError
        {
            get { return ParseText(m_ServerError); }
            set { m_ServerError = value; }
        }

        string m_AdminError =
            "wyUpdate needs administrative privileges to update %product%. You can do this one of two ways:\r\n\r\n" +
            "1. When prompted, enter an administrator's username and password.\r\n\r\n" +
            "2. In Windows Explorer right click wyUpdate.exe and click \"Run as Administrator\"";
        public string AdminError
        {
            get { return ParseText(m_AdminError); }
            set { m_AdminError = value; }
        }

        string m_DownloadError = "The update failed to download.";
        public string DownloadError
        {
            get { return ParseText(m_DownloadError); }
            set { m_DownloadError = value; }
        }

        string m_GeneralUpdateError = "The update failed to install.";
        public string GeneralUpdateError
        {
            get { return ParseText(m_GeneralUpdateError); }
            set { m_GeneralUpdateError = value; }
        }

        string m_SelfUpdateInstallError =
            "The updated version of wyUpdate required to update %product% failed to install.";
        public string SelfUpdateInstallError
        {
            get { return ParseText(m_SelfUpdateInstallError); }
            set { m_SelfUpdateInstallError = value; }
        }

        string m_LogOffError = "Updating %product%. You must cancel wyUpdate before you can log off.";
        public string LogOffError
        {
            get { return ParseText(m_LogOffError); }
            set { m_LogOffError = value; }
        }


                // Update Screens

        ScreenDialog m_Checking = new ScreenDialog("Searching for updates",
                                                   "wyUpdate is searching for updates.",
                                                   "wyUpdate is searching for updates to %product%. This process could take a few minutes.");
        public ScreenDialog Checking
        {
            get { return ParseScreenDialog(m_Checking); }
            set { m_Checking = value; }
        }

        ScreenDialog m_UpdateInfo = new ScreenDialog("Update Information",
                                                     "Changes in the latest version of %product%.",
                                                     "The version of %product% installed on this computer is %old_version%. The latest version is %new_version%. Listed below are the changes and improvements:");
        public ScreenDialog UpdateInfo
        {
            get { return ParseScreenDialog(m_UpdateInfo); }
            set { m_UpdateInfo = value; }
        }

        ScreenDialog m_DownInstall = new ScreenDialog("Downloading & Installing updates",
                                                      "Updating %product% to the latest version.",
                                                      "wyUpdate is downloading and installing updates for %product%. This process could take a few minutes.");
        public ScreenDialog DownInstall
        {
            get { return ParseScreenDialog(m_DownInstall); }
            set { m_DownInstall = value; }
        }

        ScreenDialog m_Uninstall = new ScreenDialog("Uninstalling files, folders, and registry",
                                                    "Uninstalling files and registry for %product%.",
                                                    "wyUpdate is uninstalling files and registry created when updates were applied to %product%.");
        public ScreenDialog Uninstall
        {
            get { return ParseScreenDialog(m_Uninstall); }
            set { m_Uninstall = value; }
        }

        ScreenDialog m_SuccessUpdate = new ScreenDialog("Update successful!",
                                                        null,
                                                        "%product% has been successfully updated to version %new_version%");
        public ScreenDialog SuccessUpdate
        {
            get { return ParseScreenDialog(m_SuccessUpdate); }
            set { m_SuccessUpdate = value; }
        }

        ScreenDialog m_AlreadyLatest = new ScreenDialog("Latest version already installed",
                                                        null,
                                                        "%product% is currently up-to-date. Remember to check for new updates frequently.");
        public ScreenDialog AlreadyLatest
        {
            get { return ParseScreenDialog(m_AlreadyLatest); }
            set { m_AlreadyLatest = value; }
        }


        ScreenDialog m_NoUpdateToLatest = new ScreenDialog("No update to the latest version",
                                                           null,
                                                           "There is a newer version of %product% (version %new_version%), but no update available from the version you currently have installed (version %old_version%).");
        public ScreenDialog NoUpdateToLatest
        {
            get { return ParseScreenDialog(m_NoUpdateToLatest); }
            set { m_NoUpdateToLatest = value; }
        }


        ScreenDialog m_UpdateError = new ScreenDialog("An error occurred",
                                                      null,
                                                      null);
        public ScreenDialog UpdateError
        {
            get { return ParseScreenDialog(m_UpdateError); }
            set { m_UpdateError = value; }
        }


        // Bottom instructions
        private string m_UpdateBottom = "Click Update to begin.";
        public string UpdateBottom
        {
            get { return ParseText(m_UpdateBottom); }
            set { m_UpdateBottom = value; }
        }

        private string m_FinishBottom = "Click Finish to exit.";
        public string FinishBottom
        {
            get { return ParseText(m_FinishBottom); }
            set { m_FinishBottom = value; }
        }

                // Status

        string m_Download = "Downloading update";
        public string Download
        {
            get { return ParseText(m_Download); }
            set { m_Download = value; }
        }

        string m_DownloadingSelfUpdate = "Downloading new wyUpdate";
        public string DownloadingSelfUpdate
        {
            get { return ParseText(m_DownloadingSelfUpdate); }
            set { m_DownloadingSelfUpdate = value; }
        }

        string m_SelfUpdate = "Updating wyUpdate";
        public string SelfUpdate
        {
            get { return ParseText(m_SelfUpdate); }
            set { m_SelfUpdate = value; }
        }

        string m_Extract = "Extracting files";
        public string Extract
        {
            get { return ParseText(m_Extract); }
            set { m_Extract = value; }
        }

        string m_Processes = "Closing processes";
        public string Processes
        {
            get { return ParseText(m_Processes); }
            set { m_Processes = value; }
        }

        string m_PreExec = "Executing files";
        public string PreExec
        {
            get { return ParseText(m_PreExec); }
            set { m_PreExec = value; }
        }

        string m_Files = "Backing up and updating files";
        public string Files
        {
            get { return ParseText(m_Files); }
            set { m_Files = value; }
        }

        string m_Registry = "Backing up and updating registry";
        public string Registry
        {
            get { return ParseText(m_Registry); }
            set { m_Registry = value; }
        }

        string m_Optimize = "Optimizing and executing files";
        public string Optimize
        {
            get { return ParseText(m_Optimize); }
            set { m_Optimize = value; }
        }

        string m_TempFiles = "Removing temporary files";
        public string TempFiles
        {
            get { return ParseText(m_TempFiles); }
            set { m_TempFiles = value; }
        }

        string m_UninstallRegistry = "Uninstalling registry";
        public string UninstallRegistry
        {
            get { return ParseText(m_UninstallRegistry); }
            set { m_UninstallRegistry = value; }
        }

        string m_UninstallFiles = "Uninstalling files & folders";
        public string UninstallFiles
        {
            get { return ParseText(m_UninstallFiles); }
            set { m_UninstallFiles = value; }
        }

        string m_RollingBackFiles = "Rolling back files";
        public string RollingBackFiles
        {
            get { return ParseText(m_RollingBackFiles); }
            set { m_RollingBackFiles = value; }
        }

        string m_RollingBackRegistry = "Rolling back registry";
        public string RollingBackRegistry
        {
            get { return ParseText(m_RollingBackRegistry); }
            set { m_RollingBackRegistry = value; }
        }
#else
        // Language Name
        public string EnglishName, Name = "English", Culture = "en-US";

        // Buttons
        public string NextButton = "Next";
        public string UpdateButton = "Update";
        public string FinishButton = "Finish";
        public string CancelButton = "Cancel";
        public string ShowDetails = "Show details";

        // Dialogs
        public ScreenDialog ProcessDialog = new ScreenDialog("Close processes...",
                                               null,
                                               "The following processes need to be closed before updating can continue. Select a process and click Close Process.");

        public ScreenDialog FilesInUseDialog = new ScreenDialog("Files in use...",
                                                                "These files are used by the following processes:",
                                                                "The following files are in use. These files must be closed before the update can continue.");

        public ScreenDialog CancelDialog = new ScreenDialog("Cancel update?",
                                                       null,
                                                       "Are you sure you want to exit before the update is complete?");


        public string ClosePrc = "Close Process";
        public string CloseAllPrc = "Close All Processes";
        public string CancelUpdate = "Cancel Update";

        // Errors
        public string ServerError = "Unable to check for updates, the server file failed to load.";
        public string AdminError =
            "wyUpdate needs administrative privileges to update %product%. You can do this one of two ways:\r\n\r\n" +
            "1. When prompted, enter an administrator's username and password.\r\n\r\n" +
            "2. In Windows Explorer right click wyUpdate.exe and click \"Run as Administrator\"";
        public string DownloadError = "The update failed to download.";
        public string GeneralUpdateError = "The update failed to install.";
        public string SelfUpdateInstallError =
            "The updated version of wyUpdate required to update %product% failed to install.";
        public string LogOffError = "Updating %product%. You must cancel wyUpdate before you can log off.";


        // Update Screens
        public ScreenDialog
            Checking = new ScreenDialog("Searching for updates",
                                          "wyUpdate is searching for updates.",
                                          "wyUpdate is searching for updates to %product%. This process could take a few minutes.");

        public ScreenDialog UpdateInfo = new ScreenDialog("Update Information",
                                                             "Changes in the latest version of %product%.",
                                                             "The version of %product% installed on this computer is %old_version%. The latest version is %new_version%. Listed below are the changes and improvements:");

        public ScreenDialog DownInstall = new ScreenDialog("Downloading & Installing updates",
                                                              "Updating %product% to the latest version.",
                                                              "wyUpdate is downloading and installing updates for %product%. This process could take a few minutes.");

        public ScreenDialog Uninstall = new ScreenDialog("Uninstalling files, folders, and registry",
                                                            "Uninstalling files and registry for %product%.",
                                                            "wyUpdate is uninstalling files and registry created when updates were applied to %product%.");

        public ScreenDialog SuccessUpdate = new ScreenDialog("Update successful!",
                                                                null,
                                                                "%product% has been successfully updated to version %new_version%");

        public ScreenDialog AlreadyLatest = new ScreenDialog("Latest version already installed",
                                                                null,
                                                                "%product% is currently up-to-date. Remember to check for new updates frequently.");

        public ScreenDialog NoUpdateToLatest = new ScreenDialog("No update to the latest version",
                                                                   null,
                                                                   "There is a newer version of %product% (version %new_version%), but no update available from the version you currently have installed (version %old_version%).");

        public ScreenDialog UpdateError = new ScreenDialog("An error occurred",
                                                              null,
                                                              null);


        // Bottom instructions
        public string UpdateBottom = "Click Update to begin.";
        public string FinishBottom = "Click Finish to exit.";

        // Status
        public string Download = "Downloading update";
        public string DownloadingSelfUpdate = "Downloading new wyUpdate";
        public string SelfUpdate = "Updating wyUpdate";
        public string Extract = "Extracting files";
        public string Processes = "Closing processes";
        public string PreExec = "Executing files";
        public string Files = "Backing up and updating files";
        public string Registry = "Backing up and updating registry";
        public string Optimize = "Optimizing and executing files";
        public string TempFiles = "Removing temporary files";
        public string UninstallFiles = "Uninstalling files & folders";
        public string UninstallRegistry = "Uninstalling registry";
        public string RollingBackFiles = "Rolling back files";
        public string RollingBackRegistry = "Rolling back registry";


        public void ResetLanguage()
        {
            Name = null;
            EnglishName = null;
            NextButton = null;
            UpdateButton = null;
            FinishButton = null;
            CancelButton = null;
            ShowDetails = null;
            ClosePrc = null;
            CloseAllPrc = null;
            CancelUpdate = null;
            Culture = null;

            ProcessDialog.Clear();
            CancelDialog.Clear();
            FilesInUseDialog.Clear();

            //Errors
            ServerError = null;
            AdminError = null;
            GeneralUpdateError = null;
            DownloadError = null;
            SelfUpdateInstallError = null;
            LogOffError = null;

            //Update Screens
            Checking.Clear();
            UpdateInfo.Clear();
            DownInstall.Clear();
            Uninstall.Clear();
            SuccessUpdate.Clear();
            AlreadyLatest.Clear();
            NoUpdateToLatest.Clear();
            UpdateError.Clear();

            //bottoms
            FinishBottom = null;
            UpdateBottom = null;

            //Status
            Download = null;
            DownloadingSelfUpdate = null;
            SelfUpdate = null;
            Extract = null;
            Processes = null;
            PreExec = null;
            Files = null;
            Registry = null;
            Optimize = null;
            TempFiles = null;
            UninstallFiles = null;
            UninstallRegistry = null;
            RollingBackFiles = null;
            RollingBackRegistry = null;
        }
#endif
#if CLIENT
        string m_ProductName, m_OldVersion, m_NewVersion;
        public string NewVersion { set { m_NewVersion = value; } }

        public void SetVariables(string product, string oldversion)
        {
            m_ProductName = product;
            m_OldVersion = oldversion;
        }

        string ParseText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            List<string> excludeVariables = new List<string>();

            return ParseVariableText(text, ref excludeVariables);
        }

        string ParseVariableText(string text, ref List<string> excludeVariables)
        {
            //parse a string, and return a pretty string (sans %%)
            StringBuilder returnString = new StringBuilder();

            int firstIndex = text.IndexOf('%', 0);

            if (firstIndex == -1)
            {
                //return the original
                return text;
            }

            returnString.Append(text.Substring(0, firstIndex));

            while (firstIndex != -1)
            {
                //find the next percent sign
                int currentIndex = text.IndexOf('%', firstIndex + 1);

                //if no closing percent sign...
                if (currentIndex == -1)
                {
                    //return the rest of the string
                    returnString.Append(text.Substring(firstIndex, text.Length - firstIndex));
                    return returnString.ToString();
                }


                //return the content of the variable
                string tempString = VariableToPretty(text.Substring(firstIndex + 1, currentIndex - firstIndex - 1), ref excludeVariables);

                //if the variable isn't defined
                if (tempString == null)
                {
                    //return the string with the percent signs
                    returnString.Append(text.Substring(firstIndex, currentIndex - firstIndex));
                }
                else
                {
                    //variable exists, add the parsed content
                    returnString.Append(tempString);
                    currentIndex++;
                    if (currentIndex == text.Length)
                    {
                        return returnString.ToString();
                    }
                }

                firstIndex = currentIndex;
            }

            return returnString.ToString();
        }

        string VariableToPretty(string variable, ref List<string> excludeVariables)
        {
            variable = variable.ToLower();

            if (excludeVariables.Contains(variable))
                return null;

            string returnValue;


            excludeVariables.Add(variable);

            switch (variable)
            {
                case "product":
                    returnValue = ParseVariableText(m_ProductName, ref excludeVariables);
                    break;
                case "old_version":
                    returnValue = ParseVariableText(m_OldVersion, ref excludeVariables);
                    break;
                case "new_version":
                    returnValue = ParseVariableText(m_NewVersion, ref excludeVariables);
                    break;
                default:
                    excludeVariables.RemoveAt(excludeVariables.Count - 1);
                    return null;
            }

            //allow the variable to be processed again
            excludeVariables.Remove(variable);

            return returnValue;
        }

        ScreenDialog ParseScreenDialog(ScreenDialog dialog)
        {
            return new ScreenDialog(ParseText(dialog.Title),
                ParseText(dialog.SubTitle),
                ParseText(dialog.Content));
        }

#endif

        #region Reading XML language file


#if CLIENT
        public void Open(MemoryStream ms)
        {
            XmlTextReader reader = null;

            ms.Position = 0;

            try
            {
                reader = new XmlTextReader(ms);

                ReadLanguageFile(reader);
            }
            catch (Exception)
            {
                if (reader != null)
                    reader.Close();
            }
        }
#else
        public void Open(string filename)
        {
            using (XmlTextReader reader = new XmlTextReader(filename))
            {
                ReadLanguageFile(reader);
            }
        }
#endif

        void ReadLanguageFile(XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement)
                {
#if !CLIENT
                    if (reader.LocalName.Equals("Lang"))
                        Name = reader.ReadString();
                    else if (reader.LocalName.Equals("LangEn"))
                        EnglishName = reader.ReadString();
                    else if (reader.LocalName.Equals("Culture"))
                        Culture = reader.ReadString();
                    else
#endif
                    if (reader.LocalName.Equals("Buttons"))
                        ReadButtons(reader);
                    else if (reader.LocalName.Equals("Screens"))
                        ReadScreens(reader);
                    else if (reader.LocalName.Equals("Dialogs"))
                        ReadDialogs(reader);
                    else if (reader.LocalName.Equals("Status"))
                        ReadStatus(reader);
                    else if (reader.LocalName.Equals("Errors"))
                        ReadErrors(reader);
                    else if (reader.LocalName.Equals("Bottoms"))
                        ReadBottoms(reader);
                }
            }

            reader.Close();
        }

        void ReadButtons(XmlReader reader)
        {
            while (reader.Read())
            {
                //end of node </Buttons>
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals("Buttons"))
                    return;

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName.Equals("Next"))
                        NextButton = reader.ReadString();
                    else if (reader.LocalName.Equals("Update"))
                        UpdateButton = reader.ReadString();
                    else if (reader.LocalName.Equals("Finish"))
                        FinishButton = reader.ReadString();
                    else if (reader.LocalName.Equals("Cancel"))
                        CancelButton = reader.ReadString();
                    else if (reader.LocalName.Equals("ShowDetails"))
                        ShowDetails = reader.ReadString();
                    else if (reader.LocalName.Equals("Close"))
                        ClosePrc = reader.ReadString();
                    else if (reader.LocalName.Equals("CloseAll"))
                        CloseAllPrc = reader.ReadString();
                    else if (reader.LocalName.Equals("CancelUpdate"))
                        CancelUpdate = reader.ReadString();
                }
            }
        }

        void ReadScreens(XmlReader reader)
        {
            while (reader.Read())
            {
                //end of node </Screens>
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals("Screens"))
                    return;

                if (reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement)
                {
                    if (reader.LocalName.Equals("Checking"))
                        ReadScreenDialog(reader, Checking);
                    else if (reader.LocalName.Equals("UpdateInfo"))
                        ReadScreenDialog(reader, UpdateInfo);
                    else if (reader.LocalName.Equals("DownInstall"))
                        ReadScreenDialog(reader, DownInstall);
                    else if (reader.LocalName.Equals("Uninstall"))
                        ReadScreenDialog(reader, Uninstall);
                    else if (reader.LocalName.Equals("SuccessUpdate"))
                        ReadScreenDialog(reader, SuccessUpdate);
                    else if (reader.LocalName.Equals("AlreadyLatest"))
                        ReadScreenDialog(reader, AlreadyLatest);
                    else if (reader.LocalName.Equals("NoUpdateToLatest"))
                        ReadScreenDialog(reader, NoUpdateToLatest);
                    else if (reader.LocalName.Equals("UpdateError"))
                        ReadScreenDialog(reader, UpdateError);
                }
            }
        }

        void ReadDialogs(XmlReader reader)
        {
            while (reader.Read())
            {
                //end of node </Dialogs>
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals("Dialogs"))
                    return;

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName.Equals("Cancel"))
                        ReadScreenDialog(reader, CancelDialog);
                    else if (reader.LocalName.Equals("Processes"))
                        ReadScreenDialog(reader, ProcessDialog);
                    else if (reader.LocalName.Equals("FilesInUse"))
                        ReadScreenDialog(reader, FilesInUseDialog);
                }
            }
        }

        static void ReadScreenDialog(XmlReader reader, ScreenDialog sd)
        {
            string screenEndName = reader.LocalName;

            while (reader.Read())
            {
                //end of node </screenEndName>
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals(screenEndName))
                    return;

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName.Equals("Title"))
                        sd.Title = reader.ReadString();
                    else if (reader.LocalName.Equals("SubTitle"))
                        sd.SubTitle = reader.ReadString();
                    else if (reader.LocalName.Equals("Content"))
                        sd.Content = reader.ReadString();
                }
            }
        }

        void ReadStatus(XmlReader reader)
        {
            while (reader.Read())
            {
                //end of node </Status>
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals("Status"))
                    return;

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName.Equals("Download"))
                        Download = reader.ReadString();
                    else if (reader.LocalName.Equals("DownloadSelfUpdate"))
                        DownloadingSelfUpdate = reader.ReadString();
                    else if (reader.LocalName.Equals("SelfUpdate"))
                        SelfUpdate = reader.ReadString();
                    else if (reader.LocalName.Equals("Extract"))
                        Extract = reader.ReadString();
                    else if (reader.LocalName.Equals("Processes"))
                        Processes = reader.ReadString();
                    else if (reader.LocalName.Equals("PreExec"))
                        PreExec = reader.ReadString();
                    else if (reader.LocalName.Equals("Files"))
                        Files = reader.ReadString();
                    else if (reader.LocalName.Equals("Registry"))
                        Registry = reader.ReadString();
                    else if (reader.LocalName.Equals("Optimize"))
                        Optimize = reader.ReadString();
                    else if (reader.LocalName.Equals("TempFiles"))
                        TempFiles = reader.ReadString();
                    else if (reader.LocalName.Equals("UninstallFiles"))
                        UninstallFiles = reader.ReadString();
                    else if (reader.LocalName.Equals("UninstallReg"))
                        UninstallRegistry = reader.ReadString();
                    else if (reader.LocalName.Equals("RollingBackFiles"))
                        RollingBackFiles = reader.ReadString();
                    else if (reader.LocalName.Equals("RollingBackRegistry"))
                        RollingBackRegistry = reader.ReadString();
                }
            }
        }

        void ReadErrors(XmlReader reader)
        {
            while (reader.Read())
            {
                //end of node </Errors>
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals("Errors"))
                    return;

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName.Equals("ServFile"))
                        ServerError = reader.ReadString();
                    else if (reader.LocalName.Equals("Admin"))
                        AdminError = reader.ReadString();
                    else if (reader.LocalName.Equals("Update"))
                        GeneralUpdateError = reader.ReadString();
                    else if (reader.LocalName.Equals("Download"))
                        DownloadError = reader.ReadString();
                    else if (reader.LocalName.Equals("SelfUpdate"))
                        SelfUpdateInstallError = reader.ReadString();
                    else if (reader.LocalName.Equals("LogOff"))
                        LogOffError = reader.ReadString();
                }
            }
        }

        void ReadBottoms(XmlReader reader)
        {
            while (reader.Read())
            {
                //end of node </Bottoms>
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals("Bottoms"))
                    return;

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName.Equals("Update"))
                        UpdateBottom = reader.ReadString();
                    else if (reader.LocalName.Equals("Finish"))
                        FinishBottom = reader.ReadString();
                }
            }
        }

        #endregion Reading XML language file

        #region Saving XML language file

#if !CLIENT
        public void Save(string filename)
        {
            // save the language file
            using (XmlTextWriter writer = new XmlTextWriter(filename, Encoding.UTF8))
            {
                WriteLanguageFile(writer);
            }
        }

        void WriteLanguageFile(XmlTextWriter writer)
        {
            writer.Formatting = Formatting.Indented;
            writer.IndentChar = '\t';
            writer.Indentation = 1;
            writer.Namespaces = false;

            writer.WriteStartDocument();

            writer.WriteStartElement("Translation"); //<Translation>
            {
                if (!string.IsNullOrEmpty(EnglishName))
                    writer.WriteElementString("LangEn", EnglishName);

                if (!string.IsNullOrEmpty(Name))
                    writer.WriteElementString("Lang", Name);

                if (!string.IsNullOrEmpty(Culture))
                    writer.WriteElementString("Culture", Culture);

                writer.WriteStartElement("Buttons"); //<Buttons>
                {
                    WriteString(writer, "Next", NextButton);
                    WriteString(writer, "Update", UpdateButton);
                    WriteString(writer, "Finish", FinishButton);
                    WriteString(writer, "Cancel", CancelButton);
                    WriteString(writer, "ShowDetails", ShowDetails);
                    WriteString(writer, "Close", ClosePrc);
                    WriteString(writer, "CloseAll", CloseAllPrc);
                    WriteString(writer, "CancelUpdate", CancelUpdate);
                }
                writer.WriteEndElement(); //</Buttons>

                writer.WriteStartElement("Dialogs"); //<Dialogs>
                {
                    WriteScreenDialog(writer, "Cancel", CancelDialog);
                    WriteScreenDialog(writer, "Processes", ProcessDialog);
                    WriteScreenDialog(writer, "FilesInUse", FilesInUseDialog);
                }
                writer.WriteEndElement(); //</Dialogs>

                writer.WriteStartElement("Errors"); //<Errors>
                {
                    WriteString(writer, "Admin", AdminError);
                    WriteString(writer, "Download", DownloadError);
                    WriteString(writer, "LogOff", LogOffError);
                    WriteString(writer, "SelfUpdate", SelfUpdateInstallError);
                    WriteString(writer, "ServFile", ServerError);
                    WriteString(writer, "Update", GeneralUpdateError);
                }
                writer.WriteEndElement(); //</Errors>

                writer.WriteStartElement("Screens"); //<Screens>
                {
                    WriteScreenDialog(writer, "Checking", Checking);
                    WriteScreenDialog(writer, "UpdateInfo", UpdateInfo);
                    WriteScreenDialog(writer, "DownInstall", DownInstall);
                    WriteScreenDialog(writer, "Uninstall", Uninstall);
                    WriteScreenDialog(writer, "SuccessUpdate", SuccessUpdate);
                    WriteScreenDialog(writer, "AlreadyLatest", AlreadyLatest);
                    WriteScreenDialog(writer, "NoUpdateToLatest", NoUpdateToLatest);
                    WriteScreenDialog(writer, "UpdateError", UpdateError);
                }
                writer.WriteEndElement(); //</Screens>

                writer.WriteStartElement("Status"); //<Status>
                {
                    WriteString(writer, "Download", Download);
                    WriteString(writer, "DownloadSelfUpdate", DownloadingSelfUpdate);
                    WriteString(writer, "SelfUpdate", SelfUpdate);
                    WriteString(writer, "Extract", Extract);
                    WriteString(writer, "Processes", Processes);
                    WriteString(writer, "PreExec", PreExec);
                    WriteString(writer, "Files", Files);
                    WriteString(writer, "Registry", Registry);
                    WriteString(writer, "Optimize", Optimize);
                    WriteString(writer, "TempFiles", TempFiles);
                    WriteString(writer, "UninstallFiles", UninstallFiles);
                    WriteString(writer, "UninstallReg", UninstallRegistry);
                    WriteString(writer, "RollingBackFiles", RollingBackFiles);
                    WriteString(writer, "RollingBackRegistry", RollingBackRegistry);
                }
                writer.WriteEndElement(); //</Status>

                writer.WriteStartElement("Bottoms"); //<Bottoms>
                {
                    WriteString(writer, "Update", UpdateBottom);
                    WriteString(writer, "Finish", FinishBottom);
                }
                writer.WriteEndElement(); //</Bottoms>
            }
            writer.WriteEndElement(); // </Translation>

            writer.Flush();
            writer.Close();
        }

        static void WriteString(XmlWriter writer, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
                writer.WriteElementString(name, value);
        }

        static void WriteScreenDialog(XmlWriter writer, string name, ScreenDialog sd)
        {
            if (!sd.IsEmpty)
            {
                writer.WriteStartElement(name); //<name>

                WriteString(writer, "Title", sd.Title);
                WriteString(writer, "SubTitle", sd.SubTitle);
                WriteString(writer, "Content", sd.Content);

                writer.WriteEndElement(); // </name>
            }
        }
#endif

        #endregion Saving XML language file
    }

#endif
}
