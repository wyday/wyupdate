using System;
using System.Diagnostics;
using System.IO;
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

        bool onlyUpdatingLocalUser = false;

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
            if ((updateFrom.InstallingTo & InstallingTo.BaseDir) != 0 && !SystemFolders.IsDirInDir(userProfileFolder, baseDirectory))
                return false;

            //if the client data file isn't in the userprofile folder
            if (!SystemFolders.IsFileInDirectory(userProfileFolder, clientFileLoc))
                return false;

            // when self-updating, if this client isn't in the userprofile folder
            if ((SelfUpdateState == SelfUpdateState.WillUpdate
                || SelfUpdateState == SelfUpdateState.FullUpdate
                || SelfUpdateState == SelfUpdateState.Extracted)
                && !SystemFolders.IsFileInDirectory(userProfileFolder, Application.ExecutablePath))
            {
                return false;
            }

            //it's not changing anything outside the user profile folder
            onlyUpdatingLocalUser = true;
            return true;
        }
    }
}