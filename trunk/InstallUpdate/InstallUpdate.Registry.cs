using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using wyUpdate.Common;

namespace wyUpdate
{
    partial class InstallUpdate
    {
        private void UpdateRegistry(List<RegChange> rollbackRegistry)
        {
            int i = 0;

            ThreadHelper.ReportProgress(Sender, SenderDelegate, string.Empty, GetRelativeProgess(5, 0));

            foreach (RegChange change in UpdtDetails.RegistryModifications)
            {
                if (canceled)
                    break;

                i++;

                ThreadHelper.ReportProgress(Sender, SenderDelegate,
                    change.ToString(),
                    GetRelativeProgess(5, (i * 100) / UpdtDetails.RegistryModifications.Count));

                //execute the regChange, while storing the opposite operation
                change.ExecuteOperation(rollbackRegistry);
            }
        }

        public void RunUpdateRegistry()
        {
            Thread.CurrentThread.IsBackground = true; //make them a daemon

            string backupFolder = Path.Combine(TempDirectory, "backup");
            List<RegChange> rollbackRegistry = new List<RegChange>();

            //parse variables in the regChanges
            for (int i = 0; i < UpdtDetails.RegistryModifications.Count; i++)
                UpdtDetails.RegistryModifications[i] = ParseRegChange(UpdtDetails.RegistryModifications[i]);

            Exception except = null;
            try
            {
                UpdateRegistry(rollbackRegistry);
            }
            catch (Exception ex)
            {
                except = ex;
            }

            RollbackUpdate.WriteRollbackRegistry(Path.Combine(backupFolder, "regList.bak"), rollbackRegistry);

            if (canceled || except != null)
            {
                RollbackUpdate.RollbackRegistry(TempDirectory, ProgramDirectory);
                ThreadHelper.ReportError(Sender, SenderDelegate, string.Empty, except);
            }
            else
            {
                //registry modification completed sucessfully
                ThreadHelper.ReportSuccess(Sender, SenderDelegate, string.Empty);
            }
        }
    }
}