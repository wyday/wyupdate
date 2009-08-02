// ZipEntry.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2006, 2007, 2008, 2009 Dino Chiesa and Microsoft Corporation.  
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License. 
// See the file License.txt for the license details.
// More info on: http://dotnetzip.codeplex.com
//
// ------------------------------------------------------------------
//
// last saved (in emacs): 
// Time-stamp: <2009-July-30 22:49:08>
//
// ------------------------------------------------------------------
//
// This module defines the ZipEntry class, which models the entries within a zip file.
//
// Created: Tue, 27 Mar 2007  15:30
// 
// ------------------------------------------------------------------


using System;
using System.IO;
using RE = System.Text.RegularExpressions;

namespace Ionic.Zip
{
    /// <summary>
    /// An enum that provides the various encryption algorithms supported by this
    /// library.
    /// </summary>
    ///
    /// <remarks>
    ///
    /// <para>
    /// PkzipWeak implies the use of Zip 2.0 encryption, which is known to be weak and
    /// subvertible.
    /// </para>
    ///
    /// <para>
    /// A note on interoperability: Values of PkzipWeak and None are specified in <see
    /// href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">PKWARE's zip
    /// specification</see>, and are considered to be "standard".  Zip archives produced
    /// using these options will be interoperable with many other zip tools and libraries,
    /// including Windows Explorer.
    /// </para>
    ///
    /// <para>
    /// Values of <c>WinZipAes128</c> and <c>WinZipAes256</c> are not part of the Zip
    /// specification, but rather imply the use of a vendor-specific extension from
    /// WinZip. If you want to produce interoperable Zip archives, do not use these values.
    /// For example, if you produce a zip archive using WinZipAes256, you will be able to
    /// open it in Windows Explorer on Windows XP and Vista, but you will not be able to
    /// extract entries; trying this will lead to an "unspecified error". For this reason,
    /// some people have said that a zip archive that uses WinZip's AES encryption is not
    /// actually a zip archive at all.  A zip archive produced this way will be readable
    /// with the WinZip tool (Version 11 and beyond).
    /// </para>
    ///
    /// <para>
    /// There are other third-party tools and libraries, both commercial and otherwise, that
    /// support WinZip's AES encryption. These will be able to read AES-encrypted zip
    /// archives produced by DotNetZip, and conversely applications that use DotNetZip to
    /// read zip archives will be able to read AES-encrypted archives produced by those
    /// tools or libraries.  Consult the documentation for those other tools and libraries
    /// to find out if WinZip's AES encryption is supported.
    /// </para>
    ///
    /// <para>
    /// In case you care: According to <see href="http://www.winzip.com/aes_info.htm">the
    /// WinZip specification</see>, the actual AES key used is derived from the <see
    /// cref="ZipEntry.Password"/> via an algorithm that complies with <see
    /// href="http://www.ietf.org/rfc/rfc2898.txt">RFC 2898</see>, using an iteration count
    /// of 1000.  The algorithm is sometimes referred to as PBKDF2, which stands for
    /// "Password Based Key Derivation Function #2".
    /// </para>
    ///
    /// <para>
    /// A word about password strength and length: The AES encryption technology is very
    /// good, but any system is only as secure as the weakest link.  If you want to
    /// secure your data, be sure to use a password that is hard to guess.  To make it
    /// harder to guess (increase its "entropy"), you should make it longer.  If you use
    /// normal characters from an ASCII keyboard, a password of length 20 will be strong
    /// enough that it will be impossible to guess.  For more information on that, I'd
    /// encourage you to read <see
    /// href="http://www.redkestrel.co.uk/Articles/RandomPasswordStrength.html">this
    /// article.</see>
    /// </para>
    ///
    /// <para>
    /// The WinZip AES algorithms are not supported with the version of DotNetZip that
    /// runs on the .NET Compact Framework.  This is because .NET CF lacks the
    /// HMACSHA1 class that is required for producing the archive.
    /// </para>
    /// </remarks>
    public enum EncryptionAlgorithm
    {
        /// <summary>
        /// No encryption at all.
        /// </summary>
        None = 0,

        /// <summary>
        /// Traditional or Classic pkzip encryption.
        /// </summary>
        PkzipWeak,

#if AESCRYPTO
        /// <summary>
        /// WinZip AES encryption (128 key bits).
        /// </summary>
        WinZipAes128,

        /// <summary>
        /// WinZip AES encryption (256 key bits).
        /// </summary>
        WinZipAes256,
#endif

        /// <summary>
        /// An encryption algorithm that is not supported by DotNetZip.
        /// </summary>
        Unsupported = 4,


        // others... not implemented (yet?)
    }



    /// <summary>
    /// An enum for the options when extracting an entry would overwrite an existing file. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// This enum describes the actions that the library can take when an
    /// <c>Extract()</c> or <c>ExtractWithPassword()</c> method is called to extract an
    /// entry to a filesystem, and the extraction would overwrite an existing filesystem
    /// file.
    /// </para>
    /// </remarks>
    public enum ExtractExistingFileAction
    {
        /// <summary>
        /// Throw an exception when extraction would overwrite an existing file. (For
        /// COM clients, this is a 0 (zero).)
        /// </summary>
        Throw,

        /// <summary>
        /// When extraction would overwrite an existing file, overwrite the file silently.
        /// The overwrite will happen even if the target file is marked as read-only.
        /// (For COM clients, this is a 1.)
        /// </summary>
        OverwriteSilently,

        /// <summary>
        /// When extraction would overwrite an existing file, don't overwrite the file, silently. 
        /// (For COM clients, this is a 2.)
        /// </summary>
        DontOverwrite,

        /// <summary>
        /// When extraction would overwrite an existing file, invoke the ExtractProgress
        /// event, using an event type of <see
        /// cref="ZipProgressEventType.Extracting_ExtractEntryWouldOverwrite"/>.  In
        /// this way, the application can decide, just-in-time, whether to overwrite the
        /// file. For example, a GUI application may wish to pop up a dialog to allow
        /// the user to choose.  (For COM clients, this is a 3.)
        /// </summary>
        InvokeExtractProgressEvent,
    }



    /// <summary>
    /// Represents a single entry in a ZipFile. Typically, applications get a ZipEntry
    /// by enumerating the entries within a ZipFile, or by adding an entry to a ZipFile.
    /// </summary>
    public partial class ZipEntry
    {
        /// <summary>
        /// Default constructor.  
        /// </summary>
        /// <remarks>
        /// Applications should never need to call this directly.  It is exposed to
        /// support COM Automation environments.
        /// </remarks>
        public ZipEntry() { }

        //internal ZipEntry(int size) { BufferSize = size; }

        /// <summary>
        /// The time and date at which the file indicated by the <c>ZipEntry</c> was last modified. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The DotNetZip library sets the LastModified value for an entry, equal to the
        /// Last Modified time of the file in the filesystem.  If an entry is added from
        /// a stream, in which case no Last Modified attribute is available, the library
        /// uses <c>System.DateTime.Now</c> for this value, for the given entry.
        /// </para>
        ///
        /// <para>
        /// This property allows the application to retrieve and possibly set the
        /// LastModified value on an entry, to an arbitrary value.  <see
        /// cref="System.DateTime"/> values with a <see cref="System.DateTimeKind" />
        /// setting of <c>DateTimeKind.Unspecified</c> are taken to be expressed as
        /// <c>DateTimeKind.Local</c>.
        /// </para>
        ///
        /// <para>
        /// Be aware that because of the way the PKZip specification describes how times
        /// are stored in the zip file, the full precision of the <c>System.DateTime</c>
        /// datatype is not stored for the last modified time when saving zip files.
        /// For more information on how times are formatted, see the PKZip
        /// specification.
        /// </para>
        ///
        /// <para> The actual last modified time of a file can be stored in two ways in the
        /// zip file: first in the so-called "DOS" format, which has a 2-second
        /// precision. Values are rounded to the nearest even second. For example, if the
        /// time on the file is 12:34:43, then it will be stored as 12:34:44. This first
        /// value is accessible via the <c>LastModified</c> property.  Also, the last
        /// modified time can optionally be stored as an 8-byte integer quantity expressed
        /// as the number of 1/10 milliseconds (in other words the number of 100 nanosecond
        /// units) since January 1, 1601 (UTC).  This is the so-called Win32 time.  This
        /// time is accessible via the <c>Mtime</c> property.  Zip tools and libraries will
        /// always at least handle (read or write) the DOS time, and may also handle the
        /// Win32 time. When reading ZIP files, The DotNetZip library reads the Win32 time,
        /// if it is stored in the entry, and sets both <c>LastModified</c> and <c>Mtime</c>
        /// to that value. When writing ZIP files, the DotNetZip library will write both
        /// time quantities.</para>
        ///
        /// <para>
        /// The last modified time of the file created upon a call to
        /// <c>ZipEntry.Extract()</c> may be adjusted during extraction to compensate
        /// for differences in how the .NET Base Class Library deals with daylight
        /// saving time (DST) versus how the Windows filesystem deals with daylight
        /// saving time.  Raymond Chen <see
        /// href="http://blogs.msdn.com/oldnewthing/archive/2003/10/24/55413.aspx">provides
        /// some good context</see>.
        /// </para>
        ///
        /// <para>
        /// In a nutshell: Daylight savings time rules change regularly.  In 2007, for
        /// example, the inception week of DST changed.  In 1977, DST was in place all
        /// year round. In 1945, likewise.  And so on.  Win32 does not attempt to guess
        /// which time zone rules were in effect at the time in question.  It will
        /// render a time as "standard time" and allow the app to change to DST as
        /// necessary.  .NET makes a different choice.
        /// </para>
        ///
        /// <para>
        /// Compare the output of FileInfo.LastWriteTime.ToString("f") with what you
        /// see in the Windows Explorer property sheet for a file that was last
        /// written to on the other side of the DST transition. For example, suppose
        /// the file was last modified on October 17, 2003, during DST but DST is not
        /// currently in effect. Explorer's file properties reports Thursday, October
        /// 17, 2003, 8:45:38 AM, but .NETs FileInfo reports Thursday, October 17,
        /// 2003, 9:45 AM.
        /// </para>
        /// <para>
        /// Win32 says, "Thursday, October 17, 2002 8:45:38 AM PST". Note: Pacific
        /// STANDARD Time. Even though October 17 of that year occurred during Pacific
        /// Daylight Time, Win32 displays the time as standard time because that's
        /// what time it is NOW.
        /// </para>
        /// <para>
        /// .NET BCL assumes that the current DST rules were in place at the time in
        /// question.  So, .NET says, "Well, if the rules in effect now were also in
        /// effect on October 17, 2003, then that would be daylight time" so it
        /// displays "Thursday, October 17, 2003, 9:45 AM PDT" - daylight time.
        /// </para>
        /// <para>
        /// So .NET gives a value which is more intuitively correct, but is also
        /// potentially incorrect, and which is not invertible. Win32 gives a value
        /// which is intuitively incorrect, but is strictly correct.
        /// </para>
        /// <para>
        /// Because of this funkiness, this library adds one hour to the LastModified
        /// time on the extracted file, if necessary.  That is to say, if the time in
        /// question had occurred in what the .NET Base Class Library assumed to be
        /// DST. This assumption may be wrong given the constantly changing DST
        /// rules, but it is the best we can do.
        /// </para>
        /// </remarks>
        ///
        public DateTime LastModified
        {
            get { return _LastModified.ToLocalTime(); }
            set
            {
                _LastModified = (value.Kind == DateTimeKind.Unspecified)
                    ? DateTime.SpecifyKind(value, DateTimeKind.Local)
                    : value;
                //if (_ntfsTimesAreSet)
                _Mtime = _LastModified.ToUniversalTime();

                _metadataChanged = true;
            }
        }


        private int BufferSize
        {
            get { return this._zipfile.BufferSize; }
        }

        /// <summary>
        /// Last Modified time for the file represented by the entry.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para> This value corresponds to the "last modified" time in the NTFS file times
        /// as described in <see
        /// href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">the Zip
        /// specification</see>.  This value is different from <see cref="LastModified" />.
        /// </para>
        ///
        /// <para> Let me explain. Originally, waaaaay back in 1989 when the ZIP
        /// specification was originally described by the esteemed Mr. Phil Katz, the
        /// dominant operating system of the time was MS-DOS. MSDOS stored file times with a
        /// 2-second precision, because, c'mon, <em>who is ever going to need better
        /// resolution than THAT?</em> And so ZIP files, regardless of the platform on which
        /// the zip file was created, store file times in exactly <see
        /// href="http://www.vsft.com/hal/dostime.htm">the same format that DOS used in
        /// 1989</see>.  </para>
        ///
        /// <para> Since then, the ZIP spec has evolved, but the internal format for file
        /// timestamps remains the same.  Despite the fact that the way times are stored in
        /// a zip file is rooted in DOS heritage, any program on any operating system can
        /// format a time in this way, and most zip tools and libraries DO - they round file
        /// times to the nearest even second and store it just like DOS did 20 years ago.
        /// </para>
        ///
        /// <para> PKWare extended the ZIP specification to allow a zip file to store what
        /// is called "NTFS Times" for a file.  These are the <em>last write</em>, <em>last
        /// access</em>, and <em>file creation</em> times of a particular file. These
        /// metadata are not actually specific to NTFS. They are tracked for each file by
        /// NTFS, but they are also tracked by other filesystems. DotNetZip represents this
        /// with the <c>Mtime</c>, <c>Atime</c> and <c>Ctime</c> properties on a
        /// <c>ZipEntry</c>.</para>
        ///
        /// <para> Inside a zip file, these three distinct timestamps are stored in the same
        /// format that Windows NTFS uses to store file times. Rather than a resolution of 2
        /// seconds, NTFS times have a resolution of 100 nanoseconds. And, just as with the
        /// DOS time, any tool or library on any operating system is capable of formatting a
        /// time in this way and embedding it into the zip file. The key is, not all zip
        /// tools or libraries do.  Storing the higher-precision times for each entry is
        /// optional for zip files, and many tools and libraries don't use the option,
        /// though it is much nicer than the DOS time.  And futher, there are also cases
        /// where the timestamp of the file entry is not known, and is not stored. For
        /// example, this happens when content for the entry is obtained from a stream.  The
        /// bottom line is that the higher-resolution time is not guaranteed to be present
        /// for a ZipEntry.  The old DOS time, represented by <see cref="LastModified"/>, is
        /// guaranteed to be present, though it sometimes unset. </para>
        ///
        /// <para> Ok, getting back to the question about how the <c>LastModified</c>
        /// property relates to this <c>Mtime</c> property... <c>LastModified</c> is always
        /// set, while <c>Mtime</c> is not. (The other times stored in the <em>NTFS times
        /// extension</em>, <c>Ctime</c> and <c>Atime</c> also may not be set on an entry
        /// that is read from an existing zip file.) When reading a zip file, then
        /// <c>LastModified</c> takes the DOS time that is stored with the file. If the DOS time
        /// has been stored as zero in the zipfile, then this library will use
        /// <c>DateTime.Now</c> for the <c>LastModified</c> value.  If the ZIP file was
        /// created by an evolved tool, then there will also be NTFS times in the zip file.
        /// In that case, this library will read those times, and set <c>LastModified</c>
        /// and <c>Mtime</c> to the same value, the one corresponding to the last write time
        /// of the file.  If there are no "NTFS times" stored for the entry, then
        /// <c>Mtime</c> remains unset (likewise <c>Atime</c> and <c>Ctime</c>), and
        /// <c>LastModified</c> keeps its DOS time. </para>
        ///
        /// <para> When creating zip files with this library, then the NTFS time properties
        /// (<c>Mtime</c>, <c>Atime</c>, and <c>Ctime</c>) are always set on the ZipEntry
        /// instance, and these data are always stored in the zip archive for each entry. If
        /// you add an entry from an actual filesystem file, then the entry gets the actual
        /// NTFS times for that file.  If you add an entry from a stream, or a string, then
        /// the times get the value <c>DateTime.Now</c>.  In this case <c>LastModified</c>
        /// and <c>Mtime</c> will be identical, to 2 seconds of precision.  You can
        /// explicitly set the <c>Ctime</c>, <c>Atime</c>, and <c>Mtime</c> of an entry
        /// using <see cref="SetNtfsTimes(DateTime, DateTime, DateTime)"/>. Those changes
        /// are not made permanent in the zip file until you call <see
        /// cref="ZipFile.Save()"/> or one of its cousins.  </para>
        ///
        /// <para>
        /// And that is why <c>Mtime</c> may or may not be meaningful, and it may or may not agree 
        /// exactly with the <c>LastModified</c> time on the <c>ZipEntry</c>.
        /// </para>
        ///
        /// <para>
        /// I'll bet you didn't think one person could type so much about time, eh?  And reading
        /// it was so enjoyable, too!  Well, in appreciation, <see
        /// href="http://cheeso.members.winisp.net/DotNetZipDonate.aspx">maybe you should
        /// donate</see>?
        /// </para>
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipEntry.LastModified"/>
        public DateTime Mtime { get { return _Mtime; } }

        /// <summary>
        /// Last Access time for the file represented by the entry.
        /// </summary>
        /// <remarks>
        /// This value may or may not be meaningful.  If the <c>ZipEntry</c> was read from an existing
        /// Zip archive, this information may not be available. For an explanation of why, see
        /// <see cref="Mtime"/>.
        /// </remarks>
        public DateTime Atime { get { return _Atime; } }

        /// <summary>
        /// The file creation time for the file represented by the entry.
        /// </summary>
        ///
        /// <remarks> This value may or may not be meaningful.  If the <c>ZipEntry</c> was read
        /// from an existing zip archive, and the creation time was not set on the entry
        /// when the zip file was created, then this property may be meaningless. For an
        /// explanation of why, see <see cref="Mtime"/>.  </remarks>
        public DateTime Ctime { get { return _Ctime; } }

        /// <summary>
        /// Sets the NTFS Creation, Access, and Modified times for the given entry.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// When adding an entry from a file or directory, the Creation, Access, and
        /// Modified times for the given entry are automatically set from the filesystem
        /// values. When adding an entry from a stream or string, the values are
        /// implicitly set to DateTime.Now.  The application may wish to set these
        /// values to some arbitrary value, before saving the archive.  If you set the
        /// times using this method, the <see cref="LastModified"/> property also gets
        /// set, to the same value provided for mtime.
        /// </para>
        ///
        /// <para>
        /// The values you set here will be retrievable with the <see cref="Mtime"/>,
        /// <see cref="Ctime"/> and <see cref="Atime"/> read-only properties.
        /// </para>
        ///
        /// <para>
        /// When this method is called, the <see
        /// cref="EmitTimesInWindowsFormatWhenSaving"/> flag is automatically set.
        /// </para>
        ///
        /// <para>
        /// DateTime values provided here without a DateTimeKind are assumed to be Local Time.
        /// </para>
        /// </remarks>
        /// <param name="created">the creation time of the entry.</param>
        /// <param name="accessed">the last access time of the entry.</param>
        /// <param name="modified">the last modified time of the entry.</param>
        ///
        /// <seealso cref="EmitTimesInWindowsFormatWhenSaving" />
        /// <seealso cref="EmitTimesInUnixFormatWhenSaving" />
        /// <seealso cref="Atime"/>
        /// <seealso cref="Ctime"/>
        /// <seealso cref="Mtime"/>
        public void SetEntryTimes(DateTime created, DateTime accessed, DateTime modified)
        {
            _ntfsTimesAreSet = true;
            _Ctime = created.ToUniversalTime();
            _Atime = accessed.ToUniversalTime();
            _Mtime = modified.ToUniversalTime();
            _LastModified = _Mtime;
            _emitNtfsTimes = true;
            _metadataChanged = true;
        }


        /// <summary>
        /// This method is obsolete.
        /// </summary>
        [Obsolete("Please use method SetEntryTimes(DateTime,DateTime,DateTime)")]
        public void SetNtfsTimes(DateTime created, DateTime accessed, DateTime modified)
        {
            SetEntryTimes(created, accessed, modified);
        }

        /// <summary>
        /// Specifies whether the Creation, Access, and Modified times for the
        /// given entry will be emitted in "Windows format" when the zip archive is saved.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// An application creating a zip archive can use this flag to explicitly
        /// specify that the file times for the entry should or should not be stored in
        /// the zip archive in the format used by Windows. The default value of this
        /// property is <c>true</c>.
        /// </para>
        ///
        /// <para>
        /// When adding an entry from a file or directory, the Creation (<see
        /// cref="Ctime"/>), Access (<see cref="Atime"/>), and Modified (<see
        /// cref="Mtime"/>) times for the given entry are automatically set from the
        /// filesystem values. When adding an entry from a stream or string, all three
        /// values are implicitly set to DateTime.Now.  Applications can also explicitly
        /// set those times by calling <see cref="SetNtfsTimes(DateTime, DateTime,
        /// DateTime)" />.
        /// </para>
        ///
        /// <para>
        /// <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">PKWARE's
        /// zip specification</see> describes multiple ways to format these times in a
        /// zip file. One is the format Windows applications normally use: 100ns ticks
        /// since Jan 1, 1601 UTC.  The other is a format Unix applications typically
        /// use: seconds since Jan 1, 1970 UTC.  Each format can be stored in an "extra
        /// field" in the zip entry when saving the zip archive. The former uses an
        /// extra field with a Header Id of 0x000A, while the latter uses a header ID of
        /// 0x5455.
        /// </para>
        ///
        /// <para>
        /// Not all tools and libraries can interpret these fields.  Windows compressed
        /// folders is one that can read the Windows Format timestamps, while I believe
        /// the <see href="http://www.info-zip.org/">Infozip</see> tools can read the Unix
        /// format timestamps. Some tools and libraries may be able to read only one or
        /// the other.
        /// </para>
        ///
        /// <para>
        /// The times stored are taken from <see cref="Mtime"/>, <see
        /// cref="Atime"/>, and <see cref="Ctime"/>.
        /// </para>
        ///
        /// <para>
        /// This property is not mutually exclusive from the <see
        /// cref="ZipFile.EmitTimesInUnixFormatWhenSaving"/> property.
        /// It is possible that a zip entry can embed the timestamps in both
        /// forms.
        /// </para>
        ///
        /// <para>
        /// Normally you will use the <see
        /// cref="ZipFile.EmitTimesInWindowsFormatWhenSaving"/> property, to specify the
        /// behavior for all entries in a zip, rather than the property on each
        /// individual entry.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <seealso cref="SetEntryTimes(DateTime, DateTime, DateTime)"/>
        /// <seealso cref="EmitTimesInUnixFormatWhenSaving"/>
        /// <seealso cref="Ctime"/>
        /// <seealso cref="Atime"/>
        /// <seealso cref="Mtime"/>
        public bool EmitTimesInWindowsFormatWhenSaving
        {
            get
            {
                return _emitNtfsTimes;
            }
            set
            {
                _emitNtfsTimes = value;
                _metadataChanged = true;
            }
        }

        /// <summary>
        /// Specifies whether the Creation, Access, and Modified times for the given
        /// entry will be emitted in "Unix(tm) format" when the zip archive is saved.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// An application creating a zip archive can use this flag to explicitly
        /// specify that the file times for the entry should or should not be stored in
        /// the zip archive in the format used by Unix. By default this flag is
        /// <c>false</c>.
        /// </para>
        ///
        /// <para>
        /// When adding an entry from a file or directory, the Creation (<see
        /// cref="Ctime"/>), Access (<see cref="Atime"/>), and Modified (<see
        /// cref="Mtime"/>) times for the given entry are automatically set from the
        /// filesystem values. When adding an entry from a stream or string, all three
        /// values are implicitly set to DateTime.Now.  Applications can also explicitly
        /// set those times by calling <see cref="SetNtfsTimes(DateTime, DateTime,
        /// DateTime)"/>.
        /// </para>
        ///
        /// <para>
        /// <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">PKWARE's
        /// zip specification</see> describes multiple ways to format these times in a
        /// zip file. One is the format Windows applications normally use: 100ns ticks
        /// since Jan 1, 1601 UTC.  The other is a format Unix applications typically
        /// use: seconds since Jan 1, 1970 UTC.  Each format can be stored in an "extra
        /// field" in the zip entry when saving the zip archive. The former uses an
        /// extra field with a Header Id of 0x000A, while the latter uses a header ID of
        /// 0x5455.
        /// </para>
        ///
        /// <para>
        /// Not all tools and libraries can interpret these fields.  Windows compressed
        /// folders is one that can read the Windows Format timestamps, while I believe
        /// the <see href="http://www.info-zip.org/">Infozip</see> tools can read the Unix
        /// format timestamps. Some tools and libraries may be able to read only one or
        /// the other.
        /// </para>
        ///
        /// <para>
        /// The times stored are taken from <see cref="Mtime"/>, <see
        /// cref="Atime"/>, and <see cref="Ctime"/>.
        /// </para>
        ///
        /// <para>
        /// This property is not mutually exclusive from the <see
        /// cref="EmitTimesInWindowsFormatWhenSaving" /> flag.  It is possible that a
        /// zip entry can embed the timestamps in both forms.
        /// </para>
        ///
        /// <para>
        /// Normally you will use the <see
        /// cref="ZipFile.EmitTimesInUnixFormatWhenSaving"/> property, to specify the
        /// behavior for all entries, rather than the property on each individual entry.
        /// </para>
        /// </remarks>
        ///
        /// <seealso cref="SetEntryTimes(DateTime, DateTime, DateTime)"/>
        /// <seealso cref="EmitTimesInWindowsFormatWhenSaving"/>
        /// <seealso cref="ZipFile.EmitTimesInUnixFormatWhenSaving"/>
        /// <seealso cref="Ctime"/>
        /// <seealso cref="Atime"/>
        /// <seealso cref="Mtime"/>
        public bool EmitTimesInUnixFormatWhenSaving
        {
            get
            {
                return _emitUnixTimes;
            }
            set
            {
                _emitUnixTimes = value;
                _metadataChanged = true;
            }
        }


        /// <summary>
        /// The type of timestamp attached to the ZipEntry.
        /// </summary>
        ///
        /// <remarks>
        /// This property is valid only for a ZipEntry that was read from a zip archive.
        /// It indicates the type of timestamp attached to the entry. 
        /// </remarks>
        ///
        /// <seealso cref="EmitTimesInWindowsFormatWhenSaving"/>
        /// <seealso cref="EmitTimesInUnixFormatWhenSaving"/>
        public ZipEntryTimestamp Timestamp
        {
            get
            {
                return _timestamp;
            }
        }

        /// <summary>
        /// The file attributes for the entry.
        /// </summary>
        /// <remarks>
        ///
        /// <para>
        /// The <see cref="System.IO.FileAttributes">attributes</see> in NTFS include
        /// ReadOnly, Archive, Hidden, System, and Indexed.  When adding a
        /// <c>ZipEntry</c> to a ZipFile, these attributes are set implicitly when
        /// adding an entry from the filesystem.  When adding an entry from a stream or
        /// string, the Attributes are not set.
        /// </para>
        ///
        /// <para>
        /// When reading a <c>ZipEntry</c> from a ZipFile, the attributes are set
        /// according to the data stored in the ZipFile. If you extract the entry from
        /// the archive to a disk file, DotNetZip will set the attributes on the
        /// resulting file accordingly.
        /// </para>
        ///
        /// <para>
        /// The attributes can be set explicitly by the application for whatever
        /// purpose.  For example the application may wish to set the
        /// FileAttributes.ReadOnly bit for all entries added to an archive, so that on
        /// unpack, this attribute will be set on the extracted file.  Any changes you
        /// make to this property are made permanent only when you call a Save() method
        /// on the <c>ZipFile</c> instance that contains the ZipEntry.
        /// </para>
        ///
        /// <para>
        /// For example, an application may wish to zip up a directory and set the
        /// ReadOnly bit on every file in the archive, so that upon later extraction,
        /// the resulting files will be marked as ReadOnly.  Not every extraction tool
        /// respects these attributes, but if you unpack with DotNetZip, then the
        /// attributes will be set as they are stored in the ZipFile.
        /// </para>
        ///
        /// <para>
        /// These attributes may not be interesting or useful if the resulting archive
        /// is extracted on a non-Windows platform.  How these attributes get used upon
        /// extraction depends on the platform and tool used.
        /// </para>
        ///
        /// </remarks>
        public System.IO.FileAttributes Attributes
        {
            // workitem 7071
            get { return (System.IO.FileAttributes)_ExternalFileAttrs; }
            set
            {
                _ExternalFileAttrs = (int)value;
                // Since the application is explicitly setting the attributes, overwriting
                // whatever was there, we will explicitly set the Version made by field.  
                // workitem 7926 - "version made by" OS should be zero for compat with WinZip
                _VersionMadeBy = (0 << 8) + 45;  // v4.5 of the spec
                _metadataChanged = true;
            }
        }


        /// <summary>
        /// Disables compression for the entry when calling ZipFile.Save().
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// By default, the library compresses entries when saving them to archives. 
        /// When this property is set to true, the entry is not compressed when written to 
        /// the archive.  For example, the application might want to set flag to <c>true</c>
        /// this when zipping up JPG or MP3 files, which are already compressed.  The application
        /// may also want to turn off compression for other reasons.
        /// </para>
        ///
        /// <para> When updating a ZipFile, you may not turn off compression on an entry
        /// that has been encrypted.  In other words, if you read an existing <c>ZipFile</c> with
        /// one of the <c>ZipFile.Read()</c> methods, and then change the
        /// <c>CompressionMethod</c> on an entry that has <c>Encryption</c> not equal to
        /// <c>None</c>, you will receive an exception.  There is no way to modify the
        /// compression on an encrypted entry, without extracting it and re-adding it into
        /// the ZipFile.  </para> </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.ForceNoCompression"/>
        /// <seealso cref="CompressionMethod"/>
        public bool ForceNoCompression
        {
            get { return _ForceNoCompression; }
            set
            {
                if (value == _ForceNoCompression) return; // nothing to do.

                _ForceNoCompression = value;
                if (_ForceNoCompression) CompressionMethod = 0x0;
            }
        }


        /// <summary>
        /// The name of the filesystem file, referred to by the ZipEntry. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This may be different than the path used in the archive itself. What I mean is, 
        /// if you call <c>Zip.AddFile("fooo.txt", AlternativeDirectory)</c>, then the 
        /// path used for the <c>ZipEntry</c> within the zip archive will be different than this path.  
        /// This path is used to locate the thing-to-be-zipped on disk. 
        /// </para>
        /// 
        /// <para>
        /// If the entry is being added from a stream, then this is null (Nothing in VB).
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="FileName"/>
        private string LocalFileName
        {
            get { return _LocalFileName; }
        }

        /// <summary>
        /// The name of the file contained in the ZipEntry. 
        /// </summary>
        /// 
        /// <remarks>
        ///
        /// <para> This is the name of the entry in the <c>ZipFile</c> itself.  When creating or
        /// updating a zip file, this name may be different than the name of the filesystem
        /// file used to create the entry (LocalFileName). In fact, there may be no
        /// filesystem file at all, if the entry is created from a stream or a string.  When
        /// reading a zip file, this property takes the value of the entry name as stored in
        /// the zip file. If you extract such an entry, the extracted file will take the name
        /// given by this property.</para>
        ///
        /// <para> When setting this property, the value is made permanent only after a call
        /// to one of the <c>ZipFile.Save()</c> methods on the <c>ZipFile</c> that contains
        /// the ZipEntry. By reading in a <c>ZipFile</c>, then explicitly setting the
        /// FileName on an entry contained within the <c>ZipFile</c>, and then calling
        /// <c>Save()</c>, you will effectively rename the entry within the zip archive.
        /// </para>
        ///
        /// <para> When writing a zip, this value written to the zip file replaces
        /// backslashes with forward slashes, according to <see
        /// href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">the Zip
        /// specification</see>, for compatibility with Unix(tm) and ... get this.... Amiga!
        /// </para>
        ///
        /// </remarks>
        public string FileName
        {
            get { return _FileNameInArchive; }
            set
            {
                // rename the entry!
                if (value == null || value == "") throw new ZipException("The FileName must be non empty and non-null.");

                var filename = ZipEntry.NameInArchive(value, null);
                // workitem 8180
                if (_FileNameInArchive == filename) return; // nothing to do
                // workitem 8047 - renaming to a name that already exists
                if (this._zipfile.EntryFileNames.Contains(filename))
                    this._zipfile.RemoveEntry(filename);
                _FileNameInArchive = filename;
                if (this._zipfile != null) this._zipfile.NotifyEntryChanged();
                _metadataChanged = true;
            }
        }


        /// <summary>
        /// The stream that provides content for the ZipEntry.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para> The application can use this property to set the input stream for an
        /// entry on a just-in-time basis. Imagine a scenario where the application
        /// creates a <c>ZipFile</c> comprised of content obtained from hundreds of
        /// files, via calls to <c>AddFile()</c>. The DotNetZip library opens streams on
        /// these files on a just-in-time basis, only when writing the entry out to an
        /// external store within the scope of a <c>ZipFile.Save()</c> call.  Only one
        /// input stream is opened at a time, as each entry is being written
        /// out. </para>
        ///
        /// <para> Now imagine a different application that creates a zipfile with
        /// content obtained from hundreds of streams, added through <see
        /// cref="ZipFile.AddFileFromStream(string, string, System.IO.Stream)"/>.
        /// Normally the application would supply an open stream to that call.  But when
        /// large numbers of streams are being added, this can mean many open streams at
        /// one time, unnecessarily.  </para>
        ///
        /// <para> To avoid this, at the time of calling
        /// <c>ZipFile.AddFileFromStream</c> for each entry, the application can supply
        /// Stream.Null as the value of the stream parameter. The application
        /// can then open the stream on a just-in-time basis, setting this property, and
        /// thus insuring, as with the latter example, that only one stream need be
        /// opened at a time while constructing and saving the ZipFile.  </para>
        ///
        /// <para> To do this, the application should set the <c>InputStream</c>
        /// property within the context of the <see cref="ZipFile.SaveProgress"/> event,
        /// when the event type is <see
        /// cref="ZipProgressEventType.Saving_BeforeWriteEntry"/>. The application
        /// should only set <see cref="InputStream" /> for a <c>ZipEntry</c> which has the <see
        /// cref="ZipEntry.Source"/> equal to <see cref="ZipEntrySource.Stream"/>.  When
        /// the input stream is provided by the application in this way, the application
        /// is also responsible for closing or disposing the stream.  This would
        /// normally be done in the <see cref="ZipFile.SaveProgress"/> event, when the
        /// event type is <see cref="ZipProgressEventType.Saving_AfterWriteEntry"/>. See
        /// the example code provided here for how this can be done. </para>
        ///
        /// <para> Setting the value of this property when the entry was not added from
        /// a stream (for example, when the <c>ZipEntry</c> was added with <see
        /// cref="ZipFile.AddFile(String)"/> or <see
        /// cref="ZipFile.AddDirectory(String)"/>, or when the entry was added by
        /// reading an existing zip archive) will throw an exception.  </para>
        ///
        /// </remarks>
        ///
        /// <example>
        ///
        /// <para> This example adds a large number of entries to a ZipFile. The
        /// application uses the just-in-time stream provisioning mechanism to avoid
        /// keeping all the streams open simultaneiously. </para>
        ///
        /// <code lang="C#">
        /// public static void ProvisionStreams(object sender, SaveProgressEventArgs e)
        /// {
        ///     if (e.EventType == ZipProgressEventType.Saving_BeforeWriteEntry)
        ///     {
        ///         if (e.CurrentEntry.Source == ZipEntrySource.Stream &amp;&amp;
        ///               e.CurrentEntry.InputStream == Stream.Null)
        ///         {
        ///             System.IO.Stream s = MyStreamOpener(e.CurrentEntry.FileName);
        ///             e.CurrentEntry.InputStream = s;
        ///         }
        ///     }
        ///     else if (e.EventType == ZipProgressEventType.Saving_AfterWriteEntry)
        ///     {
        ///         if (e.CurrentEntry.InputStreamWasJitProvided)
        ///         {
        ///             e.CurrentEntry.InputStream.Close();
        ///         }
        ///     }
        /// }
        ///
        /// public void CreateZip()
        /// {
        ///     using (ZipFile zip = new ZipFile())
        ///     {
        ///         // add content into a "content" dir in the zip archive
        ///         foreach (string s in namesOfStreamsToZip)
        ///             zip.AddFileFromStream(s, "content", null);
        ///         // set up the event handler to provision streams "just-in-time"
        ///         zip.SaveProgress += ProvisionStreams;
        ///         zip.AddFile("Readme.txt");
        ///         zip.Save(ZipToCreate);
        ///     }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Public Shared Sub ProvisionStreams(ByVal sender As Object, ByVal e As SaveProgressEventArgs)
        ///     If (e.EventType = ZipProgressEventType.Saving_BeforeWriteEntry) Then
        ///         If (e.CurrentEntry.Source = ZipEntrySource.Stream) Then
        ///             If (e.CurrentEntry.InputStream = Stream.Null) Then
        ///                 Dim s As Stream = MyStreamOpener(e.CurrentEntry.FileName)
        ///                 e.CurrentEntry.InputStream = s
        ///             End If
        ///         End If
        ///     ElseIf (e.EventType = ZipProgressEventType.Saving_AfterWriteEntry) Then
        ///         If (e.CurrentEntry.InputStreamWasJitProvided) Then
        ///             e.CurrentEntry.InputStream.Close
        ///         End If
        ///     End If
        /// End Sub
        ///
        /// Public Sub CreateZip()
        ///     Using zip As ZipFile = new ZipFile()
        ///         ' add content into a "content" dir in the zip archive
        ///         Dim s As String
        ///         For Each s In namesOfStreamsToZip
        ///             zip.AddFileFromStream(s, "content", Nothing)
        ///         ' set up the event handler to provision streams "just-in-time"
        ///         AddHandler zip.SaveProgress, AddressOf ProvisionStreams
        ///         zip.AddFile("Readme.txt")
        ///         zip.Save(ZipToCreate)
        ///     End Using
        /// End Sub
        /// </code>
        /// </example>
        ///
        /// <seealso cref="InputStreamWasJitProvided"/>
        public Stream InputStream
        {
            get { return _sourceStream; }

            set
            {
                if (this._Source != ZipEntrySource.Stream)
                    throw new ZipException("You must not set the input stream for this ZipEntry.");

                _sourceWasJitProvided = true;
                _sourceStream = value;
            }
        }


        /// <summary>
        /// A flag indicating whether the InputStream was provided Just-in-time.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        /// When creating a zip archive, an application can obtain content for one or more of the
        /// <c>ZipEntry</c> instances from streams, using the <see
        /// cref="ZipFile.AddFileFromStream(string, string, System.IO.Stream)"/> method.  At the
        /// time of calling that method, the application can supply null as the value of the
        /// stream parameter.  By doing so, the application indicates to the library that it will
        /// provide a stream for the entry on a just-in-time basis, at the time one of the
        /// <c>ZipFile.Save()</c> methods is called and the data for the various entries are being
        /// compressed and written out.
        /// </para>
        ///
        /// <para>
        /// In this case, the application can set the <see cref="InputStream"/> property,
        /// typically within the SaveProgress event (event type: <see
        /// cref="ZipProgressEventType.Saving_BeforeWriteEntry"/>) for that entry.  
        /// </para>
        ///
        /// <para>
        /// The application will later want to call Close() and Dispose() on that stream.  In the
        /// SaveProgress event, when the event type is <see
        /// cref="ZipProgressEventType.Saving_AfterWriteEntry"/>, the application can do so.  This
        /// flag indicates that the stream has been provided by the application on a just-in-time
        /// basis and that it is the application's responsibility to call Close/Dispose on that
        /// stream.
        /// </para>
        ///
        /// </remarks>
        /// <seealso cref="InputStream"/>
        public bool InputStreamWasJitProvided
        {
            get { return _sourceWasJitProvided; }
        }



        /// <summary>
        /// An enum indicating the source of the ZipEntry.
        /// </summary>
        public ZipEntrySource Source
        {
            get { return _Source; }
        }


        /// <summary>
        /// The version of the zip engine needed to read the ZipEntry.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This is a readonly property, indicating the version of <a
        /// href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">the Zip
        /// specification</a> that the extracting tool or library must support to
        /// extract the given entry.  Generally higher versions indicate newer features.
        /// Older zip engines obviously won't know about new features, and won't be able
        /// to extract entries that depend on those newer features.
        /// </para>
        /// 
        /// <list type="table">
        /// <listheader>
        /// <term>value</term>
        /// <description>Features</description>
        /// </listheader>
        /// 
        /// <item>
        /// <term>20</term>
        /// <description>a basic Zip Entry, potentially using PKZIP encryption. 
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>45</term>
        /// <description>The ZIP64 extension is used on the entry.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>46</term>
        /// <description> File is compressed using BZIP2 compression*</description>
        /// </item>
        ///
        /// <item>
        /// <term>50</term>
        /// <description> File is encrypted using PkWare's DES, 3DES, (broken) RC2 or RC4</description>
        /// </item>
        ///
        /// <item>
        /// <term>51</term>
        /// <description> File is encrypted using PKWare's AES encryption or corrected RC2 encryption.</description>
        /// </item>
        ///
        /// <item>
        /// <term>52</term>
        /// <description> File is encrypted using corrected RC2-64 encryption**</description>
        /// </item>
        ///
        /// <item>
        /// <term>61</term>
        /// <description> File is encrypted using non-OAEP key wrapping***</description>
        /// </item>
        ///
        /// <item>
        /// <term>63</term>
        /// <description> File is compressed using LZMA, PPMd+, Blowfish, or Twofish</description>
        /// </item>
        ///
        /// </list>
        ///
        /// <para>
        /// There are other values possible, not listed here. DotNetZip supports regular
        /// PKZip encryption, and ZIP64 extensions.  DotNetZip cannot extract entries
        /// that require a zip engine higher than 45.
        /// </para>
        ///
        /// <para>
        /// This value is set upon reading an existing zip file, or after saving a zip
        /// archive.
        /// </para>
        /// </remarks>
        public Int16 VersionNeeded
        {
            get { return _VersionNeeded; }
        }

        /// <summary>
        /// The comment attached to the ZipEntry. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// Each entry in a zip file can optionally have a comment associated to it. The
        /// comment might be displayed by a zip tool during extraction, for example. 
        /// </para>
        ///
        /// <para>
        /// By default, the <c>Comment</c> is encoded in IBM437 code page. You can specify 
        /// an alternative with <see cref="ProvisionalAlternateEncoding"/>
        /// </para>
        /// </remarks>
        /// <seealso cref="ProvisionalAlternateEncoding"/>
        public string Comment
        {
            get { return _Comment; }
            set
            {
                _Comment = value;
                _metadataChanged = true;
            }
        }


        /// <summary>
        /// Indicates whether the entry requires ZIP64 extensions.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para> This property is null (Nothing in VB) until a <c>Save()</c> method on the
        /// containing <see cref="ZipFile"/> instance has been called. The property is
        /// non-null (<c>HasValue</c> is true) only after a <c>Save()</c> method has been called.
        /// </para>
        ///
        /// <para> After the containing <c>ZipFile</c> has been saved, the Value of this
        /// property is true if any of the following three conditions holds: the
        /// uncompressed size of the entry is larger than 0xFFFFFFFF; the compressed size of
        /// the entry is larger than 0xFFFFFFFF; the relative offset of the entry within the
        /// zip archive is larger than 0xFFFFFFFF.  These quantities are not known until a
        /// <c>Save()</c> is attempted on the zip archive and the compression is applied.
        /// </para>
        ///
        /// <para>If none of the three conditions holds, then the <c>Value</c> is false.</para>
        ///
        /// <para> A <c>Value</c> of false does not indicate that the entry, as saved in the
        /// zip archive, does not use ZIP64.  It merely indicates that ZIP64 is <em>not
        /// required</em>.  An entry may use ZIP64 even when not required if the <see
        /// cref="ZipFile.UseZip64WhenSaving"/> property on the containing <c>ZipFile</c> instance
        /// is set to <see cref="Zip64Option.Always"/>, or if the <see
        /// cref="ZipFile.UseZip64WhenSaving"/> property on the containing <c>ZipFile</c>
        /// instance is set to <see cref="Zip64Option.AsNecessary"/> and the output stream
        /// was not seekable.  </para>
        ///
        /// </remarks>
        /// <seealso cref="OutputUsedZip64"/>
        public Nullable<bool> RequiresZip64
        {
            get
            {
                return _entryRequiresZip64;
            }
        }

        /// <summary>
        /// Indicates whether the entry actually used ZIP64 extensions, as it was most recently 
        /// written to the output file or stream.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para> This Nullable property is null (Nothing in VB) until a <c>Save()</c>
        /// method on the containing <see cref="ZipFile"/> instance has been
        /// called. <c>HasValue</c> is true only after a <c>Save()</c> method has been
        /// called.  </para>
        ///
        /// <para>
        /// The value of this property for a particular <c>ZipEntry</c> may change over successive calls to
        /// <c>Save()</c> methods on the containing ZipFile, even if the file that corresponds to the <c>ZipEntry</c> does
        /// not. This may happen if other entries contained in the <c>ZipFile</c> expand, causing the offset 
        /// for this particular entry to exceed 0xFFFFFFFF. 
        /// </para>
        /// </remarks>
        /// <seealso cref="RequiresZip64"/>
        public Nullable<bool> OutputUsedZip64
        {
            get { return _OutputUsesZip64; }
        }


        /// <summary>
        /// The bitfield for the entry as defined in the zip spec. You probably never need to look at this.
        /// </summary>
        ///
        /// <remarks>
        /// <list type="table">
        /// <listheader>
        /// <term>bit</term>
        /// <description>meaning</description>
        /// </listheader>
        /// 
        /// <item>
        /// <term>0</term>
        /// <description>set if encryption is used.</description>
        /// </item>
        /// 
        /// <item>
        /// <term>1-2</term>
        /// <description>
        /// set to determine whether normal, max, fast deflation.  DotNetZip library
        /// always leaves these bits unset when writing (indicating "normal"
        /// deflation"), but can read an entry with any value here.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>3</term>
        /// <description>
        /// Indicates that the Crc32, Compressed and Uncompressed sizes are zero in the
        /// local header.  This bit gets set on an entry during writing a zip file, when
        /// it is saved to a non-seekable output stream.
        /// </description>
        /// </item>
        /// 
        ///
        /// <item>
        /// <term>4</term>
        /// <description>reserved for "enhanced deflating". This library doesn't do enhanced deflating.</description>
        /// </item>
        /// 
        /// <item>
        /// <term>5</term>
        /// <description>set to indicate the zip is compressed patched data.  This library doesn't do that.</description>
        /// </item>
        /// 
        /// <item>
        /// <term>6</term>
        /// <description>
        /// set if PKWare's strong encryption is used (must also set bit 1 if bit 6 is
        /// set). This bit is not set if WinZip's AES encryption is set.</description>
        /// </item>
        /// 
        /// <item>
        /// <term>7</term>
        /// <description>not used</description>
        /// </item>
        /// 
        /// <item>
        /// <term>8</term>
        /// <description>not used</description>
        /// </item>
        /// 
        /// <item>
        /// <term>9</term>
        /// <description>not used</description>
        /// </item>
        /// 
        /// <item>
        /// <term>10</term>
        /// <description>not used</description>
        /// </item>
        /// 
        /// <item>
        /// <term>11</term>
        /// <description>
        /// Language encoding flag (EFS).  If this bit is set, the filename and comment
        /// fields for this file must be encoded using UTF-8. This library currently
        /// does not support UTF-8.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>12</term>
        /// <description>Reserved by PKWARE for enhanced compression.</description>
        /// </item>
        /// 
        /// <item>
        /// <term>13</term>
        /// <description>
        /// Used when encrypting the Central Directory to indicate selected data values
        /// in the Local Header are masked to hide their actual values.  See the section
        /// in <a href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">the Zip
        /// specification</a> describing the Strong Encryption Specification for
        /// details.
        /// </description>
        /// </item>
        ///         
        /// <item>
        /// <term>14</term>
        /// <description>Reserved by PKWARE.</description>
        /// </item>
        /// 
        /// <item>
        /// <term>15</term>
        /// <description>Reserved by PKWARE.</description>
        /// </item>
        ///
        /// </list>
        ///
        /// </remarks>
        public Int16 BitField
        {
            get { return _BitField; }
        }

        /// <summary>
        /// The compression method employed for this ZipEntry. 
        /// </summary>
        /// 
        /// <remarks>
        ///
        /// <para> <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">The
        /// Zip specification</see> allows a variety of compression methods.  This library
        /// supports just two: 0x08 = Deflate.  0x00 = Store (no compression).  </para>
        /// 
        /// <para>
        /// When reading an entry from an existing zipfile, the value you retrieve here
        /// indicates the compression method used on the entry by the original creator of the zip.  
        /// When writing a zipfile, you can specify either 0x08 (Deflate) or 0x00 (None).  If you 
        /// try setting something else, you will get an exception.  
        /// </para>
        /// 
        /// <para> You may wish to set <c>CompressionMethod</c> to 0 (None) when zipping
        /// previously compressed data like a jpg, png, or mp3 file.  This can save time and
        /// cpu cycles.  Setting <c>CompressionMethod</c> to 0 is equivalent to setting
        /// <c>ForceNoCompression</c> to <c>true</c>.  </para>
        /// 
        /// <para> When updating a <c>ZipFile</c>, you may not modify the
        /// <c>CompressionMethod</c> on an entry that has been encrypted.  In other words,
        /// if you read an existing <c>ZipFile</c> with one of the <c>ZipFile.Read()</c>
        /// methods, and then change the <c>CompressionMethod</c> on an entry that has
        /// <c>Encryption</c> not equal to <c>None</c>, you will receive an exception.
        /// There is no way to modify the compression on an encrypted entry, without
        /// extracting it and re-adding it into the <c>ZipFile</c>.  </para>
        ///
        /// </remarks>
        /// 
        /// <example>
        /// In this example, the first entry added to the zip archive uses 
        /// the default behavior - compression is used where it makes sense.  
        /// The second entry, the MP3 file, is added to the archive without being compressed.
        /// <code>
        /// using (ZipFile zip = new ZipFile(ZipFileToCreate))
        /// {
        ///   ZipEntry e1= zip.AddFile(@"notes\Readme.txt");
        ///   ZipEntry e2= zip.AddFile(@"music\StopThisTrain.mp3");
        ///   e2.CompressionMethod = 0;
        ///   zip.Save();
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip As New ZipFile(ZipFileToCreate)
        ///   zip.AddFile("notes\Readme.txt")
        ///   Dim e2 as ZipEntry = zip.AddFile("music\StopThisTrain.mp3")
        ///   e2.CompressionMethod = 0
        ///   zip.Save
        /// End Using
        /// </code>
        /// </example>
        public Int16 CompressionMethod
        {
            get { return _CompressionMethod; }
            set
            {
                if (value == _CompressionMethod) return; // nothing to do.

                if (value != 0x00 && value != 0x08)
                    throw new InvalidOperationException("Unsupported compression method. Specify 8 or 0.");

                // If the source is a zip archive and there was encryption on the 
                // entry, changing the compression method is not supported. 
                if (this._Source == ZipEntrySource.Zipfile && _sourceIsEncrypted)
                    throw new InvalidOperationException("Cannot change compression method on encrypted entries read from archives.");

                _CompressionMethod = value;

                _ForceNoCompression = (_CompressionMethod == 0x0);

                _restreamRequiredOnSave = true;
            }
        }


        /// <summary>
        /// The compressed size of the file, in bytes, within the zip archive. 
        /// </summary>
        ///
        /// <remarks> The compressed size is computed during compression. The value is valid
        /// AFTER reading in an existing zip file, or AFTER saving a zipfile you are
        /// creating.  </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipEntry.UncompressedSize"/>
        public Int64 CompressedSize
        {
            get { return _CompressedSize; }
        }

        /// <summary>
        /// The size of the file, in bytes, before compression, or after extraction. 
        /// </summary>
        /// <remarks>
        /// This property is valid AFTER reading in an existing zip file, or AFTER saving the 
        /// <c>ZipFile</c> that contains the ZipEntry.
        /// </remarks>
        /// <seealso cref="Ionic.Zip.ZipEntry.CompressedSize"/>
        public Int64 UncompressedSize
        {
            get { return _UncompressedSize; }
        }

        /// <summary>
        /// The ratio of compressed size to uncompressed size of the ZipEntry.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This is a ratio of the compressed size to the uncompressed size of the entry,
        /// expressed as a double in the range of 0 to 100+. A value of 100 indicates no
        /// compression at all.  It could be higher than 100 when the compression algorithm
        /// actually inflates the data, as may occur for small files, or uncompressible
        /// data that is encrypted.
        /// </para>
        ///
        /// <para>
        /// You could format it for presentation to a user via a format string of "{3,5:F0}%"
        /// to see it as a percentage. 
        /// </para>
        ///
        /// <para>
        /// If the size of the original uncompressed file is 0, (indicating a denominator of 0)
        /// the return value will be zero. 
        /// </para>
        ///
        /// <para>
        /// This property is valid AFTER reading in an existing zip file, or AFTER saving the 
        /// <c>ZipFile</c> that contains the ZipEntry. You cannot know the effect of a compression 
        /// transform until you try it. 
        /// </para>
        ///
        /// </remarks>
        public Double CompressionRatio
        {
            get
            {
                if (UncompressedSize == 0) return 0;
                return 100 * (1.0 - (1.0 * CompressedSize) / (1.0 * UncompressedSize));
            }
        }

        /// <summary>
        /// The 32-bit CRC (Cyclic Redundancy Check) on the contents of the ZipEntry. 
        /// </summary>
        /// 
        /// <remarks>
        ///
        /// <para> You probably don't need to concern yourself with this. It is used
        /// internally by DotNetZip to verify files or streams upon extraction.  </para>
        ///
        /// <para> The value is a <see href="http://en.wikipedia.org/wiki/CRC32">32-bit
        /// CRC</see> using 0xEDB88320 for the polynomial. This is the same CRC-32 used in
        /// PNG, MPEG-2, and other protocols and formats.  It is a read-only property; when
        /// creating a Zip archive, the CRC for each entry is set only after a call to
        /// Save() on the containing ZipFile. When reading an existing zip file, the value
        /// of this property reflects the stored CRC for the entry.  </para>
        ///
        /// </remarks>
        public Int32 Crc32
        {
            get { return _Crc32; }
        }

        /// <summary>
        /// True if the entry is a directory (not a file). 
        /// This is a readonly property on the entry.
        /// </summary>
        public bool IsDirectory
        {
            get { return _IsDirectory; }
        }

        /// <summary>
        /// A derived property that is <c>true</c> if the entry uses encryption.  
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This is a readonly property on the entry.  When reading a zip file, the
        /// value for the <c>ZipEntry</c> is determined by the data read from the zip
        /// file.  After saving a ZipFile, the value of this property for each
        /// <c>ZipEntry</c> indicates whether encryption was actually used (which will
        /// have been true if the <see cref="Password"/> was set and the <see
        /// cref="Encryption"/> property was something other than <see
        /// cref="EncryptionAlgorithm.None"/>.
        /// </para>
        /// </remarks>
        public bool UsesEncryption
        {
            get { return (Encryption != EncryptionAlgorithm.None); }
        }

        /// <summary>
        /// Set this to specify which encryption algorithm to use for the entry
        /// when saving it to a zip archive.
        /// </summary>
        /// 
        /// <remarks>
        ///
        /// <para>
        /// Set this property in order to encrypt the entry when the <c>ZipFile</c> is
        /// saved. When setting this property, you must also set a <see
        /// cref="Password"/> on the entry.  If you set a value other than <see
        /// cref="EncryptionAlgorithm.None"/> on this property and do not set a
        /// <c>Password</c> then the entry will not be encrypted. The <c>ZipEntry</c>
        /// data is encrypted as the <c>ZipFile</c> is saved, when you call <see
        /// cref="ZipFile.Save()"/> or one of its cousins on the containing
        /// <c>ZipFile</c> instance. You do not need to specify the <c>Encryption</c>
        /// when extracting entries from an archive.
        /// </para>
        ///
        /// <para>
        /// The Zip specification from PKWare defines a set of encryption algorithms,
        /// and the data formats for the zip archive that support them, and PKWare
        /// supports those algorithms in the tools it produces. Other vendors of tools
        /// and libraries, such as WinZip or Xceed, typically support <em>a subset</em>
        /// of the algorithms specified by PKWare. These tools can sometimes support
        /// additional different encryption algorithms and data formats, not specified
        /// by PKWare. The AES Encryption specified and supported by WinZip is the most
        /// popular example. This library supports a subset of the complete set of
        /// algorithms specified by PKWare and other vendors.  
        /// </para>
        ///
        /// <para>
        /// There is no common, ubiquitous multi-vendor standard for strong encryption
        /// within zip files. There is broad support for so-called "traditional" Zip
        /// encryption, sometimes called Zip 2.0 encryption, as <see
        /// href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">specified
        /// by PKWare</see>, but this encryption is considered weak and
        /// breakable. This library currently supports the Zip 2.0 "weak" encryption,
        /// and also a stronger WinZip-compatible AES encryption, using either 128-bit
        /// or 256-bit key strength. If you want DotNetZip to support an algorithm
        /// that is not currently supported, call the author of this library and maybe
        /// we can talk business.
        /// </para>
        ///
        /// <para>
        /// The <see cref="ZipFile"/> class also has a <see cref="ZipFile.Encryption"/>
        /// property.  In most cases you will use <em>that</em> property when setting
        /// encryption. This property takes precedence over any <c>Encryption</c> set on the
        /// <c>ZipFile</c> itself.  Typically, you would use the per-entry Encryption when
        /// most entries in the zip archive use one encryption algorithm, and a few entries
        /// use a different one.  If all entries in the zip file use the same Encryption,
        /// then it is simpler to just set this property on the ZipFile itself, when
        /// creating a zip archive.
        /// </para>
        ///
        /// <para>
        /// Some comments on updating archives: If you read a <c>ZipFile</c>, you cannot
        /// modify the Encryption on any encrypted entry, except by extracting the entry
        /// using  the original password (if any), removing the original entry via <see
        /// cref="ZipFile.RemoveEntry(ZipEntry)"/>, and then adding a new entry with a
        /// new Password and Encryption.
        /// </para>
        ///
        /// <para>
        /// For example, suppose you read a <c>ZipFile</c>, and there is an entry encrypted
        /// with PKZip 2.0 encryption.  Setting the <c>Encryption</c> property on that
        /// <c>ZipEntry</c> to <see cref="EncryptionAlgorithm.WinZipAes256"/> will cause an
        /// exception to be thrown.  Setting the <c>Encryption</c> on the <c>ZipFile</c> and
        /// then adding new entries will allow that encryption to be used on the newly added
        /// entries.  During the <c>Save()</c>, the existing entries are copied through to
        /// the new zip archive, in their original encrypted form (encrypted or not), while
        /// the newly-added entries are encrypted as usual.
        /// </para>
        ///
        /// <para>
        /// The WinZip AES encryption algorithms are not supported on the .NET Compact
        /// Framework.
        /// </para>
        /// </remarks>
        ///
        /// <example>
        /// <para>
        /// This example creates a zip archive that uses encryption, and then extracts entries
        /// from the archive.  When creating the zip archive, the ReadMe.txt file is zipped
        /// without using a password or encryption.  The other file uses encryption.  
        /// </para>
        /// <code>
        /// // Create a zip archive with AES Encryption.
        /// using (ZipFile zip = new ZipFile())
        /// {
        ///     zip.AddFile("ReadMe.txt")
        ///     ZipEntry e1= zip.AddFile("2008-Regional-Sales-Report.pdf");
        ///     e1.Encryption= EncryptionAlgorithm.WinZipAes256;
        ///     e1.Password= "Top.Secret.No.Peeking!";
        ///     zip.Save("EncryptedArchive.zip");
        /// }
        /// 
        /// // Extract a zip archive that uses AES Encryption.
        /// // You do not need to specify the algorithm during extraction.
        /// using (ZipFile zip = ZipFile.Read("EncryptedArchive.zip"))
        /// {
        ///     // Specify the password that is used during extraction, for
        ///     // all entries that require a password:
        ///     zip.Password= "Top.Secret.No.Peeking!";
        ///     zip.ExtractAll("extractDirectory");
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// ' Create a zip that uses Encryption.
        /// Using zip As New ZipFile()
        ///     zip.AddFile("ReadMe.txt")
        ///     Dim e1 as ZipEntry
        ///     e1= zip.AddFile("2008-Regional-Sales-Report.pdf")
        ///     e1.Encryption= EncryptionAlgorithm.WinZipAes256
        ///     e1.Password= "Top.Secret.No.Peeking!"
        ///     zip.Save("EncryptedArchive.zip")
        /// End Using
        /// 
        /// ' Extract a zip archive that uses AES Encryption.
        /// ' You do not need to specify the algorithm during extraction.
        /// Using (zip as ZipFile = ZipFile.Read("EncryptedArchive.zip"))
        ///     ' Specify the password that is used during extraction, for
        ///     ' all entries that require a password:
        ///     zip.Password= "Top.Secret.No.Peeking!"
        ///     zip.ExtractAll("extractDirectory")
        /// End Using
        /// </code>
        ///
        /// </example>
        /// 
        /// <exception cref="System.InvalidOperationException">
        /// Thrown in the setter if EncryptionAlgorithm.Unsupported is specified, or
        /// if setting the property on an entry read from a zip archive.
        /// </exception>
        ///
        /// <seealso cref="Ionic.Zip.ZipEntry.Password">ZipEntry.Password</seealso>
        /// <seealso cref="Ionic.Zip.ZipFile.Encryption">ZipFile.Encryption</seealso>
        public EncryptionAlgorithm Encryption
        {
            get
            {
                return _Encryption;
            }
            set
            {
                if (value == _Encryption) return;

                if (value == EncryptionAlgorithm.Unsupported)
                    throw new InvalidOperationException("You may not set Encryption to that value.");

                // If the source is a zip archive and there was encryption
                // on the entry, this will not work. 
                if (this._Source == ZipEntrySource.Zipfile && _sourceIsEncrypted)
                    throw new InvalidOperationException("You cannot change the encryption method on encrypted entries read from archives.");

                _Encryption = value;
                _restreamRequiredOnSave = true;

#if AESCRYPTO
                if (value == EncryptionAlgorithm.WinZipAes256) this._KeyStrengthInBits = 256;
                else if (value == EncryptionAlgorithm.WinZipAes128) this._KeyStrengthInBits = 128;
#endif
            }
        }


        /// <summary>
        /// The Password to be used when encrypting a <c>ZipEntry</c> upon
        /// <c>ZipFile.Save()</c>, or when decrypting an entry upon Extract().
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This is a write-only property on the entry. Set this to request that the
        /// entry be encrypted when writing the zip archive, or set it to specify the
        /// password to be used when extracting an existing entry that is encrypted.
        /// </para>
        ///
        /// <para>
        /// The password set here is implicitly used to encrypt the entry during the
        /// <see cref="ZipFile.Save()"/> operation, or to decrypt during the <see
        /// cref="Extract()"/> or <see cref="OpenReader()"/> operation.  If you set the
        /// Password on a <c>ZipEntry</c> after calling <c>Save()</c>, there is no effect.
        /// </para>
        ///
        /// <para>
        /// Consider setting the <see cref="Encryption"/> property when using a
        /// password. Answering concerns that the standard password protection supported by
        /// all zip tools is weak, WinZip has extended the ZIP specification with a way to
        /// use AES Encryption to protect entries in the Zip file. Unlike the "PKZIP 2.0"
        /// encryption specified in the PKZIP specification, <see href=
        /// "http://en.wikipedia.org/wiki/Advanced_Encryption_Standard">AES Encryption</see>
        /// uses a standard, strong, tested, encryption algorithm. DotNetZip can create zip
        /// archives that use WinZip-compatible AES encryption, if you set the <see
        /// cref="Encryption"/> property. But, archives created that use AES encryption may
        /// not be readable by all other tools and libraries. For example, Windows Explorer
        /// cannot read a "compressed folder" (a zip file) that uses AES encryption, though
        /// it can read a zip file that uses "PKZIP encryption."
        /// </para>
        ///
        /// <para>
        /// The <see cref="ZipFile"/> class also has a <see cref="ZipFile.Password"/>
        /// property.  This property takes precedence over any password set on the ZipFile
        /// itself.  Typically, you would use the per-entry Password when most entries in
        /// the zip archive use one password, and a few entries use a different password.
        /// If all entries in the zip file use the same password, then it is simpler to just
        /// set this property on the ZipFile itself, whether creating a zip archive or
        /// extracting a zip archive.
        /// </para>
        ///
        /// <para>
        /// Some comments on updating archives: If you read a <c>ZipFile</c>, you cannot
        /// modify the password on any encrypted entry, except by extracting the entry
        /// with the original password (if any), removing the original entry via <see
        /// cref="ZipFile.RemoveEntry(ZipEntry)"/>, and then adding a new entry with a
        /// new Password.
        /// </para>
        ///
        /// <para>
        /// For example, suppose you read a <c>ZipFile</c>, and there is an encrypted entry.
        /// Setting the Password property on that <c>ZipEntry</c> and then calling
        /// <c>Save()</c> on the <c>ZipFile</c> does not update the password on that entry in
        /// the archive.  Neither is an exception thrown. Instead, what happens during
        /// the <c>Save()</c> is the existing entry is copied through to the new zip
        /// archive, in its original encrypted form. Upon re-reading that archive, the
        /// entry can be decrypted with its original password.
        /// </para>
        ///
        /// <para>
        /// If you read a ZipFile, and there is an un-encrypted entry, you can set the
        /// <c>Password</c> on the entry and then call Save() on the ZipFile, and get
        /// encryption on that entry.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <example>
        /// <para>
        /// This example creates a zip file with two entries, and then extracts the
        /// entries from the zip file.  When creating the zip file, the two files are
        /// added to the zip file using password protection. Each entry uses a different
        /// password.  During extraction, each file is extracted with the appropriate
        /// password.
        /// </para>
        /// <code>
        /// // create a file with encryption
        /// using (ZipFile zip = new ZipFile())
        /// {
        ///     ZipEntry entry;
        ///     entry= zip.AddFile("Declaration.txt");
        ///     entry.Password= "123456!";
        ///     entry = zip.AddFile("Report.xls");
        ///     entry.Password= "1Secret!";
        ///     zip.Save("EncryptedArchive.zip");
        /// }
        /// 
        /// // extract entries that use encryption
        /// using (ZipFile zip = ZipFile.Read("EncryptedArchive.zip"))
        /// {
        ///     ZipEntry entry;
        ///     entry = zip["Declaration.txt"];
        ///     entry.Password = "123456!";
        ///     entry.Extract("extractDir");
        ///     entry = zip["Report.xls"];
        ///     entry.Password = "1Secret!";
        ///     entry.Extract("extractDir");
        /// }
        /// 
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As New ZipFile
        ///     Dim entry as ZipEntry
        ///     entry= zip.AddFile("Declaration.txt")
        ///     entry.Password= "123456!"
        ///     entry = zip.AddFile("Report.xls")
        ///     entry.Password= "1Secret!"
        ///     zip.Save("EncryptedArchive.zip")
        /// End Using
        ///
        ///
        /// ' extract entries that use encryption
        /// Using (zip as ZipFile = ZipFile.Read("EncryptedArchive.zip"))
        ///     Dim entry as ZipEntry
        ///     entry = zip("Declaration.txt")
        ///     entry.Password = "123456!"
        ///     entry.Extract("extractDir")
        ///     entry = zip("Report.xls")
        ///     entry.Password = "1Secret!"
        ///     entry.Extract("extractDir")
        /// End Using
        /// 
        /// </code>
        ///
        /// </example>
        /// 
        /// <seealso cref="Ionic.Zip.ZipEntry.Encryption"/>
        /// <seealso cref="Ionic.Zip.ZipFile.Password">ZipFile.Password</seealso>
        public string Password
        {
            set
            {
                _Password = value;
                if (_Password == null)
                {
                    _Encryption = EncryptionAlgorithm.None;
                }
                else
                {
                    // We're setting a non-null password.

                    // For entries obtained from a zip file that are encrypted, we cannot
                    // simply restream (recompress, re-encrypt) the file data, because we
                    // need the old password in order to decrypt the data, and then we
                    // need the new password to encrypt.  So, setting the password is
                    // never going to work on an entry that is stored encrypted in a zipfile. 

                    // But it is not en error to set the password, obviously: callers will
                    // set the password in order to Extract encrypted archives.

                    // If the source is a zip archive and there was previously no encryption
                    // on the entry, then we must re-stream the entry in order to encrypt it.
                    if (this._Source == ZipEntrySource.Zipfile && !_sourceIsEncrypted)
                        _restreamRequiredOnSave = true;

                    if (Encryption == EncryptionAlgorithm.None)
                    {
                        _Encryption = EncryptionAlgorithm.PkzipWeak;
                    }
                }

            }
        }



        /// <summary>
        /// Specifies that the extraction should overwrite any existing files. This
        /// property is Obsolete.
        /// </summary>
        /// <remarks>
        /// This property is Obsolete. Please don't use it!  Instead, use property <see
        /// cref="ExtractExistingFile"/>.  If you must use it, you should know this:
        /// this property applies only when calling an Extract method. By default this
        /// property is false.
        /// </remarks>
        /// <seealso cref="Ionic.Zip.ZipEntry.ExtractExistingFile"/>
        [Obsolete("Please use property ExtractExistingFile")]
        public bool OverwriteOnExtract
        {
            get
            {
                return (ExtractExistingFile == ExtractExistingFileAction.OverwriteSilently);
            }
            set
            {
                // legacy behavior
                ExtractExistingFile = (value)
                    ? ExtractExistingFileAction.OverwriteSilently
                    : ExtractExistingFileAction.Throw;
            }
        }




        /// <summary>
        /// The action the library should take when extracting a file that already exists.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property affects the behavior of the Extract methods (one of the <c>Extract()</c>
        /// or <c>ExtractWithPassword()</c> overloads), when extraction would would overwrite an
        /// existing filesystem file. If you do not set this property, the library throws an
        /// exception when extracting an entry would overwrite an existing file.
        /// </para>
        ///
        /// <para>
        /// This property has no effect when extracting to a stream, or when the file to be
        /// extracted does not already exist. 
        /// </para>
        /// </remarks>
        /// <seealso cref="Ionic.Zip.ZipFile.ExtractExistingFile"/>
        public ExtractExistingFileAction ExtractExistingFile
        {
            get;
            set;
        }


        /// <summary>
        /// A callback that allows the application to specify whether multiple reads of the
        /// stream should be performed, in the case that a compression operation actually
        /// inflates the size of the file data.  
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// In some cases, applying the Deflate compression algorithm in DeflateStream can
        /// result an increase in the size of the data.  This "inflation" can happen with
        /// previously compressed files, such as a zip, jpg, png, mp3, and so on.  In a few
        /// tests, inflation on zip files can be as large as 60%!  Inflation can also happen
        /// with very small files.  In these cases, by default, the DotNetZip library
        /// discards the compressed bytes, and stores the uncompressed file data into the
        /// zip archive.  This is an optimization where smaller size is preferred over
        /// longer run times.
        /// </para>
        ///
        /// <para>
        /// The application can specify that compression is not even tried, by setting the
        /// ForceNoCompression flag.  In this case, the compress-and-check-sizes process as
        /// decribed above, is not done.
        /// </para>
        ///
        /// <para>
        /// In some cases, neither choice is optimal.  The application wants compression,
        /// but in some cases also wants to avoid reading the stream more than once.  This
        /// may happen when the stream is very large, or when the read is very expensive, or
        /// when the difference between the compressed and uncompressed sizes is not
        /// significant.
        /// </para>
        ///
        /// <para>
        /// To satisfy these applications, this delegate allows the DotNetZip library to ask
        /// the application to for approval for re-reading the stream.  As with other
        /// properties (like Password and ForceNoCompression), setting the corresponding
        /// delegate on the <c>ZipFile</c> class itself will set it on all <c>ZipEntry</c> items that are
        /// subsequently added to the <c>ZipFile</c> instance.
        /// </para>
        ///
        /// </remarks>
        /// <seealso cref="Ionic.Zip.ZipFile.WillReadTwiceOnInflation"/>
        /// <seealso cref="Ionic.Zip.ReReadApprovalCallback"/>
        public ReReadApprovalCallback WillReadTwiceOnInflation
        {
            get;
            set;
        }


        /// <summary>
        /// A callback that allows the application to specify whether compression should
        /// be used for a given entry that is about to be added to the zip archive.
        /// </summary>
        ///
        /// <remarks>
        /// See <see cref="ZipFile.WantCompression" />
        /// </remarks>
        public WantCompressionCallback WantCompression
        {
            get;
            set;
        }



        /// <summary>
        /// Set to indicate whether to use UTF-8 encoding for filenames and 
        /// comments.  
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para> If this flag is set, the comment and filename for the entry will be
        /// encoded with UTF-8, as described in <see
        /// href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">the Zip
        /// specification</see>, if necessary. "Necessary" means, the filename or entry
        /// comment (if any) cannot be reflexively encoded and decoded using the default
        /// code page, IBM437.  </para>
        ///
        /// <para>
        /// Setting this flag to true is equivalent to setting
        /// <see cref="ProvisionalAlternateEncoding"/> to <c>System.Text.Encoding.UTF8</c>.
        /// </para>
        ///
        /// <para> This flag has no effect or relation to the text encoding used within the file
        /// itself. </para>
        ///
        /// </remarks>
        public bool UseUnicodeAsNecessary
        {
            get
            {
                return _provisionalAlternateEncoding == System.Text.Encoding.GetEncoding("UTF-8");
            }
            set
            {
                _provisionalAlternateEncoding = (value) ? System.Text.Encoding.GetEncoding("UTF-8") : Ionic.Zip.ZipFile.DefaultEncoding;
            }
        }

        /// <summary>
        /// The text encoding to use for the FileName and Comment on this ZipEntry, when the 
        /// default encoding is insufficient.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para>
        /// This is a long and odd name for a property, but there's a good reason for
        /// it. According to <see
        /// href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">the Zip
        /// specification from PKWare</see>, filenames and comments for a
        /// <c>ZipEntry</c> are encoded either with IBM437 or with UTF8.  But, some zip
        /// archiver tools or libraries do not follow the specification, and instead
        /// encode characters using the system default code page, or an arbitrary code
        /// page.  For example, WinRAR when run on a machine in Shanghai may encode
        /// filenames with the Chinese (Big-5) code page.  This behavior is contrary to
        /// the Zip specification, but it occurs anyway.  This property exists to
        /// support that non-compliant behavior when reading or writing zip files.
        /// </para>
        ///
        /// <para>
        /// When writing zip archives that will be read by one of these other archivers,
        /// use this property to specify the code page to use when encoding filenames
        /// and comments into the zip file, when the IBM437 code page will not suffice.
        /// </para>
        ///
        /// <para>
        /// Be aware that a zip file created after you've explicitly specified the code
        /// page will not be compliant to the PKWare specification, and may not be
        /// readable by compliant archivers.  On the other hand, many archivers are
        /// non-compliant and can read zip files created in arbitrary code pages. For
        /// example, if you run WinRar on your PC desktop in Kyoto, Japan, you will
        /// probably be able to open zip files that we encoded by DotNetZip in the
        /// Shift_JIS code page.
        /// </para>
        ///
        /// <para>
        /// When using an arbitrary, non-UTF8 code page for encoding, there is no
        /// standard way for the creator (DotNetZip) to specify in the zip file which
        /// code page has been used. DotNetZip is not able to inspect the zip file and
        /// determine the codepage used for the entries within it. Therefore, you, the
        /// application author, must determine that.  If you read a zip file using a
        /// codepage other than the one used to encode the zipfile, this may result in
        /// filenames that are not legal in the filesystem, and you will get exceptions
        /// during calls to <c>Extract()</c> methods for those entries.  Caveat Emptor.
        /// </para>
        ///
        /// </remarks>
        public System.Text.Encoding ProvisionalAlternateEncoding
        {
            get
            {
                return _provisionalAlternateEncoding;
            }
            set
            {
                _provisionalAlternateEncoding = value;
            }
        }


        /// <summary>
        /// The text encoding actually used for this ZipEntry.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para> This read-only property describes the encoding used by the
        /// <c>ZipEntry</c>.  If the entry has been read in from an existing ZipFile, then
        /// it may take the value UTF-8, if the entry is coded to specify UTF-8.  If the
        /// entry does not specify UTF-8, the typical case, then the encoding used is
        /// whatever the application specified in the call to <c>ZipFile.Read()</c>. If the
        /// application has used one of the overloads of <c>ZipFile.Read()</c> that does not
        /// accept an encoding parameter, then the encoding used is IBM437, which is the
        /// default encoding described in the ZIP specification.  </para>
        ///
        /// <para> If the entry is being created, then the value of ActualEncoding is taken
        /// according to the logic described in the documentation for <see
        /// cref="ZipFile.ProvisionalAlternateEncoding" />.  </para>
        ///
        /// <para> An application might be interested in retrieving this property to see if
        /// an entry read in from a file has used Unicode (UTF-8).  </para>
        ///
        /// </remarks>
        ///
        /// <seealso cref="ZipFile.ProvisionalAlternateEncoding" />
        public System.Text.Encoding ActualEncoding
        {
            get
            {
                return _actualEncoding;
            }
        }

        private void ReadExtraField()
        {
            // workitem 8098: ok (restore)
            long posn = this.ArchiveStream.Position;

            this._zipfile.SeekFromOrigin(this._RelativeOffsetOfLocalHeader);
            byte[] block = new byte[30];
            int n = this.ArchiveStream.Read(block, 0, block.Length);
            int i = 26;
            Int16 filenameLength = (short)(block[i++] + block[i++] * 256);
            Int16 extraFieldLength = (short)(block[i++] + block[i++] * 256);

            // workitem 8098: ok (relative)
            this.ArchiveStream.Seek(filenameLength, SeekOrigin.Current);

            ProcessExtraField(extraFieldLength);

            // workitem 8098: ok (restore)
            this.ArchiveStream.Seek(posn, SeekOrigin.Begin);
        }


        private static bool ReadHeader(ZipEntry ze, System.Text.Encoding defaultEncoding)
        {
            int bytesRead = 0;

            // change for workitem 8098
            // ze._RelativeOffsetOfLocalHeader = ze.ArchiveStream.Position;
            ze._RelativeOffsetOfLocalHeader = ze._zipfile.RelativeOffset;

            int signature = Ionic.Zip.SharedUtilities.ReadSignature(ze.ArchiveStream);
            bytesRead += 4;

            // Return false if this is not a local file header signature.
            if (ZipEntry.IsNotValidSig(signature))
            {
                // Getting "not a ZipEntry signature" is not always wrong or an error. 
                // This will happen after the last entry in a zipfile.  In that case, we 
                // expect to read : 
                //    a ZipDirEntry signature (if a non-empty zip file) or 
                //    a ZipConstants.EndOfCentralDirectorySignature.  
                //
                // Anything else is a surprise.

                ze.ArchiveStream.Seek(-4, SeekOrigin.Current); // unread the signature
                if (ZipEntry.IsNotValidZipDirEntrySig(signature) && (signature != ZipConstants.EndOfCentralDirectorySignature))
                {
                    throw new BadReadException(String.Format("  ZipEntry::ReadHeader(): Bad signature (0x{0:X8}) at position  0x{1:X8}", signature, ze.ArchiveStream.Position));
                }
                return false;
            }

            byte[] block = new byte[26];
            int n = ze.ArchiveStream.Read(block, 0, block.Length);
            if (n != block.Length) return false;
            bytesRead += n;

            int i = 0;
            ze._VersionNeeded = (short)(block[i++] + block[i++] * 256);
            ze._BitField = (short)(block[i++] + block[i++] * 256);
            ze._CompressionMethod = (short)(block[i++] + block[i++] * 256);
            ze._TimeBlob = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
            // transform the time data into something usable (a DateTime)
            ze._LastModified = Ionic.Zip.SharedUtilities.PackedToDateTime(ze._TimeBlob);
            ze._timestamp |= ZipEntryTimestamp.DOS;

            // NB: if ((ze._BitField & 0x0008) != 0x0008), then the Compressed, uncompressed and 
            // CRC values are not true values; the true values will follow the entry data.  
            // But, regardless of the status of bit 3 in the bitfield, the slots for 
            // the three amigos may contain marker values for ZIP64.  So we must read them.
            {
                ze._Crc32 = (Int32)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
                ze._CompressedSize = (uint)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
                ze._UncompressedSize = (uint)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);

                if ((uint)ze._CompressedSize == 0xFFFFFFFF ||
                    (uint)ze._UncompressedSize == 0xFFFFFFFF)

                    ze._InputUsesZip64 = true;

            }

            Int16 filenameLength = (short)(block[i++] + block[i++] * 256);
            Int16 extraFieldLength = (short)(block[i++] + block[i++] * 256);

            block = new byte[filenameLength];
            n = ze.ArchiveStream.Read(block, 0, block.Length);
            bytesRead += n;

            // if the UTF8 bit is set for this entry, override the encoding the application requested.
            ze._actualEncoding = ((ze._BitField & 0x0800) == 0x0800)
                ? System.Text.Encoding.UTF8
                : defaultEncoding;

            // need to use this form of GetString() for .NET CF
            ze._FileNameInArchive = ze._actualEncoding.GetString(block, 0, block.Length);

            // when creating an entry by reading, the LocalFileName is the same as the FileNameInArchive
            ze._LocalFileName = ze._FileNameInArchive;

            // workitem 6898
            if (ze._LocalFileName.EndsWith("/")) ze.MarkAsDirectory();

            bytesRead += ze.ProcessExtraField(extraFieldLength);

            ze._LengthOfTrailer = 0;

            // workitem 6607 - don't read for directories
            // actually get the compressed size and CRC if necessary
            if (!ze._LocalFileName.EndsWith("/") && (ze._BitField & 0x0008) == 0x0008)
            {
                // This descriptor exists only if bit 3 of the general
                // purpose bit flag is set (see below).  It is byte aligned
                // and immediately follows the last byte of compressed data,
                // as well as any encryption trailer, as with AES.
                // This descriptor is used only when it was not possible to
                // seek in the output .ZIP file, e.g., when the output .ZIP file
                // was standard output or a non-seekable device.  For ZIP64(tm) format
                // archives, the compressed and uncompressed sizes are 8 bytes each.

                // workitem 8098: ok (restore)
                long posn = ze.ArchiveStream.Position;

                // Here, we're going to loop until we find a ZipEntryDataDescriptorSignature and 
                // a consistent data record after that.   To be consistent, the data record must 
                // indicate the length of the entry data. 
                bool wantMore = true;
                long SizeOfDataRead = 0;
                int tries = 0;
                while (wantMore)
                {
                    tries++;
                    // We call the FindSignature shared routine to find the specified signature
                    // in the already-opened zip archive, starting from the current cursor
                    // position in that filestream.  If we cannot find the signature, then the
                    // routine returns -1, and the ReadHeader() method returns false,
                    // indicating we cannot read a legal entry header.  If we have found it,
                    // then the FindSignature() method returns the number of bytes in the
                    // stream we had to seek forward, to find the sig.  We need this to
                    // determine if the zip entry is valid, later.

                    ze._zipfile.OnReadBytes(ze);

                    long d = Ionic.Zip.SharedUtilities.FindSignature(ze.ArchiveStream, ZipConstants.ZipEntryDataDescriptorSignature);
                    if (d == -1) return false;

                    // total size of data read (through all loops of this). 
                    SizeOfDataRead += d;

                    if (ze._InputUsesZip64)
                    {
                        // read 1x 4-byte (CRC) and 2x 8-bytes (Compressed Size, Uncompressed Size)
                        block = new byte[20];
                        n = ze.ArchiveStream.Read(block, 0, block.Length);
                        if (n != 20) return false;

                        // do not increment bytesRead - it is for entry header only.
                        // the data we have just read is a footer (falls after the file data)
                        //bytesRead += n; 

                        i = 0;
                        ze._Crc32 = (Int32)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
                        ze._CompressedSize = BitConverter.ToInt64(block, i);
                        i += 8;
                        ze._UncompressedSize = BitConverter.ToInt64(block, i);
                        i += 8;

                        ze._LengthOfTrailer += 24;  // bytes including sig, CRC, Comp and Uncomp sizes
                    }
                    else
                    {
                        // read 3x 4-byte fields (CRC, Compressed Size, Uncompressed Size)
                        block = new byte[12];
                        n = ze.ArchiveStream.Read(block, 0, block.Length);
                        if (n != 12) return false;

                        // do not increment bytesRead - it is for entry header only.
                        // the data we have just read is a footer (falls after the file data)
                        //bytesRead += n; 

                        i = 0;
                        ze._Crc32 = (Int32)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
                        ze._CompressedSize = (uint)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
                        ze._UncompressedSize = (uint)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);

                        ze._LengthOfTrailer += 16;  // bytes including sig, CRC, Comp and Uncomp sizes

                    }

                    wantMore = (SizeOfDataRead != ze._CompressedSize);

                    if (wantMore)
                    {
                        // Seek back to un-read the last 12 bytes  - maybe THEY contain 
                        // the ZipEntryDataDescriptorSignature.
                        // (12 bytes for the CRC, Comp and Uncomp size.)
                        ze.ArchiveStream.Seek(-12, SeekOrigin.Current);

                        // Adjust the size to account for the false signature read in 
                        // FindSignature().
                        SizeOfDataRead += 4;
                    }
                }

                // seek back to previous position, to prepare to read file data
                // workitem 8098: ok (restore)
                ze.ArchiveStream.Seek(posn, SeekOrigin.Begin);
            }

            ze._CompressedFileDataSize = ze._CompressedSize;


            // bit 0 set indicates that some kind of encryption is in use
            if ((ze._BitField & 0x01) == 0x01)
            {
#if AESCRYPTO
                if (ze.Encryption == EncryptionAlgorithm.WinZipAes128 ||
                    ze.Encryption == EncryptionAlgorithm.WinZipAes256)
                {
                    // read in the WinZip AES metadata: salt + PV. 18 bytes for AES256. 10 bytes for AES128.
                    ze._aesCrypto = WinZipAesCrypto.ReadFromStream(null, ze._KeyStrengthInBits, ze.ArchiveStream);
                    bytesRead += ze._aesCrypto.SizeOfEncryptionMetadata - 10; // MAC (follows crypto bytes)
                    // according to WinZip, the CompressedSize includes the AES Crypto framing data. 
                    ze._CompressedFileDataSize -= ze._aesCrypto.SizeOfEncryptionMetadata;
                    ze._LengthOfTrailer += 10;  // MAC
                }
                else
#endif
                {
                    // read in the header data for "weak" encryption
                    ze._WeakEncryptionHeader = new byte[12];
                    bytesRead += ZipEntry.ReadWeakEncryptionHeader(ze._archiveStream, ze._WeakEncryptionHeader);
                    // decrease the filedata size by 12 bytes
                    ze._CompressedFileDataSize -= 12;
                }
            }

            // Remember the size of the blob for this entry. 
            // We also have the starting position in the stream for this entry. 
            ze._LengthOfHeader = bytesRead;
            ze._TotalEntrySize = ze._LengthOfHeader + ze._CompressedFileDataSize + ze._LengthOfTrailer;


            // We've read in the regular entry header, the extra field, and any encryption
            // header.  The pointer in the file is now at the start of the filedata, which is
            // potentially compressed and encrypted.  Just ahead in the file, there are
            // _CompressedFileDataSize bytes of data, followed by potentially a non-zero length
            // trailer, consisting of optionally, some encryption stuff (10 byte MAC for AES),
            // and the bit-3 trailer (16 or 24 bytes).

            return true;
        }



        internal static int ReadWeakEncryptionHeader(Stream s, byte[] buffer)
        {
            // PKZIP encrypts the compressed data stream.  Encrypted files must
            // be decrypted before they can be extracted.

            // Each PKZIP-encrypted file has an extra 12 bytes stored at the start of the data
            // area defining the encryption header for that file.  The encryption header is
            // originally set to random values, and then itself encrypted, using three, 32-bit
            // keys.  The key values are initialized using the supplied encryption password.
            // After each byte is encrypted, the keys are then updated using pseudo-random
            // number generation techniques in combination with the same CRC-32 algorithm used
            // in PKZIP and implemented in the CRC32.cs module in this project.

            // read the 12-byte encryption header
            int additionalBytesRead = s.Read(buffer, 0, 12);
            if (additionalBytesRead != 12)
                throw new ZipException(String.Format("Unexpected end of data at position 0x{0:X8}", s.Position));

            return additionalBytesRead;
        }



        private static bool IsNotValidSig(int signature)
        {
            return (signature != ZipConstants.ZipEntrySignature);
        }


        /// <summary>
        /// Reads one <c>ZipEntry</c> from the given stream.  If the entry is encrypted, we don't
        /// decrypt at this point.  We also do not decompress.  Mostly we read metadata.
        /// </summary>
        /// <param name="zf">the zipfile this entry belongs to.</param>
        /// <param name="first">true of this is the first entry being read from the stream.</param>
        /// <returns>the <c>ZipEntry</c> read from the stream.</returns>
        internal static ZipEntry Read(ZipFile zf, bool first)
        {
            Stream s = zf.ReadStream;

            System.Text.Encoding defaultEncoding = zf.ProvisionalAlternateEncoding;
            ZipEntry entry = new ZipEntry();
            entry._Source = ZipEntrySource.Zipfile;
            entry._zipfile = zf;
            entry._archiveStream = s;
            zf.OnReadEntry(true, null);

            if (first) HandlePK00Prefix(s);

            // Read entry header, including any encryption header
            if (!ReadHeader(entry, defaultEncoding)) return null;

            // Store the position in the stream for this entry
            // change for workitem 8098
            //entry.__FileDataPosition = entry.ArchiveStream.Position;
            entry.__FileDataPosition = entry._zipfile.RelativeOffset;

            // seek past the data without reading it. We will read on Extract()            
            s.Seek(entry._CompressedFileDataSize + entry._LengthOfTrailer, SeekOrigin.Current);

            // ReadHeader moves the file pointer to the end of the entry header,
            // as well as any encryption header. 

            // CompressedFileDataSize includes:
            //   the maybe compressed, maybe encrypted file data
            //   the encryption trailer, if any
            //   the bit 3 descriptor, if any 

            // workitem 5306
            // http://www.codeplex.com/DotNetZip/WorkItem/View.aspx?WorkItemId=5306
            HandleUnexpectedDataDescriptor(entry);

            zf.OnReadBytes(entry);
            zf.OnReadEntry(false, entry);

            return entry;
        }


        internal static void HandlePK00Prefix(Stream s)
        {
            // in some cases, the zip file begins with "PK00".  This is a throwback and is rare,
            // but we handle it anyway. We do not change behavior based on it.
            uint datum = (uint)Ionic.Zip.SharedUtilities.ReadInt(s);
            if (datum != ZipConstants.PackedToRemovableMedia)
            {
                s.Seek(-4, SeekOrigin.Current); // unread the block
            }
        }



        private static void HandleUnexpectedDataDescriptor(ZipEntry entry)
        {
            Stream s = entry.ArchiveStream;

            // In some cases, the "data descriptor" is present, without a signature, even when
            // bit 3 of the BitField is NOT SET.  This is the CRC, followed
            //    by the compressed length and the uncompressed length (4 bytes for each 
            //    of those three elements).  Need to check that here.             
            //
            uint datum = (uint)Ionic.Zip.SharedUtilities.ReadInt(s);
            if (datum == entry._Crc32)
            {
                int sz = Ionic.Zip.SharedUtilities.ReadInt(s);
                if (sz == entry._CompressedSize)
                {
                    sz = Ionic.Zip.SharedUtilities.ReadInt(s);
                    if (sz == entry._UncompressedSize)
                    {
                        // ignore everything and discard it.
                    }
                    else
                        s.Seek(-12, SeekOrigin.Current); // unread the three blocks
                }
                else
                    s.Seek(-8, SeekOrigin.Current); // unread the two blocks
            }
            else
                s.Seek(-4, SeekOrigin.Current); // unread the block

        }





        internal static string NameInArchive(String filename, string directoryPathInArchive)
        {
            string result = null;
            if (directoryPathInArchive == null)
                result = filename;

            else
            {
                if (String.IsNullOrEmpty(directoryPathInArchive))
                {
                    //if (filename.EndsWith("\\"))
                    //{
                    //    result = Path.GetFileName(filename.Substring(0, filename.Length - 1));
                    //}
                    //else
                    result = Path.GetFileName(filename);
                }
                else
                {
                    // explicitly specify a pathname for this file  
                    result = Path.Combine(directoryPathInArchive, Path.GetFileName(filename));
                }

            }
            result = SharedUtilities.TrimVolumeAndSwapSlashes(result);
            result = SharedUtilities.NormalizeFwdSlashPath(result);

            while (result.StartsWith("/")) result = result.Substring(1);

            return result;
        }



        internal static ZipEntry Create(String filename, string nameInArchive)
        {
            return Create(filename, nameInArchive, false, null);
        }


        internal static ZipEntry Create(String filename, string nameInArchive, bool isStream, Stream stream)
        {
            if (String.IsNullOrEmpty(filename))
                throw new Ionic.Zip.ZipException("The entry name must be non-null and non-empty.");

            ZipEntry entry = new ZipEntry();

            // workitem 7071
            // workitem 7926 - "version made by" OS should be zero for compat with WinZip
            entry._VersionMadeBy = (0 << 8) + 45; // indicates the attributes are FAT Attributes, and v4.5 of the spec

            // workitem 7192 - late bound streams
            if (isStream)
            {
                entry._Source = ZipEntrySource.Stream;
                entry._sourceStream = stream; // may  or may not be null
                entry._Mtime = entry._Atime = entry._Ctime = DateTime.UtcNow;
            }
            else
            {
                // The named file may or may not exist at this time.  For example, when 
                // adding a directory by name.  We test existence when necessary:
                // when saving the ZipFile, or when getting the attributes, and so on. 

                entry._Source = ZipEntrySource.Filesystem;

#if NETCF
                // workitem 6878
                // Ionic.Zip.SharedUtilities.AdjustTime_Win32ToDotNet
                entry._Mtime = Ionic.Zip.SharedUtilities.AdjustTime_Win32ToDotNet(File.GetLastWriteTime(filename));
                entry._Ctime = Ionic.Zip.SharedUtilities.AdjustTime_Win32ToDotNet(File.GetCreationTime(filename));
                entry._Atime = Ionic.Zip.SharedUtilities.AdjustTime_Win32ToDotNet(File.GetLastAccessTime(filename));

                // workitem 7071
                // can only get attributes of files that exist.
                if (File.Exists(filename) || Directory.Exists(filename))
                    entry._ExternalFileAttrs = (int)NetCfFile.GetAttributes(filename);
                
#else
                // workitem 6878
                // Ionic.Zip.SharedUtilities.AdjustTime_Win32ToDotNet
                entry._Mtime = File.GetLastWriteTimeUtc(filename);
                entry._Ctime = File.GetCreationTimeUtc(filename);
                entry._Atime = File.GetLastAccessTimeUtc(filename);

                // workitem 7071
                // can only get attributes on files that exist.
                if (File.Exists(filename) || Directory.Exists(filename))
                    entry._ExternalFileAttrs = (int)File.GetAttributes(filename);
                // else ??

#endif
            }

            entry._ntfsTimesAreSet = true;

            entry._LastModified = entry._Mtime;
            entry._LocalFileName = filename; // may include a path
            entry._FileNameInArchive = nameInArchive.Replace('\\', '/');

            // We don't actually slurp in the file data until the caller invokes Write on this entry.

            return entry;
        }




        #region Extract methods
        /// <summary>
        /// Extract the entry to the filesystem, starting at the current working directory. 
        /// </summary>
        /// 
        /// <overloads>
        /// This method has a bunch of overloads! One of them is sure to be
        /// the right one for you... If you don't like these, check out the 
        /// <c>ExtractWithPassword()</c> methods.
        /// </overloads>
        ///
        /// <seealso cref="Ionic.Zip.ZipEntry.ExtractExistingFile"/>
        /// <seealso cref="ZipEntry.Extract(ExtractExistingFileAction)"/>
        ///
        /// <remarks>
        ///
        /// <para> This method extracts an entry from a zip file into the current working
        /// directory.  The path of the entry as extracted is the full path as specified in
        /// the zip archive, relative to the current working directory.  After the file is
        /// extracted successfully, the file attributes and timestamps are set.  </para>
        ///
        /// <para>
        /// The action taken when extraction an entry would overwrite an existing file
        /// is determined by the <see cref="ExtractExistingFile" /> property. 
        /// </para>
        ///
        /// <para>
        /// See the remarks the <see cref="LastModified"/> property, for some details 
        /// about how the last modified time of the file is set after extraction.
        /// </para>
        ///
        /// </remarks>
        public void Extract()
        {
            InternalExtract(".", null, null);
        }

        /// <summary>
        /// Extract the entry to a file in the filesystem, potentially overwriting
        /// any existing file. This method is Obsolete.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is Obsolete, please don't use it.  Please use method <see
        /// cref="Extract(ExtractExistingFileAction)"/> instead.
        /// </para>
        /// <para>
        /// See the remarks on the <see cref="LastModified"/> property, for some details 
        /// about how the last modified time of the created file is set after extraction.
        /// </para>
        /// </remarks>
        /// <param name="overwrite">
        /// true if the caller wants to overwrite an existing bfile 
        /// by the same name in the filesystem.
        /// </param>
        /// <seealso cref="Extract(ExtractExistingFileAction)"/>
        [Obsolete("Please use method Extract(ExtractExistingFileAction)")]
        public void Extract(bool overwrite)
        {
            OverwriteOnExtract = overwrite;
            InternalExtract(".", null, null);
        }

        /// <summary>
        /// Extract the entry to a file in the filesystem, using the specified behavior 
        /// when extraction would overwrite an existing file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// See the remarks on the <see cref="LastModified"/> property, for some details 
        /// about how the last modified time of the file is set after extraction.
        /// </para>
        /// </remarks>
        /// <param name="extractExistingFile">The action to take if extraction would 
        /// overwrite an existing file.</param>
        public void Extract(ExtractExistingFileAction extractExistingFile)
        {
            ExtractExistingFile = extractExistingFile;
            InternalExtract(".", null, null);
        }

        /// <summary>
        /// Extracts the entry to the specified stream. 
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para> For example, the caller could specify <c>Console.Out</c>, or a
        /// <c>MemoryStream</c>, or ASP.NET's <c>Response.OutputStream</c>.  The content
        /// will be decrypted and decompressed as necessary. If the entry is encrypted and
        /// no password is provided, this method will throw.  </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="stream">the stream to which the entry should be extracted.  </param>
        /// 
        public void Extract(Stream stream)
        {
            InternalExtract(null, stream, null);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory. 
        /// </summary>
        /// 
        /// <param name="baseDirectory">the pathname of the base directory</param>
        /// 
        /// <seealso cref="Ionic.Zip.ZipEntry.ExtractExistingFile"/>
        /// <seealso cref="Ionic.Zip.ZipEntry.Extract(string, ExtractExistingFileAction)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.Extract(string)"/>
        /// 
        /// <example>
        /// This example extracts only the entries in a zip file that are .txt files, 
        /// into a directory called "textfiles".
        /// <code lang="C#">
        /// using (ZipFile zip = ZipFile.Read("PackedDocuments.zip"))
        /// {
        ///   foreach (string s1 in zip.EntryFilenames)
        ///   {
        ///     if (s1.EndsWith(".txt")) 
        ///     {
        ///       zip[s1].Extract("textfiles");
        ///     }
        ///   }
        /// }
        /// </code>
        /// <code lang="VB">
        ///   Using zip As ZipFile = ZipFile.Read("PackedDocuments.zip")
        ///       Dim s1 As String
        ///       For Each s1 In zip.EntryFilenames
        ///           If s1.EndsWith(".txt") Then
        ///               zip(s1).Extract("textfiles")
        ///           End If
        ///       Next
        ///   End Using
        /// </code>
        /// </example>
        /// 
        /// <remarks>
        ///
        /// <para> Using this method, existing entries in the filesystem will not be
        /// overwritten. If you would like to force the overwrite of existing files, see the
        /// <see cref="ExtractExistingFile"/> property, or call <see cref="Extract(string,
        /// ExtractExistingFileAction)"/>. </para>
        ///
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        public void Extract(string baseDirectory)
        {
            InternalExtract(baseDirectory, null, null);
        }

        
        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory, 
        /// and potentially overwriting existing files in the filesystem. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="baseDirectory">the pathname of the base directory</param>
        /// <param name="overwrite">If true, overwrite any existing files if necessary
        /// upon extraction.</param>
        ///
        /// <seealso cref="Extract(String,ExtractExistingFileAction)"/>
        [Obsolete("Please use method Extract(String,ExtractExistingFileAction)")]
        public void Extract(string baseDirectory, bool overwrite)
        {
            OverwriteOnExtract = overwrite;
            InternalExtract(baseDirectory, null, null);
        }


        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory, and
        /// using the specified behavior when extraction would overwrite an existing file.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the remarks on the <see cref="LastModified"/> property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        ///
        /// <example>
        /// <code lang="C#">
        /// String sZipPath = "Airborne.zip";
        /// String sFilePath = "Readme.txt";
        /// String sRootFolder = "Digado";
        /// using (ZipFile zip = ZipFile.Read(sZipPath))
        /// {
        ///   if (zip.EntryFileNames.Contains(sFilePath))
        ///   {
        ///     // use the string indexer on the zip file
        ///     zip[sFileName].Extract(sRootFolder,
        ///                            ExtractExistingFileAction.OverwriteSilently);
        ///   }
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim sZipPath as String = "Airborne.zip"
        /// Dim sFilePath As String = "Readme.txt"
        /// Dim sRootFolder As String = "Digado"
        /// Using zip As ZipFile = ZipFile.Read(sZipPath)
        ///   If zip.EntryFileNames.Contains(sFilePath)
        ///     ' use the string indexer on the zip file
        ///     zip(sFilePath).Extract(sRootFolder, _
        ///                            ExtractExistingFileAction.OverwriteSilently)
        ///   End If
        /// End Using
        /// </code>
        /// </example>
        ///
        /// <param name="baseDirectory">the pathname of the base directory</param>
        /// <param name="extractExistingFile">
        /// The action to take if extraction would overwrite an existing file.
        /// </param>
        public void Extract(string baseDirectory, ExtractExistingFileAction extractExistingFile)
        {
            ExtractExistingFile = extractExistingFile;
            InternalExtract(baseDirectory, null, null);
        }


        /// <summary>
        /// Extract the entry to the filesystem, using the current working directory
        /// and the specified password. 
        /// </summary>
        ///
        /// <overloads>
        /// This method has a bunch of overloads! One of them is sure to be
        /// the right one for you...
        /// </overloads>
        ///         
        /// <seealso cref="Ionic.Zip.ZipEntry.ExtractExistingFile"/>
        /// <seealso cref="Ionic.Zip.ZipEntry.ExtractWithPassword(ExtractExistingFileAction, string)"/>
        ///
        /// <remarks>
        ///
        /// <para> Existing entries in the filesystem will not be overwritten. If you would
        /// like to force the overwrite of existing files, see the <see
        /// cref="Ionic.Zip.ZipEntry.ExtractExistingFile"/>property, or call <see
        /// cref="ExtractWithPassword(ExtractExistingFileAction,string)"/>.</para>
        ///
        /// <para>
        /// See the remarks on the <see cref="LastModified"/> property for some details 
        /// about how the "last modified" time of the created file is set.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// In this example, entries that use encryption are extracted using a particular password.
        /// <code>
        /// using (var zip = ZipFile.Read(FilePath))
        /// {
        ///     foreach (ZipEntry e in zip)
        ///     {
        ///         if (e.UsesEncryption)
        ///             e.ExtractWithPassword("Secret!");
        ///         else
        ///             e.Extract();
        ///     }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = ZipFile.Read(FilePath)
        ///     Dim e As ZipEntry
        ///     For Each e In zip
        ///         If (e.UsesEncryption)
        ///           e.ExtractWithPassword("Secret!")
        ///         Else
        ///           e.Extract
        ///         End If 
        ///     Next
        /// End Using
        /// </code>
        /// </example>
        /// <param name="password">The Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(string password)
        {
            InternalExtract(".", null, password);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory,
        /// and using the specified password. 
        /// </summary>
        /// 
        /// <seealso cref="Ionic.Zip.ZipEntry.ExtractExistingFile"/>
        /// <seealso cref="Ionic.Zip.ZipEntry.ExtractWithPassword(string, ExtractExistingFileAction, string)"/>
        ///
        /// <remarks>
        /// <para> Existing entries in the filesystem will not be overwritten. If you would
        /// like to force the overwrite of existing files, see the <see
        /// cref="Ionic.Zip.ZipEntry.ExtractExistingFile"/>property, or call <see
        /// cref="ExtractWithPassword(ExtractExistingFileAction,string)"/>.</para>
        ///
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="baseDirectory">The pathname of the base directory.</param>
        /// <param name="password">The Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(string baseDirectory, string password)
        {
            InternalExtract(baseDirectory, null, password);
        }

        /// <summary>
        /// Extract the entry to a file in the filesystem, potentially overwriting
        /// any existing file.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="overwrite">true if the caller wants to overwrite an existing 
        /// file by the same name in the filesystem.</param>
        /// <param name="password">The Password to use for decrypting the entry.</param>
        /// <seealso cref="ExtractWithPassword(ExtractExistingFileAction,String)"/>
        [Obsolete("Please use method ExtractWithPassword(ExtractExistingFileAction,String)")]
        public void ExtractWithPassword(bool overwrite, string password)
        {
            OverwriteOnExtract = overwrite;
            InternalExtract(".", null, password);
        }


        /// <summary>
        /// Extract the entry to a file in the filesystem, relative to the current directory,
        /// using the specified behavior when extraction would overwrite an existing file.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="password">The Password to use for decrypting the entry.</param>
        /// 
        /// <param name="extractExistingFile">
        /// The action to take if extraction would overwrite an existing file.
        /// </param>
        public void ExtractWithPassword(ExtractExistingFileAction extractExistingFile, string password)
        {
            ExtractExistingFile = extractExistingFile;
            InternalExtract(".", null, password);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory, 
        /// and potentially overwriting existing files in the filesystem. 
        /// </summary>
        /// 
        /// <remarks>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </remarks>
        ///
        /// <param name="baseDirectory">the pathname of the base directory</param>
        ///
        /// <param name="overwrite">If true, overwrite any existing files if necessary
        /// upon extraction.</param>
        ///
        /// <param name="password">The Password to use for decrypting the entry.</param>
        ///
        /// <seealso cref="ExtractWithPassword(String,ExtractExistingFileAction,String)"/>
        [Obsolete("Please use method ExtractWithPassword(String,ExtractExistingFileAction,String)")]
        public void ExtractWithPassword(string baseDirectory, bool overwrite, string password)
        {
            OverwriteOnExtract = overwrite;
            InternalExtract(baseDirectory, null, password);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory, and
        /// using the specified behavior when extraction would overwrite an existing file.
        /// </summary>
        /// 
        /// <remarks>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </remarks>
        ///
        /// <param name="baseDirectory">the pathname of the base directory</param>
        ///
        /// <param name="extractExistingFile">The action to take if extraction would
        /// overwrite an existing file.</param>
        ///
        /// <param name="password">The Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(string baseDirectory, ExtractExistingFileAction extractExistingFile, string password)
        {
            ExtractExistingFile = extractExistingFile;
            InternalExtract(baseDirectory, null, password);
        }

        /// <summary>
        /// Extracts the entry to the specified stream, using the specified Password.
        /// For example, the caller could extract to Console.Out, or to a MemoryStream.
        /// </summary>
        /// 
        /// <remarks>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </remarks>
        /// 
        /// <param name="stream">the stream to which the entry should be extracted.  </param>
        /// <param name="password">The password to use for decrypting the entry.</param>
        public void ExtractWithPassword(Stream stream, string password)
        {
            InternalExtract(null, stream, password);
        }


        /// <summary>
        /// Opens the backing stream for the zip entry in the archive, for reading. 
        /// </summary>
        /// 
        /// <remarks>
        ///
        /// <para>
        /// DotNetZip offers a variety of ways to extract entries from a zip file.  This
        /// method allows an application to extract and entry by reading a Stream. 
        /// </para>
        ///
        /// <para>
        /// The return value is a <see cref="Ionic.Zlib.CrcCalculatorStream"/>.  Use it
        /// as you would any stream for reading.  The data you get by calling <see
        /// cref="Stream.Read(byte[], int, int)"/> on that stream will be decrypted and
        /// decompressed.
        /// </para>
        /// 
        /// <para>
        /// CrcCalculatorStream adds one additional feature: it keeps a CRC32 checksum
        /// on the bytes of the stream as it is read.  The CRC value is available in the
        /// <see cref="Ionic.Zlib.CrcCalculatorStream.Crc32"/> property on the
        /// <c>CrcCalculatorStream</c>.  When the read is complete, this CRC
        /// <em>should</em> be checked against the <see cref="ZipEntry.Crc32"/> property
        /// on the <c>ZipEntry</c> to validate the content of the ZipEntry.  You don't
        /// have to validate the entry using the CRC, but you should. Check the example
        /// for how to do this.
        /// </para>
        /// 
        /// <para>
        /// If the entry is protected with a password, then you need to provide a
        /// password prior to calling <see cref="OpenReader()"/>, either by setting the
        /// <see cref="Password"/> property on the entry, or the <see
        /// cref="ZipFile.Password"/> property on the <c>ZipFile</c> itself. Or, you can
        /// use <see cref="OpenReader(String)" />, the overload of OpenReader that
        /// accepts a password parameter.
        /// </para>
        /// 
        /// <para>
        /// If you want to extract entry data into a stream that is already opened, like
        /// a <see cref="System.IO.FileStream"/>, consider the <see
        /// cref="Extract(Stream)"/> method.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// This example shows how to open a zip archive, then read in a named entry via
        /// a stream.  After the read loop is complete, the code compares the calculated
        /// during the read loop with the expected CRC on the <c>ZipEntry</c>, to verify
        /// the extraction.
        /// <code>
        /// using (ZipFile zip = new ZipFile(ZipFileToRead))
        /// {
        ///   ZipEntry e1= zip["Elevation.mp3"];
        ///   using (Ionic.Zlib.CrcCalculatorStream s = e1.OpenReader())
        ///   {
        ///     byte[] buffer = new byte[4096];
        ///     int n, totalBytesRead= 0;
        ///     do {
        ///       n = s.Read(buffer,0, buffer.Length);
        ///       totalBytesRead+=n; 
        ///     } while (n&gt;0);
        ///      if (s.Crc32 != e1.Crc32)
        ///       throw new Exception(string.Format("The Zip Entry failed the CRC Check. (0x{0:X8}!=0x{1:X8})", s.Crc32, e1.Crc32));
        ///      if (totalBytesRead != e1.UncompressedSize)
        ///       throw new Exception(string.Format("We read an unexpected number of bytes. ({0}!={1})", totalBytesRead, e1.UncompressedSize));
        ///   }
        /// }
        /// </code>
        /// <code lang="VB">
        ///   Using zip As New ZipFile(ZipFileToRead)
        ///       Dim e1 As ZipEntry = zip.Item("Elevation.mp3")
        ///       Using s As Ionic.Zlib.CrcCalculatorStream = e1.OpenReader
        ///           Dim n As Integer
        ///           Dim buffer As Byte() = New Byte(4096) {}
        ///           Dim totalBytesRead As Integer = 0
        ///           Do
        ///               n = s.Read(buffer, 0, buffer.Length)
        ///               totalBytesRead = (totalBytesRead + n)
        ///           Loop While (n &gt; 0)
        ///           If (s.Crc32 &lt;&gt; e1.Crc32) Then
        ///               Throw New Exception(String.Format("The Zip Entry failed the CRC Check. (0x{0:X8}!=0x{1:X8})", s.Crc32, e1.Crc32))
        ///           End If
        ///           If (totalBytesRead &lt;&gt; e1.UncompressedSize) Then
        ///               Throw New Exception(String.Format("We read an unexpected number of bytes. ({0}!={1})", totalBytesRead, e1.UncompressedSize))
        ///           End If
        ///       End Using
        ///   End Using
        /// </code>
        /// </example>
        /// <seealso cref="Ionic.Zip.ZipEntry.Extract(System.IO.Stream)"/>
        /// <returns>The Stream for reading.</returns>
        public Ionic.Zlib.CrcCalculatorStream OpenReader()
        {
            // use the entry password if it is non-null, else use the zipfile password, which is possibly null
            return InternalOpenReader(this._Password ?? this._zipfile._Password);
        }

        /// <summary>
        /// Opens the backing stream for an encrypted zip entry in the archive, for reading. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the documentation on the <see cref="OpenReader()"/> method for full
        /// details.  This overload allows the application to specify a password for the
        /// <c>ZipEntry</c> to be read.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="password">The password to use for decrypting the entry.</param>
        /// <returns>The Stream for reading.</returns>
        public Ionic.Zlib.CrcCalculatorStream OpenReader(string password)
        {
            return InternalOpenReader(password);
        }



        private Ionic.Zlib.CrcCalculatorStream InternalOpenReader(string password)
        {
            ValidateCompression();
            ValidateEncryption();
            SetupCrypto(password);

            // workitem 7958
            if (this._Source != ZipEntrySource.Zipfile)
                throw new BadStateException("You must call ZipFile.Save before calling OpenReader.");

            Stream input = this.ArchiveStream;

            // change for workitem 8098
            // this.ArchiveStream.Seek(this.FileDataPosition, SeekOrigin.Begin);
            this._zipfile.SeekFromOrigin(this.FileDataPosition);

            // get a stream that either decrypts or not.
            Stream input2 = input;
            if (Encryption == EncryptionAlgorithm.PkzipWeak)
                input2 = new ZipCipherStream(input, _zipCrypto, CryptoMode.Decrypt);

#if AESCRYPTO
            else if (Encryption == EncryptionAlgorithm.WinZipAes128 ||
                     Encryption == EncryptionAlgorithm.WinZipAes256)
            {
                input2 = new WinZipAesCipherStream(input, _aesCrypto, _CompressedFileDataSize, CryptoMode.Decrypt);
            }
#endif
            return new Ionic.Zlib.CrcCalculatorStream((CompressionMethod == 0x08)
                                                      ? new Ionic.Zlib.DeflateStream(input2, Ionic.Zlib.CompressionMode.Decompress, true)
                                                      : input2,
                                                      _UncompressedSize);
        }


        internal Stream ArchiveStream
        {
            get
            {
                if (_archiveStream == null)
                {
                    if (_zipfile != null)
                    {
                        _zipfile.Reset();
                        _archiveStream = _zipfile.ReadStream;
                    }
                }
                return _archiveStream;
            }
        }
        #endregion



        private void OnExtractProgress(Int64 bytesWritten, Int64 totalBytesToWrite)
        {
            _ioOperationCanceled = _zipfile.OnExtractBlock(this, bytesWritten, totalBytesToWrite);
        }

        private void OnBeforeExtract(string path)
        {
            // When in the context of a ZipFile.ExtractAll, the events are generated from 
            // the ZipFile method, not from within the ZipEntry instance. (why?)
            // Therefore we suppress the events originating from the ZipEntry method.
            if (!_zipfile._inExtractAll)
            {
                _ioOperationCanceled = _zipfile.OnSingleEntryExtract(this, path, true);
            }
        }

        private void OnAfterExtract(string path)
        {
            // When in the context of a ZipFile.ExtractAll, the events are generated from 
            // the ZipFile method, not from within the ZipEntry instance. (why?)
            // Therefore we suppress the events originating from the ZipEntry method.
            if (!_zipfile._inExtractAll)
            {
                _zipfile.OnSingleEntryExtract(this, path, false);
            }
        }

        private void OnExtractExisting(string path)
        {
            _ioOperationCanceled = _zipfile.OnExtractExisting(this, path);
        }

        private void OnWriteBlock(Int64 bytesXferred, Int64 totalBytesToXfer)
        {
            _ioOperationCanceled = _zipfile.OnSaveBlock(this, bytesXferred, totalBytesToXfer);
        }

        private void ReallyDelete(string fileName)
        {
            // workitem 7881
            // reset ReadOnly bit if necessary
#if NETCF
            if ( (NetCfFile.GetAttributes(fileName) & (uint)FileAttributes.ReadOnly) == (uint)FileAttributes.ReadOnly)
                NetCfFile.SetAttributes(fileName, (uint)FileAttributes.Normal);
#else
            if ((File.GetAttributes(fileName) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                File.SetAttributes(fileName, FileAttributes.Normal);
#endif
            File.Delete(fileName);
        }

        // Pass in either basedir or s, but not both. 
        // In other words, you can extract to a stream or to a directory (filesystem), but not both!
        // The Password param is required for encrypted entries.
        private void InternalExtract(string baseDir, Stream outstream, string password)
        {
            // workitem 7958
            if (_zipfile == null)
                throw new BadStateException("This ZipEntry is an orphan.");

            _zipfile.Reset();
            if (this._Source != ZipEntrySource.Zipfile)
                throw new BadStateException("You must call ZipFile.Save before calling any Extract method.");

            OnBeforeExtract(baseDir);
            _ioOperationCanceled = false;
            string TargetFile = null;
            Stream output = null;
            bool fileExistsBeforeExtraction = false;
            bool checkLaterForResetDirTimes = false;
            try
            {
                ValidateCompression();
                ValidateEncryption();

                if (ValidateOutput(baseDir, outstream, out TargetFile))
                {
                    if (_zipfile.Verbose) _zipfile.StatusMessageTextWriter.WriteLine("extract dir {0}...", TargetFile);
                    // if true, then the entry was a directory and has been created.
                    // We need to fire the Extract Event.
                    OnAfterExtract(baseDir);
                    return;
                }

                // if no password explicitly specified, use the password on the entry itself,
                // or on the zipfile itself.
                string p = password ?? this._Password ?? this._zipfile._Password;
                if (UsesEncryption)
                {
                    if (p == null)
                        throw new BadPasswordException();
                    SetupCrypto(p);
                }

                // set up the output stream
                if (TargetFile != null)
                {
                    if (_zipfile.Verbose) _zipfile.StatusMessageTextWriter.WriteLine("extract file {0}...", TargetFile);
                    // ensure the target path exists
                    if (!Directory.Exists(Path.GetDirectoryName(TargetFile)))
                    {
                        // we create the directory here, but we do not set the
                        // create/modified/accessed times on it because it is being
                        // created implicitly, not explcitly. There's no entry in the
                        // zip archive for the directory. 
                        Directory.CreateDirectory(Path.GetDirectoryName(TargetFile));
                    }
                    else
                        checkLaterForResetDirTimes = true;  // workitem 8264


                    // Take care of the behavior when extraction would overwrite an existing file
                    if (File.Exists(TargetFile))
                    {
                        fileExistsBeforeExtraction = true;
                        switch (ExtractExistingFile)
                        {
                            case ExtractExistingFileAction.Throw:
                                throw new ZipException(String.Format("The file {0} already exists.", TargetFile));
                            case ExtractExistingFileAction.OverwriteSilently:
                                {
                                    if (_zipfile.Verbose)
                                        _zipfile.StatusMessageTextWriter.WriteLine("the file {0} exists; deleting it...", TargetFile);

                                    //File.Delete(TargetFile);
                                    ReallyDelete(TargetFile);
                                }
                                break;
                            case ExtractExistingFileAction.DontOverwrite:
                                if (_zipfile.Verbose)
                                    _zipfile.StatusMessageTextWriter.WriteLine("the file {0} exists; not extracting entry...", FileName);
                                OnAfterExtract(baseDir);
                                return;
                            case ExtractExistingFileAction.InvokeExtractProgressEvent:
                                OnExtractExisting(baseDir);
                                // Check the ExtractExistingFile property again, it may have been reset.
                                if (ExtractExistingFile == ExtractExistingFileAction.Throw)
                                    throw new ZipException("The file already exists.");
                                else if (ExtractExistingFile == ExtractExistingFileAction.OverwriteSilently)
                                //File.Delete(TargetFile);
                                {
                                    if (_zipfile.Verbose)
                                        _zipfile.StatusMessageTextWriter.WriteLine("the file {0} exists; deleting it...", FileName);
                                    //File.Delete(TargetFile);
                                    ReallyDelete(TargetFile);
                                }
                                else if (ExtractExistingFile == ExtractExistingFileAction.DontOverwrite)
                                {
                                    if (_zipfile.Verbose)
                                        _zipfile.StatusMessageTextWriter.WriteLine("the file {0} exists; not extracting entry...", FileName);
                                    OnAfterExtract(baseDir);
                                    return;
                                }
                                else throw new ZipException("The file already exists.");
                                break;
                        }
                    }
                    output = new FileStream(TargetFile, FileMode.CreateNew);
                }
                else
                {
                    if (_zipfile.Verbose) _zipfile.StatusMessageTextWriter.WriteLine("extract entry {0} to stream...", FileName);
                    output = outstream;

                }


                if (_ioOperationCanceled)
                {
                    try
                    {
                        if (TargetFile != null)
                        {
                            if (output != null) output.Close();
                            // attempt to remove the target file if an exception has occurred:
                            if (File.Exists(TargetFile))
                                File.Delete(TargetFile);
                        }
                    }
                    finally { }

                }

                Int32 ActualCrc32 = _ExtractOne(output);

                if (_ioOperationCanceled)
                {
                    try
                    {
                        if (TargetFile != null)
                        {
                            if (output != null) output.Close();
                            // attempt to remove the target file if an exception has occurred:
                            if (File.Exists(TargetFile))
                                File.Delete(TargetFile);
                        }
                    }
                    finally { }
                }

                // After extracting, Validate the CRC32
                if (ActualCrc32 != _Crc32)
                {
#if AESCRYPTO
                    // CRC is not meaningful with WinZipAES and AES method 2 (AE-2)
                    if ((Encryption != EncryptionAlgorithm.WinZipAes128 &&
                         Encryption != EncryptionAlgorithm.WinZipAes256)
                        || _WinZipAesMethod != 0x02)
#endif

                        throw new BadCrcException("CRC error: the file being extracted appears to be corrupted. " +
                                                  String.Format("Expected 0x{0:X8}, Actual 0x{1:X8}", _Crc32, ActualCrc32));
                }


#if AESCRYPTO
                // Read the MAC if appropriate
                if (Encryption == EncryptionAlgorithm.WinZipAes128 ||
                    Encryption == EncryptionAlgorithm.WinZipAes256)
                {
                    _aesCrypto.ReadAndVerifyMac(this.ArchiveStream); // throws if MAC is bad
                    // side effect: advances file position.
                }
#endif


                if (TargetFile != null)
                {
                    output.Close();
                    output = null;

                    _SetTimes(TargetFile, true);

                    // workitem 8264
                    if (checkLaterForResetDirTimes)
                    {
                        // String.Contains is not available on .NET CF 2.0
                        if (this.FileName.IndexOf('/') != -1)
                        {
                            string dirname = Path.GetDirectoryName(this.FileName);
                            if (this._zipfile[dirname]!=null)
                                _SetTimes(Path.GetDirectoryName(TargetFile), false);
                        }
                    }

#if NETCF
                    // workitem 7926 - version made by OS can be zero or 10
                    if ((_VersionMadeBy & 0xFF00) == 0x0a00 || (_VersionMadeBy & 0xFF00) == 0x0000)
                        NetCfFile.SetAttributes(TargetFile, (uint)_ExternalFileAttrs);

#else
                    // workitem 7071
                    // We can only apply attributes if they are relevant to the NTFS OS. 
                    // Must do this LAST because it may involve a ReadOnly bit, which would prevent
                    // us from setting the time, etc. 
                    // workitem 7926 - version made by OS can be zero (FAT) or 10 (NTFS)
                    if ((_VersionMadeBy & 0xFF00) == 0x0a00 || (_VersionMadeBy & 0xFF00) == 0x0000)
                        File.SetAttributes(TargetFile, (FileAttributes)_ExternalFileAttrs);

#endif
                }

                OnAfterExtract(baseDir);
            }
            catch (Exception ex1)
            {
                try
                {
                    if (ex1 != null)
                        if (TargetFile != null)
                        {
                            if (output != null) output.Close();
                            // An exception has occurred.
                            // if the file exists, check to see if it existed before we tried extracting.
                            // if it did not, or if we were overwriting the file, attempt to remove the target file.
                            if (File.Exists(TargetFile))
                            {
                                if (!fileExistsBeforeExtraction || (ExtractExistingFile == ExtractExistingFileAction.OverwriteSilently))
                                    File.Delete(TargetFile);
                            }
                        }
                }
                finally { }

                if (ex1 as Ionic.Zip.ZipException == null)
                    // wrap the original exception and throw
                    throw new ZipException("Cannot extract", ex1);
                else
                    throw;
            }
        }


        private void _SetTimes(string fileOrDirectory, bool isFile)
        {
            if (_ntfsTimesAreSet)
            {
#if NETCF
                // workitem 7944: set time should not be a fatal error on CF
                int rc = NetCfFile.SetTimes(fileOrDirectory, _Ctime, _Atime, _Mtime);
                if ( rc != 0)
                {
                    if (_zipfile.Verbose)
                        _zipfile.StatusMessageTextWriter.WriteLine("Warning: SetTimes failed.  entry({0})  file({1})  rc({2})",
                                                                   FileName, fileOrDirectory, rc);
                }
#else
                if (isFile)
                {
                File.SetCreationTimeUtc(fileOrDirectory, _Ctime);
                File.SetLastAccessTimeUtc(fileOrDirectory, _Atime);
                File.SetLastWriteTimeUtc(fileOrDirectory, _Mtime);
                }
                else
                {
                Directory.SetCreationTimeUtc(fileOrDirectory, _Ctime);
                Directory.SetLastAccessTimeUtc(fileOrDirectory, _Atime);
                Directory.SetLastWriteTimeUtc(fileOrDirectory, _Mtime);
                }
#endif
            }
            else
            {
                // workitem 6191
                DateTime AdjustedLastModified = Ionic.Zip.SharedUtilities.AdjustTime_DotNetToWin32(LastModified);

#if NETCF
                int rc = NetCfFile.SetLastWriteTime(fileOrDirectory, AdjustedLastModified);
                        
                if ( rc != 0)
                {
                    if (_zipfile.Verbose)
                        _zipfile.StatusMessageTextWriter.WriteLine("Warning: SetLastWriteTime failed.  entry({0})  file({1})  rc({2})",
                                                                   FileName, fileOrDirectory, rc);
                }
#else
                if (isFile)
                    File.SetLastWriteTime(fileOrDirectory, AdjustedLastModified);
                else
                    Directory.SetLastWriteTime(fileOrDirectory, AdjustedLastModified);
#endif
            }
        }


        
        private void ValidateEncryption()
        {
            if (Encryption != EncryptionAlgorithm.PkzipWeak &&
#if AESCRYPTO
 Encryption != EncryptionAlgorithm.WinZipAes128 &&
                Encryption != EncryptionAlgorithm.WinZipAes256 &&
#endif
 Encryption != EncryptionAlgorithm.None)
            {
                // workitem 7968
                if (_UnsupportedAlgorithmId != 0)
                    throw new ZipException(String.Format("Cannot extract: Entry {0} is encrypted with an algorithm not supported by DotNetZip: {1}",
                                                         FileName, UnsupportedAlgorithm));
                else
                    throw new ZipException(String.Format("Cannot extract: Entry {0} uses an unsupported encryption algorithm ({1:X2})",
                                                         FileName, (int)Encryption));
            }
        }


        private void ValidateCompression()
        {
            if ((CompressionMethod != 0) && (CompressionMethod != 0x08))  // deflate
                throw new ArgumentException(String.Format("Entry {0} uses an unsupported Compression method (0x{1:X2})",
                                                          FileName, CompressionMethod));
        }


        private void SetupCrypto(string password)
        {
            if (password == null)
                return;

            if (Encryption == EncryptionAlgorithm.PkzipWeak)
            {
                // change for workitem 8098
                //this.ArchiveStream.Seek(this.FileDataPosition - 12, SeekOrigin.Begin);
                this._zipfile.SeekFromOrigin(this.FileDataPosition - 12);
                _zipCrypto = ZipCrypto.ForRead(password, this);
            }

#if AESCRYPTO
            else if (Encryption == EncryptionAlgorithm.WinZipAes128 ||
                 Encryption == EncryptionAlgorithm.WinZipAes256)
            {

                // if we already have a WinZipAesCrypto object in place, use it.
                if (_aesCrypto != null)
                {
                    _aesCrypto.Password = password;
                }
                else
                {
                    int sizeOfSaltAndPv = LengthOfCryptoHeaderBytes;
                    // change for workitem 8098
                    //this.ArchiveStream.Seek(this.FileDataPosition - sizeOfSaltAndPv, SeekOrigin.Begin);
                    this._zipfile.SeekFromOrigin(this.FileDataPosition - sizeOfSaltAndPv);
                    _aesCrypto = WinZipAesCrypto.ReadFromStream(password, _KeyStrengthInBits, this.ArchiveStream);

                }
            }
#endif


        }




        private bool ValidateOutput(string basedir, Stream outstream, out string OutputFile)
        {
            if (basedir != null)
            {
                // Sometimes the name on the entry starts with a slash.
                // Rather than unpack to the root of the volume, we're going to 
                // drop the slash and unpack to the specified base directory. 
                OutputFile = (this.FileName.StartsWith("/"))
            ? Path.Combine(basedir, this.FileName.Substring(1))
            : Path.Combine(basedir, this.FileName);

                // check if it is a directory
                if ((IsDirectory) || (FileName.EndsWith("/")))
                {
                    if (!Directory.Exists(OutputFile))
                    {
                        Directory.CreateDirectory(OutputFile);
                        _SetTimes(OutputFile, false);
                    }
                    else
                    {
                        // the dir exists, maybe we want to overwrite times. 
                        if (ExtractExistingFile == ExtractExistingFileAction.OverwriteSilently)
                            _SetTimes(OutputFile, false);
                    }
                    return true;  // true == all done, caller will return 
                }
                return false;  // false == work to do by caller.
            }

            if (outstream != null)
            {
                OutputFile = null;
                if ((IsDirectory) || (FileName.EndsWith("/")))
                {
                    // extract a directory to streamwriter?  nothing to do!
                    return true;  // true == all done!  caller can return
                }
                return false;
            }

            throw new ZipException("Cannot extract.", new ArgumentException("Invalid input.", "outstream | basedir"));
        }


        
        private void _CheckRead(int nbytes)
        {
            if (nbytes == 0)
                throw new BadReadException(String.Format("bad read of entry {0} from compressed archive.",
                             this.FileName));
        }



        private Int32 _ExtractOne(Stream output)
        {
            Stream input = this.ArchiveStream;

            // change for workitem 8098
            //input.Seek(this.FileDataPosition, SeekOrigin.Begin);
            this._zipfile.SeekFromOrigin(this.FileDataPosition);

            // to validate the CRC. 
            Int32 CrcResult = 0;

            byte[] bytes = new byte[BufferSize];

            // The extraction process varies depending on how the entry was stored.
            // It could have been encrypted, and it coould have been compressed, or both, or
            // neither. So we need to check both the encryption flag and the compression flag,
            // and take the proper action in all cases.  

            Int64 LeftToRead = (CompressionMethod == 0x08) ? this.UncompressedSize : this._CompressedFileDataSize;

            // Get a stream that either decrypts or not.
            Stream input2 = null;
            if (Encryption == EncryptionAlgorithm.PkzipWeak)
                input2 = new ZipCipherStream(input, _zipCrypto, CryptoMode.Decrypt);

#if AESCRYPTO

            else if (Encryption == EncryptionAlgorithm.WinZipAes128 ||
                 Encryption == EncryptionAlgorithm.WinZipAes256)
                input2 = new WinZipAesCipherStream(input, _aesCrypto, _CompressedFileDataSize, CryptoMode.Decrypt);
#endif

            else
                // Thu, 18 Jun 2009  01:45
                // why do I need a CrcCalculatorStream?  couldn't I just assign input1 ?
                //input2 = new Ionic.Zlib.CrcCalculatorStream(input, _CompressedFileDataSize);
                input2 = input;


            //Stream input2a = new TraceStream(input2);

            // Using the above, now we get a stream that either decompresses or not.
            Stream input3 = (CompressionMethod == 0x08)
                ? new Ionic.Zlib.DeflateStream(input2, Ionic.Zlib.CompressionMode.Decompress, true)
                : input2;

            Int64 bytesWritten = 0;
            // As we read, we maybe decrypt, and then we maybe decompress. Then we write.
            using (var s1 = new Ionic.Zlib.CrcCalculatorStream(input3))
            {
                while (LeftToRead > 0)
                {
                    //Console.WriteLine("ExtractOne: LeftToRead {0}", LeftToRead);


                    // Casting LeftToRead down to an int is ok here in the else clause,
                    // because that only happens when it is less than bytes.Length,
                    // which is much less than MAX_INT.
                    int len = (LeftToRead > bytes.Length) ? bytes.Length : (int)LeftToRead;
                    int n = s1.Read(bytes, 0, len);

                    // must check data read - essential for detecting corrupt zip files
                    _CheckRead(n);

                    output.Write(bytes, 0, n);
                    LeftToRead -= n;
                    bytesWritten += n;

                    // fire the progress event, check for cancels
                    OnExtractProgress(bytesWritten, UncompressedSize);
                    if (_ioOperationCanceled)
                    {
                        break;
                    }
                }

                CrcResult = s1.Crc32;


#if AESCRYPTO
                // Read the MAC if appropriate
                if (Encryption == EncryptionAlgorithm.WinZipAes128 ||
                    Encryption == EncryptionAlgorithm.WinZipAes256)
                {
                    var wzs = input2 as WinZipAesCipherStream;
                    _aesCrypto.CalculatedMac = wzs.FinalAuthentication;
                }
#endif
            }

            return CrcResult;
        }




        internal void MarkAsDirectory()
        {
            _IsDirectory = true;
            // workitem 6279
            if (!_FileNameInArchive.EndsWith("/"))
                _FileNameInArchive += "/";
        }



        /// <summary>
        /// Indicates whether an entry is mark as a text file.
        /// </summary>
        /// <remarks>
        ///
        /// <para>
        /// The ZIP format includes a provision for specifying whether an entry in the
        /// zip archive is a text or binary file.  Such a distinction may seem irrelevant
        /// now, but some zip tools or libraries, in particular older PKUnzip on IBM
        /// mainframes, require this bit to be set in order to unzip text and binary
        /// files properly.  Set this property to true to set the Text bit for an entry
        /// that represents a text file, if you want your zip files to be readable by
        /// these older zip tools and libraries.
        /// </para>
        ///
        /// <para>
        /// When writing a zip file, you must set the property before calling
        /// <c>Save()</c> on the ZipFile.
        /// </para>
        ///
        /// <para>
        /// If you are not having compatibility problems with zip archives, you can
        /// safely ignore this property.
        /// </para>
        /// </remarks>
        ///
        /// <example>
        /// <code>
        /// using (var zip = new ZipFile())
        /// {
        ///     var e = zip.UpdateFile("Descriptions.txt", "");
        ///     e.IsText = true;
        ///     zip.Save(zipPath);
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As New ZipFile
        ///     Dim e2 as ZipEntry = zip.AddFile("Descriptions.txt", "")
        ///     e.IsText= True
        ///     zip.Save(zipPath)
        /// End Using
        /// </code>
        /// </example>
        public bool IsText
        {
            // workitem 7801
            get { return _IsText; }
            set { _IsText = value; }
        }



        internal void WriteCentralDirectoryEntry(Stream s)
        {
            _ConsAndWriteCentralDirectoryEntry(s);
        }


        private void _ConsAndWriteCentralDirectoryEntry(Stream s)
        {
            byte[] bytes = new byte[4096];
            int i = 0;
            // signature 
            bytes[i++] = (byte)(ZipConstants.ZipDirEntrySignature & 0x000000FF);
            bytes[i++] = (byte)((ZipConstants.ZipDirEntrySignature & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((ZipConstants.ZipDirEntrySignature & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((ZipConstants.ZipDirEntrySignature & 0xFF000000) >> 24);

            // Version Made By
            // workitem 7071
            // We must not overwrite the VersionMadeBy field when writing out a zip archive.
            // The VersionMadeBy tells the zip reader the meaning of the File attributes. 
            // Overwriting the VersionMadeBy will result in inconsistent metadata. 
            // Consider the scenario where the application opens and reads a zip file that had been created
            // on Linux. Then the app adds one file to the Zip archive, and saves it.
            // The file attributes for all the entries added on Linux will be significant 
            // for Linux.  Therefore the VersionMadeBy for those entries must not be changed.
            // Only the entries that are actually created on Windows NTFS should get the 
            // VersionMadeBy indicating Windows/NTFS.  
            bytes[i++] = (byte)(_VersionMadeBy & 0x00FF);
            bytes[i++] = (byte)((_VersionMadeBy & 0xFF00) >> 8);

            // Apparently we want to duplicate the extra field here; we cannot
            // simply zero it out and assume tools and apps will use the right one.

            ////Int16 extraFieldLengthSave = (short)(_EntryHeader[28] + _EntryHeader[29] * 256);
            ////_EntryHeader[28] = 0;
            ////_EntryHeader[29] = 0;

            // Version Needed, Bitfield, compression method, lastmod,
            // crc, compressed and uncompressed sizes, filename length and extra field length.
            // These are all present in the local file header, but they may be zero values there.
            // So we cannot just copy them. 

            Int16 versionNeededToExtract = (Int16)(_OutputUsesZip64.Value ? 45 : 20);

            bytes[i++] = (byte)(versionNeededToExtract & 0x00FF);
            bytes[i++] = (byte)((versionNeededToExtract & 0xFF00) >> 8);
            bytes[i++] = (byte)(_BitField & 0x00FF);
            bytes[i++] = (byte)((_BitField & 0xFF00) >> 8);

            bytes[i++] = (byte)(CompressionMethod & 0x00FF);
            bytes[i++] = (byte)((CompressionMethod & 0xFF00) >> 8);

#if AESCRYPTO
            if (Encryption == EncryptionAlgorithm.WinZipAes128 ||
            Encryption == EncryptionAlgorithm.WinZipAes256)
            {
                i -= 2;
                bytes[i++] = 0x63;
                bytes[i++] = 0;
            }
#endif

            bytes[i++] = (byte)(_TimeBlob & 0x000000FF);
            bytes[i++] = (byte)((_TimeBlob & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_TimeBlob & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_TimeBlob & 0xFF000000) >> 24);
            bytes[i++] = (byte)(_Crc32 & 0x000000FF);
            bytes[i++] = (byte)((_Crc32 & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_Crc32 & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_Crc32 & 0xFF000000) >> 24);

            int j = 0;
            if (_OutputUsesZip64.Value)
            {
                // CompressedSize (Int32) and UncompressedSize - all 0xFF
                for (j = 0; j < 8; j++)
                    bytes[i++] = 0xFF;
            }
            else
            {
                bytes[i++] = (byte)(_CompressedSize & 0x000000FF);
                bytes[i++] = (byte)((_CompressedSize & 0x0000FF00) >> 8);
                bytes[i++] = (byte)((_CompressedSize & 0x00FF0000) >> 16);
                bytes[i++] = (byte)((_CompressedSize & 0xFF000000) >> 24);

                bytes[i++] = (byte)(_UncompressedSize & 0x000000FF);
                bytes[i++] = (byte)((_UncompressedSize & 0x0000FF00) >> 8);
                bytes[i++] = (byte)((_UncompressedSize & 0x00FF0000) >> 16);
                bytes[i++] = (byte)((_UncompressedSize & 0xFF000000) >> 24);
            }

            byte[] FileNameBytes = _GetEncodedFileNameBytes();
            Int16 filenameLength = (Int16)FileNameBytes.Length;
            bytes[i++] = (byte)(filenameLength & 0x00FF);
            bytes[i++] = (byte)((filenameLength & 0xFF00) >> 8);


            // do this again because now we have real data
            _presumeZip64 = _OutputUsesZip64.Value;
            _Extra = ConsExtraField(true);

            Int16 extraFieldLength = (Int16)((_Extra == null) ? 0 : _Extra.Length);
            bytes[i++] = (byte)(extraFieldLength & 0x00FF);
            bytes[i++] = (byte)((extraFieldLength & 0xFF00) >> 8);

            // File (entry) Comment Length
            // the _CommentBytes private field was set during WriteHeader()
            int commentLength = (_CommentBytes == null) ? 0 : _CommentBytes.Length;

            // the size of our buffer defines the max length of the comment we can write
            if (commentLength + i > bytes.Length) commentLength = bytes.Length - i;
            bytes[i++] = (byte)(commentLength & 0x00FF);
            bytes[i++] = (byte)((commentLength & 0xFF00) >> 8);

            // Disk number start
            bytes[i++] = 0;
            bytes[i++] = 0;

            // internal file attrs
            // workitem 7801
            bytes[i++] = (byte)((_IsText) ? 1 : 0); // lo bit: filetype hint.  0=bin, 1=txt. 
            bytes[i++] = 0;

            // external file attrs
            // workitem 7071
            bytes[i++] = (byte)(_ExternalFileAttrs & 0x000000FF);
            bytes[i++] = (byte)((_ExternalFileAttrs & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_ExternalFileAttrs & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_ExternalFileAttrs & 0xFF000000) >> 24);

            // relative offset of local header
            if (_OutputUsesZip64.Value)
            {
                // Value==true means it used Zip64.  
                for (j = 0; j < 4; j++) bytes[i++] = 0xFF;
            }
            else
            {
                bytes[i++] = (byte)(_RelativeOffsetOfLocalHeader & 0x000000FF);
                bytes[i++] = (byte)((_RelativeOffsetOfLocalHeader & 0x0000FF00) >> 8);
                bytes[i++] = (byte)((_RelativeOffsetOfLocalHeader & 0x00FF0000) >> 16);
                bytes[i++] = (byte)((_RelativeOffsetOfLocalHeader & 0xFF000000) >> 24);
            }

            // actual filename 
            for (j = 0; j < filenameLength; j++)
                bytes[i + j] = FileNameBytes[j];
            i += j;

            // "Extra field"
            if (_Extra != null)
            {
                for (j = 0; j < extraFieldLength; j++)
                    bytes[i + j] = _Extra[j];
                i += j;
            }

            // file (entry) comment
            if (commentLength != 0)
            {
                // now actually write the comment itself into the byte buffer
                for (j = 0; (j < commentLength) && (i + j < bytes.Length); j++)
                    bytes[i + j] = _CommentBytes[j];
                i += j;
            }

            s.Write(bytes, 0, i);
        }


#if INFOZIP_UTF8
        static private bool FileNameIsUtf8(char[] FileNameChars)
        {
            bool isUTF8 = false;
            bool isUnicode = false;
            for (int j = 0; j < FileNameChars.Length; j++)
            {
                byte[] b = System.BitConverter.GetBytes(FileNameChars[j]);
                isUnicode |= (b.Length != 2);
                isUnicode |= (b[1] != 0);
                isUTF8 |= ((b[0] & 0x80) != 0);
            }

            return isUTF8;
        }
#endif


        private byte[] ConsExtraField(bool forCentralDirectory)
        {
            var listOfBlocks = new System.Collections.Generic.List<byte[]>();
            byte[] block;

            // Always emit an extra field with zip64 information.
            // Later, if we don't need it, we'll set the header ID to rubbish and
            // the data will be ignored.  This results in additional overhead metadata
            // in the zip file, but it will be small in comparison to the entry data.
            if (_zipfile._zip64 != Zip64Option.Never)
            {
                // add extra field for zip64 here
                // workitem 7924
                int sz = 4 + (forCentralDirectory ? 28 : 16);
                block = new byte[sz];
                int i = 0;

                if (_presumeZip64)
                {
                    // HeaderId = always use zip64 extensions.
                    block[i++] = 0x01;
                    block[i++] = 0x00;
                }
                else
                {
                    // HeaderId = dummy data now, maybe set to 0x0001 (ZIP64) later.
                    block[i++] = 0x99;
                    block[i++] = 0x99;
                }

                // DataSize
                block[i++] = (byte)(sz - 4);  // decimal 28 or 16  (workitem 7924)
                block[i++] = 0x00;

                // The actual metadata - we may or may not have real values yet...

                // uncompressed size
                Array.Copy(BitConverter.GetBytes(_UncompressedSize), 0, block, i, 8);
                i += 8;
                // compressed size
                Array.Copy(BitConverter.GetBytes(_CompressedSize), 0, block, i, 8);

                // workitem 7924 - only include this if the "extra" field is for use in the central directory.
                // It is unnecessary and not useful for local header; makes WinZip choke.
                if (forCentralDirectory)
                {
                    i += 8;
                    // relative offset
                    Array.Copy(BitConverter.GetBytes(_RelativeOffsetOfLocalHeader), 0, block, i, 8);
                    i += 8;
                    // starting disk number
                    Array.Copy(BitConverter.GetBytes(0), 0, block, i, 4);
                }
                listOfBlocks.Add(block);
            }


#if AESCRYPTO
            if (Encryption == EncryptionAlgorithm.WinZipAes128 ||
                Encryption == EncryptionAlgorithm.WinZipAes256)
            {
                block = new byte[4 + 7];
                int i = 0;
                // extra field for WinZip AES 
                // header id
                block[i++] = 0x01;
                block[i++] = 0x99;

                // data size
                block[i++] = 0x07;
                block[i++] = 0x00;

                // vendor number
                block[i++] = 0x01;  // AE-1 - means "Verify CRC"
                block[i++] = 0x00;

                // vendor id "AE"
                block[i++] = 0x41;
                block[i++] = 0x45;

                // key strength
                block[i] = 0xFF;
                if (_KeyStrengthInBits == 128)
                    block[i] = 1;
                if (_KeyStrengthInBits == 256)
                    block[i] = 3;
                i++;

                // actual compression method
                block[i++] = (byte)(_CompressionMethod & 0x00FF);
                block[i++] = (byte)(_CompressionMethod & 0xFF00);

                listOfBlocks.Add(block);
            }
#endif

            if (_ntfsTimesAreSet && _emitNtfsTimes)
            {
                block = new byte[32 + 4];
                // HeaderId   2 bytes    0x000a == NTFS times
                // Datasize   2 bytes    32
                // reserved   4 bytes    ?? don't care 
                // timetag    2 bytes    0x0001 == NTFS time
                // size       2 bytes    24 == 8 bytes each for ctime, mtime, atime
                // mtime      8 bytes    win32 ticks since win32epoch
                // atime      8 bytes    win32 ticks since win32epoch
                // ctime      8 bytes    win32 ticks since win32epoch
                int i = 0;
                // extra field for NTFS times
                // header id
                block[i++] = 0x0a;
                block[i++] = 0x00;

                // data size
                block[i++] = 32;
                block[i++] = 0;

                i += 4; // reserved

                // time tag
                block[i++] = 0x01;
                block[i++] = 0x00;

                // data size (again)
                block[i++] = 24;
                block[i++] = 0;

                Int64 z = _Mtime.ToFileTime();
                Array.Copy(BitConverter.GetBytes(z), 0, block, i, 8);
                i += 8;
                z = _Atime.ToFileTime();
                Array.Copy(BitConverter.GetBytes(z), 0, block, i, 8);
                i += 8;
                z = _Ctime.ToFileTime();
                Array.Copy(BitConverter.GetBytes(z), 0, block, i, 8);
                i += 8;

                listOfBlocks.Add(block);
            }

            if (_ntfsTimesAreSet && _emitUnixTimes)
            {
                int len = 5 + 4;
                if (!forCentralDirectory) len += 8;

                block = new byte[len];
                // local form:
                // --------------
                // HeaderId   2 bytes    0x5455 == unix timestamp
                // Datasize   2 bytes    13 
                // flags      1 byte     7 (low three bits all set)
                // mtime      4 bytes    seconds since unix epoch
                // atime      4 bytes    seconds since unix epoch
                // ctime      4 bytes    seconds since unix epoch
                //
                // central directory form:
                //---------------------------------
                // HeaderId   2 bytes    0x5455 == unix timestamp
                // Datasize   2 bytes    5
                // flags      1 byte     7 (low three bits all set)
                // mtime      4 bytes    seconds since unix epoch
                //
                int i = 0;
                // extra field for "unix" times
                // header id
                block[i++] = 0x55;
                block[i++] = 0x54;

                // data size
                block[i++] = unchecked((byte)(len - 4));
                block[i++] = 0;

                // flags
                block[i++] = 0x07;

                Int32 z = unchecked((int)((_Mtime - _unixEpoch).TotalSeconds));
                Array.Copy(BitConverter.GetBytes(z), 0, block, i, 4);
                i += 4;
                if (!forCentralDirectory)
                {
                    z = unchecked((int)((_Atime - _unixEpoch).TotalSeconds));
                    Array.Copy(BitConverter.GetBytes(z), 0, block, i, 4);
                    i += 4;
                    z = unchecked((int)((_Ctime - _unixEpoch).TotalSeconds));
                    Array.Copy(BitConverter.GetBytes(z), 0, block, i, 4);
                    i += 4;
                }
                listOfBlocks.Add(block);
            }


            // inject other blocks here...


            // concatenate any blocks we've got: 
            byte[] aggregateBlock = null;
            if (listOfBlocks.Count > 0)
            {
                int totalLength = 0;
                int i, current = 0;
                for (i = 0; i < listOfBlocks.Count; i++)
                    totalLength += listOfBlocks[i].Length;
                aggregateBlock = new byte[totalLength];
                for (i = 0; i < listOfBlocks.Count; i++)
                {
                    System.Array.Copy(listOfBlocks[i], 0, aggregateBlock, current, listOfBlocks[i].Length);
                    current += listOfBlocks[i].Length;
                }
            }

            return aggregateBlock;
        }



        // workitem 6513: when writing, use alt encoding only when ibm437 will not do
        private System.Text.Encoding GenerateCommentBytes()
        {
            _CommentBytes = ibm437.GetBytes(_Comment);
            // need to use this form of GetString() for .NET CF
            string s1 = ibm437.GetString(_CommentBytes, 0, _CommentBytes.Length);
            if (s1 == _Comment)
                return ibm437;
            else
            {
                _CommentBytes = _provisionalAlternateEncoding.GetBytes(_Comment);
                return _provisionalAlternateEncoding;
            }
        }


        // workitem 6513
        private byte[] _GetEncodedFileNameBytes()
        {
            // here, we need to flip the backslashes to forward-slashes, 
            // also, we need to trim the \\server\share syntax from any UNC path.
            // and finally, we need to remove any leading .\

            string SlashFixed = FileName.Replace("\\", "/");
            string s1 = null;
            if ((_TrimVolumeFromFullyQualifiedPaths) && (FileName.Length >= 3)
                && (FileName[1] == ':') && (SlashFixed[2] == '/'))
            {
                // trim off volume letter, colon, and slash
                s1 = SlashFixed.Substring(3);
            }
            else if ((FileName.Length >= 4)
                     && ((SlashFixed[0] == '/') && (SlashFixed[1] == '/')))
            {
                int n = SlashFixed.IndexOf('/', 2);
                //System.Console.WriteLine("input Path '{0}'", FileName);
                //System.Console.WriteLine("xformed: '{0}'", SlashFixed);
                //System.Console.WriteLine("third slash: {0}\n", n);
                if (n == -1)
                    throw new ArgumentException("The path for that entry appears to be badly formatted");
                s1 = SlashFixed.Substring(n + 1);
            }
            else if ((FileName.Length >= 3)
                     && ((SlashFixed[0] == '.') && (SlashFixed[1] == '/')))
            {
                // trim off dot and slash
                s1 = SlashFixed.Substring(2);
            }
            else
            {
                s1 = SlashFixed;
            }

            // workitem 6513: when writing, use the alternative encoding only when ibm437 will not do.
            byte[] result = ibm437.GetBytes(s1);
            // need to use this form of GetString() for .NET CF
            string s2 = ibm437.GetString(result, 0, result.Length);
            _CommentBytes = null;
            if (s2 == s1)
            {
                // file can be encoded with ibm437, now try comment

                // case 1: no comment.  use ibm437
                if (_Comment == null || _Comment.Length == 0)
                {
                    _actualEncoding = ibm437;
                    return result;
                }

                // there is a comment.  Get the encoded form.
                System.Text.Encoding commentEncoding = GenerateCommentBytes();

                // case 2: if the comment also uses 437, we're good. 
                if (commentEncoding.CodePage == 437)
                {
                    _actualEncoding = ibm437;
                    return result;
                }

                // case 3: comment requires non-437 code page.  Use the same
                // code page for the filename.
                _actualEncoding = commentEncoding;
                result = commentEncoding.GetBytes(s1);
                return result;
            }
            else
            {
                // Cannot encode with ibm437 safely.
                // Therefore, use the provisional encoding
                result = _provisionalAlternateEncoding.GetBytes(s1);
                if (_Comment != null && _Comment.Length != 0)
                {
                    _CommentBytes = _provisionalAlternateEncoding.GetBytes(_Comment);
                }

                _actualEncoding = _provisionalAlternateEncoding;
                return result;
            }
        }


        private bool WantReadAgain()
        {
            if (_UncompressedSize < 10) return false;
            if (_CompressionMethod == 0x00) return false;
            if (_CompressedSize < _UncompressedSize) return false;
            if (ForceNoCompression) return false;
            if (this._Source == ZipEntrySource.Stream && !this._sourceStream.CanSeek) return false;

#if AESCRYPTO
            if (_aesCrypto != null && (CompressedSize - _aesCrypto.SizeOfEncryptionMetadata) <= UncompressedSize) return false;
#endif

            if (_zipCrypto != null && (CompressedSize - 12) <= UncompressedSize) return false;

            // finally, check the delegate 
            if (WillReadTwiceOnInflation != null)
                return WillReadTwiceOnInflation(_UncompressedSize, _CompressedSize, FileName);

            return true;
        }

        // heuristic - if the filename is one of a known list of non-compressible files, 
        // return false. else true.  We apply this by just checking the extension. 
        // (?i) = use case-insensitive matching
        private static RE.Regex _IncompressibleRegex = new RE.Regex("(?i)^(.+)\\.(mp3|png|docx|xlsx|pptx|jpg|zip)$");
        private static bool SeemsCompressible(string filename)
        {
            return !_IncompressibleRegex.IsMatch(filename);
        }


        private bool DefaultWantCompression()
        {
            if (_LocalFileName != null)
                return SeemsCompressible(_LocalFileName);

            if (_FileNameInArchive != null)
                return SeemsCompressible(_FileNameInArchive);

            return true;
        }



        private void FigureCompressionMethodForWriting(int cycle)
        {
            // if we've already tried with compression... turn it off this time
            if (cycle > 1)
            {
                _CompressionMethod = 0x0;
            }
            // compression for directories = 0x00 (No Compression)
            else if (IsDirectory)
            {
                _CompressionMethod = 0x0;
            }
            else if (__FileDataPosition != -1)
            {
                // If at this point, __FileDataPosition is non-zero, that means we've read this
                // entry from an existing zip archive. 
                // 
                // In this case, we just keep the existing file data and metadata (including
                // CompressionMethod, CRC, compressed size, uncompressed size, etc).
                // 
                // All those member variables have been set during read! 
                // 
            }
            else
            {
                // If __FileDataPosition is zero, then that means we will get the data
                // from a file or stream.

                // It is never possible to compress a zero-length file, so we check for 
                // this condition. 

                if (this._Source == ZipEntrySource.Stream)
                {
                    // workitem 7742
                    if (_sourceStream != null && _sourceStream.CanSeek)
                    {
                        // Length prop will throw if CanSeek is false
                        long fileLength = _sourceStream.Length;
                        if (fileLength == 0)
                        {
                            _CompressionMethod = 0x00;
                            return;
                        }
                    }
                }
                else
                {
                    // special case zero-length files
                    FileInfo fi = new FileInfo(LocalFileName);
                    long fileLength = fi.Length;
                    if (fileLength == 0)
                    {
                        _CompressionMethod = 0x00;
                        return;
                    }
                }

                if (_ForceNoCompression)
                {
                    _CompressionMethod = 0x00;
                    return;
                }

                // Ok, we're getting the data to be compressed from a non-zero length file
                // or stream.  In that case we check the callback to see if the app
                // wants to tell us whether to compress or not.  

                if (WantCompression != null)
                {
                    _CompressionMethod = (short)(WantCompression(LocalFileName, _FileNameInArchive)
                                                 ? 0x08 : 0x00);
                    return;
                }

                // if there is no callback set, we use the default behavior.
                _CompressionMethod = (short)(DefaultWantCompression()
                                             ? 0x08 : 0x00);
                //Console.WriteLine("DefaultWantCompression: {0}", _CompressionMethod);
                return;
            }
        }



        // write the header info for an entry
        private void WriteHeader(Stream s, int cycle)
        {
            int j = 0;

            // remember the offset, within the output stream, of this particular entry header
            var counter = s as CountingStream;
            // workitem 8098: ok (output)
            _RelativeOffsetOfLocalHeader = (counter != null)
                ? counter.BytesWritten
                : s.Position;

            byte[] bytes = new byte[512];  // large enough for looooong filenames (MAX_PATH == 260)

            int i = 0;
            // signature
            bytes[i++] = (byte)(ZipConstants.ZipEntrySignature & 0x000000FF);
            bytes[i++] = (byte)((ZipConstants.ZipEntrySignature & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((ZipConstants.ZipEntrySignature & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((ZipConstants.ZipEntrySignature & 0xFF000000) >> 24);

            // validate the ZIP64 usage
            if (_zipfile._zip64 == Zip64Option.Never && (uint)_RelativeOffsetOfLocalHeader >= 0xFFFFFFFF)
                throw new ZipException("Offset within the zip archive exceeds 0xFFFFFFFF. Consider setting the UseZip64WhenSaving property on the ZipFile instance.");


            // Design notes for ZIP64:

            // The specification says that the header must include the Compressed and
            // Uncompressed sizes, as well as the CRC32 value.  When creating a zip via
            // streamed processing, these quantities are not known until after the compression
            // is done.  Thus, a typical way to do it is to insert zeroes for these quantities,
            // then do the compression, then seek back to insert the appropriate values, then
            // seek forward to the end of the file data.

            // There is also the option of using bit 3 in the GP bitfield - to specify that
            // there is a data descriptor after the file data containing these three
            // quantities.

            // This works when the size of the quantities is known, either 32-bits or 64 bits as 
            // with the ZIP64 extensions.  

            // With Zip64, the 4-byte fields are set to 0xffffffff, and there is a
            // corresponding data block in the "extra field" that contains the actual
            // Compressed, uncompressed sizes.  (As well as an additional field, the "Relative
            // Offset of Local Header")

            // The problem is when the app desires to use ZIP64 extensions optionally, only
            // when necessary.  Suppose the library assumes no zip64 extensions when writing
            // the header, then after compression finds that the size of the data requires
            // zip64.  At this point, the header, already written to the file, won't have the
            // necessary data block in the "extra field".  The size of the entry header is
            // fixed, so it is not possible to just "add on" the zip64 data block after
            // compressing the file.  On the other hand, always using zip64 will break
            // interoperability with many other systems and apps.

            // The approach we take is to insert a 32-byte dummy data block in the extra field,
            // whenever zip64 is to be used "as necessary". This data block will get the actual
            // zip64 HeaderId and zip64 metadata if necessary.  If not necessary, the data
            // block will get a meaningless HeaderId (0x1111), and will be filled with zeroes.

            // When zip64 is actually in use, we also need to set the VersionNeededToExtract
            // field to 45.
            //

            // There is one additional wrinkle: using zip64 as necessary conflicts with output
            // to non-seekable devices.  The header is emitted and must indicate whether zip64
            // is in use, before we know if zip64 is necessary.  Because there is no seeking,
            // the header can never be changed.  Therefore, on non-seekable devices,
            // Zip64Option.AsNecessary is the same as Zip64Option.Always.

            // version needed- see AppNote.txt.
            // need v5.1 for PKZIP strong encryption, or v2.0 for no encryption or for PK
            // encryption, 4.5 for zip64.  We may reset this later, as necessary or zip64.

            _presumeZip64 = (_zipfile._zip64 == Zip64Option.Always || (_zipfile._zip64 == Zip64Option.AsNecessary && !s.CanSeek));
            Int16 VersionNeededToExtract = (Int16)(_presumeZip64 ? 45 : 20);

            // (i==4)
            bytes[i++] = (byte)(VersionNeededToExtract & 0x00FF);
            bytes[i++] = (byte)((VersionNeededToExtract & 0xFF00) >> 8);

            // get byte array including any encoding
            // workitem 6513
            byte[] FileNameBytes = _GetEncodedFileNameBytes();
            Int16 filenameLength = (Int16)FileNameBytes.Length;

            // general purpose bitfield
            // In the current implementation, this library uses only these bits 
            // in the GP bitfield:
            //  bit 0 = if set, indicates the entry is encrypted
            //  bit 3 = if set, indicates the CRC, C and UC sizes follow the file data.
            //  bit 6 = strong encryption 
            //  bit 11 = UTF-8 encoding is used in the comment and filename

            _BitField = (Int16)((UsesEncryption) ? 1 : 0);

            // workitem 7941: WinZip does not set this when using AES.
            // this "Strong Encryption" is a PKWare Strong encryption thing.
            //             if (UsesEncryption && (IsStrong(Encryption)))
            //                 _BitField |= 0x0020;

            // set the UTF8 bit if necessary
            if (ActualEncoding.CodePage == System.Text.Encoding.UTF8.CodePage) _BitField |= 0x0800;

            // The PKZIP spec says that if bit 3 is set (0x0008) in the General Purpose BitField,
            // then the CRC, Compressed size, and uncompressed size are written directly after the
            // file data.   
            // 
            // Those 3 quantities are not knowable until after the compression is done. Yet they
            // are required to be in the header.  Normally, we'd 
            //  - write the header, using zeros for these quantities
            //  - compress the data, and incidentally compute these quantities.
            //  - seek back and write the correct values them into the header. 
            //
            // This is nice because it is simpler and less error prone to read the zip file.
            //
            // But if seeking in the output stream is not possible, then we need to set the
            // appropriate bitfield and emit these quantities after the compressed file data in
            // the output.

            // workitem 7216 - having trouble formatting a zip64 file that is readable by WinZip.
            // not sure why!  What I found is that setting bit 3 and following all the implications,
            // the zip64 file is readable by WinZip 12. and Perl's  IO::Compress::Zip . 
            // Perl takes an interesting approach - it always sets bit 3 if ZIP64 in use. 
            // I do the same, and it gives better compatibility with WinZip 12.

            // workitem 7924 - don't need this for WinZip compat any longer.
            //if (!s.CanSeek || _presumeZip64)
            if (!s.CanSeek)
                _BitField |= 0x0008;

            // (i==6)
            bytes[i++] = (byte)(_BitField & 0x00FF);
            bytes[i++] = (byte)((_BitField & 0xFF00) >> 8);

            // Here, we want to set values for Compressed Size, Uncompressed Size, and CRC.  If
            // we have __FileDataPosition as not -1 (zero is a valid FDP), then that means we
            // are reading this zip entry from a zip file, and we have good values for those
            // quantities.
            // 
            // If _FileDataPosition is -1, then we are consing up this Entry from scratch.  We
            // zero those quantities now, and we will compute actual values for the three
            // quantities later, when we do the compression, and then seek back to write them
            // into the appropriate place in the header.
            if (this.__FileDataPosition == -1)
            {
                _UncompressedSize = 0;
                _CompressedSize = 0;
                _Crc32 = 0;
                _crcCalculated = false;
            }

            // set compression method here
            FigureCompressionMethodForWriting(cycle);

            // (i==8) compression method         
            bytes[i++] = (byte)(CompressionMethod & 0x00FF);
            bytes[i++] = (byte)((CompressionMethod & 0xFF00) >> 8);

#if AESCRYPTO
            if (Encryption == EncryptionAlgorithm.WinZipAes128 || Encryption == EncryptionAlgorithm.WinZipAes256)
            {
                i -= 2;
                bytes[i++] = 0x63;
                bytes[i++] = 0;
            }
#endif

            // LastMod
            _TimeBlob = Ionic.Zip.SharedUtilities.DateTimeToPacked(LastModified);

            // (i==10) time blob
            bytes[i++] = (byte)(_TimeBlob & 0x000000FF);
            bytes[i++] = (byte)((_TimeBlob & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_TimeBlob & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_TimeBlob & 0xFF000000) >> 24);

            // (i==14) CRC - if source==filesystem, this is zero now, actual value will be calculated later.
            // if source==archive, this is a bonafide value.
            bytes[i++] = (byte)(_Crc32 & 0x000000FF);
            bytes[i++] = (byte)((_Crc32 & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_Crc32 & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_Crc32 & 0xFF000000) >> 24);

            if (_presumeZip64)
            {
                // (i==18) CompressedSize (Int32) and UncompressedSize - all 0xFF for now
                for (j = 0; j < 8; j++)
                    bytes[i++] = 0xFF;
            }
            else
            {
                // (i==18) CompressedSize (Int32) - this value may or may not be bonafide.
                // if source == filesystem, then it is zero, and we'll learn it after we compress.
                // if source == archive, then it is bonafide data.
                bytes[i++] = (byte)(_CompressedSize & 0x000000FF);
                bytes[i++] = (byte)((_CompressedSize & 0x0000FF00) >> 8);
                bytes[i++] = (byte)((_CompressedSize & 0x00FF0000) >> 16);
                bytes[i++] = (byte)((_CompressedSize & 0xFF000000) >> 24);

                // (i==22) UncompressedSize (Int32) - this value may or may not be bonafide.
                bytes[i++] = (byte)(_UncompressedSize & 0x000000FF);
                bytes[i++] = (byte)((_UncompressedSize & 0x0000FF00) >> 8);
                bytes[i++] = (byte)((_UncompressedSize & 0x00FF0000) >> 16);
                bytes[i++] = (byte)((_UncompressedSize & 0xFF000000) >> 24);
            }

            // (i==26) filename length (Int16)
            bytes[i++] = (byte)(filenameLength & 0x00FF);
            bytes[i++] = (byte)((filenameLength & 0xFF00) >> 8);

            _Extra = ConsExtraField(false);

            // (i==28) extra field length (short)
            Int16 ExtraFieldLength = (Int16)((_Extra == null) ? 0 : _Extra.Length);
            bytes[i++] = (byte)(ExtraFieldLength & 0x00FF);
            bytes[i++] = (byte)((ExtraFieldLength & 0xFF00) >> 8);

            // The filename written to the archive.
            // The buffer is already encoded; we just copy across the bytes.
            for (j = 0; (j < FileNameBytes.Length) && (i + j < bytes.Length); j++)
                bytes[i + j] = FileNameBytes[j];

            i += j;

            // "Extra field"
            if (_Extra != null)
            {
                for (j = 0; j < _Extra.Length; j++)
                    bytes[i + j] = _Extra[j];
                i += j;
            }

            _LengthOfHeader = i;

            // finally, write the header to the stream
            s.Write(bytes, 0, i);

            // preserve this header data, we'll use it again later.
            // ..when seeking backward, to write again, after we have the Crc, compressed
            //   and uncompressed sizes.  
            // ..and when writing the central directory structure.
            _EntryHeader = new byte[i];
            for (j = 0; j < i; j++)
                _EntryHeader[j] = bytes[j];
        }




        private Int32 FigureCrc32()
        {
            if (_crcCalculated == false)
            {
                Stream input = null;
                // get the original stream:
                if (this._Source == ZipEntrySource.Stream)
                {
                    PrepSourceStream();
                    input = _sourceStream;
                }
                else
                {
                    //input = File.OpenRead(LocalFileName);
                    input = File.Open(LocalFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                }
                var crc32 = new Ionic.Zlib.CRC32();

                _Crc32 = crc32.GetCrc32(input);
                if (_sourceStream == null)
                {
                    input.Close();
#if !NETCF20
                    input.Dispose();
#endif
                }
                _crcCalculated = true;
            }
            return _Crc32;
        }

        /// <summary>
        /// Stores the position of the entry source stream, or, if the position is
        /// already stored, seeks to that position.
        /// </summary>
        ///
        /// <remarks>
        /// This method is called in prep for reading the source stream.  If PKZIP
        /// encryption is used, then we need to calc the CRC32 before doing the
        /// encryption.  Hence we need to be able to seek backward in the source when
        /// saving the ZipEntry. This method is called from the place which calculates
        /// the CRC, and also from the method that does the encryption of the file data.
        /// </remarks>
        private void PrepSourceStream()
        {
            if (_sourceStream == null)
                throw new ZipException(String.Format("The input stream is null for entry '{0}'.", FileName));

            if (this._sourceStreamOriginalPosition != null)
            {
                // this will happen the 2nd cycle through, if the stream is seekable
                this._sourceStream.Position = this._sourceStreamOriginalPosition.Value;
            }
            else if (this._sourceStream.CanSeek)
            {
                // this will happen the first cycle through, if seekable
                this._sourceStreamOriginalPosition = new Nullable<Int64>(this._sourceStream.Position);
            }
            else if (this.Encryption == EncryptionAlgorithm.PkzipWeak)
                throw new ZipException("It is not possible to use PKZIP encryption on a non-seekable stream");
        }


        /// <summary>
        /// Copy metadata that may have been changed by the app.  We do this when
        /// resetting the zipFile instance.  If the app calls Save() on a ZipFile, then
        /// tries to party on that file some more, we may need to Reset() it , which
        /// means re-reading the entries and then copying the metadata.  I think.
        /// </summary>
        internal void CopyMetaData(ZipEntry source)
        {
            this.__FileDataPosition = source.__FileDataPosition;
            this.CompressionMethod = source.CompressionMethod;
            this._CompressedFileDataSize = source._CompressedFileDataSize;
            this._UncompressedSize = source._UncompressedSize;
            this._BitField = source._BitField;
            this._Source = source._Source;
            this._LastModified = source._LastModified;
            this._Mtime = source._Mtime;
            this._Atime = source._Atime;
            this._Ctime = source._Ctime;
            this._ntfsTimesAreSet = source._ntfsTimesAreSet;
            this._emitUnixTimes = source._emitUnixTimes;
            this._emitNtfsTimes = source._emitNtfsTimes;
        }


        private void _WriteFileData(Stream s)
        {
            // Read in the data from the input stream (often a file in the filesystem),
            // and write it to the output stream, calculating a CRC on it as we go.
            // We will also deflate and encrypt as necessary. 

            Stream input = null;
            Ionic.Zlib.CrcCalculatorStream input1 = null;
            CountingStream outputCounter = null;
            try
            {
                // s.Position may fail on some write-only streams, eg stdout or
                // System.Web.HttpResponseStream We swallow that exception, because we don't
                // care!
                this.__FileDataPosition = s.Position;
            }
            catch { }

            try
            {
                long fileLength = 0;
                // get the original stream:
                if (this._Source == ZipEntrySource.Stream)
                {
                    PrepSourceStream();
                    input = this._sourceStream;

                    //if (this._sourceStream.CanSeek)
                    // Try to get the length, no big deal if not available.
                    try { fileLength = this._sourceStream.Length; }
                    catch { }
                }
                else
                {
                    //input = File.OpenRead(LocalFileName);
                    // workitem 7145
                    FileShare fs = FileShare.ReadWrite;
#if !NETCF
                    // FileShare.Delete is not defined for the Compact Framework
                    fs |= FileShare.Delete;
#endif
                    FileInfo fi = new FileInfo(LocalFileName);
                    fileLength = fi.Length;

                    input = File.Open(LocalFileName, FileMode.Open, FileAccess.Read, fs);
                }


                // wrap a CRC Calculator Stream around the raw input stream. 
                input1 = new Ionic.Zlib.CrcCalculatorStream(input);

                // Wrap a counting stream around the raw output stream:
                // This is the last thing that happens before the bits go to the 
                // application-provided stream. 
                outputCounter = new CountingStream(s);

                // Maybe wrap an encrypting stream around that:
                // This will happen AFTER deflation but before counting, if encryption 
                // is used.
                Stream output1 = outputCounter;
                if (Encryption == EncryptionAlgorithm.PkzipWeak)
                    output1 = new ZipCipherStream(outputCounter, _zipCrypto, CryptoMode.Encrypt);

#if AESCRYPTO

                else if (Encryption == EncryptionAlgorithm.WinZipAes128 ||
                         Encryption == EncryptionAlgorithm.WinZipAes256)
                {
                    output1 = new WinZipAesCipherStream(outputCounter, _aesCrypto, CryptoMode.Encrypt);
                }
#endif


                //Stream output1a = new TraceStream(output1);

                // Maybe wrap a DeflateStream around that.
                // This will happen BEFORE encryption (if any) as we write data out.
                Stream output2 = null;
                bool mustCloseDeflateStream = false;
                if (CompressionMethod == 0x08)
                {
                    var o = new Ionic.Zlib.DeflateStream(output1, Ionic.Zlib.CompressionMode.Compress,
                                                         _zipfile.CompressionLevel,
                                                         true);
                    if (_zipfile.CodecBufferSize > 0)
                        o.BufferSize = _zipfile.CodecBufferSize;
                    o.Strategy = _zipfile.Strategy;
                    mustCloseDeflateStream = true;
                    output2 = o;
                }
                else
                {
                    output2 = output1;
                }

                //Stream output2 = new TraceStream(output2a);

                // as we emit the file, we maybe deflate, then maybe encrypt, then write the bytes. 
                byte[] buffer = new byte[BufferSize];
                int n;
                while ((n = SharedUtilities.ReadWithRetry(input1, buffer, 0, buffer.Length, FileName)) != 0)
                {
                    output2.Write(buffer, 0, n);
                    OnWriteBlock(input1.TotalBytesSlurped, fileLength);
                    if (_ioOperationCanceled)
                        break;
                }

                // by calling Close() on the deflate stream, we write the footer bytes, as necessary.
                if (mustCloseDeflateStream)
                    output2.Close();

                output1.Flush();
                output1.Close();

                _LengthOfTrailer = 0;
#if AESCRYPTO
                WinZipAesCipherStream wzacs = output1 as WinZipAesCipherStream;
                if (wzacs != null)
                {
                    s.Write(wzacs.FinalAuthentication, 0, 10);
                    _LengthOfTrailer += 10;
                }
#endif
            }
            finally
            {

                if (this._Source != ZipEntrySource.Stream && input != null)
                {
                    input.Close();
#if !NETCF
                    input.Dispose();
#endif
                }
            }

            if (_ioOperationCanceled)
                return;

            _UncompressedSize = input1.TotalBytesSlurped;
            _CompressedFileDataSize = outputCounter.BytesWritten;
            _CompressedSize = _CompressedFileDataSize; // may be adjusted
            _Crc32 = input1.Crc32;

            if (_Password != null)
            {
                if (Encryption == EncryptionAlgorithm.PkzipWeak)
                {
                    _CompressedSize += 12; // 12 extra bytes for the encryption header
                }
#if AESCRYPTO
                else if (Encryption == EncryptionAlgorithm.WinZipAes128 ||
                         Encryption == EncryptionAlgorithm.WinZipAes256)
                {
                    // adjust the compressed size to include the variable (salt+pv) 
                    // security header and 10-byte trailer. according to the winzip AES
                    // spec, that metadata is included in the "Compressed Size" figure
                    // when encoding the zip archive.
                    _CompressedSize += _aesCrypto.SizeOfEncryptionMetadata;
                }
#endif
            }


            int i = 8;
            _EntryHeader[i++] = (byte)(CompressionMethod & 0x00FF);
            _EntryHeader[i++] = (byte)((CompressionMethod & 0xFF00) >> 8);

            i = 14;
            // CRC - the correct value now
            _EntryHeader[i++] = (byte)(_Crc32 & 0x000000FF);
            _EntryHeader[i++] = (byte)((_Crc32 & 0x0000FF00) >> 8);
            _EntryHeader[i++] = (byte)((_Crc32 & 0x00FF0000) >> 16);
            _EntryHeader[i++] = (byte)((_Crc32 & 0xFF000000) >> 24);

            // zip64 housekeeping
            _entryRequiresZip64 = new Nullable<bool>
                (_CompressedSize >= 0xFFFFFFFF || _UncompressedSize >= 0xFFFFFFFF || _RelativeOffsetOfLocalHeader >= 0xFFFFFFFF);

            // validate the ZIP64 usage
            if (_zipfile._zip64 == Zip64Option.Never && _entryRequiresZip64.Value)
                throw new ZipException("Compressed or Uncompressed size, or offset exceeds the maximum value. Consider setting the UseZip64WhenSaving property on the ZipFile instance.");

            _OutputUsesZip64 = new Nullable<bool>(_zipfile._zip64 == Zip64Option.Always || _entryRequiresZip64.Value);

            // (i==26) filename length (Int16)
            Int16 filenameLength = (short)(_EntryHeader[26] + _EntryHeader[27] * 256);
            Int16 extraFieldLength = (short)(_EntryHeader[28] + _EntryHeader[29] * 256);

            if (_OutputUsesZip64.Value)
            {
                // VersionNeededToExtract - set to 45 to indicate zip64
                _EntryHeader[4] = (byte)(45 & 0x00FF);
                _EntryHeader[5] = 0x00;

                // workitem 7924 - don't need bit 3
                // // workitem 7917
                // // set bit 3 for ZIP64 compatibility with WinZip12
                // _BitField |= 0x0008;
                // _EntryHeader[6] = (byte)(_BitField & 0x00FF);

                // CompressedSize and UncompressedSize - 0xFF
                for (int j = 0; j < 8; j++)
                    _EntryHeader[i++] = 0xff;

                // At this point we need to find the "Extra field" that follows
                // the filename.  We had already emitted it, but the data
                // (uncomp, comp, Relative Offset) was not available at the
                // time we did so.  Here, we emit it again, with final values.

                i = 30 + filenameLength;
                _EntryHeader[i++] = 0x01;  // zip64
                _EntryHeader[i++] = 0x00;

                i += 2; // skip over data size, which is 16+4

                Array.Copy(BitConverter.GetBytes(_UncompressedSize), 0, _EntryHeader, i, 8);
                i += 8;
                Array.Copy(BitConverter.GetBytes(_CompressedSize), 0, _EntryHeader, i, 8);
            }
            else
            {
                // VersionNeededToExtract - reset to 20 since no zip64
                _EntryHeader[4] = (byte)(20 & 0x00FF);
                _EntryHeader[5] = 0x00;

                // CompressedSize - the correct value now
                i = 18;
                _EntryHeader[i++] = (byte)(_CompressedSize & 0x000000FF);
                _EntryHeader[i++] = (byte)((_CompressedSize & 0x0000FF00) >> 8);
                _EntryHeader[i++] = (byte)((_CompressedSize & 0x00FF0000) >> 16);
                _EntryHeader[i++] = (byte)((_CompressedSize & 0xFF000000) >> 24);

                // UncompressedSize - the correct value now
                _EntryHeader[i++] = (byte)(_UncompressedSize & 0x000000FF);
                _EntryHeader[i++] = (byte)((_UncompressedSize & 0x0000FF00) >> 8);
                _EntryHeader[i++] = (byte)((_UncompressedSize & 0x00FF0000) >> 16);
                _EntryHeader[i++] = (byte)((_UncompressedSize & 0xFF000000) >> 24);

                // The HeaderId in the extra field header, is already dummied out.
                if (extraFieldLength != 0)
                {
                    i = 30 + filenameLength;
                    // For zip archives written by this library, if the zip64 header exists, 
                    // it is the first header. Because of the logic used when first writing the 
                    // _EntryHeader bytes, the HeaderId is not guaranteed to be any
                    // particular value.  So we determine if the first header is a putative zip64
                    // header by examining the datasize.  
                    //UInt16 HeaderId = (UInt16)(_EntryHeader[i] + _EntryHeader[i + 1] * 256);
                    Int16 DataSize = (short)(_EntryHeader[i + 2] + _EntryHeader[i + 3] * 256);
                    if (DataSize == 16)
                    {
                        // reset to Header Id to dummy value, effectively dummy-ing out the zip64 metadata
                        _EntryHeader[i++] = 0x99;
                        _EntryHeader[i++] = 0x99;
                    }
                }
            }


#if AESCRYPTO

            if (Encryption == EncryptionAlgorithm.WinZipAes128 ||
                Encryption == EncryptionAlgorithm.WinZipAes256)
            {
                // Must set compressionmethod to 0x0063 (decimal 99)
                // and then set the compression method bytes inside the extra field to the actual 
                // compression method value.

                i = 8;
                _EntryHeader[i++] = 0x63;
                _EntryHeader[i++] = 0;

                i = 30 + filenameLength;
                do
                {
                    UInt16 HeaderId = (UInt16)(_EntryHeader[i] + _EntryHeader[i + 1] * 256);
                    Int16 DataSize = (short)(_EntryHeader[i + 2] + _EntryHeader[i + 3] * 256);
                    if (HeaderId != 0x9901)
                    {
                        // skip this header
                        i += DataSize + 4;
                    }
                    else
                    {
                        i += 9;
                        // actual compression method
                        _EntryHeader[i++] = (byte)(_CompressionMethod & 0x00FF);
                        _EntryHeader[i++] = (byte)(_CompressionMethod & 0xFF00);
                    }
                } while (i < (extraFieldLength - 30 - filenameLength));

            }
#endif


            // workitem 7216 - sometimes we don't seek even if we CAN.
            // ASP.NET Response.OutputStream, or stdout are non-seekable.
            // But we may also want to NOT seek in other cases, eg zip64.
            // For all cases, we just check bit 3 to see if we want to seek.

            if ((_BitField & 0x0008) != 0x0008)
            {
                // seek in the raw output stream, to the beginning of the header for
                // this entry.
                // workitem 8098: ok (output)
                s.Seek(this._RelativeOffsetOfLocalHeader, SeekOrigin.Begin);

                // write the updated header to the output stream
                s.Write(_EntryHeader, 0, _EntryHeader.Length);

                // adjust the count on the CountingStream as necessary
                var s1 = s as CountingStream;
                if (s1 != null) s1.Adjust(_EntryHeader.Length);

                // seek in the raw output stream, to the end of the file data for this entry
                s.Seek(_CompressedSize, SeekOrigin.Current);
            }
            else
            {
                byte[] Descriptor = new byte[16 + (_OutputUsesZip64.Value?8:0)];
                i = 0;
                    
                // signature
                Array.Copy(BitConverter.GetBytes(ZipConstants.ZipEntryDataDescriptorSignature), 0, Descriptor, i, 4);
                i += 4;

                // CRC - the correct value now
                Array.Copy(BitConverter.GetBytes(_Crc32), 0, Descriptor, i, 4);
                i += 4;

                // workitem 7917
                if (_OutputUsesZip64.Value)
                {
                    // CompressedSize - the correct value now
                    Array.Copy(BitConverter.GetBytes(_CompressedSize), 0, Descriptor, i, 8);
                    i += 8;

                    // UncompressedSize - the correct value now
                    Array.Copy(BitConverter.GetBytes(_UncompressedSize), 0, Descriptor, i, 8);
                    i += 8;
                }
                else
                {
                    // CompressedSize - (lower 32 bits) the correct value now
                    Descriptor[i++] = (byte)(_CompressedSize & 0x000000FF);
                    Descriptor[i++] = (byte)((_CompressedSize & 0x0000FF00) >> 8);
                    Descriptor[i++] = (byte)((_CompressedSize & 0x00FF0000) >> 16);
                    Descriptor[i++] = (byte)((_CompressedSize & 0xFF000000) >> 24);

                    // UncompressedSize - (lower 32 bits) the correct value now
                    Descriptor[i++] = (byte)(_UncompressedSize & 0x000000FF);
                    Descriptor[i++] = (byte)((_UncompressedSize & 0x0000FF00) >> 8);
                    Descriptor[i++] = (byte)((_UncompressedSize & 0x00FF0000) >> 16);
                    Descriptor[i++] = (byte)((_UncompressedSize & 0xFF000000) >> 24);
                }

                // finally, write the trailing descriptor to the output stream
                s.Write(Descriptor, 0, Descriptor.Length);

                _LengthOfTrailer += Descriptor.Length;
            }
        }




        internal void Write(Stream s)
        {
            if (_Source == ZipEntrySource.Zipfile && !_restreamRequiredOnSave)
            {
                CopyThroughOneEntry(s);
                return;
            }

            // Ok, the source for this entry is not a previously created zip file, or
            // the settings whave changed in important ways and therefore we will need to
            // process the bytestream (compute crc, maybe compress, maybe encrypt) in
            // order to create the zip.
            //
            // We do this in potentially 2 passes: The first time we do it as requested, maybe
            // with compression and maybe encryption.  If that causes the bytestream to inflate
            // in size, and if compression was on, then we turn off compression and do it again.

            bool readAgain = true;
            int nCycles = 0;
            do
            {
                nCycles++;

                WriteHeader(s, nCycles);

                if (IsDirectory)
                {
                    // nothing more to write, but we need to do some housekeeping.
                    _entryRequiresZip64 = new Nullable<bool>(_RelativeOffsetOfLocalHeader >= 0xFFFFFFFF);
                    _OutputUsesZip64 = new Nullable<bool>(_zipfile._zip64 == Zip64Option.Always || _entryRequiresZip64.Value);
                    return;
                }

                // now, write the actual file data. (incl the encrypted header)
                _EmitOne(s);

                // The file data has now been written to the stream, and 
                // the file pointer is positioned directly after file data.

                if (nCycles > 1) readAgain = false;
                else if (!s.CanSeek) readAgain = false;
                else readAgain = WantReadAgain();

                if (readAgain)
                {
                    // Seek back in the raw output stream, to the beginning of the file
                    // data for this entry.

                    // workitem 8098: ok (output).
                    s.Seek(_RelativeOffsetOfLocalHeader, SeekOrigin.Begin);

                    // If the last entry expands, we read again; but here, we must
                    // truncate the stream to prevent garbage data after the
                    // end-of-central-directory.

                    // workitem 8098: ok (output).
                    s.SetLength(s.Position);

                    // Adjust the count on the CountingStream as necessary.
                    var s1 = s as CountingStream;
                    if (s1 != null) s1.Adjust(_TotalEntrySize);
                }
            }
            while (readAgain);
        }




        private void _EmitOne(Stream outstream)
        {
            _WriteSecurityMetadata(outstream);

            // write the (potentially compressed, potentially encrypted) file data
            _WriteFileData(outstream);

            // track total entry size (including the trailing descriptor and MAC)
            _TotalEntrySize = _LengthOfHeader + _CompressedFileDataSize + _LengthOfTrailer;
        }



        private void _WriteSecurityMetadata(Stream outstream)
        {
            //Console.WriteLine("_WriteSecurityMetadata({0})", FileName);
            if (_Password == null) return;
            if (Encryption == EncryptionAlgorithm.PkzipWeak)
            {
                // If PKZip (weak) encryption is in use, then the encrypted entry data
                // is preceded by 12-byte "encryption header" for the entry.

                _zipCrypto = ZipCrypto.ForWrite(_Password);

                // generate the random 12-byte header:
                var rnd = new System.Random();
                byte[] encryptionHeader = new byte[12];
                rnd.NextBytes(encryptionHeader);

                // workitem 8271
                if ((this._BitField & 0x0008) == 0x0008)
                {
                    // In the case that bit 3 of the general purpose bit flag is set to
                    // indicate the presence of a 'data
                    // descriptor' (signature 0x08074b50), the last byte of the decrypted
                    // header is sometimes compared with the high-order byte of the
                    // lastmodified time, rather than the high-order byte of the CRC, to
                    // verify the password.
                    //
                    // This is not documented in the PKWare Appnote.txt.  
                    // This was discovered this by analysis of the Crypt.c source file in the InfoZip library
                    // http://www.info-zip.org/pub/infozip/

                    // Also, winzip insists on this!
                    _TimeBlob = Ionic.Zip.SharedUtilities.DateTimeToPacked(LastModified);
                    encryptionHeader[11] = (byte)((this._TimeBlob >> 8) & 0xff);
                }
                else
                {
                    // When bit 3 is not set, the CRC value is required before
                    // encryption of the file data begins. In this case there is no way
                    // around it: must read the stream in its entirety to compute the
                    // actual CRC before proceeding.
                    FigureCrc32();
                    encryptionHeader[11] = (byte)((this._Crc32 >> 24) & 0xff);
                }

                // Encrypt the random header, INCLUDING the final byte which is either
                // the high-order byte of the CRC32, or the high-order byte of the
                // _TimeBlob.  Must do this BEFORE encrypting the file data.  This
                // step changes the state of the cipher, or in the words of the PKZIP
                // spec, it "further initializes" the cipher keys.
                
                byte[] cipherText = _zipCrypto.EncryptMessage(encryptionHeader, encryptionHeader.Length);

                // Write the ciphered bonafide encryption header. 
                outstream.Write(cipherText, 0, cipherText.Length);
            }

#if AESCRYPTO
            else if (Encryption == EncryptionAlgorithm.WinZipAes128 ||
                Encryption == EncryptionAlgorithm.WinZipAes256)
            {
                // If WinZip AES encryption is in use, then the encrypted entry data is
                // preceded by a variable-sized Salt and a 2-byte "password
                // verification" value for the entry.

                _aesCrypto = WinZipAesCrypto.Generate(_Password, _KeyStrengthInBits);
                outstream.Write(_aesCrypto.Salt, 0, _aesCrypto._Salt.Length);
                outstream.Write(_aesCrypto.GeneratedPV, 0, _aesCrypto.GeneratedPV.Length);
            }
#endif

        }


        private void CopyThroughOneEntry(Stream outstream)
        {
            int n;
            byte[] bytes = new byte[BufferSize];

            // just read from the existing input zipfile and write to the output
            Stream input = this.ArchiveStream;
            var input1 = new Ionic.Zlib.CrcCalculatorStream(input);

            // must re-compute metadata if it changed, or if the Zip64 option changed.
            if (_metadataChanged ||
        (_InputUsesZip64 && _zipfile.UseZip64WhenSaving == Zip64Option.Never) ||
                (!_InputUsesZip64 && _zipfile.UseZip64WhenSaving == Zip64Option.Always))
            {
                long origRelativeOffsetOfHeader = _RelativeOffsetOfLocalHeader;
                if (this.LengthOfHeader == 0)
                    throw new ZipException("Bad header length.");

                // The header length may change due to rename of file, add a comment, etc.
                // We need to retain the original. 
                int origLengthOfHeader = LengthOfHeader; // including crypto bytes!

                // WriteHeader() has the side effect of changing _RelativeOffsetOfLocalHeader 
                // and setting _LengthOfHeader.  While ReadHeader() reads the crypto header if
                // present, WriteHeader() does not write the crypto header.
                WriteHeader(outstream, 0);

                if (!this.FileName.EndsWith("/"))
                {
                    // not a directory, we have file data
                    // seek to the beginning of the entry data in the input stream
                    long pos = origRelativeOffsetOfHeader + origLengthOfHeader;
                    pos -= LengthOfCryptoHeaderBytes; // want to keep the crypto header
                    _LengthOfHeader += LengthOfCryptoHeaderBytes;

                    // change for workitem 8098
                    //input.Seek(pos, SeekOrigin.Begin);
                    this._zipfile.SeekFromOrigin(pos);

                    // copy through everything after the header to the output stream
                    long remaining = this._CompressedSize;

                    while (remaining > 0)
                    {
                        int len = (remaining > bytes.Length) ? bytes.Length : (int)remaining;

                        // read
                        n = input1.Read(bytes, 0, len);
                        //_CheckRead(n);

                        // write
                        outstream.Write(bytes, 0, n);
                        remaining -= n;
                        OnWriteBlock(input1.TotalBytesSlurped, this._CompressedSize);
                        if (_ioOperationCanceled)
                            break;
                    }

                    // bit 3 descriptor
                    if ((this._BitField & 0x0008) == 0x0008)
                    {
                        int size = 16;
                        if (_InputUsesZip64) size += 8;
                        byte[] Descriptor = new byte[size];
                        input.Read(Descriptor, 0, size);

                        if (_InputUsesZip64 && _zipfile.UseZip64WhenSaving == Zip64Option.Never)
                        {
                            // original descriptor was 24 bytes, now we need 16.
                            // Must check for underflow here.
                            // signature + CRC.
                            outstream.Write(Descriptor, 0, 8);

                            // Compressed
                            if (_CompressedSize > 0xFFFFFFFF)
                                throw new InvalidOperationException("ZIP64 is required");
                            outstream.Write(Descriptor, 8, 4);

                            // UnCompressed
                            if (_UncompressedSize > 0xFFFFFFFF)
                                throw new InvalidOperationException("ZIP64 is required");
                            outstream.Write(Descriptor, 16, 4);
                            _LengthOfTrailer -= 8;
                        }
                        else if (!_InputUsesZip64 && _zipfile.UseZip64WhenSaving == Zip64Option.Always)
                        {
                            // original descriptor was 16 bytes, now we need 24
                            // signature + CRC
                            byte[] pad = new byte[4];
                            outstream.Write(Descriptor, 0, 8);
                            // Compressed
                            outstream.Write(Descriptor, 8, 4);
                            outstream.Write(pad, 0, 4);
                            // UnCompressed
                            outstream.Write(Descriptor, 12, 4);
                            outstream.Write(pad, 0, 4);
                            _LengthOfTrailer += 8;
                        }
                        else
                        {
                            // same descriptor on input and output. Copy it through.
                            outstream.Write(Descriptor, 0, size);
                            //_LengthOfTrailer += size;
                        }
                    }
                }

                _TotalEntrySize = _LengthOfHeader + _CompressedFileDataSize + _LengthOfTrailer;

            }
            else
            {
                if (this.LengthOfHeader == 0)
                    throw new ZipException("Bad header length.");

                //long origRelativeOffsetOfHeader = _RelativeOffsetOfLocalHeader;

                // seek to the beginning of the entry data (header + file data) in the input stream
                //input.Seek(this._RelativeOffsetOfLocalHeader, SeekOrigin.Begin);

                // Here, we need to grab the header and fill it with real data. Some of
                // the fields may take marker values - eg, the CRC may be all zero and
                // the Uncomp and Comp sizes may be 0xFFFFFFFF.  Those are all "fake"
                // values, but we need to set the real ones into the header.  We don't
                // write the header here; instead we're just copying through.  But the
                // _EntryHeader array is used later when writing the Central Directory
                // Structure, and the header data must be correct at that point.

                // ?? that is a doomed approach !! 

                //_EntryHeader = new byte[this._LengthOfHeader];
                //n = input.Read(_EntryHeader, 0, _EntryHeader.Length);
                //_CheckRead(n);

                // once again, seek to the beginning of the entry data in the input stream
                // change for workitem 8098
                //input.Seek(this._RelativeOffsetOfLocalHeader, SeekOrigin.Begin);
                this._zipfile.SeekFromOrigin(this._RelativeOffsetOfLocalHeader);

                if (this._TotalEntrySize == 0)
                {
                    // We've never set the length of the entry.  
                    // Set it here.
                    this._TotalEntrySize = this._LengthOfHeader + this._CompressedFileDataSize + _LengthOfTrailer;

                    // The CompressedSize includes all the leading metadata associated
                    // to encryption, if any, as well as the compressed data, or
                    // compressed-then-encrypted data, and the trailer in case of AES.

                    // The CompressedFileData size is the same, less the encryption
                    // framing data (12 bytes header for PKZip; 10/18 bytes header and
                    // 10 byte trailer for AES).

                    // The _LengthOfHeader includes all the zip entry header plus the
                    // crypto header, if any.  The _LengthOfTrailer includes the
                    // 10-byte MAC for AES, where appropriate, and the bit-3
                    // Descriptor, where applicable.
                }


                // workitem 5616
                // remember the offset, within the output stream, of this particular entry header.
                // This may have changed if any of the other entries changed (eg, if a different
                // entry was removed or added.)
                var counter = outstream as CountingStream;
                _RelativeOffsetOfLocalHeader = (counter != null) ? counter.BytesWritten : outstream.Position;

                // copy through the header, filedata, trailer, everything...
                long remaining = this._TotalEntrySize;
                while (remaining > 0)
                {
                    int len = (remaining > bytes.Length) ? bytes.Length : (int)remaining;

                    // read
                    n = input1.Read(bytes, 0, len);
                    //_CheckRead(n);

                    // write
                    outstream.Write(bytes, 0, n);
                    remaining -= n;
                    //OnWriteBlock(input1.TotalBytesSlurped, this._CompressedSize);
                    OnWriteBlock(input1.TotalBytesSlurped, this._TotalEntrySize);
                    if (_ioOperationCanceled)
                        break;
                }

            }

            // zip64 housekeeping
            _entryRequiresZip64 = new Nullable<bool>
                (_CompressedSize >= 0xFFFFFFFF ||
         _UncompressedSize >= 0xFFFFFFFF ||
         _RelativeOffsetOfLocalHeader >= 0xFFFFFFFF
                );

            _OutputUsesZip64 = new Nullable<bool>(_zipfile._zip64 == Zip64Option.Always || _entryRequiresZip64.Value);

        }




        // At current cursor position in the stream, read the extra field,
        // and set the properties on the ZipEntry instance appropriately. 
        // This can be called when processing the Extra field in the Central Directory, 
        // or in the local header.
        internal int ProcessExtraField(Int16 extraFieldLength)
        {
            int additionalBytesRead = 0;

            Stream s = ArchiveStream;

            if (extraFieldLength > 0)
            {
                byte[] Buffer = this._Extra = new byte[extraFieldLength];
                additionalBytesRead = s.Read(Buffer, 0, Buffer.Length);

                int j = 0;
                while (j < Buffer.Length)
                {
                    int start = j;

                    UInt16 HeaderId = (UInt16)(Buffer[j] + Buffer[j + 1] * 256);
                    Int16 DataSize = (short)(Buffer[j + 2] + Buffer[j + 3] * 256);

                    j += 4;

                    switch (HeaderId)
                    {
                        case 0x000a:  // NTFS ctime, atime, mtime
                            // The NTFS filetimes are 64-bit unsigned integers, stored in Intel
                            // (least significant byte first) byte order. They are expressed as the
                            // number of 1.0E-07 seconds (1/10th microseconds!) past WinNT "epoch",
                            // which is "01-Jan-1601 00:00:00 UTC".
                            //
                            // HeaderId   2 bytes    0x000a == NTFS stuff
                            // Datasize   2 bytes    ?? (usually 32)
                            // reserved   4 bytes    ?? 
                            // timetag    2 bytes    0x0001 == time
                            // size       2 bytes    24 == 8 bytes each for ctime, mtime, atime
                            // mtime      8 bytes    win32 ticks since win32epoch
                            // atime      8 bytes    win32 ticks since win32epoch
                            // ctime      8 bytes    win32 ticks since win32epoch
                            {
                                if (DataSize != 32)
                                    throw new BadReadException(String.Format("  Unexpected datasize (0x{0:X4}) for NTFS times extra field at position 0x{1:X16}", DataSize, s.Position - additionalBytesRead));

                                j += 4;  // reserved
                                Int16 timetag = (Int16)(Buffer[j] + Buffer[j + 1] * 256);
                                Int16 addlsize = (Int16)(Buffer[j + 2] + Buffer[j + 3] * 256);
                                j += 4;  // tag and size

                                if (timetag == 0x0001 && addlsize == 24)
                                {
                                    Int64 z = BitConverter.ToInt64(Buffer, j);
                                    this._Mtime = DateTime.FromFileTimeUtc(z);
                                    j += 8;

                                    // At this point the library *could* set the
                                    // LastModified value to coincide with the Mtime
                                    // value.  In theory, they refer to the same
                                    // property of the file, and should be the same
                                    // anyway, allowing for differences in precision.
                                    // But they are independent quantities in the zip
                                    // archive, and this library will keep them separate
                                    // in the object model. There is no ill effect from
                                    // this, because as files are extracted, the
                                    // higher-precision value (Mtime) is used if it is
                                    // present.  Apps may wish to compare the Mtime
                                    // versus LastModified values, but any difference
                                    // when both are present is not germaine to the
                                    // correctness of the library. but note: when
                                    // explicitly setting either value, both are
                                    // set. See the setter for LastModified or
                                    // the SetNtfsTimes() method.

                                    z = BitConverter.ToInt64(Buffer, j);
                                    this._Atime = DateTime.FromFileTimeUtc(z);
                                    j += 8;

                                    z = BitConverter.ToInt64(Buffer, j);
                                    this._Ctime = DateTime.FromFileTimeUtc(z);
                                    j += 8;

                                    _ntfsTimesAreSet = true;
                                    _timestamp |= ZipEntryTimestamp.Windows;
                                    _emitNtfsTimes = true;
                                }
                            }
                            break;

                        case 0x5455:  // Unix ctime, atime, mtime
                            // The Unix filetimes are 32-bit unsigned integers,
                            // storing seconds since Unix epoch.
                            {
                                if (DataSize != 13 && DataSize != 5)
                                    throw new BadReadException(String.Format("  Unexpected datasize (0x{0:X4}) for Extended Timestamp extra field at position 0x{1:X16}", DataSize, s.Position - additionalBytesRead));

                                if (DataSize == 13)
                                {
                                    byte flag = Buffer[j++];

                                    if ((flag & 0x0001) != 0)
                                    {
                                        Int32 timet = BitConverter.ToInt32(Buffer, j);
                                        this._Mtime = _unixEpoch.AddSeconds(timet);
                                        j += 4;
                                    }

                                    if ((flag & 0x0002) != 0)
                                    {
                                        Int32 timet = BitConverter.ToInt32(Buffer, j);
                                        this._Atime = _unixEpoch.AddSeconds(timet);
                                        j += 4;
                                    }
                                    else
                                        this._Atime = DateTime.UtcNow;

                                    if ((flag & 0x0004) != 0)
                                    {
                                        Int32 timet = BitConverter.ToInt32(Buffer, j);
                                        this._Ctime = _unixEpoch.AddSeconds(timet);
                                        j += 4;
                                    }
                                    else
                                        this._Ctime = DateTime.UtcNow;

                                    _timestamp |= ZipEntryTimestamp.Unix;
                                    _ntfsTimesAreSet = true;
                                    _emitUnixTimes = true;
                                }
                                else
                                    ReadExtraField();

                            }
                            break;


                        case 0x5855:  // Info-zip Extra field (outdated)
                            // This is outdated, so the field is supported on
                            // read only. 
                            {
                                if (DataSize != 12 && DataSize != 8)
                                    throw new BadReadException(String.Format("  Unexpected datasize (0x{0:X4}) for InfoZip v1 extra field at position 0x{1:X16}", DataSize, s.Position - additionalBytesRead));

                                Int32 timet = BitConverter.ToInt32(Buffer, j);
                                this._Mtime = _unixEpoch.AddSeconds(timet);
                                j += 4;

                                timet = BitConverter.ToInt32(Buffer, j);
                                this._Atime = _unixEpoch.AddSeconds(timet);
                                j += 4;

                                this._Ctime = DateTime.UtcNow;

                                _ntfsTimesAreSet = true;
                                _timestamp |= ZipEntryTimestamp.InfoZip1;
                            }

                            break;


                        case 0x0001: // ZIP64
                            {
                                // The PKWare spec says that any of {UncompressedSize, CompressedSize,
                                // RelativeOffset} exceeding 0xFFFFFFFF can lead to the ZIP64 header,
                                // and the ZIP64 header may contain one or more of those.  If the
                                // values are present, they will be found in the prescribed order.
                                // There may also be a 4-byte "disk start number."
                                // This means that the DataSize must be 28 bytes or less.  

                                this._InputUsesZip64 = true;

                                // workitem 7941: check datasize before reading.
                                if (DataSize > 28)
                                    throw new BadReadException(String.Format("  Inconsistent datasize (0x{0:X4}) for ZIP64 extra field at position 0x{1:X16}",
                                                                             DataSize, s.Position - additionalBytesRead));
                                int remainingData = DataSize;
                                
                                if (this._UncompressedSize == 0xFFFFFFFF)
                                {
                                    if (remainingData < 8)
                                        throw new BadReadException(String.Format("  Missing data for ZIP64 extra field (Uncompressed Size) at position 0x{1:X16}",
                                                                                 s.Position - additionalBytesRead));

                                    this._UncompressedSize = BitConverter.ToInt64(Buffer, j);
                                    j += 8;
                                    remainingData -= 8;
                                }
                                if (this._CompressedSize == 0xFFFFFFFF)
                                {
                                    if (remainingData < 8)
                                        throw new BadReadException(String.Format("  Missing data for ZIP64 extra field (Compressed Size) at position 0x{1:X16}",
                                                                                 s.Position - additionalBytesRead));

                                    this._CompressedSize = BitConverter.ToInt64(Buffer, j);
                                    j += 8;
                                    remainingData -= 8;
                                }
                                if (this._RelativeOffsetOfLocalHeader == 0xFFFFFFFF)
                                {
                                    if (remainingData < 8)
                                        throw new BadReadException(String.Format("  Missing data for ZIP64 extra field (Relative Offset) at position 0x{1:X16}",
                                                                                 s.Position - additionalBytesRead));

                                    this._RelativeOffsetOfLocalHeader = BitConverter.ToInt64(Buffer, j);
                                    j += 8;
                                    remainingData -= 8;
                                }

                                // Ignore anything else. Potentially there are 4 more bytes for the
                                // disk start number.  DotNetZip currently doesn't handle multi-disk
                                // archives.
                            }
                            break;

#if AESCRYPTO
                        case 0x9901: // WinZip AES encryption is in use.  (workitem 6834)
                            // we will handle this extra field only  if compressionmethod is 0x63
                            //Console.WriteLine("Found WinZip AES Encryption header (compression:0x{0:X2})", this._CompressionMethod);
                            if (this._CompressionMethod == 0x0063)
                            {
                                if ((this._BitField & 0x01) != 0x01)
                                    throw new BadReadException(String.Format("  Inconsistent metadata at position 0x{0:X16}", s.Position - additionalBytesRead));


                                this._sourceIsEncrypted = true;

                                //this._aesCrypto = new WinZipAesCrypto(this);
                                // see spec at http://www.winzip.com/aes_info.htm
                                if (DataSize != 7)
                                    throw new BadReadException(String.Format("  Inconsistent WinZip AES datasize (0x{0:X4}) at position 0x{1:X16}", DataSize, s.Position - additionalBytesRead));

                                this._WinZipAesMethod = BitConverter.ToInt16(Buffer, j);
                                j += 2;
                                if (this._WinZipAesMethod != 0x01 && this._WinZipAesMethod != 0x02)
                                    throw new BadReadException(String.Format("  Unexpected vendor version number (0x{0:X4}) for WinZip AES metadata at position 0x{1:X16}",
                                        this._WinZipAesMethod, s.Position - additionalBytesRead));

                                Int16 vendorId = BitConverter.ToInt16(Buffer, j);
                                j += 2;
                                if (vendorId != 0x4541)
                                    throw new BadReadException(String.Format("  Unexpected vendor ID (0x{0:X4}) for WinZip AES metadata at position 0x{1:X16}", vendorId, s.Position - additionalBytesRead));

                                this._KeyStrengthInBits = -1;
                                if (Buffer[j] == 1) _KeyStrengthInBits = 128;
                                if (Buffer[j] == 3) _KeyStrengthInBits = 256;

                                if (this._KeyStrengthInBits < 0)
                                    throw new Exception(String.Format("Invalid key strength ({0})", this._KeyStrengthInBits));

                                this._Encryption = (this._KeyStrengthInBits == 128)
                                    ? EncryptionAlgorithm.WinZipAes128
                                    : EncryptionAlgorithm.WinZipAes256;

                                j++;

                                // set the actual compression method
                                this._CompressionMethod = BitConverter.ToInt16(Buffer, j);
                                j += 2; // for the next segment of the extra field 
                            }
                            break;
#endif
                        case 0x0017: // workitem 7968: handle PKWare Strong encryption header
                            //           Value     Size     Description
                            //           -----     ----     -----------
                            //           0x0017    2 bytes  Tag for this "extra" block type
                            //           TSize     2 bytes  Size of data that follows
                            //           Format    2 bytes  Format definition for this record
                            //           AlgID     2 bytes  Encryption algorithm identifier
                            //           Bitlen    2 bytes  Bit length of encryption key
                            //           Flags     2 bytes  Processing flags
                            //           CertData  TSize-8  Certificate decryption extra field data
                            //                              (refer to the explanation for CertData
                            //                               in the section describing the 
                            //                               Certificate Processing Method under 
                            //                               the Strong Encryption Specification)
                            {
                                Int16 format = (Int16)(Buffer[j] + Buffer[j + 1] * 256);
                                j += 2;
                                _UnsupportedAlgorithmId = (UInt16)(Buffer[j] + Buffer[j + 1] * 256);
                                j += 2;
                                _Encryption = EncryptionAlgorithm.Unsupported;

                                // DotNetZip doesn't support this algorithm, but we don't need to throw here.
                                // we might just be reading the archive, which is fine.  We'll need to
                                // throw if Extract() is called.
                            }
                            break;
                    }

                    // move to the next Header in the extra field
                    j = start + DataSize + 4;
                }
            }
            return additionalBytesRead;
        }



        // workitem 7968
        private string UnsupportedAlgorithm
        {
            get
            {
                string alg = String.Empty;
                switch (_UnsupportedAlgorithmId)
                {
                    case 0:
                        alg = "--";
                        break;
                    case 0x6601:
                        alg = "DES";
                        break;
                    case 0x6602: // - RC2 (version needed to extract < 5.2)
                        alg = "RC2";
                        break;
                    case 0x6603: // - 3DES 168
                        alg = "3DES-168";
                        break;
                    case 0x6609: // - 3DES 112
                        alg = "3DES-112";
                        break;
                    case 0x660E: // - AES 128
                        alg = "PKWare AES128";
                        break;
                    case 0x660F: // - AES 192
                        alg = "PKWare AES192";
                        break;
                    case 0x6610: // - AES 256
                        alg = "PKWare AES256";
                        break;
                    case 0x6702: // - RC2 (version needed to extract >= 5.2)
                        alg = "RC2";
                        break;
                    case 0x6720: // - Blowfish
                        alg = "Blowfish";
                        break;
                    case 0x6721: // - Twofish
                        alg = "Twofish";
                        break;
                    case 0x6801: // - RC4
                        alg = "RC4";
                        break;
                    case 0xFFFF: // - Unknown algorithm
                    default:
                        alg = String.Format("Unknown (0x{0:X4})", _UnsupportedAlgorithmId);
                        break;
                }
                return alg;
            }
        }


        private void SetFdpLoh()
        {
            // The value for FileDataPosition has not yet been set. 
            // Therefore, seek to the local header, and figure the start of file data.
            // workitem 8098: ok (restore)
            long origPosition = this.ArchiveStream.Position;
            try
            {
                // change for workitem 8098
                //this.ArchiveStream.Seek(this._RelativeOffsetOfLocalHeader, SeekOrigin.Begin);
                this._zipfile.SeekFromOrigin(this._RelativeOffsetOfLocalHeader);

            }
            catch (System.Exception exc1)
            {
                string description = String.Format("Exception seeking  entry({0}) offset(0x{1:X8}) len(0x{2:X8})",
                                                   this.FileName, this._RelativeOffsetOfLocalHeader,
                                                   this.ArchiveStream.Length);
                throw new Exception(description, exc1);
            }

            byte[] block = new byte[30];
            this.ArchiveStream.Read(block, 0, block.Length);

            // At this point we could verify the contents read from the local header
            // with the contents read from the central header.  We could, but don't need to. 
            // So we won't.

            Int16 filenameLength = (short)(block[26] + block[27] * 256);
            Int16 extraFieldLength = (short)(block[28] + block[29] * 256);

            this.ArchiveStream.Seek(filenameLength + extraFieldLength, SeekOrigin.Current);
            this._LengthOfHeader = 30 + extraFieldLength + filenameLength +
                LengthOfCryptoHeaderBytes;

            // workitem 8098: ok (arithmetic)
            this.__FileDataPosition = _RelativeOffsetOfLocalHeader + _LengthOfHeader;

            // restore file position:
            // workitem 8098: ok (restore)
            this.ArchiveStream.Seek(origPosition, SeekOrigin.Begin);
        }




        internal int LengthOfCryptoHeaderBytes
        {
            get
            {
                if ((_BitField & 0x01) != 0x01) return 0;

#if AESCRYPTO
                if (Encryption == EncryptionAlgorithm.WinZipAes128 ||
                    Encryption == EncryptionAlgorithm.WinZipAes256)
                {
                    int sizeOfSaltAndPv = ((_KeyStrengthInBits / 8 / 2) + 2);
                    return sizeOfSaltAndPv;
                }
#endif
                if (Encryption == EncryptionAlgorithm.PkzipWeak)
                    return 12;
                throw new ZipException("internal error");
            }
        }


        internal long FileDataPosition
        {
            get
            {
                if (__FileDataPosition == -1)
                    SetFdpLoh();

                return __FileDataPosition;
            }
        }

        private int LengthOfHeader
        {
            get
            {
                if (_LengthOfHeader == 0)
                    SetFdpLoh();

                return _LengthOfHeader;
            }
        }



        internal ZipCrypto _zipCrypto;
#if AESCRYPTO
        internal WinZipAesCrypto _aesCrypto;
        internal Int16 _KeyStrengthInBits;
        private Int16 _WinZipAesMethod;
#endif

        internal DateTime _LastModified;
        private DateTime _Mtime, _Atime, _Ctime;  // workitem 6878: NTFS quantities
        private bool _ntfsTimesAreSet;
        private bool _emitNtfsTimes = true;
        private bool _emitUnixTimes;  // by default, false
        private bool _TrimVolumeFromFullyQualifiedPaths = true;  // by default, trim them.
        private bool _ForceNoCompression;  // by default, false: do compression if it makes sense.
        internal string _LocalFileName;
        private string _FileNameInArchive;
        internal Int16 _VersionNeeded;
        internal Int16 _BitField;
        internal Int16 _CompressionMethod;
        internal string _Comment;
        private bool _IsDirectory;
        private byte[] _CommentBytes;
        internal Int64 _CompressedSize;
        internal Int64 _CompressedFileDataSize; // CompressedSize less 12 bytes for the encryption header, if any
        internal Int64 _UncompressedSize;
        internal Int32 _TimeBlob;
        private bool _crcCalculated = false;
        internal Int32 _Crc32;
        internal byte[] _Extra;
        private bool _metadataChanged;
        private bool _restreamRequiredOnSave;
        private bool _sourceIsEncrypted;
        //private long _cdrPosition;

        private static System.Text.Encoding ibm437 = System.Text.Encoding.GetEncoding("IBM437");
        private System.Text.Encoding _provisionalAlternateEncoding = System.Text.Encoding.GetEncoding("IBM437");
        private System.Text.Encoding _actualEncoding = null;

        internal ZipFile _zipfile;
        internal long __FileDataPosition = -1;
        private byte[] _EntryHeader;
        internal Int64 _RelativeOffsetOfLocalHeader;
        private Int64 _TotalEntrySize;
        internal int _LengthOfHeader;
        internal int _LengthOfTrailer;
        private bool _InputUsesZip64;
        private UInt32 _UnsupportedAlgorithmId;

        internal string _Password;
        internal ZipEntrySource _Source = ZipEntrySource.None;
        internal EncryptionAlgorithm _Encryption = EncryptionAlgorithm.None;
        internal byte[] _WeakEncryptionHeader;
        internal Stream _archiveStream;
        private Stream _sourceStream;
        private Nullable<Int64> _sourceStreamOriginalPosition;
        private bool _sourceWasJitProvided;
        private object LOCK = new object();
        private bool _ioOperationCanceled;
        private bool _presumeZip64;
        private Nullable<bool> _entryRequiresZip64;
        private Nullable<bool> _OutputUsesZip64;
        private bool _IsText; // workitem 7801
        private ZipEntryTimestamp _timestamp;

        private static System.DateTime _unixEpoch = new System.DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);


        // summary
        // The default size of the IO buffer for ZipEntry instances. Currently it is 8192 bytes.
        // summary
        //public const int IO_BUFFER_SIZE_DEFAULT = 8192; // 0x8000; // 0x4400

    }


    /// <summary>
    /// An enum that specifies the source of the ZipEntry. 
    /// </summary>
    public enum ZipEntrySource
    {
        /// <summary>
        /// Default value.  Invalid on a bonafide ZipEntry.
        /// </summary>
        None = 0,

        /// <summary>
        /// The entry was instantiated by calling AddFile() or another method that 
        /// added an entry from the filesystem.
        /// </summary>
        Filesystem,

        /// <summary>
        /// The entry was instantiated via <see cref="ZipFile.AddEntry(string,string,string)"/> or
        /// <see cref="ZipFile.AddEntry(string,string,System.IO.Stream)"/> .
        /// </summary>
        Stream,

        /// <summary>
        /// The ZipEntry was instantiated by reading a zipfile.
        /// </summary>
        Zipfile,
    }



    /// <summary>
    /// An enum that specifies the type of timestamp available on the ZipEntry. 
    /// </summary>
    [Flags]
    public enum ZipEntryTimestamp
    {
        /// <summary>
        /// Default value.  
        /// </summary>
        None = 0,

        /// <summary>
        /// A DOS timestamp with 2-second precision.
        /// </summary>
        DOS = 1,

        /// <summary>
        /// A Windows timestamp with 100-ns precision.
        /// </summary>
        Windows = 2,

        /// <summary>
        /// A Unix timestamp with 1-second precision.
        /// </summary>
        Unix = 4,

        /// <summary>
        /// A Unix timestamp with 1-second precision, stored in InfoZip v1 format.  This
        /// format is outdated and is supported for reading archives only.  
        /// </summary>
        InfoZip1 = 8,
    }





#if NETCF
    internal class NetCfFile
    {
        public static int SetTimes(string filename, DateTime ctime, DateTime atime, DateTime mtime)
        {
            IntPtr hFile  = (IntPtr) CreateFileCE(filename, 
                                                  (uint)FileAccess.Write, 
                                                  (uint)FileShare.Write, 
                                                  0, 
                                                  (uint) 3,  // == open existing
                                                  (uint)0, // flagsAndAttributes 
                                                  0);

            if((int)hFile == -1)
            {
                // workitem 7944: don't throw on failure to set file times
                // throw new ZipException("CreateFileCE Failed");
                return System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            }
                        
            SetFileTime(hFile, 
                        BitConverter.GetBytes(ctime.ToFileTime()), 
                        BitConverter.GetBytes(atime.ToFileTime()), 
                        BitConverter.GetBytes(mtime.ToFileTime()));

            CloseHandle(hFile);
            return 0;
        }


        public static int SetLastWriteTime(string filename, DateTime mtime)
        {
            IntPtr hFile  = (IntPtr) CreateFileCE(filename, 
                                                  (uint)FileAccess.Write, 
                                                  (uint)FileShare.Write, 
                                                  0, 
                                                  (uint) 3,  // == open existing
                                                  (uint)0, // flagsAndAttributes 
                                                  0);

            if((int)hFile == -1)
            {
                // workitem 7944: don't throw on failure to set file time
                // throw new ZipException(String.Format("CreateFileCE Failed ({0})",
                //                                      System.Runtime.InteropServices.Marshal.GetLastWin32Error()));
                return System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            }
                        
            SetFileTime(hFile, null, null, 
                        BitConverter.GetBytes(mtime.ToFileTime()));

            CloseHandle(hFile);
            return 0;
        }


        [System.Runtime.InteropServices.DllImport("coredll.dll", EntryPoint="CreateFile", SetLastError=true)]
        internal static extern int CreateFileCE(string lpFileName,
                                                uint dwDesiredAccess,
                                                uint dwShareMode,
                                                int lpSecurityAttributes,
                                                uint dwCreationDisposition,
                                                uint dwFlagsAndAttributes,
                                                int hTemplateFile);


        [System.Runtime.InteropServices.DllImport("coredll", EntryPoint="GetFileAttributes", SetLastError=true)]
        internal static extern uint GetAttributes(string lpFileName);

        [System.Runtime.InteropServices.DllImport("coredll", EntryPoint="SetFileAttributes", SetLastError=true)]
        internal static extern bool SetAttributes(string lpFileName, uint dwFileAttributes);

        [System.Runtime.InteropServices.DllImport("coredll", EntryPoint="SetFileTime", SetLastError=true)]
        internal static extern bool SetFileTime(IntPtr hFile, byte[] lpCreationTime, byte[] lpLastAccessTime, byte[] lpLastWriteTime); 

        [System.Runtime.InteropServices.DllImport("coredll.dll", SetLastError=true)]
        internal static extern bool CloseHandle(IntPtr hObject);

    }
#endif



}
