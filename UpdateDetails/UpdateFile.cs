using System;
using System.Diagnostics;
using System.IO;

namespace wyUpdate.Common
{
    [Flags]
    public enum COMRegistration { None = 0, IsNETAssembly = 1, Register = 2, UnRegister = 4, PreviouslyRegistered = 8 }

    public class UpdateFile
    {
        //full path of file for creating zip file
        public string Filename { get; set; }

        public string RelativePath { get; set; }

        //execute the file?
        public bool Execute { get; set; }

        //if so, before or after update?
        public bool ExBeforeUpdate { get; set; }

        //command line arguents
        public string CommandLineArgs { get; set; }

        //is it a .NET assembly?
        public bool IsNETAssembly { get; set; }

        public CPUVersion CPUVersion { get; set; }

        public FrameworkVersion FrameworkVersion { get; set; }

        public bool WaitForExecution { get; set; }

        //Delta Patching Particulars:
        public string DeltaPatchRelativePath { get; set; }

        public bool DeleteFile { get; set; }

        public long NewFileAdler32 { get; set; }

        public ProcessWindowStyle ProcessWindowStyle { get; set; }

        public COMRegistration RegisterCOMDll { get; set; }

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