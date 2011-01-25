using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using wyUpdate.Common;

namespace wyUpdate
{
    partial class InstallUpdate
    {
        public void RunUpdateRegistry()
        {
            bw.DoWork += bw_DoWorkRegistry;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_RunWorkerCompletedRegistry;

            bw.RunWorkerAsync();
        }

        void bw_DoWorkRegistry(object sender, DoWorkEventArgs e)
        {
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

            RollbackUpdate.WriteRollbackRegistry(Path.Combine(TempDirectory, "backup\\regList.bak"), rollbackRegistry);

            if (IsCancelled() || except != null)
            {
                // rollback the registry
                bw.ReportProgress(1, true);
                RollbackUpdate.RollbackRegistry(TempDirectory);

                // rollback files
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
                // registry modification completed sucessfully
                bw.ReportProgress(0, new object[] { -1, -1, string.Empty, ProgressStatus.Success, null });
            }
        }

        void bw_RunWorkerCompletedRegistry(object sender, RunWorkerCompletedEventArgs e)
        {
            bw.DoWork -= bw_DoWorkRegistry;
            bw.ProgressChanged -= bw_ProgressChanged;
            bw.RunWorkerCompleted -= bw_RunWorkerCompletedRegistry;
        }

        void UpdateRegistry(List<RegChange> rollbackRegistry)
        {
            int i = 0;

            bw.ReportProgress(0, new object[] { GetRelativeProgess(5, 0), 0, string.Empty, ProgressStatus.None, null });

            foreach (RegChange change in UpdtDetails.RegistryModifications)
            {
                if (IsCancelled())
                    break;

                i++;
                int unweightedProgress = (i * 100) / UpdtDetails.RegistryModifications.Count;

                bw.ReportProgress(0, new object[] { GetRelativeProgess(5, unweightedProgress), unweightedProgress, change.ToString(), ProgressStatus.None, null });

                //execute the regChange, while storing the opposite operation
                change.ExecuteOperation(rollbackRegistry);
            }
        }
    }
}