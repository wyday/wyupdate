using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using wyUpdate.Common;

namespace wyUpdate
{
    partial class InstallUpdate
    {
        public void RunUpdateClientDataFile()
        {
            bw.DoWork += bw_DoWorkClientData;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_RunWorkerCompletedClientData;

            bw.RunWorkerAsync();
        }

        void bw_DoWorkClientData(object sender, DoWorkEventArgs e)
        {
            Exception except = null;

            try
            {
                OutputDirectory = Path.Combine(TempDirectory, "ClientData");
                Directory.CreateDirectory(OutputDirectory);

                string oldClientFile = null;

                // see if a 1.1+ client file exists (client.wyc)
                if (ClientFileType != ClientFileType.Final
                    && File.Exists(Path.Combine(Path.GetDirectoryName(Filename), "client.wyc")))
                {
                    oldClientFile = Filename;
                    Filename = Path.Combine(Path.GetDirectoryName(Filename), "client.wyc");
                    ClientFileType = ClientFileType.Final;
                }


                if (ClientFileType == ClientFileType.PreRC2)
                {
                    //convert pre-RC2 client file by saving images to disk
                    string tempImageFilename;

                    //create the top image
                    if (ClientFile.TopImage != null)
                    {
                        ClientFile.TopImageFilename = "t.png";

                        tempImageFilename = Path.Combine(OutputDirectory, "t.png");
                        ClientFile.TopImage.Save(tempImageFilename, System.Drawing.Imaging.ImageFormat.Png);
                    }

                    //create the side image
                    if (ClientFile.SideImage != null)
                    {
                        ClientFile.SideImageFilename = "s.png";

                        tempImageFilename = Path.Combine(OutputDirectory, "s.png");
                        ClientFile.SideImage.Save(tempImageFilename, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                else
                {
                    //Extract the contents of the client data file
                    ExtractUpdateFile();

                    if (File.Exists(Path.Combine(OutputDirectory, "iuclient.iuc")))
                    {
                        // load and merge the existing file

                        ClientFile tempClientFile = new ClientFile();
                        tempClientFile.LoadClientData(Path.Combine(OutputDirectory, "iuclient.iuc"));
                        tempClientFile.InstalledVersion = ClientFile.InstalledVersion;
                        ClientFile = tempClientFile;

                        File.Delete(Path.Combine(OutputDirectory, "iuclient.iuc"));
                    }
                }

                List<UpdateFile> updateDetailsFiles = UpdtDetails.UpdateFiles;

                FixUpdateFilesPaths(updateDetailsFiles);


                //write the uninstall file
                RollbackUpdate.WriteUninstallFile(TempDirectory, Path.Combine(OutputDirectory, "uninstall.dat"), updateDetailsFiles);

                List<UpdateFile> files = new List<UpdateFile>();

                //add all the files in the outputDirectory
                AddFiles(OutputDirectory.Length + 1, OutputDirectory, files);

                //recompress all the client data files
                string tempClient = Path.Combine(TempDirectory, "client.file");
                ClientFile.SaveClientFile(files, tempClient);

                // overrite existing client.wyc, while keeping the file attributes

                FileAttributes atr = FileAttributes.Normal;

                if (File.Exists(Filename))
                    atr = File.GetAttributes(Filename);

                bool resetAttributes = (atr & FileAttributes.Hidden) != 0 || (atr & FileAttributes.ReadOnly) != 0 || (atr & FileAttributes.System) != 0;

                // remove the ReadOnly & Hidden atributes temporarily
                if (resetAttributes)
                    File.SetAttributes(Filename, FileAttributes.Normal);

                //replace the original
                File.Copy(tempClient, Filename, true);

                if (resetAttributes)
                    File.SetAttributes(Filename, atr);


                if (oldClientFile != null)
                {
                    // delete the old client file
                    File.Delete(oldClientFile);
                }
            }
            catch (Exception ex)
            {
                // handle failed to write to client file
                except = ex;
            }

            if (except != null)
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

        void bw_RunWorkerCompletedClientData(object sender, RunWorkerCompletedEventArgs e)
        {
            bw.DoWork -= bw_DoWorkClientData;
            bw.ProgressChanged -= bw_ProgressChanged;
            bw.RunWorkerCompleted -= bw_RunWorkerCompletedClientData;
        }

        //creates list of files to add to client data file
        static void AddFiles(int charsToTrim, string dir, List<UpdateFile> files)
        {
            string[] filenames = Directory.GetFiles(dir);
            string[] dirs = Directory.GetDirectories(dir);

            foreach (string file in filenames)
                files.Add(new UpdateFile { Filename = file, RelativePath = file.Substring(charsToTrim) });

            foreach (string directory in dirs)
                AddFiles(charsToTrim, directory, files);
        }
    }
}