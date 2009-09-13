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
// Time-stamp: <2009-September-11 12:35:36>
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
using Interop=System.Runtime.InteropServices;

namespace Ionic.Zip
{
    /// <summary>
    /// Represents a single entry in a ZipFile. Typically, applications get a ZipEntry
    /// by enumerating the entries within a ZipFile, or by adding an entry to a ZipFile.
    /// </summary>

    [Interop.GuidAttribute("ebc25cf6-9120-4283-b972-0e5520d00004")]
    [Interop.ComVisible(true)]
#if !NETCF
    [Interop.ClassInterface(Interop.ClassInterfaceType.AutoDispatch)]
#endif    
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
        /// time is accessible via the <c>ModifiedTime</c> property.  Zip tools and libraries will
        /// always at least handle (read or write) the DOS time, and may also handle the
        /// Win32 time. When reading ZIP files, The DotNetZip library reads the Win32 time,
        /// if it is stored in the entry, and sets both <c>LastModified</c> and <c>ModifiedTime</c>
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
        /// <para>
        ///   This value corresponds to the "last modified" time in the NTFS file times
        ///   as described in <see
        ///   href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">the Zip
        ///   specification</see>.  When getting this property, the value may be
        ///   different from <see cref="LastModified" />.  When setting the property,
        ///   the <see cref="LastModified"/> property also gets set, but with a lower
        ///   precision.
        /// </para>
        ///
        /// <para>
        ///   Let me explain. It's going to take a while, so get
        ///   comfortable. Originally, waaaaay back in 1989 when the ZIP specification
        ///   was originally described by the esteemed Mr. Phil Katz, the dominant
        ///   operating system of the time was MS-DOS. MSDOS stored file times with a
        ///   2-second precision, because, c'mon, <em>who is ever going to need better
        ///   resolution than THAT?</em> And so ZIP files, regardless of the platform on
        ///   which the zip file was created, store file times in exactly <see
        ///   href="http://www.vsft.com/hal/dostime.htm">the same format that DOS used
        ///   in 1989</see>.
        /// </para>
        ///
        /// <para>
        ///   Since then, the ZIP spec has evolved, but the internal format for file
        ///   timestamps remains the same.  Despite the fact that the way times are
        ///   stored in a zip file is rooted in DOS heritage, any program on any
        ///   operating system can format a time in this way, and most zip tools and
        ///   libraries DO - they round file times to the nearest even second and store
        ///   it just like DOS did 25+ years ago.
        /// </para>
        ///
        /// <para>
        ///   PKWare extended the ZIP specification to allow a zip file to store what
        ///   are called "NTFS Times" and "Unix(tm) times" for a file.  These are the
        ///   <em>last write</em>, <em>last access</em>, and <em>file creation</em>
        ///   times of a particular file. These metadata are not actually specific to
        ///   NTFS or Unix. They are tracked for each file by NTFS and by various Unix
        ///   filesystems, but they are also tracked by other filesystems, too.  The key
        ///   point is that the times are <em>formatted in the zip file</em> in the same
        ///   way that NTFS formats the time (ticks since win32 epoch), or in the same
        ///   way that Unix formats the time (seconds since Unix epoch).
        /// </para>
        ///
        /// <para>
        ///   These extended times are higher precision quantities than the DOS time.
        ///   As described above, the (DOS) LastModified has a precision of 2 seconds.
        ///   The Unix time is stored with a precision of 1 second. The NTFS time is
        ///   stored with a precision of 0.0000001 seconds. The quantities are easily
        ///   convertible, except for the loss of precision you may incur. 
        /// </para>
        ///
        /// <para>
        ///   A zip archive can store the {C,A,M} times in NTFS format, in Unix format,
        ///   or not at all.  Often a tool running on Unix or Mac will embed the times
        ///   in Unix format (1 second precision), while WinZip running on Windows might
        ///   embed the times in NTFS format (precision of of 0.0000001 seconds).  When
        ///   reading a zip file with these "extended" times, in either format,
        ///   DotNetZip represents the values with the
        ///   <c>ModifiedTime</c>, <c>AccessedTime</c> and <c>CreationTime</c>
        ///   properties on the <c>ZipEntry</c>.
        /// </para>
        ///
        /// <para>
        ///   As with the DOS time, any tool or library running on any operating system
        ///   is capable of formatting a time in one of these ways and embedding it into
        ///   the zip file. The key is, not all zip tools or libraries support all these
        ///   formats.  Storing the higher-precision times for each entry is optional
        ///   for zip files, and many tools and libraries don't use the higher precision
        ///   quantities at all, though it is much nicer than the DOS time.  And futher,
        ///   there are also cases where the timestamp of the file entry is not known,
        ///   and is not stored. For example, this happens when content for the entry is
        ///   obtained from a stream.  The bottom line is that the higher-resolution
        ///   times, in either format, are not guaranteed to be present for a ZipEntry.
        ///   The old DOS time, represented by <see cref="LastModified"/>, is guaranteed
        ///   to be present, though it sometimes unset.
        /// </para>
        ///
        /// <para>
        ///   Ok, getting back to the question about how the <c>LastModified</c>
        ///   property relates to this <c>ModifiedTime</c>
        ///   property... <c>LastModified</c> is always set, while <c>ModifiedTime</c>
        ///   is not. (The other times stored in the <em>NTFS times extension</em>,
        ///   <c>CreationTime</c> and <c>AccessedTime</c> also may not be set on an
        ///   entry that is read from an existing zip file.) When reading a zip file,
        ///   then <c>LastModified</c> takes the DOS time that is stored with the
        ///   file. If the DOS time has been stored as zero in the zipfile, then this
        ///   library will use <c>DateTime.Now</c> for the <c>LastModified</c> value.
        ///   If the ZIP file was created by an evolved tool, then there will also be
        ///   NTFS times in the zip file.  In that case, this library will read those
        ///   times, and set <c>LastModified</c> and <c>ModifiedTime</c> to the same
        ///   value, the one corresponding to the last write time of the file.  If there
        ///   are no "NTFS times" stored for the entry, then <c>ModifiedTime</c> remains
        ///   unset (likewise <c>AccessedTime</c> and <c>CreationTime</c>), and
        ///   <c>LastModified</c> keeps its DOS time.
        /// </para>
        ///
        /// <para>
        ///   When creating zip files with this library, by default the extended time
        ///   properties (<c>ModifiedTime</c>, <c>AccessedTime</c>, and
        ///   <c>CreationTime</c>) are set on the ZipEntry instance, and these data are
        ///   stored in the zip archive for each entry, in NTFS format. If you add an
        ///   entry from an actual filesystem file, then the entry gets the actual file
        ///   times for that file, to NTFS-level precision.  If you add an entry from a
        ///   stream, or a string, then the times get the value <c>DateTime.Now</c>.  In
        ///   this case <c>LastModified</c> and <c>ModifiedTime</c> will be identical,
        ///   to 2 seconds of precision.  You can explicitly set the
        ///   <c>CreationTime</c>, <c>AccessedTime</c>, and <c>ModifiedTime</c> of an
        ///   entry using the property setters.  If you want to set all of those
        ///   quantities, it's more efficient to use the <see
        ///   cref="SetEntryTimes(DateTime, DateTime, DateTime)"/> method.  Those
        ///   changes are not made permanent in the zip file until you call <see
        ///   cref="ZipFile.Save()"/> or one of its cousins.
        /// </para>
        ///
        /// <para>
        ///   When creating a zip file, you can override the default behavior for
        ///   formatting times in the zip file, disabling the embedding of file times in
        ///   NTFS format or enabling the storage of file times in Unix format, or both.
        ///   You may want to do this, for example, when creating a zip file on Windows,
        ///   that will be consumed on a Mac, by an application that is not hip to the
        ///   "NTFS times" format. To do this, use the <see
        ///   cref="EmitTimesInWindowsFormatWhenSaving"/> and <see
        ///   cref="EmitTimesInUnixFormatWhenSaving"/> properties.  A valid zip file may
        ///   store the file times in both formats.  But, there are no guarantees that a
        ///   program running on Mac or Linux will gracefully handle the NTFS Formatted
        ///   times, or that a non-DotNetZip-powered application running on Windows will
        ///   be able to handle file times in Unix format. When in doubt, test.
        /// </para>
        /// 
        /// <para>
        ///   I'll bet you didn't think one person could type so much about time, eh?
        ///   And reading it was so enjoyable, too!  Well, in appreciation, <see
        ///   href="http://cheeso.members.winisp.net/DotNetZipDonate.aspx">maybe you
        ///   should donate</see>?
        /// </para>
        /// </remarks>
        ///
        /// <seealso cref="AccessedTime"/>
        /// <seealso cref="CreationTime"/>
        /// <seealso cref="Ionic.Zip.ZipEntry.LastModified"/>
        /// <seealso cref="SetEntryTimes"/>
        public DateTime ModifiedTime
        {
            get { return _Mtime; }
            set
            {
                SetEntryTimes(_Ctime, _Atime, value);
            }
        }

        /// <summary>
        /// Last Access time for the file represented by the entry.
        /// </summary>
        /// <remarks>
        /// This value may or may not be meaningful.  If the <c>ZipEntry</c> was read from an existing
        /// Zip archive, this information may not be available. For an explanation of why, see
        /// <see cref="ModifiedTime"/>.
        /// </remarks>
        /// <seealso cref="ModifiedTime"/>
        /// <seealso cref="CreationTime"/>
        /// <seealso cref="SetEntryTimes"/>
        public DateTime AccessedTime {
            get { return _Atime; }
            set
            {
                SetEntryTimes(_Ctime, value, _Mtime);
            }
        }

        /// <summary>
        /// The file creation time for the file represented by the entry.
        /// </summary>
        ///
        /// <remarks>
        /// This value may or may not be meaningful.  If the <c>ZipEntry</c> was read
        /// from an existing zip archive, and the creation time was not set on the entry
        /// when the zip file was created, then this property may be meaningless. For an
        /// explanation of why, see <see cref="ModifiedTime"/>.
        /// </remarks>
        /// <seealso cref="ModifiedTime"/>
        /// <seealso cref="AccessedTime"/>
        /// <seealso cref="SetEntryTimes"/>
        public DateTime CreationTime {
            get { return _Ctime; }
            set
            {
                SetEntryTimes(value, _Atime, _Mtime);
            }
        }

        /// <summary>
        ///   Sets the NTFS Creation, Access, and Modified times for the given entry.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   When adding an entry from a file or directory, the Creation, Access, and
        ///   Modified times for the given entry are automatically set from the
        ///   filesystem values. When adding an entry from a stream or string, the
        ///   values are implicitly set to DateTime.Now.  The application may wish to
        ///   set these values to some arbitrary value, before saving the archive, and
        ///   can do so using the various setters.  If you want to set all of the times,
        ///   this method is more efficient.
        /// </para>
        ///
        /// <para>
        ///   The values you set here will be retrievable with the <see
        ///   cref="ModifiedTime"/>, <see cref="CreationTime"/> and <see
        ///   cref="AccessedTime"/> properties.
        /// </para>
        ///
        /// <para>
        ///   When this method is called, if both <see
        ///   cref="EmitTimesInWindowsFormatWhenSaving"/> and <see
        ///   cref="EmitTimesInUnixFormatWhenSaving"/> are false, then the
        ///   <c>EmitTimesInWindowsFormatWhenSaving</c> flag is automatically set.
        /// </para>
        ///
        /// <para>
        ///   DateTime values provided here without a DateTimeKind are assumed to be Local Time.
        /// </para>
        ///
        /// </remarks>
        /// <param name="created">the creation time of the entry.</param>
        /// <param name="accessed">the last access time of the entry.</param>
        /// <param name="modified">the last modified time of the entry.</param>
        ///
        /// <seealso cref="EmitTimesInWindowsFormatWhenSaving" />
        /// <seealso cref="EmitTimesInUnixFormatWhenSaving" />
        /// <seealso cref="AccessedTime"/>
        /// <seealso cref="CreationTime"/>
        /// <seealso cref="ModifiedTime"/>
        public void SetEntryTimes(DateTime created, DateTime accessed, DateTime modified)
        {
            _ntfsTimesAreSet = true;
            if (created == _zeroHour &&  created.Kind == _zeroHour.Kind) created = _win32Epoch;
            if (accessed == _zeroHour && accessed.Kind == _zeroHour.Kind) accessed = _win32Epoch;
            if (modified == _zeroHour && modified.Kind == _zeroHour.Kind) modified = _win32Epoch;
            _Ctime = created.ToUniversalTime();
            _Atime = accessed.ToUniversalTime();
            _Mtime = modified.ToUniversalTime();
            _LastModified = _Mtime;
            if (!_emitUnixTimes && !_emitNtfsTimes)
                _emitNtfsTimes = true;
            _metadataChanged = true;
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
        /// cref="CreationTime"/>), Access (<see cref="AccessedTime"/>), and Modified (<see
        /// cref="ModifiedTime"/>) times for the given entry are automatically set from the
        /// filesystem values. When adding an entry from a stream or string, all three
        /// values are implicitly set to DateTime.Now.  Applications can also explicitly
        /// set those times by calling <see cref="SetEntryTimes(DateTime, DateTime,
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
        /// the <see href="http://www.info-zip.org/">Infozip</see> tools can read the
        /// Unix format timestamps. Although the time values are easily convertible,
        /// subject to a loss of precision, some tools and libraries may be able to read
        /// only one or the other.
        /// </para>
        ///
        /// <para>
        /// The times stored are taken from <see cref="ModifiedTime"/>, <see
        /// cref="AccessedTime"/>, and <see cref="CreationTime"/>.
        /// </para>
        ///
        /// <para>
        ///   This property is not mutually exclusive from the <see
        ///   cref="ZipEntry.EmitTimesInUnixFormatWhenSaving"/> property.  It is
        ///   possible that a zip entry can embed the timestamps in both forms, one
        ///   form, or neither.  But, there are no guarantees that a program running on
        ///   Mac or Linux will gracefully handle NTFS Formatted times, or that a
        ///   non-DotNetZip-powered application running on Windows will be able to
        ///   handle file times in Unix format. When in doubt, test.
        /// </para>
        ///
        /// <para>
        /// Normally you will use the <see
        /// cref="ZipFile.EmitTimesInWindowsFormatWhenSaving">ZipFile.EmitTimesInWindowsFormatWhenSaving</see>
        /// property, to specify the behavior for all entries in a zip, rather than the
        /// property on each individual entry.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <seealso cref="SetEntryTimes(DateTime, DateTime, DateTime)"/>
        /// <seealso cref="EmitTimesInUnixFormatWhenSaving"/>
        /// <seealso cref="CreationTime"/>
        /// <seealso cref="AccessedTime"/>
        /// <seealso cref="ModifiedTime"/>
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
        /// cref="CreationTime"/>), Access (<see cref="AccessedTime"/>), and Modified (<see
        /// cref="ModifiedTime"/>) times for the given entry are automatically set from the
        /// filesystem values. When adding an entry from a stream or string, all three
        /// values are implicitly set to DateTime.Now.  Applications can also explicitly
        /// set those times by calling <see cref="SetEntryTimes(DateTime, DateTime,
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
        /// the <see href="http://www.info-zip.org/">Infozip</see> tools can read the
        /// Unix format timestamps. Although the time values are easily convertible,
        /// subject to a loss of precision, some tools and libraries may be able to read
        /// only one or the other.
        /// </para>
        ///
        /// <para>
        /// The times stored are taken from <see cref="ModifiedTime"/>, <see
        /// cref="AccessedTime"/>, and <see cref="CreationTime"/>.
        /// </para>
        ///
        /// <para>
        ///   This property is not mutually exclusive from the <see
        ///   cref="ZipEntry.EmitTimesInWindowsFormatWhenSaving"/> property.  It is
        ///   possible that a zip entry can embed the timestamps in both forms, one
        ///   form, or neither.  But, there are no guarantees that a program running on
        ///   Mac or Linux will gracefully handle NTFS Formatted times, or that a
        ///   non-DotNetZip-powered application running on Windows will be able to
        ///   handle file times in Unix format. When in doubt, test.
        /// </para>
        ///
        /// <para>
        /// Normally you will use the <see
        /// cref="ZipFile.EmitTimesInUnixFormatWhenSaving">ZipFile.EmitTimesInUnixFormatWhenSaving</see>
        /// property, to specify the behavior for all entries, rather than the property
        /// on each individual entry.
        /// </para>
        /// </remarks>
        ///
        /// <seealso cref="SetEntryTimes(DateTime, DateTime, DateTime)"/>
        /// <seealso cref="EmitTimesInWindowsFormatWhenSaving"/>
        /// <seealso cref="ZipFile.EmitTimesInUnixFormatWhenSaving"/>
        /// <seealso cref="CreationTime"/>
        /// <seealso cref="AccessedTime"/>
        /// <seealso cref="ModifiedTime"/>
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
        /// string, the Attributes are not set implicitly.  Regardless of the way an
        /// entry was added to a <c>ZipFile</c>, you can set the attributes explicitly
        /// if you like.
        /// </para>
        ///
        /// <para>
        /// When reading a <c>ZipEntry</c> from a <c>ZipFile</c>, the attributes are set
        /// according to the data stored in the <c>ZipFile</c>. If you extract the entry from
        /// the archive to a filesystem file, DotNetZip will set the attributes on the
        /// resulting file accordingly.
        /// </para>
        ///
        /// <para>
        /// The attributes can be set explicitly by the application.
        /// For example the application may wish to set the
        /// <c>FileAttributes.ReadOnly</c> bit for all entries added to an archive, so that on
        /// unpack, this attribute will be set on the extracted file.  Any changes you
        /// make to this property are made permanent only when you call a <c>Save()</c> method
        /// on the <c>ZipFile</c> instance that contains the ZipEntry.
        /// </para>
        ///
        /// <para>
        /// For example, an application may wish to zip up a directory and set the
        /// ReadOnly bit on every file in the archive, so that upon later extraction,
        /// the resulting files will be marked as ReadOnly.  Not every extraction tool
        /// respects these attributes, but if you unpack with DotNetZip, as for example
        /// in a self-extracting archive, then the attributes will be set as they are
        /// stored in the <c>ZipFile</c>.
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
        internal string LocalFileName
        {
            get { return _LocalFileName; }
        }

        /// <summary>
        /// The name of the file contained in the ZipEntry. 
        /// </summary>
        /// 
        /// <remarks>
        ///
        /// <para>
        /// This is the name of the entry in the <c>ZipFile</c> itself.  When creating a zip
        /// archive, if the <c>ZipEntry</c> has been created from a filesystem file, via a
        /// call to <see cref="ZipFile.AddFile(String,String)"/> or <see
        /// cref="ZipFile.AddItem(String,String)"/>, or a related overload, the value of this
        /// property is derived from the name of that file. The <c>FileName</c> property does
        /// not include drive letters, and may include a different directory path, depending
        /// on the value of the <c>directoryPathInArchive</c> parameter used when adding the
        /// entry into the <c>ZipFile</c>.
        /// </para>
        ///
        /// <para>
        /// In some cases there is no related filesystem file - for example when a
        /// <c>ZipEntry</c> is created using <see cref="ZipFile.AddEntry(string, string,
        /// string)"/> or one of
        /// the similar overloads.  In this case, the value of this property is derived from
        /// the fileName and the directory path passed to that method.
        /// </para>
        ///
        /// <para>
        /// When reading a zip file, this property takes the value of the entry name as
        /// stored in the zip file. If you extract such an entry, the extracted file will
        /// take the name given by this property.
        /// </para>
        ///
        /// <para>
        /// Applications can set this property when creating new zip archives or when reading
        /// existing archives. When setting this property, the actual value that is set will
        /// replace backslashes with forward slashes, in accordance with <see
        /// href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">the Zip
        /// specification</see>, for compatibility with Unix(tm) and ... get this.... Amiga!
        /// </para>
        ///
        /// <para>
        /// If an application reads a <c>ZipFile</c> via <see cref="ZipFile.Read(String)"/>
        /// or a related overload, and then explicitly sets the FileName on an entry
        /// contained within the <c>ZipFile</c>, and then calls <see cref="ZipFile.Save()"/>,
        /// the application will effectively rename the entry within the zip archive.
        /// </para>
        ///
        /// <para>
        /// If an application sets the value of <c>FileName</c>, then calls <c>Extract()</c>
        /// on the entry, the entry is extracted to a file using the newly set value as the
        /// filename.  The <c>FileName</c> value is made permanent in the zip archive only
        /// <em>after</em> a call to one of the <c>ZipFile.Save()</c> methods on the
        /// <c>ZipFile</c> that contains the ZipEntry.
        /// </para>
        ///
        /// <para>
        /// If an application attempts to set the <c>FileName</c> to a value that would
        /// result in a duplicate entry in the <c>ZipFile</c>, an exception is thrown.
        /// </para>
        ///
        /// </remarks>

        public string FileName
        {
            get { return _FileNameInArchive; }
            set
            {
                // rename the entry!
                if (String.IsNullOrEmpty(value)) throw new ZipException("The FileName must be non empty and non-null.");

                var filename = ZipEntry.NameInArchive(value, null);
                // workitem 8180
                if (_FileNameInArchive == filename) return; // nothing to do
                
                // workitem 8047 - renaming to a name that already exists
                if (this._zipfile.EntryFileNames.Contains(filename))
                    throw new ZipException(String.Format("Cannot rename {0} to {1}; an entry by that name already exists in the archive.", _FileNameInArchive, filename));
                        //this._zipfile.RemoveEntry(filename);
                
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
        /// <para> Now imagine a different application that creates a <c>ZipFile</c> with
        /// content obtained from hundreds of streams, added through <see
        /// cref="ZipFile.AddEntry(string, string, System.IO.Stream)"/>.
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
        /// cref="ZipFile.AddEntry(string, string, System.IO.Stream)"/> method.  At the
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
                if (this._Source == ZipEntrySource.ZipFile && _sourceIsEncrypted)
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
        /// <c>Save()</c> on the containing ZipFile. When reading an existing zip file, the value
        /// of this property reflects the stored CRC for the entry.  </para>
        ///
        /// </remarks>
        public Int32 Crc
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
                if (this._Source == ZipEntrySource.ZipFile && _sourceIsEncrypted)
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
                    if (this._Source == ZipEntrySource.ZipFile && !_sourceIsEncrypted)
                        _restreamRequiredOnSave = true;

                    if (Encryption == EncryptionAlgorithm.None)
                    {
                        _Encryption = EncryptionAlgorithm.PkzipWeak;
                    }
                }

            }
        }



        /// <summary>
        /// The action the library should take when extracting a file that already exists.
        /// </summary>
        ///
        /// <remarks>
        ///   <para>
        ///     This property affects the behavior of the Extract methods (one of the
        ///     <c>Extract()</c> or <c>ExtractWithPassword()</c> overloads), when
        ///     extraction would would overwrite an existing filesystem file. If you do
        ///     not set this property, the library throws an exception when extracting
        ///     an entry would overwrite an existing file.
        ///   </para>
        ///
        ///   <para>
        ///     This property has no effect when extracting to a stream, or when the file to be
        ///     extracted does not already exist. 
        ///   </para>
        ///
        /// </remarks>
        /// <seealso cref="Ionic.Zip.ZipFile.ExtractExistingFile"/>
        ///
        /// <example>
        /// This example shows how to set the <c>ExtractExistingFile</c> property in an
        /// <c>ExtractProgress</c> event, in response to user input. The <c>ExtractProgress</c> event
        /// is invoked if and only if the <c>ExtractExistingFile</c> property was previously
        /// set to <c>ExtractExistingFileAction.InvokeExtractProgressEvent</c>.
        /// <code lang="C#">
        /// public static void ExtractProgress(object sender, ExtractProgressEventArgs e)
        /// {
        ///     if (e.EventType == ZipProgressEventType.Extracting_BeforeExtractEntry)
        ///         Console.WriteLine("extract {0} ", e.CurrentEntry.FileName);
        ///     
        ///     else if (e.EventType == ZipProgressEventType.Extracting_ExtractEntryWouldOverwrite)
        ///     {
        ///         ZipEntry entry = e.CurrentEntry;
        ///         string response = null;
        ///         // Ask the user if he wants overwrite the file 
        ///         do
        ///         {
        ///             Console.Write("Overwrite {0} in {1} ? (y/n/C) ", entry.FileName, e.ExtractLocation);
        ///             response = Console.ReadLine();
        ///             Console.WriteLine();
        ///             
        ///         } while (response != null &amp;&amp; response[0]!='Y' &amp;&amp;
        ///                  response[0]!='N' &amp;&amp; response[0]!='C');
        ///
        ///         if  (response[0]=='C')
        ///             e.Cancel = true;
        ///         else if (response[0]=='Y')
        ///             entry.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
        ///         else
        ///             entry.ExtractExistingFile= ExtractExistingFileAction.DoNotOverwrite;
        ///     }
        /// }
        /// </code>
        /// </example>
        public ExtractExistingFileAction ExtractExistingFile
        {
            get;
            set;
        }


        /// <summary>
        ///   The action to take when an error is encountered while
        ///   opening or reading files as they are saved into a zip archive. 
        /// </summary>
        ///
        /// <remarks>
        ///  <para>
        ///     Errors can occur within a call to <see
        ///     cref="ZipFile.Save()">ZipFile.Save</see>, as the various files contained
        ///     in a ZipFile are being saved into the zip archive.  During the
        ///     <c>Save</c>, DotNetZip will perform a <c>File.Open</c> on the file
        ///     associated to the ZipEntry, and then will read the entire contents of
        ///     the file as it is zipped. Either the open or the Read may fail, because
        ///     of lock conflicts or other reasons.  Using this property, you can
        ///     specify the action to take when such errors occur.
        ///  </para>
        ///
        ///  <para>
        ///     Typically you will NOT set this property on individual ZipEntry
        ///     instances.  Instead, you will set the <see
        ///     cref="ZipFile.ZipErrorAction">ZipFile.ZipErrorAction</see> property on
        ///     the ZipFile instance, before adding any entries to the
        ///     <c>ZipFile</c>. If you do this, errors encountered on behalf of any of
        ///     the entries in the ZipFile will be handled the same way.
        ///  </para>
        ///
        ///  <para>
        ///     But, if you use a <see cref="ZipFile.ZipError"/> handler, you will want
        ///     to set this property on the <c>ZipEntry</c> within the handler, to
        ///     communicate back to DotNetZip what you would like to do with the
        ///     particular error.
        ///  </para>
        ///
        /// </remarks>
        /// <seealso cref="Ionic.Zip.ZipFile.ZipErrorAction"/>
        /// <seealso cref="Ionic.Zip.ZipFile.ZipError"/>
        public ZipErrorAction ZipErrorAction
        {
            get;
            set;
        }


        /// <summary>
        /// Indicates whether the entry was included in the most recent save.
        /// </summary>
        /// <remarks>
        /// An entry can be excluded or skipped from a save if there is an error
        /// opening or reading the entry.
        /// </remarks>
        /// <seealso cref="ZipErrorAction"/>
        public bool IncludedInMostRecentSave
        {
            get 
            {
                return !_skippedDuringSave;
            }
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

                entry._Source = ZipEntrySource.FileSystem;

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
                
            entry._ntfsTimesAreSet = true;
#else
                //try
                //{
                // workitem 6878??
                // Ionic.Zip.SharedUtilities.AdjustTime_Win32ToDotNet
                entry._Mtime = File.GetLastWriteTimeUtc(filename);
                entry._Ctime = File.GetCreationTimeUtc(filename);
                entry._Atime = File.GetLastAccessTimeUtc(filename);

                // workitem 7071
                // can only get attributes on files that exist.
                if (File.Exists(filename) || Directory.Exists(filename))
                    entry._ExternalFileAttrs = (int)File.GetAttributes(filename);
                // else ??

                entry._ntfsTimesAreSet = true;
                
                //}
                //catch
                //{
                //    entry._ntfsTimesAreSet = false;
                //    entry._Mtime = System.DateTime.Now;
                //}
#endif
            }

            entry._LastModified = entry._Mtime;
            entry._LocalFileName = filename; // may include a path
            entry._FileNameInArchive = nameInArchive.Replace('\\', '/');

            // We don't actually slurp in the file data until the caller invokes Write on this entry.

            return entry;
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


        
        /// <summary>Provides a string representation of the instance.</summary>
        /// <returns>a string representation of the instance.</returns>
        public override String ToString()
        {
            return String.Format ("ZipEntry/{0}", FileName);
        }


        internal Stream ArchiveStream
        {
            get
            {
                if (_archiveStream == null)
                {
                    _zipfile.Reset();
                    _archiveStream = _zipfile.StreamForDiskNumber(_diskNumber);
                }
                return _archiveStream;
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
                // Console.WriteLine("SetFdpLoh...\n  pos  0x{0:X8} ({0})", this.ArchiveStream.Position);
                // Console.WriteLine("  seek 0x{0:X8} ({0})", this._RelativeOffsetOfLocalHeader);
                // Console.WriteLine("  stream: {0}", this.ArchiveStream.ToString());
                // change for workitem 8098
                this.ArchiveStream.Seek(this._RelativeOffsetOfLocalHeader, SeekOrigin.Begin);
                //this._zipfile.SeekFromOrigin(this._RelativeOffsetOfLocalHeader);
            }
            catch (System.IO.IOException exc1)
            {
                string description = String.Format("Exception seeking  entry({0}) offset(0x{1:X8}) len(0x{2:X8})",
                                                   this.FileName, this._RelativeOffsetOfLocalHeader,
                                                   this.ArchiveStream.Length);
                throw new BadStateException(description, exc1);
            }

            byte[] block = new byte[30];
            this.ArchiveStream.Read(block, 0, block.Length);

            // At this point we could verify the contents read from the local header
            // with the contents read from the central header.  We could, but don't need to. 
            // So we won't.

            Int16 filenameLength = (short)(block[26] + block[27] * 256);
            Int16 extraFieldLength = (short)(block[28] + block[29] * 256);

            // Console.WriteLine("  pos  0x{0:X8} ({0})", this.ArchiveStream.Position);
            // Console.WriteLine("  seek 0x{0:X8} ({0})", filenameLength + extraFieldLength);
            
            this.ArchiveStream.Seek(filenameLength + extraFieldLength, SeekOrigin.Current);
            this._LengthOfHeader = 30 + extraFieldLength + filenameLength +
                LengthOfCryptoHeaderBytes;

            // Console.WriteLine("  ROLH  0x{0:X8} ({0})", _RelativeOffsetOfLocalHeader);
            // Console.WriteLine("  LOH   0x{0:X8} ({0})", _LengthOfHeader);
            // workitem 8098: ok (arithmetic)
            this.__FileDataPosition = _RelativeOffsetOfLocalHeader + _LengthOfHeader;
            // Console.WriteLine("  FDP   0x{0:X8} ({0})", __FileDataPosition);

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
        private bool _crcCalculated;
        internal Int32 _Crc32;
        internal byte[] _Extra;
        private bool _metadataChanged;
        private bool _restreamRequiredOnSave;
        private bool _sourceIsEncrypted;
        private bool _skippedDuringSave;
        private UInt32 _diskNumber;
        
        private static System.Text.Encoding ibm437 = System.Text.Encoding.GetEncoding("IBM437");
        private System.Text.Encoding _provisionalAlternateEncoding = System.Text.Encoding.GetEncoding("IBM437");
        private System.Text.Encoding _actualEncoding;

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
        private bool _ioOperationCanceled;
        private bool _presumeZip64;
        private Nullable<bool> _entryRequiresZip64;
        private Nullable<bool> _OutputUsesZip64;
        private bool _IsText; // workitem 7801
        private ZipEntryTimestamp _timestamp;

        private static System.DateTime _unixEpoch = new System.DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static System.DateTime _win32Epoch = System.DateTime.FromFileTimeUtc(0L);
        private static System.DateTime _zeroHour = new System.DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);


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
        FileSystem,

        /// <summary>
        /// The entry was instantiated via <see cref="Ionic.Zip.ZipFile.AddEntry(string,string,string)"/> or
        /// <see cref="Ionic.Zip.ZipFile.AddEntry(string,string,System.IO.Stream)"/> .
        /// </summary>
        Stream,

        /// <summary>
        /// The ZipEntry was instantiated by reading a zipfile.
        /// </summary>
        ZipFile,
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
                                                  (uint)0x40000000L, // (uint)FileAccess.Write, 
                                                  (uint)0x00000002L, // (uint)FileShare.Write, 
                                                  0, 
                                                  (uint) 3,  // == open existing
                                                  (uint)0, // flagsAndAttributes 
                                                  0);

            if((int)hFile == -1)
            {
                // workitem 7944: don't throw on failure to set file times
                // throw new ZipException("CreateFileCE Failed");
                return Interop.Marshal.GetLastWin32Error();
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
                                                  (uint)0x40000000L, // (uint)FileAccess.Write, 
                                                  (uint)0x00000002L, // (uint)FileShare.Write, 
                                                  0, 
                                                  (uint) 3,  // == open existing
                                                  (uint)0, // flagsAndAttributes 
                                                  0);

            if((int)hFile == -1)
            {
                // workitem 7944: don't throw on failure to set file time
                // throw new ZipException(String.Format("CreateFileCE Failed ({0})",
                //                                      Interop.Marshal.GetLastWin32Error()));
                return Interop.Marshal.GetLastWin32Error();
            }
                        
            SetFileTime(hFile, null, null, 
                        BitConverter.GetBytes(mtime.ToFileTime()));

            CloseHandle(hFile);
            return 0;
        }


        [Interop.DllImport("coredll.dll", EntryPoint="CreateFile", SetLastError=true)]
        internal static extern int CreateFileCE(string lpFileName,
                                                uint dwDesiredAccess,
                                                uint dwShareMode,
                                                int lpSecurityAttributes,
                                                uint dwCreationDisposition,
                                                uint dwFlagsAndAttributes,
                                                int hTemplateFile);


        [Interop.DllImport("coredll", EntryPoint="GetFileAttributes", SetLastError=true)]
        internal static extern uint GetAttributes(string lpFileName);

        [Interop.DllImport("coredll", EntryPoint="SetFileAttributes", SetLastError=true)]
        internal static extern bool SetAttributes(string lpFileName, uint dwFileAttributes);

        [Interop.DllImport("coredll", EntryPoint="SetFileTime", SetLastError=true)]
        internal static extern bool SetFileTime(IntPtr hFile, byte[] lpCreationTime, byte[] lpLastAccessTime, byte[] lpLastWriteTime); 

        [Interop.DllImport("coredll.dll", SetLastError=true)]
        internal static extern bool CloseHandle(IntPtr hObject);

    }
#endif



}
