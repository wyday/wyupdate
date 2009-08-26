// ZipFile.cs
//
// Copyright (c) 2006, 2007, 2008, 2009 Dino Chiesa and Microsoft Corporation.
// All rights reserved.
//
// This module is part of DotNetZip, a zipfile class library. 
// The class library reads and writes zip files, according to the format
// described by PKware, at:
// http://www.pkware.com/business_and_developers/developer/popups/appnote.txt
//
// This implementation was originally based on the
// System.IO.Compression.DeflateStream base class in the .NET Framework
// v2.0 base class library, but now includes a managed-code port of Zlib.
//
// There are other Zip class libraries available.  For example, it is
// possible to read and write zip files within .NET via the J# runtime.
// But some people don't like to install the extra DLL.  Also, there is
// a 3rd party LGPL-based (or is it GPL?) library called SharpZipLib,
// which works, in both .NET 1.1 and .NET 2.0.  But some people don't
// like the GPL, and some people say it's complicated and slow. 
// Finally, there are commercial tools (From ComponentOne,
// XCeed, etc).  But some people don't want to incur the cost.
//
// This alternative implementation is not GPL licensed, is free of cost,
// and does not require J#. It does require .NET 2.0 .
// 
// This code is released under the Microsoft Public License . 
// See the License.txt for details.  
//
// Bugs:
// 1. no support for reading or writing multi-disk zip archives
// 2. no support for asynchronous operation
// 
// Fri, 31 Mar 2006  14:43
//


using System;
using System.IO;
using Interop=System.Runtime.InteropServices;


namespace Ionic.Zip
{
    /// <summary>
    /// The ZipFile type represents a zip archive file.  This is the main type in the
    /// DotNetZip class library.  This class reads and writes zip files, as defined in
    /// the format for zip described by PKWare.  The compression for this implementation
    /// was, at one time, based on the System.IO.Compression.DeflateStream base class in
    /// the .NET Framework base class library, available in v2.0 and later of the .NET
    /// Framework. As of v1.7 of DotNetZip, the compression is provided by a
    /// managed-code version of Zlib, included with DotNetZip.
    /// </summary>
    [Interop.GuidAttribute("ebc25cf6-9120-4283-b972-0e5520d00005")]
    [Interop.ComVisible(true)]
#if !NETCF    
    [Interop.ClassInterface(Interop.ClassInterfaceType.AutoDispatch)]
#endif
    public partial class ZipFile :
    System.Collections.IEnumerable,
    System.Collections.Generic.IEnumerable<ZipEntry>,
    IDisposable
    {

        #region public properties

            /// <summary>
            /// Indicates whether to perform a full scan of the zip file when reading it. 
            /// </summary>
            ///
            /// <remarks>
            ///
            /// <para>
            /// When reading a zip file, if this flag is <c>true</c> (<c>True</c> in
            /// VB), the entire zip archive will be scanned and searched for entries.
            /// For large archives, this can take a very, long time. The much more
            /// efficient default behavior is to read the zip directory, at the end of
            /// the zip file. However, in some cases the directory is corrupted and it
            /// is desirable to perform a full scan of the zip file to determine the
            /// contents of the zip file.
            /// </para>
            ///
            /// <para>
            /// If you want to track progress, you can set the ReadProgress event. 
            /// </para>
            ///
            /// <para>
            /// This flag is effective only when calling Initialize.  The Initialize
            /// method may take a long time to run for large zip files, when
            /// <c>Fullscan</c> is true.
            /// </para>
            ///
            /// </remarks>
            ///
            /// <example>
            /// This example shows how to read a zip file using the full scan approach,
            /// and then save it, thereby producing a corrected zip file. 
            /// <code lang="C#">
            /// using (var zip = new ZipFile())
            /// {
            ///     zip.FullScan = true;
            ///     zip.Initialize(zipFileName);
            ///     zip.Save(newName);
            /// }
            /// </code>
            ///
            /// <code lang="VB">
            /// Using zip As New ZipFile
            ///     zip.FullScan = True
            ///     zip.Initialize(zipFileName)
            ///     zip.Save(newName)
            /// End Using
            /// </code>
            /// </example>
            ///
            public bool FullScan
        {
            get;
            set;
        }


        
        /// <summary>
        /// Size of the IO buffer used while saving.
        /// </summary>
        /// <remarks>
        ///
        /// <para>
        /// First, let me say that you really don't need to bother with this.  It is
        /// here to allow for optimizations that you probably won't make! It will work
        /// fine if you don't set or get this property at all. Ok?
        /// </para>
        ///
        /// <para>
        /// Now that we have <em>that</em> out of the way, the fine print: This
        /// property affects the size of the buffer that is used for I/O for each entry
        /// contained in the zip file. When a file is read in to be compressed, it uses
        /// a buffer given by the size here.  When you update a zip file, the data for
        /// unmodified entries is copied from the first zip file to the other, through a
        /// buffer given by the size here.
        /// </para>
        ///
        /// <para>
        /// Changing the buffer size affects a few things: first, for larger buffer
        /// sizes, the memory used by the <c>ZipFile</c>, obviously, will be larger
        /// during I/O operations.  This may make operations faster for very much larger
        /// files.  Last, for any given entry, when you use a larger buffer there will be
        /// fewer progress events during I/O operations, because there's one progress
        /// event generated for each time the buffer is filled and then emptied.
        /// </para>
        ///
        /// <para>
        /// The default buffer size is 8k.  Increasing the buffer size may speed things
        /// up as you compress larger files.  But there are no hard-and-fast rules here,
        /// eh?  You won't know til you test it.  And there will be a limit where ever
        /// larger buffers actually slow things down.  So as I said in the beginning,
        /// it's probably best if you don't set or get this property at all.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <example>
        /// This example shows how you might set a large buffer size for efficiency when
        /// dealing with zip entries that are larger than 1gb. 
        /// <code lang="C#">
        /// using (ZipFile zip = new ZipFile())
        /// {
        ///     zip.SaveProgress += this.zip1_SaveProgress;
        ///     zip.AddDirectory(directoryToZip, "");
        ///     zip.UseZip64WhenSaving = Zip64Option.Always;
        ///     zip.BufferSize = 65536*8; // 65536 * 8 = 512k
        ///     zip.Save(ZipFileToCreate);
        /// }
        /// </code>
        /// </example>
        
        public int BufferSize
        {
            get { return _BufferSize; }
            set { _BufferSize = value; }
        }

        /// <summary>
        /// Size of the work buffer to use for the ZLIB codec during compression.
        /// </summary>
        public int CodecBufferSize
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether extracted files should keep their paths as
        /// stored in the zip archive. 
        /// </summary>
        public bool FlattenFoldersOnExtract
        {
            get;
            set;
        }

    
        /// <summary>
        /// The compression strategy to use for all entries.
        /// </summary>
        ///
        /// <remarks>
        /// This refers to the Strategy used by the ZLIB-compatible compressor. Different
        /// compression strategies work better on different sorts of data. The strategy parameter
        /// can affect the compression ratio and the speed of compression but not the correctness
        /// of the compresssion.  For more information see <see
        /// cref="Ionic.Zlib.CompressionStrategy "/>.
        /// </remarks>
        public Ionic.Zlib.CompressionStrategy Strategy
        {
            get { return _Strategy; }
            set { _Strategy = value; }
        }

        /// <summary>
        /// The name of the <c>ZipFile</c>, on disk.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para>
        /// When the <c>ZipFile</c> instance was created by reading an archive using one
        /// of the <c>ZipFile.Read</c> methods, this property represents the name of the
        /// zip file that was read.  When the <c>ZipFile</c> instance was created by
        /// using the no-argument constructor, this value is <c>null</c> (<c>Nothing</c>
        /// in VB).
        /// </para>
        ///
        /// <para>
        /// If you use the no-argument constructor, and you then explicitly set this
        /// property, when you call <see cref="ZipFile.Save()"/>, this name will specify
        /// the name of the zip file created.  Doing so is equivalent to calling <see
        /// cref="ZipFile.Save(String)"/>.  When instantiating a ZipFile by reading from
        /// a stream or byte array, the Name property remains <c>null</c>.  When saving
        /// to a stream, the Name property is implicitly set to <c>null</c>.
        /// </para>
        /// </remarks>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }


        /// <summary>
        /// Sets the compression level to be used for entries when saving the zip archive.
        /// </summary>
        /// <remarks>
        /// The compression level setting is used at the time of <c>Save()</c>. The same
        /// level is applied to all <c>ZipEntry</c> instances contained in the
        /// <c>ZipFile</c> during the save.  If you do not set this property, the
        /// default compression level is used, which normally gives a good balance of
        /// compression efficiency and compression speed.  In some tests, using
        /// <c>BestCompression</c> can double the time it takes to compress, while
        /// delivering just a small increase in compression efficiency.  This behavior
        /// will vary with the type of data you compress.  If you are in doubt, just
        /// leave this setting alone, and accept the default.
        /// </remarks>
        public Ionic.Zlib.CompressionLevel CompressionLevel
        {
            get;
            set;
        }

        /// <summary>
        /// A comment attached to the zip archive.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para>
        /// This property is read/write. It allows the application to specify a comment
        /// for the <c>ZipFile</c>, or read the comment for the <c>ZipFile</c>.  After
        /// setting this property, changes are only made permanent when you call a
        /// <c>Save()</c> method.
        /// </para>
        ///
        /// <para>
        /// According to <see
        /// href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">PKWARE's zip
        /// specification</see>, the comment is not encrypted, even if there is a
        /// password set on the zip file.
        /// </para>
        ///
        /// <para>
        /// The zip spec does not describe how to encode the comment string in a code
        /// page other than IBM437.  Therefore, for "compliant" zip tools and libraries,
        /// comments will use IBM437.  However, there are situations where you want an
        /// encoded Comment, for example using code page 950 "Big-5 Chinese".  DotNetZip
        /// will encode the comment in the code page specified by <see
        /// cref="ProvisionalAlternateEncoding"/>, at the time of the call to
        /// ZipFile.Save().
        /// </para>
        ///
        /// <para>
        /// When creating a zip archive using this library, it is possible to change the
        /// value of <see cref="ProvisionalAlternateEncoding" /> between each entry you
        /// add, and between adding entries and the call to Save(). Don't do this.  It
        /// will likely result in a zipfile that is not readable by any tool or
        /// application.  For best interoperability, leave <see
        /// cref="ProvisionalAlternateEncoding" /> alone, or specify it only once,
        /// before adding any entries to the <c>ZipFile</c> instance.
        /// </para>
        ///
        /// </remarks>
        public string Comment
        {
            get { return _Comment; }
            set
            {
                _Comment = value;
                _contentsChanged = true;
            }
        }


        

        /// <summary>
        /// Specifies whether the Creation, Access, and Modified times
        /// for entries added to the zip file will be emitted in "Unix(tm)
        /// format" when the zip archive is saved.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// An application creating a zip archive can use this flag to explicitly
        /// specify that the file times for the entries should or should not be stored
        /// in the zip archive in the format used by Unix. By default this flag is
        /// <c>false</c>.
        /// </para>
        ///
        /// <para>
        /// When adding an entry from a file or directory, the Creation (<see
        /// cref="ZipEntry.CreationTime"/>), Access (<see cref="ZipEntry.AccessedTime"/>),
        /// and Modified (<see cref="ZipEntry.ModifiedTime"/>) times for the given entry are
        /// automatically set from the filesystem values. When adding an entry from a stream
        /// or string, all three values are implicitly set to DateTime.Now.  Applications
        /// can also explicitly set those times by calling <see
        /// cref="ZipEntry.SetEntryTimes(DateTime, DateTime, DateTime)"/>.
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
        /// The times stored are taken from <see cref="ZipEntry.ModifiedTime"/>, <see
        /// cref="ZipEntry.AccessedTime"/>, and <see cref="ZipEntry.CreationTime"/>.
        /// </para>
        ///
        /// <para>
        /// The value set here applies to all entries subsequently added to the
        /// <c>ZipFile</c>.
        /// </para>
        ///
        /// <para>
        /// This property is not mutually exclusive of the <see
        /// cref="EmitTimesInUnixFormatWhenSaving" /> property.  It is possible and
        /// legal and valid to produce a zip file that contains timestamps encoded in
        /// the Unix format as well as in the Windows format.  I haven't got a complete
        /// list of tools and which sort of timestamps they can use and will
        /// tolerate. You'll have to test it yourself.  If you get any good information
        /// and would like to pass it on, please do so and I will include that
        /// information in this documentation.
        /// </para>
        /// </remarks>
        ///
        /// <example>
        /// This example shows how to save a zip file that contains file timestamps
        /// in a format normally used by Unix.
        /// <code lang="C#">
        /// using (var zip = new ZipFile())
        /// {
        ///     zip.EmitTimesInWindowsFormatWhenSaving = false;
        ///     zip.EmitTimesInUnixFormatWhenSaving = true;
        ///     zip.AddDirectory(directoryToZip, "files");
        ///     zip.Save(outputFile);
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As New ZipFile
        ///     zip.EmitTimesInWindowsFormatWhenSaving = False
        ///     zip.EmitTimesInUnixFormatWhenSaving = True
        ///     zip.AddDirectory(directoryToZip, "files")
        ///     zip.Save(outputFile)
        /// End Using
        /// </code>
        /// </example>
        ///
        /// <seealso cref="ZipEntry.EmitTimesInWindowsFormatWhenSaving" />
        /// <seealso cref="EmitTimesInUnixFormatWhenSaving" />
        public bool EmitTimesInWindowsFormatWhenSaving
        {
            get
            {
                return _emitNtfsTimes;
            }
            set
            {
                _emitNtfsTimes= value;
            }
        }


        /// <summary>
        /// Specifies whether the Creation, Access, and Modified times
        /// for entries added to the zip file will be emitted in "Unix(tm)
        /// format" when the zip archive is saved.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// An application creating a zip archive can use this flag to explicitly
        /// specify that the file times for the entries should or should not be stored
        /// in the zip archive in the format used by Unix. By default this flag is
        /// <c>false</c>.
        /// </para>
        ///
        /// <para>
        /// When adding an entry from a file or directory, the Creation (<see
        /// cref="ZipEntry.CreationTime"/>), Access (<see cref="ZipEntry.AccessedTime"/>), and
        /// Modified (<see cref="ZipEntry.ModifiedTime"/>) times for the given entry are
        /// automatically set from the filesystem values. When adding an entry from a
        /// stream or string, all three values are implicitly set to DateTime.Now.
        /// Applications can also explicitly set those times by calling <see
        /// cref="ZipEntry.SetEntryTimes(DateTime, DateTime, DateTime)"/>.
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
        /// The times stored are taken from <see cref="ZipEntry.ModifiedTime"/>, <see
        /// cref="ZipEntry.AccessedTime"/>, and <see cref="ZipEntry.CreationTime"/>.
        /// </para>
        ///
        /// <para>
        /// This property is not mutually exclusive of the <see
        /// cref="EmitTimesInWindowsFormatWhenSaving" /> property.  It is possible and
        /// legal and valid to produce a zip file that contains timestamps encoded in
        /// the Unix format as well as in the Windows format.  I haven't got a complete
        /// list of tools and which sort of timestamps they can use and will
        /// tolerate. You'll have to test it yourself.  If you get any good information
        /// and would like to pass it on, please do so and I will include that
        /// information in this documentation.
        /// </para>
        /// </remarks>
        ///
        /// <seealso cref="ZipEntry.EmitTimesInUnixFormatWhenSaving" />
        /// <seealso cref="EmitTimesInWindowsFormatWhenSaving" />
        public bool EmitTimesInUnixFormatWhenSaving
        {
            get
            {
                return _emitUnixTimes;
            }
            set
            {
                _emitUnixTimes= value;
            }
        }


                
        #if LEGACY
        /// <summary>
            /// When this is set, any volume name (eg C:) is trimmed 
            /// from fully-qualified pathnames on any ZipEntry, before writing the 
            /// ZipEntry into the <c>ZipFile</c>. 
            /// </summary>
            ///
            /// <remarks>
            /// <para>
            /// The default value is <c>true</c>. This setting must be true to allow 
            /// Windows Explorer to read the zip archives properly. It's also required to be 
            /// true if you want to read the generated zip files on any other non-Windows OS. 
            /// </para>
            /// 
            /// <para>
            /// The property is included for backwards compatibility only.  You'll 
            /// almost never need or want to set this to false.
            /// </para>
            ///
            /// </remarks>
            private bool TrimVolumeFromFullyQualifiedPaths
            {
                get { return _TrimVolumeFromFullyQualifiedPaths; }
                set { _TrimVolumeFromFullyQualifiedPaths = value; }
            }
        #endif

        /// <summary>
        /// Indicates whether verbose output is sent to the StatusMessageTextWriter during
        /// <c>AddXxx()</c> and <c>ReadXxx()</c> operations.
        /// </summary>
        ///
        /// <remarks>
        /// This is a synthetic property.  It returns true if the <see
        /// cref="StatusMessageTextWriter"/> is non-null.
        /// </remarks>
        internal bool Verbose
        {
            get { return (_StatusMessageTextWriter != null); }
            //set { _Verbose = value; }
        }


        /// <summary>
        /// Indicates whether to perform case-sensitive matching on the filename when
        /// retrieving entries in the zipfile via the string-based indexer.
        /// </summary>
        ///
        /// <remarks>
        /// The default value is <c>false</c>, which means DON'T do case-sensitive
        /// matching. In other words, retrieving zip["ReadMe.Txt"] is the same as
        /// zip["readme.txt"].  It really makes sense to set this to <c>true</c> only if
        /// you are not running on Windows, which has case-insensitive filenames. But
        /// since this library is not built for non-Windows platforms, in most cases you
        /// should just leave this property alone.
        /// </remarks>
        public bool CaseSensitiveRetrieval
        {
            get { return _CaseSensitiveRetrieval; }
            set { _CaseSensitiveRetrieval = value; }
        }


        /// <summary>
        /// Indicates whether to encode entry filenames and entry comments using Unicode 
        /// (UTF-8).
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">The
        /// PKWare zip specification</see> provides for encoding file names and file
        /// comments in either the IBM437 code page, or in UTF-8.  This flag selects the
        /// encoding according to that specification.  By default, this flag is false,
        /// and filenames and comments are encoded into the zip file in the IBM437
        /// codepage.  Setting this flag to true will specify that filenames and
        /// comments that cannot be encoded with IBM437 will be encoded with UTF-8.
        /// </para>
        ///
        /// <para>
        /// Zip files created with strict adherence to the PKWare specification with
        /// respect to UTF-8 encoding can contain entries with filenames containing any
        /// combination of Unicode characters, including the full range of characters
        /// from Chinese, Latin, Hebrew, Greek, Cyrillic, and many other alphabets.
        /// However, because at this time, the UTF-8 portion of the PKWare specification
        /// is not broadly supported by other zip libraries and utilities, such zip
        /// files may not be readable by your favorite zip tool or archiver. In other
        /// words, interoperability will decrease if you set this flag to true.
        /// </para>
        ///
        /// <para>
        /// In particular, Zip files created with strict adherence to the PKWare
        /// specification with respect to UTF-8 encoding will not work well with
        /// Explorer in Windows XP or Windows Vista, because Windows compressed folders,
        /// as far as I know, do not support UTF-8 in zip files.  Vista can read the zip
        /// files, but shows the filenames incorrectly. Unpacking from Windows Vista
        /// Explorer will result in filenames that have rubbish characters in place of
        /// the high-order UTF-8 bytes.
        /// </para>
        ///
        /// <para>
        /// Also, zip files that use UTF-8 encoding will not work well with Java
        /// applications that use the java.util.zip classes, as of v5.0 of the Java
        /// runtime. The Java runtime does not correctly implement the PKWare
        /// specification in this regard.
        /// </para>
        ///
        /// <para>
        /// As a result, we have the unfortunate situation that "correct" behavior by the
        /// DotNetZip library with regard to Unicode encoding of filenames during zip
        /// creation will result in zip files that are readable by strictly compliant and
        /// current tools (for example the most recent release of the commercial WinZip
        /// tool); but these zip files will not be readable by various other tools or
        /// libraries, including Windows Explorer.
        /// </para>
        ///
        /// <para>
        /// The DotNetZip library can read and write zip files with UTF8-encoded
        /// entries, according to the PKware spec.  If you use DotNetZip for both
        /// creating and reading the zip file, and you use UTF-8, there will be no loss
        /// of information in the filenames. For example, using a self-extractor created
        /// by this library will allow you to unpack files correctly with no loss of
        /// information in the filenames.
        /// </para>
        ///
        /// <para>
        /// If you do not set this flag, it will remain false.  If this flag is false,
        /// your ZipFile will encode all filenames and comments using the IBM437
        /// codepage.  This can cause "loss of information" on some filenames, but the
        /// resulting zipfile will be more interoperable with other utilities. As an
        /// example of the loss of information, diacritics can be lost.  The o-tilde
        /// character will be down-coded to plain o.  The c with a cedilla (Unicode
        /// 0xE7) used in Portugese will be downcoded to a c.  Likewise, the O-stroke
        /// character (Unicode 248), used in Danish and Norwegian, will be down-coded to
        /// plain o. Chinese characters cannot be represented in codepage IBM437; when
        /// using the default encoding, Chinese characters in filenames will be
        /// represented as ?. These are all examples of "information loss".
        /// </para>
        ///
        /// <para>
        /// The loss of information associated to the use of the IBM437 encoding is
        /// inconvenient, and can also lead to runtime errors. For example, using
        /// IBM437, any sequence of 4 Chinese characters will be encoded as ????.  If
        /// your application creates a ZipFile, then adds two files, each with names of
        /// four Chinese characters each, this will result in a duplicate filename
        /// exception.  In the case where you add a single file with a name containing
        /// four Chinese characters, calling Extract() on the entry that has question
        /// marks in the filename will result in an exception, because the question mark
        /// is not legal for use within filenames on Windows.  These are just a few
        /// examples of the problems associated to loss of information.
        /// </para>
        ///
        /// <para>
        /// This flag is independent of the encoding of the content within the entries
        /// in the zip file. Think of the zip file as a container - it supports an
        /// encoding.  Within the container are other "containers" - the file entries
        /// themselves.  The encoding within those entries is independent of the
        /// encoding of the zip archive container for those entries.
        /// </para>
        ///
        /// <para>
        /// Rather than specify the encoding in a binary fashion using this flag, an
        /// application can specify an arbitrary encoding via the <see
        /// cref="ProvisionalAlternateEncoding"/> property.  Setting the encoding
        /// explicitly when creating zip archives will result in non-compliant zip files
        /// that, curiously, are fairly interoperable.  The challenge is, the PKWare
        /// specification does not provide for a way to specify that an entry in a zip
        /// archive uses a code page that is neither IBM437 nor UTF-8.  Therefore if you
        /// set the encoding explicitly when creating a zip archive, you must take care
        /// upon reading the zip archive to use the same code page.  If you get it
        /// wrong, the behavior is undefined and may result in incorrect filenames,
        /// exceptions, stomach upset, hair loss, and acne.
        /// </para>
        /// </remarks>
        /// <seealso cref="ProvisionalAlternateEncoding"/>
        public bool UseUnicodeAsNecessary
        {
            get
            {
                return _provisionalAlternateEncoding == System.Text.Encoding.GetEncoding("UTF-8");
            }
            set
            {
                _provisionalAlternateEncoding = (value) ? System.Text.Encoding.GetEncoding("UTF-8") : DefaultEncoding;
            }
        }


        /// <summary>
        /// Specify whether to use ZIP64 extensions when saving a zip archive. 
        /// </summary>
        /// <remarks>
        ///
        /// <para>
        /// Designed many years ago, the <see
        /// href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">original zip
        /// specification from PKWARE</see> allowed for 32-bit quantities for the
        /// compressed and uncompressed sizes of zip entries, as well as a 32-bit
        /// quantity for specifying the length of the zip archive itself, and a maximum
        /// of 65535 entries.  These limits are now regularly exceeded in many backup
        /// and archival scenarios.  Recently, PKWare added extensions to the original
        /// zip spec, called "ZIP64 extensions", to raise those limitations.  This
        /// property governs whether the <c>ZipFile</c> instance will use those
        /// extensions when writing zip archives within a call to one of the Save()
        /// methods.  The use of these extensions is optional and explicit in DotNetZip
        /// because, despite the status of ZIP64 as a bona fide standard, many other zip
        /// tools and libraries do not support ZIP64, and therefore a zip file saved
        /// with ZIP64 extensions may be unreadable by some of those other tools.
        /// </para>
        /// 
        /// <para>
        /// Set this property to <see cref="Zip64Option.Always"/> to always use ZIP64
        /// extensions when saving, regardless of whether your zip archive needs it.
        /// Suppose you add 5 files, each under 100k, to a ZipFile.  If you specify
        /// Always for this flag before calling the Save() method, you will get a ZIP64
        /// archive, though you do not need to use ZIP64 because none of the original
        /// zip limits had been exceeded.
        /// </para>
        ///
        /// <para>
        /// Set this property to <see cref="Zip64Option.Never"/> to tell the DotNetZip
        /// library to never use ZIP64 extensions.  This is useful for maximum
        /// compatibility and interoperability, at the expense of the capability of
        /// handling large files or large archives.  NB: Windows Explorer in Windows XP
        /// and Windows Vista cannot currently extract files from a zip64 archive, so if
        /// you want to guarantee that a zip archive produced by this library will work
        /// in Windows Explorer, use <c>Never</c>. If you set this property to <see
        /// cref="Zip64Option.Never"/>, and your application creates a zip that would
        /// exceed one of the ZIP limits, the library will throw an exception during the
        /// Save().
        /// </para>
        ///
        /// <para>
        /// Set this property to <see cref="Zip64Option.AsNecessary"/> to tell the
        /// DotNetZip library to use the zip64 extensions when required by the
        /// entry. After the file is compressed, the original and compressed sizes are
        /// checked, and if they exceed the limits described above, then zip64 can be
        /// used. That is the general idea, but there is an additional wrinkle when
        /// saving to a non-seekable device, like the ASP.NET
        /// <c>Response.OutputStream</c>, or <c>Console.Out</c>.  When using
        /// non-seekable streams for output, the entry header - which indicates whether
        /// zip64 is in use - is emitted before it is known if zip64 is necessary.  It
        /// is only after all entries have been saved that it can be known if ZIP64 will
        /// be required.  On seekable output streams, after saving all entries, the
        /// library can seek backward and re-emit the zip file header to be consistent
        /// with the actual ZIP64 requirement.  But using a non-seekable output stream,
        /// the library cannot seek backward, so the header can never be changed. In
        /// other words, the archive's use of ZIP64 extensions is not alterable after
        /// the header is emitted.  Therefore, when saving to non-seekable streams,
        /// using <see cref="Zip64Option.AsNecessary"/> is the same as using <see
        /// cref="Zip64Option.Always"/>: it will always produce a zip archive that uses
        /// zip64 extensions.
        /// </para>
        ///
        /// <para>
        /// The default value for the property is <see cref="Zip64Option.Never"/>. <see
        /// cref="Zip64Option.AsNecessary"/> is safest, in the sense that you will not
        /// get an Exception if a pre-ZIP64 limit is exceeded.
        /// </para>
        ///
        /// <para>
        /// You may set the property at any time before calling Save(). 
        /// </para>
        ///
        /// <para>
        /// The <c>Zipfile.Read()</c> method will properly read ZIP64-endowed zip
        /// archives, regardless of the value of this property.  DotNetZip will always
        /// read ZIP64 archives.  This property governs whether DotNetZip will write
        /// them. Therefore, when updating archives, be careful about setting this
        /// property after reading an archive that may use ZIP64 extensions.
        /// </para>
        ///
        /// <para>
        /// An interesting question is, if you have set this property to
        /// <c>AsNecessary</c>, and then successfully saved, does the resulting archive
        /// use ZIP64 extensions or not?  To learn this, check the <see
        /// cref="OutputUsedZip64"/> property, after calling Save().
        /// </para>
        ///
        /// <para>
        /// Have you thought about
        /// <see href="http://cheeso.members.winisp.net/DotNetZipDonate.aspx">donating</see>?
        /// </para>
        ///
        /// </remarks>
        /// <seealso cref="RequiresZip64"/>
        public Zip64Option UseZip64WhenSaving
        {
            get
            {
                return _zip64;
            }
            set
            {
                _zip64 = value;
            }
        }



        /// <summary>
        /// Indicates whether the archive requires ZIP64 extensions.
        /// </summary>
        /// <remarks>
        ///
        /// <para>
        /// This property is <c>null</c> (or <c>Nothing</c> in VB) if the archive has not been
        /// saved, and there are fewer than 65334 ZipEntry items contained in the archive.
        /// </para>
        ///
        /// <para>
        /// The <c>Value</c> is true if any of the following four conditions holds: the
        /// uncompressed size of any entry is larger than 0xFFFFFFFF; the compressed
        /// size of any entry is larger than 0xFFFFFFFF; the relative offset of any
        /// entry within the zip archive is larger than 0xFFFFFFFF; or there are more
        /// than 65534 entries in the archive.  (0xFFFFFFFF = 4,294,967,295).  The
        /// result may not be known until a Save() is attempted on the zip archive.  The
        /// Value of this <see cref="System.Nullable"/> property may be set only AFTER
        /// one of the Save() methods has been called.
        /// </para>
        ///
        /// <para>
        /// If none of the four conditions holds, and the archive has been saved, then
        /// the Value is false.
        /// </para>
        ///
        /// <para>
        /// A <c>Value</c> of false does not indicate that the zip archive, as saved,
        /// does not use ZIP64.  It merely indicates that ZIP64 is not required.  An
        /// archive may use ZIP64 even when not required if the <see
        /// cref="ZipFile.UseZip64WhenSaving"/> property is set to <see
        /// cref="Zip64Option.Always"/>, or if the <see
        /// cref="ZipFile.UseZip64WhenSaving"/> property is set to <see
        /// cref="Zip64Option.AsNecessary"/> and the output stream was not seekable. Use
        /// the <see cref="OutputUsedZip64"/> property to determine if the most recent
        /// <c>Save()</c> method resulted in an archive that utilized the ZIP64
        /// extensions.
        /// </para>
        ///
        /// </remarks>
        /// <seealso cref="UseZip64WhenSaving"/>
        /// <seealso cref="OutputUsedZip64"/>
        public Nullable<bool> RequiresZip64
        {
            get
            {
                if (_entries.Count > 65534)
                    return new Nullable<bool>(true);

                // If the <c>ZipFile</c> has not been saved or if the contents have changed, then
                // it is not known if ZIP64 is required.
                if (!_hasBeenSaved || _contentsChanged) return null;

                // Whether ZIP64 is required is knowable.
                foreach (ZipEntry e in _entries)
                {
                    if (e.RequiresZip64.Value) return new Nullable<bool>(true);
                }

                return new Nullable<bool>(false);
            }
        }


        /// <summary>
        /// Describes whether the most recent <c>Save()</c> operation used ZIP64 extensions.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The use of ZIP64 extensions within an archive is not always necessary, and for
        /// interoperability concerns, it may be desired to NOT use ZIP64 if possible.  The
        /// <see cref="ZipFile.UseZip64WhenSaving"/> property can be set to use ZIP64
        /// extensions only when necessary.  In those cases, Sometimes applications want to
        /// know whether a Save() actually used ZIP64 extensions.  Applications can query
        /// this read-only property to learn whether ZIP64 has been used in a just-saved
        /// <c>ZipFile</c>.
        /// </para>
        ///
        /// <para>
        /// The value is <c>null</c> (or <c>Nothing</c> in VB) if the archive has not
        /// been saved.
        /// </para>
        ///
        /// <para>
        /// Non-null values (<c>HasValue</c> is true) indicate whether ZIP64 extensions
        /// were used during the most recent <c>Save()</c> operation.  The ZIP64
        /// extensions may have been used as required by any particular entry because of
        /// its uncompressed or compressed size, or because the archive is larger than
        /// 4294967295 bytes, or because there are more than 65534 entries in the
        /// archive, or because the <c>UseZip64WhenSaving</c> property was set to <see
        /// cref="Zip64Option.Always"/>, or because the <c>UseZip64WhenSaving</c>
        /// property was set to <see cref="Zip64Option.AsNecessary"/> and the output
        /// stream was not seekable.  The value of this property does not indicate the
        /// reason the ZIP64 extensions were used.
        /// </para>
        /// </remarks>
        /// <seealso cref="UseZip64WhenSaving"/>
        /// <seealso cref="RequiresZip64"/>
        public Nullable<bool> OutputUsedZip64
        {
            get
            {
                return _OutputUsesZip64;
            }
        }


        /// <summary>
        /// The text encoding to use when writing new entries to the <c>ZipFile</c>, for
        /// those entries that cannot be encoded with the default (IBM437) encoding; or,
        /// the text encoding that was used when reading the entries from the
        /// <c>ZipFile</c>.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// In <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">its
        /// zip specification</see>, PKWare describes two options for encoding filenames
        /// and comments: using IBM437 or UTF-8.  But, some archiving tools or libraries
        /// do not follow the specification, and instead encode characters using the
        /// system default code page.  For example, WinRAR when run on a machine in
        /// Shanghai may encode filenames with the Big-5 Chinese (950) code page.  This
        /// behavior is contrary to the Zip specification, but it occurs anyway.
        /// </para>
        ///
        /// <para>
        /// When using DotNetZip to write zip archives that will be read by one of these
        /// other archivers, set this property to specify the code page to use when
        /// encoding the <see cref="ZipEntry.FileName"/> and <see
        /// cref="ZipEntry.Comment"/> for each ZipEntry in the zip file, for values that
        /// cannot be encoded with the default codepage for zip files, IBM437.  This is
        /// why this property is "provisional".  In all cases, IBM437 is used where
        /// possible, in other words, where no loss of data would result. It is
        /// possible, therefore, to have a given entry with a Comment encoded in IBM437
        /// and a FileName encoded with the specified "provisional" codepage.
        /// </para>
        ///
        /// <para>
        /// Be aware that a zip file created after you've explicitly set the <see
        /// cref="ProvisionalAlternateEncoding" /> property to a value other than IBM437
        /// may not be compliant to the PKWare specification, and may not be readable by
        /// compliant archivers.  On the other hand, many (most?) archivers are
        /// non-compliant and can read zip files created in arbitrary code pages.  The
        /// trick is to use or specify the proper codepage when reading the zip.
        /// </para>
        ///
        /// <para>
        /// When creating a zip archive using this library, it is possible to change the
        /// value of <see cref="ProvisionalAlternateEncoding" /> between each entry you
        /// add, and between adding entries and the call to Save(). Don't do this. It
        /// will likely result in a zipfile that is not readable.  For best
        /// interoperability, either leave <see cref="ProvisionalAlternateEncoding" />
        /// alone, or specify it only once, before adding any entries to the
        /// <c>ZipFile</c> instance.  There is one exception to this recommendation,
        /// described later.
        /// </para>
        ///
        /// <para>
        /// When using an arbitrary, non-UTF8 code page for encoding, there is no
        /// standard way for the creator application - whether DotNetZip, WinZip,
        /// WinRar, or something else - to formally specify in the zip file which
        /// codepage has been used for the entries. As a result, readers of zip files
        /// are not able to inspect the zip file and determine the codepage that was
        /// used for the entries contained within it.  It is left to the application or
        /// user to determine the necessary codepage when reading zip files encoded this
        /// way.  If you use an incorrect codepage when reading a zipfile, you will get
        /// entries with filenames that are incorrect, and the incorrect filenames may
        /// even contain characters that are not legal for use within filenames in
        /// Windows. Extracting entries with illegal characters in the filenames will
        /// lead to exceptions. It's too bad, but this is just the way things are with
        /// code pages in zip files. Caveat Emptor.
        /// </para>
        ///
        /// <para>
        /// When using DotNetZip to read a zip archive, and the zip archive uses an
        /// arbitrary code page, you must specify the encoding to use before or when the
        /// <c>Zipfile</c> is READ.  This means you must use a <c>ZipFile.Read()</c>
        /// method that allows you to specify a System.Text.Encoding parameter.  Setting
        /// the ProvisionalAlternateEncoding property after your application has read in
        /// the zip archive will not affect the entry names of entries that have already
        /// been read in, and is probably not what you want.
        /// </para>
        ///     
        /// <para>
        /// And now, the exception to the rule described above.  One strategy for
        /// specifying the code page for a given zip file is to describe the code page
        /// in a human-readable form in the Zip comment. For example, the comment may
        /// read "Entries in this archive are encoded in the Big5 code page".  For
        /// maximum interoperability, the zip comment in this case should be encoded in
        /// the default, IBM437 code page.  In this case, the zip comment is encoded
        /// using a different page than the filenames.  To do this, Specify
        /// <c>ProvisionalAlternateEncoding</c> to your desired region-specific code
        /// page, once before adding any entries, and then reset
        /// <c>ProvisionalAlternateEncoding</c> to IBM437 before setting the <see
        /// cref="Comment"/> property and calling Save().
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// This example shows how to read a zip file using the Big-5 Chinese code page
        /// (950), and extract each entry in the zip file.  For this code to work as
        /// desired, the <c>Zipfile</c> must have been created using the big5 code page
        /// (CP950). This is typical, for example, when using WinRar on a machine with
        /// CP950 set as the default code page.  In that case, the names of entries
        /// within the Zip archive will be stored in that code page, and reading the zip
        /// archive must be done using that code page.  If the application did not use
        /// the correct code page in ZipFile.Read(), then names of entries within the
        /// zip archive would not be correctly retrieved.
        /// <code>
        /// using (var zip = ZipFile.Read(zipFileName, System.Text.Encoding.GetEncoding("big5")))
        /// {
        ///     // retrieve and extract an entry using a name encoded with CP950
        ///     zip[MyDesiredEntry].Extract("unpack");
        /// }
        /// </code>
        ///
        /// <code Lang="VB">
        /// Using zip As ZipFile = ZipFile.Read(ZipToExtract, System.Text.Encoding.fileGetencoding(950))
        ///     ' retrieve and extract an entry using a name encoded with CP950
        ///     zip(MyDesiredEntry).Extract("unpack")
        /// End Using
        /// </code>
        /// </example>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.DefaultEncoding">DefaultEncoding</seealso>
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
        /// The default text encoding used in zip archives.  It is numeric 437, also 
        /// known as IBM437.
        /// </summary>
        /// <seealso cref="Ionic.Zip.ZipFile.ProvisionalAlternateEncoding"/>
        public readonly static System.Text.Encoding DefaultEncoding = System.Text.Encoding.GetEncoding("IBM437");


        /// <summary>
        /// Gets or sets the <c>TextWriter</c> to which status messages are delivered 
        /// for the instance. 
        /// </summary>
        ///
        /// <remarks>
        /// If the TextWriter is set to a non-null value, then verbose output is sent to the
        /// <c>TextWriter</c> during <c>Add</c><c>, Read</c><c>, Save</c> and <c>Extract</c>
        /// operations.  Typically, console applications might use <c>Console.Out</c>
        /// and graphical or headless applications might use a
        /// <c>System.IO.StringWriter</c>. The output of this is suitable for viewing by
        /// humans.
        /// </remarks>
        ///
        /// <example>
        /// <para>
        /// In this example, a console application instantiates a ZipFile, then sets
        /// the StatusMessageTextWriter to Console.Out.  At that point, all verbose
        /// status messages for that ZipFile are sent to the console. 
        /// </para>
        ///
        /// <code lang="C#">
        /// using (ZipFile zip= ZipFile.Read(FilePath))
        /// {
        ///   zip.StatusMessageTextWriter= System.Console.Out;
        ///   // messages are sent to the console during extraction
        ///   zip.ExtractAll();
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As ZipFile = ZipFile.Read(FilePath)
        ///   zip.StatusMessageTextWriter= System.Console.Out
        ///   'Status Messages will be sent to the console during extraction
        ///   zip.ExtractAll()
        /// End Using
        /// </code>
        /// </example>
        public TextWriter StatusMessageTextWriter
        {
            get { return _StatusMessageTextWriter; }
            set { _StatusMessageTextWriter = value; }
        }

        /// <summary>
        /// Gets or sets the flag that indicates whether the <c>ZipFile</c> should use
        /// compression for subsequently added entries in the <c>ZipFile</c> instance.
        /// </summary>
        ///
        /// <remarks>
        /// <para> When saving an entry into a zip archive, the DotNetZip by default
        /// compresses the file. That's what a ZIP archive is all about, isn't it?  For
        /// files that are already compressed, like MP3's or JPGs, the deflate algorithm
        /// can actually slightly expand the size of the data.  Setting this property to
        /// trye allows you to specify that compression should not be used.  The default
        /// value is false.  </para>
        ///
        /// <para>
        /// Do not construe setting this flag to false as "Force Compression".  Setting
        /// it to false merely does NOT force No compression.  If you want to force the
        /// use of the deflate algorithm when storing each entry into the zip archive,
        /// define a <see cref="WillReadTwiceOnInflation"/> callback, which always
        /// returns false, and a <see cref="WantCompression" /> callback that always
        /// returns true.  This is probably the wrong thing to do, but you could do
        /// it. Forcing the use of the Deflate algorithm when storing an entry does not
        /// guarantee that the data size will get smaller. It could increase, as
        /// described above.
        /// </para>
        ///
        /// <para>
        /// Changes to this flag apply to all entries subsequently added to the archive. 
        /// The application can also set the <see cref="ZipEntry.CompressionMethod"/>
        /// property on each ZipEntry, for more granular control of this capability.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipEntry.CompressionMethod"/>
        /// <seealso cref="Ionic.Zip.ZipFile.CompressionLevel"/>
        /// <seealso cref="Ionic.Zip.ZipFile.WantCompression"/>
        ///
        /// <example>
        /// This example shows how to specify that Compression will not be used when
        /// adding files to the zip archive. None of the files added to the archive in
        /// this example will use compression.
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// {
        ///   zip.ForceNoCompression = true;
        ///   zip.AddDirectory(@"c:\reports\January");
        ///   zip.Comment = "All files in this archive will be uncompressed.";
        ///   zip.Save(ZipFileToCreate);
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As New ZipFile()
        ///   zip.ForceNoCompression = true
        ///   zip.AddDirectory("c:\reports\January")
        ///   zip.Comment = "All files in this archive will be uncompressed."
        ///   zip.Save(ZipFileToCreate)
        /// End Using
        /// </code>
        ///
        /// </example>
        public bool ForceNoCompression
        {
            get { return _ForceNoCompression; }
            set { _ForceNoCompression = value; }
        }


        /// <summary>
        /// Gets or sets the name for the folder to store the temporary file
        /// this library writes when saving a zip archive. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This library will create a temporary file when saving a Zip archive to a file.
        /// This file is written when calling one of the <c>Save()</c> methods that does
        /// not save to a stream, or one of the <c>SaveSelfExtractor()</c> methods.  
        /// <para>
        ///
        /// </para>
        /// By default, the library will create the temporary file in the directory
        /// specified for the file itself, via the <see cref="Name"/> property or via the
        /// <see cref="ZipFile.Save(String)"/> method.
        /// </para>
        ///
        /// <para>
        /// Setting this property allows applications to override this default behavior,
        /// so that the library will create the temporary file in the specified
        /// folder. For example, to have the library create the temporary file in the
        /// current working directory, regardless where the <c>ZipFile</c> is saved,
        /// specfy ".".  To revert to the default behavior, set this property to
        /// <c>null</c> (<c>Nothing</c> in VB).
        /// </para>
        ///
        /// <para>
        /// When setting the property to a non-null value, the folder specified must exist;
        /// if it does not an exception is thrown.  The application should have write and
        /// delete permissions on the folder.  The permissions are not explicitly checked
        /// ahead of time; if the application does not have the appropriate rights, an
        /// exception will be thrown at the time <c>Save()</c> is called.
        /// </para>
        ///
        /// <para>
        /// There is no temporary file created when reading a zip archive.  When saving
        /// to a Stream, there is no temporary file created.  For example, if the
        /// application is an ASP.NET application and calls <c>Save()</c> specifying the
        /// <c>Response.OutputStream</c> as the output stream, there is no temporary
        /// file created.
        /// </para>
        /// </remarks>
        ///
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown when setting the property if the directory does not exist. 
        /// </exception>
        ///
        public String TempFileFolder
        {
            get { return _TempFileFolder; }

            set
            {
                _TempFileFolder = value;
                if (value == null) return;

                if (!Directory.Exists(value))
                    throw new FileNotFoundException(String.Format("That directory ({0}) does not exist.", value));

            }
        }

        /// <summary>
        /// Sets the password to be used on the <c>ZipFile</c> instance.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// When writing a zip archive, this password is applied to the entries, not to
        /// the zip archive itself. It applies to any ZipEntry subsequently added to the
        /// <c>ZipFile</c>, using one of the <c>AddFile</c>, <c>AddDirectory</c>,
        /// <c>AddEntry</c>, or <c>AddItem</c> methods, etc.  When reading a zip
        /// archive, this property applies to any entry subsequently extracted from the
        /// <c>ZipFile</c> using one of the Extract methods on the <c>ZipFile</c> class.
        /// </para>
        /// 
        /// <para>
        /// When writing a zip archive, keep this in mind: though the password is set on the
        /// ZipFile object, according to the Zip spec, the "directory" of the archive - in
        /// other words the list of entries contained in the archive - is not encrypted with
        /// the password, or protected in any way.  if you set the Password property, the
        /// password actually applies to individual entries that are added to the archive,
        /// subsequent to the setting of this property.  The list of filenames in the
        /// archive that is eventually created will appear in clear text, but the contents
        /// of the individual files are encrypted.  This is how Zip encryption works.
        /// </para>
        /// 
        /// <para>
        /// If you set the password on the zip archive, and then add a set of files to the
        /// archive, then each entry is encrypted with that password.  You may also want to
        /// change the password between adding different entries. If you set the password,
        /// add an entry, then set the password to <c>null</c> (<c>Nothing</c> in VB), and
        /// add another entry, the first entry is encrypted and the second is not.  If you
        /// call <c>AddFile()</c>, then set the <c>Password</c> property, then call
        /// <c>ZipFile.Save</c>, the file added will not be password-protected, and no
        /// warning will be generated.
        /// </para>
        /// 
        /// <para>
        /// When setting the Password, you may also want to explicitly set the <see
        /// cref="Encryption"/> property, to specify how to encrypt the entries added to
        /// the ZipFile.  If you set the Password to a non-null value and do not set
        /// <see cref="Encryption"/>, then PKZip 2.0 ("Weak") encryption is used.  This
        /// encryption is relatively weak but is very interoperable. If you set the
        /// password to a <c>null</c> value (<c>Nothing</c> in VB), Encryption is reset
        /// to None.
        /// </para>
        ///
        /// <para>
        /// All of the preceding applies to writing zip archives, in other words when
        /// you use one of the Save methods.  To use this property when reading or an
        /// existing ZipFile, do the following: set the Password property on the
        /// <c>ZipFile</c>, then call one of the Extract() overloads on the <see
        /// cref="ZipEntry" />. In this case, the entry is extracted using the
        /// <c>Password</c> that is specified on the <c>ZipFile</c> instance. If you
        /// have not set the <c>Password</c> property, then the password is <c>null</c>,
        /// and the entry is extracted with no password.
        /// </para>
        ///
        /// <para>
        /// If you set the Password property on the <c>ZipFile</c>, then call Extract()
        /// an entry that has not been encrypted with a password, the password is not
        /// used for that entry, and the <c>ZipEntry</c> is extracted as normal. In
        /// other words, the password is used only if necessary.
        /// </para>
        /// 
        /// <para>
        /// The <see cref="ZipEntry"/> class also has a <see
        /// cref="ZipEntry.Password">Password</see> property.  It takes precedence over
        /// this property on the <c>ZipFile</c>.  Typically, you would use the per-entry
        /// Password when most entries in the zip archive use one password, and a few
        /// entries use a different password.  If all entries in the zip file use the
        /// same password, then it is simpler to just set this property on the
        /// <c>ZipFile</c> itself, whether creating a zip archive or extracting a zip
        /// archive.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <example>
        /// <para>
        /// This example creates a zip file, using password protection for the entries,
        /// and then extracts the entries from the zip file.  When creating the zip
        /// file, the Readme.txt file is not protected with a password, but the other
        /// two are password-protected as they are saved. During extraction, each file
        /// is extracted with the appropriate password.
        /// </para>
        /// <code>
        /// // create a file with encryption
        /// using (ZipFile zip = new ZipFile())
        /// {
        ///     zip.AddFile("ReadMe.txt");
        ///     zip.Password= "!Secret1";
        ///     zip.AddFile("MapToTheSite-7440-N49th.png");
        ///     zip.AddFile("2008-Regional-Sales-Report.pdf");
        ///     zip.Save("EncryptedArchive.zip");
        /// }
        /// 
        /// // extract entries that use encryption
        /// using (ZipFile zip = ZipFile.Read("EncryptedArchive.zip"))
        /// {
        ///     zip.Password= "!Secret1";
        ///     zip.ExtractAll("extractDir");
        /// }
        /// 
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As New ZipFile
        ///     zip.AddFile("ReadMe.txt")
        ///     zip.Password = "123456!"
        ///     zip.AddFile("MapToTheSite-7440-N49th.png")
        ///     zip.Password= "!Secret1";
        ///     zip.AddFile("2008-Regional-Sales-Report.pdf")
        ///     zip.Save("EncryptedArchive.zip")
        /// End Using
        ///
        ///
        /// ' extract entries that use encryption
        /// Using (zip as ZipFile = ZipFile.Read("EncryptedArchive.zip"))
        ///     zip.Password= "!Secret1"
        ///     zip.ExtractAll("extractDir")
        /// End Using
        /// 
        /// </code>
        ///
        /// </example>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.Encryption">ZipFile.Encryption</seealso>
        /// <seealso cref="Ionic.Zip.ZipEntry.Password">ZipEntry.Password</seealso>
        public String Password
        {
            set
            {
                _Password = value;
                if (_Password == null)
                {
                    Encryption = EncryptionAlgorithm.None;
                }
                else if (Encryption == EncryptionAlgorithm.None)
                {
                    Encryption = EncryptionAlgorithm.PkzipWeak;
                }
            }
        }





        /// <summary>
        /// The action the library should take when extracting a file that already exists.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property affects the behavior of the Extract methods (one of the
        /// <c>Extract()</c> or <c>ExtractWithPassword()</c> overloads), when extraction
        /// would would overwrite an existing filesystem file. If you do not set this
        /// property, the library throws an exception when extracting an entry would
        /// overwrite an existing file.
        /// </para>
        ///
        /// <para>
        /// This property has no effect when extracting to a stream, or when the file to
        /// be extracted does not already exist.
        /// </para>
        /// </remarks>
        /// <seealso cref="Ionic.Zip.ZipEntry.ExtractExistingFile"/>
        public ExtractExistingFileAction ExtractExistingFile
        {
            get;
            set;
        }


        /// <summary>
        ///   The action the library should take when an error is encountered while
        ///   opening or reading files as they are added to a zip archive. 
        /// </summary>
        ///
        /// <remarks>
        ///  <para>
        ///     In some cases an error will occur when DotNetZip tries to open a file to be
        ///     added to the zip archive.  In other cases, an error might occur after the
        ///     file has been successfully opened, while DotNetZip is reading the file.
        ///  </para>
        /// 
        ///  <para>
        ///    The first problem might occur when calling Adddirectory() on a directory
        ///    that contains a Clipper .dbf file; the file is locked by Clipper and
        ///    cannot be opened bby another process. An example of the second problem is
        ///    the ERROR_LOCK_VIOLATION that results when a file is opened by another
        ///    process, but not locked, and a range lock has been taken on the file.
        ///    Microsoft Outlook takes range locks on .PST files.
        ///  </para>
        ///
        /// </remarks>
        /// <seealso cref="Ionic.Zip.ZipEntry.ZipErrorAction"/>
        public ZipErrorAction ZipErrorAction
        {
            get;
            set;
        }


        /// <summary>
        /// The Encryption to use for entries added to the <c>ZipFile</c>.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// Set this when creating a zip archive, or when updating a zip archive. The
        /// specified Encryption is applied to the entries subsequently added to the
        /// <c>ZipFile</c> instance.  Applications do not need to set <c>Encryption</c>
        /// when reading or extracting a zip archive.
        /// </para>
        /// 
        /// <para>
        /// If you set this to something other than EncryptionAlgorithm.None, you will also
        /// need to set the <see cref="Password"/>.
        /// </para>
        ///
        /// <para>
        /// As with other properties (like <see cref="Password"/> and <see
        /// cref="ForceNoCompression"/>), setting this property a <c>ZipFile</c>
        /// instance will cause that <c>EncryptionAlgorithm</c> to be used on all <see
        /// cref="ZipEntry"/> items that are subsequently added to the <c>ZipFile</c>
        /// instance. In other words, if you set this property after you have added
        /// items to the <c>ZipFile</c>, but before you have called <c>Save()</c>, those
        /// items will not be encrypted or protected with a password in the resulting
        /// zip archive. To get a zip archive with encrypted entries, set this property,
        /// along with the <see cref="Password"/> property, before calling
        /// <c>AddFile</c>, <c>AddItem</c>, or <c>AddDirectory</c> (etc.) on
        /// the <c>ZipFile</c> instance.
        /// </para>
        ///
        /// <para>
        /// Some comments on updating archives: If you read a <c>ZipFile</c>, you cannot
        /// modify the Encryption on any encrypted entry, except by extracting the entry
        /// with the original password (if any), removing the original entry via <see
        /// cref="ZipFile.RemoveEntry(ZipEntry)"/>, and then adding a new entry with a
        /// new Password and Encryption setting.
        /// </para>
        ///
        /// <para>
        /// For example, suppose you read a <c>ZipFile</c>, and there is an encrypted
        /// entry.  Setting the Encryption property on that <c>ZipFile</c> and then
        /// calling <c>Save()</c> on the <c>ZipFile</c> does not update the Encryption
        /// used for the entries in the archive.  Neither is an exception
        /// thrown. Instead, what happens during the <c>Save()</c> is that all
        /// previously existing entries are copied through to the new zip archive, with
        /// whatever encryption and password that was used when originally creating the
        /// zip archive. Upon re-reading that archive, to extract entries, applications
        /// should use the original password or passwords, if any.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <example>
        /// <para>
        /// This example creates a zip archive that uses encryption, and then extracts
        /// entries from the archive.  When creating the zip archive, the ReadMe.txt
        /// file is zipped without using a password or encryption.  The other files use
        /// encryption.
        /// </para>
        ///
        /// <code>
        /// // Create a zip archive with AES Encryption.
        /// using (ZipFile zip = new ZipFile())
        /// {
        ///     zip.AddFile("ReadMe.txt");
        ///     zip.Encryption= EncryptionAlgorithm.WinZipAes256;
        ///     zip.Password= "Top.Secret.No.Peeking!";
        ///     zip.AddFile("7440-N49th.png");
        ///     zip.AddFile("2008-Regional-Sales-Report.pdf");
        ///     zip.Save("EncryptedArchive.zip");
        /// }
        /// 
        /// // Extract a zip archive that uses AES Encryption.
        /// // You do not need to specify the algorithm during extraction.
        /// using (ZipFile zip = ZipFile.Read("EncryptedArchive.zip"))
        /// {
        ///     zip.Password= "Top.Secret.No.Peeking!";
        ///     zip.ExtractAll("extractDirectory");
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// ' Create a zip that uses Encryption.
        /// Using zip As New ZipFile()
        ///     zip.Encryption= EncryptionAlgorithm.WinZipAes256
        ///     zip.Password= "Top.Secret.No.Peeking!"
        ///     zip.AddFile("ReadMe.txt")
        ///     zip.AddFile("7440-N49th.png")
        ///     zip.AddFile("2008-Regional-Sales-Report.pdf")
        ///     zip.Save("EncryptedArchive.zip")
        /// End Using
        /// 
        /// ' Extract a zip archive that uses AES Encryption.
        /// ' You do not need to specify the algorithm during extraction.
        /// Using (zip as ZipFile = ZipFile.Read("EncryptedArchive.zip"))
        ///     zip.Password= "Top.Secret.No.Peeking!"
        ///     zip.ExtractAll("extractDirectory")
        /// End Using
        /// </code>
        ///
        /// </example>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.Password">ZipFile.Password</seealso>
        /// <seealso cref="Ionic.Zip.ZipEntry.Encryption">ZipEntry.Encryption</seealso>
        public EncryptionAlgorithm Encryption
        {
            get
            {
                return _Encryption;
            }
            set
            {
                if (value == EncryptionAlgorithm.Unsupported)
                    throw new InvalidOperationException("You may not set Encryption to that value.");
                _Encryption= value;
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
        /// In some cases, applying the Deflate compression algorithm in
        /// <c>DeflateStream</c> can result an increase in the size of the data.  This
        /// "inflation" can happen with previously compressed files, such as a zip, jpg,
        /// png, mp3, and so on.  In a few tests, inflation on zip files can be as large
        /// as 60%!  Inflation can also happen with very small files.  In these cases,
        /// by default, the DotNetZip library discards the compressed bytes, and stores
        /// the uncompressed file data into the zip archive.  This is an optimization
        /// where smaller size is preferred over longer run times.
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
        /// the application to for approval for re-reading the stream, in the case where
        /// inflation occurs.  The callback is invoked only in the case of inflation; that
        /// is to say when the uncompressed stream is smaller than the compressed stream.
        /// </para>
        ///
        /// <para>
        /// As with other properties (like <see cref="Password"/> and <see
        /// cref="ForceNoCompression"/>), setting the corresponding delegate on a
        /// <c>ZipFile</c> instance will caused it to be applied to all ZipEntry items
        /// that are subsequently added to the <c>ZipFile</c> instance. In other words,
        /// if you set this callback after you have added files to the <c>ZipFile</c>,
        /// but before you have called Save(), those items will not be governed by the
        /// callback when you do call Save(). Your best bet is to set this callback
        /// before adding any entries.
        /// </para>
        ///
        /// <para>
        /// Of course, if you want to have different callbacks for different entries,
        /// you may do so.
        /// </para>
        ///
        /// </remarks>
        /// <example>
        /// <para>
        /// In this example, the application callback checks to see if the difference
        /// between the compressed and uncompressed data is greater than 25%.  If it is,
        /// then the callback returns true, and the application tells the library to
        /// re-read the stream.  If not, then the callback returns false, and the
        /// library just keeps the "inflated" file data.
        /// </para>
        ///
        /// <code>
        ///
        /// public bool ReadTwiceCallback(long uncompressed, long compressed, string filename)
        /// {
        ///     return ((uncompressed * 1.0/compressed) > 1.25);
        /// }
        /// 
        /// public void CreateTheZip()
        /// {
        ///     using (ZipFile zip = new ZipFile())
        ///     {
        ///         // set the callback before adding files to the zip
        ///         zip2.WillReadTwiceOnInflation = ReadTwiceCallback;
        ///         zip2.AddFile(filename1);
        ///         zip2.AddFile(filename2);
        ///         zip2.Save(ZipFileToCreate);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="Ionic.Zip.ZipFile.WantCompression"/>
        /// <seealso cref="Ionic.Zip.WantCompressionCallback"/>
        /// <seealso cref="Ionic.Zip.ZipEntry.WillReadTwiceOnInflation"/>
        public ReReadApprovalCallback WillReadTwiceOnInflation
        {
            get;
            set;
        }


        /// <summary>
        /// A callback that allows the application to specify whether compression should
        /// be used for entries subsequently added to the zip archive.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// In some cases, applying the Deflate compression algorithm to an entry *may*
        /// result a slight increase in the size of the data.  This "inflation" can
        /// happen with previously compressed files, such as a zip, jpg, png, mp3, and
        /// so on; it results from adding DEFLATE framing data around incompressible data.
        /// Inflation can also happen with very small files. Applications may wish to
        /// avoid the use of compression in these cases. As well, applications may wish
        /// to avoid compression to save time.
        /// </para>
        ///
        /// <para>
        /// By default, the DotNetZip library takes this approach to decide whether to
        /// apply compression: first it applies a heuristic, to determine whether it
        /// should try to compress a file or not.  The library checks the extension of
        /// the entry, and if it is one of a known list of uncompressible file types
        /// (mp3, zip, docx, and others), the library will not attempt to compress the
        /// entry.  The library does not actually check the content of the entry.  If
        /// you name a text file "Text.mp3", and then attempt to add it to a zip
        /// archive, this library will, by default, not attempt to compress the entry,
        /// based on the extension of the filename.
        /// </para>
        ///
        /// <para>
        /// If this default behavior is not satisfactory, there are two options. First,
        /// the application can override it by setting this <see
        /// cref="ZipFile.WantCompression"/> callback.  This affords maximum control to
        /// the application.  With this callback, the application can supply its own
        /// logic for determining whether to apply the Deflate algorithm or not.  For
        /// example, an application may desire that files over 40mb in size are never
        /// compressed, or always compressed.  An application may desire that the first
        /// 7 entries added to an archive are compressed, and the remaining ones are
        /// not.  The WantCompression callback allows the application full control, on
        /// an entry-by-entry basis.
        /// </para>
        ///
        /// <para>
        /// The second option for overriding the default logic regarding whether to
        /// apply compression is the ForceNoCompression flag.  If this flag is set to
        /// true, the compress-and-check-sizes process as decribed above, is not done,
        /// nor is the callback invoked.  In other words, if you set ForceNoCompression
        /// to true, andalso set the WantCompression callback, only the
        /// ForceNoCompression flag is considered.
        /// </para>
        ///
        /// <para>
        /// This is how the library determines whether compression will be attempted for
        /// an entry.  If it is to be attempted, the library reads the entry, runs it
        /// through the deflate algorithm, and then checks the size of the result.  If
        /// applying the Deflate algorithm increases the size of the data, then the
        /// library discards the compressed bytes, re-reads the raw entry data, and
        /// stores the uncompressed file data into the zip archive, in compliance with
        /// the zip spec.  This is an optimization where smaller size is preferred over
        /// longer run times. The re-reading is gated on the <see
        /// cref="WillReadTwiceOnInflation"/> callback, if it is set. This callback
        /// applies independently of the WantCompression callback.
        /// </para>
        ///
        /// <para>
        /// If by the logic described above, compression is not to be attempted for an
        /// entry, the library reads the entry, and simply stores the entry data
        /// uncompressed.
        /// </para>
        ///
        /// <para>
        /// And, if you have read this far, I would like to point out that a single
        /// person wrote all the code that does what is described above, and also wrote
        /// the description.  Isn't it about time you <see
        /// href="http://cheeso.members.winisp.net/DotNetZipDonate.aspx">donated $5 in
        /// appreciation?</see> The money goes to a charity.
        /// </para>
        ///
        /// </remarks>
        /// <seealso cref="Ionic.Zip.ZipFile.WillReadTwiceOnInflation"/>
        public WantCompressionCallback WantCompression
        {
            get;
            set;
        }



        /// <summary>Provides a string representation of the instance.</summary>
        /// <returns>a string representation of the instance.</returns>
        public override String ToString()
        {
            return String.Format ("ZipFile/{0}", Name);
        }

        

        /// <summary>
        /// Returns the version number on the DotNetZip assembly.
        /// </summary>
        ///
        /// <remarks>
        /// This property is exposed as a convenience.  Callers
        /// could also get the version value by retrieving  GetName().Version 
        /// on the System.Reflection.Assembly object pointing to the
        /// DotNetZip assembly. But sometimes it is not clear which
        /// assembly is being loaded.  This property makes it clear. 
        /// </remarks>
        public static System.Version LibraryVersion
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            }
        }


        internal void NotifyEntryChanged()
        {
            _contentsChanged = true;
        }



        internal Stream ReadStream
        {
            get
            {
                if (_readstream == null)
                {
                    if (_name != null)
                    {
                        try
                        {
                            _readstream = File.OpenRead(_name);
                            _ReadStreamIsOurs = true;
                        }
                        catch (System.IO.IOException ioe)
                        {
                            throw new ZipException("Error opening the file", ioe);
                        }
                    }
                }
                return _readstream;
            }
        }



        // called by ZipEntry in ZipEntry.Extract(), when there is no stream set for the
        // ZipEntry.
        internal void Reset()
        {
            if (_JustSaved)
            {
                // read in the just-saved zip archive
                ZipFile x = new ZipFile();
                x._name = this._name;
                x.ProvisionalAlternateEncoding = this.ProvisionalAlternateEncoding;
                ReadIntoInstance(x);
                // copy the contents of the entries.
                // cannot just replace the entries - the app may be holding them
                foreach (ZipEntry e1 in x)
                {
                    foreach (ZipEntry e2 in this)
                    {
                        if (e1.FileName == e2.FileName)
                        {
                            e2.CopyMetaData(e1);
                        }
                    }
                }
                _JustSaved = false;
            }
        }


        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <c>ZipFile</c> instance, using the specified filename. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Applications can use this constructor to create a new ZipFile for writing, 
        /// or to slurp in an existing zip archive for read and update purposes. 
        /// </para>
        /// 
        /// <para>
        /// To create a new zip archive, an application can call this constructor,
        /// passing the name of a file that does not exist.  The name may be a fully
        /// qualified path. Then the application can add directories or files to the
        /// <c>ZipFile</c> via <c>AddDirectory()</c>, <c>AddFile()</c>, <c>AddItem()</c>
        /// and then write the zip archive to the disk by calling <c>Save()</c>. The zip
        /// file is not actually opened and written to the disk until the application
        /// calls <c>ZipFile.Save()</c>.  At that point the new zip file with the given
        /// name is created.
        /// </para>
        /// 
        /// <para>
        /// If you won't know the name of the <c>Zipfile</c> until the time you call
        /// <c>ZipFile.Save()</c>, or if you plan to save to a stream (which has no
        /// name), then you should use the no-argument constructor.
        /// </para>
        /// 
        /// <para>
        /// The application can also call this constructor to read an existing zip
        /// archive.  passing the name of a valid zip file that does exist. But, it's
        /// better form to use the static <see cref="ZipFile.Read(String)"/> method,
        /// passing the name of the zip file, because using <c>ZipFile.Read()</c> in
        /// your code communicates very clearly what you are doing.  In either case, the
        /// file is then read into the <c>ZipFile</c> instance.  The app can then
        /// enumerate the entries or can modify the zip file, for example adding
        /// entries, removing entries, changing comments, and so on.
        /// </para>
        /// 
        /// <para>
        /// One advantage to this parameterized constructor: it allows applications to
        /// use the same code to add items to a zip archive, regardless of whether the
        /// zip file exists.
        /// </para>
        /// 
        /// <para>
        /// Instances of the <c>ZipFile</c> class are not multi-thread safe.  You may
        /// not party on a single instance with multiple threads.  You may have multiple
        /// threads that each use a distinct <c>ZipFile</c> instance, or you can
        /// synchronize multi-thread access to a single instance.
        /// </para>
        /// 
        /// <para>
        /// By the way, since DotNetZip is so easy to use, don't you think <see
        /// href="http://cheeso.members.winisp.net/DotNetZipDonate.aspx">you should donate
        /// $5 or $10</see>?
        /// </para>
        ///
        /// </remarks>
        ///
        /// <exception cref="Ionic.Zip.ZipException">
        /// Thrown if name refers to an existing file that is not a valid zip file. 
        /// </exception>
        ///
        /// <example>
        /// This example shows how to create a zipfile, and add a few files into it. 
        /// <code>
        /// String ZipFileToCreate = "archive1.zip";
        /// String DirectoryToZip  = "c:\\reports";
        /// using (ZipFile zip = new ZipFile())
        /// { 
        ///   // Store all files found in the top level directory, into the zip archive.
        ///   String[] filenames = System.IO.Directory.GetFiles(DirectoryToZip);
        ///   zip.AddFiles(filenames, "files");
        ///   zip.Save(ZipFileToCreate);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim ZipFileToCreate As String = "archive1.zip"
        /// Dim DirectoryToZip As String = "c:\reports"
        /// Using zip As ZipFile = New ZipFile()
        ///     Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        ///     zip.AddFiles(filenames, "files")
        ///     zip.Save(ZipFileToCreate)
        /// End Using
        /// </code>
        /// </example>
        ///
        /// <param name="fileName">The filename to use for the new zip archive.</param>
        ///
        public ZipFile(string fileName)
        {
            try
            {
                _InitInstance(fileName, null);
            }
            catch (Exception e1)
            {
                throw new ZipException(String.Format("{0} is not a valid zip file", fileName), e1);
            }
        }


        /// <summary>
        /// Creates a new <c>ZipFile</c> instance, using the specified name for the
        /// filename, and the specified Encoding.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the documentation on the <see cref="ZipFile(String)">ZipFile constructor
        /// that accepts a single string argument</see> for basic information on all the
        /// <c>ZipFile</c> constructors.
        /// </para>
        ///
        /// <para>
        /// The Encoding is used as the default alternate encoding for entries with
        /// filenames or comments that cannot be encoded with the IBM437 code page.
        /// This is equivalent to setting the <see cref="ProvisionalAlternateEncoding"/>
        /// property on the <c>ZipFile</c> instance after construction.
        /// </para>
        ///
        /// <para>
        /// Instances of the <c>ZipFile</c> class are not multi-thread safe.  You may
        /// not party on a single instance with multiple threads.  You may have multiple
        /// threads that each use a distinct <c>ZipFile</c> instance, or you can
        /// synchronize multi-thread access to a single instance.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <exception cref="Ionic.Zip.ZipException">
        /// Thrown if name refers to an existing file that is not a valid zip file. 
        /// </exception>
        ///
        /// <param name="fileName">The filename to use for the new zip archive.</param>
        /// <param name="encoding">The Encoding is used as the default alternate 
        /// encoding for entries with filenames or comments that cannot be encoded 
        /// with the IBM437 code page. </param>
        public ZipFile(string fileName, System.Text.Encoding encoding)
        {
            try
            {
                _InitInstance(fileName, null);
                ProvisionalAlternateEncoding = encoding;
            }
            catch (Exception e1)
            {
                throw new ZipException(String.Format("{0} is not a valid zip file", fileName), e1);
            }
        }



        /// <summary>
        /// Create a zip file, without specifying a target filename or stream to save to. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the documentation on the <see cref="ZipFile(String)">ZipFile constructor
        /// that accepts a single string argument</see> for basic information on all the
        /// <c>ZipFile</c> constructors.
        /// </para>
        ///
        /// <para> After instantiating with this constructor and adding entries to the
        /// archive, the application should call <see cref="ZipFile.Save(String)"/> or
        /// <see cref="ZipFile.Save(System.IO.Stream)"/> to save to a file or a stream,
        /// respectively.  The application can also set the <see cref="Name"/> property
        /// and then call the no-argument <see cref="Save()"/> method.  (This is the
        /// preferred approach for applications that use the library through COM
        /// interop.)  If you call the no-argument <see cref="Save()"/> method without
        /// having set the <c>Name</c> of the <c>ZipFile</c>, either through the
        /// parameterized constructor or through the explicit property , the Save() will
        /// throw, because there is no place to save the file.  </para>
        ///
        /// <para>
        /// Instances of the <c>ZipFile</c> class are not multi-thread safe.  You may
        /// have multiple threads that each use a distinct <c>ZipFile</c> instance, or
        /// you can synchronize multi-thread access to a single instance.  </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// This example creates a Zip archive called Backup.zip, containing all the files
        /// in the directory DirectoryToZip. Files within subdirectories are not zipped up.
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// { 
        ///   // Store all files found in the top level directory, into the zip archive.
        ///   // note: this code does not recurse subdirectories!
        ///   String[] filenames = System.IO.Directory.GetFiles(DirectoryToZip);
        ///   zip.AddFiles(filenames, "files");
        ///   zip.Save("Backup.zip");
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As New ZipFile
        ///     ' Store all files found in the top level directory, into the zip archive.
        ///     ' note: this code does not recurse subdirectories!
        ///     Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        ///     zip.AddFiles(filenames, "files")
        ///     zip.Save("Backup.zip")
        /// End Using
        /// </code>
        /// </example>
        public ZipFile()
        {
            _InitInstance(null, null);
        }


        /// <summary>
        /// Create a zip file, specifying a text Encoding, but without specifying a target
        /// filename or stream to save to.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the documentation on the <see cref="ZipFile(String)">ZipFile constructor
        /// that accepts a single string argument</see> for basic information on all the
        /// <c>ZipFile</c> constructors.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <param name="encoding">
        /// The Encoding is used as the default alternate encoding for entries with
        /// filenames or comments that cannot be encoded with the IBM437 code page.
        /// </param>
        public ZipFile(System.Text.Encoding encoding)
        {
            _InitInstance(null, null);
            ProvisionalAlternateEncoding = encoding;
        }


        /// <summary>
        /// Creates a new <c>ZipFile</c> instance, using the specified name for the
        /// filename, and the specified status message writer.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// See the documentation on the <see cref="ZipFile(String)">ZipFile constructor
        /// that accepts a single string argument</see> for basic information on all the
        /// <c>ZipFile</c> constructors.
        /// </para>
        ///
        /// <para>
        /// This version of the constructor allows the caller to pass in a TextWriter,
        /// to which verbose messages will be written during extraction or creation of
        /// the zip archive.  A console application may wish to pass System.Console.Out
        /// to get messages on the Console. A graphical or headless application may wish
        /// to capture the messages in a different <c>TextWriter</c>, for example, a
        /// <c>StringWriter</c>, and then display the messages in a TextBox, or generate
        /// an audit log of ZipFile operations.
        /// </para>
        /// 
        /// <para>
        /// To encrypt the data for the files added to the <c>ZipFile</c> instance, set
        /// the Password property after creating the <c>ZipFile</c> instance.
        /// </para>
        /// 
        /// <para>
        /// Instances of the <c>ZipFile</c> class are not multi-thread safe.  You may
        /// not party on a single instance with multiple threads.  You may have multiple
        /// threads that each use a distinct <c>ZipFile</c> instance, or you can
        /// synchronize multi-thread access to a single instance.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <exception cref="Ionic.Zip.ZipException">
        /// Thrown if name refers to an existing file that is not a valid zip file. 
        /// </exception>
        ///
        /// <example>
        /// <code>
        /// using (ZipFile zip = new ZipFile("Backup.zip", Console.Out))
        /// { 
        ///   // Store all files found in the top level directory, into the zip archive.
        ///   // note: this code does not recurse subdirectories!
        ///   // Status messages will be written to Console.Out
        ///   String[] filenames = System.IO.Directory.GetFiles(DirectoryToZip);
        ///   zip.AddFiles(filenames);
        ///   zip.Save();
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As New ZipFile("Backup.zip", Console.Out)
        ///     ' Store all files found in the top level directory, into the zip archive.
        ///     ' note: this code does not recurse subdirectories!
        ///     ' Status messages will be written to Console.Out
        ///     Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        ///     zip.AddFiles(filenames)
        ///     zip.Save()
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="fileName">The filename to use for the new zip archive.</param>
        /// <param name="statusMessageWriter">A TextWriter to use for writing 
        /// verbose status messages.</param>
        public ZipFile(string fileName, TextWriter statusMessageWriter)
        {
            try
            {
                _InitInstance(fileName, statusMessageWriter);
            }
            catch (Exception e1)
            {
                throw new ZipException(String.Format("{0} is not a valid zip file", fileName), e1);
            }
        }


        /// <summary>
        /// Creates a new <c>ZipFile</c> instance, using the specified name for the
        /// filename, the specified status message writer, and the specified Encoding.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This constructor works like the <see cref="ZipFile(String)">ZipFile
        /// constructor that accepts a single string argument.</see> See that reference
        /// for detail on what this constructor does.
        /// </para>
        ///
        /// <para>
        /// This version of the constructor allows the caller to pass in a TextWriter,
        /// and an Encoding.  The TextWriter will collect verbose messages that are
        /// generated by the library during extraction or creation of the zip archive.
        /// A console application may wish to pass System.Console.Out to get messages on
        /// the Console. A graphical or headless application may wish to capture the
        /// messages in a different <c>TextWriter</c>, for example, a
        /// <c>StringWriter</c>, and then display the messages in a TextBox, or generate
        /// an audit log of ZipFile operations.
        /// </para>
        /// 
        /// <para>
        /// The Encoding is used as the default alternate encoding for entries with
        /// filenames or comments that cannot be encoded with the IBM437 code page.
        /// This is a equivalent to setting the <see
        /// cref="ProvisionalAlternateEncoding"/> property on the <c>ZipFile</c>
        /// instance after construction.
        /// </para>
        /// 
        /// <para>
        /// To encrypt the data for the files added to the <c>ZipFile</c> instance, set
        /// the Password property after creating the <c>ZipFile</c> instance.
        /// </para>
        /// 
        /// <para>
        /// Instances of the <c>ZipFile</c> class are not multi-thread safe.  You may
        /// not party on a single instance with multiple threads.  You may have multiple
        /// threads that each use a distinct ZipFile instance, or you can synchronize
        /// multi-thread access to a single instance.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <exception cref="Ionic.Zip.ZipException">
        /// Thrown if name refers to an existing file that is not a valid zip file. 
        /// </exception>
        ///
        /// <param name="fileName">The filename to use for the new zip archive.</param>
        /// <param name="statusMessageWriter">A TextWriter to use for writing verbose 
        /// status messages.</param>
        /// <param name="encoding">
        /// The Encoding is used as the default alternate encoding for entries with
        /// filenames or comments that cannot be encoded with the IBM437 code page.
        /// </param>
        public ZipFile(string fileName, TextWriter statusMessageWriter,
                       System.Text.Encoding encoding)
        {
            try
            {
                _InitInstance(fileName, statusMessageWriter);
                ProvisionalAlternateEncoding = encoding;
            }
            catch (Exception e1)
            {
                throw new ZipException(String.Format("{0} is not a valid zip file", fileName), e1);
            }
        }




        /// <summary>
        /// Initialize a <c>ZipFile</c> instance by reading in a zip file.
        /// </summary>
        /// <remarks>
        ///
        /// <para>
        /// This method is primarily useful from COM Automation environments, when
        /// reading or extracting zip files. In COM, it is not possible to invoke
        /// parameterized constructors for a class. A COM Automation application can
        /// update a zip file by using the default (no argument) constructor, then
        /// calling Initialize() to read the contents of an on-disk zip archive into the
        /// <c>ZipFile</c> instance.
        /// </para>
        ///
        /// <para>
        /// .NET applications are encouraged to use the <c>ZipFile.Read()</c> methods for
        /// better clarity.
        /// </para>
        ///
        /// </remarks>
        /// <param name="fileName">the name of the existing zip file to read in.</param>
        public void Initialize(string fileName)
        {
            try
            {
                _InitInstance(fileName, null);
            }
            catch (Exception e1)
            {
                throw new ZipException(String.Format("{0} is not a valid zip file", fileName), e1);
            }
        }


        private void _InitInstance(string zipFileName, TextWriter statusMessageWriter)
        {
            // create a new zipfile
            _name = zipFileName;
            _StatusMessageTextWriter = statusMessageWriter;
            _contentsChanged = true;
            CompressionLevel = Ionic.Zlib.CompressionLevel.Default;
            // workitem 7685
            _entries = new System.Collections.Generic.List<ZipEntry>();
            if (File.Exists(_name))
            {
                if (FullScan)
                    ReadIntoInstance_Orig(this);
                else
                    ReadIntoInstance(this);
                this._fileAlreadyExists = true;
            }

            return;
        }
        #endregion



        #region Indexers and Collections


        /// <summary>
        /// This is an integer indexer into the Zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This property is read-write. But don't get too excited: When setting the
        /// value, the only legal value is <c>null</c> (<c>Nothing</c> in VB). If you
        /// assign a non-null value, the setter will throw an exception.
        /// </para>
        ///
        /// <para>
        /// Setting the value to <c>null</c> is equivalent to calling <see
        /// cref="ZipFile.RemoveEntry(String)"/> with the filename for the given entry.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="System.ArgumentException">
        /// Thrown if the caller attempts to assign a non-null value to the indexer, 
        /// or if the caller uses an out-of-range index value.
        /// </exception>
        ///
        /// <param name="ix">
        /// The index value.
        /// </param>
        /// 
        /// <returns>
        /// The <c>ZipEntry</c> within the Zip archive at the specified index. If the 
        /// entry does not exist in the archive, this indexer throws.
        /// </returns>
        /// 
        public ZipEntry this[int ix]
        {
            // workitem 6402
            get
            {
                return _entries[ix];
            }

            set
            {
                if (value != null)
                    throw new ZipException("You may not set this to a non-null ZipEntry value.",
                                           new ArgumentException("this[int]"));
                RemoveEntry(_entries[ix]);
            }
        }


        /// <summary>
        /// This is a name-based indexer into the Zip archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Retrieval by the string-based indexer is done on a case-insensitive basis,
        /// by default.  Set the <see cref="CaseSensitiveRetrieval"/> property to use
        /// case-sensitive comparisons.
        /// </para>
        ///
        /// <para>
        /// This property is read-write. When setting the value, the only legal value is
        /// <c>null</c> (<c>Nothing</c> in VB). Setting the value to <c>null</c> is
        /// equivalent to calling <see cref="ZipFile.RemoveEntry(String)"/> with the
        /// filename.
        /// </para>
        ///
        /// <para>
        /// If you assign a non-null value, the setter will throw an exception.
        /// </para>
        ///
        /// <para>
        /// It is can be true that <c>this[value].FileName == value</c>, but not
        /// always. In other words, the <c>FileName</c> property of the <c>ZipEntry</c>
        /// you retrieve with this indexer, can be equal to the index value, but not
        /// always.  In the case of directory entries in the archive, you may retrieve
        /// them with the name of the directory with no trailing slash, even though in
        /// the entry itself, the actual <see cref="ZipEntry.FileName"/> property may
        /// include a trailing slash.  In other words, for a directory entry named
        /// "dir1", you may find <c>zip["dir1"].FileName == "dir1/"</c>. Also, for any
        /// entry with slashes, they are stored in the zip file as forward slashes, but
        /// you may retrieve them with either forward or backslashes.  So,
        /// <c>zip["dir1\\entry1.txt"].FileName == "dir1/entry.txt"</c>.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// This example extracts only the entries in a zip file that are .txt files.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read("PackedDocuments.zip"))
        /// {
        ///   foreach (string s1 in zip.EntryFilenames)
        ///   {
        ///     if (s1.EndsWith(".txt"))
        ///       zip[s1].Extract("textfiles");
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
        /// <seealso cref="Ionic.Zip.ZipFile.RemoveEntry(string)"/>
        ///
        /// <exception cref="System.ArgumentException">
        /// Thrown if the caller attempts to assign a non-null value to the indexer.
        /// </exception>
        ///
        /// <param name="fileName">
        /// The name of the file, including any directory path, to retrieve from the zip. 
        /// The filename match is not case-sensitive by default; you can use the
        /// <see cref="CaseSensitiveRetrieval"/> property to change this behavior. The
        /// pathname can use forward-slashes or backward slashes.
        /// </param>
        /// 
        /// <returns>
        /// The <c>ZipEntry</c> within the Zip archive, given by the specified
        /// filename. If the named entry does not exist in the archive, this indexer
        /// returns <c>null</c> (<c>Nothing</c> in VB).
        /// </returns>
        /// 
        public ZipEntry this[String fileName]
        {
            get
            {
                foreach (ZipEntry e in _entries)
                {
                    if (this.CaseSensitiveRetrieval)
                    {
                        // check for the file match with a case-sensitive comparison.
                        if (e.FileName == fileName) return e;
                        // also check for equivalence
                        if (fileName.Replace("\\", "/") == e.FileName) return e;
                        if (e.FileName.Replace("\\", "/") == fileName) return e;

                        // check for a difference only in trailing slash
                        if (e.FileName.EndsWith("/"))
                        {
                            var fileNameNoSlash = e.FileName.Trim("/".ToCharArray());
                            if (fileNameNoSlash == fileName) return e;
                            // also check for equivalence
                            if (fileName.Replace("\\", "/") == fileNameNoSlash) return e;
                            if (fileNameNoSlash.Replace("\\", "/") == fileName) return e;
                        }

                    }
                    else
                    {
                        // check for the file match in a case-insensitive manner.
                        if (String.Compare(e.FileName, fileName, StringComparison.CurrentCultureIgnoreCase) == 0) return e;
                        // also check for equivalence
                        if (String.Compare(fileName.Replace("\\", "/"), e.FileName, StringComparison.CurrentCultureIgnoreCase) == 0) return e;
                        if (String.Compare(e.FileName.Replace("\\", "/"), fileName, StringComparison.CurrentCultureIgnoreCase) == 0) return e;

                        // check for a difference only in trailing slash
                        if (e.FileName.EndsWith("/"))
                        {
                            var fileNameNoSlash = e.FileName.Trim("/".ToCharArray());

                            if (String.Compare(fileNameNoSlash, fileName, StringComparison.CurrentCultureIgnoreCase) == 0) return e;
                            // also check for equivalence
                            if (String.Compare(fileName.Replace("\\", "/"), fileNameNoSlash, StringComparison.CurrentCultureIgnoreCase) == 0) return e;
                            if (String.Compare(fileNameNoSlash.Replace("\\", "/"), fileName, StringComparison.CurrentCultureIgnoreCase) == 0) return e;

                        }

                    }

                }
                return null;
            }

            set
            {
                if (value != null)
                    throw new ArgumentException("You may not set this to a non-null ZipEntry value.");
                RemoveEntry(fileName);
            }
        }

        /// <summary>
        /// The list of filenames for the entries contained within the zip archive.  
        /// </summary>
        ///
        /// <remarks>
        /// According to the ZIP specification, the names of the entries use forward
        /// slashes in pathnames.  If you are scanning through the list, you may have to
        /// swap forward slashes for backslashes.
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.this[string]"/>
        ///
        /// <example>
        /// This example shows one way to test if a filename is already contained within 
        /// a zip archive.
        /// <code>
        /// String ZipFileToRead= "PackedDocuments.zip";
        /// string Candidate = "DatedMaterial.xps";
        /// using (ZipFile zip = new ZipFile(ZipFileToRead))
        /// {
        ///   if (zip.EntryFilenames.Contains(Candidate))
        ///     Console.WriteLine("The file '{0}' exists in the zip archive '{1}'",
        ///                       Candidate,
        ///                       ZipFileName);
        ///   else
        ///     Console.WriteLine("The file, '{0}', does not exist in the zip archive '{1}'",
        ///                       Candidate,
        ///                       ZipFileName);
        ///   Console.WriteLine();
        /// }
        /// </code>
        /// <code lang="VB">
        ///   Dim ZipFileToRead As String = "PackedDocuments.zip"
        ///   Dim Candidate As String = "DatedMaterial.xps"
        ///   Using zip As New ZipFile(ZipFileToRead)
        ///       If zip.EntryFilenames.Contains(Candidate) Then
        ///           Console.WriteLine("The file '{0}' exists in the zip archive '{1}'", _
        ///                       Candidate, _
        ///                       ZipFileName)
        ///       Else
        ///         Console.WriteLine("The file, '{0}', does not exist in the zip archive '{1}'", _
        ///                       Candidate, _
        ///                       ZipFileName)
        ///       End If
        ///       Console.WriteLine
        ///   End Using
        /// </code>
        /// </example>
        ///
        /// <returns>
        /// The list of strings for the filenames contained within the Zip archive.
        /// </returns>
        /// 
        public System.Collections.ObjectModel.ReadOnlyCollection<string> EntryFileNames
        {
            get
            {
                var foo = _entries.ConvertAll((e) => e.FileName );
                return foo.AsReadOnly();
            }
        }


        /// <summary>
        /// Returns the readonly collection of entries in the Zip archive.
        /// </summary>
        /// <remarks>
        /// If there are no entries in the current ZipFile, the value returned is a
        /// non-null zero-element collection.
        /// </remarks>
        public System.Collections.ObjectModel.ReadOnlyCollection<ZipEntry> Entries
        {
            get
            {
                return _entries.AsReadOnly();
            }
        }


        /// <summary>
        /// Returns the number of entries in the Zip archive.
        /// </summary>
        public int Count
        {
            get
            {
                return _entries.Count;
            }
        }



        /// <summary>
        /// Removes the given ZipEntry from the zip archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// After calling <c>RemoveEntry</c>, the application must call <c>Save</c> to
        /// make the changes permanent.
        /// </para>
        /// </remarks>
        ///
        /// <exception cref="System.ArgumentException">
        /// Thrown if the specified ZipEntry does not exist in the <c>ZipFile</c>.
        /// </exception>
        ///
        /// <example>
        /// In this example, all entries in the zip archive dating from before December
        /// 31st, 2007, are removed from the archive.  This is actually much easier if
        /// you use the RemoveSelectedEntries method.  But I needed an example for
        /// RemoveEntry, so here it is.
        /// <code>
        /// String ZipFileToRead = "ArchiveToModify.zip";
        /// System.DateTime Threshold = new System.DateTime(2007,12,31);
        /// using (ZipFile zip = ZipFile.Read(ZipFileToRead))
        /// {
        ///   var EntriesToRemove = new System.Collections.Generic.List&lt;ZipEntry&gt;();
        ///   foreach (ZipEntry e in zip)
        ///   {
        ///     if (e.LastModified &lt; Threshold)
        ///     {
        ///       // We cannot remove the entry from the list, within the context of 
        ///       // an enumeration of said list.
        ///       // So we add the doomed entry to a list to be removed later.
        ///       EntriesToRemove.Add(e);
        ///     }
        ///   }
        ///   
        ///   // actually remove the doomed entries. 
        ///   foreach (ZipEntry zombie in EntriesToRemove)
        ///     zip.RemoveEntry(zombie);
        ///   
        ///   zip.Comment= String.Format("This zip archive was updated at {0}.", 
        ///                              System.DateTime.Now.ToString("G"));
        ///
        ///   // save with a different name
        ///   zip.Save("Archive-Updated.zip");
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        ///   Dim ZipFileToRead As String = "ArchiveToModify.zip"
        ///   Dim Threshold As New DateTime(2007, 12, 31)
        ///   Using zip As ZipFile = ZipFile.Read(ZipFileToRead)
        ///       Dim EntriesToRemove As New System.Collections.Generic.List(Of ZipEntry)
        ///       Dim e As ZipEntry
        ///       For Each e In zip
        ///           If (e.LastModified &lt; Threshold) Then
        ///               ' We cannot remove the entry from the list, within the context of 
        ///               ' an enumeration of said list.
        ///               ' So we add the doomed entry to a list to be removed later.
        ///               EntriesToRemove.Add(e)
        ///           End If
        ///       Next
        ///   
        ///       ' actually remove the doomed entries. 
        ///       Dim zombie As ZipEntry
        ///       For Each zombie In EntriesToRemove
        ///           zip.RemoveEntry(zombie)
        ///       Next
        ///       zip.Comment = String.Format("This zip archive was updated at {0}.", DateTime.Now.ToString("G"))
        ///       'save as a different name
        ///       zip.Save("Archive-Updated.zip")
        ///   End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="entry">
        /// The <c>ZipEntry</c> to remove from the zip. 
        /// </param>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.RemoveSelectedEntries(string)"/>
        ///
        public void RemoveEntry(ZipEntry entry)
        {
            if (!_entries.Contains(entry))
                throw new ArgumentException("The entry you specified does not exist in the zip archive.");

            _entries.Remove(entry);

#if NOTNEEDED
            if (_direntries != null)
            {
                bool FoundAndRemovedDirEntry = false;
                foreach (ZipDirEntry de1 in _direntries)
                {
                    if (entry.FileName == de1.FileName)
                    {
                        _direntries.Remove(de1);
                        FoundAndRemovedDirEntry = true;
                        break;
                    }
                }

                if (!FoundAndRemovedDirEntry)
                    throw new BadStateException("The entry to be removed was not found in the directory.");
            }
#endif
            _contentsChanged = true;
        }




        /// <summary>
        /// Removes the <c>ZipEntry</c> with the given filename from the zip archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// After calling <c>RemoveEntry</c>, the application must call <c>Save</c> to
        /// make the changes permanent.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the <c>ZipFile</c> is not updatable. 
        /// </exception>
        ///
        /// <exception cref="System.ArgumentException">
        /// Thrown if a ZipEntry with the specified filename does not exist in the <c>ZipFile</c>.
        /// </exception>
        ///
        /// <example>
        /// This example shows one way to remove an entry with a given filename from an 
        /// existing zip archive.
        /// <code>
        /// String ZipFileToRead= "PackedDocuments.zip";
        /// string Candidate = "DatedMaterial.xps";
        /// using (ZipFile zip = new ZipFile(ZipFileToRead))
        /// {
        ///   if (zip.EntryFilenames.Contains(Candidate))
        ///   {
        ///     zip.RemoveEntry(Candidate);
        ///     zip.Comment= String.Format("The file '{0}' has been removed from this archive.", 
        ///                                Candidate);
        ///     zip.Save();
        ///   }
        /// }
        /// </code>
        /// <code lang="VB">
        ///   Dim ZipFileToRead As String = "PackedDocuments.zip"
        ///   Dim Candidate As String = "DatedMaterial.xps"
        ///   Using zip As ZipFile = New ZipFile(ZipFileToRead)
        ///       If zip.EntryFilenames.Contains(Candidate) Then
        ///           zip.RemoveEntry(Candidate)
        ///           zip.Comment = String.Format("The file '{0}' has been removed from this archive.", Candidate)
        ///           zip.Save
        ///       End If
        ///   End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="fileName">
        /// The name of the file, including any directory path, to remove from the zip. 
        /// The filename match is not case-sensitive by default; you can use the
        /// <c>CaseSensitiveRetrieval</c> property to change this behavior. The
        /// pathname can use forward-slashes or backward slashes.
        /// </param>
        /// 
        public void RemoveEntry(String fileName)
        {
            string modifiedName = ZipEntry.NameInArchive(fileName, null);
            ZipEntry e = this[modifiedName];
            if (e == null)
                throw new ArgumentException("The entry you specified was not found in the zip archive.");

            RemoveEntry(e);
        }


        #endregion

        #region Destructors and Disposers

        /// <summary>
        /// This is the class Destructor, which gets called implicitly when the instance
        /// is destroyed.  Because the <c>ZipFile</c> type implements IDisposable, this
        /// method calls Dispose(false).
        /// </summary>
        ~ZipFile()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            Dispose(false);
        }

        /// <summary>
        /// Handles closing of the read and write streams associated
        /// to the <c>ZipFile</c>, if necessary.  
        /// </summary>
        ///
        /// <remarks>
        /// The Dispose() method is generally 
        /// employed implicitly, via a using() {} statement. (Using...End Using in VB)
        /// Always use a using statement, or always insure that you are calling Dispose() 
        /// explicitly.
        /// </remarks>
        ///
        /// <example>
        /// This example extracts an entry selected by name, from the Zip file to the
        /// Console.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(zipfile))
        /// {
        ///   foreach (ZipEntry e in zip)
        ///   {
        ///     if (WantThisEntry(e.FileName)) 
        ///       zip.Extract(e.FileName, Console.OpenStandardOutput());
        ///   }
        /// } // Dispose() is called implicitly here.
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip As ZipFile = ZipFile.Read(zipfile)
        ///     Dim e As ZipEntry
        ///     For Each e In zip
        ///       If WantThisEntry(e.FileName) Then
        ///           zip.Extract(e.FileName, Console.OpenStandardOutput())
        ///       End If
        ///     Next
        /// End Using ' Dispose is implicity called here
        /// </code>
        /// </example>
        public void Dispose()
        {
            // dispose of the managed and unmanaged resources
            Dispose(true);

            // tell the GC that the Finalize process no longer needs
            // to be run for this object.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The Dispose() method.  It disposes any managed resources, 
        /// if the flag is set, then marks the instance disposed.
        /// This method is typically not called from application code.
        /// </summary>
        /// <param name="disposeManagedResources">indicates whether the
        /// method should dispose streams or not.</param>
        protected virtual void Dispose(bool disposeManagedResources)
        {
            if (!this._disposed)
            {
                if (disposeManagedResources)
                {
                    // dispose managed resources
                    if (_ReadStreamIsOurs)
                    {
                        if (_readstream != null)
                        {
                            // workitem 7704
#if NETCF20
                            _readstream.Close();
#else
                            _readstream.Dispose();
#endif
                            _readstream = null;
                        }
                    }
                    // only dispose the writestream if there is a backing file 
                    //(_temporaryFileName is not null)
                    if ((_temporaryFileName != null) && (_name != null))
                        if (_writestream != null)
                        {
                            // workitem 7704
#if NETCF20
                            _writestream.Close();
#else
                            _writestream.Dispose();
#endif
                            _writestream = null;
                        }
                }
                this._disposed = true;
            }
        }
        #endregion


        #region private properties

        private Stream WriteStream
        {
            get
            {
                if (_writestream == null)
                {
                    if (_name != null)
                    {

                        if (TempFileFolder == ".")
                            _temporaryFileName = SharedUtilities.GetTempFilename();
                        else if (TempFileFolder != null)
                            _temporaryFileName = Path.Combine(TempFileFolder, SharedUtilities.GetTempFilename());
                        else // null
                        {
                            var d = Path.GetDirectoryName(_name);
                            _temporaryFileName = Path.Combine(d, SharedUtilities.GetTempFilename());
                        }
                        _writestream = new FileStream(_temporaryFileName, FileMode.CreateNew);
                    }
                }
                return _writestream;
            }
            set
            {
                if (value != null)
                    throw new ZipException("Whoa!", new ArgumentException("Cannot set the stream to a non-null value.", "value"));
                _writestream = null;
            }
        }
        #endregion

        #region private fields
        private TextWriter _StatusMessageTextWriter;
        private bool _CaseSensitiveRetrieval;
        private Stream _readstream;
        private Stream _writestream;
        private bool _disposed;
        private System.Collections.Generic.List<ZipEntry> _entries;
        private bool _ForceNoCompression;
        private string _name;
        private string _Comment;
        internal string _Password;
        private bool _emitNtfsTimes = true;
        private bool _emitUnixTimes;
        private Ionic.Zlib.CompressionStrategy _Strategy = Ionic.Zlib.CompressionStrategy.Default;
        private long _originPosition; 
        private bool _fileAlreadyExists;
        private string _temporaryFileName;
        private bool _contentsChanged;
        private bool _hasBeenSaved;
        private String _TempFileFolder;
        private bool _ReadStreamIsOurs = true;
        private object LOCK = new object();
        private bool _saveOperationCanceled;
        private bool _extractOperationCanceled;
        private EncryptionAlgorithm _Encryption;
        private bool _JustSaved;
        private bool _NeedZip64CentralDirectory;
        private long _locEndOfCDS = -1;
        private Nullable<bool> _OutputUsesZip64;
        internal bool _inExtractAll;
        private System.Text.Encoding _provisionalAlternateEncoding = System.Text.Encoding.GetEncoding("IBM437"); // default = IBM437

        private int _BufferSize = 8192;
        
        internal Zip64Option _zip64 = Zip64Option.Default;
        #pragma warning disable 649
        private bool _SavingSfx; 
        #pragma warning restore 649

        #endregion
    }

    /// <summary>
    /// Options for using ZIP64 extensions when saving zip archives. 
    /// </summary>
    public enum Zip64Option
    {
        /// <summary>
        /// The default behavior, which is "Never".
        /// (For COM clients, this is a 0 (zero).)
        /// </summary>
        Default = 0,
        /// <summary>
        /// Do not use ZIP64 extensions when writing zip archives.
        /// (For COM clients, this is a 0 (zero).)
        /// </summary>
        Never = 0,
        /// <summary>
        /// Use ZIP64 extensions when writing zip archives, as necessary. 
        /// For example, when a single entry exceeds 0xFFFFFFFF in size, or when the archive as a whole 
        /// exceeds 0xFFFFFFFF in size, or when there are more than 65535 entries in an archive.
        /// (For COM clients, this is a 1.)
        /// </summary>
        AsNecessary = 1,
        /// <summary>
        /// Always use ZIP64 extensions when writing zip archives, even when unnecessary.
        /// (For COM clients, this is a 2.)
        /// </summary>
        Always
    }


    enum AddOrUpdateAction
    {
        AddOnly = 0,
        AddOrUpdate
    }

}



// ==================================================================
//
// Information on the ZIP format:
//
// From
// http://www.pkware.com/documents/casestudies/APPNOTE.TXT
//
//  Overall .ZIP file format:
//
//     [local file header 1]
//     [file data 1]
//     [data descriptor 1]  ** sometimes
//     . 
//     .
//     .
//     [local file header n]
//     [file data n]
//     [data descriptor n]   ** sometimes
//     [archive decryption header] 
//     [archive extra data record] 
//     [central directory]
//     [zip64 end of central directory record]
//     [zip64 end of central directory locator] 
//     [end of central directory record]
//
// Local File Header format:
//         local file header signature ... 4 bytes  (0x04034b50)
//         version needed to extract ..... 2 bytes
//         general purpose bit field ..... 2 bytes
//         compression method ............ 2 bytes
//         last mod file time ............ 2 bytes
//         last mod file date............. 2 bytes
//         crc-32 ........................ 4 bytes
//         compressed size................ 4 bytes
//         uncompressed size.............. 4 bytes
//         file name length............... 2 bytes
//         extra field length ............ 2 bytes
//         file name                       varies
//         extra field                     varies
//
//
// Data descriptor:  (used only when bit 3 of the general purpose bitfield is set)
//         (although, I have found zip files where bit 3 is not set, yet this descriptor is present!)
//         local file header signature     4 bytes  (0x08074b50)  ** sometimes!!! Not always
//         crc-32                          4 bytes
//         compressed size                 4 bytes
//         uncompressed size               4 bytes
//
//
//   Central directory structure:
//
//       [file header 1]
//       .
//       .
//       . 
//       [file header n]
//       [digital signature] 
//
//
//       File header:  (This is a ZipDirEntry)
//         central file header signature   4 bytes  (0x02014b50)
//         version made by                 2 bytes
//         version needed to extract       2 bytes
//         general purpose bit flag        2 bytes
//         compression method              2 bytes
//         last mod file time              2 bytes
//         last mod file date              2 bytes
//         crc-32                          4 bytes
//         compressed size                 4 bytes
//         uncompressed size               4 bytes
//         file name length                2 bytes
//         extra field length              2 bytes
//         file comment length             2 bytes
//         disk number start               2 bytes
//         internal file attributes **     2 bytes
//         external file attributes ***    4 bytes
//         relative offset of local header 4 bytes
//         file name (variable size)
//         extra field (variable size)
//         file comment (variable size)
//
// ** The internal file attributes, near as I can tell, 
// uses 0x01 for a file and a 0x00 for a directory. 
//
// ***The external file attributes follows the MS-DOS file attribute byte, described here:
// at http://support.microsoft.com/kb/q125019/
// 0x0010 => directory
// 0x0020 => file 
//
//
// End of central directory record:
//
//         end of central dir signature    4 bytes  (0x06054b50)
//         number of this disk             2 bytes
//         number of the disk with the
//         start of the central directory  2 bytes
//         total number of entries in the
//         central directory on this disk  2 bytes
//         total number of entries in
//         the central directory           2 bytes
//         size of the central directory   4 bytes
//         offset of start of central
//         directory with respect to
//         the starting disk number        4 bytes
//         .ZIP file comment length        2 bytes
//         .ZIP file comment       (variable size)
//
// date and time are packed values, as MSDOS did them
// time: bits 0-4 : seconds (divided by 2)
//            5-10: minute
//            11-15: hour
// date  bits 0-4 : day
//            5-8: month
//            9-15 year (since 1980)
//
// see http://msdn.microsoft.com/en-us/library/ms724274(VS.85).aspx

