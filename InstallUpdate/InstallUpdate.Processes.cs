using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using wyUpdate.Common;

namespace wyUpdate
{
    partial class InstallUpdate
    {
        public void RunProcessesCheck()
        {
            // processes
            List<FileInfo> files = null;
            List<Process> rProcesses = null;

            // rollback list for stopped services
            List<string> stoppedServices = new List<string>();
            Exception except = null; // store any errors

            // create the backup folder
            Directory.CreateDirectory(Path.Combine(TempDirectory, "backup"));

            try
            {
                // if we're AutoUpdating the service has been shutdown by the AutomaticUpdaterBackend
                // and we should wait for the service to complete shutting down
                if (SkipStartService != null)
                {
                    using (ServiceController srvc = new ServiceController(SkipStartService))
                    {
                        if (srvc.Status != ServiceControllerStatus.Stopped)
                            srvc.WaitForStatus(ServiceControllerStatus.Stopped); 
                    }
                }

                // first try to stop services
                foreach (string service in UpdtDetails.ServicesToStop)
                {
                    using (ServiceController srvc = new ServiceController(service))
                    {
                        ServiceControllerStatus status = ServiceControllerStatus.Stopped;

                        try
                        {
                            // non-existent services throw an exception on queries
                            status = srvc.Status;
                        }
                        catch { }

                        if (status == ServiceControllerStatus.Running)
                        {
                            try
                            {
                                srvc.Stop();
                            }
                            catch (Exception)
                            {
                                // get the latest status of the service -- it might have crashed close
                                srvc.Refresh();
                                if (srvc.Status != ServiceControllerStatus.Stopped)
                                    throw;
                            }

                            // report that we're waiting for the service to stop so the user knows what's going on
                            ReportProcProgress("Waiting for service to stop: " + srvc.DisplayName);

                            srvc.WaitForStatus(ServiceControllerStatus.Stopped);

                            stoppedServices.Add(service);
                        }
                    }
                }

                files = new List<FileInfo>(new DirectoryInfo(ProgramDirectory).GetFiles("*.exe", SearchOption.AllDirectories));

                RemoveSelfFromProcesses(files);

                //check for (and delete) a newer client if it exists
                DeleteClientInPath(ProgramDirectory, Path.Combine(TempDirectory, "base"));

                rProcesses = ProcessesNeedClosing(files);

                if (rProcesses.Count == 0)
                {
                    // no processes need closing, all done
                    files = null;
                    rProcesses = null;
                }
                else if (SkipUIReporting) // and rProcesses.Count > 0
                {
                    // check every second for 20 seconds.
                    for (int i = 0; i < 20; ++i)
                    {
                        // sleep for 1 second
                        Thread.Sleep(1000);

                        rProcesses = ProcessesNeedClosing(files);

                        if (rProcesses.Count == 0)
                            break;
                    }

                    if (rProcesses.Count != 0)
                    {
                        StringBuilder sb = new StringBuilder();

                        sb.AppendLine(rProcesses.Count + " processes are running:\r\n");

                        foreach (Process proc in rProcesses)
                        {
                            sb.AppendLine(proc.MainWindowTitle + " (" + proc.ProcessName + ".exe)");
                        }

                        // tell the user about the open processes
                        throw new Exception(sb.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                except = ex;
            }

            // save rollback info
            RollbackUpdate.WriteRollbackServices(Path.Combine(TempDirectory, "backup\\stoppedServices.bak"), stoppedServices);

            if (canceled || except != null)
            {
                ThreadHelper.ChangeRollback(Sender, RollbackDelegate, false);

                // rollback stopped services
                RollbackUpdate.RollbackStoppedServices(TempDirectory);

                ReportProcErrorOrSuccess(null, null, except);
            }
            else // completed successfully
            {
                ReportProcErrorOrSuccess(files, rProcesses, null);
            }
        }

        void ReportProcErrorOrSuccess(List<FileInfo> files, List<Process> rProcesses, Exception ex)
        {
            /*
             *
             * The reason for the do...while and the try...catch is that when an error
             * occurrs very quickly, and the windows is locked (say for repainting efficiency)
             * the .BeginInvoke will fail. Thus, I should keep retrying until it eventually succeeds.
             * 
            */

            do
            {
                try
                {
                    //Try to send our error to the frmMain thread - wait until it succeeds

                    // NOTE: a -1 for progress assures that the progress bar won't be reset

                    // eat any messages after the sender closes (aka IsDisposed)
                    if (Sender.IsDisposed)
                        return;

                    Sender.BeginInvoke(SenderDelegate, new object[] { files, rProcesses, true, string.Empty, ex });
                    break;
                }
                catch { }

            } while (true);
        }

        void ReportProcProgress(string text)
        {
            try
            {
                // eat any messages after the sender closes (aka IsDisposed)
                if (Sender.IsDisposed)
                    return;

                Sender.BeginInvoke(SenderDelegate, new object[] { null, null, false, text, null });
            }
            catch
            {
                // don't bother with the exception (it doesn't matter if the main window misses a progress report)
            }
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