using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using wyUpdate.Common;

namespace wyUpdate
{
    partial class InstallUpdate
    {
        public void RunOptimizeExecute()
        {
            // simply update the progress bar to show the 6th step is entirely complete
            ThreadHelper.ReportProgress(Sender, SenderDelegate, string.Empty, GetRelativeProgess(6, 0), 0);

            //optimize everything but "temp" files
            for (int i = 0; i < UpdtDetails.UpdateFiles.Count; i++)
            {
                if (UpdtDetails.UpdateFiles[i].IsNETAssembly)
                {
                    //if not a temp file
                    if (UpdtDetails.UpdateFiles[i].RelativePath.Length >= 4 &&
                        UpdtDetails.UpdateFiles[i].RelativePath.Substring(0, 4) != "temp")
                    {
                        //optimize (ngen) the file
                        string filename = FixUpdateDetailsPaths(UpdtDetails.UpdateFiles[i].RelativePath);

                        if (!string.IsNullOrEmpty(filename))
                            NGenInstall(filename, UpdtDetails.UpdateFiles[i].CPUVersion); //optimize the file
                    }
                }
            }

            ThreadHelper.ReportProgress(Sender, SenderDelegate, string.Empty, GetRelativeProgess(6, 50), 50);

            //execute files
            for (int i = 0; i < UpdtDetails.UpdateFiles.Count; i++)
            {
                if (UpdtDetails.UpdateFiles[i].Execute &&
                !UpdtDetails.UpdateFiles[i].ExBeforeUpdate)
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        //use the absolute path

                        FileName =
                            FixUpdateDetailsPaths(UpdtDetails.UpdateFiles[i].RelativePath)
                    };

                    if (!string.IsNullOrEmpty(psi.FileName))
                    {
                        //command line arguments
                        if (!string.IsNullOrEmpty(UpdtDetails.UpdateFiles[i].CommandLineArgs))
                            psi.Arguments = ParseText(UpdtDetails.UpdateFiles[i].CommandLineArgs);

                        //start the process
                        Process p = Process.Start(psi);

                        if (UpdtDetails.UpdateFiles[i].WaitForExecution && p != null)
                            p.WaitForExit();
                    }
                }
            }

            ThreadHelper.ReportProgress(Sender, SenderDelegate, string.Empty, GetRelativeProgess(6, 100), 100);

            //TODO: Make command processing more versatile
            //Process text commands like $refreshicons()
            if (!string.IsNullOrEmpty(UpdtDetails.PostUpdateCommands))
                ParseCommandText(UpdtDetails.PostUpdateCommands);

            ThreadHelper.ReportSuccess(Sender, SenderDelegate, string.Empty);
        }




        private static string[] frameWorkDirs;

        public static string[] FrameworkDirectories
        {
            get
            {
                if (frameWorkDirs != null)
                    return frameWorkDirs;

                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\.NETFramework"))
                    {
                        if (key == null)
                            return null;

                        string installRoot = (string)key.GetValue("InstallRoot", null);

                        if (installRoot == null)
                            return null;

                        DirectoryInfo dir = new DirectoryInfo(installRoot);

                        dir = dir.Parent;

                        if (dir == null)
                            return null;

                        List<string> frameworkDirs = new List<string>(2);

                        string frameDir = Path.Combine(dir.FullName, "Framework\\v2.0.50727");

                        if (Directory.Exists(frameDir))
                            frameworkDirs.Add(frameDir);

                        frameDir = Path.Combine(dir.FullName, "Framework64\\v2.0.50727");

                        if (Directory.Exists(frameDir))
                            frameworkDirs.Add(frameDir);

                        if (frameworkDirs.Count == 0)
                            return null;

                        frameWorkDirs = frameworkDirs.ToArray();
                    }
                }
                catch { }


                return frameWorkDirs;
            }
        }


        static void NGenInstall(string filename, CPUVersion cpuVersion)
        {
            if (FrameworkDirectories == null)
                return;

            Process proc = new Process
            {
                StartInfo =
                {
                    FileName = Path.Combine(FrameworkDirectories[cpuVersion == CPUVersion.x86 ? 0 : FrameworkDirectories.Length - 1], "ngen.exe"),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = " install \"" + filename + "\"" + " /nologo"
                }
            };

            proc.Start();

            proc.WaitForExit();
        }

        static void NGenUninstall(string filename, CPUVersion cpuVersion)
        {
            if (FrameworkDirectories == null)
                return;

            Process proc = new Process
            {
                StartInfo =
                {
                    FileName = Path.Combine(FrameworkDirectories[cpuVersion == CPUVersion.x86 ? 0 : FrameworkDirectories.Length - 1], "ngen.exe"),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = " uninstall \"" + filename + "\"" + " /nologo"
                }
            };

            proc.Start();

            proc.WaitForExit();
        }
    }
}