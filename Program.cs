using System;
using System.Windows.Forms;

namespace wyUpdate
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            Application.EnableVisualStyles();

            frmMain mainForm = new frmMain(args);

            Application.Run(mainForm);

            return mainForm.ReturnCode;
        }
    }
}