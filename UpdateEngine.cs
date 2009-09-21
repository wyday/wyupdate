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

    public enum UpdateOn 
    { 
        DownloadingSelfUpdate = 0, FullSelfUpdate = 1, ExtractSelfUpdate = 2, InstallSelfUpdate = 3,
        DownloadingUpdate = 4, Extracting = 5, ClosingProcesses = 6, 
        PreExecute = 7, BackUpInstalling = 8, ModifyReg = 9, 
        OptimizeExecute = 10, WriteClientFile = 11, DeletingTemp = 12, Uninstalling = 13
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
    public enum InstallingTo { BaseDir = 1, SysDirx86 = 2, CommonDesktop = 4, CommonStartMenu = 8, CommonAppData = 16, SysDirx64 = 32, WindowsRoot = 64 }

    public enum ClientFileType { PreRC2, RC2, Final }

    public class UpdateEngine
    {
        #region Private Variables
        
        //Client Side Information

        public Hashtable Languages = new Hashtable();
        string m_GUID;

        //Server Side Information
        public List<VersionChoice> VersionChoices = new List<VersionChoice>();

        public UpdateEngine()
        {
            CurrentlyUpdating = UpdateOn.DownloadingUpdate;
            ServerFileSites = new List<string>(1);
            ClientServerSites = new List<string>(1);
            HeaderTextIndent = -1;
            HeaderTextColorName = "";
            HeaderImageAlign = ImageAlign.Left;
        }

        #endregion Private Variables

        #region Properties

        public UpdateOn CurrentlyUpdating { get; set; }

        public List<string> ServerFileSites { get; set; }

        public string CompanyName { get; set; }

        public string ProductName { get; set; }

        public string GUID
        {
            get
            {
                if(string.IsNullOrEmpty(m_GUID))
                {
                    // generate a GUID from the product name
                    char[] invalidChars = Path.GetInvalidFileNameChars();

                    if (ProductName.IndexOfAny(invalidChars) != -1)
                    {
                        List<char> invalidFilenameChars = new List<char>(invalidChars);

                        // there are bad filename characters
                        //make a new string builder (with at least one bad character)
                        StringBuilder newText = new StringBuilder(ProductName.Length - 1);

                        //remove the bad characters
                        for (int i = 0; i < ProductName.Length; i++)
                        {
                            if (invalidFilenameChars.IndexOf(ProductName[i]) == -1)
                                newText.Append(ProductName[i]);
                        }

                        return newText.ToString();
                    }

                    return ProductName;
                }

                return m_GUID;
            }
            set
            {
                m_GUID = value;
            }
        }

        public string InstalledVersion { get; set; }

        public ImageAlign HeaderImageAlign { get; set; }

        public string HeaderTextColorName { get; set; }

        public int HeaderTextIndent { get; set; }

        public bool HideHeaderDivider { get; set; }

        public Image TopImage { get; set; }

        public Image SideImage { get; set; }


        public string TopImageFilename { get; set; }

        public string SideImageFilename { get; set; }

        public List<string> ClientServerSites { get; set; }

        public string NewVersion { get; set; }

        public string MinClientVersion { get; set; }

        public string NoUpdateToLatestLinkText { get; set; }

        public string NoUpdateToLatestLinkURL { get; set; }

        #endregion Properties


        #region Client Data

#if CLIENT

        //Open Pre-RC2  client files
        public void OpenObsoleteClientFile(string fileName)
        {
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
            if (!ReadFiles.IsHeaderValid(fs, "IUCDFV2"))
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
                        CompanyName = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x02://Product Name
                        ProductName = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x03://Read Installed Version
                        InstalledVersion = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x04://Add server file site
                        AddUniqueSite(ReadFiles.ReadDeprecatedString(fs), ServerFileSites);
                        break;
                    case 0x09://Add client server file site
                        AddUniqueSite(ReadFiles.ReadDeprecatedString(fs), ClientServerSites);
                        break;
                    case 0x11://Header image alignment
                        try
                        {
                            HeaderImageAlign = (ImageAlign)Enum.Parse(typeof(ImageAlign), ReadFiles.ReadDeprecatedString(fs));
                        }
                        catch { }
                        break;
                    case 0x12://Header text indent
                        HeaderTextIndent = ReadFiles.ReadInt(fs);
                        break;
                    case 0x13://Header text color
                        HeaderTextColorName = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x06://top Image
                        TopImage = ReadFiles.ReadImage(fs);
                        break;
                    case 0x07://side Image
                        SideImage = ReadFiles.ReadImage(fs);
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            fs.Close();
        }


        void LoadClientData(Stream ms)
        {
            ms.Position = 0;

            // Read back the file identification data, if any
            if (!ReadFiles.IsHeaderValid(ms, "IUCDFV2"))
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
                        CompanyName = ReadFiles.ReadDeprecatedString(ms);
                        break;
                    case 0x02://Product Name
                        ProductName = ReadFiles.ReadDeprecatedString(ms);
                        break;
                    case 0x0A: // GUID
                        m_GUID = ReadFiles.ReadString(ms);
                    break;
                    case 0x03://Read Installed Version
                        InstalledVersion = ReadFiles.ReadDeprecatedString(ms);
                        break;
                    case 0x04://Add server file site
                        AddUniqueSite(ReadFiles.ReadDeprecatedString(ms), ServerFileSites);
                        break;
                    case 0x09://Add client server file site
                        AddUniqueSite(ReadFiles.ReadDeprecatedString(ms), ClientServerSites);
                        break;
                    case 0x11://Header image alignment
                        try
                        {
                            HeaderImageAlign = (ImageAlign)Enum.Parse(typeof(ImageAlign), ReadFiles.ReadDeprecatedString(ms));
                        }
                        catch { }
                        break;
                    case 0x12://Header text indent
                        HeaderTextIndent = ReadFiles.ReadInt(ms);
                        break;
                    case 0x13://Header text color
                        HeaderTextColorName = ReadFiles.ReadDeprecatedString(ms);
                        break;
                    case 0x14: //header image filename
                        TopImageFilename = ReadFiles.ReadDeprecatedString(ms);
                        break;
                    case 0x15: //side image filename
                        SideImageFilename = ReadFiles.ReadDeprecatedString(ms);
                        break;
                    case 0x18: // language culture

                        lastLanguage = new LanguageCulture(ReadFiles.ReadDeprecatedString(ms));

                        Languages.Add(lastLanguage.Culture, lastLanguage);
                        break;
                    case 0x16: //language filename

                        if (lastLanguage != null)
                            lastLanguage.Filename = ReadFiles.ReadDeprecatedString(ms);
                        else
                            Languages.Add(string.Empty, new LanguageCulture(null) { Filename = ReadFiles.ReadDeprecatedString(ms) });

                        break;
                    case 0x17: //hide the header divider
                        HideHeaderDivider = ReadFiles.ReadBool(ms);
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
                if (!string.IsNullOrEmpty(TopImageFilename))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        zip[TopImageFilename].Extract(ms);

                        // convert the bytes to an images
                        TopImage = Image.FromStream(ms, true);
                    }
                }

                // load the side image
                if (!string.IsNullOrEmpty(SideImageFilename))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        zip[SideImageFilename].Extract(ms);

                        // convert the bytes to an images
                        SideImage = Image.FromStream(ms, true);
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

        Stream SaveClientFile()
        {
            MemoryStream ms = new MemoryStream();

            // file-identification data
            WriteFiles.WriteHeader(ms, "IUCDFV2");

            //Company Name
            WriteFiles.WriteDeprecatedString(ms, 0x01, CompanyName);

            //Product Name
            WriteFiles.WriteDeprecatedString(ms, 0x02, ProductName);

            // GUID
            if (m_GUID != null)
                WriteFiles.WriteString(ms, 0x0A, m_GUID);

            //Installed Version
            WriteFiles.WriteDeprecatedString(ms, 0x03, InstalledVersion);

            foreach (string site in ServerFileSites)
            {
                //Server File Site
                WriteFiles.WriteDeprecatedString(ms, 0x04, site);
            }

            foreach (string site in ClientServerSites)
            {
                //Client Server File Site
                WriteFiles.WriteDeprecatedString(ms, 0x09, site);
            }

            //Header image alignment
            WriteFiles.WriteDeprecatedString(ms, 0x11, HeaderImageAlign.ToString());

            //Header text indent
            WriteFiles.WriteInt(ms, 0x12, HeaderTextIndent);

            //Header text color
            if (!string.IsNullOrEmpty(HeaderTextColorName))
                WriteFiles.WriteDeprecatedString(ms, 0x13, HeaderTextColorName);

            //Top image filename
            if (!string.IsNullOrEmpty(TopImageFilename))
                WriteFiles.WriteDeprecatedString(ms, 0x14, TopImageFilename);

            //Side image filename
            if (!string.IsNullOrEmpty(SideImageFilename))
                WriteFiles.WriteDeprecatedString(ms, 0x15, SideImageFilename);

            foreach (DictionaryEntry dLang in Languages)
            {
                LanguageCulture lang = (LanguageCulture)dLang.Value;

                //Language culture
                WriteFiles.WriteDeprecatedString(ms, 0x18, lang.Culture);
                
                //Language filename
                if (!string.IsNullOrEmpty(lang.Filename))
                    WriteFiles.WriteDeprecatedString(ms, 0x16, lang.Filename);
            }



            //Hide the header divider
            if (HideHeaderDivider)
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
            WriteFiles.WriteDeprecatedString(fs, 0x01, NewVersion);

            foreach (string site in ServerFileSites)
            {
                //Server File Site
                WriteFiles.WriteDeprecatedString(fs, 0x02, site);
            }

            //Minimum client version needed to install update
            WriteFiles.WriteDeprecatedString(fs, 0x07, MinClientVersion);

            
            MemoryStream ms = new MemoryStream();

            // write all but the last versionChoice (usually the catch-all update)

            for (int i = 0; i < VersionChoices.Count - 1; i++)
            {
                //Version to update from
                WriteFiles.WriteDeprecatedString(ms, 0x0B, VersionChoices[i].Version);

                foreach (string site in VersionChoices[i].FileSites)
                {
                    //Update File Site
                    WriteFiles.WriteDeprecatedString(ms, 0x03, site);
                }

                // put the marker for RTF text data
                if (VersionChoices[i].RTFChanges)
                    ms.WriteByte(0x80);

                //Changes
                WriteFiles.WriteDeprecatedString(ms, 0x04, VersionChoices[i].Changes);

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
            WriteFiles.WriteDeprecatedString(fs, 0x0B, VersionChoices[VersionChoices.Count - 1].Version);

            foreach (string site in VersionChoices[VersionChoices.Count - 1].FileSites)
            {
                //Update File Site
                WriteFiles.WriteDeprecatedString(fs, 0x03, site);
            }

            // put the marker for RTF text data
            if (VersionChoices[VersionChoices.Count - 1].RTFChanges)
                fs.WriteByte(0x80);

            //Changes
            WriteFiles.WriteDeprecatedString(fs, 0x04, VersionChoices[VersionChoices.Count - 1].Changes);

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






            foreach (string site in ClientServerSites)
            {
                //Client server site
                WriteFiles.WriteDeprecatedString(fs, 0x13, site);
            }

            // link to show when there is no update patch available

            if (!string.IsNullOrEmpty(NoUpdateToLatestLinkText))
                WriteFiles.WriteDeprecatedString(fs, 0x20, NoUpdateToLatestLinkText);

            if (!string.IsNullOrEmpty(NoUpdateToLatestLinkURL))
                WriteFiles.WriteDeprecatedString(fs, 0x21, NoUpdateToLatestLinkURL);

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
                        NewVersion = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x02://Add server file site
                        AddUniqueSite(ReadFiles.ReadDeprecatedString(fs), ServerFileSites);
                        break;
                    case 0x07: //Min Client version
                        MinClientVersion = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x0B: //The version to update from
                        if (VersionChoices.Count > 1 || VersionChoices[0].Version != null)
                            VersionChoices.Add(new VersionChoice());

                        VersionChoices[VersionChoices.Count - 1].Version = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x03://Add update file site
                        AddUniqueSite(ReadFiles.ReadDeprecatedString(fs), VersionChoices[VersionChoices.Count - 1].FileSites);
                        break;
                    case 0x80: //the changes text is in RTF format
                        VersionChoices[VersionChoices.Count - 1].RTFChanges = true;
                        break;
                    case 0x04://Read Changes
                         VersionChoices[VersionChoices.Count - 1].Changes = ReadFiles.ReadDeprecatedString(fs);
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
                        AddUniqueSite(ReadFiles.ReadDeprecatedString(fs), ClientServerSites);
                        break;
                    case 0x20:
                        NoUpdateToLatestLinkText = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x21:
                        NoUpdateToLatestLinkURL = ReadFiles.ReadDeprecatedString(fs);
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
