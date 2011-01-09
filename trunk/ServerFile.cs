using System;
using System.Collections.Generic;

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
        public byte[] SignedSHA1Hash;

        //Determine if client elevation is needed (Vista & non-admin users)
        public InstallingTo InstallingTo = 0;
        public List<RegChange> RegChanges = new List<RegChange>();
    }

    [Flags]
    public enum InstallingTo { BaseDir = 1, SysDirx86 = 2, CommonDesktop = 4, CommonStartMenu = 8, CommonAppData = 16, SysDirx64 = 32, WindowsRoot = 64, CommonFilesx86 = 128, CommonFilesx64 = 256, ServiceOrCOMReg = 512 }

    public class NoUpdatePathToNewestException : Exception { }

    public class PatchApplicationException : Exception
    {
        public PatchApplicationException(string message) : base(message) { }
    }

    public partial class ServerFile
    {
        public string NewVersion;

        //Server Side Information
        public List<VersionChoice> VersionChoices = new List<VersionChoice>();

        public string MinClientVersion;

        public string NoUpdateToLatestLinkText;

        public string NoUpdateToLatestLinkURL;

        public List<string> ServerFileSites = new List<string>(1);
    }
}