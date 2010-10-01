using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace wyUpdate.Common
{
    public partial class RegChange
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegOpenKeyEx")]
        static extern int RegOpenKeyEx(IntPtr hKey, string subKey, uint options, int sam, out IntPtr phkResult);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int RegCreateKeyEx(IntPtr hKey, string lpSubKey, int Reserved, string lpClass, int dwOptions, int samDesired,
            IntPtr lpSecurityAttributes, out IntPtr phkResult, out int lpdwDisposition);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegDeleteKeyEx", SetLastError = true)]
        static extern int RegDeleteKeyEx(IntPtr hKey, string lpSubKey, int samDesired, int Reserved);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        static extern int RegQueryInfoKey(IntPtr hKey, string lpClass, int[] lpcbClass, IntPtr lpReserved_MustBeZero, ref int lpcSubKeys, int[] lpcbMaxSubKeyLen, int[] lpcbMaxClassLen, ref int lpcValues, int[] lpcbMaxValueNameLen, int[] lpcbMaxValueLen, int[] lpcbSecurityDescriptor, int[] lpftLastWriteTime);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        static extern int RegEnumKeyEx(IntPtr hKey, int dwIndex, StringBuilder lpName, out int lpcbName, int[] lpReserved, StringBuilder lpClass, int[] lpcbClass, long[] lpftLastWriteTime);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern int RegCloseKey(IntPtr hKey);

        static RegistryKey CreateSubKey32(RegistryKey pParentKey, string pSubKeyName)
        {
            IntPtr parentKeyHandle;
            if (pParentKey == null || (parentKeyHandle = GetRegistryKeyHandle(pParentKey)).Equals(IntPtr.Zero))
                throw new Exception("OpenSubKey: Parent key is not open");

            IntPtr SubKeyHandle;
            int lpdwDisposition;

            //0x0200 = KEY_WOW64_32KEY
            //0x00020006 = Write (plus ability to created values, etc == 0x2001f)
            int Result = RegCreateKeyEx(parentKeyHandle, pSubKeyName, 0, null, 0, 0x2001f | 0x200, IntPtr.Zero, out SubKeyHandle, out lpdwDisposition);
            if (Result != 0)
            {
                if ((Result == 5) || (Result == 0x542))
                {
                    throw new SecurityException("Security_RegistryPermission - you don't have permission to create the subkey \"" + pSubKeyName + "\"");
                }

                // key doesn't exist or another error
                throw new Exception("Creating WOW64 registry subkey \"" + pSubKeyName + "\" failed. Return code: " + Result);
            }

            RegistryKey key = PointerToRegistryKey(SubKeyHandle, true);

            if (key == null)
                throw new Exception("Creating WOW64 registry subkey \"" + pSubKeyName + "\" failed. PointerToRegistryKey return null.");

            return key;
        }

        static void DeleteSubKeyTree32(RegistryKey pParentKey, string pSubKeyName)
        {
            IntPtr parentKeyHandle;
            if (pParentKey == null || (parentKeyHandle = GetRegistryKeyHandle(pParentKey)).Equals(IntPtr.Zero))
                throw new Exception("OpenSubKey: Parent key is not open");

            // enumerate keys, delete each subkey

            IntPtr key = OpenSubKey32Ptr(parentKeyHandle, pSubKeyName);
            if (key != IntPtr.Zero)
            {
                try
                {
                    int numSubKeys = InternalSubKeyCount(key);
                    if (numSubKeys > 0)
                    {
                        string[] subKeyNames = InternalGetSubKeyNames(numSubKeys, key);
                        for (int i = 0; i < subKeyNames.Length; i++)
                        {
                            DeleteSubKeyTreeInternal(key, subKeyNames[i]);
                        }
                    }
                }
                finally
                {
                    RegCloseKey(key);
                }

                //0x0200 = KEY_WOW64_32KEY
                int errorCode = RegDeleteKeyEx(parentKeyHandle, pSubKeyName, 0x200, 0);

                if (errorCode != 0)
                    Win32Error(errorCode, null);
            }
            else
            {
                throw new ArgumentException("Arg_RegSubKeyAbsent");
            }
        }

        static int InternalSubKeyCount(IntPtr hkey)
        {
            int lpcSubKeys = 0;
            int lpcValues = 0;
            int errorCode = RegQueryInfoKey(hkey, null, null, IntPtr.Zero, ref lpcSubKeys, null, null, ref lpcValues, null, null, null, null);

            if (errorCode != 0)
                Win32Error(errorCode, null);

            return lpcSubKeys;
        }

        static string[] InternalGetSubKeyNames(int numSubKeys, IntPtr hkey)
        {
            string[] strArray = new string[numSubKeys];

            if (numSubKeys > 0)
            {
                StringBuilder lpName = new StringBuilder(0x100);
                for (int i = 0; i < numSubKeys; i++)
                {
                    int capacity = lpName.Capacity;
                    int errorCode = RegEnumKeyEx(hkey, i, lpName, out capacity, null, null, null, null);

                    if (errorCode != 0)
                        Win32Error(errorCode, null);

                    strArray[i] = lpName.ToString();
                }
            }

            return strArray;
        }

        static void DeleteSubKeyTreeInternal(IntPtr hParentKey, string subkey)
        {
            IntPtr key = OpenSubKey32Ptr(hParentKey, subkey);
            if (key != IntPtr.Zero)
            {
                try
                {
                    int numSubKeys = InternalSubKeyCount(key);
                    if (numSubKeys > 0)
                    {
                        string[] subKeyNames = InternalGetSubKeyNames(numSubKeys, key);
                        for (int i = 0; i < subKeyNames.Length; i++)
                        {
                            DeleteSubKeyTreeInternal(key, subKeyNames[i]);
                        }
                    }
                }
                finally
                {
                    RegCloseKey(key);
                }

                //0x0200 = KEY_WOW64_32KEY
                int errorCode = RegDeleteKeyEx(hParentKey, subkey, 0x200, 0);

                if (errorCode != 0)
                {
                    Win32Error(errorCode, null);
                }
            }
            else
            {
                throw new ArgumentException("Arg_RegSubKeyAbsent");
            }
        }

        static void Win32Error(int errorCode, string str)
        {
            switch (errorCode)
            {
                case 2:
                    throw new IOException("Arg_RegKeyNotFound", errorCode);

                case 5:
                    if (str != null)
                    {
                        throw new UnauthorizedAccessException("UnauthorizedAccess_RegistryKeyGeneric_Key: " + str);
                    }
                    throw new UnauthorizedAccessException();
            }

            throw new IOException("Registry access failed - error code: " + errorCode, errorCode);
        }

        static IntPtr OpenSubKey32Ptr(IntPtr parentKeyHandle, string pSubKeyName)
        {
            IntPtr SubKeyHandle;

            //0x0200 = KEY_WOW64_32KEY
            //0x00020019 = Read
            int Result = RegOpenKeyEx(parentKeyHandle, pSubKeyName, 0, 0x20019 | 0x200, out SubKeyHandle);
            if (Result != 0)
            {
                if ((Result == 5) || (Result == 0x542))
                {
                    throw new SecurityException("Security_RegistryPermission - you don't have permission to open the subkey.");
                }

                // key doesn't exist or another error
                return IntPtr.Zero;
            }

            return SubKeyHandle;
        }

        static RegistryKey OpenSubKey32(RegistryKey pParentKey, string pSubKeyName)
        {
            IntPtr parentKeyHandle;
            if (pParentKey == null || (parentKeyHandle = GetRegistryKeyHandle(pParentKey)).Equals(IntPtr.Zero))
                throw new Exception("OpenSubKey: Parent key is not open");

            IntPtr SubKeyHandle = OpenSubKey32Ptr(parentKeyHandle, pSubKeyName);

            if (SubKeyHandle == IntPtr.Zero)
                return null;

            return PointerToRegistryKey(SubKeyHandle, false);
        }

        static IntPtr GetRegistryKeyHandle(RegistryKey pRegisteryKey)
        {
            Type Type = Type.GetType("Microsoft.Win32.RegistryKey");
            FieldInfo Info = Type.GetField("hkey", BindingFlags.NonPublic | BindingFlags.Instance);

            SafeHandle Handle = (SafeHandle)Info.GetValue(pRegisteryKey);

            //return the real handle
            return Handle.DangerousGetHandle();
        }

        static ConstructorInfo SafeRegistryHandleConstructor;
        static ConstructorInfo RegistryKeyConstructor;

        static RegistryKey PointerToRegistryKey(IntPtr hKey, bool pWritable)
        {
            // Use reflection to get some MS only constructors they don't want us to touch
            if (SafeRegistryHandleConstructor == null)
            {
                // Create a SafeHandles.SafeRegistryHandle from this pointer - this is a private class
                Type safeRegistryHandleType = typeof(SafeHandleZeroOrMinusOneIsInvalid).Assembly.GetType("Microsoft.Win32.SafeHandles.SafeRegistryHandle");

                Type[] safeRegistryHandleConstructorTypes = new[] { typeof(IntPtr), typeof(Boolean) };
                SafeRegistryHandleConstructor = safeRegistryHandleType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, safeRegistryHandleConstructorTypes, null);

                Type[] registryKeyConstructorTypes = new[] { safeRegistryHandleType, typeof(Boolean) };
                RegistryKeyConstructor = typeof(RegistryKey).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, registryKeyConstructorTypes, null);
            }

            Object safeHandle = SafeRegistryHandleConstructor.Invoke(new Object[] { hKey, false /*pOwnsHandle*/ });

            // Create a new Registry key using the private constructor using the safeHandle
            // this should then behave like a .NET natively opened handle and disposed of correctly
            return (RegistryKey)RegistryKeyConstructor.Invoke(new[] { safeHandle, pWritable });
        }
    }
}
