using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace wyUpdate
{
    public struct ScreenDialog
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
        //Return parsed strings?
        private bool m_ReturnParsedStrings = true;

        //Language Name
        private string m_EnglishName, m_Name = "English";

        //Version of the client that the language was made for
        private string m_ClientVersion;

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
            m_SelfUpdateInstallError = "The updated version of this client required to update %product% failed to install:";

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
            m_DownloadingSelfUpdate = "Downloading new wyUpdate client",
            m_SelfUpdate = "Updating wyUpdate client",
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

        //Version of Client that the lang was created for
        public string ClientVersion
        {
            get { return m_ClientVersion; }
            set { m_ClientVersion = value; }
        }

        //Buttons
        public string NextButton
        {
            get 
            {
                if (m_ReturnParsedStrings)
                    return ParseText(m_NextButton);
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
                    return m_SelfUpdateInstallError; 
            }
            set { m_SelfUpdateInstallError = value; }
        }

        //Update Screens
        public ScreenDialog Checking
        {
            get
            {
                if (m_ReturnParsedStrings)
                    return ParseScreenDialog(m_Checking);
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                else
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
                = m_ClosePrc = m_CloseAllPrc = m_CancelUpdate = null;

            m_ProcessDialog.Clear();
            m_CancelDialog.Clear();

            //Errors
            m_ServerError = m_AdminError = m_GeneralUpdateError = m_DownloadError = m_SelfUpdateInstallError = null;

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

            int firstIndex;
            int currentIndex;

            firstIndex = text.IndexOf('%', 0);

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
                else
                {
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
                }

                firstIndex = currentIndex;
                tempString = null;
            }

            return returnString.ToString();
        }

        private string VariableToPretty(string variable, ref List<string> excludeVariables)
        {
            variable = variable.ToLower();

            if (excludeVariables.Contains(variable))
                return null;

            string returnValue = null;


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

                ReadLanguageFile(ref reader);
            }
            catch (Exception)
            {
                if (reader != null)
                    reader.Close();
            }
        }

        public void Open(byte[] fileData)
        {
            XmlTextReader reader = null;
            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream(fileData);

                reader = new XmlTextReader(ms);

                ReadLanguageFile(ref reader);
            }
            catch (Exception)
            {
                if (reader != null)
                    reader.Close();

                if (ms != null)
                    ms.Close();
            }
        }

        private void ReadLanguageFile(ref XmlTextReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName.Equals("Lang"))
                        m_Name = reader.ReadString();

                    if (reader.LocalName.Equals("LangEn"))
                        m_EnglishName = reader.ReadString();

                    if (reader.LocalName.Equals("ClientVersion"))
                        m_ClientVersion = reader.ReadString();

                    if (reader.LocalName.Equals("Buttons"))
                        ReadButtons(ref reader);

                    if (reader.LocalName.Equals("Screens"))
                        ReadScreens(ref reader);

                    if (reader.LocalName.Equals("Dialogs"))
                        ReadDialogs(ref reader);

                    if (reader.LocalName.Equals("Status"))
                        ReadStatus(ref reader);

                    if (reader.LocalName.Equals("Errors"))
                        ReadErrors(ref reader);

                    if (reader.LocalName.Equals("Bottoms"))
                        ReadBottoms(ref reader);
                }
            }

            reader.Close();
        }

        private void ReadButtons(ref XmlTextReader reader)
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

                    if (reader.LocalName.Equals("Update"))
                        m_UpdateButton = reader.ReadString();

                    if (reader.LocalName.Equals("Finish"))
                        m_FinishButton = reader.ReadString();

                    if (reader.LocalName.Equals("Cancel"))
                        m_CancelButton = reader.ReadString();

                    if (reader.LocalName.Equals("Close"))
                        m_ClosePrc = reader.ReadString();

                    if (reader.LocalName.Equals("CloseAll"))
                        m_CloseAllPrc = reader.ReadString();

                    if (reader.LocalName.Equals("CancelUpdate"))
                        m_CancelUpdate = reader.ReadString();
                }
            }
        }

        private void ReadScreens(ref XmlTextReader reader)
        {
            while (reader.Read())
            {
                //end of node </Screens>
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals("Screens"))
                    return;

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName.Equals("Checking"))
                        ReadScreenDialog(ref reader, ref m_Checking);

                    if (reader.LocalName.Equals("UpdateInfo"))
                        ReadScreenDialog(ref reader, ref m_UpdateInfo);

                    if (reader.LocalName.Equals("DownInstall"))
                        ReadScreenDialog(ref reader, ref m_DownInstall);

                    if (reader.LocalName.Equals("Uninstall"))
                        ReadScreenDialog(ref reader, ref m_Uninstall);

                    if (reader.LocalName.Equals("SuccessUpdate"))
                        ReadScreenDialog(ref reader, ref m_SuccessUpdate);

                    if (reader.LocalName.Equals("AlreadyLatest"))
                        ReadScreenDialog(ref reader, ref m_AlreadyLatest);

                    if (reader.LocalName.Equals("NoUpdateToLatest"))
                        ReadScreenDialog(ref reader, ref m_NoUpdateToLatest);

                    if (reader.LocalName.Equals("UpdateError"))
                        ReadScreenDialog(ref reader, ref m_UpdateError);
                }
            }
        }

        private void ReadDialogs(ref XmlTextReader reader)
        {
            while (reader.Read())
            {
                //end of node </Dialogs>
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals("Dialogs"))
                    return;

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName.Equals("Cancel"))
                        ReadScreenDialog(ref reader, ref m_CancelDialog);

                    if (reader.LocalName.Equals("Processes"))
                        ReadScreenDialog(ref reader, ref m_ProcessDialog);
                }
            }
        }

        private void ReadScreenDialog(ref XmlTextReader reader, ref ScreenDialog sd)
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

                    if (reader.LocalName.Equals("SubTitle"))
                        sd.SubTitle = reader.ReadString();

                    if (reader.LocalName.Equals("Content"))
                        sd.Content = reader.ReadString();
                }
            }
        }

        private void ReadStatus(ref XmlTextReader reader)
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

                    if (reader.LocalName.Equals("DownloadSelfUpdate"))
                        m_DownloadingSelfUpdate = reader.ReadString();

                    if (reader.LocalName.Equals("SelfUpdate"))
                        m_SelfUpdate = reader.ReadString();

                    if (reader.LocalName.Equals("Extract"))
                        m_Extract = reader.ReadString();

                    if (reader.LocalName.Equals("Processes"))
                        m_Processes = reader.ReadString();

                    if (reader.LocalName.Equals("PreExec"))
                        m_PreExec = reader.ReadString();

                    if (reader.LocalName.Equals("Files"))
                        m_Files = reader.ReadString();

                    if (reader.LocalName.Equals("Registry"))
                        m_Registry = reader.ReadString();

                    if (reader.LocalName.Equals("Optimize"))
                        m_Optimize = reader.ReadString();

                    if (reader.LocalName.Equals("TempFiles"))
                        m_TempFiles = reader.ReadString();

                    if (reader.LocalName.Equals("UninstallFiles"))
                        m_UninstallFiles = reader.ReadString();

                    if (reader.LocalName.Equals("UninstallReg"))
                        m_UninstallRegistry = reader.ReadString();
                }
            }
        }

        private void ReadErrors(ref XmlTextReader reader)
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

                    if (reader.LocalName.Equals("Admin"))
                        m_AdminError = reader.ReadString();

                    if (reader.LocalName.Equals("Update"))
                        m_GeneralUpdateError = reader.ReadString();

                    if (reader.LocalName.Equals("Download"))
                        m_DownloadError = reader.ReadString();

                    if (reader.LocalName.Equals("SelfUpdate"))
                        m_SelfUpdateInstallError = reader.ReadString();
                }
            }
        }

        private void ReadBottoms(ref XmlTextReader reader)
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

                    if (reader.LocalName.Equals("Finish"))
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

                WriteLanguageFile(ref writer);
            }
            catch (Exception)
            {
                if (writer != null)
                    writer.Close();
            }
        }

        private void WriteLanguageFile(ref XmlTextWriter writer)
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

                if (!string.IsNullOrEmpty(m_ClientVersion))
                    writer.WriteElementString("ClientVersion", m_ClientVersion);

                writer.WriteStartElement("Buttons"); //<Buttons>
                {
                    WriteString(ref writer, "Next", ref m_NextButton);
                    WriteString(ref writer, "Update", ref m_UpdateButton);
                    WriteString(ref writer, "Finish", ref m_FinishButton);
                    WriteString(ref writer, "Cancel", ref m_CancelButton);
                    WriteString(ref writer, "Close", ref m_ClosePrc);
                    WriteString(ref writer, "CloseAll", ref m_CloseAllPrc);
                    WriteString(ref writer, "CancelUpdate", ref m_CancelUpdate);
                }
                writer.WriteEndElement(); //</Buttons>

                writer.WriteStartElement("Dialogs"); //<Dialogs>
                {
                    WriteScreenDialog(ref writer, "Cancel", ref m_CancelDialog);
                    WriteScreenDialog(ref writer, "Processes", ref m_ProcessDialog);
                }
                writer.WriteEndElement(); //</Dialogs>

                writer.WriteStartElement("Errors"); //<Errors>
                {
                    WriteString(ref writer, "Admin", ref m_AdminError);
                    WriteString(ref writer, "Download", ref m_DownloadError);
                    WriteString(ref writer, "SelfUpdate", ref m_SelfUpdateInstallError);
                    WriteString(ref writer, "ServFile", ref m_ServerError);
                    WriteString(ref writer, "Update", ref m_GeneralUpdateError);
                }
                writer.WriteEndElement(); //</Errors>

                writer.WriteStartElement("Screens"); //<Screens>
                {
                    WriteScreenDialog(ref writer, "Checking", ref m_Checking);
                    WriteScreenDialog(ref writer, "UpdateInfo", ref m_UpdateInfo);
                    WriteScreenDialog(ref writer, "DownInstall", ref m_DownInstall);
                    WriteScreenDialog(ref writer, "Uninstall", ref m_Uninstall);
                    WriteScreenDialog(ref writer, "SuccessUpdate", ref m_SuccessUpdate);
                    WriteScreenDialog(ref writer, "AlreadyLatest", ref m_AlreadyLatest);
                    WriteScreenDialog(ref writer, "NoUpdateToLatest", ref m_NoUpdateToLatest);
                    WriteScreenDialog(ref writer, "UpdateError", ref m_UpdateError);
                }
                writer.WriteEndElement(); //</Screens>

                writer.WriteStartElement("Status"); //<Status>
                {
                    WriteString(ref writer, "Download", ref m_Download);
                    WriteString(ref writer, "DownloadSelfUpdate", ref m_DownloadingSelfUpdate);
                    WriteString(ref writer, "SelfUpdate", ref m_SelfUpdate);
                    WriteString(ref writer, "Extract", ref m_Extract);
                    WriteString(ref writer, "Processes", ref m_Processes);
                    WriteString(ref writer, "PreExec", ref m_PreExec);
                    WriteString(ref writer, "Files", ref m_Files);
                    WriteString(ref writer, "Registry", ref m_Registry);
                    WriteString(ref writer, "Optimize", ref m_Optimize);
                    WriteString(ref writer, "TempFiles", ref m_TempFiles);
                    WriteString(ref writer, "UninstallFiles", ref m_UninstallFiles);
                    WriteString(ref writer, "UninstallReg", ref m_UninstallRegistry);
                }
                writer.WriteEndElement(); //</Status>

                writer.WriteStartElement("Bottoms"); //<Bottoms>
                {
                    WriteString(ref writer, "Update", ref m_UpdateBottom);
                    WriteString(ref writer, "Finish", ref m_FinishBottom);
                }
                writer.WriteEndElement(); //</Bottoms>
            }
            writer.WriteEndElement(); // </Translation>

            writer.Flush();
            writer.Close();
        }

        private void WriteString(ref XmlTextWriter writer, string name, ref string value)
        {
            if (!string.IsNullOrEmpty(value))
                writer.WriteElementString(name, value);
        }

        private void WriteScreenDialog(ref XmlTextWriter writer, string name, ref ScreenDialog sd)
        {
            if (!sd.IsEmpty)
            {
                writer.WriteStartElement(name); //<name>

                WriteString(ref writer, "Title", ref sd.Title);
                WriteString(ref writer, "SubTitle", ref sd.SubTitle);
                WriteString(ref writer, "Content", ref sd.Content);

                writer.WriteEndElement(); // </name>
            }
        }

        #endregion Saving XML language file
    }
}
