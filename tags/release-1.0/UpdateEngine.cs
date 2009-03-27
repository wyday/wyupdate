using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace wyUpdate.Common
{
    //also used in settings.cs
    public enum ImageAlign { Left = 0, Right = 1, Fill = 2 }

    public class UpdateFile
    {
        //full path of file for creating zip file
        string m_Filename;
        
        //relative path
        string m_RelativePath;
        
        //execute the file?
        bool m_Execute;
        
        //if so, before or after update?
        bool m_ExBeforeUpdate;

        bool m_WaitForExecution;


        //command line arguents
        string m_CommandLineArgs;

        //is it a .NET assembly?
        bool m_IsNETAssembly;

        //Delta Patching Particulars:

        bool m_DeleteFile = false;
        string m_DeltaPatchRelativePath;

        long m_NewFileAdler32;


        #region Properties

        public string Filename
        {
            get { return m_Filename; }
            set { m_Filename = value; }
        }

        public string RelativePath
        {
            get { return m_RelativePath; }
            set { m_RelativePath = value; }
        }

        public bool Execute
        {
            get { return m_Execute; }
            set { m_Execute = value; }
        }

        public bool ExBeforeUpdate
        {
            get { return m_ExBeforeUpdate; }
            set { m_ExBeforeUpdate = value; }
        }

        public string CommandLineArgs
        {
            get { return m_CommandLineArgs; }
            set { m_CommandLineArgs = value; }
        }

        public bool IsNETAssembly
        {
            get { return m_IsNETAssembly; }
            set { m_IsNETAssembly = value; }
        }

        public bool WaitForExecution
        {
            get { return m_WaitForExecution; }
            set { m_WaitForExecution = value; }
        }

        public string DeltaPatchRelativePath
        {
            get { return m_DeltaPatchRelativePath; }
            set { m_DeltaPatchRelativePath = value; }
        }

        public bool DeleteFile
        {
            get { return m_DeleteFile; }
            set { m_DeleteFile = value; }
        }

        public long NewFileAdler32
        {
            get { return m_NewFileAdler32; }
            set { m_NewFileAdler32 = value; }
        }

        #endregion Properties

        public UpdateFile() { }

        public UpdateFile(string filename, string prefix)
        {
            m_Filename = filename;

            if (!string.IsNullOrEmpty(filename))
                m_RelativePath = prefix + Path.GetExtension(filename);
        }

        public UpdateFile(string filename, string relativeFilename, bool uselessMember)
        {
            m_Filename = filename;
            m_RelativePath = relativeFilename;
        }

        public UpdateFile(string filename, string relative, bool execute, bool executeBef, bool waitForExecution, string commArgs, bool deleteFile, string oldFile)
        {
            m_Filename = filename;
            m_RelativePath = relative;
            m_Execute = execute;
            m_ExBeforeUpdate = executeBef;
            m_WaitForExecution = waitForExecution;
            m_CommandLineArgs = commArgs;

            m_DeleteFile = deleteFile;
            m_DeltaPatchRelativePath = oldFile;
        }
    }

    enum UpdateOn 
    { 
        DownloadingClientUpdt = 0, SelfUpdating = 1,
        DownloadingUpdate = 2, Extracting = 3, ClosingProcesses = 4, 
        PreExecute = 5, BackingUp = 6, ModifyReg = 7, 
        OptimizeExecute = 8, WriteClientFile = 9, DeletingTemp = 10, Uninstalling = 11
    }

    public class VersionChoice
    {
        public string Version;
        public string Changes;
        public bool RTFChanges = false;
        public List<string> FileSites = new List<string>();
        public long FileSize;
        public long Adler32;

        //Determine if client elevation is needed (Vista & non-admin users)
        public InstallingTo InstallingTo = 0;
        public List<RegChange> RegChanges = new List<RegChange>();
    }

    public class NoUpdatePathToNewestException : Exception { }

    public class PatchApplicationException : Exception 
    {
        public PatchApplicationException(string message) : base(message) { }
    }

    [Flags]
    public enum InstallingTo { BaseDir = 1, SysDir = 2, CommonDesktop = 4, CommonStartMenu = 8, CommonAppData = 16 }

    public enum ClientFileType { PreRC2, RC2, Final }

    class UpdateEngine
    {
        #region Private Variables
        //Client Side Information
        private string productName = "";
        private string companyName = "";
        private string installedVersion = "";
        List<string> serverFileSites = new List<string>();
        List<string> clientServerSites = new List<string>();

        private ImageAlign m_HeaderImageAlign = ImageAlign.Left;
        private string m_HeaderTextColorName = "";
        private int m_HeaderTextIndent = -1;
        private bool m_HideHeaderDivider = false;

        private Image m_TopImage = null;
        private string m_TopImageFilename = null;
        private Image m_SideImage = null;
        private string m_SideImageFilename = null;
        private string m_LanguageFilename = null;

        private UpdateOn m_CurrentlyUpdating = UpdateOn.DownloadingUpdate;

        //Server Side Information
        private string newVersion;
        private string m_MinClientVersion = "";
        public List<VersionChoice> VersionChoices = new List<VersionChoice>();

        private string m_NoUpdateToLatestLinkText;
        private string m_NoUpdateToLatestLinkURL;

        #endregion Private Variables

        #region Properties

        public UpdateOn CurrentlyUpdating
        {
            get { return m_CurrentlyUpdating; }
            set { m_CurrentlyUpdating = value; }
        }

        public List<string> ServerFileSites
        {
            get { return serverFileSites; }
            set { serverFileSites = value; }
        }

        public string CompanyName
        {
            get { return companyName; }
            set { companyName = value; }
        }

        public string ProductName
        {
            get { return productName; }
            set { productName = value; }
        }

        public string InstalledVersion
        {
            get { return installedVersion; }
            set { installedVersion = value; }
        }

        public ImageAlign HeaderImageAlign
        {
            get { return m_HeaderImageAlign; }
            set { m_HeaderImageAlign = value; }
        }

        public string HeaderTextColorName
        {
            get { return m_HeaderTextColorName; }
            set { m_HeaderTextColorName = value; }
        }

        public int HeaderTextIndent
        {
            get { return m_HeaderTextIndent; }
            set { m_HeaderTextIndent = value; }
        }

        public bool HideHeaderDivider
        {
            get { return m_HideHeaderDivider; }
            set { m_HideHeaderDivider = value; }
        }

        public Image TopImage
        {
            get { return m_TopImage; }
            set { m_TopImage = value; }
        }

        public Image SideImage
        {
            get { return m_SideImage; }
            set { m_SideImage = value; }
        }


        public string TopImageFilename
        {
            get { return m_TopImageFilename; }
            set { m_TopImageFilename = value; }
        }

        public string SideImageFilename
        {
            get { return m_SideImageFilename; }
            set { m_SideImageFilename = value; }
        }

        public string LanguageFilename
        {
            get { return m_LanguageFilename; }
            set { m_LanguageFilename = value; }
        }

        public List<string> ClientServerSites
        {
            get { return clientServerSites; }
            set { clientServerSites = value; }
        }

        public string NewVersion
        {
            get { return newVersion; }
            set { newVersion = value; }
        }

        public string MinClientVersion
        {
            get { return m_MinClientVersion; }
            set { m_MinClientVersion = value; }
        }

        public string NoUpdateToLatestLinkText
        {
            get { return m_NoUpdateToLatestLinkText; }
            set { m_NoUpdateToLatestLinkText = value; }
        }

        public string NoUpdateToLatestLinkURL
        {
            get { return m_NoUpdateToLatestLinkURL; }
            set { m_NoUpdateToLatestLinkURL = value; }
        }

        #endregion Properties

        //Constructors
        public UpdateEngine()
        {
            productName = "";
            companyName = "";
            installedVersion = "";
        }

        #region Client Data

#if CLIENT

        //Open Pre-RC2  client files
        public void OpenObsoleteClientFile(string fileName)
        {
            byte[] fileIDBytes = new byte[7];
            string fileID = "";

            byte bType;

            FileStream fs = null;

            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            }
            catch (Exception ex)
            {
                if (fs != null)
                    fs.Close();

                throw new ArgumentException("The client data file failed to open.\n\nFull details:\n\n" + ex.Message);
            }
            

            // Read back the file identification data, if any
            fs.Read(fileIDBytes, 0, 7);
            fileID = System.Text.Encoding.UTF8.GetString(fileIDBytes);
            if (fileID != "IUCDFV2")
            {
                //free up the file so it can be deleted
                fs.Close();

                throw new ArgumentException("The client file does not have the correct identifier - this is usually caused by file corruption. \n\nA possible solution is to replace the following file by reinstalling:\n\n" + fileName);
            }

            bType = (byte)fs.ReadByte();
            while (!ReadFiles.ReachedEndByte(fs, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x01://Read Company Name
                        companyName = ReadFiles.ReadString(fs);
                        break;
                    case 0x02://Product Name
                        productName = ReadFiles.ReadString(fs);
                        break;
                    case 0x03://Read Installed Version
                        installedVersion = ReadFiles.ReadString(fs);
                        break;
                    case 0x04://Add server file site
                        AddUniqueSite(ReadFiles.ReadString(fs), serverFileSites);
                        break;
                    case 0x09://Add client server file site
                        AddUniqueSite(ReadFiles.ReadString(fs), clientServerSites);
                        break;
                    case 0x11://Header image alignment
                        try
                        {
                            m_HeaderImageAlign = (ImageAlign)Enum.Parse(typeof(ImageAlign), ReadFiles.ReadString(fs));
                        }
                        catch (Exception) { }
                        break;
                    case 0x12://Header text indent
                        m_HeaderTextIndent = ReadFiles.ReadInt(fs);
                        break;
                    case 0x13://Header text color
                        m_HeaderTextColorName = ReadFiles.ReadString(fs);
                        break;
                    case 0x06://top Image
                        m_TopImage = ReadFiles.ReadImage(fs);
                        break;
                    case 0x07://side Image
                        m_SideImage = ReadFiles.ReadImage(fs);
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            fs.Close();
        }


        private void LoadClientData(Stream ms)
        {
            byte[] fileIDBytes = new byte[7];
            string fileID = "";

            byte bType;

            // Read back the file identification data, if any
            ms.Read(fileIDBytes, 0, 7);
            fileID = System.Text.Encoding.UTF8.GetString(fileIDBytes);
            if (fileID != "IUCDFV2")
            {
                //free up the file so it can be deleted
                ms.Close();

                throw new Exception("The client file does not have the correct identifier - this is usually caused by file corruption.");
            }

            bType = (byte)ms.ReadByte();
            while (!ReadFiles.ReachedEndByte(ms, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x01://Read Company Name
                        companyName = ReadFiles.ReadString(ms);
                        break;
                    case 0x02://Product Name
                        productName = ReadFiles.ReadString(ms);
                        break;
                    case 0x03://Read Installed Version
                        installedVersion = ReadFiles.ReadString(ms);
                        break;
                    case 0x04://Add server file site
                        AddUniqueSite(ReadFiles.ReadString(ms), serverFileSites);
                        break;
                    case 0x09://Add client server file site
                        AddUniqueSite(ReadFiles.ReadString(ms), clientServerSites);
                        break;
                    case 0x11://Header image alignment
                        try
                        {
                            m_HeaderImageAlign = (ImageAlign)Enum.Parse(typeof(ImageAlign), ReadFiles.ReadString(ms));
                        }
                        catch (Exception) { }
                        break;
                    case 0x12://Header text indent
                        m_HeaderTextIndent = ReadFiles.ReadInt(ms);
                        break;
                    case 0x13://Header text color
                        m_HeaderTextColorName = ReadFiles.ReadString(ms);
                        break;
                    case 0x14: //header image filename
                        m_TopImageFilename = ReadFiles.ReadString(ms);
                        break;
                    case 0x15: //side image filename
                        m_SideImageFilename = ReadFiles.ReadString(ms);
                        break;
                    case 0x16: //language filename
                        m_LanguageFilename = ReadFiles.ReadString(ms);
                        break;
                    case 0x17: //hide the header divider
                        m_HideHeaderDivider = ReadFiles.ReadBool(ms);
                        break;
                    default:
                        ReadFiles.SkipField(ms, bType);
                        break;
                }

                bType = (byte)ms.ReadByte();
            }

            ms.Close();
        }

        public void LoadClientData(string filename)
        {
            Stream fs = null;

            try
            {
                fs = new FileStream(filename, FileMode.Open, FileAccess.Read);

                LoadClientData(fs);
            }
            catch (Exception)
            {
                if (fs != null)
                    fs.Close();

                throw;
            }

            if (fs != null)
                fs.Close();
        }


        public void OpenClientFile(string m_Filename, ClientLanguage lang)
        {
            ZipEntry theEntry = null;

            //load the client file
            using (ZipInputStream s = new ZipInputStream(File.OpenRead(m_Filename)))
            {
                while ((theEntry = s.GetNextEntry()) != null)
                {
                    if (theEntry.Name.Equals("iuclient.iuc"))
                    {
                        using (MemoryStream streamWriter = new MemoryStream())
                        {
                            int size = 2048;
                            byte[] data = new byte[2048];
                            do
                            {
                                //read compressed data
                                size = s.Read(data, 0, data.Length);

                                //write to uncompressed file
                                streamWriter.Write(data, 0, size);
                            } while (size > 0);


                            streamWriter.Position = 0;

                            //read in the client data
                            LoadClientData(streamWriter);
                        }

                        break;
                    }
                }
            }//end using(ZipInputStream ... )


            using (ZipInputStream s = new ZipInputStream(File.OpenRead(m_Filename)))
            {
                while ((theEntry = s.GetNextEntry()) != null)
                {
                    if (!theEntry.Name.Equals("iuclient.iuc"))
                    {
                        using (MemoryStream streamWriter = new MemoryStream())
                        {
                            int size = 2048;
                            byte[] data = new byte[2048];
                            do
                            {
                                //read compressed data
                                size = s.Read(data, 0, data.Length);

                                //write to uncompressed file
                                streamWriter.Write(data, 0, size);
                            } while (size > 0);

                            //load the decompressed files
                            if (theEntry.Name.Equals(m_TopImageFilename))
                                m_TopImage = Image.FromStream(streamWriter, true);
                            else if (theEntry.Name.Equals(m_SideImageFilename))
                                m_SideImage = Image.FromStream(streamWriter, true);
                            else if (theEntry.Name.Equals(m_LanguageFilename) && lang != null)
                                lang.Open(streamWriter.ToArray());
                        }
                    }
                }
            }//end using(ZipInputStream ... )
        }
#endif

        public void SaveClientFile(List<UpdateFile> files, string outputFilename)
        {
            byte[] buffer = new byte[4096];

            int m_CompressionLevel = 7;

            try
            {
                // 'using' statements gaurantee the stream is closed properly which is a big source
                // of problems otherwise.  Its exception safe as well which is great.
                using (ZipOutputStream s = new ZipOutputStream(File.Create(outputFilename)))
                {
                    //s.UseZip64 = UseZip64.Off;
                    s.SetLevel(m_CompressionLevel); // 0 (store only) to 9 (best compression)
                    ZipEntry entry;
                    int sourceBytes = 0;

                    //filenames.Length+1 instead of just filenames.Length
                    //because we are adding the UpdateDetails "file"
                    for (int i = 0; i < files.Count; i++)
                    {
                        entry = new ZipEntry(files[i].RelativePath);
                        entry.DateTime = File.GetLastWriteTime(files[i].Filename);
                        s.PutNextEntry(entry);

                        using (FileStream fs = File.OpenRead(files[i].Filename))
                        {

                            // Using a fixed size buffer here makes no noticeable difference
                            // for output but keeps a lid on memory usage.
                            sourceBytes = 0;
                            do
                            {
                                sourceBytes = fs.Read(buffer, 0, buffer.Length);
                                s.Write(buffer, 0, sourceBytes);
                            } while (sourceBytes > 0);
                        }

                        if (i == files.Count - 1)
                        {
                            //add the client file
                            entry = new ZipEntry("iuclient.iuc");
                            entry.DateTime = DateTime.Now;
                            s.PutNextEntry(entry);

                            SaveClientFile(s);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //send back the error message
                throw new Exception(Path.GetFileName(outputFilename) + ". \r\n\r\n" + ex.Message);
            }
        }

        private void SaveClientFile(Stream ms)
        {
            // file-identification data
            ms.Write(System.Text.Encoding.UTF8.GetBytes("IUCDFV2"), 0, 7);

            //Company Name
            WriteFiles.WriteString(ms, 0x01, companyName);

            //Product Name
            WriteFiles.WriteString(ms, 0x02, productName);

            //Installed Version
            WriteFiles.WriteString(ms, 0x03, installedVersion);

            foreach (string site in serverFileSites)
            {
                //Server File Site
                WriteFiles.WriteString(ms, 0x04, site);
            }

            foreach (string site in clientServerSites)
            {
                //Client Server File Site
                WriteFiles.WriteString(ms, 0x09, site);
            }

            //Header image alignment
            WriteFiles.WriteString(ms, 0x11, m_HeaderImageAlign.ToString());

            //Header text indent
            WriteFiles.WriteInt(ms, 0x12, m_HeaderTextIndent);

            //Header text color
            if (!string.IsNullOrEmpty(m_HeaderTextColorName))
                WriteFiles.WriteString(ms, 0x13, m_HeaderTextColorName);

            //Top image filename
            if (!string.IsNullOrEmpty(m_TopImageFilename))
                WriteFiles.WriteString(ms, 0x14, m_TopImageFilename);

            //Side image filename
            if (!string.IsNullOrEmpty(m_SideImageFilename))
                WriteFiles.WriteString(ms, 0x15, m_SideImageFilename);

            //Language filename
            if (!string.IsNullOrEmpty(m_LanguageFilename))
                WriteFiles.WriteString(ms, 0x16, m_LanguageFilename);

            //Hide the header divider
            if (m_HideHeaderDivider)
                WriteFiles.WriteBool(ms, 0x17, true);

            ms.WriteByte(0xFF);
        }

        #endregion Client Data


        #region Server Data

        public void SaveServerDatav2(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

            // Write any file-identification data you want to here
            fs.Write(System.Text.Encoding.UTF8.GetBytes("IUSDFV2"), 0, 7);

            //Current Version
            WriteFiles.WriteString(fs, 0x01, newVersion);

            foreach (string site in serverFileSites)
            {
                //Server File Site
                WriteFiles.WriteString(fs, 0x02, site);
            }

            //Minimum client version needed to install update
            WriteFiles.WriteString(fs, 0x07, m_MinClientVersion);

            
            MemoryStream ms = new MemoryStream();

            //TODO: write all but the last versionChoice (usually the catch-all update)

            for (int i = 0; i < VersionChoices.Count - 1; i++)
            {
                //Version to update from
                WriteFiles.WriteString(ms, 0x0B, VersionChoices[i].Version);

                foreach (string site in VersionChoices[i].FileSites)
                {
                    //Update File Site
                    WriteFiles.WriteString(ms, 0x03, site);
                }

                // put the marker for RTF text data
                if (VersionChoices[i].RTFChanges)
                    ms.WriteByte(0x80);

                //Changes
                WriteFiles.WriteString(ms, 0x04, VersionChoices[i].Changes);

                //Filesize for the update file
                WriteFiles.WriteLong(ms, 0x09, VersionChoices[i].FileSize);

                //Update file's Adler32 checksum
                WriteFiles.WriteLong(ms, 0x08, VersionChoices[i].Adler32);


                //Installing to directories
                WriteFiles.WriteInt(ms, 0x0A, (int)VersionChoices[i].InstallingTo);

                //Representative reg changes to check if elevation
                //is needed for Vista and non-admins
                if (VersionChoices[i].RegChanges.Count > 0)
                {
                    WriteFiles.WriteInt(ms, 0x12, VersionChoices[i].RegChanges.Count);

                    foreach (RegChange reg in VersionChoices[i].RegChanges)
                    {
                        reg.WriteToStream(ms, true);
                    }
                }
            }

            // write out the ms data as a 'skip region' for 1.0RC1 & 1.0RC2
            WriteFiles.WriteByteArray(fs, 0x0F, ms.ToArray());

            // close the stream and lose it
            ms.Close();
            ms = null;



            // write out the last VersionChoice

            //Version to update from
            WriteFiles.WriteString(fs, 0x0B, VersionChoices[VersionChoices.Count - 1].Version);

            foreach (string site in VersionChoices[VersionChoices.Count - 1].FileSites)
            {
                //Update File Site
                WriteFiles.WriteString(fs, 0x03, site);
            }

            // put the marker for RTF text data
            if (VersionChoices[VersionChoices.Count - 1].RTFChanges)
                fs.WriteByte(0x80);

            //Changes
            WriteFiles.WriteString(fs, 0x04, VersionChoices[VersionChoices.Count - 1].Changes);

            //Filesize for the update file
            WriteFiles.WriteLong(fs, 0x09, VersionChoices[VersionChoices.Count - 1].FileSize);

            //Update file's Adler32 checksum
            WriteFiles.WriteLong(fs, 0x08, VersionChoices[VersionChoices.Count - 1].Adler32);


            //Installing to directories
            WriteFiles.WriteInt(fs, 0x0A, (int)VersionChoices[VersionChoices.Count - 1].InstallingTo);

            //Representative reg changes to check if elevation
            //is needed for Vista and non-admins
            if (VersionChoices[VersionChoices.Count - 1].RegChanges.Count > 0)
            {
                WriteFiles.WriteInt(fs, 0x12, VersionChoices[VersionChoices.Count - 1].RegChanges.Count);

                foreach (RegChange reg in VersionChoices[VersionChoices.Count - 1].RegChanges)
                {
                    reg.WriteToStream(fs, true);
                }
            }






            foreach (string site in clientServerSites)
            {
                //Client server site
                WriteFiles.WriteString(fs, 0x13, site);
            }

            // link to show when there is no update patch available

            if (!string.IsNullOrEmpty(m_NoUpdateToLatestLinkText))
                WriteFiles.WriteString(fs, 0x20, m_NoUpdateToLatestLinkText);

            if (!string.IsNullOrEmpty(m_NoUpdateToLatestLinkURL))
                WriteFiles.WriteString(fs, 0x21, m_NoUpdateToLatestLinkURL);

            fs.WriteByte(0xFF);

            fs.Close();
        }

        public void LoadServerDatav2(string fileName)
        {
            byte[] fileIDBytes = new byte[7];
            string fileID = "";

            byte bType;

            Stream fs = null;

            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);

                // Read the first 7 bytes of identification data
                fs.Read(fileIDBytes, 0, 7);
            }
            catch (Exception)
            {
                if (fs != null)
                    fs.Close();

                throw;
            }

            // check for compression (see if PKZip header is there)
            if (fileIDBytes[0] == 0x50 && fileIDBytes[1] == 0x4B && fileIDBytes[2] == 0x03 && fileIDBytes[3] == 0x04)
            {
                // decompress the "actual" server file to memory
                fs.Close();

                using (ZipInputStream s = new ZipInputStream(File.OpenRead(fileName)))
                {
                    s.GetNextEntry();

                    fs = new MemoryStream();

                    int size = 2048;
                    byte[] data = new byte[2048];
                    do
                    {
                        //read compressed data
                        size = s.Read(data, 0, data.Length);

                        //write to uncompressed file
                        fs.Write(data, 0, size);
                    } while (size > 0);

                } //end using(ZipInputStream ... )


                fs.Position = 0;

                // Read the first 7 bytes of identification data
                fs.Read(fileIDBytes, 0, 7);
            }


            // see if the file is in the correct server format

            fileID = System.Text.Encoding.UTF8.GetString(fileIDBytes);

            if (fileID != "IUSDFV2")
            {
                //free up the file so it can be deleted
                fs.Close();
                throw new Exception("The downloaded server file does not have the correct identifier. This is usually caused by file corruption.");
            }



            VersionChoices.Add(new VersionChoice());

            bType = (byte)fs.ReadByte();
            while (!ReadFiles.ReachedEndByte(fs, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x01://Read New Version
                        newVersion = ReadFiles.ReadString(fs);
                        break;
                    case 0x02://Add server file site
                        AddUniqueSite(ReadFiles.ReadString(fs), serverFileSites);
                        break;
                    case 0x07: //Min Client version
                        m_MinClientVersion = ReadFiles.ReadString(fs);
                        break;
                    case 0x0B: //The version to update from
                        if (VersionChoices.Count > 1 || VersionChoices[0].Version != null)
                            VersionChoices.Add(new VersionChoice());

                        VersionChoices[VersionChoices.Count - 1].Version = ReadFiles.ReadString(fs);
                        break;
                    case 0x03://Add update file site
                        AddUniqueSite(ReadFiles.ReadString(fs), VersionChoices[VersionChoices.Count - 1].FileSites);
                        break;
                    case 0x80: //the changes text is in RTF format
                        VersionChoices[VersionChoices.Count - 1].RTFChanges = true;
                        break;
                    case 0x04://Read Changes
                         VersionChoices[VersionChoices.Count - 1].Changes = ReadFiles.ReadString(fs);
                        break;
                    case 0x09://update's filesize
                        VersionChoices[VersionChoices.Count - 1].FileSize = ReadFiles.ReadLong(fs);
                        break;
                    case 0x08://update's Adler32 checksum
                        VersionChoices[VersionChoices.Count - 1].Adler32 = ReadFiles.ReadLong(fs);
                        break;
                    case 0x0A: //Installing to which directories?
                        VersionChoices[VersionChoices.Count - 1].InstallingTo = (InstallingTo)ReadFiles.ReadInt(fs);
                        break;
                    case 0x12: //how many regchanges to test
                         VersionChoices[VersionChoices.Count - 1].RegChanges.Capacity = ReadFiles.ReadInt(fs);
                        break;
                    case 0x8E: //the RegChanges
                        VersionChoices[VersionChoices.Count - 1].RegChanges.Add(RegChange.ReadFromStream(fs));
                        break;
                    case 0x13://add client server sites
                        AddUniqueSite(ReadFiles.ReadString(fs), clientServerSites);
                        break;
                    case 0x20:
                        m_NoUpdateToLatestLinkText = ReadFiles.ReadString(fs);
                        break;
                    case 0x21:
                        m_NoUpdateToLatestLinkURL = ReadFiles.ReadString(fs);
                        break;
                    case 0x0F:
                        //skip over the integer (4 bytes) length
                        //this is just used to trick pre-1.0 Final versions
                        //of wyUpdate to correctly read the server file correctly
                        fs.Position += 4;
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            fs.Close();
        }

        #endregion Server Data



        private static string[] greek_ltrs = { "alpha", "beta", "gamma", "delta", 
                "epsilon", "zeta", "eta", "theta", 
                "iota", "kappa", "lambda", "mu", 
                "nu", "xi", "omicron", "pi", 
                "rho", "sigma", "tau", "upsilon", 
                "phi", "chi", "psi", "omega", 
                "rc" }; //RC = release candidate

        public static int VersionCompare(string versionA, string versionB)
        {
            //compare indices
            int iVerA = 0, iVerB = 0;
            int greekIndA = -1, greekIndB = -1;

            int strComp = 0;

            string objA = null, objB = null;
            bool lastAWasLetter = true, lastBWasLetter = true;

            do
            {
                //store index before GetNextObject just in case we need to rollback
                greekIndA = iVerA;
                greekIndB = iVerB;

                objA = GetNextObject(versionA, ref iVerA, ref lastAWasLetter);
                objB = GetNextObject(versionB, ref iVerB, ref lastBWasLetter);


                //normalize versions so comparing integer against integer, 
                //(i.e. "1 a" is expanded to "1.0.0 a" when compared with "1.0.0 XXX")
                //also, rollback the index on the version modified
                if ((!lastBWasLetter && objB != null) && (objA == null || lastAWasLetter))
                {
                    objA = "0";
                    iVerA = greekIndA;
                }
                else if ((!lastAWasLetter && objA != null) && (objB == null || lastBWasLetter))
                {
                    objB = "0";
                    iVerB = greekIndB;
                }


                //find greek index for A and B
                greekIndA = GetGreekIndex(objA);
                greekIndB = GetGreekIndex(objB);


                if (objA == null && objB == null)
                    break; //versions are equal
                else if (objA == null && objB != null)
                {
                    //if versionB has a greek letter, then A is greater
                    if (greekIndB != -1)
                        return 1;
                    else
                        return -1;
                }
                else if (objA != null && objB == null)
                {
                    //if versionA has a greek letter, then B is greater
                    if (greekIndA != -1)
                        return -1;
                    else
                        return 1;
                }
                else if (char.IsDigit(objA[0]) == char.IsDigit(objB[0]))
                {
                    if (char.IsDigit(objA[0]))
                    {
                        //compare integers
                        strComp = IntCompare(objA, objB);

                        if (strComp != 0)
                            return strComp;
                    }
                    else
                    {
                        if (greekIndA == -1 && greekIndB == -1)
                        {
                            //compare non-greek strings
                            strComp = string.Compare((string)objA, (string)objB, true);

                            if (strComp != 0)
                                return strComp;
                        }
                        else if (greekIndA == -1)
                            return 1; //versionB has a greek letter, thus A is newer
                        else if (greekIndB == -1)
                            return -1; //versionA has a greek letter, thus B is newer
                        else
                        {
                            //compare greek letters
                            if (greekIndA > greekIndB)
                                return 1;
                            else if (greekIndB > greekIndA)
                                return -1;
                        }
                    }
                }
                else if (char.IsDigit(objA[0]))
                    return 1; //versionA is newer than versionB
                else
                    return -1; //verisonB is newer than versionA


            } while (objA != null && objB != null);


            return 0;
        }


        public static string GetNextObject(string version, ref int index, ref bool lastWasLetter)
        {
            //1 == string, 2 == int
            int StringOrInt = -1;

            int startIndex = index;

            while (version.Length != index)
            {
                if (StringOrInt == -1)
                {
                    if (char.IsLetter(version[index]))
                    {
                        startIndex = index;
                        StringOrInt = 1;
                    }
                    else if (char.IsDigit(version[index]))
                    {
                        startIndex = index;
                        StringOrInt = 2;
                    }
                    else if (lastWasLetter && !char.IsWhiteSpace(version[index]))
                    {
                        index++;
                        lastWasLetter = false;
                        return "0";
                    }
                }
                else if (StringOrInt == 1 && !char.IsLetter(version[index]))
                    break;
                else if (StringOrInt == 2 && !char.IsDigit(version[index]))
                    break;

                index++;
            }

            //set the last "type" retrieved
            if (StringOrInt == 1)
                lastWasLetter = true;
            else
                lastWasLetter = false;

            //return the retitrved sub-string
            if (StringOrInt == 1 || StringOrInt == 2)
                return version.Substring(startIndex, index - startIndex);
            else
                return null;
        }

        public static int GetGreekIndex(object version)
        {
            if (version != null && version.GetType() == typeof(string))
            {
                for (int i = 0; i < greek_ltrs.Length; i++)
                {
                    if (string.Compare((string)version, greek_ltrs[i], true) == 0)
                        return i;
                }
            }

            return -1;
        }


        private static int IntCompare(string a, string b)
        {
            int lastZero = -1;

            //if ((int)objA > (int)objB)
            //    return 1;
            //else if ((int)objB > (int)objA)
            //    return -1;

            //Clear any preceding zeros

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != '0')
                    break;
                else
                    lastZero = i;
            }

            if (lastZero != -1)
                a = a.Substring(lastZero + 1, a.Length - (lastZero + 1));

            lastZero = -1;

            for (int i = 0; i < b.Length; i++)
            {
                if (b[i] != '0')
                    break;
                else
                    lastZero = i;
            }

            if (lastZero != -1)
                b = b.Substring(lastZero + 1, b.Length - (lastZero + 1));


            if (a.Length > b.Length)
                return 1;
            else if (a.Length < b.Length)
                return -1;
            else
                return string.Compare(a, b);
        }

        //e.g. 1.0.2 to 1.0.3
        public static string VerisonPlusPlus(string version)
        {
            int previ = 0, i = 0;
            object prevObj = null, obj = null;

            bool junkBool = false;

            do
            {
                previ = i;
                prevObj = obj;
                obj = GetNextObject(version, ref i, ref junkBool);

            } while (obj != null);

            if (prevObj != null)
            {
                if (char.IsDigit(((string)prevObj)[0]))
                    return version.Substring(0, previ - ((string)prevObj).Length) + NumberPlusOne((string)prevObj);
                else
                    return version + " 2";
            }

            return version;
        }

        private static string NumberPlusOne(string number)
        {
            StringBuilder sb = new StringBuilder();

            int i = number.Length - 1;
            int tempInt = 1;


            //process the number
            for (; i >= 0; i--)
            {
                tempInt += number[i] - '0';

                if (tempInt == 10)
                {
                    sb.Insert(0, '0');
                    tempInt = 1;
                }
                else
                {
                    sb.Insert(0, (char)(tempInt + '0'));
                    tempInt = 0;
                    break;
                }
            }

            if (tempInt != 0)
                //e.g. 99 + 1
                sb.Insert(0, '1');
            else if (i > 0)
                //insert the higher digits that didn't need process
                //e.g. 573 + 1 = 574, the leading '57' is copied over
                sb.Insert(0, number.Substring(0, i));

            return sb.ToString();
        }

        public static bool UpdateNeccessary(string oldVersion, string newVersion)
        {
            int comp = VersionCompare(oldVersion, newVersion);

            if (comp == -1)
                return true;
            else
                return false;
        }


        /// <summary>
        /// Get the full version of a file.
        /// </summary>
        /// <param name="assembly">The file of which you want the version.</param>
        /// <returns>String version (ex. 1.45.7 or 1.45.7 Beta 1)</returns>
        public static string GetFullVersion(string filename)
        {
            return FileVersionInfo.GetVersionInfo(filename).FileVersion;
        }

        public void AddUniqueSite(string newSite, List<string> sites)
        {
            //if the site already exists, bail out
            foreach (string site in sites)
                if (string.Equals(newSite, site, StringComparison.InvariantCultureIgnoreCase))
                    return;

            //add the site
            sites.Add(newSite);
        }
    }
}
