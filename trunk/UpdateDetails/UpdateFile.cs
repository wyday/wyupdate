using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace wyUpdate.Common
{
    [Flags]
    public enum COMRegistration { None = 0, IsNETAssembly = 1, Register = 2, UnRegister = 4, PreviouslyRegistered = 8 }

    public class UpdateFile
    {
        //full path of file for creating zip file
        public string Filename;

        public string RelativePath;

        //execute the file?
        public bool Execute;

        //if so, before or after update?
        public bool ExBeforeUpdate;

        //command line arguents
        public string CommandLineArgs;

        public bool WaitForExecution;

        public bool RollBackOnFailure;

        public List<int> RetExceptions;

        //is it a .NET assembly?
        public bool IsNETAssembly;

        public CPUVersion CPUVersion;

        public FrameworkVersion FrameworkVersion;

        //Delta Patching Particulars:
        public string DeltaPatchRelativePath;

        public bool DeleteFile;

        public long NewFileAdler32;

        public ProcessWindowStyle ProcessWindowStyle;

        public COMRegistration RegisterCOMDll;

#if DESIGNER
        public UpdateFile() { }

        public UpdateFile(string filename, string prefix)
        {
            Filename = filename;

            if (!string.IsNullOrEmpty(filename))
                RelativePath = prefix + Path.GetExtension(filename);
        }
#endif
    }
}