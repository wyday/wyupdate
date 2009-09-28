using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using wyUpdate.Common;

namespace wyUpdate
{
    public partial class frmMain
    {
        void StartSelfElevated()
        {
            ProcessStartInfo psi = new ProcessStartInfo
                                       {
                                           ErrorDialog = true,
                                           ErrorDialogParentHandle = Handle
                                       };

            if (SelfUpdateState == SelfUpdateState.WillUpdate)
            {
                //create the filename for the newly copied client
                psi.FileName = Path.Combine(tempDirectory, Path.GetFileName(Application.ExecutablePath));

                //copy self to the temp folder
                File.Copy(Application.ExecutablePath, psi.FileName, true);
            }
            else if (SelfUpdateState == SelfUpdateState.FullUpdate)
            {
                //launch the newly updated self
                psi.FileName = oldSelfLocation;
            }
            else if (isAutoUpdateMode)
            {
                psi.FileName = IsNewSelf ? newSelfLocation : oldSelfLocation;

                // oldSelfLocation is null when elevation is needed, but no self update is taking place
                if (string.IsNullOrEmpty(psi.FileName))
                    psi.FileName = Application.ExecutablePath;
            }
            else
                psi.FileName = Application.ExecutablePath;

            if (needElevation)
                psi.Verb = "runas"; //elevate to administrator

            try
            {
                // write necessary info (base/temp dirs, new client files, etc.) to a file
                SaveSelfUpdateData(Path.Combine(tempDirectory, "selfUpdate.sup"));

                psi.Arguments = "-supdf:\"" + Path.Combine(tempDirectory, "selfUpdate.sup") + "\"";

                Process.Start(psi);
                Close();
            }
            catch (Exception ex)
            {
                // the process couldn't be started. This happens for 1 of 3 reasons:

                // 1. The user cancelled the UAC box
                // 2. The limited user tried to elevate to an Admin that has a blank password
                // 3. The limited user tries to elevate as a Guest account
                error = clientLang.AdminError;
                errorDetails = ex.Message;

                ShowFrame(Frame.Error);
            }
        }

        bool NeedElevationToUpdate()
        {
            bool willSelfUpdate = (SelfUpdateState == SelfUpdateState.WillUpdate ||
                                   SelfUpdateState == SelfUpdateState.FullUpdate ||
                                   SelfUpdateState == SelfUpdateState.Extracted);

            // no elevation necessary if it's not overwriting important files
            if (IsAdmin || (updateFrom.InstallingTo == 0 && updateFrom.RegChanges.Count == 0 && !willSelfUpdate))
                return false;

            try
            {
                // if only updating local user files, no elevation is needed
                if (OnlyUpdatingLocalUser())
                    return false;

                // UAC Shield on next button for Windows Vista+
                if (VistaTools.AtLeastVista())
                    VistaTools.SetButtonShield(btnNext, true);
            }
            catch { }

            return true;
        }

        bool OnlyUpdatingLocalUser()
        {
            //Vista only check when the client isn't already 
            // running with Admin (and elevated) priviledges

            //Elevation is needed...

            //if updating any registry other than HKEY_CURRENT_USER
            foreach (RegChange reg in updateFrom.RegChanges)
                if (reg.RegBasekey != RegBasekeys.HKEY_CURRENT_USER) return false;

            //if installing to the system folder or one of the common folders
            if (updateFrom.InstallingTo != 0 && (updateFrom.InstallingTo & InstallingTo.BaseDir) == 0)
                return false;

            string userProfileFolder = Environment.GetEnvironmentVariable("userprofile");

            //if the basedirectory isn't in the userprofile folder (C:\Users\UserName)
            if ((updateFrom.InstallingTo & InstallingTo.BaseDir) != 0 && !IsDirInDir(userProfileFolder, baseDirectory))
                return false;

            //if the client data file isn't in the userprofile folder
            if (!IsFileInDirectory(userProfileFolder, clientFileLoc))
                return false;

            // when self-updating, if this client isn't in the userprofile folder
            if ((SelfUpdateState == SelfUpdateState.WillUpdate
                || SelfUpdateState == SelfUpdateState.FullUpdate
                || SelfUpdateState == SelfUpdateState.Extracted)
                && !IsFileInDirectory(userProfileFolder, Application.ExecutablePath))
            {
                return false;
            }

            //it's not changing anything outside the user profile folder
            return true;
        }

        static bool IsFileInDirectory(string dir, string file)
        {
            StringBuilder strBuild = new StringBuilder(InstallUpdate.MAX_PATH);

            bool bRet = InstallUpdate.PathRelativePathTo(
                strBuild,
                dir, (uint)InstallUpdate.PathAttribute.Directory,
                file, (uint)InstallUpdate.PathAttribute.File
            );

            if (bRet && strBuild.Length >= 2)
            {
                //get the first two characters
                if (strBuild.ToString().Substring(0, 2) == @".\")
                {
                    //if file is in the directory (or a subfolder)
                    return true;
                }
            }

            return false;
        }

        static bool IsDirInDir(string dir, string checkDir)
        {
            StringBuilder strBuild = new StringBuilder(InstallUpdate.MAX_PATH);

            bool bRet = InstallUpdate.PathRelativePathTo(
                strBuild,
                dir, (uint)InstallUpdate.PathAttribute.Directory,
                checkDir, (uint)InstallUpdate.PathAttribute.Directory
            );

            if (bRet)
            {
                if (strBuild.Length == 1) //result is "."
                    return true;

                if (strBuild.Length >= 2
                    //get the first two characters
                    && strBuild.ToString().Substring(0, 2) == @".\")
                {
                    //if checkDir is the directory (or a subfolder)
                    return true;
                }
            }

            return false;
        }
    }
}