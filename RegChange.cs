using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.IO;

namespace wyUpdate.Common
{
    // The operations that can be carried out on the registry.
    public enum RegOperations { CreateValue = 0, RemoveValue = 1, CreateKey = 2, RemoveKey = 3 }

    public enum RegBasekeys { HKEY_CLASSES_ROOT = 0, HKEY_CURRENT_CONFIG = 1, HKEY_CURRENT_USER = 2, HKEY_LOCAL_MACHINE = 3, HKEY_USERS = 4 }

    public class RegChange : ICloneable
    {
        private RegOperations m_RegOperation;
        private RegBasekeys m_RegBasekey;

        private string m_SubKey;
        private string m_ValueName;
        private object m_ValueData;
        private RegistryValueKind m_RegValueKind;

        #region Properties

        public RegistryValueKind RegValueKind
        {
            get { return m_RegValueKind; }
            set { m_RegValueKind = value; }
        }

        public string ValueName
        {
            get { return m_ValueName; }
            set { m_ValueName = value; }
        }

        public object ValueData
        {
            get { return m_ValueData; }
            set { m_ValueData = value; }
        }

        public string SubKey
        {
            get { return m_SubKey; }
            set { m_SubKey = value; }
        }


        // The base key of the operation.
        internal RegBasekeys RegBasekey
        {
            get { return m_RegBasekey; }
            set { m_RegBasekey = value; }
        }

        // The operation to be carried out on the registry.
        public RegOperations RegOperation
        {
            get { return m_RegOperation; }
            set { m_RegOperation = value; }
        }

        #endregion Properties

        public RegChange() { }

        public RegChange(RegOperations regOp, RegBasekeys regBase, string subKey)
        {
            m_RegOperation = regOp;
            m_RegBasekey = regBase;
            m_SubKey = subKey;
            m_RegValueKind = RegistryValueKind.String;
        }

        public RegChange(RegOperations regOp, RegBasekeys regBase, string subKey, string valueName)
        {
            m_RegOperation = regOp;
            m_RegBasekey = regBase;
            m_SubKey = subKey;
            m_ValueName = valueName;
            m_RegValueKind = RegistryValueKind.String;
        }

        public RegChange(RegOperations regOp, RegBasekeys regBase, string subKey, string valueName, object valueData)
        {
            m_RegOperation = regOp;
            m_RegBasekey = regBase;
            m_SubKey = subKey;
            m_ValueData = valueData;
            m_ValueName = valueName;
            m_RegValueKind = RegistryValueKind.String;
        }

        public RegChange(RegOperations regOp, RegBasekeys regBase, string subKey, string valueName, object valueData, RegistryValueKind valueType)
        {
            m_RegOperation = regOp;
            m_RegBasekey = regBase;
            m_SubKey = subKey;
            m_ValueData = valueData;
            m_ValueName = valueName;
            m_RegValueKind = valueType;
        }

        public void ExecuteOperation()
        {
            ExecuteOperation(null);
        }

        public void ExecuteOperation(List<RegChange> rollbackRegistry)
        {
            switch (m_RegOperation)
            {
                case RegOperations.CreateValue:
                    CreateValue(rollbackRegistry);
                    break;
                case RegOperations.RemoveValue:
                    DeleteRegistryValue(rollbackRegistry);
                    break;
                case RegOperations.CreateKey:
                    CreateKey(rollbackRegistry);
                    break;
                case RegOperations.RemoveKey:
                    DeleteRegistryKey(rollbackRegistry);
                    break;
            }
        }

        private void CreateValue(List<RegChange> rollbackRegistry)
        {
            if (rollbackRegistry != null)
            {
                RegChange origValue = new RegChange(RegOperations.CreateValue, m_RegBasekey, m_SubKey, m_ValueName);

                try
                {
                    RegistryKey rk = ReturnOpenKey(m_SubKey);

                    //create a rollback to the previous value
                    origValue.m_RegValueKind = rk.GetValueKind(m_ValueName);
                    origValue.m_ValueData = rk.GetValue(m_ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                }
                catch (Exception) 
                {
                    origValue.m_RegOperation = RegOperations.RemoveValue;
                }

                rollbackRegistry.Add(origValue);
            }

            //backup any new subtree creations
            if (rollbackRegistry != null)
                BackupCreateKeyTree(rollbackRegistry);

            //set the new registry value
            RegistryKey regValue = ReturnCreateKey();

            regValue.SetValue(m_ValueName,
                m_RegValueKind == RegistryValueKind.MultiString ? StringToMultiString(m_ValueData)  : m_ValueData, 
                m_RegValueKind);
            
            regValue.Close();
        }

        private static object StringToMultiString(object str)
        {
            if (typeof(string[]) == str.GetType())
                return str;
            
            return ((string)str).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void DeleteRegistryValue(List<RegChange> rollbackRegistry)
        {
            RegistryKey reg = ReturnOpenKey(m_SubKey);

            //check to see if value exists
            object regValue = null;

            try
            {
                regValue = reg.GetValue(m_ValueName, null);
            }
            catch (Exception) { }

            if (regValue != null)
            {
                //close the read-only regkey
                reg.Close();

                //reopen as writeable
                reg = ReturnCreateKey();
                reg.DeleteValue(m_ValueName);

                if (rollbackRegistry != null)
                {
                    //add "create value" to rollback list
                    rollbackRegistry.Add(new RegChange(RegOperations.CreateValue, m_RegBasekey, m_SubKey, m_ValueName, regValue));
                }
            }

            reg.Close();
        }

        private RegistryKey ReturnCreateKey()
        {
            switch (m_RegBasekey)
            {
                case RegBasekeys.HKEY_CLASSES_ROOT:
                    return Registry.ClassesRoot.CreateSubKey(m_SubKey);
                case RegBasekeys.HKEY_CURRENT_CONFIG:
                    return Registry.CurrentConfig.CreateSubKey(m_SubKey);
                case RegBasekeys.HKEY_CURRENT_USER:
                    return Registry.CurrentUser.CreateSubKey(m_SubKey);
                case RegBasekeys.HKEY_LOCAL_MACHINE:
                    return Registry.LocalMachine.CreateSubKey(m_SubKey);
                case RegBasekeys.HKEY_USERS:
                    return Registry.Users.CreateSubKey(m_SubKey);
                default:
                    return Registry.CurrentUser.CreateSubKey(m_SubKey);
            }
        }

        private RegistryKey ReturnOpenKey(string subkey)
        {
            switch (m_RegBasekey)
            {
                case RegBasekeys.HKEY_CLASSES_ROOT:
                    return Registry.ClassesRoot.OpenSubKey(subkey);
                case RegBasekeys.HKEY_CURRENT_CONFIG:
                    return Registry.CurrentConfig.OpenSubKey(subkey);
                case RegBasekeys.HKEY_CURRENT_USER:
                    return Registry.CurrentUser.OpenSubKey(subkey);
                case RegBasekeys.HKEY_LOCAL_MACHINE:
                    return Registry.LocalMachine.OpenSubKey(subkey);
                case RegBasekeys.HKEY_USERS:
                    return Registry.Users.OpenSubKey(subkey);
                default:
                    return Registry.CurrentUser.OpenSubKey(subkey);
            }
        }

        private void CreateKey(List<RegChange> rollbackRegistry)
        {
            //if Key already exists, bail out
            if (SubkeyExists(m_SubKey))
                return;

            if (rollbackRegistry != null)
                BackupCreateKeyTree(rollbackRegistry);

            switch (m_RegBasekey)
            {
                case RegBasekeys.HKEY_CLASSES_ROOT:
                    Registry.ClassesRoot.CreateSubKey(m_SubKey);
                    break;
                case RegBasekeys.HKEY_CURRENT_CONFIG:
                    Registry.CurrentConfig.CreateSubKey(m_SubKey);
                    break;
                case RegBasekeys.HKEY_LOCAL_MACHINE:
                    Registry.LocalMachine.CreateSubKey(m_SubKey);
                    break;
                case RegBasekeys.HKEY_USERS:
                    Registry.Users.CreateSubKey(m_SubKey);
                    break;

                default:
                    Registry.CurrentUser.CreateSubKey(m_SubKey);
                    break;
            }
        }

        private void BackupCreateKeyTree(List<RegChange> rollbackRegistry)
        {
            char[] delim = { '/', '\\' };
            string[] subKeys = m_SubKey.Split(delim, StringSplitOptions.RemoveEmptyEntries);

            string tempKey = "";

            for (int i = 0; i < subKeys.Length; i++)
            {
                tempKey += subKeys[i] + "\\";

                //if regKey is null, the key doesn't exist
                if (!SubkeyExists(tempKey))
                {
                    //backup a "delete" key
                    rollbackRegistry.Add(new RegChange(RegOperations.RemoveKey, m_RegBasekey, tempKey));
                    break;
                }
            }
        }

        private bool SubkeyExists(string subkey)
        {
            return ReturnOpenKey(subkey) != null;
        }

        private void DeleteRegistryKey(List<RegChange> rollbackRegistry)
        {
            bool ret = true;

            if (rollbackRegistry != null)
            {
                //backup the key and all subkeys and values
                ret = BackupDeleteKeyTree(m_SubKey, rollbackRegistry);
            }

            //ret == true if the subkey exists
            if (ret)
            {
                try
                {
                    switch (m_RegBasekey)
                    {
                        case RegBasekeys.HKEY_CLASSES_ROOT:
                            Registry.ClassesRoot.DeleteSubKeyTree(m_SubKey);
                            break;
                        case RegBasekeys.HKEY_CURRENT_CONFIG:
                            Registry.CurrentConfig.DeleteSubKeyTree(m_SubKey);
                            break;
                        case RegBasekeys.HKEY_CURRENT_USER:
                            Registry.CurrentUser.DeleteSubKeyTree(m_SubKey);
                            break;
                        case RegBasekeys.HKEY_LOCAL_MACHINE:
                            Registry.LocalMachine.DeleteSubKeyTree(m_SubKey);
                            break;
                        case RegBasekeys.HKEY_USERS:
                            Registry.Users.DeleteSubKeyTree(m_SubKey);
                            break;
                    }
                }
                catch (Exception) { }
            }
        }

        private bool BackupDeleteKeyTree(string subkey, List<RegChange> rollbackRegistry)
        {
            RegistryKey regKey = ReturnOpenKey(subkey);

            if (regKey == null)
                return false;

            //backup this current subkey
            rollbackRegistry.Add(new RegChange(RegOperations.CreateKey, m_RegBasekey, subkey));

            //backup all the regvalues in the subkey
            string[] regValueNames = regKey.GetValueNames();
            foreach (string regValName in regValueNames)
            {
                rollbackRegistry.Add(new RegChange(RegOperations.CreateValue,
                    m_RegBasekey, subkey, regValName, 
                    regKey.GetValue(regValName, null, RegistryValueOptions.DoNotExpandEnvironmentNames), 
                    regKey.GetValueKind(regValName)));
            }

            //backup all children subkeys
            string[] regChildSubkeys = regKey.GetSubKeyNames();
            foreach (string sKey in regChildSubkeys)
            {
                BackupDeleteKeyTree(subkey + "\\" + sKey, rollbackRegistry);
            }

            regKey.Close();

            return true;
        }



        //return what the RegChange does
        public override string ToString()
        {
            StringBuilder retStr = new StringBuilder();

            switch (m_RegOperation)
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
                default:
                    break;
            }

            if (m_RegOperation == RegOperations.CreateValue || m_RegOperation == RegOperations.RemoveValue)
                if (!string.IsNullOrEmpty(m_ValueName))
                    retStr.Append("\"" + m_ValueName + "\" in ");
                else
                    retStr.Append("\"(Default)\" in ");

            retStr.Append(m_RegBasekey.ToString() + "\\" + m_SubKey);

            return retStr.ToString();
        }

        public object Clone()
        {
            return new RegChange(m_RegOperation, m_RegBasekey, m_SubKey, m_ValueName, m_ValueData, m_RegValueKind);
        }

        

        public void WriteToStream(Stream fs, bool embedBinaryData)
        {
            //beginning of RegChange
            fs.WriteByte(0x8E);

            //save the operation
            WriteFiles.WriteInt(fs, 0x01, (int)RegOperation);

            //save BaseKey
            WriteFiles.WriteInt(fs, 0x02, (int)RegBasekey);

            //Save the valueKind
            WriteFiles.WriteInt(fs, 0x03, (int)RegValueKind);

            //Save SubKey
            WriteFiles.WriteString(fs, 0x04, SubKey);

            //Value Name
            WriteFiles.WriteString(fs, 0x05, ValueName);


            bool isBinaryString = !embedBinaryData
                && RegValueKind == RegistryValueKind.Binary
                && ValueData.GetType() == typeof(string);

            if (isBinaryString)
                fs.WriteByte(0x80);


            //Value Data
            switch (RegValueKind)
            {
                case RegistryValueKind.Binary:

                    if (isBinaryString)
                        //just saving the string pointing to a file on the disk
                        WriteFiles.WriteString(fs, 0x07, (string)ValueData);
                    else if (embedBinaryData
                            && RegValueKind == RegistryValueKind.Binary
                            && ValueData.GetType() == typeof(string))
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
                    WriteFiles.WriteString(fs, 0x07, MultiStringToString(ValueData));
                    break;
                case RegistryValueKind.ExpandString:
                case RegistryValueKind.String:
                    WriteFiles.WriteString(fs, 0x07, (string)ValueData);
                    break;
            }

            //end of RegChange
            fs.WriteByte(0x9E);
        }

        private static void WriteOutFile(Stream fs, byte flag, string filename)
        {
            //yes, I'm casting 'long' as an 'int'. But if someone is trying to save a 
            //file larger than 4 Gb to the registry then they have some serious problems.
            int fileNumBytes = (int)new FileInfo(filename).Length;

            //file buffer
            byte[] buffer = new byte[4096];

            //write the flag (e.g. 0x01, 0xFF, etc.)
            fs.WriteByte(flag);

            //write the length of the byte data
            fs.Write(BitConverter.GetBytes(fileNumBytes), 0, 4);


            // load the binary data from file and immediately write it to 'fs'
            FileStream binfs = null;

            try
            {
                binfs = new FileStream(filename, FileMode.Open, FileAccess.Read);

                int sourceBytes = 0;

                do
                {
                    sourceBytes = binfs.Read(buffer, 0, buffer.Length);

                    fs.Write(buffer, 0, sourceBytes);

                } while (sourceBytes > 0);


                binfs.Close();
            }
            catch (Exception ex)
            {
                if (binfs != null)
                    binfs.Close();

                throw new Exception("The binary data failed to load from file " + filename, ex);
            }
        }

        private static string MultiStringToString(object strs)
        {
            if (strs.GetType() == typeof(string))
                return (string)strs;

            StringBuilder stb = new StringBuilder();

            for (int i = 0; i < ((string[])strs).Length; i++)
            {
                stb.Append(((string[])strs)[i]);

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
                        tempReg.SubKey = ReadFiles.ReadString(fs);
                        break;
                    case 0x05://value name
                        tempReg.ValueName = ReadFiles.ReadString(fs);
                        break;
                    case 0x06: //Depreciated: Use 0x07. All 0x06 will be converted to a string "ValueKind"
                        if (tempReg.RegValueKind != RegistryValueKind.ExpandString
                            && tempReg.RegValueKind != RegistryValueKind.String)
                        {
                            //Read in the entry,if it's
                            tempReg.RegValueKind = RegistryValueKind.String;
                        }

                        tempReg.ValueData = ReadFiles.ReadString(fs);
                        break;
                    case 0x80:
                        isBinaryString = true;
                        break;
                    case 0x07: //Value data
                        switch (tempReg.RegValueKind)
                        {
                            case RegistryValueKind.Binary:

                                if (isBinaryString)
                                    tempReg.ValueData = ReadFiles.ReadString(fs);
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
                                tempReg.ValueData = ReadFiles.ReadString(fs);
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
