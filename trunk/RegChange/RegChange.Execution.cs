using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace wyUpdate.Common
{
    public partial class RegChange
    {

        public void ExecuteOperation()
        {
            ExecuteOperation(null);
        }

        public void ExecuteOperation(List<RegChange> rollbackRegistry)
        {
            switch (RegOperation)
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

        static object StringToMultiString(object str)
        {
            if (str is string[])
                return str;

            return ((string)str).Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        }

        void CreateValue(List<RegChange> rollbackRegistry)
        {
            if (rollbackRegistry != null)
            {
                RegChange origValue = new RegChange(RegOperations.CreateValue, RegBasekey, SubKey, Is32BitKey, ValueName);

                try
                {
                    using (RegistryKey rk = ReturnOpenKey(SubKey))
                    {
                        //create a rollback to the previous value
                        origValue.RegValueKind = rk.GetValueKind(ValueName);
                        origValue.ValueData = rk.GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    }
                }
                catch (Exception)
                {
                    origValue.RegOperation = RegOperations.RemoveValue;
                }

                rollbackRegistry.Add(origValue);
            }

            //backup any new subtree creations
            if (rollbackRegistry != null)
                BackupCreateKeyTree(rollbackRegistry);

            //set the new registry value
            using (RegistryKey regValue = ReturnCreateKey())
            {
                regValue.SetValue(ValueName,
                    RegValueKind == RegistryValueKind.MultiString
                        ? StringToMultiString(ValueData)
                        : ValueData,
                    RegValueKind);
            }
        }


        void DeleteRegistryValue(List<RegChange> rollbackRegistry)
        {
            RegistryKey reg = ReturnOpenKey(SubKey);

            //check to see if value exists
            object regValue = null;

            try
            {
                regValue = reg.GetValue(ValueName, null);
            }
            catch { }

            if (regValue != null)
            {
                //close the read-only regkey
                reg.Close();

                //reopen as writeable
                reg = ReturnCreateKey();
                reg.DeleteValue(ValueName);

                if (rollbackRegistry != null)
                {
                    //add "create value" to rollback list
                    rollbackRegistry.Add(new RegChange(RegOperations.CreateValue, RegBasekey, SubKey, Is32BitKey, ValueName, regValue));
                }
            }

            reg.Close();
        }

        RegistryKey ReturnCreateKey()
        {
            // are we a 64-bit process and installing to 32-bit registry
            bool use32bit = Is32BitKey && IntPtr.Size == 8;

            switch (RegBasekey)
            {
                case RegBasekeys.HKEY_CLASSES_ROOT:

                    if (use32bit)
                    {
#if NET4
                        return RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry32).CreateSubKey(SubKey);
#else
                        return CreateSubKey32(Registry.ClassesRoot, SubKey);
#endif
                    }

                    return Registry.ClassesRoot.CreateSubKey(SubKey);

                case RegBasekeys.HKEY_CURRENT_CONFIG:

                    if (use32bit)
                    {
#if NET4
                        return RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, RegistryView.Registry32).CreateSubKey(SubKey);
#else
                        return CreateSubKey32(Registry.CurrentConfig, SubKey);
#endif
                    }

                    return Registry.CurrentConfig.CreateSubKey(SubKey);

                case RegBasekeys.HKEY_LOCAL_MACHINE:

                    if (use32bit)
                    {
#if NET4
                        return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).CreateSubKey(SubKey);
#else
                        return CreateSubKey32(Registry.LocalMachine, SubKey);
#endif
                    }

                    return Registry.LocalMachine.CreateSubKey(SubKey);

                case RegBasekeys.HKEY_USERS:

                    if (use32bit)
                    {
#if NET4
                        return RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry32).CreateSubKey(SubKey);
#else
                        return CreateSubKey32(Registry.Users, SubKey);
#endif
                    }

                    return Registry.Users.CreateSubKey(SubKey);

                default: // HKEY_CURRENT_USER

                    if (use32bit)
                    {
#if NET4
                        return RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32).CreateSubKey(SubKey);
#else
                        return CreateSubKey32(Registry.CurrentUser, SubKey);
#endif
                    }

                    return Registry.CurrentUser.CreateSubKey(SubKey);
            }
        }

        RegistryKey ReturnOpenKey(string skey)
        {
            bool use32bit = Is32BitKey && IntPtr.Size == 8;

            //if (use32bit)
            //    return OpenSubKey32(Registry.LocalMachine, subkey, false);
            switch (RegBasekey)
            {
                case RegBasekeys.HKEY_CLASSES_ROOT:

                    if (use32bit)
                    {
#if NET4
                        return RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry32).OpenSubKey(skey);
#else
                        return OpenSubKey32(Registry.ClassesRoot, skey);
#endif
                    }

                    return Registry.ClassesRoot.OpenSubKey(skey);

                case RegBasekeys.HKEY_CURRENT_CONFIG:

                    if (use32bit)
                    {
#if NET4
                        return RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, RegistryView.Registry32).OpenSubKey(skey);
#else
                        return OpenSubKey32(Registry.CurrentConfig, skey);
#endif
                    }

                    return Registry.CurrentConfig.OpenSubKey(skey);

                case RegBasekeys.HKEY_LOCAL_MACHINE:

                    if (use32bit)
                    {
#if NET4
                        return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(skey);
#else
                        return OpenSubKey32(Registry.LocalMachine, skey);
#endif
                    }

                    return Registry.LocalMachine.OpenSubKey(skey);

                case RegBasekeys.HKEY_USERS:

                    if (use32bit)
                    {
#if NET4
                        return RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry32).OpenSubKey(skey);
#else
                        return OpenSubKey32(Registry.Users, skey);
#endif
                    }

                    return Registry.Users.OpenSubKey(skey);

                default: //HKEY_CURRENT_USER

                    if (use32bit)
                    {
#if NET4
                        return RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32).OpenSubKey(skey);
#else
                        return OpenSubKey32(Registry.CurrentUser, skey);
#endif
                    }

                    return Registry.CurrentUser.OpenSubKey(skey);
            }
        }

        void CreateKey(List<RegChange> rollbackRegistry)
        {
            //if Key already exists, bail out
            if (SubkeyExists(SubKey))
                return;

            if (rollbackRegistry != null)
                BackupCreateKeyTree(rollbackRegistry);

            // create the key
            RegistryKey key = ReturnCreateKey();
            key.Close();
        }

        void BackupCreateKeyTree(List<RegChange> rollbackRegistry)
        {
            char[] delim = { '/', '\\' };
            string[] subKeys = SubKey.Split(delim, StringSplitOptions.RemoveEmptyEntries);

            string tempKey = "";

            foreach (string t in subKeys)
            {
                tempKey += t + "\\";

                if (!SubkeyExists(tempKey))
                {
                    //backup a "delete" key
                    rollbackRegistry.Add(new RegChange(RegOperations.RemoveKey, RegBasekey, tempKey, Is32BitKey));
                    break;
                }
            }
        }

        bool SubkeyExists(string subkey)
        {
            // if key is null, the key doesn't exist
            using (RegistryKey key = ReturnOpenKey(subkey))
            {
                return key != null;
            }
        }

        void DeleteRegistryKey(List<RegChange> rollbackRegistry)
        {
            bool use32bit = Is32BitKey && IntPtr.Size == 8;

            bool ret = true;

            if (rollbackRegistry != null)
            {
                //backup the key and all subkeys and values
                ret = BackupDeleteKeyTree(SubKey, rollbackRegistry);
            }

            //ret == true if the subkey exists
            if (ret)
            {
                try
                {
                    switch (RegBasekey)
                    {
                        case RegBasekeys.HKEY_CLASSES_ROOT:
                            if (use32bit)
                            {
#if NET4
                                RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry32).DeleteSubKeyTree(SubKey);
#else
                                DeleteSubKeyTree32(Registry.ClassesRoot, SubKey);
#endif
                            }
                            else
                                Registry.ClassesRoot.DeleteSubKeyTree(SubKey);
                            break;
                        case RegBasekeys.HKEY_CURRENT_CONFIG:
                            if (use32bit)
                            {
#if NET4
                                RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, RegistryView.Registry32).DeleteSubKeyTree(SubKey);
#else
                                DeleteSubKeyTree32(Registry.CurrentConfig, SubKey);
#endif
                            }
                            else
                                Registry.CurrentConfig.DeleteSubKeyTree(SubKey);
                            break;
                        case RegBasekeys.HKEY_CURRENT_USER:
                            if (use32bit)
                            {
#if NET4
                                RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32).DeleteSubKeyTree(SubKey);
#else
                                DeleteSubKeyTree32(Registry.CurrentUser, SubKey);
#endif
                            }
                            else
                                Registry.CurrentUser.DeleteSubKeyTree(SubKey);
                            break;
                        case RegBasekeys.HKEY_LOCAL_MACHINE:
                            if (use32bit)
                            {
#if NET4
                                RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).DeleteSubKeyTree(SubKey);
#else
                                DeleteSubKeyTree32(Registry.LocalMachine, SubKey);
#endif
                            }
                            else
                                Registry.LocalMachine.DeleteSubKeyTree(SubKey);
                            break;
                        case RegBasekeys.HKEY_USERS:
                            if (use32bit)
                            {
#if NET4
                                RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry32).DeleteSubKeyTree(SubKey);
#else
                                DeleteSubKeyTree32(Registry.Users, SubKey);
#endif
                            }
                            else
                                Registry.Users.DeleteSubKeyTree(SubKey);
                            break;
                    }
                }
                catch { }
            }
        }

        bool BackupDeleteKeyTree(string subkey, List<RegChange> rollbackRegistry)
        {
            using (RegistryKey regKey = ReturnOpenKey(subkey))
            {
                if (regKey == null)
                    return false;

                //backup this current subkey
                rollbackRegistry.Add(new RegChange(RegOperations.CreateKey, RegBasekey, subkey, Is32BitKey));

                //backup all the regvalues in the subkey
                string[] regValueNames = regKey.GetValueNames();
                foreach (string regValName in regValueNames)
                {
                    rollbackRegistry.Add(new RegChange(RegOperations.CreateValue,
                        RegBasekey, subkey, Is32BitKey, regValName,
                        regKey.GetValue(regValName, null, RegistryValueOptions.DoNotExpandEnvironmentNames),
                        regKey.GetValueKind(regValName)));
                }

                //backup all children subkeys
                string[] regChildSubkeys = regKey.GetSubKeyNames();
                foreach (string sKey in regChildSubkeys)
                {
                    BackupDeleteKeyTree(subkey + "\\" + sKey, rollbackRegistry);
                }
            }

            return true;
        }
    }
}