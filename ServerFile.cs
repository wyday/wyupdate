using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ionic.Zip;

namespace wyUpdate.Common
{
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

    [Flags]
    public enum InstallingTo { BaseDir = 1, SysDirx86 = 2, CommonDesktop = 4, CommonStartMenu = 8, CommonAppData = 16, SysDirx64 = 32, WindowsRoot = 64 }

    public class NoUpdatePathToNewestException : Exception { }

    public class PatchApplicationException : Exception
    {
        public PatchApplicationException(string message) : base(message) { }
    }

    public class ServerFile
    {
        public ServerFile()
        {
            ServerFileSites = new List<string>(1);
            ClientServerSites = new List<string>(1);
        }

        public string NewVersion { get; set; }

        //Server Side Information
        public List<VersionChoice> VersionChoices = new List<VersionChoice>();

        public string MinClientVersion { get; set; }


        public string NoUpdateToLatestLinkText { get; set; }

        public string NoUpdateToLatestLinkURL { get; set; }

        public List<string> ClientServerSites { get; set; }

        public List<string> ServerFileSites { get; set; }

#if DESIGNER
        public void Save(string fileName)
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
#endif

        public static ServerFile Load(string fileName)
        {
            ServerFile serv = new ServerFile();

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



            serv.VersionChoices.Add(new VersionChoice());

            byte bType = (byte)fs.ReadByte();
            while (!ReadFiles.ReachedEndByte(fs, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x01://Read New Version
                        serv.NewVersion = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x02://Add server file site
                        ClientFile.AddUniqueSite(ReadFiles.ReadDeprecatedString(fs), serv.ServerFileSites);
                        break;
                    case 0x07: //Min Client version
                        serv.MinClientVersion = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x0B: //The version to update from
                        if (serv.VersionChoices.Count > 1 || serv.VersionChoices[0].Version != null)
                            serv.VersionChoices.Add(new VersionChoice());

                        serv.VersionChoices[serv.VersionChoices.Count - 1].Version = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x03://Add update file site
                        ClientFile.AddUniqueSite(ReadFiles.ReadDeprecatedString(fs), serv.VersionChoices[serv.VersionChoices.Count - 1].FileSites);
                        break;
                    case 0x80: //the changes text is in RTF format
                        serv.VersionChoices[serv.VersionChoices.Count - 1].RTFChanges = true;
                        break;
                    case 0x04://Read Changes
                        serv.VersionChoices[serv.VersionChoices.Count - 1].Changes = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x09://update's filesize
                        serv.VersionChoices[serv.VersionChoices.Count - 1].FileSize = ReadFiles.ReadLong(fs);
                        break;
                    case 0x08://update's Adler32 checksum
                        serv.VersionChoices[serv.VersionChoices.Count - 1].Adler32 = ReadFiles.ReadLong(fs);
                        break;
                    case 0x0A: //Installing to which directories?
                        serv.VersionChoices[serv.VersionChoices.Count - 1].InstallingTo = (InstallingTo)ReadFiles.ReadInt(fs);
                        break;
                    case 0x12: //how many regchanges to test
                        serv.VersionChoices[serv.VersionChoices.Count - 1].RegChanges.Capacity = ReadFiles.ReadInt(fs);
                        break;
                    case 0x8E: //the RegChanges
                        serv.VersionChoices[serv.VersionChoices.Count - 1].RegChanges.Add(RegChange.ReadFromStream(fs));
                        break;
                    case 0x13://add client server sites
                        ClientFile.AddUniqueSite(ReadFiles.ReadDeprecatedString(fs), serv.ClientServerSites);
                        break;
                    case 0x20:
                        serv.NoUpdateToLatestLinkText = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x21:
                        serv.NoUpdateToLatestLinkURL = ReadFiles.ReadDeprecatedString(fs);
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

            return serv;
        }

#if CLIENT
        public VersionChoice GetVersionChoice(string installedVersion)
        {
            VersionChoice updateFrom = null;

            for (int i = 0; i < VersionChoices.Count; i++)
            {
                // select the correct delta-patch version choice
                if (VersionTools.Compare(VersionChoices[i].Version, installedVersion) == 0)
                {
                    updateFrom = VersionChoices[i];
                    break;
                }
            }

            // if no delta-patch update has been selected, use the catch-all update (if it exists)
            if (updateFrom == null && CatchAllUpdateExists)
                updateFrom = VersionChoices[VersionChoices.Count - 1];

            if (updateFrom == null)
                throw new NoUpdatePathToNewestException();

            return updateFrom;
        }

        private bool? catchAllExists;

        public bool CatchAllUpdateExists
        {
            get
            {
                if (catchAllExists == null)
                {
                    catchAllExists = VersionChoices.Count > 0 &&
                                     VersionTools.Compare(VersionChoices[VersionChoices.Count - 1].Version, NewVersion) == 0;
                }

                return catchAllExists.Value;
            }
        }
#endif
    }
}