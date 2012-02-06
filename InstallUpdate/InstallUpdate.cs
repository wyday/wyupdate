using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using wyUpdate.Common;

namespace wyUpdate
{
    public enum ProgressStatus { None, Success, Failure, SharingViolation }

    public delegate void ProgressChangedHandler(int percentDone, int unweightedPercent, string extraStatus, ProgressStatus status, Object payload);
    public delegate void ChangeRollbackDelegate(bool rbRegistry);

    partial class InstallUpdate
    {
        readonly BackgroundWorker bw = new BackgroundWorker();
        public event ProgressChangedHandler ProgressChanged;
        public event ChangeRollbackDelegate Rollback;

        //Used for unzipping
        public string Filename;
        public string OutputDirectory;

        //Backupfiles
        public string TempDirectory;
        public string ProgramDirectory;

        // only used in UpdateFiles()
        public bool IsAdmin;
        public IntPtr MainWindowHandle;

        //Modify registry, executing/optimizing files
        public UpdateDetails UpdtDetails;

        //for writing the client data file

        public ClientFileType ClientFileType;

        public ClientFile ClientFile;

        public bool SkipProgressReporting;

        public bool SkipUIReporting;

        //cancellation & pausing
        volatile bool paused;


        public InstallUpdate()
        {
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
        }

        void bw_DoWorkUpdateFiles(object sender, DoWorkEventArgs e)
        {
            //check if folders exist, and count files to be moved
            string backupFolder = Path.Combine(TempDirectory, "backup");
            string[] backupFolders = new string[11];
            string[] origFolders = { "base", "system", "64system", "root", "appdata", "lappdata", "comappdata", "comdesktop", "comstartmenu", "cp86", "cp64" };
            string[] destFolders = { ProgramDirectory, 
                SystemFolders.GetSystem32x86(),
                SystemFolders.GetSystem32x64(),
                SystemFolders.GetRootDrive(),
                SystemFolders.GetCurrentUserAppData(),
                SystemFolders.GetCurrentUserLocalAppData(),
                SystemFolders.GetCommonAppData(), 
                SystemFolders.GetCommonDesktop(), 
                SystemFolders.GetCommonProgramsStartMenu(),
                SystemFolders.GetCommonProgramFilesx86(),
                SystemFolders.GetCommonProgramFilesx64()
            };

            List<FileFolder> rollbackList = new List<FileFolder>();
            int totalDone = 0;

            Exception except = null;

            try
            {
                int totalFiles = 0;

                // count the files and create backup folders
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
                        if (IsAdmin)
                            SetACLOnFolders(destFolders[i], origFolders[i], backupFolders[i]);

                        // delete "newer" client, if it will overwrite this client
                        DeleteClientInPath(destFolders[i], origFolders[i]);

                        //count the total files
                        totalFiles += CountFiles(origFolders[i]);
                    }
                }


                //run the backup & replace
                for (int i = 0; i < origFolders.Length; i++)
                {
                    if (IsCancelled())
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

            if (IsCancelled() || except != null)
            {
                // rollback files
                bw.ReportProgress(1, false);
                RollbackUpdate.RollbackFiles(TempDirectory, ProgramDirectory);

                // rollback unregged COM
                RollbackUpdate.RollbackUnregedCOM(TempDirectory);

                // rollback stopped services
                RollbackUpdate.RollbackStoppedServices(TempDirectory);

                bw.ReportProgress(0, new object[] { -1, -1, string.Empty, ProgressStatus.Failure, except });
            }
            else
            {
                // backup & replace was successful
                bw.ReportProgress(0, new object[] { -1, -1, string.Empty, ProgressStatus.Success, null });
            }
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 0)
            {
                object[] arr = (object[])e.UserState;

                if (ProgressChanged != null)
                    ProgressChanged((int)arr[0], (int)arr[1], (string)arr[2], (ProgressStatus)arr[3], arr[4]);
            }
            else if (e.ProgressPercentage == 1)
            {
                if (Rollback != null)
                    Rollback((bool)e.UserState);
            }
        }

        void bw_RunWorkerCompletedUpdateFiles(object sender, RunWorkerCompletedEventArgs e)
        {
            bw.DoWork -= bw_DoWorkUpdateFiles;
            bw.ProgressChanged -= bw_ProgressChanged;
            bw.RunWorkerCompleted -= bw_RunWorkerCompletedUpdateFiles;
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
                if (IsCancelled())
                    break;

                int unweightedProgress = (totalDone * 100) / totalFiles;
                bw.ReportProgress(0, new object[] { GetRelativeProgess(4, unweightedProgress), unweightedProgress, "Updating " + tempFiles[i].Name, ProgressStatus.None, null });

                if (File.Exists(Path.Combine(progDir, tempFiles[i].Name)))
                {
                    int retriedTimes = 0;

                    while (true)
                    {
                        try
                        {
                            string origFile = Path.Combine(progDir, tempFiles[i].Name);

                            // backup
                            File.Copy(origFile, Path.Combine(backupFolder, tempFiles[i].Name), true);

                            FileAttributes atr = File.GetAttributes(origFile);
                            bool resetAttributes = (atr & FileAttributes.Hidden) != 0 || (atr & FileAttributes.ReadOnly) != 0 ||
                                                   (atr & FileAttributes.System) != 0;

                            // remove the ReadOnly & Hidden atributes temporarily
                            if (resetAttributes)
                                File.SetAttributes(origFile, FileAttributes.Normal);

                            // replace
                            File.Copy(tempFiles[i].FullName, origFile, true);

                            if (resetAttributes)
                                File.SetAttributes(origFile, atr);
                        }
                        catch (IOException IOEx)
                        {
                            int HResult = Marshal.GetHRForException(IOEx);

                            // if sharing violation
                            if ((HResult & 0xFFFF) == 32)
                            {
                                if (!SkipUIReporting)
                                {
                                    // notify main window of sharing violation
                                    bw.ReportProgress(0, new object[] { -1, -1, string.Empty, ProgressStatus.SharingViolation, Path.Combine(progDir, tempFiles[i].Name) });
                                }

                                // sleep for 1 second
                                Thread.Sleep(1000);

                                // stop waiting if cancelled
                                if (IsCancelled())
                                    break;

                                // if we're skipping UI and we've already waited 20 seconds for a file to be released
                                // then throw the exception, rollback updates, etc
                                if (SkipUIReporting && retriedTimes == 20)
                                    throw;

                                // otherwise, retry file copy
                                ++retriedTimes;
                                continue;
                            }

                            throw;
                        }

                        break;
                    }
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

            if (IsCancelled())
                return;

            DirectoryInfo[] tempDirs = tempDirInf.GetDirectories("*");

            for (int i = 0; i < tempDirs.Length; i++)
            {
                if (IsCancelled())
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
            bw.DoWork += bw_DoWorkUpdateFiles;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_RunWorkerCompletedUpdateFiles;

            bw.RunWorkerAsync();
        }

        void DeleteFiles(string backupFolder, List<FileFolder> rollbackList)
        {
            string tempPath;

            string wyUpdateLoc = Assembly.GetExecutingAssembly().Location;

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

                    // if the user is trying to delete this file, then skip it
                    if (string.Compare(wyUpdateLoc, tempFile, StringComparison.OrdinalIgnoreCase) == 0)
                        continue;

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





        public void RunDeleteTemporary()
        {
            bw.DoWork += bw_DoWorkDeleteTemporary;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_RunWorkerCompletedDeleteTemporary;

            bw.RunWorkerAsync();
        }

        void bw_DoWorkDeleteTemporary(object sender, DoWorkEventArgs e)
        {
            try
            {
                //delete the temp directory
                Directory.Delete(TempDirectory, true);
            }
            catch { }

            bw.ReportProgress(0, new object[] { -1, -1, string.Empty, ProgressStatus.Success, null });
        }

        void bw_RunWorkerCompletedDeleteTemporary(object sender, RunWorkerCompletedEventArgs e)
        {
            bw.DoWork -= bw_DoWorkDeleteTemporary;
            bw.ProgressChanged -= bw_ProgressChanged;
            bw.RunWorkerCompleted -= bw_RunWorkerCompletedDeleteTemporary;
        }

        public void RunUninstall()
        {
            bw.DoWork += bw_DoWorkUninstall;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_RunWorkerCompletedUninstall;

            bw.RunWorkerAsync();
        }

        void bw_DoWorkUninstall(object sender, DoWorkEventArgs e)
        {
            List<UninstallFileInfo> filesToUninstall = new List<UninstallFileInfo>();
            List<string> foldersToDelete = new List<string>();
            List<RegChange> registryToDelete = new List<RegChange>();
            List<UninstallFileInfo> comDllsToUnreg = new List<UninstallFileInfo>();
            List<string> servicesToStop = new List<string>();

            // Load the list of files, folders etc. from the client file (Filename)
            RollbackUpdate.ReadUninstallData(Filename, filesToUninstall, foldersToDelete, registryToDelete, comDllsToUnreg, servicesToStop);

            // stop the services
            foreach (string service in servicesToStop)
            {
                try
                {
                    // stop the service
                    using (ServiceController srvc = new ServiceController(service))
                    {
                        srvc.Stop();
                        srvc.WaitForStatus(ServiceControllerStatus.Stopped);
                    }
                }
                catch { }
            }

            // unregister COM files
            foreach (var uninstallFileInfo in comDllsToUnreg)
            {
                try
                {
                    RegisterDllServer(uninstallFileInfo.Path, true);
                }
                catch { }
            }

            // uninstall files
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
            for (int i = foldersToDelete.Count - 1; i >= 0; i--)
            {
                //delete the last folder first (this fixes the problem of nested folders)
                try
                {
                    //directory must be empty in order to delete it
                    Directory.Delete(foldersToDelete[i]);
                }
                catch { }
            }


            // tell the sender that we're uninstalling reg now:
            bw.ReportProgress(0, new object[] { -1, -1, string.Empty, ProgressStatus.None, null });

            //uninstall registry
            foreach (RegChange reg in registryToDelete)
            {
                try
                {
                    reg.ExecuteOperation();
                }
                catch { }
            }

            // All done
            bw.ReportProgress(0, new object[] { -1, -1, string.Empty, ProgressStatus.Success, null });
        }

        void bw_RunWorkerCompletedUninstall(object sender, RunWorkerCompletedEventArgs e)
        {
            bw.DoWork -= bw_DoWorkUninstall;
            bw.ProgressChanged -= bw_ProgressChanged;
            bw.RunWorkerCompleted -= bw_RunWorkerCompletedUninstall;
        }

        public void RunPreExecute()
        {
            bw.DoWork += bw_DoWorkPreExecute;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_RunWorkerCompletedPreExecute;

            bw.RunWorkerAsync();
        }

        void bw_DoWorkPreExecute(object sender, DoWorkEventArgs e)
        {
            // simply update the progress bar to show the 3rd step is entirely complete
            bw.ReportProgress(0, new object[] { GetRelativeProgess(3, 0), 0, string.Empty, ProgressStatus.None, null });

            List<UninstallFileInfo> rollbackCOM = new List<UninstallFileInfo>();
            Exception except = null;

            for (int i = 0; i < UpdtDetails.UpdateFiles.Count; i++)
            {
                bool unregister = (UpdtDetails.UpdateFiles[i].RegisterCOMDll &
                                   (COMRegistration.UnRegister | COMRegistration.PreviouslyRegistered)) != 0;

                // skip non-executing files, skip execute "after" updates
                if ((!UpdtDetails.UpdateFiles[i].Execute || !UpdtDetails.UpdateFiles[i].ExBeforeUpdate) && !unregister)
                    continue;

                // form the absolute path of the file to execute or unregister
                string fullFile = FixUpdateDetailsPaths(UpdtDetails.UpdateFiles[i].RelativePath);

                if (string.IsNullOrEmpty(fullFile))
                    continue;

                if (!unregister)
                {
                    try
                    {
                        // we only support starting non-elevated on Vista+
                        // And the user must be an admin (otherwise starting as the same elevation as wyUpdate is ample)
                        if (UpdtDetails.UpdateFiles[i].ElevationType == ElevationType.NotElevated
                            && IsAdmin
                            && VistaTools.AtLeastVista())
                        {
                            LimitedProcess.Start(fullFile,
                                                 string.IsNullOrEmpty(UpdtDetails.UpdateFiles[i].CommandLineArgs)
                                                     ? null
                                                     : ParseText(UpdtDetails.UpdateFiles[i].CommandLineArgs), false,
                                                     UpdtDetails.UpdateFiles[i].WaitForExecution,
                                                     UpdtDetails.UpdateFiles[i].ProcessWindowStyle);
                        }
                        else // Same as wyUpdate or elevated
                        {
                            ProcessStartInfo psi = new ProcessStartInfo
                                                       {
                                                           FileName = fullFile,
                                                           WindowStyle = UpdtDetails.UpdateFiles[i].ProcessWindowStyle
                                                       };

                            // command line arguments
                            if (!string.IsNullOrEmpty(UpdtDetails.UpdateFiles[i].CommandLineArgs))
                                psi.Arguments = ParseText(UpdtDetails.UpdateFiles[i].CommandLineArgs);

                            // only elevate if the current process isn't already elevated
                            if (!IsAdmin && UpdtDetails.UpdateFiles[i].ElevationType == ElevationType.Elevated)
                            {
                                psi.Verb = "runas";
                                psi.ErrorDialog = true;
                                psi.ErrorDialogParentHandle = MainWindowHandle;
                            }

                            //start the process
                            Process p = Process.Start(psi);

                            if (UpdtDetails.UpdateFiles[i].WaitForExecution && p != null)
                            {
                                p.WaitForExit();

                                // if we're rolling back on non-zero return codes, the return code is non-zero, and it's not in the exception list
                                if (UpdtDetails.UpdateFiles[i].RollbackOnNonZeroRet && p.ExitCode != 0 && (UpdtDetails.UpdateFiles[i].RetExceptions == null
                                    || !UpdtDetails.UpdateFiles[i].RetExceptions.Contains(p.ExitCode)))
                                {
                                    except = new Exception("\"" + psi.FileName + "\" returned " + p.ExitCode + ".");
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // failure when executing the file
                        except = new Exception("Failed to execute the file \"" + fullFile + "\": " + ex.Message, ex);
                        break;
                    }
                }
                else // unregistering DLL
                {
                    try
                    {
                        RegisterDllServer(fullFile, true);

                        // add to the rollback list
                        rollbackCOM.Add(new UninstallFileInfo { Path = fullFile, RegisterCOMDll = COMRegistration.Register });
                    }
                    catch (Exception ex)
                    {
                        except = ex;
                        break;
                    }
                }
            }

            // save rollback info
            RollbackUpdate.WriteRollbackCOM(Path.Combine(TempDirectory, "backup\\unreggedComList.bak"), rollbackCOM);

            if (IsCancelled() || except != null)
            {
                // rollback unregged COM
                bw.ReportProgress(1, false);
                RollbackUpdate.RollbackUnregedCOM(TempDirectory);

                // rollback stopped services
                RollbackUpdate.RollbackStoppedServices(TempDirectory);

                bw.ReportProgress(0, new object[] { -1, -1, string.Empty, ProgressStatus.Failure, except });
            }
            else
            {
                // registry modification completed sucessfully
                bw.ReportProgress(0, new object[] { -1, -1, string.Empty, ProgressStatus.Success, null });
            }
        }

        void bw_RunWorkerCompletedPreExecute(object sender, RunWorkerCompletedEventArgs e)
        {
            bw.DoWork -= bw_DoWorkPreExecute;
            bw.ProgressChanged -= bw_ProgressChanged;
            bw.RunWorkerCompleted -= bw_RunWorkerCompletedPreExecute;
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
                case "lapp": //lappdata
                    return Path.Combine(SystemFolders.GetCurrentUserLocalAppData(), relPath.Substring(9));
                case "coma": //comappdata
                    return Path.Combine(SystemFolders.GetCommonAppData(), relPath.Substring(11));
                case "comd": //comdesktop
                    return Path.Combine(SystemFolders.GetCommonDesktop(), relPath.Substring(11));
                case "coms": //comstartmenu
                    return Path.Combine(SystemFolders.GetCommonProgramsStartMenu(), relPath.Substring(13));
                case "root": //root windows (e.g. C:\)
                    return Path.Combine(SystemFolders.GetRootDrive(), relPath.Substring(5));
                case "cp86": //cp86 == common program files (x86)
                    return Path.Combine(SystemFolders.GetCommonProgramFilesx86(), relPath.Substring(5));
                case "cp64": //cp64 == common program files (x64)
                    return Path.Combine(SystemFolders.GetCommonProgramFilesx64(), relPath.Substring(5));
            }

            return null;
        }

        /// <summary>
        /// Cancel the current update step.
        /// </summary>
        public void Cancel()
        {
            bw.CancelAsync();
        }

        /// <summary>
        /// Pause or Unpause the current update step.
        /// </summary>
        /// <param name="pause">Should we pause or unpause?</param>
        public void Pause(bool pause)
        {
            paused = pause;
        }

        /// <summary>
        /// Returns if the progress is cancelled. Waits if paused.
        /// </summary>
        /// <returns>True if cancelled.</returns>
        bool IsCancelled()
        {
            while (paused)
            {
                if (bw.CancellationPending)
                    return true;

                Thread.Sleep(1000);
            }

            return bw.CancellationPending;
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
    }
}
