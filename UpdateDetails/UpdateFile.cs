using System.IO;

namespace wyUpdate.Common
{
    public class UpdateFile
    {
        #region Properties

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

        public bool WaitForExecution { get; set; }

        //Delta Patching Particulars:
        public string DeltaPatchRelativePath { get; set; }

        public bool DeleteFile { get; set; }

        public long NewFileAdler32 { get; set; }

        #endregion Properties

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