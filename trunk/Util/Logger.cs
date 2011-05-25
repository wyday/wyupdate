using System;
using System.IO;

namespace wyUpdate
{
    public class Logger
    {
        readonly string Filename;

        public Logger(string file)
        {
            Filename = file;
        }

        public void Write(string message)
        {
            // append to the file, but eat any exceptions
            try
            {
                using (StreamWriter outfile = new StreamWriter(Filename, true))
                {
                    // write the current date/time
                    outfile.Write(DateTime.Now.ToString("M/d/yyyy HH:mm:ss tt") + ": ");

                    // write the message
                    outfile.WriteLine(message);
                }
            }
            catch { }
        }
    }
}
