using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ionic.Zip;
using wyUpdate.Common;

namespace wyUpdate
{
    public class UninstallFileInfo
    {
        public string Path;
        public bool DeleteFile;
        public bool UnNGENFile;
        public CPUVersion CPUVersion;
        public FrameworkVersion FrameworkVersion;
        public COMRegistration RegisterCOMDll;

        public static UninstallFileInfo Read(Stream fs)
        {
            UninstallFileInfo tempUFI = new UninstallFileInfo();
            
            //read in the fileinfo

            byte bType = (byte)fs.ReadByte();
            while (!ReadFiles.ReachedEndByte(fs, bType, 0x9A)) //if end byte is detected, bail out
            {
                switch (bType)
                {
                    case 0x01: //file path
                        tempUFI.Path = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x02: //delete the file?
                        tempUFI.DeleteFile = ReadFiles.ReadBool(fs);
                        break;
                    case 0x03: //un-NGEN the file?
                        tempUFI.UnNGENFile = ReadFiles.ReadBool(fs);
                        break;
                    case 0x04:
                        tempUFI.CPUVersion = (CPUVersion) ReadFiles.ReadInt(fs);
                        break;
                    case 0x05:
                        tempUFI.FrameworkVersion = (FrameworkVersion) ReadFiles.ReadInt(fs);
                        break;
                    case 0x06:
                        tempUFI.RegisterCOMDll = (COMRegistration) ReadFiles.ReadInt(fs);
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            return tempUFI;
        }

        public void Write(Stream fs, bool comFiles)
        {
            //beginning of the uninstall file info
            if (comFiles)
                fs.WriteByte(0x8B);
            else
                fs.WriteByte(0x8A);

            //path to the file
            WriteFiles.WriteDeprecatedString(fs, 0x01, Path);

            //delete the file?
            if (DeleteFile)
                WriteFiles.WriteBool(fs, 0x02, true);

            if (UnNGENFile)
            {
                WriteFiles.WriteBool(fs, 0x03, true);

                // the CPU version of the file to un-ngen
                WriteFiles.WriteInt(fs, 0x04, (int) CPUVersion);

                WriteFiles.WriteInt(fs, 0x05, (int)FrameworkVersion);
            }

            if (RegisterCOMDll != COMRegistration.None)
                WriteFiles.WriteInt(fs, 0x06, (int) RegisterCOMDll);

            //end of uninstall file info
            fs.WriteByte(0x9A);
        }
    }

    class RollbackUpdate
    {
        public static void RollbackFiles(string m_TempDirectory, string m_ProgramDirectory)
        {
            string backupFolder = Path.Combine(m_TempDirectory, "backup");

            // read in the list of files to delete
            List<string> fileList = new List<string>(),
                foldersToDelete = new List<string>(),
                foldersToCreate = new List<string>();
            try
            {
                ReadRollbackFiles(Path.Combine(backupFolder, "fileList.bak"), fileList, foldersToDelete, foldersToCreate);
            }
            catch { }

            //delete the files
            foreach (string file in fileList)
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch { }
            }

            //delete the folders
            foreach (string folder in foldersToDelete)
            {
                //TODO: test rolling back read-only / hidden folders
                try { Directory.Delete(folder, true); }
                catch { }
            }

            //create the folders
            foreach (string folder in foldersToCreate)
            {
                try { Directory.CreateDirectory(folder); }
                catch { }
            }

            //restore old versions
            string[] backupFolders = {
                                         Path.Combine(backupFolder, "base"),
                                         Path.Combine(backupFolder, "system"),
                                         Path.Combine(backupFolder, "64system"),
                                         Path.Combine(backupFolder, "root"),
                                         Path.Combine(backupFolder, "appdata"),
                                         Path.Combine(backupFolder, "comappdata"),
                                         Path.Combine(backupFolder, "comdesktop"),
                                         Path.Combine(backupFolder, "comstartmenu"),
                                         Path.Combine(backupFolder, "cp86"),
                                         Path.Combine(backupFolder, "cp64")
                                     };
            string[] destFolders = {
                                       m_ProgramDirectory,
                                       SystemFolders.GetSystem32x86(),
                                       SystemFolders.GetSystem32x64(),
                                       SystemFolders.GetRootDrive(),
                                       SystemFolders.GetCurrentUserAppData(),
                                       SystemFolders.GetCommonAppData(),
                                       SystemFolders.GetCommonDesktop(),
                                       SystemFolders.GetCommonProgramsStartMenu(),
                                       SystemFolders.GetCommonProgramFilesx86(),
                                       SystemFolders.GetCommonProgramFilesx64()
                                   };

            for (int i = 0; i < backupFolders.Length; i++)
            {
                // only backup if the back-folder & dest-folder exists (i.e. the 64-bit system32 folder)
                if (Directory.Exists(backupFolders[i]) && destFolders[i] != null)
                    RestoreFiles(destFolders[i], backupFolders[i]);
            }
        }

        public static void RestoreFiles(string destDir, string backupDir)
        {
            DirectoryInfo backupDirInf = new DirectoryInfo(backupDir);

            //get all the files in the backup directory
            FileInfo[] backupFiles = backupDirInf.GetFiles("*");

            foreach (FileInfo file in backupFiles)
            {
                try
                {
                    string origFile = Path.Combine(destDir, file.Name);

                    FileAttributes atr = file.Attributes;
                    bool resetAttributes = (atr & FileAttributes.Hidden) != 0 || (atr & FileAttributes.ReadOnly) != 0 || (atr & FileAttributes.System) != 0;

                    // remove the ReadOnly & Hidden atributes temporarily
                    if (resetAttributes)
                        File.SetAttributes(origFile, FileAttributes.Normal);

                    //overwrite the existing failed/cancelled update
                    File.Copy(file.FullName, origFile, true);

                    if (resetAttributes)
                        File.SetAttributes(origFile, atr);
                }
                catch { }
            }

            //backup all of the subdirectories
            DirectoryInfo[] backupSubDirs = backupDirInf.GetDirectories("*");

            foreach (DirectoryInfo dir in backupSubDirs)
            {
                RestoreFiles(Path.Combine(destDir, dir.Name), dir.FullName);
            }
        }

        public static void RollbackRegistry(string m_TempDirectory)
        {
            List<RegChange> rollbackRegistry = new List<RegChange>();

            try
            {
                ReadRollbackRegistry(Path.Combine(m_TempDirectory, "backup\\regList.bak"), rollbackRegistry);
            }
            catch { }

            // roll the registry back
            foreach (RegChange regCh in rollbackRegistry)
            {
                try
                {
                    regCh.ExecuteOperation();
                }
                catch { }
            }
        }

        public static void RollbackUnregedCOM(string tempDir)
        {
            List<UninstallFileInfo> rollbackList = new List<UninstallFileInfo>();

            try
            {
                ReadRollbackCOM(Path.Combine(tempDir, "backup\\unreggedComList.bak"), rollbackList);
            }
            catch { }

            // re-reg COM dlls
            foreach (UninstallFileInfo fileinfo in rollbackList)
            {
                try
                {
                    InstallUpdate.RegisterDllServer(fileinfo.Path, false);
                }
                catch { }
            }

            // Delete temporary files
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch { }
        }

        public static void RollbackRegedCOM(string tempDir)
        {
            List<UninstallFileInfo> rollbackList = new List<UninstallFileInfo>();

            try
            {
                ReadRollbackCOM(Path.Combine(tempDir, "backup\\reggedComList.bak"), rollbackList);
            }
            catch { }

            // re-reg COM dlls
            foreach (UninstallFileInfo fileinfo in rollbackList)
            {
                try
                {
                    InstallUpdate.RegisterDllServer(fileinfo.Path, true);
                }
                catch { }
            }
        }

        #region Write/Read RollbackRegistry

        public static void WriteRollbackRegistry(string fileName, List<RegChange> rollbackRegistry)
        {
            //if the list is empty, bail out
            if (rollbackRegistry.Count == 0)
                return;

            using(FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                // file-identification data
                WriteFiles.WriteHeader(fs, "IURURV1");

                WriteFiles.WriteInt(fs, 0x01, rollbackRegistry.Count);

                foreach (RegChange regCh in rollbackRegistry)
                {
                    regCh.WriteToStream(fs, true);
                }

                fs.WriteByte(0xFF);
            }
        }

        public static void ReadRollbackRegistry(string fileName, List<RegChange> rollbackRegistry)
        {
            FileStream fs = null;

            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            }
            catch (Exception)
            {
                if (fs != null)
                    fs.Close();

                throw;
            }

            // Read back the file identification data, if any
            if (!ReadFiles.IsHeaderValid(fs, "IURURV1"))
            {
                //free up the file so it can be deleted
                fs.Close();
                throw new Exception("Identifier incorrect");
            }

            byte bType = (byte)fs.ReadByte();
            while (!ReadFiles.ReachedEndByte(fs, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x01: //num of registry changes
                        rollbackRegistry.Capacity = ReadFiles.ReadInt(fs);
                        break;
                    case 0x8E: //add RegChange
                        rollbackRegistry.Add(RegChange.ReadFromStream(fs));
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            fs.Close();
        }

        #endregion Write/Read RollbackRegistry

        #region Write/Read RollbackFiles

        public static void WriteRollbackFiles(string fileName, List<FileFolder> rollbackList)
        {
            //if the list is empty, bail out
            if (rollbackList.Count == 0)
                return;

            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                // file-identification data
                fs.Write(Encoding.UTF8.GetBytes("IURUFV1"), 0, 7);


                foreach (FileFolder fileFolder in rollbackList)
                {
                    if (fileFolder.isFolder)
                    {
                        if (fileFolder.deleteFolder)
                            WriteFiles.WriteString(fs, 0x04, fileFolder.Path);
                        else
                            //folder to create on rollback
                            WriteFiles.WriteString(fs, 0x06, fileFolder.Path);
                    }
                    else
                    {
                        WriteFiles.WriteString(fs, 0x02, fileFolder.Path);
                    }
                }

                fs.WriteByte(0xFF);
            }
        }

        public static void ReadRollbackFiles(string fileName, List<string> rollbackFiles, List<string> rollbackFolders, List<string> createFolders)
        {
            FileStream fs = null;

            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            }
            catch (Exception)
            {
                if (fs != null)
                    fs.Close();

                throw;
            }

            // Read back the file identification data, if any
            if (!ReadFiles.IsHeaderValid(fs, "IURUFV1"))
            {
                //free up the file so it can be deleted
                fs.Close();
                throw new Exception("Identifier incorrect");
            }

            byte bType = (byte)fs.ReadByte();
            while (!ReadFiles.ReachedEndByte(fs, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x02: // file to delete
                        rollbackFiles.Add(ReadFiles.ReadString(fs));
                        break;
                    case 0x04: // folder to delete
                        rollbackFolders.Add(ReadFiles.ReadString(fs));
                        break;
                    case 0x06: //folder to create
                        if (createFolders != null)
                            createFolders.Add(ReadFiles.ReadString(fs));
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            fs.Close();
        }

        #endregion Write/Read RollbackFiles

        #region Write/Read RollbackCOMRegistration

        public static void WriteRollbackCOM(string fileName, List<UninstallFileInfo> rollbackList)
        {
            //if the list is empty, bail out
            if (rollbackList.Count == 0)
                return;

            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                // file-identification data
                fs.Write(Encoding.UTF8.GetBytes("IURUCV1"), 0, 7);


                foreach (UninstallFileInfo file in rollbackList)
                {
                    fs.WriteByte(0x8B); //Beginning of the file information

                    // write the filename (absolute)
                    WriteFiles.WriteString(fs, 0x01, file.Path);

                    WriteFiles.WriteInt(fs, 0x02, (int) file.RegisterCOMDll);

                    fs.WriteByte(0x9B); //End of the file information
                }

                fs.WriteByte(0xFF);
            }
        }

        public static void ReadRollbackCOM(string fileName, List<UninstallFileInfo> rollbackList)
        {
            FileStream fs = null;

            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            }
            catch (Exception)
            {
                if (fs != null)
                    fs.Close();

                throw;
            }

            // Read back the file identification data, if any
            if (!ReadFiles.IsHeaderValid(fs, "IURUCV1"))
            {
                //free up the file so it can be deleted
                fs.Close();
                throw new Exception("Identifier incorrect");
            }

            UninstallFileInfo tempUpdateFile = new UninstallFileInfo();

            byte bType = (byte)fs.ReadByte();
            while (!ReadFiles.ReachedEndByte(fs, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x01:
                        tempUpdateFile.Path = ReadFiles.ReadString(fs);
                        break;
                    case 0x02:
                        tempUpdateFile.RegisterCOMDll = (COMRegistration)ReadFiles.ReadInt(fs);
                        break;
                    case 0x9B://end of file
                        rollbackList.Add(tempUpdateFile);
                        tempUpdateFile = new UninstallFileInfo();
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            fs.Close();
        }


        #endregion COM

        #region Write/Read Uninstall Files, Folders, Registry

        public static void WriteUninstallFile(string tempDir, string uninstallDataFile, List<UpdateFile> updateDetailsFiles)
        {
            string registryRollbackFile = Path.Combine(tempDir, "backup\\regList.bak");
            string filesRollbackFile = Path.Combine(tempDir, "backup\\fileList.bak");
            string comRollbackFile = Path.Combine(tempDir, "backup\\reggedComList.bak");

            List<UninstallFileInfo> filesToUninstall = new List<UninstallFileInfo>();
            List<string> foldersToDelete = new List<string>();
            List<RegChange> registryToDelete = new List<RegChange>();
            List<UninstallFileInfo> comDllsToUnreg = new List<UninstallFileInfo>();

            //add files/folders/Registry from uninstall file
            try
            {
                if (File.Exists(uninstallDataFile))
                    ReadUninstallFile(uninstallDataFile, filesToUninstall, foldersToDelete, registryToDelete, comDllsToUnreg);
            }
            catch { }


            // load COM Dlls to rollback
            if (File.Exists(comRollbackFile))
            {
                try
                {
                    ReadRollbackCOM(comRollbackFile, comDllsToUnreg);
                }
                catch { }
            }

            //add files/folders from rollback file
            if (File.Exists(filesRollbackFile))
            {
                List<string> rollbackFiles = new List<string>();

                try
                {
                    ReadRollbackFiles(filesRollbackFile, rollbackFiles, foldersToDelete, null);
                }
                catch { }

                //add files to the uninstall list
                foreach (string filename in rollbackFiles)
                {

                    filesToUninstall.Add(new UninstallFileInfo { Path = filename, DeleteFile = true });
                }
            }

            //add files to un-NGEN
            foreach (UpdateFile ngenedFile in updateDetailsFiles)
            {
                if (ngenedFile.IsNETAssembly)
                {
                    bool addFile = true;

                    for (int i = 0; i < filesToUninstall.Count; i++)
                    {
                        if (ngenedFile.Filename == filesToUninstall[i].Path)
                        {
                            if (!filesToUninstall[i].UnNGENFile)
                            {
                                // telling it to unNgen too
                                filesToUninstall[i].UnNGENFile = true;
                            }

                            //don't add the file, and stop searching for a match
                            addFile = false;
                            break;
                        }
                    }

                    if (addFile)
                        //add the file to the list
                        filesToUninstall.Add(new UninstallFileInfo { Path = ngenedFile.Filename, UnNGENFile = true, CPUVersion = ngenedFile.CPUVersion });
                }
            }

            //add registry from rollback file (just the entries that delete values or delete keys)
            if (File.Exists(registryRollbackFile))
            {
                try
                {
                    ReadRollbackRegistry(registryRollbackFile, registryToDelete);

                    //don't include any regchanges that aren't "RemoveKey" or "RemoveValue"
                    for (int i = 0; i < registryToDelete.Count; i++)
                    {
                        if (!(registryToDelete[i].RegOperation == RegOperations.RemoveKey || registryToDelete[i].RegOperation == RegOperations.RemoveValue))
                        {
                            registryToDelete.RemoveAt(i);
                            i--;
                        }
                    }
                }
                catch { }
            }

            //write out the new uninstall data file
            if (filesToUninstall.Count != 0 || foldersToDelete.Count != 0 || registryToDelete.Count != 0)
            {
                FileStream fs = new FileStream(uninstallDataFile, FileMode.Create, FileAccess.Write);

                // Write any file-identification data you want to here
                WriteFiles.WriteHeader(fs, "IUUFRV1");

                //write COM files to uninstall
                foreach (UninstallFileInfo file in comDllsToUnreg)
                    file.Write(fs, true);

                //write files to delete
                foreach (UninstallFileInfo file in filesToUninstall)
                    file.Write(fs, false);

                //write folders to delete
                foreach (string folder in foldersToDelete)
                    WriteFiles.WriteDeprecatedString(fs, 0x10, folder);

                //write registry changes
                foreach (RegChange reg in registryToDelete)
                    reg.WriteToStream(fs, true);

                //end of file
                fs.WriteByte(0xFF);
                
                fs.Close();
            }
        }

        public static void ReadUninstallData(string clientFile, List<UninstallFileInfo> uninstallFiles, List<string> uninstallFolders, List<RegChange> uninstallRegistry, List<UninstallFileInfo> comDllsToUnreg)
        {
            try
            {
                using (ZipFile zip = ZipFile.Read(clientFile))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        //read in the uninstall data
                        zip["uninstall.dat"].Extract(ms);

                        LoadUninstallData(ms, uninstallFiles, uninstallFolders, uninstallRegistry, comDllsToUnreg);
                    }
                }
            }
            catch { }
        }

        static void ReadUninstallFile(string uninstallFile, List<UninstallFileInfo> uninstallFiles, List<string> uninstallFolders, List<RegChange> uninstallRegistry, List<UninstallFileInfo> comDllsToUnreg)
        {
            FileStream fs = null;

            try
            {
                fs = new FileStream(uninstallFile, FileMode.Open, FileAccess.Read);
            }
            catch (Exception)
            {
                if (fs != null)
                    fs.Close();

                throw;
            }

            LoadUninstallData(fs, uninstallFiles, uninstallFolders, uninstallRegistry, comDllsToUnreg);
        }

        static void LoadUninstallData(Stream ms, List<UninstallFileInfo> uninstallFiles, List<string> uninstallFolders, List<RegChange> uninstallRegistry, List<UninstallFileInfo> comDllsToUnreg)
        {
            ms.Position = 0;

            // Read back the file identification data, if any
            if (!ReadFiles.IsHeaderValid(ms, "IUUFRV1"))
            {
                //free up the file so it can be deleted
                ms.Close();

                throw new Exception("The uninstall file does not have the correct identifier - this is usually caused by file corruption.");
            }
            
            byte bType = (byte)ms.ReadByte();
            while (!ReadFiles.ReachedEndByte(ms, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x8A://file to delete
                        uninstallFiles.Add(UninstallFileInfo.Read(ms));
                        break;
                    case 0x8B: // files to unreg COM
                        comDllsToUnreg.Add(UninstallFileInfo.Read(ms));
                        break;
                    case 0x10://folder to delete
                        uninstallFolders.Add(ReadFiles.ReadDeprecatedString(ms));
                        break;
                    case 0x8E://regChanges to execute
                        uninstallRegistry.Add(RegChange.ReadFromStream(ms));
                        break;
                    default:
                        ReadFiles.SkipField(ms, bType);
                        break;
                }

                bType = (byte)ms.ReadByte();
            }

            ms.Close();
        }

        #endregion
    }

    public class FileFolder
    {
        public string Path;
        
        public bool isFolder; //is it a folder? (default: no, file)
        public bool deleteFolder;


        public FileFolder(string filePath)
        {
            Path = filePath;
            isFolder = false;
        }

        public FileFolder(string path, bool deleteFolder)
        {
            Path = path;
            isFolder = true;
            this.deleteFolder = deleteFolder;
        }
    }
}
