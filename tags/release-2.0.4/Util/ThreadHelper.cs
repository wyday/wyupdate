using System;
using System.Windows.Forms;

namespace wyUpdate.Common
{
    public static class ThreadHelper
    {
        public static void ReportError(ContainerControl sender, Delegate del, string errorText, Exception ex)
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

                    sender.BeginInvoke(del, new object[] { -1, true, errorText, ex });
                    break;
                }
                catch { }

            } while (true);
        }

        public static void ReportProgress(ContainerControl sender, Delegate del, string text, int progress)
        {
            try
            {
                sender.BeginInvoke(del, new object[] { progress, false, text, null });
            }
            catch
            {
                // don't bother with the exception (it doesn't matter if the main window misses a progress report)
            }
        }

        public static void ReportSuccess(ContainerControl sender, Delegate del, string text)
        {
            do
            {
                try
                {
                    //Try to send our success to the frmMain thread - wait until it gets through

                    // NOTE: a -1 for progress assures that the progress bar won't be reset

                    sender.BeginInvoke(del, new object[] { -1, true, text, null });
                    break;
                }
                catch { }

            } while (true);
        }

        public static void ChangeRollback(ContainerControl sender, Delegate del, bool rbRegistry)
        {
            do
            {
                try
                {
                    //Try to send our changing status to rolling back

                    sender.BeginInvoke(del, new object[] { rbRegistry });
                    break;
                }
                catch { }

            } while (true);
        }
    }
}
