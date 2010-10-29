using System;
using System.IO;
using System.Text;
using Ionic.Zip;

namespace wyUpdate.Common
{
    public partial class ServerFile
    {
        public static ServerFile Load(string fileName, string updatePathVar, string customUrlArgs)
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

                        string updateSite = ReadFiles.ReadDeprecatedString(fs);

                        if (updatePathVar != null)
                            updateSite = updateSite.Replace("%updatepath%", updatePathVar);

                        updateSite = updateSite.Replace("%urlargs%", customUrlArgs ?? string.Empty);

                        ClientFile.AddUniqueSite(updateSite, serv.VersionChoices[serv.VersionChoices.Count - 1].FileSites);
                        
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
                    case 0x14: // signed SHA1 hash
                        serv.VersionChoices[serv.VersionChoices.Count - 1].SignedSHA1Hash = ReadFiles.ReadByteArray(fs);
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

        bool? catchAllExists;

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
    }
}