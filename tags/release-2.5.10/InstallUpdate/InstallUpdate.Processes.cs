using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace wyUpdate
{
    partial class InstallUpdate
    {
        public void RunProcessesCheck()
        {
            List<FileInfo> files = new List<FileInfo>(new DirectoryInfo(ProgramDirectory).GetFiles("*.exe", SearchOption.AllDirectories));

            RemoveSelfFromProcesses(files);

            //check for (and delete) a newer client if it exists
            DeleteClientInPath(ProgramDirectory, Path.Combine(TempDirectory, "base"));

            List<Process> rProcesses = ProcessesNeedClosing(files);

            if (rProcesses.Count == 0)
            {
                //no processes need closing, all done
                files = null;
                rProcesses = null;
            }

            Sender.BeginInvoke(SenderDelegate, new object[] { files, rProcesses, true });
        }

        static void RemoveSelfFromProcesses(List<FileInfo> files)
        {
            for (int i = 0; i < files.Count; i++)
            {
                if (ProcessIsSelf(files[i].FullName))
                {
                    // remove self from the list of processes
                    files.RemoveAt(i);
                    return;
                }
            }
        }

        public static bool ProcessIsSelf(string processPath)
        {
            string self = Assembly.GetExecutingAssembly().Location;

#if DEBUG
            string vhostFile = self.Substring(0, self.Length - 3) + "vshost.exe"; //for debugging

            if (processPath.ToLower() == vhostFile.ToLower())
                return true;
#endif

            if (processPath.ToLower() == self.ToLower())
                return true;

            return false;
        }

        static List<Process> ProcessesNeedClosing(List<FileInfo> baseFiles)
        {
            Process[] aProcess = Process.GetProcesses();

            List<Process> rProcesses = new List<Process>();

            foreach (Process proc in aProcess)
            {
                foreach (FileInfo filename in baseFiles)
                {
                    try
                    {
                        //are one of the exe's in baseDir running?
                        if (proc.MainModule != null && proc.MainModule.FileName.ToLower() == filename.FullName.ToLower())
                        {
                            rProcesses.Add(proc);
                        }
                    }
                    catch { }
                }
            }

            return rProcesses;
        }
    }
}