using System;
using System.IO;
using System.Threading;
using Ionic.Zip;
using wyUpdate.Common;
using wyUpdate.Compression.Vcdiff;

namespace wyUpdate
{
    partial class InstallUpdate
    {
        private void ExtractUpdateFile()
        {
            using (ZipFile zip = ZipFile.Read(Filename))
            {
                int totalFiles = zip.Entries.Count;
                int filesDone = 0;

                foreach (ZipEntry e in zip)
                {
                    if (canceled)
                        break; //stop outputting new files

                    if (!SkipProgressReporting)
                    {
                        ThreadHelper.ReportProgress(Sender, SenderDelegate,
                            "Extracting " + Path.GetFileName(e.FileName),
                            totalFiles > 0 ?
                               GetRelativeProgess(1, (filesDone * 100) / totalFiles) :
                               GetRelativeProgess(1, 0));

                        filesDone++;
                    }

                    e.Extract(OutputDirectory, ExtractExistingFileAction.OverwriteSilently);  // overwrite == true
                }
            }
        }

        // unzip the update to the temp folder
        public void RunUnzipProcess()
        {
            Thread.CurrentThread.IsBackground = true; //make them a daemon

            Exception except = null;

            string updtDetailsFilename = Path.Combine(TempDirectory, "updtdetails.udt");

            try
            {
                ExtractUpdateFile();

                try
                {
                    // remove update file (it's no longer needed)
                    File.Delete(Filename);
                }
                catch (Exception) { }


                // Try to load the update details file

                if (File.Exists(updtDetailsFilename))
                {
                    UpdtDetails = new UpdateDetails();
                    UpdtDetails.Load(updtDetailsFilename);
                }

                if (Directory.Exists(Path.Combine(TempDirectory, "patches")))
                {
                    // patch the files
                    foreach (UpdateFile file in UpdtDetails.UpdateFiles)
                    {
                        if (file.DeltaPatchRelativePath != null)
                        {
                            string tempFilename = Path.Combine(TempDirectory, file.RelativePath);

                            // create the directory to store the patched file
                            if (!Directory.Exists(Path.GetDirectoryName(tempFilename)))
                                Directory.CreateDirectory(Path.GetDirectoryName(tempFilename));

                            try
                            {
                                using (FileStream original = File.OpenRead(FixUpdateDetailsPaths(file.RelativePath)))
                                using (FileStream patch = File.OpenRead(Path.Combine(TempDirectory, file.DeltaPatchRelativePath)))
                                using (FileStream target = File.Open(tempFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                                {
                                    VcdiffDecoder.Decode(original, patch, target, file.NewFileAdler32);
                                }
                            }
                            catch
                            {
                                throw new PatchApplicationException("Patch failed to apply to " + FixUpdateDetailsPaths(file.RelativePath));
                            }


                            // the 'last write time' of the patch file is really the 'lwt' of the dest. file
                            File.SetLastWriteTime(tempFilename, File.GetLastWriteTime(Path.Combine(TempDirectory, file.DeltaPatchRelativePath)));
                        }
                    }


                    try
                    {
                        // remove the patches directory (frees up a bit of space)
                        Directory.Delete(Path.Combine(TempDirectory, "patches"), true);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                except = ex;
            }


            if (canceled || except != null)
            {
                //report cancellation
                ThreadHelper.ReportProgress(Sender, SenderDelegate, "Cancelling update...", -1);

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

                    // remove the update details
                    if (File.Exists(updtDetailsFilename))
                    {
                        File.Delete(updtDetailsFilename);
                    }
                }

                ThreadHelper.ReportError(Sender, SenderDelegate, string.Empty, except);
            }
            else
            {
                ThreadHelper.ReportSuccess(Sender, SenderDelegate, "Extraction complete");
            }
        }
    }
}