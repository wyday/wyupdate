using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using wyUpdate.Common;
using wyUpdate.Compression.Vcdiff;

namespace wyUpdate
{
    partial class InstallUpdate
    {
        //for self update
        public string NewSelfLoc;
        public string OldSelfLoc;

        public void RunSelfUpdate()
        {
            bw.DoWork += bw_DoWorkSelfUpdate;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_RunWorkerCompletedSelfUpdate;

            bw.RunWorkerAsync();
        }

        void bw_DoWorkSelfUpdate(object sender, DoWorkEventArgs e)
        {
            Exception except = null;

            try
            {
                //extract downloaded self update
                ExtractUpdateFile();

                try
                {
                    // remove update file (it's no longer needed)
                    File.Delete(Filename);
                }
                catch { }

                //find and forcibly close oldClientLocation
                KillProcess(OldSelfLoc);

                string updtDetailsFilename = Path.Combine(OutputDirectory, "updtdetails.udt");

                if (File.Exists(updtDetailsFilename))
                {
                    UpdtDetails = UpdateDetails.Load(updtDetailsFilename);

                    //remove the file to prevent conflicts with the regular product update
                    File.Delete(updtDetailsFilename);
                }


                // generate files from patches
                CreatewyUpdateFromPatch();


                //find self in Path.Combine(OutputDirectory, "base")
                UpdateFile updateFile = FindNewClient();

                FileAttributes atr = File.GetAttributes(OldSelfLoc);
                bool resetAttributes = (atr & FileAttributes.Hidden) != 0 || (atr & FileAttributes.ReadOnly) != 0 || (atr & FileAttributes.System) != 0;

                // remove the ReadOnly & Hidden atributes temporarily
                if (resetAttributes)
                    File.SetAttributes(OldSelfLoc, FileAttributes.Normal);

                //transfer new client to the directory (Note: this assumes a standalone wyUpdate - i.e. no dependencies)
                File.Copy(NewSelfLoc, OldSelfLoc, true);

                if (resetAttributes)
                    File.SetAttributes(OldSelfLoc, atr);

                //Optimize client if necessary
                if (updateFile != null)
                    NGenInstall(OldSelfLoc, updateFile);

                //cleanup the client update files to prevent conflicts with the product update
                File.Delete(NewSelfLoc);
                Directory.Delete(Path.Combine(OutputDirectory, "base"));
            }
            catch (Exception ex)
            {
                except = ex;
            }

            if (IsCancelled() || except != null)
            {
                // report cancellation
                bw.ReportProgress(0, new object[] { -1, -1, "Cancelling update...", ProgressStatus.None, null });

                // Delete temporary files
                if (except != null && except.GetType() != typeof(PatchApplicationException))
                {
                    // remove the entire temp directory
                    try
                    {
                        Directory.Delete(OutputDirectory, true);
                    }
                    catch { }
                }
                else
                {
                    //only 'gut' the folder leaving the server file

                    string[] dirs = Directory.GetDirectories(TempDirectory);

                    foreach (string dir in dirs)
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                        }
                        catch { }
                    }
                }

                bw.ReportProgress(0, new object[] { -1, -1, string.Empty, ProgressStatus.Failure, except });
            }
            else
            {
                bw.ReportProgress(0, new object[] { -1, -1, "Self update complete", ProgressStatus.Success, null });
            }
        }

        void bw_RunWorkerCompletedSelfUpdate(object sender, RunWorkerCompletedEventArgs e)
        {
            bw.DoWork -= bw_DoWorkSelfUpdate;
            bw.ProgressChanged -= bw_ProgressChanged;
            bw.RunWorkerCompleted -= bw_RunWorkerCompletedSelfUpdate;
        }

        public void JustExtractSelfUpdate()
        {
            bw.DoWork += bw_DoWorkExtractSelfUpdate;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_RunWorkerCompletedExtractSelfUpdate;

            bw.RunWorkerAsync();
        }

        void bw_DoWorkExtractSelfUpdate(object sender, DoWorkEventArgs e)
        {
            Exception except = null;

            try
            {
                if (!Directory.Exists(OutputDirectory))
                    Directory.CreateDirectory(OutputDirectory);

                //extract downloaded self update
                ExtractUpdateFile();

                try
                {
                    // remove update file (it's no longer needed)
                    File.Delete(Filename);
                }
                catch { }


                string updtDetailsFilename = Path.Combine(OutputDirectory, "updtdetails.udt");

                if (File.Exists(updtDetailsFilename))
                {
                    UpdtDetails = UpdateDetails.Load(updtDetailsFilename);
                }


                // generate files from patches
                CreatewyUpdateFromPatch();


                //find self in Path.Combine(OutputDirectory, "base")
                FindNewClient();
            }
            catch (Exception ex)
            {
                except = ex;
            }

            if (IsCancelled() || except != null)
            {
                // report cancellation
                bw.ReportProgress(0, new object[] { -1, -1, "Cancelling update...", ProgressStatus.None, null });

                // Delete temporary files
                if (except != null)
                {
                    // remove the entire temp directory
                    try
                    {
                        Directory.Delete(OutputDirectory, true);
                    }
                    catch { }
                }

                bw.ReportProgress(0, new object[] { -1, -1, string.Empty, ProgressStatus.Failure, except });
            }
            else
            {
                bw.ReportProgress(0, new object[] { -1, -1, "Self update extraction complete", ProgressStatus.Success, null });
            }
        }

        void bw_RunWorkerCompletedExtractSelfUpdate(object sender, RunWorkerCompletedEventArgs e)
        {
            bw.DoWork -= bw_DoWorkExtractSelfUpdate;
            bw.ProgressChanged -= bw_ProgressChanged;
            bw.RunWorkerCompleted -= bw_RunWorkerCompletedExtractSelfUpdate;
        }

        public void JustInstallSelfUpdate()
        {
            bw.DoWork += bw_DoWorkInstallSelfUpdate;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_RunWorkerCompletedInstallSelfUpdate;

            bw.RunWorkerAsync();
        }

        void bw_DoWorkInstallSelfUpdate(object sender, DoWorkEventArgs e)
        {
            Exception except = null;

            try
            {
                string updtDetailsFilename = Path.Combine(OutputDirectory, "updtdetails.udt");

                if (File.Exists(updtDetailsFilename))
                {
                    UpdtDetails = UpdateDetails.Load(updtDetailsFilename);
                }

                //find self in Path.Combine(OutputDirectory, "base")
                UpdateFile updateFile = FindNewClient();

                //find and forcibly close oldClientLocation
                KillProcess(OldSelfLoc);


                FileAttributes atr = File.GetAttributes(OldSelfLoc);
                bool resetAttributes = (atr & FileAttributes.Hidden) != 0 || (atr & FileAttributes.ReadOnly) != 0 || (atr & FileAttributes.System) != 0;

                // remove the ReadOnly & Hidden atributes temporarily
                if (resetAttributes)
                    File.SetAttributes(OldSelfLoc, FileAttributes.Normal);

                //transfer new client to the directory (Note: this assumes a standalone client - i.e. no dependencies)
                File.Copy(NewSelfLoc, OldSelfLoc, true);

                if (resetAttributes)
                    File.SetAttributes(OldSelfLoc, atr);


                //Optimize client if necessary
                if (updateFile != null)
                    NGenInstall(OldSelfLoc, updateFile);
            }
            catch (Exception ex)
            {
                except = ex;
            }


            if (IsCancelled() || except != null)
            {
                // report cancellation
                bw.ReportProgress(0, new object[] { -1, -1, "Cancelling update...", ProgressStatus.None, null });

                // Delete temporary files
                if (except != null)
                {
                    // remove the entire temp directory
                    try
                    {
                        Directory.Delete(OutputDirectory, true);
                    }
                    catch { }
                }

                bw.ReportProgress(0, new object[] { -1, -1, string.Empty, ProgressStatus.Failure, except });
            }
            else
            {
                bw.ReportProgress(0, new object[] { -1, -1, "Self update complete", ProgressStatus.Success, null });
            }
        }

        void bw_RunWorkerCompletedInstallSelfUpdate(object sender, RunWorkerCompletedEventArgs e)
        {
            bw.DoWork -= bw_DoWorkInstallSelfUpdate;
            bw.ProgressChanged -= bw_ProgressChanged;
            bw.RunWorkerCompleted -= bw_RunWorkerCompletedInstallSelfUpdate;
        }

        void CreatewyUpdateFromPatch()
        {
            // generate files from patches

            if (Directory.Exists(Path.Combine(OutputDirectory, "patches")))
            {
                // set the base directory to the home of the client file
                ProgramDirectory = Path.GetDirectoryName(OldSelfLoc);
                TempDirectory = OutputDirectory;

                // patch the file (assume only one - wyUpdate.exe)

                if (UpdtDetails.UpdateFiles[0].DeltaPatchRelativePath != null)
                {
                    string tempFilename = Path.Combine(TempDirectory, UpdtDetails.UpdateFiles[0].RelativePath);

                    // create the directory to store the patched file
                    if (!Directory.Exists(Path.GetDirectoryName(tempFilename)))
                        Directory.CreateDirectory(Path.GetDirectoryName(tempFilename));

                    try
                    {
                        using (FileStream original = File.OpenRead(OldSelfLoc))
                        using (FileStream patch = File.OpenRead(Path.Combine(TempDirectory, UpdtDetails.UpdateFiles[0].DeltaPatchRelativePath)))
                        using (FileStream target = File.Open(tempFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                        {
                            VcdiffDecoder.Decode(original, patch, target, UpdtDetails.UpdateFiles[0].NewFileAdler32);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new PatchApplicationException("Patch failed to apply to this file: " + FixUpdateDetailsPaths(UpdtDetails.UpdateFiles[0].RelativePath) + "\r\n\r\nBecause that file failed to patch, and there's no \"catch-all\" update to download, the update failed to apply. The failure to patch usually happens because the file was modified from the original version. Reinstall the original version of this app.\r\n\r\n\r\nInternal error: " + ex.Message);
                    }

                    // the 'last write time' of the patch file is really the 'lwt' of the dest. file
                    File.SetLastWriteTime(tempFilename, File.GetLastWriteTime(Path.Combine(TempDirectory, UpdtDetails.UpdateFiles[0].DeltaPatchRelativePath)));
                }


                try
                {
                    // remove the patches directory (frees up a bit of space)
                    Directory.Delete(Path.Combine(TempDirectory, "patches"), true);
                }
                catch { }
            }
        }

        UpdateFile FindNewClient()
        {
            //first search the update details file
            for (int i = 0; i < UpdtDetails.UpdateFiles.Count; i++)
            {
                if (UpdtDetails.UpdateFiles[i].IsNETAssembly)
                {
                    //optimize (ngen) the file
                    NewSelfLoc = Path.Combine(OutputDirectory, UpdtDetails.UpdateFiles[i].RelativePath);

                    return UpdtDetails.UpdateFiles[i];
                }
            }

            //not found yet, so keep searching
            //get a list of files in the "base" folder
            string[] files = Directory.GetFiles(Path.Combine(OutputDirectory, "base"), "*.exe", SearchOption.AllDirectories);

            if (files.Length > 0)
            {
                NewSelfLoc = files[0];
            }
            else
            {
                throw new Exception("New wyUpdate couldn't be found.");
            }

            //not ngen-able
            return null;
        }

        static void KillProcess(string filename)
        {
            Process[] aProcess = Process.GetProcesses();

            foreach (Process proc in aProcess)
            {
                //The Try{} block needs to be outside the if statement because 'proc.MainModule'
                // can throw an exception in more than one case (x64 processes for x86 wyUpdate, 
                // permissions for Vista / 7, etc.) wyUpdate will be detected despite the try/catch block.
                try
                {
                    if (proc.MainModule.FileName.ToLower() == filename.ToLower())
                    {
                        proc.Kill();
                    }
                }
                catch { }
            }
        }
    }
}