using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace wyUpdate
{
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

    public class LanguageCulture
    {
        public string Culture;
        public string Filename;

        public LanguageCulture (string culture)
        {
            Culture = culture;
        }
    }

    public class ClientLanguage
    {
        //Return parsed strings?
        private bool m_ReturnParsedStrings = true;

        //Language Name
        private string m_EnglishName, m_Name = "English", m_Culture = "en-US";

        //Buttons
        private string m_NextButton = "Next", 
            m_UpdateButton = "Update", 
            m_FinishButton = "Finish", 
            m_CancelButton = "Cancel";

        //Dialogs
        private ScreenDialog 
            m_ProcessDialog = new ScreenDialog("Close processes...",
                null,
                "The following processes need to be closed before updating can continue. Select a process and click Close Process."),
            m_CancelDialog = new ScreenDialog("Cancel update?",
                null,
                "Are you sure you want to exit before the update is complete?");

        private string m_ClosePrc = "Close Process",
            m_CloseAllPrc = "Close All Processes", 
            m_CancelUpdate = "Cancel Update";

        //Errors
        private string m_ServerError = "Unable to check for updates, the server file failed to load:",
            m_AdminError = "wyUpdate needs administrative privileges to update %product%. You can do this one of two ways:\r\n\r\n" +
                "1. When prompted, enter an administrator's username and password.\r\n\r\n" +
                "2. In Windows Explorer right click wyUpdate.exe and click \"Run as Administrator\"",
            m_DownloadError = "The update failed to download:",
            m_GeneralUpdateError = "The update failed to install:",
            m_SelfUpdateInstallError = "The updated version of wyUpdate required to update %product% failed to install:",
            m_LogOffError = "Updating %product%. You must cancel wyUpdate before you can log off.";

        //Update Screens
        private ScreenDialog
            m_Checking = new ScreenDialog("Searching for updates",
                "wyUpdate is searching for updates.",
                "wyUpdate is searching for updates to %product%. This process could take a few minutes."),
            m_UpdateInfo = new ScreenDialog("Update Information",
                "Changes in the latest version of %product%.",
                "The version of %product% installed on this computer is %old_version%. The latest version is %new_version%. Listed below are the changes and improvements:"),
            m_DownInstall = new ScreenDialog("Downloading & Installing updates",
                "Updating %product% to the latest version.",
                "wyUpdate is downloading and installing updates for %product%. This process could take a few minutes."),
            m_Uninstall = new ScreenDialog("Uninstalling files, folders, and registry",
                "Uninstalling files and registry for %product%.",
                "wyUpdate is uninstalling files and registry created when updates were applied to %product%."),
            m_SuccessUpdate = new ScreenDialog("Update successful!",
                null,
                "%product% has been successfully updated to version %new_version%"),
            m_AlreadyLatest = new ScreenDialog("Latest version already installed",
                null,
                "%product% is currently up-to-date. Remember to check for new updates frequently."),
            m_NoUpdateToLatest = new ScreenDialog("No update to the latest version",
                null,
                "There is a newer version of %product% (version %new_version%), but no update available from the version you currently have installed (version %old_version%)."),
            m_UpdateError = new ScreenDialog("An error occurred",
                null,
                null);

        //Bottoms
        private string m_UpdateBottom = "Click Update to begin.", 
            m_FinishBottom = "Click Finish to exit.";

        //Status
        private string m_Download = "Downloading update",
            m_DownloadingSelfUpdate = "Downloading new wyUpdate",
            m_SelfUpdate = "Updating wyUpdate",
            m_Extract = "Extracting files",
            m_Processes = "Closing processes",
            m_PreExec = "Executing files",
            m_Files = "Backing up and updating files",
            m_Registry = "Backing up and updating registry",
            m_Optimize = "Optimizing and executing files",
            m_TempFiles = "Removing temporary files",
            m_UninstallFiles = "Uninstalling files & folders",
            m_UninstallRegistry = "Uninstalling registry";

        //Variables
        private string m_ProductName, m_OldVersion, m_NewVersion;
        
        #region Properties

        public bool ReturnParsedStrings
        {
            get { return m_ReturnParsedStrings; }
            set { m_ReturnParsedStrings = value; }
        }

        //Name of the Language
        public string EnglishName
        {
            get { return m_EnglishName; }
            set { m_EnglishName = value; }
        }

        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public string Culture
        {
            get { return m_Culture; }
            set { m_Culture = value; }
        }

        //Buttons
        public string NextButton
        {
            get 
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_NextButton);
                
                return m_NextButton;
            }
            set { m_NextButton = value; }
        }

        public string UpdateButton
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_UpdateButton);
                
                return m_UpdateButton;
            }
            set { m_UpdateButton = value; }
        }

        public string FinishButton
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_FinishButton);
                
                return m_FinishButton;
            }
            set { m_FinishButton = value; }
        }

        public string CancelButton
        {
            get 
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_CancelButton);
                
                return m_CancelButton;
            }
            set { m_CancelButton = value; }
        }

        //Dialogs
        public ScreenDialog ProcessDialog
        {
            get 
            {
                if (m_ReturnParsedStrings)
                    return ParseScreenDialog(m_ProcessDialog);
                
                return m_ProcessDialog;
            }
            set { m_ProcessDialog = value; }
        }

        public ScreenDialog CancelDialog
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseScreenDialog(m_CancelDialog);
                
                return m_CancelDialog;
            }
            set { m_CancelDialog = value; }
        }

        public string ClosePrc
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_ClosePrc);
                
                return m_ClosePrc;
            }
            set { m_ClosePrc = value; }
        }

        public string CloseAllPrc
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_CloseAllPrc);
                
                return m_CloseAllPrc;
            }
            set { m_CloseAllPrc = value; }
        }

        public string CancelUpdate
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_CancelUpdate);
                
                return m_CancelUpdate;
            }
            set { m_CancelUpdate = value; }
        }

        //Errors
        public string ServerError
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_ServerError);
                
                return m_ServerError;
            }
            set { m_ServerError = value; }
        }

        public string AdminError
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_AdminError);
                
                return m_AdminError;
            }
            set { m_AdminError = value; }
        }

        public string DownloadError
        {
            get 
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_DownloadError);
                
                return m_DownloadError;
            }
            set { m_DownloadError = value; }
        }

        public string GeneralUpdateError
        {
            get 
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_GeneralUpdateError);
                
                return m_GeneralUpdateError;
            }
            set { m_GeneralUpdateError = value; }
        }

        public string SelfUpdateInstallError
        {
            get 
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_SelfUpdateInstallError);
                
                return m_SelfUpdateInstallError; 
            }
            set { m_SelfUpdateInstallError = value; }
        }

        public string LogOffError
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_LogOffError);

                return m_LogOffError;
            }
            set { m_LogOffError = value; }
        }

        //Update Screens
        public ScreenDialog Checking
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseScreenDialog(m_Checking);
                
                return m_Checking;
            }
            set { m_Checking = value; }
        }

        public ScreenDialog UpdateInfo
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseScreenDialog(m_UpdateInfo);
                
                return m_UpdateInfo;
            }
            set { m_UpdateInfo = value; }
        }

        public ScreenDialog DownInstall
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseScreenDialog(m_DownInstall);
                
                return m_DownInstall;
            }
            set { m_DownInstall = value; }
        }

        public ScreenDialog Uninstall
        {
            get 
            {
                if (m_ReturnParsedStrings)
                    return ParseScreenDialog(m_Uninstall);
                
                return m_Uninstall;
            }
            set { m_Uninstall = value; }
        }

        public ScreenDialog SuccessUpdate
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseScreenDialog(m_SuccessUpdate);
                
                return m_SuccessUpdate;
            }
            set { m_SuccessUpdate = value; }
        }

        public ScreenDialog AlreadyLatest
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseScreenDialog(m_AlreadyLatest);
                
                return m_AlreadyLatest;
            }
            set { m_AlreadyLatest = value; }
        }

        public ScreenDialog NoUpdateToLatest
        {
            get 
            {
                if (m_ReturnParsedStrings)
                    return ParseScreenDialog(m_NoUpdateToLatest);
                
                return m_NoUpdateToLatest;
            }
            set { m_NoUpdateToLatest = value; }
        }

        public ScreenDialog UpdateError
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseScreenDialog(m_UpdateError);
                
                return m_UpdateError;
            }
            set { m_UpdateError = value; }
        }

        //Bottom instructions
        public string UpdateBottom
        {
            get 
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_UpdateBottom);
                
                return m_UpdateBottom;
            }
            set { m_UpdateBottom = value; }
        }

        public string FinishBottom
        {
            get 
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_FinishBottom);
                
                return m_FinishBottom;
            }
            set { m_FinishBottom = value; }
        }

        //Status
        public string Download
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_Download);
                
                return m_Download;
            }
            set { m_Download = value; }
        }

        public string DownloadingSelfUpdate
        {
            get 
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_DownloadingSelfUpdate);
                
                return m_DownloadingSelfUpdate; 
            }
            set { m_DownloadingSelfUpdate = value; }
        }

        public string SelfUpdate
        {
            get 
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_SelfUpdate);
                
                return m_SelfUpdate;
            }
            set { m_SelfUpdate = value; }
        }

        public string Extract
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_Extract);
                
                return m_Extract;
            }
            set { m_Extract = value; }
        }

        public string Processes
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_Processes);
                
                return m_Processes;
            }
            set { m_Processes = value; }
        }

        public string PreExec
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_PreExec);
                
                return m_PreExec;
            }
            set { m_PreExec = value; }
        }

        public string Files
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_Files);
                
                return m_Files;
            }
            set { m_Files = value; }
        }

        public string Registry
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_Registry);
                
                return m_Registry;
            }
            set { m_Registry = value; }
        }

        public string Optimize
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_Optimize);
                
                return m_Optimize;
            }
            set { m_Optimize = value; }
        }

        public string TempFiles
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_TempFiles);
                
                return m_TempFiles;
            }
            set { m_TempFiles = value; }
        }

        public string UninstallRegistry
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_UninstallRegistry);
                
                return m_UninstallRegistry;
            }
            set { m_UninstallRegistry = value; }
        }

        public string UninstallFiles
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_UninstallFiles);
                
                return m_UninstallFiles;
            }
            set { m_UninstallFiles = value; }
        }

        #endregion Properties

        #region Variable Properties (Product, Version, etc.)

        public string ProductName
        {
            get { return m_ProductName; }
            set { m_ProductName = value; }
        }

        public string OldVersion
        {
            get { return m_OldVersion; }
            set { m_OldVersion = value; }
        }

        public string NewVersion
        {
            get { return m_NewVersion; }
            set { m_NewVersion = value; }
        }

        #endregion

        public void SetVariables(string product, string oldversion)
        {
            m_ProductName = product;
            m_OldVersion = oldversion;
        }

        public void ResetLanguage()
        {
            m_Name = m_EnglishName = m_NextButton = m_UpdateButton = m_FinishButton = m_CancelButton 
                = m_ClosePrc = m_CloseAllPrc = m_CancelUpdate  = m_Culture = null;

            m_ProcessDialog.Clear();
            m_CancelDialog.Clear();

            //Errors
            m_ServerError = m_AdminError = m_GeneralUpdateError = m_DownloadError = m_SelfUpdateInstallError = m_LogOffError = null;

            //Update Screens
            m_Checking.Clear();
            m_UpdateInfo.Clear();
            m_DownInstall.Clear();
            m_Uninstall.Clear();
            m_SuccessUpdate.Clear();
            m_AlreadyLatest.Clear();
            m_NoUpdateToLatest.Clear();
            m_UpdateError.Clear();

            //bottoms
            m_FinishBottom = m_UpdateBottom = null;

            //Status
            m_Download = m_DownloadingSelfUpdate = m_SelfUpdate = m_Extract = m_Processes = m_PreExec
                = m_Files = m_Registry = m_Optimize = m_TempFiles = m_UninstallFiles = m_UninstallRegistry = null;
        }

        #region Parsing language strings

        private string ParseText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            List<string> excludeVariables = new List<string>();

            return ParseVariableText(text, ref excludeVariables);
        }

        private string ParseVariableText(string text, ref List<string> excludeVariables)
        {
            //parse a string, and return a pretty string (sans %%)
            StringBuilder returnString = new StringBuilder();
            string tempString;

            int currentIndex;

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
                currentIndex = text.IndexOf('%', firstIndex + 1);

                //if no closing percent sign...
                if (currentIndex == -1)
                {
                    //return the rest of the string
                    returnString.Append(text.Substring(firstIndex, text.Length - firstIndex));
                    return returnString.ToString();
                }


                //return the content of the variable
                tempString = VariableToPretty(text.Substring(firstIndex + 1, currentIndex - firstIndex - 1), ref excludeVariables);

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

        private string VariableToPretty(string variable, ref List<string> excludeVariables)
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

        private ScreenDialog ParseScreenDialog(ScreenDialog dialog)
        {
            return new ScreenDialog(ParseText(dialog.Title),
                ParseText(dialog.SubTitle),
                ParseText(dialog.Content));
        }

        #endregion Parsing language strings

        #region Reading XML language file


        public void Open(string filename)
        {
            XmlTextReader reader = null;

            try
            {
                reader = new XmlTextReader(filename);

                ReadLanguageFile(reader);
            }
            catch (Exception)
            {
                if (reader != null)
                    reader.Close();
            }
        }

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

        private void ReadLanguageFile(XmlTextReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement)
                {
                    if (reader.LocalName.Equals("Lang"))
                        m_Name = reader.ReadString();
                    else if (reader.LocalName.Equals("LangEn"))
                        m_EnglishName = reader.ReadString();
                    else if (reader.LocalName.Equals("Culture"))
                        m_Culture = reader.ReadString();
                    else if (reader.LocalName.Equals("Buttons"))
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

        private void ReadButtons(XmlTextReader reader)
        {
            while (reader.Read())
            {
                //end of node </Buttons>
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals("Buttons"))
                    return;

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName.Equals("Next"))
                        m_NextButton = reader.ReadString();
                    else if (reader.LocalName.Equals("Update"))
                        m_UpdateButton = reader.ReadString();
                    else if (reader.LocalName.Equals("Finish"))
                        m_FinishButton = reader.ReadString();
                    else if (reader.LocalName.Equals("Cancel"))
                        m_CancelButton = reader.ReadString();
                    else if (reader.LocalName.Equals("Close"))
                        m_ClosePrc = reader.ReadString();
                    else if (reader.LocalName.Equals("CloseAll"))
                        m_CloseAllPrc = reader.ReadString();
                    else if (reader.LocalName.Equals("CancelUpdate"))
                        m_CancelUpdate = reader.ReadString();
                }
            }
        }

        private void ReadScreens(XmlTextReader reader)
        {
            while (reader.Read())
            {
                //end of node </Screens>
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals("Screens"))
                    return;

                if (reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement)
                {
                    if (reader.LocalName.Equals("Checking"))
                        ReadScreenDialog(reader, m_Checking);
                    else if (reader.LocalName.Equals("UpdateInfo"))
                        ReadScreenDialog(reader, m_UpdateInfo);
                    else if (reader.LocalName.Equals("DownInstall"))
                        ReadScreenDialog(reader, m_DownInstall);
                    else if (reader.LocalName.Equals("Uninstall"))
                        ReadScreenDialog(reader, m_Uninstall);
                    else if (reader.LocalName.Equals("SuccessUpdate"))
                        ReadScreenDialog(reader, m_SuccessUpdate);
                    else if (reader.LocalName.Equals("AlreadyLatest"))
                        ReadScreenDialog(reader, m_AlreadyLatest);
                    else if (reader.LocalName.Equals("NoUpdateToLatest"))
                        ReadScreenDialog(reader, m_NoUpdateToLatest);
                    else if (reader.LocalName.Equals("UpdateError"))
                        ReadScreenDialog(reader, m_UpdateError);
                }
            }
        }

        private void ReadDialogs(XmlTextReader reader)
        {
            while (reader.Read())
            {
                //end of node </Dialogs>
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals("Dialogs"))
                    return;

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName.Equals("Cancel"))
                        ReadScreenDialog(reader, m_CancelDialog);
                    else if (reader.LocalName.Equals("Processes"))
                        ReadScreenDialog(reader, m_ProcessDialog);
                }
            }
        }

        private static void ReadScreenDialog(XmlTextReader reader, ScreenDialog sd)
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

        private void ReadStatus(XmlTextReader reader)
        {
            while (reader.Read())
            {
                //end of node </Status>
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals("Status"))
                    return;

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName.Equals("Download"))
                        m_Download = reader.ReadString();
                    else if (reader.LocalName.Equals("DownloadSelfUpdate"))
                        m_DownloadingSelfUpdate = reader.ReadString();
                    else if (reader.LocalName.Equals("SelfUpdate"))
                        m_SelfUpdate = reader.ReadString();
                    else if (reader.LocalName.Equals("Extract"))
                        m_Extract = reader.ReadString();
                    else if (reader.LocalName.Equals("Processes"))
                        m_Processes = reader.ReadString();
                    else if (reader.LocalName.Equals("PreExec"))
                        m_PreExec = reader.ReadString();
                    else if (reader.LocalName.Equals("Files"))
                        m_Files = reader.ReadString();
                    else if (reader.LocalName.Equals("Registry"))
                        m_Registry = reader.ReadString();
                    else if (reader.LocalName.Equals("Optimize"))
                        m_Optimize = reader.ReadString();
                    else if (reader.LocalName.Equals("TempFiles"))
                        m_TempFiles = reader.ReadString();
                    else if (reader.LocalName.Equals("UninstallFiles"))
                        m_UninstallFiles = reader.ReadString();
                    else if (reader.LocalName.Equals("UninstallReg"))
                        m_UninstallRegistry = reader.ReadString();
                }
            }
        }

        private void ReadErrors(XmlTextReader reader)
        {
            while (reader.Read())
            {
                //end of node </Errors>
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals("Errors"))
                    return;

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName.Equals("ServFile"))
                        m_ServerError = reader.ReadString();
                    else if (reader.LocalName.Equals("Admin"))
                        m_AdminError = reader.ReadString();
                    else if (reader.LocalName.Equals("Update"))
                        m_GeneralUpdateError = reader.ReadString();
                    else if (reader.LocalName.Equals("Download"))
                        m_DownloadError = reader.ReadString();
                    else if (reader.LocalName.Equals("SelfUpdate"))
                        m_SelfUpdateInstallError = reader.ReadString();
                    else if (reader.LocalName.Equals("LogOff"))
                        m_LogOffError = reader.ReadString();
                }
            }
        }

        private void ReadBottoms(XmlTextReader reader)
        {
            while (reader.Read())
            {
                //end of node </Bottoms>
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals("Bottoms"))
                    return;

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName.Equals("Update"))
                        m_UpdateBottom = reader.ReadString();
                    else if (reader.LocalName.Equals("Finish"))
                        m_FinishBottom = reader.ReadString();
                }
            }
        }

        #endregion Reading XML language file

        #region Saving XML language file

        public void Save(string filename)
        {
            XmlTextWriter writer = null;
            //save user templates
            try
            {
                writer = new XmlTextWriter(filename, Encoding.UTF8);

                WriteLanguageFile(writer);
            }
            catch (Exception)
            {
                if (writer != null)
                    writer.Close();
            }
        }

        private void WriteLanguageFile(XmlTextWriter writer)
        {
            writer.Formatting = Formatting.Indented;
            writer.IndentChar = '\t';
            writer.Indentation = 1;
            writer.Namespaces = false;

            writer.WriteStartDocument();

            writer.WriteStartElement("Translation"); //<Translation>
            {
                if (!string.IsNullOrEmpty(m_EnglishName))
                    writer.WriteElementString("LangEn", m_EnglishName);

                if (!string.IsNullOrEmpty(m_Name))
                    writer.WriteElementString("Lang", m_Name);

                if (!string.IsNullOrEmpty(m_Culture))
                    writer.WriteElementString("Culture", m_Culture);

                writer.WriteStartElement("Buttons"); //<Buttons>
                {
                    WriteString(writer, "Next", m_NextButton);
                    WriteString(writer, "Update", m_UpdateButton);
                    WriteString(writer, "Finish", m_FinishButton);
                    WriteString(writer, "Cancel", m_CancelButton);
                    WriteString(writer, "Close", m_ClosePrc);
                    WriteString(writer, "CloseAll", m_CloseAllPrc);
                    WriteString(writer, "CancelUpdate", m_CancelUpdate);
                }
                writer.WriteEndElement(); //</Buttons>

                writer.WriteStartElement("Dialogs"); //<Dialogs>
                {
                    WriteScreenDialog(writer, "Cancel", m_CancelDialog);
                    WriteScreenDialog(writer, "Processes", m_ProcessDialog);
                }
                writer.WriteEndElement(); //</Dialogs>

                writer.WriteStartElement("Errors"); //<Errors>
                {
                    WriteString(writer, "Admin", m_AdminError);
                    WriteString(writer, "Download", m_DownloadError);
                    WriteString(writer, "LogOff", m_LogOffError);
                    WriteString(writer, "SelfUpdate", m_SelfUpdateInstallError);
                    WriteString(writer, "ServFile", m_ServerError);
                    WriteString(writer, "Update", m_GeneralUpdateError);
                }
                writer.WriteEndElement(); //</Errors>

                writer.WriteStartElement("Screens"); //<Screens>
                {
                    WriteScreenDialog(writer, "Checking", m_Checking);
                    WriteScreenDialog(writer, "UpdateInfo", m_UpdateInfo);
                    WriteScreenDialog(writer, "DownInstall", m_DownInstall);
                    WriteScreenDialog(writer, "Uninstall", m_Uninstall);
                    WriteScreenDialog(writer, "SuccessUpdate", m_SuccessUpdate);
                    WriteScreenDialog(writer, "AlreadyLatest", m_AlreadyLatest);
                    WriteScreenDialog(writer, "NoUpdateToLatest", m_NoUpdateToLatest);
                    WriteScreenDialog(writer, "UpdateError", m_UpdateError);
                }
                writer.WriteEndElement(); //</Screens>

                writer.WriteStartElement("Status"); //<Status>
                {
                    WriteString(writer, "Download", m_Download);
                    WriteString(writer, "DownloadSelfUpdate", m_DownloadingSelfUpdate);
                    WriteString(writer, "SelfUpdate", m_SelfUpdate);
                    WriteString(writer, "Extract", m_Extract);
                    WriteString(writer, "Processes", m_Processes);
                    WriteString(writer, "PreExec", m_PreExec);
                    WriteString(writer, "Files", m_Files);
                    WriteString(writer, "Registry", m_Registry);
                    WriteString(writer, "Optimize", m_Optimize);
                    WriteString(writer, "TempFiles", m_TempFiles);
                    WriteString(writer, "UninstallFiles", m_UninstallFiles);
                    WriteString(writer, "UninstallReg", m_UninstallRegistry);
                }
                writer.WriteEndElement(); //</Status>

                writer.WriteStartElement("Bottoms"); //<Bottoms>
                {
                    WriteString(writer, "Update", m_UpdateBottom);
                    WriteString(writer, "Finish", m_FinishBottom);
                }
                writer.WriteEndElement(); //</Bottoms>
            }
            writer.WriteEndElement(); // </Translation>

            writer.Flush();
            writer.Close();
        }

        private static void WriteString(XmlTextWriter writer, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
                writer.WriteElementString(name, value);
        }

        private static void WriteScreenDialog(XmlTextWriter writer, string name, ScreenDialog sd)
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

        #endregion Saving XML language file
    }
}
