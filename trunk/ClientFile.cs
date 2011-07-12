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

    public enum ClientFileType { PreRC2, RC2, Final }

    public class ClientFile
    {
        public Hashtable Languages = new Hashtable();
        string m_GUID;

        #region Properties

        public UpdateOn CurrentlyUpdating = UpdateOn.DownloadingUpdate;

        public List<string> ServerFileSites = new List<string>(1);

        public string CompanyName;

        public string ProductName;

        public string GUID
        {
            get
            {
                if (string.IsNullOrEmpty(m_GUID))
                {
                    // generate a GUID from the product name
                    char[] invalidChars = Path.GetInvalidFileNameChars();

                    if (ProductName != null && ProductName.IndexOfAny(invalidChars) != -1)
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

        public string InstalledVersion;

        public ImageAlign HeaderImageAlign = ImageAlign.Left;

        public string HeaderTextColorName;

        public int HeaderTextIndent = -1;

        public bool HideHeaderDivider;

        public Image TopImage;

        public Image SideImage;


        public string TopImageFilename;

        public string SideImageFilename;

        public List<string> ClientServerSites = new List<string>(1);

        public bool CloseOnSuccess;

        public string CustomWyUpdateTitle;

        public string PublicSignKey;

        #endregion Properties

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

                throw new ArgumentException("The client data file (client.wyc) failed to open.\n\nFull details:\n\n" + ex.Message);
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
                        AddUniqueString(ReadFiles.ReadDeprecatedString(fs), ServerFileSites);
                        break;
                    case 0x09://Add client server file site
                        AddUniqueString(ReadFiles.ReadDeprecatedString(fs), ClientServerSites);
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
                        AddUniqueString(ReadFiles.ReadDeprecatedString(ms), ServerFileSites);
                        break;
                    case 0x09://Add client server file site
                        AddUniqueString(ReadFiles.ReadDeprecatedString(ms), ClientServerSites);
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
                    case 0x19:
                        CloseOnSuccess = ReadFiles.ReadBool(ms);
                        break;
                    case 0x1A:
                        CustomWyUpdateTitle = ReadFiles.ReadString(ms);
                        break;
                    case 0x1B:
                        PublicSignKey = ReadFiles.ReadString(ms);
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


        public void OpenClientFile(string m_Filename, ClientLanguage lang, string forcedCulture)
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
                if (Languages.Count == 1 && Languages.Contains(string.Empty))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        zip[((LanguageCulture)Languages[string.Empty]).Filename].Extract(ms);
                        lang.Open(ms);
                    }
                }
                else if (Languages.Count > 0)
                {
                    LanguageCulture useLang = null;

                    // use a forced culture
                    if (!string.IsNullOrEmpty(forcedCulture))
                        useLang = (LanguageCulture) Languages[forcedCulture];

                    // try to find the current culture
                    if (useLang == null)
                        useLang = (LanguageCulture)Languages[CultureInfo.CurrentUICulture.Name];

                    // if current culture isn't available, use the default culture (english)
                    if (useLang == null)
                        useLang = (LanguageCulture) Languages["en-US"];


                    // if the default culture isn't available, use the first available language
                    if (useLang == null)
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
                    zip.AlternateEncoding = Encoding.UTF8;
                    zip.AlternateEncodingUsage = ZipOption.AsNecessary;

                    // 0 (store only) to 9 (best compression)
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.Level7;

                    for (int i = 0; i < files.Count; i++)
                    {
                        entry = zip.AddFile(files[i].Filename, "");
                        entry.FileName = files[i].RelativePath;
                        entry.LastModified = File.GetLastWriteTime(files[i].Filename);
                    }

                    //add the client file
                    entry = zip.AddEntry("iuclient.iuc", SaveClientFile());
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

            if (CloseOnSuccess)
                WriteFiles.WriteBool(ms, 0x19, true);

            if (!string.IsNullOrEmpty(CustomWyUpdateTitle))
                WriteFiles.WriteString(ms, 0x1A, CustomWyUpdateTitle);

            if (!string.IsNullOrEmpty(PublicSignKey))
                WriteFiles.WriteString(ms, 0x1B, PublicSignKey);

            ms.WriteByte(0xFF);

            ms.Position = 0;
            return ms;
        }

        public static void AddUniqueString(string newString, List<string> list)
        {
            // if the string already exists, bail out
            foreach (string site in list)
                if (string.Equals(newString, site, StringComparison.OrdinalIgnoreCase))
                    return;

            // add the string
            list.Add(newString);
        }
    }
}
