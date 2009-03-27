using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace wyUpdate.Common
{
    // File information and instuctions for updates
    public class UpdateDetails
    {
        private string m_PostUpdateCommands;
        private List<RegChange> m_RegistryModifications = new List<RegChange>();
        private List<UpdateFile> m_UpdateFiles = new List<UpdateFile>();
        private List<ShortcutInfo> m_ShortcutInfos = new List<ShortcutInfo>();

        private List<string> m_PreviousDesktopShortcuts = new List<string>();
        private List<string> m_PreviousSMenuShortcuts = new List<string>();

        private List<string> m_FoldersToDelete = new List<string>();

        #region Properties

        public string PostUpdateCommands
        {
            get { return m_PostUpdateCommands; }
            set { m_PostUpdateCommands = value; }
        }

        public List<RegChange> RegistryModifications
        {
            get { return m_RegistryModifications; }
            set { m_RegistryModifications = value; }
        }

        public List<UpdateFile> UpdateFiles
        {
            get { return m_UpdateFiles; }
            set { m_UpdateFiles = value; }
        }

        public List<ShortcutInfo> ShortcutInfos
        {
            get { return m_ShortcutInfos; }
            set { m_ShortcutInfos = value; }
        }

        public List<string> PreviousDesktopShortcuts
        {
            get { return m_PreviousDesktopShortcuts; }
            set { m_PreviousDesktopShortcuts = value; }
        }

        public List<string> PreviousSMenuShortcuts
        {
            get { return m_PreviousSMenuShortcuts; }
            set { m_PreviousSMenuShortcuts = value; }
        }

        public List<string> FoldersToDelete
        {
            get { return m_FoldersToDelete; }
            set { m_FoldersToDelete = value; }
        }

        #endregion Properties

        // count the number of file infos that
        // execute or need .NET ngening
        private int CountFileInfos()
        {
            int count = 0;
            foreach (UpdateFile file in m_UpdateFiles)
            {
                if (file.Execute || file.IsNETAssembly)
                    count++;
            }

            return count;
        }

        public void Load(string fileName)
        {
            byte[] fileIDBytes = new byte[7];
            string fileID = "";

            byte bType;

            FileStream fs = null;

            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            }
            catch (Exception ex)
            {
                if (fs != null)
                    fs.Close();

                throw new ArgumentException("The update details file failed to open.\n\nFull details:\n\n" + ex.Message);
            }

            // Read back the file identification data, if any
            fs.Read(fileIDBytes, 0, 7);
            fileID = System.Text.Encoding.UTF8.GetString(fileIDBytes);
            if (fileID != "IUUDFV2")
            {
                //free up the file so it can be deleted
                fs.Close();

                throw new ArgumentException("The update details file failed to open because it has an incorrect file identifier.");
            }

            UpdateFile tempUpdateFile = new UpdateFile();

            bType = (byte)fs.ReadByte();
            while (!ReadFiles.ReachedEndByte(fs, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x01:
                        m_PostUpdateCommands = ReadFiles.ReadString(fs);
                        break;
                    case 0x20://num reg changes
                        m_RegistryModifications = new List<RegChange>(ReadFiles.ReadInt(fs));
                        break;
                    case 0x21://num file infos
                        m_UpdateFiles = new List<UpdateFile>(ReadFiles.ReadInt(fs));
                        break;
                    case 0x8E:
                        m_RegistryModifications.Add(RegChange.ReadFromStream(fs));
                        break;
                    case 0x30:
                        m_PreviousDesktopShortcuts.Add(ReadFiles.ReadString(fs));
                        break;
                    case 0x31:
                        m_PreviousSMenuShortcuts.Add(ReadFiles.ReadString(fs));
                        break;
                    case 0x40:
                        tempUpdateFile.RelativePath = ReadFiles.ReadString(fs);
                        break;
                    case 0x41:
                        tempUpdateFile.Execute = ReadFiles.ReadBool(fs);
                        break;
                    case 0x42:
                        tempUpdateFile.ExBeforeUpdate = ReadFiles.ReadBool(fs);
                        break;
                    case 0x43:
                        tempUpdateFile.CommandLineArgs = ReadFiles.ReadString(fs);
                        break;
                    case 0x44:
                        tempUpdateFile.IsNETAssembly = ReadFiles.ReadBool(fs);
                        break;
                    case 0x45:
                        tempUpdateFile.WaitForExecution = ReadFiles.ReadBool(fs);
                        break;
                    case 0x46:
                        tempUpdateFile.DeleteFile = ReadFiles.ReadBool(fs);
                        break;
                    case 0x47:
                        tempUpdateFile.DeltaPatchRelativePath = ReadFiles.ReadString(fs);
                        break;
                    case 0x48:
                        tempUpdateFile.NewFileAdler32 = ReadFiles.ReadLong(fs);
                        break;
                    case 0x9B://end of file
                        m_UpdateFiles.Add(tempUpdateFile);
                        tempUpdateFile = new UpdateFile();
                        break;
                    case 0x8D:
                        m_ShortcutInfos.Add(ShortcutInfo.LoadFromStream(fs));
                        break;
                    case 0x60:
                        m_FoldersToDelete.Add(ReadFiles.ReadString(fs));
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            fs.Close();
        }

        public void Save(Stream ms)
        {
            // Write any file-identification data you want to here
            ms.Write(System.Text.Encoding.UTF8.GetBytes("IUUDFV2"), 0, 7);

            //Write post-update commands
            if (!string.IsNullOrEmpty(m_PostUpdateCommands))
                WriteFiles.WriteString(ms, 0x01, m_PostUpdateCommands);

            //number of registry changes
            WriteFiles.WriteInt(ms, 0x20, m_RegistryModifications.Count);

            for (int i = 0; i < m_RegistryModifications.Count; i++)
            {
                m_RegistryModifications[i].WriteToStream(ms, true);
            }

            //Shortcut information
            foreach (ShortcutInfo si in m_ShortcutInfos)
                si.SaveToStream(ms, true);


            //Previous shortcuts that needs to be installed in order to install new shortcuts
            foreach (string shortcut in m_PreviousDesktopShortcuts)
                WriteFiles.WriteString(ms, 0x30, shortcut);

            foreach (string shortcut in m_PreviousSMenuShortcuts)
                WriteFiles.WriteString(ms, 0x31, shortcut);

            //number of file infos
            WriteFiles.WriteInt(ms, 0x21, CountFileInfos());

            // write file info for ngening .NET, execution of files, etc.
            foreach (UpdateFile file in m_UpdateFiles)
            {
                if (file.Execute || file.IsNETAssembly || file.DeleteFile || file.DeltaPatchRelativePath != null)
                {
                    ms.WriteByte(0x8B);//Beginning of the file information

                    //relative path to file
                    WriteFiles.WriteString(ms, 0x40, file.RelativePath);

                    //execution of files
                    if (file.Execute)
                    {
                        //execute?
                        WriteFiles.WriteBool(ms, 0x41, file.Execute);

                        //execute before update?
                        WriteFiles.WriteBool(ms, 0x42, file.ExBeforeUpdate);

                        WriteFiles.WriteBool(ms, 0x45, file.WaitForExecution);

                        //commandline arguments
                        if (!string.IsNullOrEmpty(file.CommandLineArgs))
                            WriteFiles.WriteString(ms, 0x43, file.CommandLineArgs);
                    }

                    //is it a .NET assembly?
                    WriteFiles.WriteBool(ms, 0x44, file.IsNETAssembly);


                    //Delta update particulars:

                    if (file.DeleteFile)
                        WriteFiles.WriteBool(ms, 0x46, true);
                    else if (file.DeltaPatchRelativePath != null)
                    {
                        WriteFiles.WriteString(ms, 0x47, file.DeltaPatchRelativePath);

                        if (file.NewFileAdler32 != 0)
                            WriteFiles.WriteLong(ms, 0x48, file.NewFileAdler32);
                    }

                    ms.WriteByte(0x9B);//End of the file information
                }
            }

            foreach (string folder in m_FoldersToDelete)
                WriteFiles.WriteString(ms, 0x60, folder);

            //end of file
            ms.WriteByte(0xFF);
        }
    }
}
