using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Ionic.Zlib;

namespace wyUpdate.Downloader
{
    /// <summary>
    /// Downloads and resumes files from HTTP, FTP, and File (file://) URLS
    /// </summary>
    public class FileDownloader
    {
        // Block size to download is by default 4K.
        private const int BufferSize = 4096;

        /// <summary>
        /// This is the name of the file we get back from the server when we
        /// try to download the provided url. It will only contain a non-null
        /// string when we've successfully contacted the server and it has started
        /// sending us a file.
        /// </summary>
        public string DownloadingTo { get; private set; }

        //used to measure download speed
        private readonly Stopwatch sw = new Stopwatch();
        private long sentSinceLastCalc;
        private string downloadSpeed = "";

        //download site and destination
        string url;
        List<string> urlList = new List<string>();
        string destFolder = "";

        bool waitingForResponse;

        public long Adler32;

        private long downloadedAdler32 = 1;

        public bool UseRelativeProgress;

        readonly BackgroundWorker bw = new BackgroundWorker();

        public delegate void ProgressChangedHandler(int percentDone, bool done, string extraStatus, Exception ex);
        public event ProgressChangedHandler ProgressChanged;

        public FileDownloader(List<string> urls, string downloadfolder)
        {
            urlList = urls;
            destFolder = downloadfolder;

            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += bw_DoWork;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            // validate input
            if (urlList == null || urlList.Count == 0)
            {
                if (string.IsNullOrEmpty(url))
                {
                    //no sites specified, bail out
                    if (!bw.CancellationPending)
                        bw.ReportProgress(0, new object[] { -1, true, string.Empty, new Exception("No download urls are specified.") });

                    return;
                }

                //single site specified, add it to the list
                urlList = new List<string> { url };
            }

            // try each url in the list until one suceeds

            bool allFailedWaitingForResponse = true;
            Exception ex = null;
            foreach (string s in urlList)
            {
                ex = null;
                try
                {
                    url = s;
                    BeginDownload();
                    ValidateDownload();
                }
                catch (Exception except)
                {
                    ex = except;

                    if (!waitingForResponse)
                        allFailedWaitingForResponse = false;
                }

                // If we got through that without an exception, we found a good url
                if (ex == null || bw.CancellationPending)
                {
                    allFailedWaitingForResponse = false;
                    break;
                }
            }


            /*
             If the all the sites failed before a response was recieved then either the 
             internet connection is shot, or the Proxy is shot. Either way it can't 
             hurt to try downloading without the proxy:
            */
            if (allFailedWaitingForResponse)
            {
                //try the sites again without the proxy
                WebRequest.DefaultWebProxy = null;

                foreach (string s in urlList)
                {
                    ex = null;
                    try
                    {
                        url = s;
                        BeginDownload();
                        ValidateDownload();
                    }
                    catch (Exception except)
                    {
                        ex = except;
                    }

                    // If we got through that without an exception, we found a good url
                    if (ex == null || bw.CancellationPending)
                        break;
                }
            }

            //Process complete (either sucessfully or failed), report back
            if (!bw.CancellationPending)
            {
                if (ex != null)
                    bw.ReportProgress(0, new object[] {-1, true, string.Empty, ex});
                else
                    bw.ReportProgress(0, new object[] { -1, true, string.Empty, null });
            }
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            object[] arr = (object[])e.UserState;

            if (ProgressChanged != null)
                ProgressChanged((int) arr[0], (bool) arr[1], (string) arr[2], (Exception) arr[3]);
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bw.DoWork -= bw_DoWork;
            bw.ProgressChanged -= bw_ProgressChanged;
            bw.RunWorkerCompleted -= bw_RunWorkerCompleted;
        }

        public static void EnableLazySSL()
        {
            //Add a delegate that accepts all SSL's. Corrupt or not.
            ServicePointManager.ServerCertificateValidationCallback += OnCheckSSLCert;
        }

        private static bool OnCheckSSLCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            //allow all downloads regardless of SSL security errors
            /* This will 'fix' the self-signed SSL certificate problem
               that's typical on most corporate intranets */
            return true;
        }

        public void Cancel()
        {
            bw.CancelAsync();
        }

        /// <summary>
        /// Download a file from a list or URLs. If downloading
        /// from one of the URLs fails, another URL is tried.
        /// </summary>
        public void Download()
        {
            bw.RunWorkerAsync();
        }

        // Begin downloading the file at the specified url, and save it to the given folder.
        private void BeginDownload()
        {
            DownloadData data = null;
            FileStream fs = null;

            try
            {
                //start the stopwatch for speed calc
                sw.Start();

                // get download details 
                waitingForResponse = true;
                data = DownloadData.Create(url, destFolder);
                waitingForResponse = false;

                // Find out the name of the file that the web server gave us.
                string destFileName = Path.GetFileName(data.Response.ResponseUri.ToString());


                // The place we're downloading to (not from) must not be a URI,
                // because Path and File don't handle them...
                destFolder = destFolder.Replace("file:///", "").Replace("file://", "");
                DownloadingTo = Path.Combine(destFolder, destFileName);

                if (!File.Exists(DownloadingTo))
                {
                    // create the file
                    fs = File.Open(DownloadingTo, FileMode.Create, FileAccess.Write);
                }
                else
                {
                    // read in the existing data to calculate the adler32
                    if (Adler32 != 0)
                        GetAdler32(DownloadingTo);

                    // apend to an existing file (resume)
                    fs = File.Open(DownloadingTo, FileMode.Append, FileAccess.Write);
                }

                // create the download buffer
                byte[] buffer = new byte[BufferSize];

                int readCount;

                // update how many bytes have already been read
                sentSinceLastCalc = data.StartPoint; //for BPS calculation

                while ((readCount = data.DownloadStream.Read(buffer, 0, BufferSize)) > 0)
                {
                    // break on cancel
                    if (bw.CancellationPending)
                    {
                        data.Close();
                        fs.Close();
                        break;
                    }

                    // update total bytes read
                    data.StartPoint += readCount;

                    // update the adler32 value
                    if (Adler32 != 0)
                        downloadedAdler32 = Adler.Adler32(downloadedAdler32, buffer, 0, readCount);

                    // save block to end of file
                    fs.Write(buffer, 0, readCount);

                    //calculate download speed
                    calculateBps(data.StartPoint);

                    // send progress info
                    if (!bw.CancellationPending)
                    {
                        bw.ReportProgress(0, new object[] {
                            //use the realtive progress or the raw progress
                            UseRelativeProgress ?
                                InstallUpdate.GetRelativeProgess(0, data.PercentDone) :
                                data.PercentDone,
                                false, downloadSpeed, null });
                    }

                    // break on cancel
                    if (bw.CancellationPending)
                    {
                        data.Close();
                        fs.Close();
                        break;
                    }
                }
            }
            catch (UriFormatException e)
            {
                throw new Exception(
                    String.Format("Could not parse the URL \"{0}\" - it's either malformed or is an unknown protocol.", url), e);
            }
            catch (Exception e)
            {
                if(string.IsNullOrEmpty(DownloadingTo))
                    throw new Exception(String.Format("Error trying to save file: {0}", e.Message), e);
                else
                    throw new Exception(String.Format("Error trying to save file \"{0}\": {1}", DownloadingTo, e.Message), e);
            }
            finally
            {
                if (data != null)
                    data.Close();
                if (fs != null)
                    fs.Close();
            }
        }

        private void calculateBps(long BytesReceived)
        {
            if (sw.Elapsed >= TimeSpan.FromSeconds(2))
            {
                sw.Stop();

                // Calculcate transfer speed.
                long bytes = BytesReceived - sentSinceLastCalc;
                double bps = bytes * 1000.0 / sw.Elapsed.TotalMilliseconds;
                downloadSpeed = BpsToString(bps);

                // Estimated seconds remaining based on the current transfer speed.
                //secondsRemaining = (int)((e.TotalBytesToReceive - e.BytesReceived) / bps);

                // Restart stopwatch for next second.
                sentSinceLastCalc = BytesReceived;
                sw.Reset();
                sw.Start();
            }
        }

        /// <summary>
        /// Constructs a download speed indicator string.
        /// </summary>
        /// <param name="bps">Bytes per second transfer rate.</param>
        /// <returns>String represenation of the transfer rate in bytes/sec, KB/sec, MB/sec, etc.</returns>
        private static string BpsToString(double bps)
        {
            string[] m = new[] { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            int i = 0;
            while (bps >= 0.9 * 1024)
            {
                bps /= 1024;
                i++;
            }

            return String.Format("{0:0.00} {1}/sec", bps, m[i]);
        }

        private void ValidateDownload()
        {
            //if an Adler32 checksum is provided, check the file
            if (!bw.CancellationPending && Adler32 != 0 && Adler32 != downloadedAdler32)
            {
                // file failed to vaildate, throw an error
                throw new Exception("The downloaded file failed the Adler32 validation.");
            }
        }

        private void GetAdler32(string fileName)
        {
            byte[] buffer = new byte[BufferSize];

            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                int sourceBytes;

                do
                {
                    sourceBytes = fs.Read(buffer, 0, buffer.Length);

                    downloadedAdler32 = Adler.Adler32(downloadedAdler32, buffer, 0, sourceBytes);

                    // break on cancel
                    if (bw.CancellationPending)
                        break;

                } while (sourceBytes > 0);
            }
        }
    }


    class DownloadData
    {
        private WebResponse response;

        private Stream stream;
        private long size;
        private long start;

        public static DownloadData Create(string url, string destFolder)
        {
            // This is what we will return
            DownloadData downloadData = new DownloadData();

            
            WebRequest req = GetRequest(url);
            try
            {
                downloadData.response = req.GetResponse();
                downloadData.GetFileSize();
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("Error downloading \"{0}\": {1}", url, e.Message), e);
            }

            // Check to make sure the response isn't an error. If it is this method
            // will throw exceptions.
            ValidateResponse(downloadData.response, url);

            // Take the name of the file given to use from the web server.
            String fileName = Path.GetFileName(downloadData.response.ResponseUri.ToString());

            String downloadTo = Path.Combine(destFolder, fileName);

            // If we don't know how big the file is supposed to be,
            // we can't resume, so delete what we already have if something is on disk already.
            if (!downloadData.IsProgressKnown && File.Exists(downloadTo))
                File.Delete(downloadTo);

            if (downloadData.IsProgressKnown && File.Exists(downloadTo))
            {
                // We only support resuming on http requests
                if (!(downloadData.Response is HttpWebResponse))
                {
                    File.Delete(downloadTo);
                }
                else
                {
                    // Try and start where the file on disk left off
                    downloadData.start = new FileInfo(downloadTo).Length;

                    // If we have a file that's bigger than what is online, then something 
                    // strange happened. Delete it and start again.
                    if (downloadData.start > downloadData.size)
                        File.Delete(downloadTo);
                    else if (downloadData.start < downloadData.size)
                    {
                        // Try and resume by creating a new request with a new start position
                        downloadData.response.Close();
                        req = GetRequest(url);
                        ((HttpWebRequest)req).AddRange((int)downloadData.start);
                        downloadData.response = req.GetResponse();

                        if (((HttpWebResponse)downloadData.Response).StatusCode != HttpStatusCode.PartialContent)
                        {
                            // They didn't support our resume request. 
                            File.Delete(downloadTo);
                            downloadData.start = 0;
                        }
                    }
                }
            }
            return downloadData;
        }

        // Checks whether a WebResponse is an error.
        private static void ValidateResponse(WebResponse response, string url)
        {
            if (response is HttpWebResponse)
            {
                HttpWebResponse httpResponse = (HttpWebResponse)response;
                // If it's an HTML page, it's probably an error page. Comment this
                // out to enable downloading of HTML pages.
                if (httpResponse.ContentType.Contains("text/html") || httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new Exception(
                        String.Format("Could not download \"{0}\" - a web page was returned from the web server.",
                        url));
                }
            }
            else if (response is FtpWebResponse)
            {
                FtpWebResponse ftpResponse = (FtpWebResponse)response;
                if (ftpResponse.StatusCode == FtpStatusCode.ConnectionClosed)
                    throw new Exception(
                        String.Format("Could not download \"{0}\" - FTP server closed the connection.", url));
            }
            // FileWebResponse doesn't have a status code to check.
        }

        private void GetFileSize()
        {
            if (response != null)
            {
                try
                {
                    size = response.ContentLength;
                }
                catch (Exception) 
                {
                    //file size couldn't be determined
                    size = -1;
                }
            }
        }

        private static WebRequest GetRequest(string url)
        {
            WebRequest request = WebRequest.Create(url);

            if (request is HttpWebRequest)
                request.Credentials = CredentialCache.DefaultCredentials;

            return request;
        }

        public void Close()
        {
            response.Close();
        }

        #region Properties

        public WebResponse Response
        {
            get { return response; }
            set { response = value; }
        }

        public Stream DownloadStream
        {
            get
            {
                if (start == size)
                    return Stream.Null;
                if (stream == null)
                    stream = response.GetResponseStream();

                return stream;
            }
        }

        public int PercentDone
        {
            get
            {
                if (size > 0)
                    return (int) ((start*100)/size);

                return 0;
            }
        }

        public long StartPoint
        {
            get { return start; }
            set { start = value; }
        }

        public bool IsProgressKnown
        {
            get
            {
                // If the size of the remote url is -1, that means we
                // couldn't determine it, and so we don't know
                // progress information.
                return size > -1;
            }
        }
        #endregion
    }
}