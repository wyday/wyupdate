using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace wyUpdate.Common
{
    [Flags]
    public enum COMRegistration { None = 0, IsNETAssembly = 1, Register = 2, UnRegister = 4, PreviouslyRegistered = 8 }
    public enum ElevationType { SameAswyUpdate = 0, Elevated = 1, NotElevated = 2 }

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

        public bool RollbackOnNonZeroRet;

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

        public ElevationType ElevationType;

        public COMRegistration RegisterCOMDll;

#if DESIGNER
        public UpdateFile() { }

        public UpdateFile(string filename, string prefix)
        {
            Filename = filename;

            if (!string.IsNullOrEmpty(filename))
                RelativePath = prefix + Path.GetExtension(filename);
        }

        public static List<int> StringToIntList(string retCodes)
        {
            string[] retSplit = retCodes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            List<int> retList = new List<int>(retSplit.Length);

            foreach (string code in retSplit)
                retList.Add(Convert.ToInt32(code));

            return retList;
        }
#endif
    }
}