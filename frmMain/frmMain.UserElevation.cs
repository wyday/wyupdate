using System;
using System.Diagnostics;
using System.IO;
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
                psi.FileName = Path.Combine(tempDirectory, Path.GetFileName(VersionTools.SelfLocation));

                //copy self to the temp folder
                File.Copy(VersionTools.SelfLocation, psi.FileName, true);
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
                    psi.FileName = VersionTools.SelfLocation;
            }
            else
                psi.FileName = VersionTools.SelfLocation;

            if (needElevation)
                psi.Verb = "runas"; //elevate to administrator

            try
            {
                string selfUpdatePath = Path.Combine(tempDirectory, "selfUpdate.sup");

                // write necessary info (base/temp dirs, new client files, etc.) to a file
                SaveSelfUpdateData(selfUpdatePath);

                psi.Arguments = "-supdf:\"" + selfUpdatePath + "\"";

                if (IsNewSelf)
                    psi.Arguments += " /ns";

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
            // if only updating local user files, no elevation is needed
            if (IsAdmin || OnlyUpdatingLocalUser())
                return false;

            // UAC Shield on next button for Windows Vista+
            if (VistaTools.AtLeastVista())
                VistaTools.SetButtonShield(btnNext, true);

            return true;
        }

        //TODO: perhaps use some more flexible method for detecting limited users
        //      given permission to access certain folders they don't normally have access
        //      to. See: http://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder
        //      The same concept can be applied to the registry.
        bool OnlyUpdatingLocalUser()
        {
            // if installing
            //         - system folders
            //         - non-user registry
            //         - Windows Services
            //         - COM files
            // then return false
            // Also note how we're excluding the "BaseDir".
            // This is because the base directory may or may not be in the userprofile
            // directory, thus it needs a separate check.
            if (((updateFrom.InstallingTo | InstallingTo.BaseDir) ^ InstallingTo.BaseDir) != 0)
                return false;

            string userProfileFolder = Environment.GetEnvironmentVariable("userprofile");

            // if the basedir isn't in the userprofile folder (C:\Users\UserName)
            if ((updateFrom.InstallingTo & InstallingTo.BaseDir) != 0 && !SystemFolders.IsDirInDir(userProfileFolder, baseDirectory))
                return false;

            // if the client data file isn't in the userprofile folder
            if (!SystemFolders.IsFileInDirectory(userProfileFolder, clientFileLoc))
                return false;

            // when self-updating, if this client isn't in the userprofile folder
            if ((SelfUpdateState == SelfUpdateState.WillUpdate
                || SelfUpdateState == SelfUpdateState.FullUpdate
                || SelfUpdateState == SelfUpdateState.Extracted)
                && !SystemFolders.IsFileInDirectory(userProfileFolder, VersionTools.SelfLocation))
            {
                return false;
            }

            return true;
        }
    }
}