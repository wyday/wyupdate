using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ionic.Zip;
using wyUpdate.Common;

namespace wyUpdate
{
    public struct UninstallFileInfo
    {
        public string Path;
        public bool DeleteFile;
        public bool UnNGENFile;

        public UninstallFileInfo(string path, bool delete, bool unNgen)
        {
            Path = path;
            DeleteFile = delete;
            UnNGENFile = unNgen;
        }

        public static UninstallFileInfo Read(Stream fs)
        {
            UninstallFileInfo tempUFI = new UninstallFileInfo();
            
            //read in the fileinfo

            int bytesToSkip = 0;

            byte bType = (byte)fs.ReadByte();
            while (!ReadFiles.ReachedEndByte(fs, bType, 0x9A)) //if end byte is detected, bail out
            {
                switch (bType)
                {
                    case 0x01://file path
                        tempUFI.Path = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x02://delete the file?
                        tempUFI.DeleteFile = ReadFiles.ReadBool(fs);
                        break;
                    case 0x03://un-NGEN the file?
                        tempUFI.UnNGENFile = ReadFiles.ReadBool(fs);
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                //skip unknown bytes
                if (bytesToSkip != 0)
                {
                    fs.Position += bytesToSkip;
                }
                bytesToSkip = 0;

                bType = (byte)fs.ReadByte();
            }

            return tempUFI;
        }

        public void Write(Stream fs)
        {
            //beginning of the uninstall file info
            fs.WriteByte(0x8A);

            //path to the file
            WriteFiles.WriteDeprecatedString(fs, 0x01, Path);

            //delete the file?
            if (DeleteFile)
                WriteFiles.WriteBool(fs, 0x02, true);

            if (UnNGENFile)
                WriteFiles.WriteBool(fs, 0x03, true);

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
            catch (Exception) { }

            //delete the files
            foreach (string file in fileList)
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch (Exception) { }
            }

            //delete the folders
            foreach (string folder in foldersToDelete)
            {

                //TODO: test rolling back read-only / hidden folders
                try { Directory.Delete(folder, true); }
                catch (Exception) { }
            }

            //create the folders
            foreach (string folder in foldersToCreate)
            {
                try { Directory.CreateDirectory(folder); }
                catch (Exception) { }
            }

            //restore old versions
            string[] backupFolders = { Path.Combine(backupFolder, "base"), 
                 Path.Combine(backupFolder, "system"), 
                 Path.Combine(backupFolder, "appdata"),
                 Path.Combine(backupFolder, "comappdata"),
                 Path.Combine(backupFolder, "comdesktop"),
                 Path.Combine(backupFolder, "comstartmenu") };
            string[] destFolders = { m_ProgramDirectory, 
                Environment.GetFolderPath(Environment.SpecialFolder.System), 
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                SystemFolders.CommonAppData,
                SystemFolders.CommonDesktop,
                SystemFolders.CommonProgramsStartMenu};

            for (int i = 0; i < backupFolders.Length; i++)
            {
                if (Directory.Exists(backupFolders[i]))
                    RestoreFiles(destFolders[i], backupFolders[i]);
            }

            //Delete temporary files
            try
            {
                Directory.Delete(m_TempDirectory, true);
            }
            catch (Exception) { }
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
                    bool resetAttributes = (atr & FileAttributes.Hidden) != 0 || (atr & FileAttributes.ReadOnly) != 0;

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

        public static void RollbackRegistry(string m_TempDirectory, string m_ProgramDirectory)
        {
            string backupFolder = Path.Combine(m_TempDirectory, "backup");
            List<RegChange> rollbackRegistry = new List<RegChange>();

            try
            {
                ReadRollbackRegistry(Path.Combine(backupFolder, "regList.bak"), rollbackRegistry);
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

        #region Write/Read RollbackRegistry

        public static void WriteRollbackRegistry(string fileName, List<RegChange> rollbackRegistry)
        {
            //if the list is empty, bail out
            if (rollbackRegistry.Count == 0)
                return;

            FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

            // file-identification data
            fs.Write(Encoding.UTF8.GetBytes("IURURV1"), 0, 7);

            WriteFiles.WriteInt(fs, 0x01, rollbackRegistry.Count);

            foreach (RegChange regCh in rollbackRegistry)
            {
                regCh.WriteToStream(fs, true);
            }

            fs.WriteByte(0xFF);
            fs.Close();
        }

        public static void ReadRollbackRegistry(string fileName, List<RegChange> rollbackRegistry)
        {
            byte[] fileIDBytes = new byte[7];

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
            fs.Read(fileIDBytes, 0, 7);
            string fileID = Encoding.UTF8.GetString(fileIDBytes);
            if (fileID != "IURURV1")
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

            FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

            // file-identification data
            fs.Write(Encoding.UTF8.GetBytes("IURUFV1"), 0, 7);


            foreach (FileFolder fileFolder in rollbackList)
            {
                if (fileFolder.isFolder)
                {
                    if (fileFolder.deleteFolder)
                        WriteFiles.WriteDeprecatedString(fs, 0x04, fileFolder.Path);
                    else
                        //folder to create on rollback
                        WriteFiles.WriteDeprecatedString(fs, 0x06, fileFolder.Path);
                }
                else
                {
                    WriteFiles.WriteDeprecatedString(fs, 0x02, fileFolder.Path);
                }
                
            }

            fs.WriteByte(0xFF);
            fs.Close();
        }

        public static void ReadRollbackFiles(string fileName, List<string> rollbackFiles, List<string> rollbackFolders, List<string> createFolders)
        {
            byte[] fileIDBytes = new byte[7];

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
            fs.Read(fileIDBytes, 0, 7);
            string fileID = Encoding.UTF8.GetString(fileIDBytes);
            if (fileID != "IURUFV1")
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
                        rollbackFiles.Add(ReadFiles.ReadDeprecatedString(fs));
                        break;
                    case 0x04: // folder to delete
                        rollbackFolders.Add(ReadFiles.ReadDeprecatedString(fs));
                        break;
                    case 0x06: //folder to create
                        if (createFolders != null)
                            createFolders.Add(ReadFiles.ReadDeprecatedString(fs));
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

        #region Write/Read Uninstall Files, Folders, Registry

        public static void WriteUninstallFile(string uninstallDataFile, string registryRollbackFile, string filesRollbackFile, List<UpdateFile> updateDetailsFiles)
        {
            List<UninstallFileInfo> filesToUninstall = new List<UninstallFileInfo>();
            List<string> foldersToDelete = new List<string>();

            List<RegChange> registryToDelete = new List<RegChange>();

            //add files/folders/Registry from uninstall file
            try
            {
                if (File.Exists(uninstallDataFile))
                    ReadUninstallFile(uninstallDataFile, filesToUninstall, foldersToDelete, registryToDelete);
            }
            catch (Exception) { }


            //add files/folders from rollback file
            if (File.Exists(filesRollbackFile))
            {
                List<string> rollbackFiles = new List<string>();

                try
                {
                    ReadRollbackFiles(filesRollbackFile, rollbackFiles, foldersToDelete, null);
                }
                catch (Exception) { }

                //add files to the uninstall list
                foreach (string filename in rollbackFiles)
                {
                    filesToUninstall.Add(new UninstallFileInfo(filename, true, false));
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
                                //Overwrite the existing item, telling it to unNgen too
                                filesToUninstall[i] = new UninstallFileInfo(filesToUninstall[i].Path, filesToUninstall[i].DeleteFile, true);
                            }

                            //don't add the file, and stop searching for a match
                            addFile = false;
                            break;
                        }
                    }

                    if (addFile)
                        //add the file to the list
                        filesToUninstall.Add(new UninstallFileInfo(ngenedFile.Filename, false, true));
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
                catch (Exception) { }
            }

            //write out the new uninstall data file
            if (filesToUninstall.Count != 0 || foldersToDelete.Count != 0 || registryToDelete.Count != 0)
            {
                FileStream fs = new FileStream(uninstallDataFile, FileMode.Create, FileAccess.Write);

                // Write any file-identification data you want to here
                fs.Write(Encoding.UTF8.GetBytes("IUUFRV1"), 0, 7);

                //write files to delete
                foreach (UninstallFileInfo file in filesToUninstall)
                    file.Write(fs);

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



        public static void ReadUninstallData(string clientFile, List<UninstallFileInfo> uninstallFiles, List<string> uninstallFolders, List<RegChange> uninstallRegistry)
        {
            try
            {
                using (ZipFile zip = ZipFile.Read(clientFile))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        //read in the uninstall data
                        zip["uninstall.dat"].Extract(ms);

                        LoadUninstallData(ms, uninstallFiles, uninstallFolders, uninstallRegistry);
                    }
                }
            }
            catch { }
        }

        private static void ReadUninstallFile(string uninstallFile, List<UninstallFileInfo> uninstallFiles, List<string> uninstallFolders, List<RegChange> uninstallRegistry)
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

            byte[] fileIDBytes = new byte[7];

            // Read back the file identification data, if any
            fs.Read(fileIDBytes, 0, 7);
            string fileID = Encoding.UTF8.GetString(fileIDBytes);
            if (fileID != "IUUFRV1")
            {
                //free up the file so it can be deleted
                fs.Close();

                throw new Exception("The uninstall file does not have the correct identifier - this is usually caused by file corruption.");
            }

            byte bType = (byte)fs.ReadByte();
            while (!ReadFiles.ReachedEndByte(fs, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x8A://file to delete
                        uninstallFiles.Add(UninstallFileInfo.Read(fs));
                        break;
                    case 0x10://folder to delete
                        uninstallFolders.Add(ReadFiles.ReadDeprecatedString(fs));
                        break;
                    case 0x8E://regChanges to execute
                        uninstallRegistry.Add(RegChange.ReadFromStream(fs));
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            fs.Close();
        }

        private static void LoadUninstallData(Stream ms, List<UninstallFileInfo> uninstallFiles, List<string> uninstallFolders, List<RegChange> uninstallRegistry)
        {
            byte[] fileIDBytes = new byte[7];

            ms.Position = 0;

            // Read back the file identification data, if any
            ms.Read(fileIDBytes, 0, 7);
            string fileID = Encoding.UTF8.GetString(fileIDBytes);
            if (fileID != "IUUFRV1")
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
