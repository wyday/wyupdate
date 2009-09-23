using System;
using System.IO;
using System.Windows.Forms;
using wyUpdate.Common;

namespace wyUpdate
{
    public partial class frmMain
    {
        string selfUpdateFileLoc;
        string clientSFLoc;

        string oldSelfLocation;
        string newSelfLocation;

        bool selfUpdateFromRC1;

        ServerFile SelfServerFile;

        void SaveSelfUpdateData(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

            // Write any file-identification data you want to here
            fs.Write(System.Text.Encoding.UTF8.GetBytes("IUSUFV2"), 0, 7);

            //Client data file location
            WriteFiles.WriteDeprecatedString(fs, 0x01, clientFileLoc);

            //Server data file location
            WriteFiles.WriteDeprecatedString(fs, 0x02, serverFileLoc);

            //Client server file
            WriteFiles.WriteDeprecatedString(fs, 0x03, clientSFLoc);

            //Base Directory
            WriteFiles.WriteDeprecatedString(fs, 0x04, baseDirectory);

            //Temporary directory
            WriteFiles.WriteDeprecatedString(fs, 0x05, tempDirectory);

            //Old client file location (self)
            WriteFiles.WriteDeprecatedString(fs, 0x06, Application.ExecutablePath);

            //self update needed
            WriteFiles.WriteBool(fs, 0x07, SelfUpdateState == SelfUpdateState.WillUpdate);

            //check if the new client really has been elevated
            WriteFiles.WriteBool(fs, 0x08, needElevation);

            if (!string.IsNullOrEmpty(serverOverwrite))
                WriteFiles.WriteDeprecatedString(fs, 0x09, serverOverwrite);

            if (!string.IsNullOrEmpty(autoUpdateStateFile))
                WriteFiles.WriteString(fs, 0x0A, autoUpdateStateFile);

            fs.WriteByte(0xFF);
            fs.Close();
        }

        void LoadSelfUpdateData(string fileName)
        {
            byte[] fileIDBytes = new byte[7];

            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            // Read back the file identification data, if any
            fs.Read(fileIDBytes, 0, 7);
            string fileID = System.Text.Encoding.UTF8.GetString(fileIDBytes);
            if (fileID != "IUSUFV2")
            {
                //handle self update from RC1 client
                if (fileID == "IUSUFV1")
                {
                    LoadSelfUpdateRC1Data(fs);
                    return;
                }

                //free up the file so it can be deleted
                fs.Close();
                throw new Exception("Self update fileID is wrong: " + fileID);
            }

            byte bType = (byte)fs.ReadByte();
            while (!ReadFiles.ReachedEndByte(fs, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x01://Read Client data file location
                        clientFileLoc = ReadFiles.ReadDeprecatedString(fs);

                        //TODO: wyUp 3.0: Remove this hackish behavior to cope with pre-RC2 client data files
                        if (clientFileLoc.EndsWith("iuc", StringComparison.InvariantCultureIgnoreCase))
                            clientFileType = ClientFileType.PreRC2;
                        else if (clientFileLoc.EndsWith("iucz", StringComparison.InvariantCultureIgnoreCase))
                            clientFileType = ClientFileType.RC2;
                        else
                            clientFileType = ClientFileType.Final;

                        break;
                    case 0x02: //Read Server data file location
                        serverFileLoc = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x03: //Client server file location
                        clientSFLoc = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x04://Read Base Directory
                        baseDirectory = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x05://Read Temporary directory
                        tempDirectory = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x06://Read Old client file location
                        oldSelfLocation = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x07: //true=Self Update, false=Continue update

                        SelfUpdateState = ReadFiles.ReadBool(fs)
                                              ? SelfUpdateState.FullUpdate
                                              : SelfUpdateState.ContinuingRegularUpdate;

                        break;
                    case 0x08: //is elevation required
                        needElevation = ReadFiles.ReadBool(fs);
                        break;
                    case 0x09:
                        serverOverwrite = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x0A:
                        autoUpdateStateFile = ReadFiles.ReadString(fs);
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            fs.Close();
        }

        //Backwards compatability with 1.0 RC1
        void LoadSelfUpdateRC1Data(Stream fs)
        {
            selfUpdateFromRC1 = true;

            //RC1 means it's guaranteed to be old-style client data file
            clientFileType = ClientFileType.PreRC2;

            byte bType = (byte)fs.ReadByte();

            while (!ReadFiles.ReachedEndByte(fs, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x01://Read Client data file location
                        clientFileLoc = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x02: //Read Server data file location
                        serverFileLoc = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x03://Read Base Directory
                        baseDirectory = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x04://Read Temporary directory
                        tempDirectory = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x05://Read Old client file location
                        oldSelfLocation = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x06://Read New client file location
                        newSelfLocation = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x07:
                        if (ReadFiles.ReadBool(fs))
                            SelfUpdateState = SelfUpdateState.FullUpdate;
                        break;
                    case 0x08:
                        needElevation = ReadFiles.ReadBool(fs);
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            fs.Close();
        }
    }
}