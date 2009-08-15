using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using Ionic.Zip;

namespace wyUpdate.Common
{
    //also used in settings.cs
    public enum ImageAlign { Left = 0, Right = 1, Fill = 2 }

    public class UpdateFile
    {
        #region Properties

        //full path of file for creating zip file
        public string Filename { get; set; }

        public string RelativePath { get; set; }

        //execute the file?
        public bool Execute { get; set; }

        //if so, before or after update?
        public bool ExBeforeUpdate { get; set; }

        //command line arguents
        public string CommandLineArgs { get; set; }

        //is it a .NET assembly?
        public bool IsNETAssembly { get; set; }

        public bool WaitForExecution { get; set; }

        //Delta Patching Particulars:
        public string DeltaPatchRelativePath { get; set; }

        public bool DeleteFile { get; set; }

        public long NewFileAdler32 { get; set; }

        #endregion Properties

        public UpdateFile() { }

        public UpdateFile(string filename, string prefix)
        {
            Filename = filename;

            if (!string.IsNullOrEmpty(filename))
                RelativePath = prefix + Path.GetExtension(filename);
        }

        public UpdateFile(string filename, string relative, bool execute, bool executeBef, bool waitForExecution, string commArgs, bool deleteFile, string oldFile)
        {
            Filename = filename;
            RelativePath = relative;
            Execute = execute;
            ExBeforeUpdate = executeBef;
            WaitForExecution = waitForExecution;
            CommandLineArgs = commArgs;

            DeleteFile = deleteFile;
            DeltaPatchRelativePath = oldFile;
        }
    }

    public enum UpdateOn 
    { 
        DownloadingClientUpdt = 0, SelfUpdating = 1,
        DownloadingUpdate = 2, Extracting = 3, ClosingProcesses = 4, 
        PreExecute = 5, BackUpInstalling = 6, ModifyReg = 7, 
        OptimizeExecute = 8, WriteClientFile = 9, DeletingTemp = 10, Uninstalling = 11
    }

    public class VersionChoice
    {
        public string Version;
        public string Changes;
        public bool RTFChanges;
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

    public class UpdateEngine
    {
        #region Private Variables
        //Client Side Information
        private string productName;
        private string companyName;
        private string installedVersion;
        List<string> serverFileSites = new List<string>(1);
        List<string> clientServerSites = new List<string>(1);

        private ImageAlign m_HeaderImageAlign = ImageAlign.Left;
        private string m_HeaderTextColorName = "";
        private int m_HeaderTextIndent = -1;
        private bool m_HideHeaderDivider;

        private Image m_TopImage;
        private string m_TopImageFilename;
        private Image m_SideImage;
        private string m_SideImageFilename;

        public Hashtable Languages = new Hashtable();

        private UpdateOn m_CurrentlyUpdating = UpdateOn.DownloadingUpdate;

        //Server Side Information
        private string newVersion;
        private string m_MinClientVersion;
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


        #region Client Data

#if CLIENT

        //Open Pre-RC2  client files
        public void OpenObsoleteClientFile(string fileName)
        {
            byte[] fileIDBytes = new byte[7];

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
            string fileID = Encoding.UTF8.GetString(fileIDBytes);
            if (fileID != "IUCDFV2")
            {
                //free up the file so it can be deleted
                fs.Close();

                throw new ArgumentException("The client file does not have the correct identifier - this is usually caused by file corruption. \n\nA possible solution is to replace the following file by reinstalling:\n\n" + fileName);
            }

            byte bType = (byte)fs.ReadByte();
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
                        catch { }
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

            ms.Position = 0;

            // Read back the file identification data, if any
            ms.Read(fileIDBytes, 0, 7);
            string fileID = Encoding.UTF8.GetString(fileIDBytes);
            if (fileID != "IUCDFV2")
            {
                //free up the file so it can be deleted
                ms.Close();

                throw new Exception("The client file does not have the correct identifier - this is usually caused by file corruption.");
            }

            LanguageCulture lastLanguage = null;

            byte bType = (byte)ms.ReadByte();
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
                        catch { }
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
                    case 0x18: // language culture

                        lastLanguage = new LanguageCulture(ReadFiles.ReadString(ms));

                        Languages.Add(lastLanguage.Culture, lastLanguage);
                        break;
                    case 0x16: //language filename

                        if (lastLanguage != null)
                            lastLanguage.Filename = ReadFiles.ReadString(ms);
                        else
                            Languages.Add(string.Empty, new LanguageCulture(null) { Filename = ReadFiles.ReadString(ms) });

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
            finally
            {
                if (fs != null)
                    fs.Close();
            }
        }


        public void OpenClientFile(string m_Filename, ClientLanguage lang)
        {
            using (ZipFile zip = ZipFile.Read(m_Filename))
            {
                // load the client details (image filenames, languages, etc.)
                using (MemoryStream ms = new MemoryStream())
                {
                    zip["iuclient.iuc"].Extract(ms);

                    //read in the client data
                    LoadClientData(ms);
                }

                // load the top image
                if (!string.IsNullOrEmpty(m_TopImageFilename))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        zip[m_TopImageFilename].Extract(ms);

                        // convert the bytes to an images
                        m_TopImage = Image.FromStream(ms, true);
                    }
                }

                // load the side image
                if (!string.IsNullOrEmpty(m_SideImageFilename))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        zip[m_SideImageFilename].Extract(ms);

                        // convert the bytes to an images
                        m_SideImage = Image.FromStream(ms, true);
                    }
                }

                
                // Backwards compatability with pre-v1.3 of wyUpdate:
                // if the languages has a culture with a null name, load that file
                if(Languages.Count == 1 && Languages.Contains(string.Empty))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        zip[((LanguageCulture)Languages[string.Empty]).Filename].Extract(ms);
                        lang.Open(ms);
                    }
                }
                else if (Languages.Count > 0)
                {
                    // detect the current culture 
                    string currentCultureName = CultureInfo.CurrentUICulture.Name;

                    // try to find the current culture
                    LanguageCulture useLang = (LanguageCulture)Languages[currentCultureName];

                    if(useLang == null)
                    {
                        // if current culture isn't available, use the default culture (english)
                        useLang = (LanguageCulture) Languages["en-US"];
                    }


                    // if the default culture isn't available, use the first available language
                    if(useLang == null)
                    {
                        foreach (LanguageCulture l in Languages.Values)
                        {
                            useLang = l;
                            break;
                        }
                    }

                    if (useLang != null && !string.IsNullOrEmpty(useLang.Filename))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            zip[useLang.Filename].Extract(ms);
                            lang.Open(ms);
                        }
                    }
                }
            }
        }
#endif

        public void SaveClientFile(List<UpdateFile> files, string outputFilename)
        {
            try
            {
                if (File.Exists(outputFilename))
                    File.Delete(outputFilename);

                ZipEntry entry;
                using (ZipFile zip = new ZipFile(outputFilename))
                {
                    zip.UseUnicodeAsNecessary = true;

                    // 0 (store only) to 9 (best compression)
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.Level7;

                    for (int i = 0; i < files.Count; i++)
                    {
                        entry = zip.AddFile(files[i].Filename, "");
                        entry.FileName = files[i].RelativePath;
                        entry.LastModified = File.GetLastWriteTime(files[i].Filename);
                    }

                    //add the client file
                    entry = zip.AddEntry("iuclient.iuc", "", SaveClientFile());
                    entry.LastModified = DateTime.Now;

                    zip.Save();
                }
            }
            catch (Exception ex)
            {
                //send back the error message
                throw new Exception(Path.GetFileName(outputFilename) + ". \r\n\r\n" + ex.Message);
            }
        }

        private Stream SaveClientFile()
        {
            MemoryStream ms = new MemoryStream();

            // file-identification data
            ms.Write(Encoding.UTF8.GetBytes("IUCDFV2"), 0, 7);

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

            foreach (DictionaryEntry dLang in Languages)
            {
                LanguageCulture lang = (LanguageCulture)dLang.Value;

                //Language culture
                WriteFiles.WriteString(ms, 0x18, lang.Culture);
                
                //Language filename
                if (!string.IsNullOrEmpty(lang.Filename))
                    WriteFiles.WriteString(ms, 0x16, lang.Filename);
            }



            //Hide the header divider
            if (m_HideHeaderDivider)
                WriteFiles.WriteBool(ms, 0x17, true);

            ms.WriteByte(0xFF);

            ms.Position = 0;
            return ms;
        }

        #endregion Client Data


        #region Server Data

        public void SaveServerDatav2(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

            // Write any file-identification data you want to here
            fs.Write(Encoding.UTF8.GetBytes("IUSDFV2"), 0, 7);

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

            // write all but the last versionChoice (usually the catch-all update)

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

            // close the stream
            ms.Close();



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

                using (ZipFile zip = ZipFile.Read(fileName))
                {
                    fs = new MemoryStream();

                    zip["0"].Extract(fs);
                }


                fs.Position = 0;

                // Read the first 7 bytes of identification data
                fs.Read(fileIDBytes, 0, 7);
            }


            // see if the file is in the correct server format

            string fileID = Encoding.UTF8.GetString(fileIDBytes);

            if (fileID != "IUSDFV2")
            {
                //free up the file so it can be deleted
                fs.Close();
                throw new Exception("The downloaded server file does not have the correct identifier. This is usually caused by file corruption.");
            }



            VersionChoices.Add(new VersionChoice());

            byte bType = (byte)fs.ReadByte();
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
