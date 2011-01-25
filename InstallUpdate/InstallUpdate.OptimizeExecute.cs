using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using Microsoft.Win32;
using wyUpdate.Common;

namespace wyUpdate
{
    partial class InstallUpdate
    {
        static string[] frameworkV2_0Dirs;
        static string[] frameworkV4_0Dirs;

        public string SkipStartService;

        public void RunOptimizeExecute()
        {
            bw.DoWork += bw_DoWorkOptimizeExecute;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_RunWorkerCompletedOptimizeExecute;

            bw.RunWorkerAsync();
        }

        void bw_DoWorkOptimizeExecute(object sender, DoWorkEventArgs e)
        {
            // simply update the progress bar to show the 6th step is entirely complete
            bw.ReportProgress(0, new object[] { GetRelativeProgess(6, 0), 0, string.Empty, ProgressStatus.None, null });

            List<UninstallFileInfo> rollbackCOM = new List<UninstallFileInfo>();
            List<string> startedServices = new List<string>();
            Exception except = null;

            //optimize everything but "temp" files
            for (int i = 0; i < UpdtDetails.UpdateFiles.Count; i++)
            {
                if (UpdtDetails.UpdateFiles[i].IsNETAssembly || (UpdtDetails.UpdateFiles[i].RegisterCOMDll & COMRegistration.Register) == COMRegistration.Register)
                {
                    if (IsCancelled())
                        break;

                    //if not a temp file
                    if (UpdtDetails.UpdateFiles[i].RelativePath.Length >= 4 &&
                        UpdtDetails.UpdateFiles[i].RelativePath.Substring(0, 4) != "temp")
                    {
                        //optimize (ngen) the file
                        string filename = FixUpdateDetailsPaths(UpdtDetails.UpdateFiles[i].RelativePath);

                        if (UpdtDetails.UpdateFiles[i].IsNETAssembly)
                        {
                            //TODO: add proper rolling back of newly NGENed files
                            if (!string.IsNullOrEmpty(filename))
                                NGenInstall(filename, UpdtDetails.UpdateFiles[i]); //optimize the file
                        }
                        else
                        {
                            try
                            {
                                RegisterDllServer(filename, false);

                                // add to the rollback list
                                rollbackCOM.Add(new UninstallFileInfo { Path = filename, RegisterCOMDll = COMRegistration.UnRegister });
                            }
                            catch (Exception ex)
                            {
                                except = ex;
                                break;
                            }
                        }
                    }
                }
            }

            RollbackUpdate.WriteRollbackCOM(Path.Combine(TempDirectory, "backup\\reggedComList.bak"), rollbackCOM);

            bw.ReportProgress(0, new object[] { GetRelativeProgess(6, 50), 50, string.Empty, ProgressStatus.None, null });

            if (!IsCancelled() && except == null)
            {
                // execute files
                for (int i = 0; i < UpdtDetails.UpdateFiles.Count; i++)
                {
                    if (UpdtDetails.UpdateFiles[i].Execute && !UpdtDetails.UpdateFiles[i].ExBeforeUpdate)
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            // use the absolute path
                            FileName =
                                FixUpdateDetailsPaths(UpdtDetails.UpdateFiles[i].RelativePath)
                        };

                        if (!string.IsNullOrEmpty(psi.FileName))
                        {
                            // command line arguments
                            if (!string.IsNullOrEmpty(UpdtDetails.UpdateFiles[i].CommandLineArgs))
                                psi.Arguments = ParseText(UpdtDetails.UpdateFiles[i].CommandLineArgs);

                            psi.WindowStyle = UpdtDetails.UpdateFiles[i].ProcessWindowStyle;

                            // start the process
                            Process p = Process.Start(psi);

                            if (UpdtDetails.UpdateFiles[i].WaitForExecution && p != null)
                            {
                                p.WaitForExit();

                                // if we're rolling back on non-zero return codes, the return code is non-zero, and it's not in the exception list
                                if (UpdtDetails.UpdateFiles[i].RollbackOnNonZeroRet && p.ExitCode != 0 && (UpdtDetails.UpdateFiles[i].RetExceptions == null
                                    || !UpdtDetails.UpdateFiles[i].RetExceptions.Contains(p.ExitCode)))
                                {
                                    except = new Exception("\"" + psi.FileName + "\" returned " + p.ExitCode + ".");
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (!IsCancelled() && except == null)
            {
                try
                {
                    // try to start services
                    foreach (StartService service in UpdtDetails.ServicesToStart)
                    {
                        // skip the start service if it will be started as part of the auto-update process
                        if (SkipStartService != null && string.Compare(SkipStartService, service.Name, StringComparison.OrdinalIgnoreCase) == 0)
                            continue;

                        using (ServiceController srvc = new ServiceController(service.Name))
                        {
                            ServiceControllerStatus status = srvc.Status;

                            if (status != ServiceControllerStatus.Running)
                            {
                                if (service.Arguments != null)
                                {
                                    // parse the arguments for variables
                                    for (int i = 0; i < service.Arguments.Length; i++)
                                        service.Arguments[i] = ParseText(service.Arguments[i]);

                                    // start the service with the arguments
                                    srvc.Start(service.Arguments);
                                }
                                else // no arguments
                                    srvc.Start();

                                // report that we're waiting for the service to start so the user knows what's going on
                                bw.ReportProgress(0, new object[] { GetRelativeProgess(6, 50), 50, "Waiting for service to start: " + srvc.DisplayName, ProgressStatus.None, null });

                                srvc.WaitForStatus(ServiceControllerStatus.Running);

                                startedServices.Add(service.Name);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    except = ex;
                }

                // save rollback info
                RollbackUpdate.WriteRollbackServices(Path.Combine(TempDirectory, "backup\\startedServices.bak"), startedServices);
            }

            if (IsCancelled() || except != null)
            {
                // tell the main window we're rolling back registry
                bw.ReportProgress(1, true);

                // rollback started services
                RollbackUpdate.RollbackStartedServices(TempDirectory);

                // rollback newly regged COM dlls
                RollbackUpdate.RollbackRegedCOM(TempDirectory);

                // rollback the registry
                RollbackUpdate.RollbackRegistry(TempDirectory);

                //rollback files
                bw.ReportProgress(1, false);
                RollbackUpdate.RollbackFiles(TempDirectory, ProgramDirectory);

                // rollback unregged COM
                RollbackUpdate.RollbackUnregedCOM(TempDirectory);

                // rollback stopped services
                RollbackUpdate.RollbackStoppedServices(TempDirectory);

                bw.ReportProgress(0, new object[] { -1, -1, string.Empty, ProgressStatus.Failure, except });
            }
            else
            {
                bw.ReportProgress(0, new object[] { -1, -1, string.Empty, ProgressStatus.Success, null });
            }
        }

        void bw_RunWorkerCompletedOptimizeExecute(object sender, RunWorkerCompletedEventArgs e)
        {
            bw.DoWork -= bw_DoWorkOptimizeExecute;
            bw.ProgressChanged -= bw_ProgressChanged;
            bw.RunWorkerCompleted -= bw_RunWorkerCompletedOptimizeExecute;
        }

        static void GetFrameworkV2_0Directories()
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
                            frameworkV2_0Dirs = fDirs.ToArray();
                            return;
                        }
                    }
                }
            }
        }

        static void GetFrameworkV4_0Directories()
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

                        string frameDir = Path.Combine(dir.FullName, "Framework\\v4.0.30319");

                        if (Directory.Exists(frameDir))
                            fDirs.Add(frameDir);

                        frameDir = Path.Combine(dir.FullName, "Framework64\\v4.0.30319");

                        if (Directory.Exists(frameDir))
                            fDirs.Add(frameDir);

                        if (fDirs.Count != 0)
                        {
                            frameworkV4_0Dirs = fDirs.ToArray();
                            return;
                        }
                    }
                }
            }
        }

        static void NGenInstall(string filename, UpdateFile updateFile)
        {
            string[] dirs;
            switch (updateFile.FrameworkVersion)
            {
                case FrameworkVersion.Net2_0:
                    if (frameworkV2_0Dirs == null)
                        GetFrameworkV2_0Directories();

                    dirs = frameworkV2_0Dirs;
                    break;
                case FrameworkVersion.Net4_0:
                    if (frameworkV4_0Dirs == null)
                        GetFrameworkV4_0Directories();

                    dirs = frameworkV4_0Dirs;
                    break;
                default:
                    // skip unknown .NET framework versions
                    return;
            }

            //TODO: install .NET 4.0 (or 2.0) preemptively if any assemblies are included
            if (dirs == null)
                return;

            Process proc = new Process
            {
                StartInfo =
                {
                    FileName = Path.Combine(dirs[updateFile.CPUVersion == CPUVersion.x86 ? 0 : dirs.Length - 1], "ngen.exe"),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = " install \"" + filename + "\"" + " /nologo"
                }
            };

            proc.Start();

            proc.WaitForExit();
        }

        static void NGenUninstall(string filename, UninstallFileInfo uninstallFile)
        {
            string[] dirs;
            switch (uninstallFile.FrameworkVersion)
            {
                case FrameworkVersion.Net2_0:
                    if (frameworkV2_0Dirs == null)
                        GetFrameworkV2_0Directories();

                    dirs = frameworkV2_0Dirs;
                    break;
                case FrameworkVersion.Net4_0:
                    if (frameworkV4_0Dirs == null)
                        GetFrameworkV4_0Directories();

                    dirs = frameworkV4_0Dirs;
                    break;
                default:
                    // skip unknown .NET framework versions
                    return;
            }

            //TODO: install .NET 4.0 (or 2.0) preemptively if any assemblies are included
            if (dirs == null)
                return;

            Process proc = new Process
            {
                StartInfo =
                {
                    FileName = Path.Combine(dirs[uninstallFile.CPUVersion == CPUVersion.x86 ? 0 : dirs.Length - 1], "ngen.exe"),
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
        public static void RegisterDllServer(string DllPath, bool Uninstall)
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
                        throw new Exception(
                            "RegSvr32 failed - Failed to load the module, you may need to check for problems with dependencies. File: " +
                            DllPath);
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

        static void RegAsm(string filename, bool Uninstall, CPUVersion cpu, FrameworkVersion frameworkVersion)
        {
            switch (frameworkVersion)
            {
                case FrameworkVersion.Net2_0:
                    if (frameworkV2_0Dirs == null)
                        GetFrameworkV2_0Directories();
                    break;
                case FrameworkVersion.Net4_0:
                    if (frameworkV4_0Dirs == null)
                        GetFrameworkV4_0Directories();
                    break;
                    //TODO: throw an error on unknown framework types
            }

            if (cpu == CPUVersion.x64 && ((frameworkVersion == FrameworkVersion.Net2_0 && frameworkV2_0Dirs.Length < 2) || (frameworkVersion == FrameworkVersion.Net4_0 && frameworkV4_0Dirs.Length < 2)))
                throw new Exception("Cannot register an x64 DLL on an x86 machine.");

            // call 32-bit regasm
            if (cpu != CPUVersion.x64)
            {
                using (Process p = new Process
                                       {
                                           StartInfo =
                                               {
                                                   FileName = frameworkV2_0Dirs[0] + "\\RegAsm.exe",
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
            if (cpu == CPUVersion.x64 || cpu == CPUVersion.AnyCPU && frameworkV2_0Dirs.Length == 2)
            {
                using (Process p = new Process
                                       {
                                           StartInfo =
                                               {
                                                   FileName = frameworkV2_0Dirs[1] + "\\RegAsm.exe",
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