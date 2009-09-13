using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using wyUpdate.Common;
using wyUpdate.Compression.Vcdiff;

namespace wyUpdate
{
    partial class InstallUpdate
    {
        //for self update
        public string NewIUPClientLoc;
        public string OldIUPClientLoc;

        public void RunSelfUpdate()
        {
            Thread.CurrentThread.IsBackground = true; //make them a daemon

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
                KillProcess(OldIUPClientLoc);

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
                bool optimize = FindNewClient();


                //transfer new client to the directory (Note: this assumes a standalone client - i.e. no dependencies)
                File.Copy(NewIUPClientLoc, OldIUPClientLoc, true);

                //Optimize client if necessary
                if (optimize)
                    NGenInstall(OldIUPClientLoc);

                //cleanup the client update files to prevent conflicts with the product update
                File.Delete(NewIUPClientLoc);
                Directory.Delete(Path.Combine(OutputDirectory, "base"));
            }
            catch (Exception ex)
            {
                except = ex;
            }

            if (canceled || except != null)
            {
                //report cancellation
                ThreadHelper.ReportProgress(Sender, SenderDelegate, "Cancelling update...", -1, -1);

                //Delete temporary files
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

                ThreadHelper.ReportError(Sender, SenderDelegate, string.Empty, except);
            }
            else
            {
                ThreadHelper.ReportSuccess(Sender, SenderDelegate, "Self update complete");
            }
        }

        public void JustExtractSelfUpdate()
        {
            Thread.CurrentThread.IsBackground = true; //make them a daemon
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
                KillProcess(OldIUPClientLoc);

                string updtDetailsFilename = Path.Combine(OutputDirectory, "updtdetails.udt");

                if (File.Exists(updtDetailsFilename))
                {
                    UpdtDetails = UpdateDetails.Load(updtDetailsFilename);
                }


                // generate files from patches
                CreatewyUpdateFromPatch();


                //find self in Path.Combine(OutputDirectory, "base")
                bool optimize = FindNewClient();


                //transfer new client to the directory (Note: this assumes a standalone client - i.e. no dependencies)
                File.Copy(NewIUPClientLoc, OldIUPClientLoc, true);

                //Optimize client if necessary
                if (optimize)
                    NGenInstall(OldIUPClientLoc);

                //cleanup the client update files to prevent conflicts with the product update
                File.Delete(NewIUPClientLoc);
                Directory.Delete(Path.Combine(OutputDirectory, "base"));
            }
            catch (Exception ex)
            {
                except = ex;
            }



            if (canceled || except != null)
            {
                //report cancellation
                ThreadHelper.ReportProgress(Sender, SenderDelegate, "Cancelling update...", -1, -1);

                //Delete temporary files
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

                ThreadHelper.ReportError(Sender, SenderDelegate, string.Empty, except);
            }
            else
            {
                ThreadHelper.ReportSuccess(Sender, SenderDelegate, "Self update extraction complete");
            }
        }

        public void JustInstallSelfUpdate()
        {
            //TODO: 


            Thread.CurrentThread.IsBackground = true; //make them a daemon
            Exception except = null;


            try
            {
                //find self in Path.Combine(OutputDirectory, "base")
                bool optimize = FindNewClient();


                //transfer new client to the directory (Note: this assumes a standalone client - i.e. no dependencies)
                File.Copy(NewIUPClientLoc, OldIUPClientLoc, true);

                //Optimize client if necessary
                if (optimize)
                    NGenInstall(OldIUPClientLoc);

                //cleanup the client update files to prevent conflicts with the product update
                File.Delete(NewIUPClientLoc);
                Directory.Delete(Path.Combine(OutputDirectory, "base"));
            }
            catch (Exception ex)
            {
                except = ex;
            }


            if (canceled || except != null)
            {
                //report cancellation
                ThreadHelper.ReportProgress(Sender, SenderDelegate, "Cancelling update...", -1, -1);

                //Delete temporary files
                if (except != null)
                {
                    // remove the entire temp directory
                    try
                    {
                        Directory.Delete(OutputDirectory, true);
                    }
                    catch { }
                }

                ThreadHelper.ReportError(Sender, SenderDelegate, string.Empty, except);
            }
            else
            {
                ThreadHelper.ReportSuccess(Sender, SenderDelegate, "Self update complete");
            }
        }


        void CreatewyUpdateFromPatch()
        {
            // generate files from patches

            if (Directory.Exists(Path.Combine(OutputDirectory, "patches")))
            {
                // set the base directory to the home of the client file
                ProgramDirectory = Path.GetDirectoryName(OldIUPClientLoc);
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
                        using (FileStream original = File.OpenRead(OldIUPClientLoc))
                        using (FileStream patch = File.OpenRead(Path.Combine(TempDirectory, UpdtDetails.UpdateFiles[0].DeltaPatchRelativePath)))
                        using (FileStream target = File.Open(tempFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                        {
                            VcdiffDecoder.Decode(original, patch, target, UpdtDetails.UpdateFiles[0].NewFileAdler32);
                        }
                    }
                    catch
                    {
                        throw new PatchApplicationException("Patch failed to apply to " + FixUpdateDetailsPaths(UpdtDetails.UpdateFiles[0].RelativePath));
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

        bool FindNewClient()
        {
            //first search the update details file
            for (int i = 0; i < UpdtDetails.UpdateFiles.Count; i++)
            {
                if (UpdtDetails.UpdateFiles[i].IsNETAssembly)
                {
                    //optimize (ngen) the file
                    NewIUPClientLoc = Path.Combine(OutputDirectory, UpdtDetails.UpdateFiles[i].RelativePath);

                    return true;
                }
            }

            //not found yet, so keep searching
            //get a list of files in the "base" folder
            string[] files = Directory.GetFiles(Path.Combine(OutputDirectory, "base"), "*.exe", SearchOption.AllDirectories);

            if (files.Length > 0)
            {
                NewIUPClientLoc = files[0];
            }
            else
            {
                throw new Exception("Self update client couldn't be found.");
            }

            //not ngen-able
            return false;
        }

        static void KillProcess(string filename)
        {
            Process[] aProcess = Process.GetProcesses();

            foreach (Process proc in aProcess)
            {
                if (proc.MainModule != null && proc.MainModule.FileName.ToLower() == filename.ToLower())
                {
                    try
                    {
                        proc.Kill();
                    }
                    catch { }
                }
            }
        }
    }
}