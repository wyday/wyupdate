using System;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace wyUpdate.Common
{
    // The operations that can be carried out on the registry.
    public enum RegOperations { CreateValue = 0, RemoveValue = 1, CreateKey = 2, RemoveKey = 3 }

    public enum RegBasekeys { HKEY_CLASSES_ROOT = 0, HKEY_CURRENT_CONFIG = 1, HKEY_CURRENT_USER = 2, HKEY_LOCAL_MACHINE = 3, HKEY_USERS = 4 }

    public partial class RegChange : ICloneable
    {
        public RegistryValueKind RegValueKind;
        public string ValueName;
        public object ValueData;
        public string SubKey;

        internal RegBasekeys RegBasekey;
        public RegOperations RegOperation;

        public bool Is32BitKey;

        public RegChange() { }

        public RegChange(RegOperations regOp, RegBasekeys regBase, string subKey, bool is32BitKey)
        {
            RegOperation = regOp;
            RegBasekey = regBase;
            SubKey = subKey;
            RegValueKind = RegistryValueKind.String;
            Is32BitKey = is32BitKey;
        }

        public RegChange(RegOperations regOp, RegBasekeys regBase, string subKey, bool is32BitKey, string valueName)
        {
            RegOperation = regOp;
            RegBasekey = regBase;
            SubKey = subKey;
            ValueName = valueName;
            RegValueKind = RegistryValueKind.String;
            Is32BitKey = is32BitKey;
        }

        public RegChange(RegOperations regOp, RegBasekeys regBase, string subKey, bool is32BitKey, string valueName, object valueData)
        {
            RegOperation = regOp;
            RegBasekey = regBase;
            SubKey = subKey;
            ValueData = valueData;
            ValueName = valueName;
            RegValueKind = RegistryValueKind.String;
            Is32BitKey = is32BitKey;
        }

        public RegChange(RegOperations regOp, RegBasekeys regBase, string subKey, bool is32BitKey, string valueName, object valueData, RegistryValueKind valueType)
        {
            RegOperation = regOp;
            RegBasekey = regBase;
            SubKey = subKey;
            ValueData = valueData;
            ValueName = valueName;
            RegValueKind = valueType;
            Is32BitKey = is32BitKey;
        }


        //return what the RegChange does
        public override string ToString()
        {
            StringBuilder retStr = new StringBuilder();

            switch (RegOperation)
            {
                case RegOperations.CreateValue:
                    retStr.Append("Creating value ");
                    break;
                case RegOperations.RemoveValue:
                    retStr.Append("Removing value ");
                    break;
                case RegOperations.CreateKey:
                    retStr.Append("Creating subkey ");
                    break;
                case RegOperations.RemoveKey:
                    retStr.Append("Removing subkey ");
                    break;
            }

            if (RegOperation == RegOperations.CreateValue || RegOperation == RegOperations.RemoveValue)
                if (!string.IsNullOrEmpty(ValueName))
                    retStr.Append("\"" + ValueName + "\" in ");
                else
                    retStr.Append("\"(Default)\" in ");

            retStr.Append(RegBasekey + "\\" + SubKey);

            return retStr.ToString();
        }

        public object Clone()
        {
            return new RegChange(RegOperation, RegBasekey, SubKey, Is32BitKey, ValueName, ValueData, RegValueKind);
        }


        public void WriteToStream(Stream fs, bool embedBinaryData)
        {
            // beginning of RegChange
            fs.WriteByte(0x8E);

            // save the operation
            WriteFiles.WriteInt(fs, 0x01, (int)RegOperation);

            // save BaseKey
            WriteFiles.WriteInt(fs, 0x02, (int)RegBasekey);

            // Save the valueKind
            WriteFiles.WriteInt(fs, 0x03, (int)RegValueKind);

            // Save SubKey
            WriteFiles.WriteDeprecatedString(fs, 0x04, SubKey);

            // Value Name
            if (!string.IsNullOrEmpty(ValueName))
                WriteFiles.WriteDeprecatedString(fs, 0x05, ValueName);

            bool isBinaryString = !embedBinaryData
                && RegValueKind == RegistryValueKind.Binary
                && ValueData is string;

            if (isBinaryString)
                fs.WriteByte(0x80);

            if (RegOperation == RegOperations.CreateValue)
            {
                // Value Data
                switch (RegValueKind)
                {
                    case RegistryValueKind.Binary:

                        if (isBinaryString)
                            //just saving the string pointing to a file on the disk
                            WriteFiles.WriteDeprecatedString(fs, 0x07, (string)ValueData);
                        else if (embedBinaryData
                                && RegValueKind == RegistryValueKind.Binary
                                && ValueData is string)
                        {
                            //load the file and immediately write it out to fs
                            WriteOutFile(fs, 0x07, (string)ValueData);
                        }
                        else
                            //the byte array is already in memory, just write it out
                            WriteFiles.WriteByteArray(fs, 0x07, (byte[])ValueData);

                        break;
                    case RegistryValueKind.DWord:
                        WriteFiles.WriteInt(fs, 0x07, (int)ValueData);
                        break;
                    case RegistryValueKind.QWord:
                        WriteFiles.WriteLong(fs, 0x07, (long)ValueData);
                        break;
                    case RegistryValueKind.MultiString:
                        WriteFiles.WriteDeprecatedString(fs, 0x07, MultiStringToString(ValueData));
                        break;
                    case RegistryValueKind.ExpandString:
                    case RegistryValueKind.String:
                        WriteFiles.WriteDeprecatedString(fs, 0x07, (string)ValueData);
                        break;
                }
            }

            // should treat as x86 under x64 systems
            if (Is32BitKey)
                fs.WriteByte(0x81);

            //end of RegChange
            fs.WriteByte(0x9E);
        }

        static void WriteOutFile(Stream fs, byte flag, string filename)
        {
            // Note: I'm casting 'long' as an 'int'. But if someone is trying to save a 
            // file larger than 4 Gb to the registry then they have some serious problems.
            int fileNumBytes = (int)new FileInfo(filename).Length;

            //file buffer
            byte[] buffer = new byte[4096];

            //write the flag (e.g. 0x01, 0xFF, etc.)
            fs.WriteByte(flag);

            //write the length of the byte data
            fs.Write(BitConverter.GetBytes(fileNumBytes), 0, 4);

            try
            {
                // load the binary data from file and immediately write it to 'fs'
                using (FileStream binfs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    int sourceBytes;

                    do
                    {
                        sourceBytes = binfs.Read(buffer, 0, buffer.Length);

                        fs.Write(buffer, 0, sourceBytes);

                    } while (sourceBytes > 0);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("The binary data failed to load from file " + filename, ex);
            }
        }

        static string MultiStringToString(object strs)
        {
            string[] strArr = strs as string[];

            if (strArr == null)
                return (string)strs;

            StringBuilder stb = new StringBuilder();

            for (int i = 0; i < strArr.Length; i++)
            {
                stb.Append(strArr[i]);

                if (i != stb.Length - 1)
                    stb.Append("\r\n");
            }

            return stb.ToString();
        }

        public static RegChange ReadFromStream(Stream fs)
        {
            RegChange tempReg = new RegChange();

            bool isBinaryString = false;

            byte bType = (byte)fs.ReadByte();

            //read until the end byte is detected
            while (!ReadFiles.ReachedEndByte(fs, bType, 0x9E))
            {
                switch (bType)
                {
                    case 0x01://RegOperation
                        tempReg.RegOperation = (RegOperations)ReadFiles.ReadInt(fs);
                        break;
                    case 0x02://load basekey
                        tempReg.RegBasekey = (RegBasekeys)ReadFiles.ReadInt(fs);
                        break;
                    case 0x03://load valuekind
                        tempReg.RegValueKind = (RegistryValueKind)ReadFiles.ReadInt(fs);
                        break;
                    case 0x04://subkey
                        tempReg.SubKey = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x05://value name
                        tempReg.ValueName = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x06: //Depreciated: Use 0x07. All 0x06 will be converted to a string "ValueKind"
                        if (tempReg.RegValueKind != RegistryValueKind.ExpandString
                            && tempReg.RegValueKind != RegistryValueKind.String)
                        {
                            //Read in the entry,if it's
                            tempReg.RegValueKind = RegistryValueKind.String;
                        }

                        tempReg.ValueData = ReadFiles.ReadDeprecatedString(fs);
                        break;
                    case 0x80:
                        isBinaryString = true;
                        break;
                    case 0x81:
                        tempReg.Is32BitKey = true;
                        break;
                    case 0x07: //Value data
                        switch (tempReg.RegValueKind)
                        {
                            case RegistryValueKind.Binary:

                                if (isBinaryString)
                                    tempReg.ValueData = ReadFiles.ReadDeprecatedString(fs);
                                else
                                    tempReg.ValueData = ReadFiles.ReadByteArray(fs);

                                break;
                            case RegistryValueKind.DWord:
                                tempReg.ValueData = ReadFiles.ReadInt(fs);
                                break;
                            case RegistryValueKind.QWord:
                                tempReg.ValueData = ReadFiles.ReadLong(fs);
                                break;
                            case RegistryValueKind.ExpandString:
                            case RegistryValueKind.MultiString:
                            case RegistryValueKind.String:
                                tempReg.ValueData = ReadFiles.ReadDeprecatedString(fs);
                                break;
                        }
                        break;
                    default:
                        ReadFiles.SkipField(fs, bType);
                        break;
                }

                bType = (byte)fs.ReadByte();
            }

            return tempReg;
        }
    }
}
