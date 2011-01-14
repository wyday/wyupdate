using System;
using System.Collections.Generic;
using System.IO;

namespace wyUpdate.Common
{
    // File information and instuctions for updates
    public class UpdateDetails
    {
        public List<RegChange> RegistryModifications = new List<RegChange>();

        public List<UpdateFile> UpdateFiles = new List<UpdateFile>();

        public List<ShortcutInfo> ShortcutInfos = new List<ShortcutInfo>();

        public List<string> PreviousDesktopShortcuts = new List<string>();

        public List<string> PreviousSMenuShortcuts = new List<string>();

        public List<string> FoldersToDelete = new List<string>();

        public List<string> ServicesToStop = new List<string>();

        public List<StartService> ServicesToStart = new List<StartService>();

#if CLIENT
        public static UpdateDetails Load(string fileName)
        {
            UpdateDetails updtDetails = new UpdateDetails();

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
            if (!ReadFiles.IsHeaderValid(fs, "IUUDFV2"))
            {
                //free up the file so it can be deleted
                fs.Close();

                throw new ArgumentException("The update details file failed to open because it has an incorrect file identifier.");
            }

            UpdateFile tempUpdateFile = new UpdateFile();
            int lastStartServiceArgOn = 0;

            byte bType = (byte)fs.ReadByte();
            while (!ReadFiles.ReachedEndByte(fs, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x20://num reg changes
                        updtDetails.RegistryModifications = new List<RegChange>(ReadFiles.ReadInt(fs));
                        break;
                    case 0x21://num file infos
                        updtDetails.UpdateFiles = new List<UpdateFile>(ReadFiles.ReadInt(fs));
                        break;
                    case 0x8E:
                        updtDetails.RegistryModifications.Add(RegChange.ReadFromStream(fs));
                        break;
                    case 0x30:
                        updtDetails.PreviousDesktopShortcuts.Add(ReadFiles.ReadDeprecatedString(fs));
                        break;
                    case 0x31:
                        updtDetails.PreviousSMenuShortcuts.Add(ReadFiles.ReadDeprecatedString(fs));
                        break;
                    case 0x32: //service to stop
                        updtDetails.ServicesToStop.Add(ReadFiles.ReadString(fs));
                        break;
                    case 0x33: //service to start
                        updtDetails.ServicesToStart.Add(new StartService(ReadFiles.ReadString(fs)));
                        break;
                    case 0x34: // number of start arguments for the last service
                        updtDetails.ServicesToStart[updtDetails.ServicesToStart.Count - 1].Arguments =
                            new string[ReadFiles.ReadInt(fs)];
                        lastStartServiceArgOn = 0;
                        break;
                    case 0x35:
                        updtDetails.ServicesToStart[updtDetails.ServicesToStart.Count - 1].Arguments[
                            lastStartServiceArgOn] = ReadFiles.ReadString(fs);
                        lastStartServiceArgOn++;
                        break;
                    case 0x40:
                        tempUpdateFile.RelativePath = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x41:
                        tempUpdateFile.Execute = ReadFiles.ReadBool(fs);
                        break;
                    case 0x42:
                        tempUpdateFile.ExBeforeUpdate = ReadFiles.ReadBool(fs);
                        break;
                    case 0x43:
                        tempUpdateFile.CommandLineArgs = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x44:
                        tempUpdateFile.IsNETAssembly = ReadFiles.ReadBool(fs);
                        break;
                    case 0x45:
                        tempUpdateFile.WaitForExecution = ReadFiles.ReadBool(fs);
                        break;
                    case 0x8F:
                        tempUpdateFile.RollbackOnNonZeroRet = true;
                        break;
                    case 0x4D:
                        if (tempUpdateFile.RetExceptions == null)
                            tempUpdateFile.RetExceptions = new List<int>();

                        tempUpdateFile.RetExceptions.Add(ReadFiles.ReadInt(fs));
                        break;
                    case 0x46:
                        tempUpdateFile.DeleteFile = ReadFiles.ReadBool(fs);
                        break;
                    case 0x47:
                        tempUpdateFile.DeltaPatchRelativePath = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x48:
                        tempUpdateFile.NewFileAdler32 = ReadFiles.ReadLong(fs);
                        break;
                    case 0x49:
                        tempUpdateFile.CPUVersion = (CPUVersion)ReadFiles.ReadInt(fs);
                        break;
                    case 0x4A:
                        tempUpdateFile.ProcessWindowStyle = (System.Diagnostics.ProcessWindowStyle)ReadFiles.ReadInt(fs);
                        break;
                    case 0x4B:
                        tempUpdateFile.FrameworkVersion = (FrameworkVersion)ReadFiles.ReadInt(fs);
                        break;
                    case 0x4C:
                        tempUpdateFile.RegisterCOMDll = (COMRegistration)ReadFiles.ReadInt(fs);
                        break;
                    case 0x9B://end of file
                        updtDetails.UpdateFiles.Add(tempUpdateFile);
                        tempUpdateFile = new UpdateFile();
                        break;
                    case 0x8D:
                        updtDetails.ShortcutInfos.Add(ShortcutInfo.LoadFromStream(fs));
                        break;
                    case 0x60:
                        updtDetails.FoldersToDelete.Add(ReadFiles.ReadDeprecatedString(fs));
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            fs.Close();

            return updtDetails;
        }
#endif

#if DESIGNER
        // count # NGEN or execute files
        int CountFileInfos()
        {
            int count = 0;
            foreach (UpdateFile file in UpdateFiles)
            {
                if (file.Execute || file.IsNETAssembly)
                    count++;
            }

            return count;
        }

        public Stream Save()
        {
            MemoryStream ms = new MemoryStream();

            // Write any file-identification data you want to here
            WriteFiles.WriteHeader(ms, "IUUDFV2");

            //number of registry changes
            WriteFiles.WriteInt(ms, 0x20, RegistryModifications.Count);

            for (int i = 0; i < RegistryModifications.Count; i++)
            {
                RegistryModifications[i].WriteToStream(ms, true);
            }

            //Shortcut information
            foreach (ShortcutInfo si in ShortcutInfos)
                si.SaveToStream(ms, true);


            //Previous shortcuts that needs to be installed in order to install new shortcuts
            foreach (string shortcut in PreviousDesktopShortcuts)
                WriteFiles.WriteDeprecatedString(ms, 0x30, shortcut);

            foreach (string shortcut in PreviousSMenuShortcuts)
                WriteFiles.WriteDeprecatedString(ms, 0x31, shortcut);

            //number of file infos
            WriteFiles.WriteInt(ms, 0x21, CountFileInfos());

            // write file info for ngening .NET, execution of files, etc.
            foreach (UpdateFile file in UpdateFiles)
            {
                if (file.Execute || file.IsNETAssembly || file.DeleteFile || file.DeltaPatchRelativePath != null || file.RegisterCOMDll != COMRegistration.None)
                {
                    ms.WriteByte(0x8B);//Beginning of the file information

                    //relative path to file
                    WriteFiles.WriteDeprecatedString(ms, 0x40, file.RelativePath);

                    //execution of files
                    if (file.Execute)
                    {
                        // execute?
                        WriteFiles.WriteBool(ms, 0x41, true);

                        // execute before update?
                        WriteFiles.WriteBool(ms, 0x42, file.ExBeforeUpdate);

                        if (file.WaitForExecution)
                        {
                            WriteFiles.WriteBool(ms, 0x45, file.WaitForExecution);

                            if (file.RollbackOnNonZeroRet)
                            {
                                // we are rolling back on non-zero return code
                                ms.WriteByte(0x8F);

                                // write all the exceptions we're making for rollback codes
                                if (file.RetExceptions != null)
                                {
                                    foreach (int except in file.RetExceptions)
                                        WriteFiles.WriteInt(ms, 0x4D, except);
                                }
                            }
                        }

                        //commandline arguments
                        if (!string.IsNullOrEmpty(file.CommandLineArgs))
                            WriteFiles.WriteDeprecatedString(ms, 0x43, file.CommandLineArgs);

                        if (file.ProcessWindowStyle != System.Diagnostics.ProcessWindowStyle.Normal)
                            WriteFiles.WriteInt(ms, 0x4A, (int)file.ProcessWindowStyle);
                    }

                    //is it a .NET assembly?
                    if (file.IsNETAssembly)
                    {
                        WriteFiles.WriteBool(ms, 0x44, true);

                        // save whether the files is AnyCPU, x86, or x64
                        WriteFiles.WriteInt(ms, 0x49, (int)file.CPUVersion);

                        // .NET framework is by default 2.0 - only save the framework version if it's .NET 4.0 or unknown
                        if (file.FrameworkVersion != FrameworkVersion.Net2_0)
                            WriteFiles.WriteInt(ms, 0x4B, (int) file.FrameworkVersion);
                    }

                    if (file.RegisterCOMDll != COMRegistration.None)
                        WriteFiles.WriteInt(ms, 0x4C, (int) file.RegisterCOMDll);

                    //Delta update particulars:

                    if (file.DeleteFile)
                        WriteFiles.WriteBool(ms, 0x46, true);
                    else if (file.DeltaPatchRelativePath != null)
                    {
                        WriteFiles.WriteDeprecatedString(ms, 0x47, file.DeltaPatchRelativePath);

                        if (file.NewFileAdler32 != 0)
                            WriteFiles.WriteLong(ms, 0x48, file.NewFileAdler32);
                    }

                    ms.WriteByte(0x9B);//End of the file information
                }
            }

            foreach (string folder in FoldersToDelete)
                WriteFiles.WriteDeprecatedString(ms, 0x60, folder);

            foreach (string service in ServicesToStop)
                WriteFiles.WriteString(ms, 0x32, service);

            foreach (StartService service in ServicesToStart)
            {
                WriteFiles.WriteString(ms, 0x33, service.Name);

                if (service.Arguments != null)
                {
                    WriteFiles.WriteInt(ms, 0x34, service.Arguments.Length);

                    foreach (string arg in service.Arguments)
                        WriteFiles.WriteString(ms, 0x35, arg);
                }
            }

            //end of file
            ms.WriteByte(0xFF);

            ms.Position = 0;
            return ms;
        }
#endif
    }

    public class StartService
    {
        public string Name;
        public string[] Arguments;

        public StartService(string name)
        {
            Name = name;
        }
    }
}