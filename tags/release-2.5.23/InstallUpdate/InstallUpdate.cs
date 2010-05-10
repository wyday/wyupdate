using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using wyUpdate.Common;

namespace wyUpdate
{
    partial class InstallUpdate
    {
        #region Private Variable

        public ContainerControl Sender;

        public Delegate SenderDelegate;
        public Delegate RollbackDelegate;

        //Used for unzipping
        public string Filename;
        public string OutputDirectory;

        //Backupfiles
        public string TempDirectory;
        public string ProgramDirectory;

        public bool JustUpdatingLocal;

        //Modify registry, executing/optimizing files
        public UpdateDetails UpdtDetails;

        //for writing the client data file

        public ClientFileType ClientFileType;

        public ClientFile ClientFile;

        public bool SkipProgressReporting;


        //cancellation & pausing
        volatile bool canceled;
        //volatile bool paused = false;
        #endregion Private Variables


        public InstallUpdate(ContainerControl sender, Delegate senderDelegate)
        {
            Sender = sender;
            SenderDelegate = senderDelegate;
        }

        
        public const int TotalUpdateSteps = 7;

        public static int GetRelativeProgess(int stepOn, int stepProgress)
        {
            return ((stepOn * 100) / TotalUpdateSteps) + (stepProgress / (TotalUpdateSteps));
        }

        //Methods
        void UpdateFiles(string tempDir, string progDir, string backupFolder, List<FileFolder> rollbackList, ref int totalDone, ref int totalFiles)
        {
            DirectoryInfo tempDirInf = new DirectoryInfo(tempDir);

            //create an array of files using FileInfo object
            //get all files for the current directory
            FileInfo[] tempFiles = tempDirInf.GetFiles("*");


            for (int i = 0; i < tempFiles.Length; i++)
            {
                if (canceled)
                    break;

                int unweightedProgress = (totalDone * 100) / totalFiles;
                ThreadHelper.ReportProgress(Sender, SenderDelegate, 
                    "Updating " + tempFiles[i].Name,
                    GetRelativeProgess(4, unweightedProgress), unweightedProgress);

                if (File.Exists(Path.Combine(progDir, tempFiles[i].Name)))
                {
                    string origFile = Path.Combine(progDir, tempFiles[i].Name);

                    //backup
                    File.Copy(origFile, Path.Combine(backupFolder, tempFiles[i].Name), true);

                    FileAttributes atr = File.GetAttributes(origFile);
                    bool resetAttributes = (atr & FileAttributes.Hidden) != 0 || (atr & FileAttributes.ReadOnly) != 0 || (atr & FileAttributes.System) != 0;

                    // remove the ReadOnly & Hidden atributes temporarily
                    if (resetAttributes)
                        File.SetAttributes(origFile, FileAttributes.Normal);

                    //replace
                    File.Copy(tempFiles[i].FullName, origFile, true);

                    if (resetAttributes)
                        File.SetAttributes(origFile, atr);

                    //Old method (didn't work on Win 98/ME):
                    //File.Replace(tempFiles[i].FullName, Path.Combine(progDir, tempFiles[i].Name), Path.Combine(backupFolder, tempFiles[i].Name));
                }
                else
                {
                    //move file
                    File.Move(tempFiles[i].FullName, Path.Combine(progDir, tempFiles[i].Name));

                    //add filename to "rollback" list
                    rollbackList.Add(new FileFolder(Path.Combine(progDir, tempFiles[i].Name)));
                }

                //update % done
                totalDone++;
            }

            if (canceled)
                return;

            DirectoryInfo[] tempDirs = tempDirInf.GetDirectories("*");

            for (int i = 0; i < tempDirs.Length; i++)
            {
                if (canceled)
                    break;

                string newProgDir = Path.Combine(progDir, tempDirs[i].Name);

                if (!Directory.Exists(newProgDir))
                {
                    //create the prog subdirectory (no backup folder needed)
                    Directory.CreateDirectory(newProgDir);

                    //add to "rollback" list
                    rollbackList.Add(new FileFolder(newProgDir, true));
                }
                else
                {
                    //prog subdirectory exists, create a backup folder
                    Directory.CreateDirectory(Path.Combine(backupFolder, tempDirs[i].Name));
                }

                //backup all of the files in that directory
                UpdateFiles(tempDirs[i].FullName, newProgDir, Path.Combine(backupFolder, tempDirs[i].Name), rollbackList, ref totalDone, ref totalFiles);
            }
        }

        static void SetACLOnFolders(string basis, string extracted, string backup)
        {
            // This codes does 3 things:
            // 1. Get the ACL for the current extracted & backup folder
            // 2. Get the ACL for the target folder
            // 3. Generates a new DirectorySecurity object for both the extracted & backup folders
            //    (this solves the "ACL not in canonical form" problem http://social.msdn.microsoft.com/Forums/en/sqlgetstarted/thread/e4725808-bd1b-476a-87a4-5fd9dc24a3b7)
            // 4. Set the gathered ACLs to the new DirectorySecurity objects (repeat process for Audit Rules)

            // get the acl of basis
            AuthorizationRuleCollection acl = new DirectoryInfo(basis).GetAccessControl(AccessControlSections.All).GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            AuthorizationRuleCollection auditCL = new DirectoryInfo(basis).GetAccessControl(AccessControlSections.All).GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));

            DirectoryInfo infoEx = new DirectoryInfo(extracted);
            DirectorySecurity dsEx = infoEx.GetAccessControl(AccessControlSections.All);
            AuthorizationRuleCollection cold = dsEx.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            AuthorizationRuleCollection coldAudit = dsEx.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));
            DirectorySecurity dsExNew = new DirectorySecurity();

            // add existing ACL rules to the new DirSec obj
            foreach (FileSystemAccessRule access in cold)
                dsExNew.AddAccessRule(access);

            foreach (FileSystemAuditRule audit in coldAudit)
                dsExNew.AddAuditRule(audit);

            DirectoryInfo infoBack = new DirectoryInfo(backup);
            DirectorySecurity dsBack = infoBack.GetAccessControl(AccessControlSections.All);
            cold = dsBack.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            coldAudit = dsBack.GetAuditRules(true, true, typeof(System.Security.Principal.NTAccount));
            DirectorySecurity dsBackNew = new DirectorySecurity();

            // add existing ACL rules to the new DirSec obj
            foreach (FileSystemAccessRule access in cold)
                dsBackNew.AddAccessRule(access);

            foreach (FileSystemAuditRule audit in coldAudit)
                dsBackNew.AddAuditRule(audit);

            // add proper ACL rules to extracted & backup
            foreach (FileSystemAccessRule access in acl)
            {
                dsExNew.AddAccessRule(access);
                dsBackNew.AddAccessRule(access);
            }

            foreach (FileSystemAuditRule audit in auditCL)
            {
                dsExNew.AddAuditRule(audit);
                dsBackNew.AddAuditRule(audit);
            }

            // apply the new ACL lists to the folders
            infoEx.SetAccessControl(dsExNew);
            infoBack.SetAccessControl(dsBackNew);
        }

        public void RunUpdateFiles()
        {
            //check if folders exist, and count files to be moved
            string backupFolder = Path.Combine(TempDirectory, "backup");
            string[] backupFolders = new string[8];
            string[] origFolders = { "base", "system", "64system", "root", "appdata", "comappdata", "comdesktop", "comstartmenu" };
            string[] destFolders = { ProgramDirectory, 
                SystemFolders.GetSystem32x86(),
                SystemFolders.GetSystem32x64(),
                SystemFolders.GetRootDrive(),
                SystemFolders.GetCurrentUserAppData(),
                SystemFolders.GetCommonAppData(), 
                SystemFolders.GetCommonDesktop(), 
                SystemFolders.GetCommonProgramsStartMenu() };


            List<FileFolder> rollbackList = new List<FileFolder>();
            int totalDone = 0;

            Exception except = null; // store any errors

            try
            {
                int totalFiles = 0;
                //count the files and create backup folders
                for (int i = 0; i < origFolders.Length; i++)
                {
                    //does orig folder exist?
                    if (Directory.Exists(Path.Combine(TempDirectory, origFolders[i])))
                    {
                        //orig folder exists, set backup & orig folder locations
                        backupFolders[i] = Path.Combine(backupFolder, origFolders[i]);
                        origFolders[i] = Path.Combine(TempDirectory, origFolders[i]);
                        Directory.CreateDirectory(backupFolders[i]);

                        // set ACL on the folders so they'll have proper user access properties
                        // there's no need to set ACL for local updates
                        if (!JustUpdatingLocal)
                            SetACLOnFolders(destFolders[i], origFolders[i], backupFolders[i]);

                        //delete "newer" client, if it will overwrite this client
                        DeleteClientInPath(destFolders[i], origFolders[i]);

                        //count the total files
                        totalFiles += CountFiles(origFolders[i]);
                    }
                }


                //run the backup & replace
                for (int i = 0; i < origFolders.Length; i++)
                {
                    if (canceled)
                        break;

                    if (backupFolders[i] != null) //if the backup folder exists
                    {
                        UpdateFiles(origFolders[i], destFolders[i], backupFolders[i], rollbackList, ref totalDone, ref totalFiles);
                    }
                }

                DeleteFiles(backupFolder, rollbackList);

                InstallShortcuts(destFolders, backupFolder, rollbackList);
            }
            catch (Exception ex)
            {
                except = ex;
            }

            //write the list of newly created files and folders
            RollbackUpdate.WriteRollbackFiles(Path.Combine(backupFolder, "fileList.bak"), rollbackList);

            if (canceled || except != null)
            {
                //rollback files
                ThreadHelper.ChangeRollback(Sender, RollbackDelegate, false);
                RollbackUpdate.RollbackFiles(TempDirectory, ProgramDirectory);

                ThreadHelper.ReportError(Sender, SenderDelegate, string.Empty, except);
            }
            else
            {
                //backup & replace was successful
                ThreadHelper.ReportSuccess(Sender, SenderDelegate, string.Empty);
            }
        }

        void DeleteFiles(string backupFolder, List<FileFolder> rollbackList)
        {
            string tempPath;

            // delete the marked files
            foreach (UpdateFile file in UpdtDetails.UpdateFiles)
            {
                if (file.DeleteFile)
                {
                    tempPath = Path.Combine(backupFolder, file.RelativePath.Substring(0, file.RelativePath.LastIndexOf('\\')));

                    // check if the backup folder exists (create it if not)
                    if (!Directory.Exists(tempPath))
                        Directory.CreateDirectory(tempPath);

                    string tempFile = FixUpdateDetailsPaths(file.RelativePath);

                    if (File.Exists(tempFile))
                    {
                        //backup the file
                        File.Copy(tempFile, Path.Combine(tempPath, Path.GetFileName(tempFile)));

                        File.SetAttributes(tempFile, FileAttributes.Normal);

                        //delete the file
                        File.Delete(tempFile);
                    }
                }
            }

            //delete empty folders by working backwords to kill nested folders, e.g.:
            //  MyFolder\
            //  MyFolder\Sub1\
            //  MyFolder\Sub2\
            for (int i = UpdtDetails.FoldersToDelete.Count - 1; i >= 0; i--)
            {
                tempPath = FixUpdateDetailsPaths(UpdtDetails.FoldersToDelete[i]);

                try
                {
                    // only recursively delete StartMenu subdirectories when they're not empty
                    // otherwise the folder has to be empty to be deleted

                    Directory.Delete(tempPath, UpdtDetails.FoldersToDelete[i].StartsWith("coms"));

                    rollbackList.Add(new FileFolder(tempPath, false));
                }
                catch { }
            }
        }

        void InstallShortcuts(string[] destFolders, string backupFolder, List<FileFolder> rollbackList)
        {
            bool installDesktopShortcut = true, installStartMenuShortcut = true;

            //see if at least one previous shortcut on the desktop exists
            foreach (string shortcut in UpdtDetails.PreviousDesktopShortcuts)
            {
                if (File.Exists(Path.Combine(destFolders[6], shortcut.Substring(11))))
                {
                    installDesktopShortcut = true;
                    break;
                }

                installDesktopShortcut = false;
            }

            //see if at least one previous shortcut in the start menu folder exists
            foreach (string shortcut in UpdtDetails.PreviousSMenuShortcuts)
            {
                if (File.Exists(Path.Combine(destFolders[7], shortcut.Substring(13))))
                {
                    installStartMenuShortcut = true;
                    break;
                }

                installStartMenuShortcut = false;
            }

            // create the shortcuts
            for (int i = 0; i < UpdtDetails.ShortcutInfos.Count; i++)
            {
                //get the first 4 letters of the shortcut's path
                string tempFile = UpdtDetails.ShortcutInfos[i].RelativeOuputPath.Substring(0, 4);

                //if we can't install to that folder then continue to the next shortcut
                if (tempFile == "comd" && !installDesktopShortcut
                    || tempFile == "coms" && !installStartMenuShortcut)
                {
                    continue;
                }

                tempFile = FixUpdateDetailsPaths(UpdtDetails.ShortcutInfos[i].RelativeOuputPath);

                string tempPath;

                // see if the shortcut already exists
                if (File.Exists(tempFile))
                {
                    tempPath = Path.Combine(backupFolder, UpdtDetails.ShortcutInfos[i].RelativeOuputPath.Substring(0, UpdtDetails.ShortcutInfos[i].RelativeOuputPath.LastIndexOf('\\')));

                    // check if the backup folder exists (create it if not)
                    if (!Directory.Exists(tempPath))
                        Directory.CreateDirectory(tempPath);

                    // backup the existing shortcut
                    File.Copy(tempFile, Path.Combine(tempPath, Path.GetFileName(tempFile)), true);

                    // delete the shortcut
                    File.Delete(tempFile);
                }
                else
                    //add file to "rollback" list
                    rollbackList.Add(new FileFolder(tempFile));

                tempPath = Path.GetDirectoryName(tempFile);

                //if the folder doesn't exist
                if (!Directory.Exists(tempPath))
                {
                    //create the directory
                    Directory.CreateDirectory(tempPath);

                    //add to the rollback list
                    rollbackList.Add(new FileFolder(tempPath, true));
                }

                ShellShortcut shellShortcut = new ShellShortcut(tempFile)
                                                  {
                                                      Path = ParseText(UpdtDetails.ShortcutInfos[i].Path),
                                                      WorkingDirectory =
                                                          ParseText(UpdtDetails.ShortcutInfos[i].WorkingDirectory),
                                                      WindowStyle = UpdtDetails.ShortcutInfos[i].WindowStyle,
                                                      Description = UpdtDetails.ShortcutInfos[i].Description
                                                  };
                //shellShortcut.IconPath
                //shellShortcut.IconIndex = 0;
                shellShortcut.Save();
            }
        }


        //count files in the directory and subdirectories
        static int CountFiles(string directory)
        {
            return new DirectoryInfo(directory).GetFiles("*", SearchOption.AllDirectories).Length;
        }


        public void RunUpdateClientDataFile()
        {
            try
            {
                OutputDirectory = Path.Combine(TempDirectory, "ClientData");
                Directory.CreateDirectory(OutputDirectory);

                string oldClientFile = null;

                // see if a 1.1+ client file exists (client.wyc)
                if (ClientFileType != ClientFileType.Final
                    && File.Exists(Path.Combine(Path.GetDirectoryName(Filename), "client.wyc")))
                {
                    oldClientFile = Filename;
                    Filename = Path.Combine(Path.GetDirectoryName(Filename), "client.wyc");
                    ClientFileType = ClientFileType.Final;
                }


                if (ClientFileType == ClientFileType.PreRC2)
                {
                    //convert pre-RC2 client file by saving images to disk
                    string tempImageFilename;

                    //create the top image
                    if (ClientFile.TopImage != null)
                    {
                        ClientFile.TopImageFilename = "t.png";

                        tempImageFilename = Path.Combine(OutputDirectory, "t.png");
                        ClientFile.TopImage.Save(tempImageFilename, System.Drawing.Imaging.ImageFormat.Png);
                    }

                    //create the side image
                    if (ClientFile.SideImage != null)
                    {
                        ClientFile.SideImageFilename = "s.png";

                        tempImageFilename = Path.Combine(OutputDirectory, "s.png");
                        ClientFile.SideImage.Save(tempImageFilename, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                else
                {
                    //Extract the contents of the client data file
                    ExtractUpdateFile();

                    if (File.Exists(Path.Combine(OutputDirectory, "iuclient.iuc")))
                    {
                        // load and merge the existing file

                        ClientFile tempClientFile = new ClientFile();
                        tempClientFile.LoadClientData(Path.Combine(OutputDirectory, "iuclient.iuc"));
                        tempClientFile.InstalledVersion = ClientFile.InstalledVersion;
                        ClientFile = tempClientFile;
                    

                        File.Delete(Path.Combine(OutputDirectory, "iuclient.iuc"));
                    }
                }

                List<UpdateFile> updateDetailsFiles = UpdtDetails.UpdateFiles;

                FixUpdateFilesPaths(updateDetailsFiles);


                //write the uninstall file
                RollbackUpdate.WriteUninstallFile(Path.Combine(OutputDirectory, "uninstall.dat"), 
                    Path.Combine(TempDirectory, "backup\\regList.bak"),
                    Path.Combine(TempDirectory, "backup\\fileList.bak"), 
                    updateDetailsFiles);

                List<UpdateFile> files = new List<UpdateFile>();
                
                //add all the files in the outputDirectory
                AddFiles(OutputDirectory.Length + 1, OutputDirectory, files);

                //recompress all the client data files
                string tempClient = Path.Combine(OutputDirectory, "client.file");
                ClientFile.SaveClientFile(files, tempClient);

                

                // overrite existing client.wyc, while keeping the file attributes

                FileAttributes atr = FileAttributes.Normal;

                if(File.Exists(Filename))
                    atr = File.GetAttributes(Filename);

                bool resetAttributes = (atr & FileAttributes.Hidden) != 0 || (atr & FileAttributes.ReadOnly) != 0 || (atr & FileAttributes.System) != 0;

                // remove the ReadOnly & Hidden atributes temporarily
                if (resetAttributes)
                    File.SetAttributes(Filename, FileAttributes.Normal);

                //replace the original
                File.Copy(tempClient, Filename, true);

                if (resetAttributes)
                    File.SetAttributes(Filename, atr);


                if (oldClientFile != null)
                {
                    // delete the old client file
                    File.Delete(oldClientFile);
                }
            }
            catch { }

            ThreadHelper.ReportSuccess(Sender, SenderDelegate, string.Empty);
        }

        //creates list of files to add to client data file
        static void AddFiles(int charsToTrim, string dir, List<UpdateFile> files)
        {
            string[] filenames = Directory.GetFiles(dir);
            string[] dirs = Directory.GetDirectories(dir);

            foreach (string file in filenames)
            {
                files.Add(new UpdateFile { Filename = file, RelativePath = file.Substring(charsToTrim) });
            }

            foreach (string directory in dirs)
            {
                AddFiles(charsToTrim, directory, files);
            }
        }


        public void RunDeleteTemporary()
        {
            try
            {
                //delete the temp directory
                Directory.Delete(TempDirectory, true);
            }
            catch { }

            ThreadHelper.ReportSuccess(Sender, SenderDelegate, string.Empty);
        }

        public void RunUninstall()
        {
            List<UninstallFileInfo> filesToUninstall = new List<UninstallFileInfo>();
            List<string> foldersToDelete = new List<string>();

            List<RegChange> registryToDelete = new List<RegChange>();

            //Load the list of files, folders etc. from the client file (Filename)
            RollbackUpdate.ReadUninstallData(Filename, filesToUninstall, foldersToDelete, registryToDelete);

            //uninstall files
            foreach (UninstallFileInfo file in filesToUninstall)
            {
                try
                {
                    if (file.UnNGENFile)
                        NGenUninstall(file.Path, file);

                    if (file.DeleteFile)
                        File.Delete(file.Path);
                }
                catch { }
            }

            //uninstall folders
            for (int i = foldersToDelete.Count-1; i >= 0; i--)
            {
                //delete the last folder first (this fixes the problem of nested folders)
                try
                {
                    //directory must be empty in order to delete it
                    Directory.Delete(foldersToDelete[i]);
                }
                catch { }
            }


            //tell the sender that we're uninstalling reg now:
            Sender.BeginInvoke(SenderDelegate, new object[] { 0, 1, "", null });

            //uninstall registry
            foreach (RegChange reg in registryToDelete)
            {
                try
                {
                    reg.ExecuteOperation();
                }
                catch { }
            }

            //All done
            Sender.BeginInvoke(SenderDelegate, new object[] { 0, 2, "", null });
        }

        public void RunPreExecute()
        {
            // simply update the progress bar to show the 3rd step is entirely complete
            ThreadHelper.ReportProgress(Sender, SenderDelegate, string.Empty, GetRelativeProgess(3, 0), 0);

            for (int i = 0; i < UpdtDetails.UpdateFiles.Count; i++)
            {
                if (UpdtDetails.UpdateFiles[i].Execute && 
                    UpdtDetails.UpdateFiles[i].ExBeforeUpdate)
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                                               {
                                                   // use the absolute path
                                                   FileName =
                                                       FixUpdateDetailsPaths(UpdtDetails.UpdateFiles[i].RelativePath)
                                               };

                    if (!string.IsNullOrEmpty(psi.FileName))
                    {
                        //command line arguments
                        if (!string.IsNullOrEmpty(UpdtDetails.UpdateFiles[i].CommandLineArgs))
                            psi.Arguments = ParseText(UpdtDetails.UpdateFiles[i].CommandLineArgs);

                        psi.WindowStyle = UpdtDetails.UpdateFiles[i].ProcessWindowStyle;

                        //start the process
                        Process p = Process.Start(psi);

                        if (UpdtDetails.UpdateFiles[i].WaitForExecution && p != null)
                            p.WaitForExit();
                    }
                }
            }

            ThreadHelper.ReportSuccess(Sender, SenderDelegate, string.Empty);
        }


        void FixUpdateFilesPaths(List<UpdateFile> updateFiles)
        {
            UpdateFile tempUFile;

            //replace every relative path with an absolute path
            for (int i = 0; i < updateFiles.Count; i++)
            {
                if (updateFiles[i].IsNETAssembly)
                {
                    tempUFile = updateFiles[i];

                    tempUFile.Filename = FixUpdateDetailsPaths(tempUFile.RelativePath);

                    updateFiles[i] = tempUFile;
                }
            }
        }

        //TODO: make this function faster (it gets called N times (N files). Store folders in a hash table
        string FixUpdateDetailsPaths(string relPath)
        {
            if (relPath.Length < 4)
                return null;

            switch (relPath.Substring(0,4))
            {
                case "base":
                    return Path.Combine(ProgramDirectory, relPath.Substring(5));
                case "syst": //system (32-bit)
                    return Path.Combine(SystemFolders.GetSystem32x86(), relPath.Substring(7));
                case "64sy": //64system (64-bit)
                    return Path.Combine(SystemFolders.GetSystem32x64(), relPath.Substring(9));
                case "temp":
                    return Path.Combine(TempDirectory, relPath);
                case "appd": //appdata
                    return Path.Combine(SystemFolders.GetCurrentUserAppData(), relPath.Substring(8));
                case "coma": //comappdata
                    return Path.Combine(SystemFolders.GetCommonAppData(), relPath.Substring(11));
                case "comd": //comdesktop
                    return Path.Combine(SystemFolders.GetCommonDesktop(), relPath.Substring(11));
                case "coms": //comstartmenu
                    return Path.Combine(SystemFolders.GetCommonProgramsStartMenu(), relPath.Substring(13));
                case "root": //root windows (e.g. C:\)
                    return Path.Combine(SystemFolders.GetRootDrive(), relPath.Substring(5));
            }

            return null;
        }

        //handle thread cancelation
        public void Cancel()
        {
            canceled = true;
        }

        #region RelativePaths



        static void DeleteClientInPath(string destPath, string origPath)
        {
            string tempClientLoc = ClientInTempBase(destPath, origPath);

            if (tempClientLoc != null)
                File.Delete(tempClientLoc);
        }

        //returns a non-null string filename of the Client in the tempbase
        //if the Running Client will be overwritten by the Temp Client
        static string ClientInTempBase(string actualBase, string tempBase)
        {
            //relative path from origFolder to client location
            StringBuilder strBuild = new StringBuilder(SystemFolders.MAX_PATH);
            string tempStr = Assembly.GetExecutingAssembly().Location;

            //find the relativity of the actualBase and this running client
            bool bRet = SystemFolders.PathRelativePathTo(
                strBuild,
                actualBase, (uint)SystemFolders.PathAttribute.Directory,
                tempStr, (uint)SystemFolders.PathAttribute.File
            );

            if (bRet && strBuild.Length >= 2)
            {
                //get the first two characters
                tempStr = strBuild.ToString().Substring(0, 2);

                if (tempStr == @".\") //if client is in the destPath
                {
                    tempStr = Path.Combine(tempBase, strBuild.ToString());

                    if (File.Exists(tempStr))
                        return tempStr;
                }
            }

            return null;
        }

        #endregion Relativepaths

        #region Parse variables

        RegChange ParseRegChange(RegChange reg)
        {
            if (reg.RegValueKind == RegistryValueKind.MultiString ||
                reg.RegValueKind == RegistryValueKind.String)
            {
                reg.ValueData = ParseText((string)reg.ValueData);
            }
            return reg;
        }

        string ParseText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            List<string> excludeVariables = new List<string>();

            return ParseVariableText(text, excludeVariables);
        }

        string ParseVariableText(string text, List<string> excludeVariables)
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
                string tempString = VariableToPretty(text.Substring(firstIndex + 1, currentIndex - firstIndex - 1), excludeVariables);

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
                        return returnString.ToString();
                }

                firstIndex = currentIndex;
            }

            return returnString.ToString();
        }

        string VariableToPretty(string variable, List<string> excludeVariables)
        {
            variable = variable.ToLower();

            if (excludeVariables.Contains(variable))
                return null;

            string returnValue;

            excludeVariables.Add(variable);

            switch (variable)
            {
                case "basedir":
                    returnValue = ProgramDirectory;

                    // add a trailing slash if it doesn't exist
                    if (returnValue[returnValue.Length - 1] != '\\')
                        returnValue += '\\';

                    break;
                default:
                    excludeVariables.RemoveAt(excludeVariables.Count - 1);
                    return null;
            }

            //allow the variable to be processed again
            excludeVariables.Remove(variable);

            return returnValue;
        }

        #endregion Parse variables

        #region Execute Commands

        static void ParseCommandText(string text)
        {
            int lastDollarIndex = text.LastIndexOf('$');

            //if no $'s found
            if (lastDollarIndex == -1)
                return;

            do
            {
                int beginParen = text.IndexOf('(', lastDollarIndex);

                if (beginParen != -1)
                {
                    //get the text between the '$' and the '('
                    CommandName currCommand = GetCommandName(text.Substring(lastDollarIndex + 1, beginParen - lastDollarIndex - 1));

                    if (currCommand != CommandName.NULL)
                    {
                        int endParen = IndexOfNonEnclosed(')', text, beginParen);

                        if (endParen != -1)
                        {
                            //replace the command, contents, and parenthesis
                            //with the modified contents
                            ExecuteTextCommand(currCommand);
                            text = text.Remove(lastDollarIndex, endParen - lastDollarIndex + 1);
                        }
                    }
                }

                lastDollarIndex = LastIndexOfReal('$', text, 0, lastDollarIndex - 1);

            } while (lastDollarIndex != -1);
        }

        static int IndexOfNonEnclosed(char ch, string str, int startIndex)
        {
            for (int i = startIndex; i < str.Length; i++)
            {
                if (str[i] == ch)
                {
                    //if not the first of last char
                    if (i > 0 && i < str.Length - 2)
                    {
                        //if not enclosed in single quotes
                        if (str[i - 1] != '\'' || str[i + 1] != '\'')
                            return i;
                    }
                    else
                        return i;
                }
            }

            return -1;
        }

        static int LastIndexOfReal(char ch, string str, int startIndex, int endIndex)
        {
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (str[i] == ch)
                    return i;
            }

            return -1;
        }

        public enum CommandName { NULL = -1, refreshicons }

        static CommandName GetCommandName(string command)
        {
            CommandName name = CommandName.NULL;

            try
            {
                name = (CommandName)Enum.Parse(typeof(CommandName), command, true);
            }
            catch { }

            return name;
        }

        static void ExecuteTextCommand(CommandName command)
        {
            switch (command)
            {
                case CommandName.refreshicons:
                    //refresh shell icons
                    SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
                    break;
            }
        }

        [DllImport("shell32.dll")]
        static extern void SHChangeNotify(long wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        #endregion
    }
}
