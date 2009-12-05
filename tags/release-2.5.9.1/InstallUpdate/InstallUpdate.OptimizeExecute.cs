using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using wyUpdate.Common;

namespace wyUpdate
{
    partial class InstallUpdate
    {
        static string[] frameworkDirs;

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

        static void GetFrameworkDirectories()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\.NETFramework"))
            {
                if (key == null)
                    return;

                string installRoot = (string)key.GetValue("InstallRoot", null);

                if (installRoot != null)
                {
                    DirectoryInfo dir = new DirectoryInfo(installRoot);

                    dir = dir.Parent;

                    if (dir != null)
                    {
                        List<string> fDirs = new List<string>(2);

                        string frameDir = Path.Combine(dir.FullName, "Framework\\v2.0.50727");

                        if (Directory.Exists(frameDir))
                            fDirs.Add(frameDir);

                        frameDir = Path.Combine(dir.FullName, "Framework64\\v2.0.50727");

                        if (Directory.Exists(frameDir))
                            fDirs.Add(frameDir);

                        if (fDirs.Count != 0)
                        {
                            frameworkDirs = fDirs.ToArray();
                            return;
                        }
                    }
                }

                throw new Exception("Failed to retrieve .NET Framework directories.");
            }
        }

        static void NGenInstall(string filename, CPUVersion cpuVersion)
        {
            if (frameworkDirs == null)
                GetFrameworkDirectories();

            Process proc = new Process
            {
                StartInfo =
                {
                    FileName = Path.Combine(frameworkDirs[cpuVersion == CPUVersion.x86 ? 0 : frameworkDirs.Length - 1], "ngen.exe"),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = " install \"" + filename + "\"" + " /nologo"
                }
            };

            proc.Start();

            proc.WaitForExit();
        }

        static void NGenUninstall(string filename, CPUVersion cpuVersion)
        {
            if (frameworkDirs == null)
                GetFrameworkDirectories();

            Process proc = new Process
            {
                StartInfo =
                {
                    FileName = Path.Combine(frameworkDirs[cpuVersion == CPUVersion.x86 ? 0 : frameworkDirs.Length - 1], "ngen.exe"),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = " uninstall \"" + filename + "\"" + " /nologo"
                }
            };

            proc.Start();

            proc.WaitForExit();
        }


        /// <summary>
        /// Registers a native dll or ocx using regsvr32.
        /// </summary>
        /// <param name="DllPath">The path to the dll or ocx.</param>
        /// <param name="Uninstall">Are we uninstalling the dll/ocx?</param>
        static void RegisterDllServer(string DllPath, bool Uninstall)
        {
            using (Process p = new Process
            {
                StartInfo =
                {
                    FileName = "regsvr32.exe",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = (Uninstall ? "/s /u" : "/s") + " \"" + DllPath + "\""
                }
            })
            {
                p.Start();

                p.WaitForExit();

                switch (p.ExitCode)
                {
                    case 0:
                        // (un) installed successfully
                        break;
                    case 1:
                        throw new Exception("RegSvr32 failed - bad arguments. File: " + DllPath);
                    case 2:
                        throw new Exception("RegSvr32 failed - OLE initilization failed for " + DllPath);
                    case 3:
                        throw new Exception("RegSvr32 failed - Failed to load the module, you may need to check for problems with dependencies. File: " + DllPath);
                    case 4:
                        throw new Exception("RegSvr32 failed - Can't find " +
                                            (Uninstall ? "DllUnregisterServer" : "DllRegisterServer") +
                                            " entry point in the file, maybe it's not a .DLL or .OCX? File: " + DllPath);
                    case 5:
                        throw new Exception("RegSvr32 failed - The assembly was loaded, but the call to " +
                                            (Uninstall ? "DllUnregisterServer" : "DllRegisterServer") +
                                            " failed. File: " + DllPath);

                    default:
                        throw new Exception("Failed to " + (Uninstall ? "unregister" : "register") +
                                            " dll with RegSvr32. Return code: " + p.ExitCode + ". File: " + DllPath);
                }
            }
        }

        static void RegAsm(string filename, bool Uninstall, CPUVersion cpu)
        {
            if (frameworkDirs == null)
                GetFrameworkDirectories();

            if (cpu == CPUVersion.x64 && frameworkDirs.Length < 2)
                throw new Exception("Cannot register an x64 DLL on an x86 machine.");

            // call 32-bit regasm
            if (cpu != CPUVersion.x64)
            {
                using (Process p = new Process
                {
                    StartInfo =
                    {
                        FileName = frameworkDirs[0] + "\\RegAsm.exe",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Arguments =
                            "\"" + filename + "\" " +
                            (Uninstall ? "/nologo /s /u" : "/nologo /s")
                    }
                })
                {
                    p.Start();

                    p.WaitForExit();

                    if (p.ExitCode != 0)
                        throw new Exception("Failed to register assembly with RegAsm (return code: " + p.ExitCode +
                                            "). File: " + filename);
                }
            }

            // call 64-bit regasm
            if (cpu == CPUVersion.x64 || cpu == CPUVersion.AnyCPU && frameworkDirs.Length == 2)
            {
                using (Process p = new Process
                {
                    StartInfo =
                    {
                        FileName = frameworkDirs[1] + "\\RegAsm.exe",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Arguments =
                            "\"" + filename + "\" " +
                            (Uninstall ? "/nologo /s /u" : "/nologo /s")
                    }
                })
                {
                    p.Start();

                    p.WaitForExit();

                    if (p.ExitCode != 0)
                        throw new Exception("Failed to register assembly with RegAsm (return code: " + p.ExitCode +
                                            "). File: " + filename);
                }
            }
        }
    }
}