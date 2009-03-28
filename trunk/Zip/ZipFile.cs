// ZipFile.cs
//
// Copyright (c) 2006, 2007, 2008, 2009 Microsoft Corporation.  All rights reserved.
//
// This class library reads and writes zip files, according to the format
// described by pkware, at:
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


namespace Ionic.Zip
{
    /// <summary>
    /// The ZipFile type represents a zip archive file.  This is the main type in the DotNetZip
    /// class library.  This class reads and writes zip files, as defined in the format for zip
    /// described by PKWare.  The compression for this implementation was, at one time, based on
    /// the System.IO.Compression.DeflateStream base class in the .NET Framework base class
    /// library, available in v2.0 and later of the .NET Framework. As of v1.7 of DotNetZip, the
    /// compression is provided by a managed-code version of Zlib, included with DotNetZip.
    /// </summary>
    public partial class ZipFile : System.Collections.Generic.IEnumerable<ZipEntry>,
    IDisposable
    {

        #region public properties


        /// <summary>
        /// Size of the IO Buffer used while saving.
        /// </summary>
        /// <remarks>
        /// You really don't need to bother with this.  It is here to allow for optimizations that
        /// you probably won't make! The default buffer size is 8k. 
        /// </remarks>
        public int BufferSize
        {
            get;
            set;
        }

        /// <summary>
        /// Size of the work buffer to use for the ZLIB codec during compression.
        /// </summary>
        public int CodecBufferSize
        {
            get;
            set;
        }


        private Ionic.Zlib.CompressionStrategy _Strategy = Ionic.Zlib.CompressionStrategy.DEFAULT;
        /// <summary>
        /// The compression strategy to use for all entries. 
        /// </summary>
        public Ionic.Zlib.CompressionStrategy Strategy
        {
            get { return _Strategy; }
            set { _Strategy = value; }
        }

        /// <summary>
        /// This read-only property specifies the name of the zipfile to read or
        /// write. It can be implicitly set when the instance of the ZipFile type is
        /// created, if you use the <see cref="ZipFile(String)"/> Constructor, or it is
        /// set when <see cref="ZipFile.Save(String)"/> is called. When instantiating a
        /// ZipFile to read from or write to a stream, the Name property remains null.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }


        /// <summary>
        /// Sets the compression level to be used for entries when saving the zip archive.
        /// </summary>
        /// <remarks>
        /// The compression level setting is used at the time of Save(). The same level is applied
        /// to all ZipEntry instances contained in the ZipFile during the save.  If you do not set
        /// this property, the default compression level is used, which normally gives a good
        /// balance of compression efficiency and compression speed.  In some tests, using
        /// BEST_COMPRESSION can double the time it takes to compress, while delivering just a
        /// small increase in compression efficiency.  This behavior will vary with the type of
        /// data you compress.  If you are in doubt, just leave this setting alone, and accept the
        /// default.
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
        /// This property is read/write for the zipfile. It allows the application to specify a
        /// comment for the zipfile, or read the comment for the zipfile.  After setting this
        /// property, changes are only made permanent when you call a <c>Save()</c> method.
        /// </para>
        ///
        /// <para>
        /// According to the zip specification, the comment is not encrypted, even if there is a
        /// password set on the zip archive.
        /// </para>
        ///
        /// <para>
        /// The zip spec does not describe how to encode the comment string in a code page other
        /// than IBM437.  Therefore, for "compliant" zip tools and libraries, comments will use
        /// IBM437.  However, there are situations where you want an encoded Comment, for example
        /// using code page 950 "Big-5 Chinese".  DotNetZip will encode the comment in the code
        /// page specified by <see cref="ProvisionalAlternateEncoding"/>, at the time of the call
        /// to ZipFile.Save().
        /// </para>
        ///
        /// <para>
        /// When creating a zip archive using this library, it is possible to change the value of
        /// <see cref="ProvisionalAlternateEncoding" /> between each entry you add, and between
        /// adding entries and the call to Save(). Don't do this.  It will likely result in a
        /// zipfile that is not readable by any tool or application.  For best interoperability,
        /// leave <see cref="ProvisionalAlternateEncoding" /> alone, or specify it only once,
        /// before adding any entries to the ZipFile instance.
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


#if LEGACY
        /// <summary>
        /// When this is set, any volume name (eg C:) is trimmed 
        /// from fully-qualified pathnames on any ZipEntry, before writing the 
        /// ZipEntry into the ZipFile. 
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
        /// Indicates whether verbose output is sent to the StatusMessageWriter during
        /// <c>AddXxx()</c> and <c>ReadXxx()</c> operations.
        /// </summary>
        ///
        /// <remarks>
        /// This is a synthetic property.  It returns true if the <see
        /// cref="StatusMessageTextWriter"/> is non-null.
        /// </remarks>
        private bool Verbose
        {
            get { return (_StatusMessageTextWriter != null); }
            //set { _Verbose = value; }
        }


        /// <summary>
        /// Indicates whether to perform case-sensitive matching on the filename when retrieving
        /// entries in the zipfile via the string-based indexer.  
        /// </summary>
        ///
        /// <remarks>
        /// The default value is <c>false</c>, which means DON'T do case-sensitive matching. In
        /// other words, retrieving zip["ReadMe.Txt"] is the same as zip["readme.txt"].  It really
        /// makes sense to set this to <c>true</c> only if you are not running on Windows, which
        /// has case-insensitive filenames. But since this library is not built for non-Windows
        /// platforms, in most cases you should just leave this property alone.
        /// </remarks>
        public bool CaseSensitiveRetrieval
        {
            get { return _CaseSensitiveRetrieval; }
            set { _CaseSensitiveRetrieval = value; }
        }


        /// <summary>
        /// Indicates whether to encode entry filenames and entry comments using Unicode 
        /// (UTF-8) according to the PKWare specification, for those filenames and comments
        /// that cannot be encoded in the IBM437 character set.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The PKWare specification provides for encoding in either the IBM437 code page, or in
        /// UTF-8.  This flag selects the encoding according to that specification.  By default,
        /// this flag is false, and filenames and comments are encoded into the zip file in the
        /// IBM437 codepage.  Setting this flag to true will specify that filenames and comments
        /// are encoded with UTF-8.
        /// </para>
        ///
        /// <para>
        /// Zip files created with strict adherence to the PKWare specification with respect to
        /// UTF-8 encoding can contain entries with filenames containing any combination of
        /// Unicode characters, including the full range of characters from Chinese, Latin,
        /// Hebrew, Greek, Cyrillic, and many other alphabets.  However, because the UTF-8 portion
        /// of the PKWare specification is not broadly supported by other zip libraries and
        /// utilities, such zip files may not be readable by your favorite zip tool or
        /// archiver. In other words, interoperability will decrease if you set this flag to true.
        /// </para>
        ///
        /// <para>
        /// In particular, Zip files created with strict adherence to the PKWare specification
        /// with respect to UTF-8 encoding will not work well with Explorer in Windows XP or
        /// Windows Vista, because Vista compressed folders do not support UTF-8 in zip files.
        /// Vista can read the zip files, but shows the filenames incorrectly.  Unpacking from
        /// Windows Vista Explorer will result in filenames that have rubbish characters in place
        /// of the high-order UTF-8 bytes.
        /// </para>
        ///
        /// <para>
        /// Also, zip files that use UTF-8 encoding will not work well with Java applications that
        /// use the java.util.zip classes, as of v5.0 of the Java runtime. The Java runtime does
        /// not correctly implement the PKWare specification in this regard.
        /// </para>
        ///
        /// <para>
        /// As a result, we have the unfortunate situation that "correct" behavior by the
        /// DotNetZip library with regard to Unicode during zip creation will result in zip files
        /// that are readable by strictly compliant and current tools (for example the most recent
        /// release of the commercial WinZip tool); but these zip files will not be readable by
        /// various other tools or libraries, including Windows Explorer.
        /// </para>
        ///
        /// <para>
        /// The DotNetZip library can read and write zip files with UTF8-encoded entries,
        /// according to the PKware spec.  If you use DotNetZip for both creating and reading the
        /// zip file, and you use UTF-8, there will be no loss of information in the
        /// filenames. For example, using a self-extractor created by this library will allow you
        /// to unpack files correctly with no loss of information in the filenames.
        /// </para>
        ///
        /// <para>
        /// Encoding filenames and comments using the IBM437 codepage, the default behavior, will
        /// cause loss of information on some filenames, but the resulting zipfile will be more
        /// interoperable with other utilities. As an example of the loss of information, the
        /// o-tilde character will be down-coded to plain o.  Likewise, the O with a stroke
        /// through it, used in Danish and Norwegian, will be down-coded to plain o. Chinese
        /// characters cannot be represented in codepage IBM437; when using the default encoding,
        /// Chinese characters in filenames will be represented as ?.
        /// </para>
        ///
        /// <para>
        /// The loss of information associated to the use of the IBM437 encoding can lead to
        /// runtime errors. For example, using IBM437, any sequence of 4 Chinese characters will
        /// be encoded as ????.  If your application creates a ZipFile, then adds two files, each
        /// with names of four Chinese characters each, this will result in a duplicate filename
        /// exception.  In the case where you add a single file with a name containing four
        /// Chinese characters, calling Extract() on the entry that has question marks in the
        /// filename will result in an exception, because the question mark is not legal for use
        /// within filenames on Windows.  These are just a few examples of the problems associated
        /// to loss of information.
        /// </para>
        ///
        /// <para>
        /// This flag is independent of the encoding of the content within the 
        /// entries in the zip file.  
        /// </para>
        ///
        /// <para>
        /// Rather than specify the encoding in a binary fashion using this flag, an application
        /// can specify an arbitrary encoding via the <see cref="ProvisionalAlternateEncoding"/>
        /// property.  Setting the encoding explicitly when creating zip archives will result in
        /// non-compliant zip files that, curiously, are fairly interoperable.  The challenge is,
        /// the PKWare specification does not provide for a way to specify that an entry in a zip
        /// archive uses a code page that is neither IBM437 nor UTF-8.  Therefore if you set the
        /// encoding explicitly when creating a zip archive, you must take care upon reading the
        /// zip archive to use the same code page.  If you get it wrong, the behavior is undefined
        /// and may result in incorrect filenames, exceptions, stomach upset, hair loss, and acne.
        /// </para>
        /// </remarks>
        /// <seealso cref="ProvisionalAlternateEncoding">ProvisionalAlternateEncoding</seealso>
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
        /// Designed many years ago, the original zip specification from PKWARE allowed for 32-bit
        /// quantities for the compressed and uncompressed sizes of zip entries, as well as a
        /// 32-bit quantity for specifying the length of the zip archive itself, and a maximum of
        /// 65535 entries.  These limits are now regularly exceeded in many backup and archival
        /// scenarios.  Recently, PKWare added extensions to the original zip spec, called "ZIP64
        /// extensions", to raise those limitations.  This property governs whether the ZipFile
        /// instance will use those extensions when writing zip archives within a call to one of
        /// the Save() methods.  The use of these extensions is optional and explicit in DotNetZip
        /// because, despite the status of ZIP64 as a bona fide standard, not all other zip tools
        /// support ZIP64.
        /// </para>
        /// 
        /// <para>
        /// Set this property to <see cref="Zip64Option.Always"/> to always use ZIP64 extensions
        /// when saving, regardless of whether your zip archive needs it.  Suppose you add 5 files,
        /// each under 100k, to a ZipFile.  If you specify Always for this flag before calling the
        /// Save() method, you will get a ZIP64 archive, though you do not need to use ZIP64
        /// because none of the original zip limits had been exceeded.
        /// </para>
        ///
        /// <para>
        /// Set this property to <see cref="Zip64Option.Never"/> to tell the DotNetZip library to
        /// never use ZIP64 extensions.  This is useful for maximum compatibility and
        /// interoperability, at the expense of the capability of handling large files or large
        /// archives.  NB: Windows Explorer in Windows XP and Windows Vista cannot currently
        /// extract files from a zip64 archive, so if you want to guarantee that a zip archive
        /// produced by this library will work in Windows Explorer, use <c>Never</c>. If you set
        /// this property to <see cref="Zip64Option.Never"/>, and your application creates a zip
        /// that would exceed one of the ZIP limits, the library will throw an exception during the
        /// Save().
        /// </para>
        ///
        /// <para>
        /// Set this property to <see cref="Zip64Option.AsNecessary"/> to tell the DotNetZip
        /// library to use the zip64 extensions when required by the entry. After the file is
        /// compressed, the original and compressed sizes are checked, and if they exceed the
        /// limits described above, then zip64 can be used. That is the general idea, but there is
        /// an additional wrinkle when saving to a non-seekable device, like the ASP.NET
        /// <c>Response.OutputStream</c>, or <c>Console.Out</c>.  When using non-seekable streams
        /// for output, the entry header - which indicates whether zip64 is in use - is emitted
        /// before it is known if zip64 is necessary.  It is only after all entries have been saved
        /// that it can be known if ZIP64 will be required.  On seekable output streams, after
        /// saving all entries, the library can seek backward and re-emit the zip file header to be
        /// consistent with the actual ZIP64 requirement.  But using a non-seekable output stream,
        /// the library cannot seek backward, so the header can never be changed. In other words,
        /// the archive's use of ZIP64 extensions is not alterable after the header is emitted.
        /// Therefore, when saving to non-seekable streams, using <see
        /// cref="Zip64Option.AsNecessary"/> is the same as using <see cref="Zip64Option.Always"/>:
        /// it will always produce a zip archive that uses zip64 extensions.
        /// </para>
        ///
        /// <para>
        /// The default value for the property is <see cref="Zip64Option.Never"/>. <see
        /// cref="Zip64Option.AsNecessary"/> is safest, in the sense that you will not get an
        /// Exception if a pre-ZIP64 limit is exceeded.
        /// </para>
        ///
        /// <para>
        /// You may set the property at any time before calling Save(). 
        /// </para>
        ///
        /// <para>
        /// The ZipFile.Read() method will properly read ZIP64-endowed zip archives, regardless of
        /// the value of this property.  ZIP64 archives can always be read, but this property
        /// governs whether they can be written.  Therefore, when updating archives, be careful
        /// about setting this property after reading an archive that may use ZIP64 extensions.
        /// </para>
        ///
        /// <para>
        /// An interesting question is, if you have set this property to <c>AsNecessary</c>, and
        /// then successfully saved, does the resulting archive use ZIP64 extensions or not?  To
        /// learn this, check the <see cref="OutputUsedZip64"/> property, after calling Save().
        /// </para>
        ///
        /// <para>
        /// Have you thought about donating? http://cheeso.members.winisp.net/DotNetZipDonate.aspx
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
        /// <para>
        /// This property is null (or Nothing in VB) if the archive has not been saved, and there are 
        /// fewer than 65334 ZipEntry items contained in the archive. 
        /// </para>
        /// <para>
        /// The Value is true if any of the following four conditions holds: the uncompressed size
        /// of any entry is larger than 0xFFFFFFFF; the compressed size of any entry is larger than
        /// 0xFFFFFFFF; the relative offset of any entry within the zip archive is larger than
        /// 0xFFFFFFFF; or there are more than 65534 entries in the archive.  (0xFFFFFFFF =
        /// 4,294,967,295).  The result may not be known until a Save() is attempted on the zip
        /// archive.  The Value of this Nullable property may be set only AFTER one of the Save()
        /// methods has been called.
        /// </para>
        /// <para>
        /// If none of the four conditions holds, and the archive has been saved, then the Value is false.
        /// </para>
        /// <para>
        /// A Value of false does not indicate that the zip archive, as saved, does not use ZIP64.
        /// It merely indicates that ZIP64 is not required.  An archive may use ZIP64 even when not
        /// required if the <see cref="ZipFile.UseZip64WhenSaving"/> property is set to <see
        /// cref="Zip64Option.Always"/>, or if the <see cref="ZipFile.UseZip64WhenSaving"/>
        /// property is set to <see cref="Zip64Option.AsNecessary"/> and the output stream was not
        /// seekable. Use the <see cref="OutputUsedZip64"/> property to determine if the most
        /// recent Save() method resulted in an archive that utilized the ZIP64 extensions.
        /// </para>
        /// </remarks>
        /// <seealso cref="UseZip64WhenSaving"/>
        /// <seealso cref="OutputUsedZip64"/>
        public Nullable<bool> RequiresZip64
        {
            get
            {
                if (_entries.Count > 65534)
                    return new Nullable<bool>(true);

                // If the ZipFile has not been saved or if the contents have changed, then
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
        /// Describes whether the most recent Save() operation used ZIP64 extensions.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The value is null (or Nothing in VB) if the archive has not been saved.
        /// </para>
        ///
        /// <para>
        /// Non-null values (HasValue is true) indicate whether ZIP64 extensions were used during
        /// the most recent Save() operation.  The ZIP64 extensions may have been used as required
        /// by any particular entry because of its uncompressed or compressed size, or because the
        /// archive is larger than 4294967295 bytes, or because there are more than 65534 entries
        /// in the archive, or because the <c>UseZip64WhenSaving</c> property was set to <see
        /// cref="Zip64Option.Always"/>, or because the <c>UseZip64WhenSaving</c> property was set
        /// to <see cref="Zip64Option.AsNecessary"/> and the output stream was not seekable.  The
        /// value of this property does not indicate the reason the ZIP64 extensions were used.
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
        /// The text encoding to use when writing new entries to the ZipFile, for those
        /// entries that cannot be encoded with the default (IBM437) encoding; or, the
        /// text encoding that was used when reading the entries from the ZipFile.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// In its AppNote.txt document, PKWare describes how to specify in the zip entry
        /// header that a filename or comment containing non-ANSI characters is encoded with
        /// UTF-8.  But, some archivers do not follow the specification, and instead encode
        /// super-ANSI characters using the system default code page.  For example, WinRAR
        /// when run on a machine in Shanghai may encode filenames with the Big-5 Chinese
        /// (950) code page.  This behavior is contrary to the Zip specification, but it
        /// occurs anyway.
        /// </para>
        ///
        /// <para>
        /// When using DotNetZip to write zip archives that will be read by one of these other
        /// archivers, set this property to specify the code page to use when encoding the <see
        /// cref="ZipEntry.FileName"/> and <see cref="ZipEntry.Comment"/> for each ZipEntry in the
        /// zip file, for values that cannot be encoded with the default codepage for zip files,
        /// IBM437.  This is why this property is "provisional".  In all cases, IBM437 is used
        /// where possible, in other words, where no loss of data would result. It is possible,
        /// therefore, to have a given entry with a Comment encoded in IBM437 and a FileName
        /// encoded with the specified "provisional" codepage.
        /// </para>
        ///
        /// <para>
        /// Be aware that a zip file created after you've explicitly set the <see
        /// cref="ProvisionalAlternateEncoding" /> property to a value other than IBM437 may not be
        /// compliant to the PKWare specification, and may not be readable by compliant archivers.
        /// On the other hand, many (most?) archivers are non-compliant and can read zip files
        /// created in arbitrary code pages.  The trick is to use or specify the proper codepage
        /// when reading the zip.
        /// </para>
        ///
        /// <para>
        /// When creating a zip archive using this library, it is possible to change the value of
        /// <see cref="ProvisionalAlternateEncoding" /> between each entry you add, and between
        /// adding entries and the call to Save(). Don't do this. It will likely result in a
        /// zipfile that is not readable.  For best interoperability, either leave <see
        /// cref="ProvisionalAlternateEncoding" /> alone, or specify it only once, before adding
        /// any entries to the ZipFile instance.  There is one exception to this recommendation,
        /// described later.
        /// </para>
        ///
        /// <para>
        /// When using an arbitrary, non-UTF8 code page for encoding, there is no standard way for
        /// the creator application - whether DotNetZip, WinZip, WinRar, or something else - to
        /// formally specify in the zip file which codepage has been used for the entries. As a
        /// result, readers of zip files are not able to inspect the zip file and determine the
        /// codepage that was used for the entries contained within it.  It is left to the
        /// application or user to determine the necessary codepage when reading zipfiles encoded
        /// this way.  If you use an incorrect codepage when reading a zipfile, you will get
        /// entries with filenames that are incorrect, and the incorrect filenames may even contain
        /// characters that are not legal for use within filenames in Windows. Extracting entries
        /// with illegal characters in the filenames will lead to exceptions. It's too bad, but
        /// this is just the way things are with code pages in zip files. Caveat Emptor.
        /// </para>
        ///
        /// <para>
        /// When using DotNetZip to read a zip archive, and the zip archive uses an arbitrary code
        /// page, you must specify the encoding to use before or when the zipfile is READ.  This
        /// means you must use a ZipFile.Read() method that allows you to specify a
        /// System.Text.Encoding parameter.  Setting the ProvisionalAlternateEncoding property
        /// after your application has read in the zip archive will not affect the entry names of
        /// entries that have already been read in, and is probably not what you want.
        /// </para>
        ///	
        /// <para>
        /// And now, the exception to the rule described above.  One strategy for specifying the
        /// code page for a given zip file is to describe the code page in a human-readable form in
        /// the Zip comment. For example, the comment may read "Entries in this archive are encoded
        /// in the Big5 code page".  For maximum interoperability, the Zip comment in this case
        /// should be encoded in the default, IBM437 code page.  In this case, the zip comment is
        /// encoded using a different page than the filenames.  To do this, specify
        /// ProvisionalAlternateEncoding to your desired region-specific code page, once before
        /// adding any entries, and then reset ProvisionalAlternateEncoding to IBM437 before
        /// setting the <see cref="Comment"/> property and calling Save().
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// This example shows how to read a zip file using the Big-5 Chinese code page (950), 
        /// and extract each entry in the zip file.
        /// For this code to work as desired, the zipfile must have been created using the big5
        /// code page (CP950). This is typical, for example, when using WinRar on a machine with
        /// CP950 set as the default code page.  In that case, the names of entries within the Zip
        /// archive will be stored in that code page, and reading the zip archive must be done
        /// using that code page.  If the application did not use the correct code page in
        /// ZipFile.Read(), then names of entries within the zip archive would not be correctly
        /// retrieved.
        /// <code>
        /// using (var zip = ZipFile.Read(zipFileName, System.Text.Encoding.GetEncoding("big5")))
        /// {
        ///     // retrieve an extract an entry using a name encoded with CP950
        ///     zip[MyDesiredEntry].Extract("unpack");
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As ZipFile = ZipFile.Read(ZipToExtract, System.Text.Encoding.GetEncoding(950))
        ///     ' retrieve an extract an entry using a name encoded with CP950
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
        /// operations.  Typically, console applications might use <c>Console.Out</c> and
        /// graphical or headless applications might use a <c>System.IO.StringWriter</c>. The
        /// output of this is suitable for viewing by humans.
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
        public System.IO.TextWriter StatusMessageTextWriter
        {
            get { return _StatusMessageTextWriter; }
            set { _StatusMessageTextWriter = value; }
        }

        /// <summary>
        /// Gets or sets the flag that indicates whether the ZipFile should use
        /// compression for subsequently added entries in the ZipFile instance.
        /// </summary>
        ///
        /// <remarks>
        /// <para> 
        /// When saving an entry into a zip archive, the DotNetZip by default compresses the
        /// file. That's what a ZIP archive is all about, isn't it?  For files that are already
        /// compressed, like MP3's or JPGs, the deflate algorithm can actually slightly expand the
        /// size of the data.  Setting this property to trye allows you to specify that
        /// compression should not be used.  The default value is false.
        /// </para> 
        ///
        /// <para>
        /// Do not construe setting this flag to false as "Force Compression".  Setting it to
        /// false merely does NOT force No compression.  If you want to force the use of the
        /// deflate algorithm when storing each entry into the zip archive, define a <see
        /// cref="WillReadTwiceOnInflation"/> callback, which always returns false, and a <see
        /// cref="WantCompression" /> callback that always returns true.  This is probably the
        /// wrong thing to do, but you could do it. Forcing the use of the Deflate algorithm when
        /// storing an entry does not guarantee that the data size will get smaller. It could
        /// increase, as described above.
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
        /// This example shows how to specify that Compression will not be used when adding files 
        /// to the zip archive. None of the files added to the archive in this example will use
        /// compression.
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
        /// This library will create a temporary file when saving a Zip archive.
        /// By default, the library uses the value of <see cref="System.IO.Path.GetTempPath"/>
        /// as the location in which to store temporary files. For some scenarios, such as ASP.NET
        /// applications, the application may wish to explicitly override this default behavior.
        /// </para>
        /// <para>
        /// The temporary file is written when calling one of the <c>Save()</c> methods that does
        /// not save to a stream, or one of the <c>SaveSelfExtractor()</c> methods.  
        /// </para>
        ///
        /// <para>
        /// The folder specified must exist; if it does not an exception is thrown.  The
        /// application should have write and delete permissions on the folder.  The permissions
        /// are not explicitly checked ahead of time; if the application does not have the
        /// appropriate rights, an exception will be thrown at the time <c>Save()</c> is called.
        /// </para>
        ///
        /// <para>
        /// There is no temporary file created when reading a zip archive.  When saving to a
        /// Stream, there is no temporary file created.  For example, if the application is an
        /// ASP.NET application and calls <c>Save()</c> specifying the
        /// <c>Response.OutputStream</c> as the output stream, there is no temporary file created.
        /// </para>
        /// </remarks>
        ///
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown when setting the property if the directory does not exist. 
        /// </exception>
        ///
        public String TempFileFolder
        {
            get
            {
                // first time through only (default value)
                if (_TempFileFolder == null)
                {
                    // Do not use Environment.GetEnvironmentVariable()
                    // Use System.IO.Path.GetTempPath for compat with .NET CF
                    _TempFileFolder = System.IO.Path.GetTempPath();
                    if (_TempFileFolder == null)
                        _TempFileFolder = ".";
                }
                return _TempFileFolder;
            }

            set
            {
                if (value == null)
                    throw new ArgumentException("You may not set the TempFileFolder to a null value.");

                if (!System.IO.Directory.Exists(value))
                    throw new System.IO.FileNotFoundException(String.Format("That directory ({0}) does not exist.", value));

                _TempFileFolder = value;
            }
        }

        /// <summary>
        /// Sets the password to be used on the ZipFile instance.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// When writing a zip archive, this password is applied to the entries, not
        /// to the zip archive itself. It applies to any ZipEntry subsequently added 
        /// to the ZipFile, using one of the AddFile, AddDirectory, or AddItem methods. 
        /// When reading a zip archive, this property applies to any entry subsequently 
        /// extracted from the ZipFile using one of the Extract methods on the ZipFile class.  
        /// </para>
        /// 
        /// <para>
        /// When writing a zip archive, keep this in mind: though the password is set on the
        /// ZipFile object, according to the Zip spec, the "directory" of the archive - in
        /// other words the list of entries contained in the archive - is not encrypted with
        /// the password, or protected in any way.  IF you set the Password property, the
        /// password actually applies to individual entries that are added to the archive,
        /// subsequent to the setting of this property.  The list of filenames in the
        /// archive that is eventually created will appear in clear text, but the contents
        /// of the individual files are encrypted.  This is how Zip encryption works.
        /// </para>
        /// 
        /// <para>
        /// If you set the password on the zip archive, and then add a set of files to the 
        /// archive, then each entry is encrypted with that password.  You may also want 
        /// to change the password between adding different entries. If you set the 
        /// password, add an entry, then set the password to null, and add another entry,
        /// the first entry is encrypted and the second is not.  Furshtay?
        /// </para>
        /// 
        /// <para>
        /// When setting the Password, you may also want to explicitly set the <see
        /// cref="Encryption"/> property, to specify how to encrypt the entries added to the
        /// ZipFile.  If you set the Password to a non-null value and do not set <see
        /// cref="Encryption"/>, then PKZip 2.0 ("Weak") encryption is used.  This
        /// encryption is relatively weak but is very interoperable. If you set the password
        /// to a null value (<c>Nothing</c> in VB), Encryption is reset to None.
        /// </para>
        ///
        /// <para>
        /// All of the preceding applies to writing zip archives - when you use one of the
        /// Save methods.  To use this property when reading or an existing ZipFile -
        /// calling Extract methods on the ZipFile or the ZipEntry instances - do the
        /// following: set the Password property, then call one of the ZipFile.Extract()
        /// overloads. In this case, the entry is extracted using the Password that is
        /// specified on the ZipFile instance. If you have not set the Password property,
        /// then the password is null, and the entry is extracted with no password.
        /// </para>
        ///
        /// <para>
        /// If you set Password on the ZipFile, then Extract() an entry that has not been 
        /// encrypted with a password, then the password is not used for that entry, and
        /// the ZipEntry is extracted as normal.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <example>
        /// <para>
        /// In this example, three files are added to a Zip archive. The ReadMe.txt file
        /// will be placed in the root of the archive, and will not be encrypted. 
        /// The .png file will be included into the zip, encrypted with the "123456!" password.
        /// The pdf file will be included, encrypted with "!Secret1" as the password.
        /// </para>
        /// <code>
        ///    try
        ///    {
        ///      using (ZipFile zip = new ZipFile())
        ///      {
        ///        zip.AddFile("ReadMe.txt");
        ///        zip.Password= "123456!";
        ///        zip.AddFile("MapToTheSite-7440-N49th.png");
        ///        zip.Password= "!Secret1";
        ///        zip.AddFile("2008-Regional-Sales-Report.pdf");
        ///
        ///        zip.Save("EncryptedArchive.zip");
        ///      }
        ///    }
        ///    catch (System.Exception ex1)
        ///    {
        ///      System.Console.Error.WriteLine("exception: {0}", ex1);
        ///    }
        /// </code>
        ///
        /// <code lang="VB">
        ///  Try 
        ///    Using zip As New ZipFile
        ///      zip.AddFile("ReadMe.txt")
        ///      zip.Password = "123456!"
        ///      zip.AddFile("MapToTheSite-7440-N49th.png")
        ///      zip.Password= "!Secret1";
        ///      zip.AddFile("2008-Regional-Sales-Report.pdf")
        ///      zip.Save("EncryptedArchive.zip")
        ///    End Using
        ///  Catch ex1 As System.Exception
        ///    System.Console.Error.WriteLine("exception: {0}", ex1)
        ///  End Try
        /// </code>
        ///
        /// </example>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.Encryption"/>
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
        /// This property affects the behavior of the Extract methods (one of the <c>Extract()</c>
        /// or <c>ExtractWithPassword()</c> overloads), when extraction would would overwrite an
        /// existing filesystem file. If you do not set this property, 
        /// the library throws an exception when extracting
        /// an entry would overwrite an existing file.  
        /// </para>
        /// <para>
        /// This property has no effect when extracting to a stream, or when the file to be extracted does not already exist. 
        /// </para>
        /// </remarks>
        /// <seealso cref="Ionic.Zip.ZipEntry.ExtractExistingFile"/>
        public ExtractExistingFileAction ExtractExistingFile
        {
            get;
            set;
        }


        /// <summary>
        /// The Encryption to use for entries added to the ZipFile.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The specified Encryption is applied to the entries subsequently added to the ZipFile instance.  
        /// </para>
        /// 
        /// <para>
        /// If you set this to something other than EncryptionAlgorithm.None, you will also need to set 
        /// the <see cref="Password"/>.
        /// </para>
        ///
        /// <para>
        /// As with other properties (like <see cref="Password"/> and <see
        /// cref="ForceNoCompression"/>), setting this property a ZipFile instance will cause that
        /// EncryptionAlgorithm to be used on all ZipEntry items that are subsequently added to
        /// the ZipFile instance. In other words, if you set this property after you have added
        /// items to the ZipFile, but before you have called Save(), those items will not be
        /// encrypted or protected with a password in the resulting zip archive. To get a zip
        /// archive with encrypted entries, set this property, along with the <see
        /// cref="Password"/> property, before calling <c>AddFile</c>, <c>AddItem</c>, or
        /// <c>AddDirectory</c> (etc) on the ZipFile instance.
        /// </para>
        /// </remarks>
        ///
        /// <example>
        /// <code>
        ///    try
        ///    {
        ///      using (ZipFile zip = new ZipFile())
        ///      {
        ///        zip.Encryption= EncryptionAlgorithm.WinZipAes256;
        ///        zip.Password= "Top.Secret.No.Peeking!";
        ///        zip.AddFile("ReadMe.txt");
        ///        zip.AddFile("7440-N49th.png");
        ///        zip.AddFile("2008-Regional-Sales-Report.pdf");
        ///        zip.Save("EncryptedArchive.zip");
        ///      }
        ///    }
        ///    catch (System.Exception ex1)
        ///    {
        ///      System.Console.Error.WriteLine("exception: {0}", ex1);
        ///    }
        /// </code>
        ///
        /// <code lang="VB">
        ///  Try 
        ///    Using zip As New ZipFile()
        ///      zip.Encryption= EncryptionAlgorithm.WinZipAes256
        ///      zip.Password= "Top.Secret.No.Peeking!"
        ///      zip.AddFile("ReadMe.txt")
        ///      zip.AddFile("7440-N49th.png")
        ///      zip.AddFile("2008-Regional-Sales-Report.pdf")
        ///      zip.Save("EncryptedArchive.zip")
        ///    End Using
        ///  Catch ex1 As System.Exception
        ///    System.Console.Error.WriteLine("exception: {0}", ex1)
        ///  End Try
        /// </code>
        ///
        /// </example>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.Password"/>
        public EncryptionAlgorithm Encryption
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
        /// In some cases, applying the Deflate compression algorithm in <c>DeflateStream</c> can
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
        /// the application to for approval for re-reading the stream, in the case where
        /// inflation occurs.  The callback is invoked only in the case of inflation; that
        /// is to say when the uncompressed stream is smaller than the compressed stream.
        /// </para>
        ///
        /// <para>
        /// As with other properties (like <see cref="Password"/> and <see
        /// cref="ForceNoCompression"/>), setting the corresponding delegate on a ZipFile instance
        /// will caused it to be applied to all ZipEntry items that are subsequently added to the
        /// ZipFile instance. In other words, if you set this callback after you have added files
        /// to the ZipFile, but before you have called Save(), those items will not be governed by
        /// the callback when you do call Save(). Your best bet is to set this callback before
        /// adding any entries.
        /// </para>
        ///
        /// <para>
        /// Of course, if you want to have different callbacks for different entries, you may do so. 
        /// </para>
        ///
        /// </remarks>
        /// <example>
        /// <para>
        /// In this example, the application callback checks to see if the difference
        /// between the compressed and uncompressed data is greater than 25%.  If it is,
        /// then the callback returns true, and the application tells the library to re-read
        /// the stream.  If not, then the callback returns false, and the library just keeps
        /// the "inflated" file data.
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
        /// an entry.  If it is to be attempted, the library reads the entry, runs it through
        /// the deflate algorithm, and then checks the size of the result.  If applying
        /// the Deflate algorithm increases the size of the data, then the library
        /// discards the compressed bytes, re-reads the raw entry data, and stores the
        /// uncompressed file data into the zip archive, in compliance with the zip
        /// spec.  This is an optimization where smaller size is preferred over longer
        /// run times. The re-reading is gated on the <see
        /// cref="WillReadTwiceOnInflation"/> callback, if it is set. This callback applies
        /// independently of the WantCompression callback.
        /// </para>
        ///
        /// <para>
        /// If by the logic described above, compression is not to be attempted for an entry, 
        /// the library reads the entry, and simply stores the entry data uncompressed. 
        /// </para>
        ///
        /// <para>
        /// And, if you have read this far, I would like to point out that a single
        /// person wrote all the code that does what is described above, and also wrote
        /// the description.  Isn't it about time you donated $5 in appreciation?  The
        /// money goes to a charity. See
        /// http://cheeso.members.winisp.net/DotNetZipDonate.aspx.
        /// </para>
        ///
        /// </remarks>
        /// <seealso cref="Ionic.Zip.ZipFile.WillReadTwiceOnInflation"/>
        public WantCompressionCallback WantCompression
        {
            get;
            set;
        }



        internal void NotifyEntryChanged()
        {
            _contentsChanged = true;
        }



        internal System.IO.Stream ReadStream
        {
            get
            {
                if (_readstream == null)

                    if (_name != null)
                    {
                        try
                        {
                            _readstream = System.IO.File.OpenRead(_name);
                            _ReadStreamIsOurs = true;
                        }
                        catch (System.IO.IOException ioe)
                        {
                            throw new ZipException("Error opening the file", ioe);
                        }
                    }

                return _readstream;
            }
        }



        // called by ZipEntry in ZipEntry.Extract(), when there is no stream set for the ZipEntry.
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
        /// Creates a new ZipFile instance, using the specified filename. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Applications can use this constructor to create a new ZipFile for writing, 
        /// or to slurp in an existing zip archive for read and write purposes. 
        /// </para>
        /// 
        /// <para>
        /// To create a new zip archive, an application can call this constructor, passing the
        /// name of a file that does not exist.  The name may be a fully qualified path. Then the
        /// application can add directories or files to the ZipFile via <c>AddDirectory()</c>,
        /// <c>AddFile()</c>, <c>AddItem()</c> and then write the zip archive to the disk by
        /// calling <c>Save()</c>. The zip file is not actually opened and written to the disk
        /// until the application calls <c>ZipFile.Save()</c>.  At that point the new zip file
        /// with the given name is created.
        /// </para>
        /// 
        /// <para>
        /// If you won't know the name of the zipfile until the time you call
        /// <c>ZipFile.Save()</c>, or if you plan to save to a stream (which has no name), then
        /// you should use the no-argument constructor.
        /// </para>
        /// 
        /// <para>
        /// The application can also call this constructor to read an existing zip archive.
        /// passing the name of a valid zip file that does exist. But, it's better form to use the
        /// static <see cref="ZipFile.Read(String)"/> method, passing the name of the zip file,
        /// because using <c>ZipFile.Read()</c> in your code communicates very clearly what you
        /// are doing.  In either case, the file is then read into the <c>ZipFile</c> instance.
        /// The app can then enumerate the entries or can modify the zip file, for example adding
        /// entries, removing entries, changing comments, and so on.  
        /// </para>
        /// 
        /// <para>
        /// One advantage to this parameterized constructor: it allows applications to use the
        /// same code to add items to a zip archive, regardless of whether the zip file exists.
        /// </para>
        /// 
        /// <para>
        /// Instances of the ZipFile class are not multi-thread safe.  You may not party on a
        /// single instance with multiple threads.  You may have multiple threads that each use a
        /// distinct ZipFile instance, or you can synchronize multi-thread access to a single
        /// instance.
        /// </para>
        /// 
        /// <para>
        /// By the way, if you are using multiple threads with DotNetZip, you really should
        /// donate $5 or $10 to the cause.  See http://cheeso.members.winisp.net/DotNetZipDonate.aspx.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <exception cref="Ionic.Zip.ZipException">
        /// Thrown if zipFileName refers to an existing file that is not a valid zip file. 
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
        ///   foreach (String filename in filenames)
        ///   {
        ///     Console.WriteLine("Adding {0}...", filename);
        ///     zip.AddFile(filename);
        ///   }  
        ///   zip.Save(ZipFileToCreate);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim ZipFileToCreate As String = "archive1.zip"
        /// Dim DirectoryToZip As String = "c:\reports"
        /// Using zip As ZipFile = New ZipFile()
        ///     Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        ///     Dim filename As String
        ///     For Each filename In filenames
        ///         Console.WriteLine("Adding {0}...", filename)
        ///         zip.AddFile(filename)
        ///     Next
        ///     zip.Save(ZipFileToCreate)
        /// End Using
        /// </code>
        /// </example>
        ///
        /// <param name="zipFileName">The filename to use for the new zip archive.</param>
        ///
        public ZipFile(string zipFileName)
        {
            try
            {
                _InitInstance(zipFileName, null);
            }
            catch (Exception e1)
            {
                throw new ZipException(String.Format("{0} is not a valid zip file", zipFileName), e1);
            }
        }


        /// <summary>
        /// Creates a new ZipFile instance, using the specified ZipFileName for the filename, and 
        /// the specified Encoding.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the documentation on the <see cref="ZipFile(String)">ZipFile constructor that
        /// accepts a single string argument</see> for basic information on all the ZipFile
        /// constructors.
        /// </para>
        ///
        /// <para>
        /// The Encoding is used as the default alternate encoding for entries with filenames or
        /// comments that cannot be encoded with the IBM437 code page.  This is a equivalent to
        /// setting the <see cref="ProvisionalAlternateEncoding"/> property on the ZIpFile
        /// instance after construction.
        /// </para>
        ///
        /// <para>
        /// Instances of the ZipFile class are not multi-thread safe.  You may not party on a
        /// single instance with multiple threads.  You may have multiple threads that each use a
        /// distinct ZipFile instance, or you can synchronize multi-thread access to a single
        /// instance.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <exception cref="Ionic.Zip.ZipException">
        /// Thrown if zipFileName refers to an existing file that is not a valid zip file. 
        /// </exception>
        ///
        /// <param name="zipFileName">The filename to use for the new zip archive.</param>
        /// <param name="encoding">The Encoding is used as the default alternate 
        /// encoding for entries with filenames or comments that cannot be encoded 
        /// with the IBM437 code page. </param>
        public ZipFile(string zipFileName, System.Text.Encoding encoding)
        {
            try
            {
                _InitInstance(zipFileName, null);
                ProvisionalAlternateEncoding = encoding;
            }
            catch (Exception e1)
            {
                throw new ZipException(String.Format("{0} is not a valid zip file", zipFileName), e1);
            }
        }



        /// <summary>
        /// Create a zip file, without specifying a target filename or stream to save to. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the documentation on the <see cref="ZipFile(String)">ZipFile constructor that
        /// accepts a single string argument</see> for basic information on all the ZipFile
        /// constructors.
        /// </para>
        ///
        /// <para>
        /// After instantiating with this constructor and adding entries to the archive, your
        /// application should call <see cref="ZipFile.Save(String)"/> or <see
        /// cref="ZipFile.Save(System.IO.Stream)"/> to save to a file or a stream, respectively.
        /// If you call the no-argument <see cref="Save()"/> method, the Save() will throw, as
        /// there is no known place to save the file.
        /// </para>
        ///
        /// <para>
        /// Instances of the ZipFile class are not multi-thread safe.  You may not party on a
        /// single instance with multiple threads.  You may have multiple threads that each use a
        /// distinct ZipFile instance, or you can synchronize multi-thread access to a single
        /// instance.
        /// </para>
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
        ///   foreach (String filename in filenames)
        ///   {
        ///     Console.WriteLine("Adding {0}...", filename);
        ///     zip.AddFile(filename);
        ///   }  
        ///   zip.Save("Backup.zip");
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As New ZipFile
        ///     ' Store all files found in the top level directory, into the zip archive.
        ///     ' note: this code does not recurse subdirectories!
        ///     Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        ///     Dim filename As String
        ///     For Each filename In filenames
        ///         Console.WriteLine("Adding {0}...", filename)
        ///         zip.AddFile(filename)
        ///     Next
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
        /// See the documentation on the <see cref="ZipFile(String)">ZipFile constructor that
        /// accepts a single string argument</see> for basic information on all the ZipFile
        /// constructors.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <param name="encoding">
        /// The Encoding is used as the default alternate encoding for entries with filenames or
        /// comments that cannot be encoded with the IBM437 code page.
        /// </param>
        public ZipFile(System.Text.Encoding encoding)
        {
            _InitInstance(null, null);
            ProvisionalAlternateEncoding = encoding;
        }


        /// <summary>
        /// Creates a new ZipFile instance, using the specified ZipFileName for the filename, 
        /// and the specified status message writer. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// See the documentation on the <see cref="ZipFile(String)">ZipFile constructor that
        /// accepts a single string argument</see> for basic information on all the ZipFile
        /// constructors.
        /// </para>
        ///
        /// <para>
        /// This version of the constructor allows the caller to pass in a TextWriter, to which
        /// verbose messages will be written during extraction or creation of the zip archive.
        /// A console application may wish to pass System.Console.Out to get messages on the
        /// Console. A graphical or headless application may wish to capture the messages in a
        /// different <c>TextWriter</c>, for example, a <c>StringWriter</c>, and then display
        /// the messages in a TextBox, or generate an audit log of ZipFile operations.
        /// </para>
        /// 
        /// <para>
        /// To encrypt the data for the  files added to the ZipFile instance, set the Password
        /// property after creating the ZipFile instance.
        /// </para>
        /// 
        /// <para>
        /// Instances of the ZipFile class are not multi-thread safe.  You may not party on a
        /// single instance with multiple threads.  You may have multiple threads that each use a
        /// distinct ZipFile instance, or you can synchronize multi-thread access to a single
        /// instance.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <exception cref="Ionic.Zip.ZipException">
        /// Thrown if zipFileName refers to an existing file that is not a valid zip file. 
        /// </exception>
        ///
        /// <example>
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// { 
        ///   // Store all files found in the top level directory, into the zip archive.
        ///   // note: this code does not recurse subdirectories!
        ///   String[] filenames = System.IO.Directory.GetFiles(DirectoryToZip);
        ///   foreach (String filename in filenames)
        ///   {
        ///     Console.WriteLine("Adding {0}...", filename);
        ///     zip.AddFile(filename);
        ///   }  
        ///   zip.Save("Backup.zip");
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As New ZipFile
        ///     ' Store all files found in the top level directory, into the zip archive.
        ///     ' note: this code does not recurse subdirectories!
        ///     Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        ///     Dim filename As String
        ///     For Each filename In filenames
        ///         Console.WriteLine("Adding {0}...", filename)
        ///         zip.AddFile(filename)
        ///     Next
        ///     zip.Save("Backup.zip")
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="zipFileName">The filename to use for the new zip archive.</param>
        /// <param name="statusMessageWriter">A TextWriter to use for writing 
        /// verbose status messages.</param>
        public ZipFile(string zipFileName, System.IO.TextWriter statusMessageWriter)
        {
            try
            {
                _InitInstance(zipFileName, statusMessageWriter);
            }
            catch (Exception e1)
            {
                throw new ZipException(String.Format("{0} is not a valid zip file", zipFileName), e1);
            }
        }


        /// <summary>
        /// Creates a new ZipFile instance, using the specified ZipFileName for the filename, 
        /// the specified status message writer, and the specified Encoding.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This constructor works like the <see cref="ZipFile(String)">ZipFile constructor
        /// that accepts a single string argument.</see> See that reference for detail on what
        /// this constructor does.
        /// </para>
        ///
        /// <para>
        /// This version of the constructor allows the caller to pass in a TextWriter, and an
        /// Encoding.  The TextWriter will collect verbose messages that are generated by the
        /// library during extraction or creation of the zip archive.  A console application
        /// may wish to pass System.Console.Out to get messages on the Console. A graphical or
        /// headless application may wish to capture the messages in a different
        /// <c>TextWriter</c>, for example, a <c>StringWriter</c>, and then display the
        /// messages in a TextBox, or generate an audit log of ZipFile operations.
        /// </para>
        /// 
        /// <para>
        /// The Encoding is used as the default alternate encoding for entries with filenames or
        /// comments that cannot be encoded with the IBM437 code page.  This is a equivalent to
        /// setting the <see cref="ProvisionalAlternateEncoding"/> property on the ZipFile
        /// instance after construction.
        /// </para>
        /// 
        /// <para>
        /// To encrypt the data for the  files added to the ZipFile instance, set the Password
        /// property after creating the ZipFile instance.
        /// </para>
        /// 
        /// <para>
        /// Instances of the ZipFile class are not multi-thread safe.  You may not party on a single
        /// instance with multiple threads.  You may have multiple threads that each use a distinct ZipFile 
        /// instance, or you can synchronize multi-thread access to a single instance.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <exception cref="Ionic.Zip.ZipException">
        /// Thrown if zipFileName refers to an existing file that is not a valid zip file. 
        /// </exception>
        ///
        /// <param name="zipFileName">The filename to use for the new zip archive.</param>
        /// <param name="statusMessageWriter">A TextWriter to use for writing verbose 
        /// status messages.</param>
        /// <param name="encoding">The Encoding is used as the default alternate encoding 
        /// for entries with filenames or comments that cannot be encoded with the IBM437 code page.
        /// </param>
        public ZipFile(string zipFileName, System.IO.TextWriter statusMessageWriter, System.Text.Encoding encoding)
        {
            try
            {
                _InitInstance(zipFileName, statusMessageWriter);
                ProvisionalAlternateEncoding = encoding;
            }
            catch (Exception e1)
            {
                throw new ZipException(String.Format("{0} is not a valid zip file", zipFileName), e1);
            }
        }



        /// <summary>
        /// Returns the version number on the DotNetZip assembly.
        /// </summary>
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

        private void _InitInstance(string zipFileName, System.IO.TextWriter statusMessageWriter)
        {
            // create a new zipfile
            _name = zipFileName;
            _StatusMessageTextWriter = statusMessageWriter;
            _contentsChanged = true;
            CompressionLevel = Ionic.Zlib.CompressionLevel.DEFAULT;
            BufferSize = 8192;  // Default
            if (System.IO.File.Exists(_name))
            {
                ReadIntoInstance(this);
                this._fileAlreadyExists = true;
            }
            else
                _entries = new System.Collections.Generic.List<ZipEntry>();
            return;
        }
        #endregion

        #region Adding Entries

        /// <summary>
        /// Adds an item, either a file or a directory, to a zip file archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method is handy if you are adding things to zip archive and don't want to bother
        /// distinguishing between directories or files.  Any files are added as single entries.
        /// A directory added through this method is added recursively: all files and
        /// subdirectories contained within the directory are added to the ZipFile.  
        /// </para>
        /// 
        /// <para>
        /// The name of the item may be a relative path or a fully-qualified path. Remember, the
        /// items contained in ZipFile instance get written to the disk only when you call
        /// ZipFile.Save() or a similar save method.
        /// </para>
        ///
        /// <para>
        /// The directory name used for the file within the archive is the same as
        /// the directory name (potentially a relative path) specified in the fileOrDirectoryName.
        /// </para>
        ///
        /// <para>
        /// For ZipFile properties including <see cref="Encryption"/>, <see cref="Password"/>,
        /// <see cref="WantCompression"/>, <see cref="ProvisionalAlternateEncoding"/>, 
        /// <see cref="ExtractExistingFile"/>, and <see
        /// cref="ForceNoCompression"/>, their respective values at the time of this call will be
        /// applied to the ZipEntry added.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.AddFile(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.AddDirectory(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateItem(string)"/>
        ///
        /// <overloads>This method has two overloads.</overloads>
        /// <param name="fileOrDirectoryName">the name of the file or directory to add.</param>
        /// 
        /// <returns>The ZipEntry added.</returns>
        public ZipEntry AddItem(string fileOrDirectoryName)
        {
            return AddItem(fileOrDirectoryName, null);
        }


        /// <summary>
        /// Adds an item, either a file or a directory, to a zip file archive, 
        /// explicitly specifying the directory path to be used in the archive. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// If adding a directory, the add is recursive on all files and subdirectories 
        /// contained within it. 
        /// </para>
        /// <para>
        /// The name of the item may be a relative path or a fully-qualified path.
        /// The item added by this call to the ZipFile is not written to the zip file
        /// archive until the application calls Save() on the ZipFile. 
        /// </para>
        /// 
        /// <para>
        /// This version of the method allows the caller to explicitly specify the 
        /// directory path to be used in the archive, which would override the 
        /// "natural" path of the filesystem file.
        /// </para>
        /// 
        /// <para>
        /// Encryption will be used on the file data if the Password
        /// has been set on the ZipFile object, prior to calling this method.
        /// </para>
        /// 
        /// <para>
        /// For ZipFile properties including <see cref="Encryption"/>, <see cref="Password"/>,
        /// <see cref="WantCompression"/>, <see cref="ProvisionalAlternateEncoding"/>, 
        /// <see cref="ExtractExistingFile"/>, and <see
        /// cref="ForceNoCompression"/>, their respective values at the time of this call will be
        /// applied to the ZipEntry added.
        /// </para>
        ///
        /// </remarks>
        /// 
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown if the file or directory passed in does not exist. 
        /// </exception>
        ///
        /// <param name="fileOrDirectoryName">the name of the file or directory to add.</param>
        /// <param name="directoryPathInArchive">
        /// The name of the directory path to use within the zip archive.  This path need not
        /// refer to an extant directory in the current filesystem.  If the files within the zip
        /// are later extracted, this is the path used for the extracted file.  Passing null
        /// (nothing in VB) will use the path on the FileOrDirectoryName.  Passing the empty
        /// string ("") will insert the item at the root path within the archive.
        /// </param>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.AddFile(string, string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.AddDirectory(string, string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateItem(string, string)"/>
        ///
        /// <example>
        /// This example shows how to zip up a set of files into a flat hierarchy, regardless of
        /// where in the filesystem the files originated. The resulting zip archive will contain a
        /// toplevel directory named "flat", which itself will contain files Readme.txt,
        /// MyProposal.docx, and Image1.jpg.  A subdirectory under "flat" called SupportFiles will
	/// contain all the files in the "c:\SupportFiles" directory on disk.
        /// <code>
        /// String[] itemnames= { 
        ///   "c:\\fixedContent\\Readme.txt",
        ///   "MyProposal.docx",
        ///   "c:\\SupportFiles",  // a directory
        ///   "images\\Image1.jpg"
        /// };
        ///
        /// try
        /// {
        ///   using (ZipFile zip = new ZipFile())
        ///   {
        ///     for (int i = 1; i &lt; itemnames.Length; i++)
        ///     {
        ///       // will add Files or Dirs, recurses and flattens subdirectories
        ///       zip.AddItem(itemnames[i],"flat"); 
        ///     }
        ///     zip.Save(ZipToCreate);
        ///   }
        /// }
        /// catch (System.Exception ex1)
        /// {
        ///   System.Console.Error.WriteLine("exception: {0}", ex1);
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        ///   Dim itemnames As String() = _
        ///     New String() { "c:\fixedContent\Readme.txt", _
        ///                    "MyProposal.docx", _
        ///                    "SupportFiles", _
        ///                    "images\Image1.jpg" }
        ///   Try 
        ///       Using zip As New ZipFile
        ///           Dim i As Integer
        ///           For i = 1 To itemnames.Length - 1
        ///               ' will add Files or Dirs, recursing and flattening subdirectories.
        ///               zip.AddItem(itemnames(i), "flat")
        ///           Next i
        ///           zip.Save(ZipToCreate)
        ///       End Using
        ///   Catch ex1 As Exception
        ///       Console.Error.WriteLine("exception: {0}", ex1.ToString())
        ///   End Try
        /// </code>
        /// </example>
        /// <returns>The ZipEntry added.</returns>
        public ZipEntry AddItem(String fileOrDirectoryName, String directoryPathInArchive)
        {
            if (System.IO.File.Exists(fileOrDirectoryName))
                return AddFile(fileOrDirectoryName, directoryPathInArchive);
            else if (System.IO.Directory.Exists(fileOrDirectoryName))
                return AddDirectory(fileOrDirectoryName, directoryPathInArchive);

            else
                throw new System.IO.FileNotFoundException(String.Format("That file or directory ({0}) does not exist!", fileOrDirectoryName));
        }

        /// <summary>
        /// Adds a File to a Zip file archive. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// The file added by this call to the ZipFile is not written to the zip file
        /// archive until the application calls Save() on the ZipFile. 
        /// </para>
        ///
        /// <para>
        /// This method will throw an Exception if an entry with the same name already exists
        /// in the ZipFile.
        /// </para>
        ///
        /// <para>
        /// For ZipFile properties including <see cref="Encryption"/>, <see cref="Password"/>,
        /// <see cref="WantCompression"/>, <see cref="ProvisionalAlternateEncoding"/>, 
        /// <see cref="ExtractExistingFile"/>, and <see
        /// cref="ForceNoCompression"/>, their respective values at the time of this call will be
        /// applied to the ZipEntry added.
        /// </para>
        ///
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// In this example, three files are added to a Zip archive. The ReadMe.txt file
        /// will be placed in the root of the archive. The .png file will be placed in a folder 
        /// within the zip called photos\personal.  The pdf file will be included into a
        /// folder within the zip called Desktop.
        /// </para>
        /// <code>
        ///    try
        ///    {
        ///      using (ZipFile zip = new ZipFile())
        ///      {
        ///        zip.AddFile("c:\\photos\\personal\\7440-N49th.png");
        ///        zip.AddFile("c:\\Desktop\\2008-Regional-Sales-Report.pdf");
        ///        zip.AddFile("ReadMe.txt");
        ///
        ///        zip.Save("Package.zip");
        ///      }
        ///    }
        ///    catch (System.Exception ex1)
        ///    {
        ///      System.Console.Error.WriteLine("exception: " + ex1);
        ///    }
        /// </code>
        /// 
        /// <code lang="VB">
        ///  Try 
        ///       Using zip As ZipFile = New ZipFile
        ///           zip.AddFile("c:\photos\personal\7440-N49th.png")
        ///           zip.AddFile("c:\Desktop\2008-Regional-Sales-Report.pdf")
        ///           zip.AddFile("ReadMe.txt")
        ///           zip.Save("Package.zip")
        ///       End Using
        ///   Catch ex1 As Exception
        ///       Console.Error.WriteLine("exception: {0}", ex1.ToString)
        ///   End Try
        /// </code>
        /// </example>
        /// 
        /// <overloads>This method has two overloads.</overloads>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.AddItem(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.AddDirectory(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateFile(string)"/>
        ///
        /// <param name="fileName">
        /// The name of the file to add. It should refer to a file in the filesystem.  
        /// The name of the file may be a relative path or a fully-qualified path. 
        /// </param>
        /// <returns>The ZipEntry corresponding to the File added.</returns>
        public ZipEntry AddFile(string fileName)
        {
            return AddFile(fileName, null);
        }





        /// <summary>
        /// Adds a File to a Zip file archive, potentially overriding the path to be used
        /// within the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The file added by this call to the ZipFile is not written to the zip file
        /// archive until the application calls Save() on the ZipFile. 
        /// </para>
        /// 
        /// <para>
        /// This method will throw an Exception if an entry with the same name already exists
        /// in the ZipFile.
        /// </para>
        ///
        /// <para>
        /// This version of the method allows the caller to explicitly specify the 
        /// directory path to be used in the archive. 
        /// </para>
        /// 
        /// <para>
        /// For ZipFile properties including <see cref="Encryption"/>, <see cref="Password"/>,
        /// <see cref="WantCompression"/>, <see cref="ProvisionalAlternateEncoding"/>, 
        /// <see cref="ExtractExistingFile"/>, and <see
        /// cref="ForceNoCompression"/>, their respective values at the time of this call will be
        /// applied to the ZipEntry added.
        /// </para>
        ///
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// In this example, three files are added to a Zip archive. The ReadMe.txt file
        /// will be placed in the root of the archive. The .png file will be placed in a folder 
        /// within the zip called images.  The pdf file will be included into a
        /// folder within the zip called files\docs, and will be encrypted with the 
        /// given password.
        /// </para>
        /// <code>
        /// try
        /// {
        ///   using (ZipFile zip = new ZipFile())
        ///   {
        ///     // the following entry will be inserted at the root in the archive.
        ///     zip.AddFile("c:\\datafiles\\ReadMe.txt", "");
        ///     // this image file will be inserted into the "images" directory in the archive.
        ///     zip.AddFile("c:\\photos\\personal\\7440-N49th.png", "images");
        ///     // the following will result in a password-protected file called 
        ///     // files\\docs\\2008-Regional-Sales-Report.pdf  in the archive.
        ///     zip.Password = "EncryptMe!";
        ///     zip.AddFile("c:\\Desktop\\2008-Regional-Sales-Report.pdf", "files\\docs");
        ///     zip.Save("Archive.zip");
        ///   }
        /// }
        /// catch (System.Exception ex1)
        /// {
        ///   System.Console.Error.WriteLine("exception: {0}", ex1);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        ///   Try 
        ///       Using zip As ZipFile = New ZipFile
        ///           ' the following entry will be inserted at the root in the archive.
        ///           zip.AddFile("c:\datafiles\ReadMe.txt", "")
        ///           ' this image file will be inserted into the "images" directory in the archive.
        ///           zip.AddFile("c:\photos\personal\7440-N49th.png", "images")
        ///           ' the following will result in a password-protected file called 
        ///           ' files\\docs\\2008-Regional-Sales-Report.pdf  in the archive.
        ///           zip.Password = "EncryptMe!"
        ///           zip.AddFile("c:\Desktop\2008-Regional-Sales-Report.pdf", "files\documents")
        ///           zip.Save("Archive.zip")
        ///       End Using
        ///   Catch ex1 As Exception
        ///       Console.Error.WriteLine("exception: {0}", ex1)
        ///   End Try
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.AddItem(string,string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.AddDirectory(string, string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateFile(string,string)"/>
        ///
        /// <param name="fileName">
        /// The name of the file to add.  The name of the file may be a relative path or 
        /// a fully-qualified path.
        /// </param>
        ///
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the FileName.  This path
        /// may, or may not, correspond to a real directory in the current filesystem.  If the
        /// files within the zip are later extracted, this is the path used for the extracted
        /// file.  Passing null (nothing in VB) will use the path on the FileName, if any.
        /// Passing the empty string ("") will insert the item at the root path within the
        /// archive.
        /// </param>
        ///
        /// <returns>The ZipEntry corresponding to the file added.</returns>
        public ZipEntry AddFile(string fileName, String directoryPathInArchive)
        {
            string nameInArchive = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
            ZipEntry ze = ZipEntry.Create(fileName, nameInArchive);
            ze.BufferSize = BufferSize;
            //ze.TrimVolumeFromFullyQualifiedPaths = TrimVolumeFromFullyQualifiedPaths;
            ze.ForceNoCompression = ForceNoCompression;
            ze.ExtractExistingFile = ExtractExistingFile;
            ze.WillReadTwiceOnInflation = WillReadTwiceOnInflation;
            ze.WantCompression = WantCompression;
            ze.ProvisionalAlternateEncoding = ProvisionalAlternateEncoding;
            ze._zipfile = this;
            ze.Encryption = Encryption;
            ze.Password = _Password;
            if (Verbose) StatusMessageTextWriter.WriteLine("adding {0}...", fileName);
            InsureUniqueEntry(ze);
            _entries.Add(ze);
            _contentsChanged = true;
            return ze;
        }



        /// <summary>
        /// This method removes a collection of entries from the ZipFile.
        /// </summary>
        ///
        /// <param name="entriesToRemove">
        /// A collection of ZipEntry instances from this zip file to be removed. For example, 
        /// you can pass in an array of ZipEntry instances; or you can call SelectEntries(), and 
        /// then add or remove entries from that ICollection&lt;ZipEntry&gt;
        /// (ICollection(Of ZipEntry) in VB), and pass that ICollection to this method.
        /// </param>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.SelectEntries(String)" />
        /// <seealso cref="Ionic.Zip.ZipFile.RemoveSelectedEntries(String)" />
        public void RemoveEntries(System.Collections.Generic.ICollection<ZipEntry> entriesToRemove)
        {
            foreach (ZipEntry e in entriesToRemove)
            {
                this.RemoveEntry(e);
            }
        }


        /// <summary>
        /// This method removes a collection of entries from the ZipFile, by name.
        /// </summary>
        ///
        /// <param name="entriesToRemove">
        /// A collection of strings that refer to names of entries to be removed from the ZipFile. 
        /// For example, 
        /// you can pass in an array of ZipEntry instances; or you can call SelectEntries(), and 
        /// then add or remove entries from that ICollection&lt;ZipEntry&gt;
        /// (ICollection(Of ZipEntry) in VB), and pass that ICollection to this method.
        /// </param>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.SelectEntries(String)" />
        /// <seealso cref="Ionic.Zip.ZipFile.RemoveSelectedEntries(String)" />
        public void RemoveEntries(System.Collections.Generic.ICollection<String> entriesToRemove)
        {
            foreach (String e in entriesToRemove)
            {
                this.RemoveEntry(e);
            }
        }


        /// <summary>
        /// This method adds a set of files to the ZipFile.
        /// </summary>
        ///
        /// <remarks>
	/// <para>
	/// Use this method to add a set of files to the zip archive, in one call.  
	/// </para>
        /// <para>
        /// For ZipFile properties including <see cref="Encryption"/>, <see cref="Password"/>,
        /// <see cref="WantCompression"/>, <see cref="ProvisionalAlternateEncoding"/>, 
        /// <see cref="ExtractExistingFile"/>, and <see
        /// cref="ForceNoCompression"/>, their respective values at the time of this call will be
        /// applied to each ZipEntry added.
        /// </para>
        /// </remarks>
        ///
        /// <param name="fileNames">
        /// The collection of names of the files to add. Each string should refer to a file in 
        /// the filesystem. The name of the file may be a relative path or a fully-qualified path. 
        /// </param>
        ///
        /// <example>
        /// This example shows how to create a zipfile, and add a few files into it. 
        /// <code>
        /// String ZipFileToCreate = "archive1.zip";
        /// String DirectoryToZip = "c:\\reports";
        /// using (ZipFile zip = new ZipFile())
        /// { 
        ///   // Store all files found in the top level directory, into the zip archive.
        ///   String[] filenames = System.IO.Directory.GetFiles(DirectoryToZip);
        ///   zip.AddFiles(filenames);
        ///   zip.Save(ZipFileToCreate);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim ZipFileToCreate As String = "archive1.zip"
        /// Dim DirectoryToZip As String = "c:\reports"
        /// Using zip As ZipFile = New ZipFile
        ///     ' Store all files found in the top level directory, into the zip archive.
        ///     Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        ///     zip.AddFiles(filenames)
        ///     zip.Save(ZipFileToCreate)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.AddSelectedFiles(String, String)" />
        public void AddFiles(System.Collections.Generic.ICollection<String> fileNames)
        {
            this.AddFiles(fileNames, null);
        }


        /// <summary>
        /// Adds or updates a set of files in the ZipFile.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// Any files that already exist in the archive are updated. Any files that don't yet
        /// exist in the archive are added.
        /// </para>
        ///
        /// <para>
        /// For ZipFile properties including <see cref="Encryption"/>, <see cref="Password"/>,
        /// <see cref="WantCompression"/>, <see cref="ProvisionalAlternateEncoding"/>, 
        /// <see cref="ExtractExistingFile"/>, and <see
        /// cref="ForceNoCompression"/>, their respective values at the time of this call will be
        /// applied to each ZipEntry added.
        /// </para>
        /// </remarks>
        ///
        /// <param name="fileNames">
        /// The collection of names of the files to update. Each string should refer to a file in 
        /// the filesystem. The name of the file may be a relative path or a fully-qualified path. 
        /// </param>
        ///
        public void UpdateFiles(System.Collections.Generic.ICollection<String> fileNames)
        {
            this.UpdateFiles(fileNames, null);
        }


        /// <summary>
        /// Adds a set of files to the ZipFile, using the specified directory path 
        /// in the archive.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// For ZipFile properties including <see cref="Encryption"/>, <see cref="Password"/>,
        /// <see cref="WantCompression"/>, <see cref="ProvisionalAlternateEncoding"/>, 
        /// <see cref="ExtractExistingFile"/>, and <see
        /// cref="ForceNoCompression"/>, their respective values at the time of this call will be
        /// applied to each ZipEntry added.
        /// </para>
        /// </remarks>
        ///
        /// <param name="fileNames">
        /// The names of the files to add. Each string should refer to a file in the filesystem.  
        /// The name of the file may be a relative path or a fully-qualified path. 
        /// </param>
        ///
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the file name.  This path
        /// may, or may not, correspond to a real directory in the current filesystem.  If the
        /// files within the zip are later extracted, this is the path used for the extracted
        /// file.  Passing null (nothing in VB) will use the path on the FileName, if any.
        /// Passing the empty string ("") will insert the item at the root path within the
        /// archive.
        /// </param>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.AddSelectedFiles(String, String)" />
        public void AddFiles(System.Collections.Generic.ICollection<String> fileNames, String directoryPathInArchive)
        {
            foreach (var f in fileNames)
                this.AddFile(f, directoryPathInArchive);
        }


        /// <summary>
        /// Adds or updates a set of files to the ZipFile, using the specified directory path 
        /// in the archive.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para>
        /// Any files that already exist in the archive are updated. Any files that don't yet
        /// exist in the archive are added.
        /// </para>
        ///
        /// <para>
        /// For ZipFile properties including <see cref="Encryption"/>, <see cref="Password"/>,
        /// <see cref="WantCompression"/>, <see cref="ProvisionalAlternateEncoding"/>, 
        /// <see cref="ExtractExistingFile"/>, and <see
        /// cref="ForceNoCompression"/>, their respective values at the time of this call will be
        /// applied to each ZipEntry added.
        /// </para>
        /// </remarks>
        ///
        /// <param name="fileNames">
        /// The names of the files to add or update. Each string should refer to a file in the filesystem.  
        /// The name of the file may be a relative path or a fully-qualified path. 
        /// </param>
        ///
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the file name.  This path
        /// may, or may not, correspond to a real directory in the current filesystem.  If the
        /// files within the zip are later extracted, this is the path used for the extracted
        /// file.  Passing null (nothing in VB) will use the path on the FileName, if any.
        /// Passing the empty string ("") will insert the item at the root path within the
        /// archive.
        /// </param>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.AddSelectedFiles(String, String)" />
        public void UpdateFiles(System.Collections.Generic.ICollection<String> fileNames, String directoryPathInArchive)
        {
            foreach (var f in fileNames)
                this.UpdateFile(f, directoryPathInArchive);
        }




        /// <summary>
        /// Adds or Updates a File in a Zip file archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method adds a file to a zip archive, or, if the file already exists in the zip archive, 
        /// this method Updates the content of that given filename in the zip archive.  
        /// The <c>UpdateFile</c> method might more accurately be called "AddOrUpdateFile".
        /// </para>
        ///
        /// <para>
        /// Upon success, there is no way for the application to learn whether the file was added
        /// versus updated.
        /// </para>
        ///
        /// <para>
        /// For ZipFile properties including <see cref="Encryption"/>, <see cref="Password"/>,
        /// <see cref="WantCompression"/>, <see cref="ProvisionalAlternateEncoding"/>, 
        /// <see cref="ExtractExistingFile"/>, and <see
        /// cref="ForceNoCompression"/>, their respective values at the time of this call will be
        /// applied to the ZipEntry added.
        /// </para>
        /// </remarks>
        ///
        /// <example>
        /// This example shows how to Update an existing entry in a zipfile. The first call to 
        /// UpdateFile adds the file to the newly-created zip archive.  The second 
        /// call to UpdateFile updates the content for that file in the zip archive.
        /// <code>
        /// using (ZipFile zip1 = new ZipFile())
        /// {
        ///   // UpdateFile might more accurately be called "AddOrUpdateFile"
        ///   zip1.UpdateFile("MyDocuments\\Readme.txt", "");
        ///   zip1.UpdateFile("CustomerList.csv", "");
        ///   zip1.Comment = "This zip archive has been created.";
        ///   zip1.Save("Content.zip");
        /// }
        /// 
        /// using (ZipFile zip2 = ZipFile.Read("Content.zip"))
        /// {
        ///   zip2.UpdateFile("Updates\\Readme.txt", "");
        ///   zip2.Comment = "This zip archive has been updated: The Readme.txt file has been changed.";
        ///   zip2.Save();
        /// }
        ///
        /// </code>
        /// <code lang="VB">
        ///   Using zip1 As New ZipFile
        ///       ' UpdateFile might more accurately be called "AddOrUpdateFile"
        ///       zip1.UpdateFile("MyDocuments\Readme.txt", "")
        ///       zip1.UpdateFile("CustomerList.csv", "")
        ///       zip1.Comment = "This zip archive has been created."
        ///       zip1.Save("Content.zip")
        ///   End Using
        ///
        ///   Using zip2 As ZipFile = ZipFile.Read("Content.zip")
        ///       zip2.UpdateFile("Updates\Readme.txt", "")
        ///       zip2.Comment = "This zip archive has been updated: The Readme.txt file has been changed."
        ///       zip2.Save
        ///   End Using
        /// </code>
        /// </example>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.AddFile(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateDirectory(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateItem(string)"/>
        ///
        /// <param name="fileName">
        /// The name of the file to add or update. It should refer to a file in the filesystem.  
        /// The name of the file may be a relative path or a fully-qualified path. 
        /// </param>
        ///
        /// <returns>The ZipEntry corresponding to the File that was added or updated.</returns>
        public ZipEntry UpdateFile(string fileName)
        {
            return UpdateFile(fileName, null);
        }



        /// <summary>
        /// Adds or Updates a File in a Zip file archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method adds a file to a zip archive, or, if the file already exists in the zip
        /// archive, this method Updates the content of that given filename in the zip archive.
        /// </para>
        /// 
        /// <para>
        /// This version of the method allows the caller to explicitly specify the 
        /// directory path to be used in the archive.  The entry to be added or updated is found by 
        /// using the specified directory path, combined with the basename of the specified 
        /// filename. 
        /// </para>
        /// 
        /// <para>
        /// Upon success, there is no way for the application to learn if the file was 
        /// added versus updated. 
        /// </para>
        ///
        /// <para>
        /// For ZipFile properties including <see cref="Encryption"/>, <see cref="Password"/>,
        /// <see cref="WantCompression"/>, <see cref="ProvisionalAlternateEncoding"/>, 
        /// <see cref="ExtractExistingFile"/>, and <see
        /// cref="ForceNoCompression"/>, their respective values at the time of this call will be
        /// applied to the ZipEntry added.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.AddFile(string,string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateDirectory(string,string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateItem(string,string)"/>
        ///
        /// <param name="fileName">
        /// The name of the file to add or update. It should refer to a file in the filesystem.  
        /// The name of the file may be a relative path or a fully-qualified path. 
        /// </param>
        ///
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the FileName.  This path
        /// may, or may not, correspond to a real directory in the current filesystem.  If the
        /// files within the zip are later extracted, this is the path used for the extracted
        /// file.  Passing null (nothing in VB) will use the path on the FileName, if any.
        /// Passing the empty string ("") will insert the item at the root path within the
        /// archive.
        /// </param>
        ///
        /// <returns>The ZipEntry corresponding to the File that was added or updated.</returns>
        public ZipEntry UpdateFile(string fileName, String directoryPathInArchive)
        {
            // ideally this would all be transactional!
            var key = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
            if (this[key] != null)
                this.RemoveEntry(key);
            return this.AddFile(fileName, directoryPathInArchive);
        }





        /// <summary>
        /// Add or Update a Directory in a zip archive.  
        /// </summary>
        ///
        /// <remarks>
        /// If the specified directory does not exist in the archive, then this method is
        /// equivalent to calling AddDirectory().  If the specified directory already exists in
        /// the archive, then this method updates any existing entries, and adds any new
        /// entries. Any entries that are in the zip archive but not in the specified directory,
        /// are left alone.  In other words, the contents of the zip file will be a union of the
        /// previous contents and the new files.
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateFile(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.AddDirectory(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateItem(string)"/>
        ///
        /// <param name="directoryName">The path to the directory to be added to the zip archive, 
        /// or updated in the zip archive.</param>
        /// <returns>The ZipEntry corresponding to the Directory that was added or updated.</returns>
        public ZipEntry UpdateDirectory(string directoryName)
        {
            return UpdateDirectory(directoryName, null);
        }


        /// <summary>
        /// Add or Update a directory in the zip archive at the specified root directory in the
        /// archive.  
        /// </summary>
        ///
        /// <remarks>
        /// If the specified directory does not exist in the archive, then this method is
        /// equivalent to calling AddDirectory().  If the specified directory already exists in
        /// the archive, then this method updates any existing entries, and adds any new
        /// entries. Any entries that are in the zip archive but not in the specified directory,
        /// are left alone.  In other words, the contents of the zip file will be a union of the
        /// previous contents and the new files.
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateFile(string,string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.AddDirectory(string,string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateItem(string,string)"/>
        ///
        /// <param name="directoryName">The path to the directory to be added to the zip archive, 
        /// or updated in the zip archive.</param>
        ///
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the ItemName.  This path
        /// may, or may not, correspond to a real directory in the current filesystem.  If the
        /// files within the zip are later extracted, this is the path used for the extracted
        /// file.  Passing null (nothing in VB) will use the path on the FileName, if any.
        /// Passing the empty string ("") will insert the item at the root path within the
        /// archive.
        /// </param>
        /// 
        /// <returns>The ZipEntry corresponding to the Directory that was added or updated.</returns>
        public ZipEntry UpdateDirectory(string directoryName, String directoryPathInArchive)
        {
            // ideally this would be transactional!
            //var key = ZipEntry.NameInArchive(directoryName, directoryPathInArchive);
            //if (this[key] != null)
            //    this.RemoveEntry(key);
            ////this.AddDirectory(DirectoryName, DirectoryPathInArchive);
            return this.AddOrUpdateDirectoryImpl(directoryName, directoryPathInArchive, AddOrUpdateAction.AddOrUpdate);
        }





        /// <summary>
        /// Add or Update a File or Directory in the zip archive. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This is useful when the application is not sure or does not care if the item to be
        /// added is a file or directory, and does not know or does not care if the item already
        /// exists in the ZipFile. Calling this method is equivalent to calling RemoveEntry() if
        /// an entry by the same name already exists, followed calling by AddItem().
        /// </para>
        ///
        /// <para>
        /// For ZipFile properties including <see cref="Encryption"/>, <see cref="Password"/>,
        /// <see cref="WantCompression"/>, <see cref="ProvisionalAlternateEncoding"/>, 
        /// <see cref="ExtractExistingFile"/>, and <see
        /// cref="ForceNoCompression"/>, their respective values at the time of this call will be
        /// applied to the ZipEntry added.
        /// </para>
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.AddItem(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateFile(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateDirectory(string)"/>
        ///
        /// <param name="itemName">the path to the file or directory to be added or updated.</param>
        public void UpdateItem(string itemName)
        {
            UpdateItem(itemName, null);
        }


        /// <summary>
        /// Add or Update a File or Directory.  
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This method is useful when the application is not sure or does not care if the item to be
        /// added is a file or directory, and does not know or does not care if the item already
        /// exists in the ZipFile. Calling this method is equivalent to  calling
        /// RemoveEntry(), if an entry by that name exists, and then calling  AddItem().
        /// </para>
        /// 
        /// <para>
        /// This version of the method allows the caller to explicitly specify the directory path
        /// to be used for the item being added to the archive.  The entry or entries that are
        /// added or updated will use the specified <c>DirectoryPathInArchive</c>. Extracting the
        /// entry from the archive will result in a file stored in that directory path.
        /// </para>
        ///
        /// <para>
        /// For ZipFile properties including <see cref="Encryption"/>, <see cref="Password"/>,
        /// <see cref="WantCompression"/>, <see cref="ProvisionalAlternateEncoding"/>, 
        /// <see cref="ExtractExistingFile"/>, and <see
        /// cref="ForceNoCompression"/>, their respective values at the time of this call will be
        /// applied to the ZipEntry added.
        /// </para>
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.AddItem(string, string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateFile(string, string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateDirectory(string, string)"/>
        ///
        /// <param name="itemName">The path for the File or Directory to be added or updated.</param>
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the ItemName.  This path
        /// may, or may not, correspond to a real directory in the current filesystem.  If the
        /// files within the zip are later extracted, this is the path used for the extracted
        /// file.  Passing null (nothing in VB) will use the path on the FileName, if any.
        /// Passing the empty string ("") will insert the item at the root path within the
        /// archive.
        /// </param>
        public void UpdateItem(string itemName, string directoryPathInArchive)
        {
            if (System.IO.File.Exists(itemName))
                UpdateFile(itemName, directoryPathInArchive);

            else if (System.IO.Directory.Exists(itemName))
                UpdateDirectory(itemName, directoryPathInArchive);

            else
                throw new System.IO.FileNotFoundException(String.Format("That file or directory ({0}) does not exist!", itemName));
        }


        /// <summary>
        /// Uses the given stream as input to create an entry in the ZipFile, with the 
        /// given filename and given directory path.  
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The stream must be open and readable during the call to 
        /// <c>ZipFile.Save</c>.  You can dispense the stream on a just-in-time basis using
        /// the <see cref="ZipEntry.InputStream"/> property. Check the documentation of that
        /// property for more information. 
        /// </para>
        /// 
        /// <para>
        /// For ZipFile properties including <see cref="Encryption"/>, <see cref="Password"/>,
        /// <see cref="WantCompression"/>, <see cref="ProvisionalAlternateEncoding"/>, 
        /// <see cref="ExtractExistingFile"/>, and <see
        /// cref="ForceNoCompression"/>, their respective values at the time of this call will be
        /// applied to the ZipEntry added.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <example>
        /// <code lang="C#">
        /// String ZipToCreate = "Content.zip";
        /// String FileNameInArchive = "Content-From-Stream.bin";
        /// System.IO.Stream StreamToRead = MyStreamOpener();
        /// using (ZipFile zip = new ZipFile())
        /// {
        ///   ZipEntry entry= zip.AddFileFromStream(FileNameInArchive, "basedirectory", StreamToRead);
        ///   entry.Comment = "The content for this entry in the zip file was obtained from a stream";
        ///   zip.AddFile("Readme.txt");
        ///   zip.Save(ZipToCreate);
        /// }
        /// 
        /// </code>
        /// <code lang="VB">
        /// Dim ZipToCreate As String = "Content.zip"
        /// Dim FileNameInArchive As String = "Content-From-Stream.bin"
        /// DIm StreamToRead as System.IO.Stream = MyStreamOpener()
        /// Using zip As ZipFile = New ZipFile()
        ///   Dim entry as ZipEntry = zip.AddFileFromStream(FileNameInArchive, "basedirectory", StreamToRead)
        ///   entry.Comment = "The content for this entry in the zip file was obtained from a stream"
        ///   zip.AddFile("Readme.txt")
        ///   zip.Save(ZipToCreate)
        /// End Using
        /// </code>
        /// </example>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateFileStream(string, string, System.IO.Stream)"/>
        ///
        /// <param name="fileName">FileName which is shown in the ZIP File</param>
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the ItemName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the files within the zip are later extracted, this is the path used for the extracted file. 
        /// Passing null (nothing in VB) will use the path on the FileName, if any.  Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        /// <param name="stream">the input stream from which to grab content for the file</param>
        /// <returns>The ZipEntry added.</returns>
        public ZipEntry AddFileFromStream(string fileName, string directoryPathInArchive, System.IO.Stream stream)
        {
            string n = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
            ZipEntry ze = ZipEntry.Create(fileName, n, true, stream);
            ze.BufferSize = BufferSize;
            //ze.TrimVolumeFromFullyQualifiedPaths = TrimVolumeFromFullyQualifiedPaths;
            ze.ForceNoCompression = ForceNoCompression;
            ze.ExtractExistingFile = ExtractExistingFile;
            ze.WillReadTwiceOnInflation = WillReadTwiceOnInflation;
            ze.WantCompression = WantCompression;
            ze.ProvisionalAlternateEncoding = ProvisionalAlternateEncoding;
            ze._zipfile = this;
            ze.Encryption = Encryption;
            ze.Password = _Password;
            if (Verbose) StatusMessageTextWriter.WriteLine("adding {0}...", fileName);
            InsureUniqueEntry(ze);
            _entries.Add(ze);
            _contentsChanged = true;
            return ze;
        }



        /// <summary>
        /// Uses the given stream as input to create an entry in the ZipFile, with the 
        /// given FileName and given Directory Path.  
        /// </summary>
        ///
        /// <remarks>
        /// This method has been deprecated. Please use <see cref="AddFileFromStream(string, string,
        /// System.IO.Stream)"/>.  This method will be removed in a future version of this library.
        /// </remarks>
        [Obsolete("Please use method AddFileFromStream()")]
        public ZipEntry AddFileStream(string fileName, string directoryPathInArchive, System.IO.Stream stream)
        {
            return AddFileFromStream(fileName, directoryPathInArchive, stream);
        }


        /// <summary>
        /// Adds an entry into the zip archive using the given filename and directory path within
        /// the archive, and the given content for the file.  
        /// </summary>
        ///
        /// <remarks>
        /// No file need exist or is created in the filesystem.
        /// </remarks>
        ///
        /// <param name="content">The content of the file, should it be extracted from the zip.</param>
        /// <param name="fileName">The filename to use within the archive.</param>
        /// <param name="directoryPathInArchive">
        /// Specifies a driectory path to use to override any path in the ItemName.  This path
        /// may, or may not, correspond to a real directory in the current filesystem.  If the
        /// files within the zip are later extracted, this is the path used for the extracted
        /// file.  Passing null (nothing in VB) will use the path on the FileName, if any.
        /// Passing the empty string ("") will insert the item at the root path within the
        /// archive.
        /// </param>
        /// <returns>The ZipEntry added.</returns>
        /// 
        /// <example>
        /// This example shows how to add an entry to the zipfile, using a string as content for
        /// that entry.
        /// <code lang="C#">
        /// string Content = "This string will be the content of the Readme.txt file in the zip archive.";
        /// using (ZipFile zip1 = new ZipFile())
        /// {
        ///   zip1.AddFile("MyDocuments\\Resume.doc", "files");
        ///   zip1.AddFileFromString("Readme.txt", "", Content); 
        ///   zip1.Comment = "This zip file was created at " + System.DateTime.Now.ToString("G");
        ///   zip1.Save("Content.zip");
        /// }
        /// 
        /// </code>
        /// <code lang="VB">
        /// Public Sub Run()
        ///   Dim Content As String = "This string will be the content of the Readme.txt file in the zip archive."
        ///   Using zip1 As ZipFile = New ZipFile
        ///     zip1.AddFileFromString("Readme.txt", "", Content)
        ///     zip1.AddFile("MyDocuments\Resume.doc", "files")
        ///     zip1.Comment = ("This zip file was created at " &amp; DateTime.Now.ToString("G"))
        ///     zip1.Save("Content.zip")
        ///   End Using
        /// End Sub
        /// </code>
        /// </example>
        public ZipEntry AddFileFromString(string fileName, string directoryPathInArchive, string content)
        {
            System.IO.MemoryStream ms = SharedUtilities.StringToMemoryStream(content);
            // note: stream leak!  We create this stream and then never Dispose() it.
            // This shouldn't be a problem though. The MemoryStream will be lazily collected at
            // some later point, after the zipfile is Saved.
            return AddFileFromStream(fileName, directoryPathInArchive, ms);
        }


        /// <summary>
        /// Updates the given entry in the ZipFile, using the given stream as input, and the
        /// given filename and given directory Path.  
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// Calling the method is equivalent to calling RemoveEntry() if an entry by the same name
	/// already exists, and then calling AddFileFromStream() with the given fileName and stream.
        /// </para>
        ///
        /// <para>
        /// The stream must be open and readable during the call to 
        /// <c>ZipFile.Save</c>.  You can dispense the stream on a just-in-time basis using
        /// the <see cref="ZipEntry.InputStream"/> property. Check the documentation of that
        /// property for more information. 
        /// </para>
        /// 
        /// <para>
        /// For ZipFile properties including <see cref="Encryption"/>, <see cref="Password"/>,
        /// <see cref="WantCompression"/>, <see cref="ProvisionalAlternateEncoding"/>, 
        /// <see cref="ExtractExistingFile"/>, and <see
        /// cref="ForceNoCompression"/>, their respective values at the time of this call will be
        /// applied to the ZipEntry added.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.AddFileFromStream(string, string, System.IO.Stream)"/>
        /// <seealso cref="Ionic.Zip.ZipEntry.InputStream"/>
        ///
        /// <param name="fileName">the name associated to the entry in the zip archive.</param>
        /// <param name="directoryPathInArchive">The root path to be used in the zip archive, 
        /// for the entry added from the stream.</param>
        /// <param name="stream">The input stream from which to read file data.</param>
        /// <returns>The ZipEntry added.</returns>
        public ZipEntry UpdateFileFromStream(string fileName, string directoryPathInArchive, System.IO.Stream stream)
        {
            var key = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
            if (this[key] != null)
                this.RemoveEntry(key);

            return AddFileFromStream(fileName, directoryPathInArchive, stream);
        }


        /// <summary>
        /// Updates the given entry in the ZipFile, using the given stream as input, and the
        /// given FileName and given Directory Path.  
        /// </summary>
        ///
        /// <remarks>
        /// This method has been deprecated. Please use <see cref="UpdateFileFromStream(string, string,
        /// System.IO.Stream)"/>.  This method will be removed in a future version of this library.
        /// </remarks>
        [Obsolete("Please use method UpdateFileFromStream()")]

        public ZipEntry UpdateFileStream(string fileName, string directoryPathInArchive, System.IO.Stream stream)
        {
            return UpdateFileFromStream(fileName, directoryPathInArchive, stream);

        }

        private void InsureUniqueEntry(ZipEntry ze1)
        {
            foreach (ZipEntry ze2 in _entries)
            {
                if (SharedUtilities.TrimVolumeAndSwapSlashes(ze1.FileName) == ze2.FileName)
                    throw new ArgumentException(String.Format("The entry '{0}' already exists in the zip archive.", ze1.FileName));
            }
        }

        /// <summary>
        /// Adds the contents of a filesystem directory to a Zip file archive. 
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// The name of the directory may be a relative path or a fully-qualified path. Any files
        /// within the named directory are added to the archive.  Any subdirectories within the
        /// named directory are also added to the archive, recursively.
        /// </para>
        /// 
        /// <para>
        /// Top-level entries in the named directory will appear as top-level 
        /// entries in the zip archive.  Entries in subdirectories in the named 
        /// directory will result in entries in subdirectories in the zip archive.
        /// </para>
        /// 
        /// <para>
        /// If you want the entries to appear in a containing directory in the zip
        /// archive itself, then you should call the AddDirectory() overload that allows
        /// you to explicitly specify a directory path for use in the archive.
        /// </para>
        /// 
        /// <para>
        /// For ZipFile properties including <see cref="Encryption"/>, <see cref="Password"/>,
        /// <see cref="WantCompression"/>, <see cref="ProvisionalAlternateEncoding"/>, 
        /// <see cref="ExtractExistingFile"/>, and <see
        /// cref="ForceNoCompression"/>, their respective values at the time of this call will be
        /// applied to each ZipEntry added.
        /// </para>
        ///
        /// </remarks>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.AddItem(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.AddFile(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateDirectory(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.AddDirectory(string, string)"/>
        ///
        /// <overloads>This method has 2 overloads.</overloads>
        /// 
        /// <param name="directoryName">The name of the directory to add.</param>
        /// <returns>The ZipEntry added.</returns>
        public ZipEntry AddDirectory(string directoryName)
        {
            return AddDirectory(directoryName, null);
        }


        /// <summary>
        /// Adds the contents of a filesystem directory to a Zip file archive, 
        /// overriding the path to be used for entries in the archive. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The name of the directory may be a relative path or a fully-qualified
        /// path. The add operation is recursive, so that any files or subdirectories
        /// within the name directory are also added to the archive.
        /// </para>
        /// 
        /// <para>
        /// Top-level entries in the named directory will appear as top-level 
        /// entries in the zip archive.  Entries in subdirectories in the named 
        /// directory will result in entries in subdirectories in the zip archive.
        /// </para>
        /// 
        /// <para>
        /// For ZipFile properties including <see cref="Encryption"/>, <see cref="Password"/>,
        /// <see cref="WantCompression"/>, <see cref="ProvisionalAlternateEncoding"/>, 
        /// <see cref="ExtractExistingFile"/>, and <see
        /// cref="ForceNoCompression"/>, their respective values at the time of this call will be
        /// applied to each ZipEntry added.
        /// </para>
        ///
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// In this code, calling the ZipUp() method with a value of "c:\reports" for the
        /// directory parameter will result in a zip file structure in which all entries
        /// are contained in a toplevel "reports" directory.
        /// </para>
        ///
        /// <code lang="C#">
        /// public void ZipUp(string targetZip, string directory)
        /// {
        ///   using (var zip = new ZipFile())
        ///   {
        ///     zip.AddDirectory(directory, System.IO.Path.GetFileName(directory));
        ///     zip.Save(targetZip);
        ///   }
        /// }
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.AddItem(string, string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.AddFile(string, string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateDirectory(string, string)"/>
        ///
        /// <param name="directoryName">The name of the directory to add.</param>
        /// 
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the DirectoryName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the zip is later extracted, this is the path used for the extracted file or directory. 
        /// Passing null (nothing in VB) or the empty string ("")
        /// will insert the items at the root path within the archive. 
        /// </param>
        /// 
        /// <returns>The ZipEntry added.</returns>
        public ZipEntry AddDirectory(string directoryName, string directoryPathInArchive)
        {
            return AddOrUpdateDirectoryImpl(directoryName, directoryPathInArchive, AddOrUpdateAction.AddOnly);
        }


        /// <summary>
        /// Creates a directory in the zip archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// Use this when you want to create a directory in the archive but there is no
        /// corresponding filesystem representation for that directory.
        /// </para>
        ///
        /// <para>
        /// You will probably not need to do this in your code. One of the only times
        /// you will want to do this is if you want an empty directory in the zip
        /// archive.  The reason: if you add a file to a zip archive that is stored within a
        /// multi-level directory, all of the directory tree is implicitly created in
        /// the zip archive.  
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="directoryNameInArchive">
        /// The name of the directory to create in the archive.
        /// </param>
        /// <returns>The ZipEntry added.</returns>
        public ZipEntry AddDirectoryByName(string directoryNameInArchive)
        {
            // add the directory itself.
            ZipEntry baseDir = ZipEntry.Create(directoryNameInArchive, directoryNameInArchive);
            baseDir.BufferSize = BufferSize;
            //baseDir.TrimVolumeFromFullyQualifiedPaths = TrimVolumeFromFullyQualifiedPaths;
            baseDir.MarkAsDirectory();
            baseDir._zipfile = this;
            InsureUniqueEntry(baseDir);
            _entries.Add(baseDir);
            _contentsChanged = true;
            return baseDir;
        }



        private ZipEntry AddOrUpdateDirectoryImpl(string directoryName, string rootDirectoryPathInArchive, AddOrUpdateAction action)
        {
            if (rootDirectoryPathInArchive == null)
            {
                rootDirectoryPathInArchive = "";
                //System.IO.Path.GetDirectoryName(SharedUtilities.TrimVolumeAndSwapSlashes(directoryName));
                //SharedUtilities.TrimVolumeAndSwapSlashes(directoryName);
            }

            return AddOrUpdateDirectoryImpl(directoryName, rootDirectoryPathInArchive, action, 0);
        }



        private ZipEntry AddOrUpdateDirectoryImpl(string directoryName, string rootDirectoryPathInArchive, AddOrUpdateAction action, int level)
        {
            if (Verbose) StatusMessageTextWriter.WriteLine("{0} {1}...",
                               (action == AddOrUpdateAction.AddOnly) ? "adding" : "Adding or updating", directoryName);

            string dirForEntries = rootDirectoryPathInArchive;
            ZipEntry baseDir = null;

            if (level > 0)
            {
                int f = directoryName.Length;
                for (int i = level; i > 0; i--)
                    f = directoryName.LastIndexOfAny("/\\".ToCharArray(), f - 1, f - 1);

                dirForEntries = directoryName.Substring(f + 1);
                dirForEntries = System.IO.Path.Combine(rootDirectoryPathInArchive, dirForEntries);
            }

            // if not top level, or if the root is non-empty, then explicitly add the directory
            if (level > 0 || rootDirectoryPathInArchive != "")
            {
                baseDir = ZipEntry.Create(directoryName, dirForEntries);
                baseDir.BufferSize = BufferSize;
                baseDir.ProvisionalAlternateEncoding = this.ProvisionalAlternateEncoding;  // workitem 6410
                //baseDir.TrimVolumeFromFullyQualifiedPaths = TrimVolumeFromFullyQualifiedPaths;
                baseDir.MarkAsDirectory();
                baseDir._zipfile = this;

                // Previously, we used to test for the existence of the directory and 
                // throw if it exists.  But that seems silly. We will still throw 
                // if a file exists and the action is AddOnly.  But for a directory, 
                // it does not matter if it already exists.  So no throw. 

                //if (action == AddOrUpdateAction.AddOnly)
                //    InsureUniqueEntry(baseDir);
                //else
                //{
                //    // For updates, remove the old entry before adding the new. 
                //    ZipEntry e = this[baseDir.FileName];
                //    if (e != null)
                //        RemoveEntry(e);
                //}


                // check for uniqueness:
                ZipEntry e = this[baseDir.FileName];
                if (e == null)
                {
                    _entries.Add(baseDir);
                    _contentsChanged = true;
                }
                dirForEntries = baseDir.FileName;
            }

            String[] filenames = System.IO.Directory.GetFiles(directoryName);

            // add the files: 
            foreach (String filename in filenames)
            {
                if (action == AddOrUpdateAction.AddOnly)
                    AddFile(filename, dirForEntries);
                else
                    UpdateFile(filename, dirForEntries);
            }

            // add the subdirectories:
            String[] dirnames = System.IO.Directory.GetDirectories(directoryName);
            foreach (String dir in dirnames)
            {
                AddOrUpdateDirectoryImpl(dir, rootDirectoryPathInArchive, action, level + 1);
            }
            //_contentsChanged = true;

            return baseDir;
        }


        #endregion

        #region Saving




        /// <summary>
        /// Saves the Zip archive, using the name given when the ZipFile was instantiated. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The zip file is written to storage only when the caller calls <c>Save</c>.  
        /// The Save operation writes the zip content to a temporary file. 
        /// Then, if the zip file already exists (for example when adding an item to a zip archive)
        /// this method will replace the existing zip file with this temporary file.
        /// If the zip file does not already exist, the temporary file is renamed 
        /// to the desired name.  
        /// </para>
        ///
        /// <para>
        /// When using a filesystem file for the Zip output, it is possible to call
        /// <c>Save</c> multiple times on the ZipFile instance. With each call the zip content
        /// is written to the output file. When saving to a <c>Stream</c>, after the initial
        /// call to <c>Save</c>, additional calls to <c>Save</c> will throw. This is because the
        /// stream is assumed to be a write-only stream, and after the initial <c>Save</c>, it
        /// is not possible to seek backwards and "unwrite" the zip file data.
        /// </para>
        ///
        /// <para>
        /// Data for entries that have been added to the <c>ZipFile</c> instance is written
        /// to the output when the <c>Save</c> method is called. This means that the input
        /// streams for those entries must be available at the time the application
        /// calls <c>Save</c>.  If, for example, the application adds entries with
        /// <c>AddFileFromStream</c> using a dynamically-allocated <c>MemoryStream</c>,
        /// the memory stream must not have been disposed before the call to <c>Save</c>. See
        /// the <see cref="ZipEntry.InputStream"/> property for more discussion of the 
        /// availability requirements of the input stream for an entry, and an approach for providing 
        /// just-in-time stream lifecycle management.
        /// </para>
        ///
        /// <para>
        /// When using the zip library within an ASP.NET application, you may wish to set the
        /// <c>TempFileFolder</c> property on the <c>ZipFile</c> instance before calling Save().
        /// </para>
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.AddFileFromStream(String, String, System.IO.Stream)"/>
        ///
        /// <exception cref="Ionic.Zip.BadStateException">
        /// Thrown if you haven't specified a location or stream for saving the zip,
        /// either in the constructor or by setting the Name property. 
        /// </exception>
        ///
        public void Save()
        {
            try
            {
                bool thisSaveUsedZip64 = false;
                _saveOperationCanceled = false;
                OnSaveStarted();

                if (WriteStream == null)
                    throw new BadStateException("You haven't specified where to save the zip.");

                // check if modified, before saving. 
                if (!_contentsChanged) return;

                if (Verbose) StatusMessageTextWriter.WriteLine("Saving....");

                // validate the number of entries
                if (_entries.Count >= 0xFFFF && _zip64 == Zip64Option.Never)
                    throw new ZipException("The number of entries is 0xFFFF or greater. Consider setting the UseZip64WhenSaving property on the ZipFile instance.");


                {

                    // write an entry in the zip for each file
                    int n = 0;
                    foreach (ZipEntry e in _entries)
                    {

                        OnSaveEntry(n, e, true);
                        e.Write(WriteStream);
                        e._zipfile = this;
                        n++;
                        OnSaveEntry(n, e, false);
                        if (_saveOperationCanceled)
                            break;

                        thisSaveUsedZip64 |= e.OutputUsedZip64.Value;
                    }

                }


                if (_saveOperationCanceled)
                    return;

                WriteCentralDirectoryStructure(WriteStream);

                OnSaveEvent(ZipProgressEventType.Saving_AfterSaveTempArchive);

                _hasBeenSaved = true;
                _contentsChanged = false;

                thisSaveUsedZip64 |= _NeedZip64CentralDirectory;
                _OutputUsesZip64 = new Nullable<bool>(thisSaveUsedZip64);

                // do the rename as necessary
                if ((_temporaryFileName != null) && (_name != null))
                {
                    // _temporaryFileName may remain null if we are writing to a stream
                    // only close the stream if there is a file behind it. 
                    WriteStream.Close();
#if !NETCF20
                    WriteStream.Dispose();
#endif
                    WriteStream = null;

                    if (_saveOperationCanceled)
                        return;

                    if ((_fileAlreadyExists) && (this._readstream != null))
                    {
                        // This means we opened and read a zip file. 
                        // If we are now saving to the same file, we need to close the
                        // orig file, first.
                        this._readstream.Close();
                        this._readstream = null;
                    }

                    if (_fileAlreadyExists)
                    {
                        // We do not just call File.Replace() here because 
                        // there is a possibility that the TEMP volume is different 
                        // that the volume for the final file (c:\ vs d:\).
                        // So we need to do a Delete+Move pair. 
                        //
                        // Ideally this would be transactional. 
                        // 
                        // It's possible that the delete succeeds and the move fails.  
                        // in that case, we're hosed, and we'll throw.
                        //
                        // Could make this more complicated by moving (renaming) the first file, then
                        // moving the second, then deleting the first file. But the
                        // error handling and unwrap logic just gets more complicated.
                        //
                        // Better to just keep it simple. 
                        System.IO.File.Delete(_name);
                        OnSaveEvent(ZipProgressEventType.Saving_BeforeRenameTempArchive);
                        System.IO.File.Move(_temporaryFileName, _name);
                        OnSaveEvent(ZipProgressEventType.Saving_AfterRenameTempArchive);
                    }
                    else
                        System.IO.File.Move(_temporaryFileName, _name);

                    _fileAlreadyExists = true;
                }

                OnSaveCompleted();
                _JustSaved = true;

            }

        // workitem 5043
            finally
            {
                CleanupAfterSaveOperation();
            }

            return;
        }



        private void RemoveTempFile()
        {
            try
            {
                if (System.IO.File.Exists(_temporaryFileName))
                {
                    System.IO.File.Delete(_temporaryFileName);
                }
            }
            catch (Exception ex1)
            {
                if (Verbose)
                    StatusMessageTextWriter.WriteLine("ZipFile::Save: could not delete temp file: {0}.", ex1.Message);
            }
        }


        private void CleanupAfterSaveOperation()
        {
            if ((_temporaryFileName != null) && (_name != null))
            {
                // only close the stream if there is a file behind it. 
                if (_writestream != null)
                {
                    try { _writestream.Close(); }
                    catch { }
                    try
                    {
#if !NETCF20
                        _writestream.Dispose();
#endif
                    }
                    catch { }
                }
                _writestream = null;
                RemoveTempFile();
            }
        }


        /// <summary>
        /// Save the file to a new zipfile, with the given name. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This is handy when reading a zip archive from a stream 
        /// and you want to modify the archive (add a file, change a 
        /// comment, etc) and then save it to a file. 
        /// </para>
        /// <para>
        /// It also works if you create a new ZipFile for writing to a 
        /// stream, and then you also want to write it to a filesystem file. 
        /// In that case, call the Save() method, and then also call this method with
        /// a filename. 
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="System.ArgumentException">
        /// Thrown if you specify a directory for the filename.
        /// </exception>
        ///
        /// <param name="zipFileName">
        /// The name of the zip archive to save to. Existing files will 
        /// be overwritten with great prejudice.
        /// </param>
        public void Save(System.String zipFileName)
        {
            // Check for the case where we are re-saving a zip archive 
            // that was originally instantiated with a stream.  In that case, 
            // the _name will be null. If so, we set _writestream to null, 
            // which insures that we'll cons up a new WriteStream (with a filesystem
            // file backing it) in the Save() method.
            if (_name == null)
                _writestream = null;

            _name = zipFileName;
            if (Directory.Exists(_name))
                throw new ZipException("Bad Directory", new System.ArgumentException("That name specifies an existing directory. Please specify a filename.", "zipFileName"));
            _contentsChanged = true;
            _fileAlreadyExists = File.Exists(_name);
            Save();
        }


        /// <summary>
        /// Save the zip archive to the specified stream.
        /// </summary>
	/// 
	/// <remarks>
	/// If you open and manage streams yourself, you can save the zip content to the stream
        /// directly, with this method.  This is the one you would use, for example, if you were
        /// writing an ASP.NET application that dynamically generated a zip file and allowed the
        /// browser to download it. 
	/// </remarks>
	/// 
        /// <param name="outputStream">The <c>System.IO.Stream</c> to write to. It must be writable.</param>
        public void Save(System.IO.Stream outputStream)
        {
            if (!outputStream.CanWrite)
                throw new ArgumentException("The outputStream must be a writable stream.");

            // if we had a filename to save to, we are now obliterating it. 
            _name = null;

            _writestream = new CountingStream(outputStream);

            _contentsChanged = true;
            _fileAlreadyExists = false;
            Save();
        }




        private void WriteCentralDirectoryStructure(Stream s)
        {
            // The Central Directory Structure.
            // We need to keep track of the start and Finish of the Central Directory Structure. 

            // Cannot always use WriteStream.Length or Position; some streams do not 
            // support these. (eg, ASP.NET Response.OutputStream)
            // In those cases we have a CountingStream.

            var output = s as CountingStream;
            long Start = (output != null) ? output.BytesWritten : s.Position;

            foreach (ZipEntry e in _entries)
            {
                e.WriteCentralDirectoryEntry(s);  // this writes a ZipDirEntry corresponding to the ZipEntry
            }

            long Finish = (output != null) ? output.BytesWritten : s.Position;

            Int64 SizeOfCentralDirectory = Finish - Start;

            _NeedZip64CentralDirectory =
        _zip64 == Zip64Option.Always ||
        _entries.Count >= 0xFFFF ||
        SizeOfCentralDirectory > 0xFFFFFFFF ||
        Start > 0xFFFFFFFF;

            // emit ZIP64 extensions as required
            if (_NeedZip64CentralDirectory)
            {
                if (_zip64 == Zip64Option.Never)
                    throw new ZipException("The archive requires a ZIP64 Central Directory. Consider setting the UseZip64WhenSaving property.");

                WriteZip64EndOfCentralDirectory(s, Start, Finish);
            }

            // now, the footer
            WriteCentralDirectoryFooter(s, Start, Finish);
        }




        private void WriteZip64EndOfCentralDirectory(Stream s, long StartOfCentralDirectory, long EndOfCentralDirectory)
        {
            int bufferLength = 12 + 44 + 20;

            byte[] bytes = new byte[bufferLength];

            int i = 0;
            // signature
            bytes[i++] = (byte)(ZipConstants.Zip64EndOfCentralDirectoryRecordSignature & 0x000000FF);
            bytes[i++] = (byte)((ZipConstants.Zip64EndOfCentralDirectoryRecordSignature & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((ZipConstants.Zip64EndOfCentralDirectoryRecordSignature & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((ZipConstants.Zip64EndOfCentralDirectoryRecordSignature & 0xFF000000) >> 24);

            // There is a possibility to include "Extensible" data in the zip64 end-of-central-dir record.
            // I cannot figure out what it might be used to store, so the size of this record is always fixed.
            // Maybe it is used for strong encryption data?  That is for another day. 
            long DataSize = 44;
            Array.Copy(BitConverter.GetBytes(DataSize), 0, bytes, i, 8);
            i += 8;

            // VersionMadeBy = 45;
            bytes[i++] = 45;
            bytes[i++] = 0x00;

            // VersionNeededToExtract = 45;
            bytes[i++] = 45;
            bytes[i++] = 0x00;

            // number of the disk, and the disk with the start of the central dir.  Always zero.
            for (int j = 0; j < 8; j++)
                bytes[i++] = 0x00;

            long numberOfEntries = _entries.Count;
            Array.Copy(BitConverter.GetBytes(numberOfEntries), 0, bytes, i, 8);
            i += 8;
            Array.Copy(BitConverter.GetBytes(numberOfEntries), 0, bytes, i, 8);
            i += 8;

            Int64 SizeofCentraldirectory = EndOfCentralDirectory - StartOfCentralDirectory;
            Array.Copy(BitConverter.GetBytes(SizeofCentraldirectory), 0, bytes, i, 8);
            i += 8;
            Array.Copy(BitConverter.GetBytes(StartOfCentralDirectory), 0, bytes, i, 8);
            i += 8;

            // now, the locator
            // signature
            bytes[i++] = (byte)(ZipConstants.Zip64EndOfCentralDirectoryLocatorSignature & 0x000000FF);
            bytes[i++] = (byte)((ZipConstants.Zip64EndOfCentralDirectoryLocatorSignature & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((ZipConstants.Zip64EndOfCentralDirectoryLocatorSignature & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((ZipConstants.Zip64EndOfCentralDirectoryLocatorSignature & 0xFF000000) >> 24);

            // number of the disk with the zip64 eocd
            bytes[i++] = 0x00;
            bytes[i++] = 0x00;
            bytes[i++] = 0x00;
            bytes[i++] = 0x00;

            // relative offset of the zip64 eocd
            Array.Copy(BitConverter.GetBytes(EndOfCentralDirectory), 0, bytes, i, 8);
            i += 8;

            // total number of disks
            bytes[i++] = 0x01;
            bytes[i++] = 0x00;
            bytes[i++] = 0x00;
            bytes[i++] = 0x00;

            s.Write(bytes, 0, i);
        }




        private void WriteCentralDirectoryFooter(Stream s, long StartOfCentralDirectory, long EndOfCentralDirectory)
        {
            int j = 0;
            int bufferLength = 24;
            byte[] block = null;
            Int16 commentLength = 0;
            if ((Comment != null) && (Comment.Length != 0))
            {
                block = ProvisionalAlternateEncoding.GetBytes(Comment);
                commentLength = (Int16)block.Length;
            }
            bufferLength += commentLength;
            byte[] bytes = new byte[bufferLength];

            int i = 0;
            // signature 
            bytes[i++] = (byte)(ZipConstants.EndOfCentralDirectorySignature & 0x000000FF);
            bytes[i++] = (byte)((ZipConstants.EndOfCentralDirectorySignature & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((ZipConstants.EndOfCentralDirectorySignature & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((ZipConstants.EndOfCentralDirectorySignature & 0xFF000000) >> 24);

            // number of this disk
            bytes[i++] = 0;
            bytes[i++] = 0;

            // number of the disk with the start of the central directory
            bytes[i++] = 0;
            bytes[i++] = 0;

            // handle ZIP64 extensions for the end-of-central-directory 
            if (_entries.Count >= 0xFFFF || _zip64 == Zip64Option.Always)
            {
                // the ZIP64 version.
                for (j = 0; j < 4; j++)
                    bytes[i++] = 0xFF;
            }
            else
            {
                // the standard version.
                // total number of entries in the central dir on this disk
                bytes[i++] = (byte)(_entries.Count & 0x00FF);
                bytes[i++] = (byte)((_entries.Count & 0xFF00) >> 8);

                // total number of entries in the central directory
                bytes[i++] = (byte)(_entries.Count & 0x00FF);
                bytes[i++] = (byte)((_entries.Count & 0xFF00) >> 8);
            }

            // size of the central directory
            Int64 SizeOfCentralDirectory = EndOfCentralDirectory - StartOfCentralDirectory;

            if (SizeOfCentralDirectory >= 0xFFFFFFFF || StartOfCentralDirectory >= 0xFFFFFFFF)
            {
                // The actual data is in the ZIP64 central directory structure
                for (j = 0; j < 8; j++)
                    bytes[i++] = 0xFF;
            }
            else
            {
                // size of the central directory (we just get the low 4 bytes)
                bytes[i++] = (byte)(SizeOfCentralDirectory & 0x000000FF);
                bytes[i++] = (byte)((SizeOfCentralDirectory & 0x0000FF00) >> 8);
                bytes[i++] = (byte)((SizeOfCentralDirectory & 0x00FF0000) >> 16);
                bytes[i++] = (byte)((SizeOfCentralDirectory & 0xFF000000) >> 24);

                // offset of the start of the central directory (we just get the low 4 bytes)
                bytes[i++] = (byte)(StartOfCentralDirectory & 0x000000FF);
                bytes[i++] = (byte)((StartOfCentralDirectory & 0x0000FF00) >> 8);
                bytes[i++] = (byte)((StartOfCentralDirectory & 0x00FF0000) >> 16);
                bytes[i++] = (byte)((StartOfCentralDirectory & 0xFF000000) >> 24);
            }


            // zip archive comment 
            if ((Comment == null) || (Comment.Length == 0))
            {
                // no comment!
                bytes[i++] = (byte)0;
                bytes[i++] = (byte)0;
            }
            else
            {
                // the size of our buffer defines the max length of the comment we can write
                if (commentLength + i + 2 > bytes.Length) commentLength = (Int16)(bytes.Length - i - 2);
                bytes[i++] = (byte)(commentLength & 0x00FF);
                bytes[i++] = (byte)((commentLength & 0xFF00) >> 8);

                if (commentLength != 0)
                {
                    // now actually write the comment itself into the byte buffer
                    for (j = 0; (j < commentLength) && (i + j < bytes.Length); j++)
                    {
                        bytes[i + j] = block[j];
                    }
                    i += j;
                }
            }

            s.Write(bytes, 0, i);
        }

        #endregion


        #region Reading Zip Files

        /// <summary>
        /// Checks the given file to see if it appears to be a valid zip file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Calling this method is equivalent to calling 
        /// <see cref="IsZipFile(string, bool)"/> with the testExtract parameter set to false.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="fileName">The file to check.</param>
        /// <returns>true if the file appears to be a zip file.</returns>
        public static bool IsZipFile(string fileName)
        {
            return IsZipFile(fileName, false);
        }


        /// <summary>
        /// Checks a to see if a file is a valid zip file.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This method opens the specified zip file, reads in the zip archive, verifying
        /// the ZIP metadata as it reads.  Then, if testExtract is true, this method extracts 
        /// each entry in the archive, dumping all the bits. 
        /// </para>
        /// 
        /// <para>
        /// If everything succeeds, then the method
        /// returns true.  If anything fails - for example if an incorrect signature or CRC
        /// is found, indicating a corrupt file, the the method returns false.  This method also returns false
        /// for a file that does not exist. 
        /// </para>
        ///
        /// <para>
        /// If <c>testExtract</c> is true, this method reads in the content for each
        /// entry, expands it, and checks CRCs.  This provides an additional check
        /// beyond verifying the zip header data.
        /// </para>
        ///
        /// <para>
        /// If <c>testExtract</c> is true, and if any of the zip entries are protected
        /// with a password, this method will return false.  If you want to verify a
        /// ZipFile that has entries which are protected with a password, you will need
        /// to do that manually.
        /// </para>
        /// </remarks>
        /// <param name="fileName">The zip file to check.</param>
        /// <param name="testExtract">true if the caller wants to extract each entry.</param>
        /// <returns>true if the file appears to be a valid zip file.</returns>
        public static bool IsZipFile(string fileName, bool testExtract)
        {
            bool result = false;
            try
            {
                if (!System.IO.File.Exists(fileName)) return false;

                var bitBucket = System.IO.Stream.Null;

                using (ZipFile zip1 = ZipFile.Read(fileName, null, System.Text.Encoding.GetEncoding("IBM437")))
                {
                    if (testExtract)
                    {
                        foreach (var e in zip1)
                        {
                            if (!e.IsDirectory)
                            {
                                e.Extract(bitBucket);
                            }
                        }
                    }
                }
                result = true;
            }
            catch (Exception) { }
            return result;
        }


        /// <summary>
        /// Reads a zip file archive and returns the instance.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The stream is read using the default <c>System.Text.Encoding</c>, which is the <c>IBM437</c> codepage.  
        /// </para>
        /// </remarks>
        /// <exception cref="System.Exception">
        /// Thrown if the ZipFile cannot be read. The implementation of this 
        /// method relies on <c>System.IO.File.OpenRead</c>, which can throw
        /// a variety of exceptions, including specific exceptions if a file
        /// is not found, an unauthorized access exception, exceptions for
        /// poorly formatted filenames, and so on. 
        /// </exception>
        /// 
        /// <param name="zipFileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <overloads>This method has a bunch of interesting overloads. They are all static
        /// (Shared in VB).  One of them is bound to be right for you.  The reason there are so
        /// many is that there are a few properties on the ZipFile class that must be set 
	/// before you read the zipfile in, for them to be useful.  The set of overloads covers
	/// the most interesting cases.  Probably there are still too many, though.</overloads>
        ///
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string zipFileName)
        {
            return ZipFile.Read(zipFileName, null, DefaultEncoding);
        }

        /// <summary>
        /// Reads a zip file archive and returns the instance, using the specified
        /// ReadProgress event handler.  
        /// </summary>
        /// 
        /// <param name="zipFileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string zipFileName, EventHandler<ReadProgressEventArgs> readProgress)
        {
            return ZipFile.Read(zipFileName, null, DefaultEncoding, readProgress);
        }

        /// <summary>
        /// Reads a zip file archive using the specified text encoding, and returns the instance.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This version of the method allows the caller to pass in a <c>TextWriter</c>.  
        /// The ZipFile is read in using the default IBM437 encoding for entries where UTF-8 
        /// encoding is not explicitly specified.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// <code lang="C#">
        /// var sw = new System.IO.StringWriter();
        /// using (ZipFile zip =  ZipFile.Read("PackedDocuments.zip", sw))
        /// {
        ///   var Threshold = new DateTime(2007,7,4);
        ///   // We cannot remove the entry from the list, within the context of 
        ///   // an enumeration of said list.
        ///   // So we add the doomed entry to a list to be removed later.
        ///   // pass 1: mark the entries for removal
        ///   var MarkedEntries = new System.Collections.Generic.List&lt;ZipEntry&gt;();
        ///   foreach (ZipEntry e in zip)
        ///   {
        ///     if (e.LastModified &lt; Threshold)
        ///       MarkedEntries.Add(e);
        ///   }
        ///   // pass 2: actually remove the entry. 
        ///   foreach (ZipEntry zombie in MarkedEntries)
        ///      zip.RemoveEntry(zombie);
        ///   zip.Comment = "This archive has been updated.";
        ///   zip.Save();
        /// }
        /// // can now use contents of sw, eg store in an audit log
        /// </code>
        ///
        /// <code lang="VB">
        ///   Dim sw As New System.IO.StringWriter
        ///   Using zip As ZipFile = ZipFile.Read("PackedDocuments.zip", sw)
        ///       Dim Threshold As New DateTime(2007, 7, 4)
        ///       ' We cannot remove the entry from the list, within the context of 
        ///       ' an enumeration of said list.
        ///       ' So we add the doomed entry to a list to be removed later.
        ///       ' pass 1: mark the entries for removal
        ///       Dim MarkedEntries As New System.Collections.Generic.List(Of ZipEntry)
        ///       Dim e As ZipEntry
        ///       For Each e In zip
        ///           If (e.LastModified &lt; Threshold) Then
        ///               MarkedEntries.Add(e)
        ///           End If
        ///       Next
        ///       ' pass 2: actually remove the entry. 
        ///       Dim zombie As ZipEntry
        ///       For Each zombie In MarkedEntries
        ///           zip.RemoveEntry(zombie)
        ///       Next
        ///       zip.Comment = "This archive has been updated."
        ///       zip.Save
        ///   End Using
        ///   ' can now use contents of sw, eg store in an audit log
        /// </code>
        /// </example>
        /// 
        /// <exception cref="System.Exception">
        /// Thrown if the zipfile cannot be read. The implementation of this 
        /// method relies on <c>System.IO.File.OpenRead</c>, which can throw
        /// a variety of exceptions, including specific exceptions if a file
        /// is not found, an unauthorized access exception, exceptions for
        /// poorly formatted filenames, and so on. 
        /// </exception>
        /// 
        /// <param name="zipFileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to use for writing verbose status messages during operations
        /// on the zip archive.  A console application may wish to pass <c>System.Console.Out</c> to get 
        /// messages on the Console. A graphical or headless application may wish to capture the messages 
        /// in a different <c>TextWriter</c>, such as a <c>System.IO.StringWriter</c>. 
        /// </param>
        /// 
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string zipFileName, System.IO.TextWriter statusMessageWriter)
        {
            return ZipFile.Read(zipFileName, statusMessageWriter, DefaultEncoding);
        }


        /// <summary>
        /// Reads a zip file archive using the specified text encoding, and the
        /// specified ReadProgress event handler, and returns the instance.  
        /// </summary>
        /// 
        /// <param name="zipFileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to use for writing verbose status messages during operations
        /// on the zip archive.  A console application may wish to pass <c>System.Console.Out</c> to get 
        /// messages on the Console. A graphical or headless application may wish to capture the messages 
        /// in a different <c>TextWriter</c>, such as a <c>System.IO.StringWriter</c>. 
        /// </param>
        /// 
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string zipFileName,
                   System.IO.TextWriter statusMessageWriter,
                   EventHandler<ReadProgressEventArgs> readProgress)
        {
            return ZipFile.Read(zipFileName, statusMessageWriter, DefaultEncoding, readProgress);
        }

        /// <summary>
        /// Reads a zip file archive using the specified text encoding, and returns the instance.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This version of the method allows the caller to pass in an <c>Encoding</c>.  
        /// The ZipFile is read in using the specified encoding for entries where UTF-8
        /// encoding is not explicitly specified.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// This example shows how to read a zip file using the Big-5 Chinese code page (950), 
        /// and extract each entry in the zip file.
        /// For this code to work as desired, the zipfile must have been created using the big5
        /// code page (CP950). This is typical, for example, when using WinRar on a machine with
        /// CP950 set as the default code page.  In that case, the names of entries within the Zip
        /// archive will be stored in that code page, and reading the zip archive must be done
        /// using that code page.  If the application did not use the correct code page in
        /// ZipFile.Read(), then names of entries within the zip archive would not be correctly
        /// retrieved.
        /// <code lang="C#">
        /// using (ZipFile zip = ZipFile.Read(ZipToExtract,
        ///                                   System.Text.Encoding.GetEncoding(950)))
        /// {
        ///   foreach (ZipEntry e in zip)
        ///   {
        ///      e.Extract(extractDirectory);
        ///   }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = ZipFile.Read(ZipToExtract, System.Text.Encoding.GetEncoding(950))
        ///     Dim e As ZipEntry
        ///     For Each e In zip
        ///      e.Extract(extractDirectory)
        ///     Next
        /// End Using
        /// </code>
        /// </example>
        ///
        /// <exception cref="System.Exception">
        /// Thrown if the zipfile cannot be read. The implementation of this 
        /// method relies on <c>System.IO.File.OpenRead</c>, which can throw
        /// a variety of exceptions, including specific exceptions if a file
        /// is not found, an unauthorized access exception, exceptions for
        /// poorly formatted filenames, and so on. 
        /// </exception>
        /// 
        /// <param name="zipFileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="encoding">
        /// The <c>System.Text.Encoding</c> to use when reading in the zip archive. Be careful specifying the
        /// encoding.  If the value you use here is not the same as the Encoding used when the zip archive was 
        /// created (possibly by a different archiver) you will get unexpected results and possibly exceptions. 
        /// </param>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.ProvisionalAlternateEncoding">ProvisionalAlternateEncoding</seealso>.
        ///
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string zipFileName, System.Text.Encoding encoding)
        {
            return ZipFile.Read(zipFileName, null, encoding);
        }


        /// <summary>
        /// Reads a zip file archive using the specified text encoding and ReadProgress
        /// event handler, and returns the instance.  
        /// </summary>
        /// 
        /// <param name="zipFileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <param name="encoding">
        /// The <c>System.Text.Encoding</c> to use when reading in the zip archive. Be careful specifying the
        /// encoding.  If the value you use here is not the same as the Encoding used when the zip archive was 
        /// created (possibly by a different archiver) you will get unexpected results and possibly exceptions. 
        /// </param>
        /// 
        /// <returns>The instance read from the zip archive.</returns>
        ///
        public static ZipFile Read(string zipFileName,
                   System.Text.Encoding encoding,
                   EventHandler<ReadProgressEventArgs> readProgress)
        {
            return ZipFile.Read(zipFileName, null, encoding, readProgress);
        }


        /// <summary>
        /// Reads a zip file archive using the specified text encoding and the specified
        /// TextWriter for status messages, and returns the instance.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This version of the method allows the caller to pass in a <c>TextWriter</c> and an <c>Encoding</c>.  
        /// The ZipFile is read in using the specified encoding for entries where UTF-8
        /// encoding is not explicitly specified.
        /// </para>
        /// </remarks>
        /// 
        /// 
        /// <example>
        /// This example shows how to read a zip file using the Big-5 Chinese code page (950), 
        /// and extract each entry in the zip file, while sending status messages out to the Console. 
        /// <code lang="C#">
        /// using (ZipFile zip = ZipFile.Read(ZipToExtract,
        ///                                   System.Console.Out,
        ///                                   System.Text.Encoding.GetEncoding(950)))
        /// {
        ///   foreach (ZipEntry e in zip)
        ///   {
        ///      e.Extract(extractDirectory);
        ///   }
        /// }
        /// </code>
        /// </example>
        ///
        /// <exception cref="System.Exception">
        /// Thrown if the zipfile cannot be read. The implementation of this 
        /// method relies on <c>System.IO.File.OpenRead</c>, which can throw
        /// a variety of exceptions, including specific exceptions if a file
        /// is not found, an unauthorized access exception, exceptions for
        /// poorly formatted filenames, and so on. 
        /// </exception>
        /// 
        /// <param name="zipFileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to use for writing verbose status messages during operations
        /// on the zip archive.  A console application may wish to pass <c>System.Console.Out</c> to get 
        /// messages on the Console. A graphical or headless application may wish to capture the messages 
        /// in a different <c>TextWriter</c>, such as a <c>System.IO.StringWriter</c>. 
        /// </param>
        /// 
        /// <param name="encoding">
        /// The <c>System.Text.Encoding</c> to use when reading in the zip archive. Be careful specifying the
        /// encoding.  If the value you use here is not the same as the Encoding used when the zip archive was 
        /// created (possibly by a different archiver) you will get unexpected results and possibly exceptions.  
        /// </param>
        /// 
        /// <seealso cref="ProvisionalAlternateEncoding"/>
        ///
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string zipFileName,
                   System.IO.TextWriter statusMessageWriter,
                   System.Text.Encoding encoding)
        {
            return Read(zipFileName, statusMessageWriter, encoding, null);
        }

        /// <summary>
        /// Reads a zip file archive using the specified text encoding,  the specified
        /// TextWriter for status messages, and the specified ReadProgress event handler, 
        /// and returns the instance.  
        /// </summary>
        /// 
        /// <param name="zipFileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to use for writing verbose status messages during operations
        /// on the zip archive.  A console application may wish to pass <c>System.Console.Out</c> to get 
        /// messages on the Console. A graphical or headless application may wish to capture the messages 
        /// in a different <c>TextWriter</c>, such as a <c>System.IO.StringWriter</c>. 
        /// </param>
        /// 
        /// <param name="encoding">
        /// The <c>System.Text.Encoding</c> to use when reading in the zip archive. Be careful specifying the
        /// encoding.  If the value you use here is not the same as the Encoding used when the zip archive was 
        /// created (possibly by a different archiver) you will get unexpected results and possibly exceptions. 
        /// </param>
        /// 
        /// <returns>The instance read from the zip archive.</returns>
        ///
        public static ZipFile Read(string zipFileName,
                   System.IO.TextWriter statusMessageWriter,
                   System.Text.Encoding encoding,
                   EventHandler<ReadProgressEventArgs> readProgress)
        {
            ZipFile zf = new ZipFile();
            zf.ProvisionalAlternateEncoding = encoding;
            zf._StatusMessageTextWriter = statusMessageWriter;
            zf._name = zipFileName;
            if (readProgress != null)
                zf.ReadProgress = readProgress;

            try
            {
                ReadIntoInstance(zf);
                zf._fileAlreadyExists = true;
            }
            catch (Exception e1)
            {
                throw new ZipException(String.Format("{0} is not a valid zip file", zipFileName), e1);
            }
            return zf;
        }

        /// <summary>
        /// Reads a zip archive from a stream.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This is useful when when the zip archive content is available from 
        /// an already-open stream. The stream must be open and readable when calling this
        /// method.  The stream is left open when the reading is completed. 
        /// </para>
        /// <para>
        /// The stream is read using the default <c>System.Text.Encoding</c>, which is the <c>IBM437</c> codepage.  
        /// </para>
        /// </remarks>
        ///
        /// <example>
        /// This example shows how to Read zip content from a stream, and extract
        /// one entry into a different stream. In this example, the filename
        /// "NameOfEntryInArchive.doc", refers only to the name of the entry
        /// within the zip archive.  A file by that name is not created in the
        /// filesystem.  The I/O is done strictly with the given streams.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(InputStream))
        /// {
        ///   zip.Extract("NameOfEntryInArchive.doc", OutputStream);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip as ZipFile = ZipFile.Read(InputStream)
        ///   zip.Extract("NameOfEntryInArchive.doc", OutputStream)
        /// End Using
        /// </code>
        /// </example>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        ///
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(System.IO.Stream zipStream)
        {
            return Read(zipStream, null, DefaultEncoding);
        }

        /// <summary>
        /// Reads a zip archive from a stream, with a given ReadProgress event handler.
        /// </summary>
        ///
        /// <remarks>
        /// When opening large zip archives, you may want to display a progress bar or other
        /// indicator of status progress while reading.  This Read() method allows you to specify
        /// a ReadProgress Event Handler directly,
        /// </remarks>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        ///
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile corresponding to the stream being read.</returns>
        public static ZipFile Read(System.IO.Stream zipStream, EventHandler<ReadProgressEventArgs> readProgress)
        {
            return Read(zipStream, null, DefaultEncoding, readProgress);
        }


        /// <summary>
        /// Reads a zip archive from a stream, using the specified TextWriter for status messages.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method is useful when when the zip archive content is available from 
        /// an already-open stream. The stream must be open and readable when calling this
        /// method.  The stream is left open when the reading is completed. 
        /// </para>
        /// 
        /// <para>
        /// The stream is read using the default <c>System.Text.Encoding</c>, which is the <c>IBM437</c> codepage.  
        /// For more information on the encoding, see the <see cref="Ionic.Zip.ZipFile.ProvisionalAlternateEncoding">ProvisionalAlternateEncoding</see> property.
        /// </para>
        /// </remarks>
        /// 
        /// 
        /// <exception cref="Ionic.Zip.ZipException">
        /// Thrown if zipStream is null.
        /// In this case, the inner exception is an ArgumentException.
        /// </exception>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written during
        /// operations on the ZipFile.  For example, in a console application,
        /// System.Console.Out works, and will get a message for each entry added to the
        /// ZipFile.  If the TextWriter is null, no verbose messages are written.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(System.IO.Stream zipStream, System.IO.TextWriter statusMessageWriter)
        {
            return Read(zipStream, statusMessageWriter, DefaultEncoding);
        }


        /// <summary>
        /// Reads a zip archive from a stream, using the specified TextWriter for status messages, 
        /// and the specified ReadProgress event handler.
        /// </summary>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written during
        /// operations on the ZipFile.  For example, in a console application,
        /// System.Console.Out works, and will get a message for each entry added to the
        /// ZipFile.  If the TextWriter is null, no verbose messages are written.
        /// </param>
        /// 
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(System.IO.Stream zipStream,
                   System.IO.TextWriter statusMessageWriter,
                   EventHandler<ReadProgressEventArgs> readProgress)
        {
            return Read(zipStream, statusMessageWriter, DefaultEncoding, readProgress);
        }

        /// <summary>
        /// Reads a zip archive from a stream, using the specified encoding.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method is useful when when the zip archive content is available from 
        /// an already-open stream. The stream must be open and readable when calling this
        /// method.  The stream is left open when the reading is completed. 
        /// </para>
        /// </remarks>
        ///
        /// <exception cref="Ionic.Zip.ZipException">
        /// Thrown if zipStream is null.
        /// In this case, the inner exception is an ArgumentException.
        /// </exception>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        /// 
        /// <param name="encoding">
        /// The text encoding to use when reading entries that do not have the UTF-8 encoding
        /// bit set.  Be careful specifying the encoding.  If the value you use here is not the
        /// same as the Encoding used when the zip archive was created (possibly by a different
        /// archiver) you will get unexpected results and possibly exceptions.  See the <see
        /// cref="Ionic.Zip.ZipFile.ProvisionalAlternateEncoding">ProvisionalAlternateEncoding</see>
        /// property for more information.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(System.IO.Stream zipStream, System.Text.Encoding encoding)
        {
            return Read(zipStream, null, encoding);
        }

        /// <summary>
        /// Reads a zip archive from a stream, using the specified encoding, and
        /// and the specified ReadProgress event handler.
        /// </summary>
        /// 
        /// <param name="zipStream">the stream containing the zip data.</param>
        /// 
        /// <param name="encoding">
        /// The text encoding to use when reading entries that do not have the UTF-8 encoding
        /// bit set.  Be careful specifying the encoding.  If the value you use here is not the
        /// same as the Encoding used when the zip archive was created (possibly by a different
        /// archiver) you will get unexpected results and possibly exceptions.  See the <see
        /// cref="Ionic.Zip.ZipFile.ProvisionalAlternateEncoding">ProvisionalAlternateEncoding</see>
        /// property for more information.
        /// </param>
        /// 
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(System.IO.Stream zipStream, System.Text.Encoding encoding,
                   EventHandler<ReadProgressEventArgs> readProgress)
        {
            return Read(zipStream, null, encoding, readProgress);
        }

        /// <summary>
        /// Reads a zip archive from a stream, using the specified text Encoding and the 
        /// specified TextWriter for status messages.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This method is useful when when the zip archive content is available from 
        /// an already-open stream. The stream must be open and readable when calling this
        /// method.  The stream is left open when the reading is completed. 
        /// </para>
        /// </remarks>
        ///
        /// <exception cref="Ionic.Zip.ZipException">
        /// Thrown if zipStream is null.
        /// In this case, the inner exception is an ArgumentException.
        /// </exception>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        ///
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written during
        /// operations on the ZipFile.  For example, in a console application,
        /// System.Console.Out works, and will get a message for each entry added to the
        /// ZipFile.  If the TextWriter is null, no verbose messages are written.
        /// </param>
        ///
        /// <param name="encoding">
        /// The text encoding to use when reading entries that do not have the UTF-8 encoding
        /// bit set.  Be careful specifying the encoding.  If the value you use here is not the
        /// same as the Encoding used when the zip archive was created (possibly by a different
        /// archiver) you will get unexpected results and possibly exceptions.  See the <see
        /// cref="Ionic.Zip.ZipFile.ProvisionalAlternateEncoding">ProvisionalAlternateEncoding</see>
        /// property for more information.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(System.IO.Stream zipStream,
                   System.IO.TextWriter statusMessageWriter,
                   System.Text.Encoding encoding)
        {
            return Read(zipStream, statusMessageWriter, encoding, null);
        }


        /// <summary>
        /// Reads a zip archive from a stream, using the specified text Encoding, the 
        /// specified TextWriter for status messages, 
        /// and the specified ReadProgress event handler.
        /// </summary>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        ///
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written during
        /// operations on the ZipFile.  For example, in a console application,
        /// System.Console.Out works, and will get a message for each entry added to the
        /// ZipFile.  If the TextWriter is null, no verbose messages are written.
        /// </param>
        ///
        /// <param name="encoding">
        /// The text encoding to use when reading entries that do not have the UTF-8 encoding
        /// bit set.  Be careful specifying the encoding.  If the value you use here is not the
        /// same as the Encoding used when the zip archive was created (possibly by a different
        /// archiver) you will get unexpected results and possibly exceptions.  See the <see
        /// cref="Ionic.Zip.ZipFile.ProvisionalAlternateEncoding">ProvisionalAlternateEncoding</see>
        /// property for more information.
        /// </param>
        /// 
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(System.IO.Stream zipStream,
                   System.IO.TextWriter statusMessageWriter,
                   System.Text.Encoding encoding,
                   EventHandler<ReadProgressEventArgs> readProgress)
        {
            if (zipStream == null)
                throw new ZipException("Cannot read.", new ArgumentException("The stream must be non-null", "zipStream"));

            ZipFile zf = new ZipFile();
            zf._provisionalAlternateEncoding = encoding;
            if (readProgress != null)
                zf.ReadProgress += readProgress;
            zf._StatusMessageTextWriter = statusMessageWriter;
            zf._readstream = zipStream;
            zf._ReadStreamIsOurs = false;
            ReadIntoInstance(zf);
            return zf;
        }


        /// <summary>
        /// Reads a zip archive from a byte array.
        /// </summary>
        /// 
        /// <remarks>
        /// This is useful when the data for the zipfile is contained in a byte array, 
        /// for example, downloaded from an FTP server without being saved to a
        /// filesystem. 
        /// </remarks>
        /// 
        /// <param name="buffer">
        /// The byte array containing the zip data.  
        /// (I don't know why, but sometimes the compiled helpfile (.chm) indicates a 2d 
        /// array when it is just one-dimensional.  This is a one-dimensional array.)
        /// </param>
        /// 
        /// <returns>an instance of ZipFile. The name on the ZipFile will be null (nothing in VB). </returns>
        ///
        /// <seealso cref="ZipFile.Read(System.IO.Stream)" />
        public static ZipFile Read(byte[] buffer)
        {
            return Read(buffer, null, DefaultEncoding);
        }


        /// <summary>
        /// Reads a zip archive from a byte array, using the given StatusMessageWriter.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method is useful when the data for the zipfile is contained in a byte array, for
        /// example when retrieving the data from a database or other non-filesystem store.  
        /// The default Text Encoding (IBM437) is used to read the zipfile data.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="buffer">the byte array containing the zip data.</param>
        ///
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written during
        /// operations on the ZipFile.  For example, in a console application,
        /// System.Console.Out works, and will get a message for each entry added to the
        /// ZipFile.  If the TextWriter is null, no verbose messages are written.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile. The name is set to null.</returns>
        /// 
        public static ZipFile Read(byte[] buffer, System.IO.TextWriter statusMessageWriter)
        {
            return Read(buffer, statusMessageWriter, DefaultEncoding);
        }


        /// <summary>
        /// Reads a zip archive from a byte array, using the given StatusMessageWriter and text Encoding.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method is useful when the data for the zipfile is contained in a byte array, for
        /// example when retrieving the data from a database or other non-filesystem store.  
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="buffer">the byte array containing the zip data.</param>
        ///
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written during
        /// operations on the ZipFile.  For example, in a console application,
        /// System.Console.Out works, and will get a message for each entry added to the
        /// ZipFile.  If the TextWriter is null, no verbose messages are written.
        /// </param>
        /// 
        /// <param name="encoding">
        /// The text encoding to use when reading entries that do not have the UTF-8 encoding
        /// bit set.  Be careful specifying the encoding.  If the value you use here is not the
        /// same as the Encoding used when the zip archive was created (possibly by a different
        /// archiver) you will get unexpected results and possibly exceptions.  See the <see
        /// cref="Ionic.Zip.ZipFile.ProvisionalAlternateEncoding"/>
        /// property for more information.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile. The name is set to null.</returns>
        /// 
        public static ZipFile Read(byte[] buffer, System.IO.TextWriter statusMessageWriter, System.Text.Encoding encoding)
        {
            ZipFile zf = new ZipFile();
            zf._StatusMessageTextWriter = statusMessageWriter;
            zf._provisionalAlternateEncoding = encoding;
            zf._readstream = new System.IO.MemoryStream(buffer);
            zf._ReadStreamIsOurs = true;
            ReadIntoInstance(zf);
            return zf;
        }


        private static void ReadIntoInstance(ZipFile zf)
        {
            System.IO.Stream s = zf.ReadStream;
            try
            {
                if (!s.CanSeek)
                {
                    ReadIntoInstance_Orig(zf);
                    return;
                }

                long origPosn = s.Position;

                // Try reading the central directory, rather than scanning the file. 


                uint datum = VerifyBeginningOfZipFile(s);

                if (datum == ZipConstants.EndOfCentralDirectorySignature)
                    return;


                // start at the end of the file...
                // seek backwards a bit, then look for the EoCD signature. 
                int nTries = 0;
                bool success = false;

                // The size of the end-of-central-directory-footer plus 2 bytes is 18.
                // This implies an archive comment length of 0.
                // We'll add a margin of safety and start "in front" of that, when 
                // looking for the EndOfCentralDirectorySignature
                long posn = s.Length - 64;
                long maxSeekback = Math.Max(s.Length - 0x4000, 10);
                do
                {
                    s.Seek(posn, System.IO.SeekOrigin.Begin);
                    long bytesRead = SharedUtilities.FindSignature(s, (int)ZipConstants.EndOfCentralDirectorySignature);
                    if (bytesRead != -1)
                        success = true;
                    else
                    {
                        nTries++;
                        //weird - with NETCF, negative offsets from SeekOrigin.End DO NOT WORK
                        posn -= (32 * (nTries + 1) * nTries); // increasingly larger
                        if (posn < 0) posn = 0;
                    }
                }
                //while (!success && nTries < 3);
                while (!success && posn > maxSeekback);

                if (success)
                {
                    byte[] block = new byte[16];
                    zf.ReadStream.Read(block, 0, block.Length);
                    int i = 12;

                    uint Offset32 = (uint)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
                    if (Offset32 == 0xFFFFFFFF)
                    {
                        Zip64SeekToCentralDirectory(s);
                    }
                    else
                    {
                        s.Seek(Offset32, System.IO.SeekOrigin.Begin);
                    }

                    ReadCentralDirectory(zf);
                }
                else
                {
                    // Could not find the central directory.
                    // Fallback to the old method.
                    s.Seek(origPosn, System.IO.SeekOrigin.Begin);
                    ReadIntoInstance_Orig(zf);
                }
            }
            catch //(Exception e1)
            {
                if (zf._ReadStreamIsOurs && zf._readstream != null)
                {
                    try
                    {
                        zf._readstream.Close();
#if !NETCF20
                        zf._readstream.Dispose();
#endif
                        zf._readstream = null;
                    }
                    finally { }
                }

                throw; // new Ionic.Utils.Zip.ZipException("Exception while reading", e1);
            }

            // the instance has been read in
            zf._contentsChanged = false;
        }



        private static void Zip64SeekToCentralDirectory(System.IO.Stream s)
        {
            byte[] block = new byte[16];

            // seek back to find the ZIP64 EoCD
            s.Seek(-40, System.IO.SeekOrigin.Current);
            s.Read(block, 0, 16);

            Int64 Offset64 = BitConverter.ToInt64(block, 8);
            s.Seek(Offset64, System.IO.SeekOrigin.Begin);

            uint datum = (uint)Ionic.Zip.SharedUtilities.ReadInt(s);
            if (datum != ZipConstants.Zip64EndOfCentralDirectoryRecordSignature)
                throw new BadReadException(String.Format("  ZipFile::Read(): Bad signature (0x{0:X8}) looking for ZIP64 EoCD Record at position 0x{1:X8}", datum, s.Position));

            s.Read(block, 0, 8);
            Int64 Size = BitConverter.ToInt64(block, 0);

            block = new byte[Size];
            s.Read(block, 0, block.Length);

            Offset64 = BitConverter.ToInt64(block, 36);
            s.Seek(Offset64, System.IO.SeekOrigin.Begin);
        }


        private static uint VerifyBeginningOfZipFile(System.IO.Stream s)
        {
            uint datum = (uint)Ionic.Zip.SharedUtilities.ReadInt(s);
            if (datum != ZipConstants.PackedToRemovableMedia  // weird edge case
                && datum != ZipConstants.ZipEntrySignature   // normal BOF marker
                && datum != ZipConstants.EndOfCentralDirectorySignature  // for zip file with no entries
                && (datum & 0x0000FFFF) != 0x00005A4D // PE/COFF BOF marker (for SFX)
                )
            {
                //Console.WriteLine("WTF, datum = 0x{0:X8}", datum);
                throw new BadReadException(String.Format("  ZipFile::Read(): Bad signature (0x{0:X8}) at start of file at position 0x{1:X8}", datum, s.Position));
            }
            return datum;
        }



        private static void ReadCentralDirectory(ZipFile zf)
        {
            //zf._direntries = new System.Collections.Generic.List<ZipDirEntry>();
            zf._entries = new System.Collections.Generic.List<ZipEntry>();

            ZipEntry de;
            while ((de = ZipEntry.ReadDirEntry(zf.ReadStream, zf.ProvisionalAlternateEncoding)) != null)
            {
                //zf._direntries.Add(de);

                de.ResetDirEntry();
                de._zipfile = zf;
                de._archiveStream = zf.ReadStream;
                zf.OnReadEntry(true, null);

                if (zf.Verbose)
                    zf.StatusMessageTextWriter.WriteLine("  {0}", de.FileName);

                zf._entries.Add(de);
            }

            ReadCentralDirectoryFooter(zf);

            if (zf.Verbose && !String.IsNullOrEmpty(zf.Comment))
                zf.StatusMessageTextWriter.WriteLine("Zip file Comment: {0}", zf.Comment);

            // when finished slurping in the zip, close the read stream
            //zf.ReadStream.Close();

            zf.OnReadCompleted();
        }

        // build the TOC by reading each entry in the file.
        private static void ReadIntoInstance_Orig(ZipFile zf)
        {
            zf.OnReadStarted();
            zf._entries = new System.Collections.Generic.List<ZipEntry>();
            ZipEntry e;
            if (zf.Verbose)
                if (zf.Name == null)
                    zf.StatusMessageTextWriter.WriteLine("Reading zip from stream...");
                else
                    zf.StatusMessageTextWriter.WriteLine("Reading zip {0}...", zf.Name);

            // work item 6647:  PK00 (packed to removable disk)
            bool firstEntry = true;
            while ((e = ZipEntry.Read(zf, firstEntry)) != null)
            {
                if (zf.Verbose)
                    zf.StatusMessageTextWriter.WriteLine("  {0}", e.FileName);

                zf._entries.Add(e);
                firstEntry = false;
            }

            // read the zipfile's central directory structure here.
            //zf._direntries = new System.Collections.Generic.List<ZipDirEntry>();

            ZipEntry de;
            while ((de = ZipEntry.ReadDirEntry(zf.ReadStream, zf.ProvisionalAlternateEncoding)) != null)
            {
                //zf._direntries.Add(de);
                // Housekeeping: Since ZipFile exposes ZipEntry elements in the enumerator, 
                // we need to copy the comment that we grab from the ZipDirEntry
                // into the ZipEntry, so the application can access the comment. 
                // Also since ZipEntry is used to Write zip files, we need to copy the 
                // file attributes to the ZipEntry as appropriate. 
                foreach (ZipEntry e1 in zf._entries)
                {
                    if (e1.FileName == de.FileName)
                    {
                        e1._Comment = de.Comment;
                        if (de.AttributesIndicateDirectory) e1.MarkAsDirectory();
                        break;
                    }
                }
            }

            ReadCentralDirectoryFooter(zf);

            if (zf.Verbose && !String.IsNullOrEmpty(zf.Comment))
                zf.StatusMessageTextWriter.WriteLine("Zip file Comment: {0}", zf.Comment);

            // when finished slurping in the zip, close the read stream
            //zf.ReadStream.Close();

            zf.OnReadCompleted();

        }




        private static void ReadCentralDirectoryFooter(ZipFile zf)
        {
            System.IO.Stream s = zf.ReadStream;
            int signature = Ionic.Zip.SharedUtilities.ReadSignature(s);

            byte[] block = null;
            int i = 0;
            if (signature == ZipConstants.Zip64EndOfCentralDirectoryRecordSignature)
            {
                // We have a ZIP64 EOCD
                // This data block is 4 bytes sig, 8 bytes size, 44 bytes fixed data, 
                // followed by a variable-sized extension block.  We have read the sig already. 
                block = new byte[8 + 44];
                s.Read(block, 0, block.Length);

                Int64 DataSize = BitConverter.ToInt64(block, 0);  // == 44 + the variable length

                if (DataSize < 44)
                    throw new ZipException("Bad DataSize in the ZIP64 Central Directory.");

                i = 8;
                i += 2; // version made by
                i += 2; // version needed to extract

                i += 4; // number of this disk
                i += 4; // number of the disk with the start of the CD

                i += 8; // total number of entries in the CD on this disk
                i += 8; // total number of entries in the CD 

                i += 8; // size of the CD

                i += 8; // offset of the CD

                block = new byte[DataSize - 44];
                s.Read(block, 0, block.Length);
                // discard the result

                signature = Ionic.Zip.SharedUtilities.ReadSignature(s);
                if (signature != ZipConstants.Zip64EndOfCentralDirectoryLocatorSignature)
                    throw new ZipException("Inconsistent metadata in the ZIP64 Central Directory.");

                block = new byte[16];
                s.Read(block, 0, block.Length);
                // discard the result

                signature = Ionic.Zip.SharedUtilities.ReadSignature(s);
            }

            // Throw if this is not a signature for "end of central directory record"
            // This is a sanity check.
            if (signature != ZipConstants.EndOfCentralDirectorySignature)
            {
                s.Seek(-4, System.IO.SeekOrigin.Current);
                throw new BadReadException(String.Format("  ZipFile::Read(): Bad signature ({0:X8}) at position 0x{1:X8}", signature, s.Position));
            }

            // read a bunch of metadata for supporting multi-disk archives, which this library does not do.
            block = new byte[16];
            zf.ReadStream.Read(block, 0, block.Length); // discard result

            // read the comment here
            ReadZipFileComment(zf);
        }



        private static void ReadZipFileComment(ZipFile zf)
        {
            // read the comment here
            byte[] block = new byte[2];
            zf.ReadStream.Read(block, 0, block.Length);

            Int16 commentLength = (short)(block[0] + block[1] * 256);
            if (commentLength > 0)
            {
                block = new byte[commentLength];
                zf.ReadStream.Read(block, 0, block.Length);

                // workitem 6513 - only use UTF8 as necessary
                // test reflexivity
                string s1 = DefaultEncoding.GetString(block, 0, block.Length);
                byte[] b2 = DefaultEncoding.GetBytes(s1);
                if (BlocksAreEqual(block, b2))
                {
                    zf.Comment = s1;
                }
                else
                {
                    // need alternate (non IBM437) encoding
                    // workitem 6415
                    // use UTF8 if the caller hasn't already set a non-default encoding
                    System.Text.Encoding e = (zf._provisionalAlternateEncoding.CodePage == 437)
                        ? System.Text.Encoding.UTF8
                        : zf._provisionalAlternateEncoding;
                    zf.Comment = e.GetString(block, 0, block.Length);
                }
            }
        }


        private static bool BlocksAreEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }


        /// <summary>
        /// Generic IEnumerator support, for use of a ZipFile in a foreach construct.  
        /// </summary>
        ///
        /// <remarks>
        /// You probably do not want to call <c>GetEnumerator</c> explicitly. Instead 
        /// it is implicitly called when you use a <c>foreach</c> loop in C#, or a 
        /// <c>For Each</c> loop in VB.
        /// </remarks>
        ///
        /// <example>
        /// This example reads a zipfile of a given name, then enumerates the 
        /// entries in that zip file, and displays the information about each 
        /// entry on the Console.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(zipfile))
        /// {
        ///   bool header = true;
        ///   foreach (ZipEntry e in zip)
        ///   {
        ///     if (header)
        ///     {
        ///        System.Console.WriteLine("Zipfile: {0}", zip.Name);
        ///        System.Console.WriteLine("Version Needed: 0x{0:X2}", e.VersionNeeded);
        ///        System.Console.WriteLine("BitField: 0x{0:X2}", e.BitField);
        ///        System.Console.WriteLine("Compression Method: 0x{0:X2}", e.CompressionMethod);
        ///        System.Console.WriteLine("\n{1,-22} {2,-6} {3,4}   {4,-8}  {0}",
        ///                     "Filename", "Modified", "Size", "Ratio", "Packed");
        ///        System.Console.WriteLine(new System.String('-', 72));
        ///        header = false;
        ///     }
        ///
        ///     System.Console.WriteLine("{1,-22} {2,-6} {3,4:F0}%   {4,-8}  {0}",
        ///                 e.FileName,
        ///                 e.LastModified.ToString("yyyy-MM-dd HH:mm:ss"),
        ///                 e.UncompressedSize,
        ///                 e.CompressionRatio,
        ///                 e.CompressedSize);
        ///
        ///     e.Extract();
        ///   }
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        ///   Dim ZipFileToExtract As String = "c:\foo.zip"
        ///   Using zip As ZipFile = ZipFile.Read(ZipFileToExtract)
        ///       Dim header As Boolean = True
        ///       Dim e As ZipEntry
        ///       For Each e In zip
        ///           If header Then
        ///               Console.WriteLine("Zipfile: {0}", zip.Name)
        ///               Console.WriteLine("Version Needed: 0x{0:X2}", e.VersionNeeded)
        ///               Console.WriteLine("BitField: 0x{0:X2}", e.BitField)
        ///               Console.WriteLine("Compression Method: 0x{0:X2}", e.CompressionMethod)
        ///               Console.WriteLine(ChrW(10) &amp; "{1,-22} {2,-6} {3,4}   {4,-8}  {0}", _
        ///                 "Filename", "Modified", "Size", "Ratio", "Packed" )
        ///               Console.WriteLine(New String("-"c, 72))
        ///               header = False
        ///           End If
        ///           Console.WriteLine("{1,-22} {2,-6} {3,4:F0}%   {4,-8}  {0}", _
        ///             e.FileName, _
        ///             e.LastModified.ToString("yyyy-MM-dd HH:mm:ss"), _
        ///             e.UncompressedSize, _
        ///             e.CompressionRatio, _
        ///             e.CompressedSize )
        ///           e.Extract
        ///       Next
        ///   End Using
        /// </code>
        /// </example>
        /// 
        /// <returns>A generic enumerator suitable for use  within a foreach loop.</returns>
        public System.Collections.Generic.IEnumerator<ZipEntry> GetEnumerator()
        {
            foreach (ZipEntry e in _entries)
                yield return e;
        }

        /// <summary>
        /// IEnumerator support, for use of a ZipFile in a foreach construct.  
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Extracts all of the items in the zip archive, to the specified path in the filesystem.
        /// The path can be relative or fully-qualified. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This method will extract all entries in the ZipFile to the specified path. 
        /// </para>
        ///
        /// <para>
        /// If an extraction of a file from the zip archive would overwrite an existing file in
        /// the filesystem, the action taken is dictated by the ExtractExistingFile property,
        /// which overrides any setting you may have made on individual ZipEntry instances.  By
        /// default, if you have not set that property on the ZipFile instance, the entry will not
        /// be extracted, the existing file will not be overwritten and an exception will be
        /// thrown. To change this, set the property, or use the <see
        /// cref="ZipFile.ExtractAll(System.String, Ionic.Zip.ExtractExistingFileAction)" /> overload
        /// that allows you to specify an ExtractExistingFileAction parameter.
        /// </para>
        ///
        /// <para>
        /// The action to take when an extract would overwrite an existing file applies to all
        /// entries.  If you want to set this on a per-entry basis, then you must use one of the
        /// <see cef="ZipEntry.Extract" /> methods.
        /// </para>
        ///
        /// <para>
        /// This method will send verbose output messages to the StatusMessageTextWriter, if it 
        /// is set on the ZipFile instance. 
        /// </para>
        ///
        /// <para>
        /// You may wish to take advantage of the <c>ExtractProgress</c> event.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <example>
        /// This example extracts all the entries in a zip archive file, to the specified target
        /// directory.  The extraction will overwrite any existing files silently.
        /// <code>
        /// String TargetDirectory= "unpack";
        /// using(ZipFile zip= ZipFile.Read(ZipFileToExtract))
        /// {
        ///     zip.ExtractExistingFile= ExtractExistingFileAction.OverwriteSilently;
        ///     zip.ExtractAll(TargetDirectory);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim TargetDirectory As String = "unpack"
        /// Using zip As ZipFile = ZipFile.Read(ZipFileToExtract)
        ///     zip.ExtractExistingFile= ExtractExistingFileAction.OverwriteSilently
        ///     zip.ExtractAll(TargetDirectory)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.ExtractProgress"/>
        /// <seealso cref="Ionic.Zip.ZipFile.ExtractExistingFile"/>
        ///
        /// <param name="path">
        /// The path to which the contents of the zipfile will be extracted.
        /// The path can be relative or fully-qualified. 
        /// </param>
        ///
        public void ExtractAll(string path)
        {
            _InternalExtractAll(path, true);
        }

        /// <summary>
        /// Extracts all of the items in the zip archive, to the specified path in the filesystem,  
        /// optionally overwriting any existing files. The path can be relative or fully-qualified. 
        /// </summary>
        ///
        /// <remarks>
        /// This method will send verbose output messages to the StatusMessageTextWriter, if it 
        /// is set on the ZipFile instance. 
        /// </remarks>
        ///
        /// <example>
        /// This example extracts all the entries in a zip archive file, 
        /// to the specified target directory.  It overwrites any existing files.
        /// <code>
        /// String TargetDirectory= "unpack";
        /// using(ZipFile zip= ZipFile.Read(ZipFileToExtract))
        /// {
        ///     zip.ExtractAll(TargetDirectory, true);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim TargetDirectory As String = "unpack"
        /// Using zip As ZipFile = ZipFile.Read(ZipFileToExtract)
        ///     zip.ExtractAll(TargetDirectory, True)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="path">the path to which the contents of the zipfile are extracted.</param>
        /// <param name="wantOverwrite">true to overwrite any existing files on extraction</param>
        /// <seealso cref="ExtractAll(String,ExtractExistingFileAction)"/>
        [Obsolete("Please use property ExtractExistingFile to specify overwrite behavior)")]
        public void ExtractAll(string path, bool wantOverwrite)
        {
            // legacy behavior
            ExtractExistingFile = (wantOverwrite)
            ? ExtractExistingFileAction.OverwriteSilently
            : ExtractExistingFileAction.Throw;

            _InternalExtractAll(path, true);
        }


        /// <summary>
        /// Extracts all of the items in the zip archive, to the specified path in the filesystem,  
        /// using the specified behavior when extraction would overwrite an existing file.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para>
        /// This method will extract all entries in the ZipFile to the specified path. 
        /// For an extraction that would overwrite an existing file, the behavior 
        /// is dictated by the extractExistingFile parameter, which overrides 
        /// any setting you may have made on individual ZipEntry instances. 
        /// </para>
        ///
        /// <para>
        /// The action to take when an extract would overwrite an existing file applies to all
        /// entries.  If you want to set this on a per-entry basis, then you must use one of the
        /// <see cef="ZipEntry.Extract" /> methods.
        /// </para>
        ///
        /// <para>
        /// Calling this method is equivalent to setting the <see cref="ExtractExistingFile"/>
        /// property and then calling <see cref="ExtractAll(String)"/>.
        /// </para>
	///
        /// <para>
        /// This method will send verbose output messages to the StatusMessageTextWriter, if it 
        /// is set on the ZipFile instance. 
        /// </para>
        /// </remarks>
        ///
        /// <example>
        /// This example extracts all the entries in a zip archive file, 
        /// to the specified target directory.  It does not overwrite any existing files.
        /// <code>
        /// String TargetDirectory= "c:\\unpack";
        /// using(ZipFile zip= ZipFile.Read(ZipFileToExtract))
        /// {
        ///   zip.ExtractAll(TargetDirectory, ExtractExistingFileAction.DontOverwrite);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim TargetDirectory As String = "c:\unpack"
        /// Using zip As ZipFile = ZipFile.Read(ZipFileToExtract)
        ///     zip.ExtractAll(TargetDirectory, ExtractExistingFileAction.DontOverwrite)
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="path">
        /// The path to which the contents of the zipfile will be extracted.
        /// The path can be relative or fully-qualified. 
        /// </param>
        ///
        /// <param name="extractExistingFile">
        /// The action to take if extraction would overwrite an existing file.
        /// </param>
        /// <seealso cref="ExtractSelectedEntries(String,ExtractExistingFileAction)"/>
        public void ExtractAll(string path, ExtractExistingFileAction extractExistingFile)
        {
            ExtractExistingFile = extractExistingFile;
            _InternalExtractAll(path, true);
        }


        private void _InternalExtractAll(string path, bool overrideExtractExistingProperty)
        {
            bool header = Verbose;
            _inExtractAll = true;
            try
            {
                OnExtractAllStarted(path);

                int n = 0;
                foreach (ZipEntry e in _entries)
                {
                    if (header)
                    {
                        StatusMessageTextWriter.WriteLine("\n{1,-22} {2,-8} {3,4}   {4,-8}  {0}",
                                  "Name", "Modified", "Size", "Ratio", "Packed");
                        StatusMessageTextWriter.WriteLine(new System.String('-', 72));
                        header = false;
                    }
                    if (Verbose)
                    {
                        StatusMessageTextWriter.WriteLine("{1,-22} {2,-8} {3,4:F0}%   {4,-8} {0}",
                                  e.FileName,
                                  e.LastModified.ToString("yyyy-MM-dd HH:mm:ss"),
                                  e.UncompressedSize,
                                  e.CompressionRatio,
                                  e.CompressedSize);
                        if (!String.IsNullOrEmpty(e.Comment))
                            StatusMessageTextWriter.WriteLine("  Comment: {0}", e.Comment);
                    }
                    e.Password = _Password;  // this may be null
                    OnExtractEntry(n, true, e, path);
                    if (overrideExtractExistingProperty)
                        e.ExtractExistingFile = this.ExtractExistingFile;
                    e.Extract(path);
                    n++;
                    OnExtractEntry(n, false, e, path);
                    if (_extractOperationCanceled)
                        break;

                }

                OnExtractAllCompleted(path);
            }
            finally
            {

                _inExtractAll = false;
            }
        }


        /// <summary>
        /// Extract a single item from the archive to the current working directory.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// A file corresponding to the entry named by the fileName parameter, including any
        /// relative qualifying path for the entry, is created at the specified directory.
        /// </para>
        ///
        /// <para>
        /// An entry that requires a password for extraction is extracted using the <see
        /// cref="ZipFile.Password"/> that is specified on the ZipFile instance, which overrides
        /// any Password you may have set directly on the ZipEntry instance. If you have not set
        /// the <see cref="ZipFile.Password"/> property, or if you have set it to null, and the
        /// entry requires a password for extraction, an Exception will be thrown.  An exception
        /// will also be thrown if the entry requires a password for extraction, and the password
        /// specified on the ZipFile instance does not match that required for the ZipEntry.
        /// </para>
        ///
        /// <para>
        /// For an extraction that would overwrite an existing file, the action taken is dictated
        /// by the <see cref="ZipFile.ExtractExistingFile" /> property, which overrides any
        /// setting you may have made on the individual ZipEntry instance, unless it is not the
        /// default "Throw" action.  If it is the default "Throw", then the action taken is that
        /// specified in the <see cref="ZipEntry.ExtractExistingFile" /> property on the ZipEntry
        /// instance.
        /// </para>
        ///
        /// <para>
        /// The file, including any relative qualifying path, is extracted to the current working
        /// directory.
        /// </para>
        ///
        /// <para>
        /// The ExtractProgress event is invoked before and after extraction, if it has been set. 
        /// </para>
        /// </remarks>
        /// 
        /// <param name="fileName">
        /// The file to extract. It must be the exact filename, including the path
        /// contained in the archive, if any. The filename match is not case-sensitive by
        /// default; you can use the <c>CaseSensitiveRetrieval</c> property to change
        /// this behavior.
        /// </param>
        public void Extract(string fileName)
        {
            ZipEntry e = this[fileName];
            if (this.ExtractExistingFile != ExtractExistingFileAction.Throw)
                e.ExtractExistingFile = this.ExtractExistingFile;
            e.Password = _Password; // possibly null
            e.Extract();
        }

        /// <summary>
        /// Extract a single item from the archive to the specified directory.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// A file corresponding to the entry named by the fileName parameter, including any
        /// relative qualifying path for the entry, is created at the specified directory.
        /// </para>
        ///
        /// <para>
        /// An entry that requires a password for extraction is extracted using the <see
        /// cref="ZipFile.Password"/> that is specified on the ZipFile instance, which overrides
        /// any Password you may have set directly on the ZipEntry instance. If you have not set
        /// the <see cref="ZipFile.Password"/> property, or if you have set it to null, and the
        /// entry requires a password for extraction, an Exception will be thrown.  An exception
        /// will also be thrown if the entry requires a password for extraction, and the password
        /// specified on the ZipFile instance does not match that required for the ZipEntry.
        /// </para>
        ///
        /// <para>
        /// For an extraction that would overwrite an existing file, the action taken is dictated
        /// by the <see cref="ZipFile.ExtractExistingFile" /> property, which overrides any
        /// setting you may have made on the individual ZipEntry instance, unless it is not the
        /// default "Throw" action.  If it is the default "Throw", then the action taken is that
        /// specified in the <see cref="ZipEntry.ExtractExistingFile" /> property on the ZipEntry
        /// instance.
        /// </para>
        ///
        /// <para>
        /// The ExtractProgress event is invoked before and after extraction, if it has been set. 
        /// </para>
        /// </remarks>
        /// 
        /// <param name="entryName">
        /// the name of the entry to extract. It must be the exact filename, including the path
        /// specified on the entry in the archive, if any. The match is not case-sensitive by
        /// default; you can use the <c>CaseSensitiveRetrieval</c> property to change
        /// this behavior.
        /// </param>
        /// <param name="directoryName">the directory into which to extract. It will be created 
        /// if it does not exist.</param>
        public void Extract(string entryName, string directoryName)
        {
            ZipEntry e = this[entryName];
            if (this.ExtractExistingFile != ExtractExistingFileAction.Throw)
                e.ExtractExistingFile = this.ExtractExistingFile;
            e.Password = _Password; // possibly null
            e.Extract(directoryName);
        }



        /// <summary>
        /// Extract a single item from the archive to the current working directory, potentially
        /// overwriting any existing file in the filesystem by the same name.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// A file corresponding to the entry named by the fileName parameter, including any
        /// relative qualifying path for the entry, is created at the current working directory.
        /// </para>
        ///
        /// <para>
        /// An entry that requires a password for extraction is extracted using the <see
        /// cref="ZipFile.Password"/> that is specified on the ZipFile instance, which overrides
        /// any Password you may have set directly on the ZipEntry instance. If you have not set
        /// the <see cref="ZipFile.Password"/> property, or if you have set it to null, and the
        /// entry requires a password for extraction, an Exception will be thrown.  An exception
        /// will also be thrown if the entry requires a password for extraction, and the password
        /// specified on the ZipFile instance does not match that required for the ZipEntry.
        /// </para>
        ///
        /// <para>
        /// The ExtractProgress event is invoked before and after extraction, if it has been set. 
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.CaseSensitiveRetrieval"/>
        /// <seealso cref="Ionic.Zip.ZipFile.Extract(String,ExtractExistingFileAction)"/>
        ///
        /// <param name="entryName">
        /// The name of the entry to extract. It must be the exact name, including the path
        /// specified on the entry in the archive, if any. The match is not case-sensitive by
        /// default; you can use the <c>CaseSensitiveRetrieval</c> property to change this
        /// behavior.  The path, if any, can use forward-slashes or backward slashes.
        /// </param>
        ///
        /// <param name="wantOverwrite">
        /// True if the caller wants to overwrite any existing files by the given name.
        /// </param>
        [Obsolete("Please use method Extract(String,ExtractExistingFileAction)")]
        public void Extract(string entryName, bool wantOverwrite)
        {
            ZipEntry e = this[entryName];
            // legacy behavior
            e.ExtractExistingFile = (wantOverwrite)
            ? ExtractExistingFileAction.OverwriteSilently
            : ExtractExistingFileAction.Throw;
            e.Password = _Password; // possibly null
            e.Extract(System.IO.Directory.GetCurrentDirectory());
        }



        /// <summary>
        /// Extract a single item from the archive to the current working directory, potentially
        /// overwriting any existing file in the filesystem by the same name.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Using this method, the entry is extracted using the Password that is specified on
        /// the ZipFile instance. If you have not set the Password property, then the password
        /// is null, and the entry is extracted with no password.  The file, including any
        /// relative qualifying path, is created at the current working directory.
        /// </para>
        ///
        /// <para>
        /// For an extraction that would overwrite an existing file, the action taken is dictated
        /// by the extractExistingFile parameter, which overrides any setting you may have made on
        /// the individual ZipEntry instance.  To avoid this, use one of the
        /// <c>ZipEntry.Extract</c> methods.
        /// </para>
        ///
        /// <para>
        /// The ExtractProgress event is invoked before and after extraction, if it has been set. 
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.CaseSensitiveRetrieval"/>
        ///
        /// <param name="entryName">
        /// The name of the entry to extract. It must be the exact name, including the path
        /// specified on the entry in the archive, if any. The match is not case-sensitive by
        /// default; you can use the <c>CaseSensitiveRetrieval</c> property to change this
        /// behavior.  The path, if any, can use forward-slashes or backward slashes.
        /// </param>
        ///
        /// <param name="extractExistingFile">
        /// The action to take if extraction would overwrite an existing file.
        /// </param>
        public void Extract(string entryName, ExtractExistingFileAction extractExistingFile)
        {
            ZipEntry e = this[entryName];
            e.ExtractExistingFile = extractExistingFile;
            e.Password = _Password; // possibly null
            e.Extract(System.IO.Directory.GetCurrentDirectory());
        }


        /// <summary>
        /// Extract a single item from the archive, into the specified directory, potentially overwriting  
        /// any existing file in the filesystem by the same name.   
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// A file corresponding to the entry named by the fileName parameter, including any
        /// relative qualifying path for the entry, is created at the specified directory.
        /// </para>
        ///
        /// <para>
        /// An entry that requires a password for extraction is extracted using the <see
        /// cref="ZipFile.Password"/> that is specified on the ZipFile instance, which overrides
        /// any Password you may have set directly on the ZipEntry instance. If you have not set
        /// the <see cref="ZipFile.Password"/> property, or if you have set it to null, and the
        /// entry requires a password for extraction, an Exception will be thrown.  An exception
        /// will also be thrown if the entry requires a password for extraction, and the password
        /// specified on the ZipFile instance does not match that required for the ZipEntry.
        /// </para>
        ///
        /// <para>
        /// The ExtractProgress event is invoked before and after extraction, if it has been set. 
        /// </para>
        /// </remarks>
        /// 
        /// <seealso 
        /// cref="Ionic.Zip.ZipFile.Extract(String, String, ExtractExistingFileAction)"/>
        /// 
        /// <param name="entryName">
        /// The name of the entry to extract. It must be the exact name, including the path
        /// specified on the entry in the archive, if any. The match is not case-sensitive by
        /// default; you can use the <c>CaseSensitiveRetrieval</c> property to change this
        /// behavior. The path, if any, can use forward-slashes or backward slashes.
        /// </param>
        /// 
        /// <param name="directoryName">
        /// The directory into which to extract. It will be created 
        /// if it does not exist.
        /// </param>
        /// 
        /// <param name="wantOverwrite">
        /// True if the caller wants to overwrite any existing files 
        /// by the given name. 
        /// </param>
        [Obsolete("Please use method Extract(String,String,ExtractExistingFileAction)")]
        public void Extract(string entryName, string directoryName, bool wantOverwrite)
        {
            ZipEntry e = this[entryName];
            e.Password = _Password; // possibly null
            // legacy behavior
            e.ExtractExistingFile = (wantOverwrite)
            ? ExtractExistingFileAction.OverwriteSilently
            : ExtractExistingFileAction.Throw;
            e.Extract(directoryName);
        }


        /// <summary>
        /// Extract a single item from the archive, into the specified directory, 
        /// using the specified behavior when extraction would overwrite an existing file.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// A file corresponding to the entry named by the fileName parameter, including any
        /// relative qualifying path for the entry, is created at the specified directory.
        /// </para>
        ///
        /// <para>
        /// An entry that requires a password for extraction is extracted using the <see
        /// cref="ZipFile.Password"/> that is specified on the ZipFile instance, which overrides
        /// any Password you may have set directly on the ZipEntry instance. If you have not set
        /// the <see cref="ZipFile.Password"/> property, or if you have set it to null, and the
        /// entry requires a password for extraction, an Exception will be thrown.  An exception
        /// will also be thrown if the entry requires a password for extraction, and the password
        /// specified on the ZipFile instance does not match that required for the ZipEntry.
        /// </para>
        ///
        /// <para>
        /// For an extraction that would overwrite an existing file, the action taken is dictated
        /// by the extractExistingFile parameter, which overrides any setting you may have made on
        /// the individual ZipEntry instance.  To avoid this, use one of the
        /// <c>ZipEntry.Extract</c> methods.
        /// </para>
        ///
        /// <para>
        /// The ExtractProgress event is invoked before and after extraction, if it has been set. 
        /// </para>
        /// </remarks>
        /// 
        /// <param name="entryName">
        /// The name of the entry to extract. It must be the exact name, including the path
        /// specified on the entry in the archive, if any. The match is not case-sensitive by
        /// default; you can use the <c>CaseSensitiveRetrieval</c> property to change this
        /// behavior. The path, if any, can use forward-slashes or backward slashes.
        /// </param>
        /// 
        /// <param name="directoryName">
        /// The directory into which to extract. It will be created if it does not exist.
        /// </param>
        ///
        /// <param name="extractExistingFile">
        /// The action to take if extraction would overwrite an existing file.
        /// </param>
        public void Extract(string entryName, string directoryName, ExtractExistingFileAction extractExistingFile)
        {
            ZipEntry e = this[entryName];
            e.ExtractExistingFile = extractExistingFile;
            e.Password = _Password; // possibly null
            e.Extract(directoryName);
        }



        /// <summary>
        /// Extract a single specified file from the archive, to the given stream.   
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The entry identified by the entryName parameter is extracted to the given stream.
        /// </para>
        ///
        /// <para>
        /// An entry that requires a password for extraction is extracted using the <see
        /// cref="ZipFile.Password"/> that is specified on the ZipFile instance, which overrides
        /// any Password you may have set directly on the ZipEntry instance. If you have not set
        /// the <see cref="ZipFile.Password"/> property, or if you have set it to null, and the
        /// entry requires a password for extraction, an Exception will be thrown.  An exception
        /// will also be thrown if the entry requires a password for extraction, and the password
        /// specified on the ZipFile instance does not match that required for the ZipEntry.
        /// </para>
        ///
        /// <para>
        /// The ExtractProgress event is invoked before and after extraction, if it has been set. 
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="Ionic.Zip.ZipException">
        /// Thrown if the outputStream is not writable, or if the filename is 
        /// null or empty. The inner exception is an ArgumentException in each case.
        /// </exception>
        ///
        /// <param name="entryName">
        /// the name of the entry to extract, including the path used in the archive, if
        /// any.  The match is not case-sensitive by default; you can use the
        /// <c>CaseSensitiveRetrieval</c> property to change this behavior. The application can
        /// specify pathnames using forward-slashes or backward slashes.
        /// </param>
        ///
        /// <param name="outputStream">
        /// the stream to which the extacted, decompressed file data is written. 
        /// The stream must be writable.
        /// </param>
        public void Extract(string entryName, System.IO.Stream outputStream)
        {
            if (outputStream == null || !outputStream.CanWrite)
                throw new ZipException("Cannot extract.", new ArgumentException("The OutputStream must be a writable stream.", "outputStream"));

            if (String.IsNullOrEmpty(entryName))
                throw new ZipException("Cannot extract.", new ArgumentException("The file name must be neither null nor empty.", "entryName"));

            ZipEntry e = this[entryName];
            e.Password = _Password; // possibly null
            e.Extract(outputStream);
        }


        /// <summary>
        /// This is an integer indexer into the Zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This property is read-write. But don't get too excited: When setting the value, the
        /// only legal value is null. If you assign a non-null value
        /// (non Nothing in VB), the setter will throw an exception.
        /// </para>
        ///
        /// <para>
        /// Setting the value to null is equivalent to calling <see cref="ZipFile.RemoveEntry(String)"/>
        /// with the filename for the given entry.
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
        /// The ZipEntry within the Zip archive at the specified index. If the 
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
                    throw new ArgumentException("You may not set this to a non-null ZipEntry value.");
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
        /// by default.  Set the <see cref="CaseSensitiveRetrieval"/> property to use case-sensitive 
        /// comparisons. 
        /// </para>
        ///
        /// <para>
        /// This property is read-write. When setting the value, the only legal value is
        /// null. Setting the value to null is equivalent to calling <see
        /// cref="ZipFile.RemoveEntry(String)"/> with the filename.
        /// </para>
        ///
        /// <para>
        /// If you assign a non-null value (non Nothing in VB), the setter will throw an
        /// exception.
        /// </para>
        ///
        /// <para>
        /// It is can be true that <c>this[value].FileName == value</c>, but not always. In
        /// other words, the FileName property of the ZipEntry you retrieve with this indexer, can
        /// be equal to the index value, but not always.  In the case of
        /// directory entries in the archive, you may retrieve them with the name of the directory
        /// with no trailing slash, even though in the entry itself, the actual <see
        /// cref="ZipEntry.FileName"/> property may include a trailing slash.  In other words, for
        /// a directory entry named "dir1", you may find <c>zip["dir1"].FileName ==
        /// "dir1/"</c>. Also, for any entry with slashes, they are stored in the zip file as
        /// forward slashes, but you may retrieve them with either forward or backslashes.  So,
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
        /// The ZipEntry within the Zip archive, given by the specified filename. If the named
        /// entry does not exist in the archive, this indexer returns null.
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
                            var FileNameNoSlash = e.FileName.Trim("/".ToCharArray());
                            if (FileNameNoSlash == fileName) return e;
                            // also check for equivalence
                            if (fileName.Replace("\\", "/") == FileNameNoSlash) return e;
                            if (FileNameNoSlash.Replace("\\", "/") == fileName) return e;
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
                            var FileNameNoSlash = e.FileName.Trim("/".ToCharArray());

                            if (String.Compare(FileNameNoSlash, fileName, StringComparison.CurrentCultureIgnoreCase) == 0) return e;
                            // also check for equivalence
                            if (String.Compare(fileName.Replace("\\", "/"), FileNameNoSlash, StringComparison.CurrentCultureIgnoreCase) == 0) return e;
                            if (String.Compare(FileNameNoSlash.Replace("\\", "/"), fileName, StringComparison.CurrentCultureIgnoreCase) == 0) return e;

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
        /// According to the ZIP specification, the names of the entries use forward slashes in pathnames. 
        /// If you are scanning through the list, you may have to swap forward slashes for backslashes.
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
                var foo = _entries.ConvertAll((e) => { return e.FileName; });
                return foo.AsReadOnly();
            }
        }


        /// <summary>
        /// Returns the readonly collection of entries in the Zip archive.
        /// </summary>
        /// <remarks>
        /// If there are no entries in the current ZipFile, the value returned is a non-null zero-element collection.
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
        /// After calling <c>RemoveEntry</c>, the application must call <c>Save</c> to make the 
        /// changes permanent.  
        /// </para>
        /// </remarks>
        ///
        /// <exception cref="System.ArgumentException">
        /// Thrown if the specified ZipEntry does not exist in the ZipFile.
        /// </exception>
        ///
        /// <example>
        /// In this example, all entries in the zip archive dating from before December 31st, 2007, are
        /// removed from the archive.  This is actually much easier if you use the RemoveSelectedEntries method.
        /// But I needed an example for RemoveEntry, so here it is. 
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
        /// The ZipEntry to remove from the zip. 
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
        /// Removes the ZipEntry with the given filename from the zip archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// After calling <c>RemoveEntry</c>, the application must call <c>Save</c> to make the changes permanent.  
        /// </para>
        ///
        /// </remarks>
        ///
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the ZipFile is not updatable. 
        /// </exception>
        ///
        /// <exception cref="System.ArgumentException">
        /// Thrown if a ZipEntry with the specified filename does not exist in the ZipFile.
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
        /// This is the class Destructor, which gets called implicitly when the instance is destroyed.  
        /// Because the ZipFile type implements IDisposable, this method calls Dispose(false).  
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
        /// to the ZipFile, if necessary.  
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
        /// This example extracts an entry selected by name, from the  Zip file to the Console.
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
        /// <param name="disposeManagedResources">indicates whether the method should dispose streams or not.</param>
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
#if !NETCF20
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
#if !NETCF20
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

        private System.IO.Stream WriteStream
        {
            get
            {
                if (_writestream == null)
                {
                    if (_name != null)
                    {
                        _temporaryFileName = (TempFileFolder != ".")
                ? System.IO.Path.Combine(TempFileFolder, SharedUtilities.GetTempFilename())
                : SharedUtilities.GetTempFilename();
                        _writestream = new System.IO.FileStream(_temporaryFileName, System.IO.FileMode.CreateNew);
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
        private System.IO.TextWriter _StatusMessageTextWriter;
        private bool _CaseSensitiveRetrieval;
        private System.IO.Stream _readstream;
        private System.IO.Stream _writestream;
        private bool _disposed;
        private System.Collections.Generic.List<ZipEntry> _entries;
        private bool _ForceNoCompression;
        private string _name;
        private string _Comment;
        private string _Password;
        private bool _fileAlreadyExists;
        private string _temporaryFileName;
        private bool _contentsChanged;
        private bool _hasBeenSaved;
        private String _TempFileFolder;
        private bool _ReadStreamIsOurs = true;
        private object LOCK = new object();
        private bool _saveOperationCanceled;
        private bool _extractOperationCanceled;
        private bool _JustSaved;
        private bool _NeedZip64CentralDirectory;
        private Nullable<bool> _OutputUsesZip64;
        internal bool _inExtractAll = false;
        private System.Text.Encoding _provisionalAlternateEncoding = System.Text.Encoding.GetEncoding("IBM437"); // default = IBM437

        internal Zip64Option _zip64 = Zip64Option.Default;
        #endregion
    }

    /// <summary>
    /// Options for using ZIP64 extensions when saving zip archives. 
    /// </summary>
    public enum Zip64Option
    {
        /// <summary>
        /// The default behavior, which is "Never".
        /// </summary>
        Default = 0,
        /// <summary>
        /// Do not use ZIP64 extensions when writing zip archives.
        /// </summary>
        Never = 0,
        /// <summary>
        /// Use ZIP64 extensions when writing zip archives, as necessary. 
        /// For example, when a single entry exceeds 0xFFFFFFFF in size, or when the archive as a whole 
        /// exceeds 0xFFFFFFFF in size, or when there are more than 65535 entries in an archive.
        /// </summary>
        AsNecessary = 1,
        /// <summary>
        /// Always use ZIP64 extensions when writing zip archives, even when unnecessary.
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

