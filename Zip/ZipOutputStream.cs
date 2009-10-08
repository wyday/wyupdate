// ZipOutputStream.cs
//
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa.  
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
// Time-stamp: <2009-October-07 16:31:58>
//
// ------------------------------------------------------------------
//
// This module defines the ZipOutputStream class, which is a stream metaphor for
// generating zip files.  This class does not depend on Ionic.Zip.ZipFile, but rather
// stands alongside it as an alternative "container" for ZipEntry.  It replicates a
// subset of the properties, including these:
//
//  - Comment
//  - Encryption
//  - Password
//  - CodecBufferSize 
//  - CompressionLevel
//  - EnableZip64 (UseZip64WhenSaving)
//
// It adds these novel methods:
//
//  - PutNextEntry
//  - Close
//
//
// ------------------------------------------------------------------
//

using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using Ionic.Zip;

namespace  Ionic.Zip
{
    /// <summary>
    ///   Provides a stream metaphor for generating zip files. Use this when
    ///   creating zip files, as an alternative to the <see cref="ZipFile"/> class,
    ///   when you wuold like to use a Stream class to write the zip file.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   This class provides alternative programming model from the one enabled by the
    ///   <see cref="ZipFile"/> class.
    /// </para>
    ///
    /// <para>
    ///   Some designs require a writable stream for output.  This stream can be used to
    ///   produce a zip file, as it is written.  
    /// </para>
    ///
    /// <para>
    ///   Both the <c>ZipOutputStream</c> class and the <c>ZipFile</c> class can be used
    ///   to create zip files. Both of them support many of the common zip features,
    ///   including Unicode, different compression levels, and ZIP64. Aside from the
    ///   differences in programming model, there are some other differences between the
    ///   two classes.
    /// </para>
    ///
    /// <list type="bullet">
    ///   <item>
    ///     <c>ZipFile</c> can be used to read and extract zip files, in addition to
    ///     creating zip files. <c>ZipOutputStream</c> cannot read zip files.
    ///   </item>
    ///
    ///   <item>
    ///     <c>ZipOutputStream</c> does not support the creation of segmented or spanned
    ///     zip files.
    ///   </item>
    ///
    ///   <item>
    ///     <c>ZipOutputStream</c> cannot produce a self-extracting archive. 
    ///   </item>
    /// </list>
    ///
    /// </remarks>
    public class ZipOutputStream : Stream
    {
        /// <summary>
        ///   Create a ZipOutputStream.
        /// </summary>
        ///
        /// <remarks>
        ///   The <see cref="ZipFile"/> class is generally easier to use when creating
        ///   zip files. The ZipOutputStream offers a different metaphor for creating a
        ///   zip file, based on the <see cref="System.IO.Stream"/> class.
        /// </remarks>
        ///
        /// <param name="stream">
        /// The stream to wrap. It must be writable. This stream will be closed at
        /// the time the ZipOutputStream is closed.
        /// </param>
        ///
        /// <example>
        ///
        ///   This example shows how to create a zip file, using the
        ///   ZipOutputStream class.
        ///
        /// <code>
        /// private void Zipup()
        /// {
        ///     if (filesToZip.Count == 0)
        ///     {
        ///         System.Console.WriteLine("Nothing to do.");
        ///         return;
        ///     }
        /// 
        ///     using (var raw = File.Open(_outputFileName, FileMode.Create, FileAccess.ReadWrite ))
        ///     {
        ///         using (var output= new ZipOutputStream(raw))
        ///         {
        ///             output.Password = "VerySecret!";
        ///             output.Encryption = EncryptionAlgorithm.WinZipAes256;
        /// 
        ///             foreach (string inputFileName in filesToZip)
        ///             {
        ///                 System.Console.WriteLine("file: {0}", inputFileName);
        /// 
        ///                 output.PutNextEntry(inputFileName); 
        ///                 using (var input = File.Open(inputFileName, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Write ))
        ///                 {
        ///                     byte[] buffer= new byte[2048];
        ///                     int n;
        ///                     while ((n= input.Read(buffer,0,buffer.Length)) > 0)
        ///                     {
        ///                         output.Write(buffer,0,n);
        ///                     }
        ///                 }
        ///             }
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        public ZipOutputStream(Stream stream)  : this (stream, false) { }

        
        /// <summary>
        ///   Create a ZipOutputStream.
        /// </summary>
        ///
        /// <remarks>
        ///   See the documentation for the <see
        ///   cref="ZipOutputStream(Stream)">ZipOutputStream(Stream)</see>
        ///   constructor for an example.
        /// </remarks>
        ///
        /// <param name="stream">
        ///   The stream to wrap. It must be writable.
        /// </param>
        ///
        /// <param name="leaveOpen">
        ///   true if the application would like the stream
        ///   to remain open after the <c>ZipOutputStream</c> has been closed.
        /// </param>
        public ZipOutputStream(Stream stream, bool leaveOpen)
        {
            _outputStream = stream;
            CompressionLevel = Ionic.Zlib.CompressionLevel.Default;
            _encryption = EncryptionAlgorithm.None;
            _entriesWritten = new List<ZipEntry>();
            _zip64 = Zip64Option.Never;
            _leaveUnderlyingStreamOpen = leaveOpen;
            Strategy = Ionic.Zlib.CompressionStrategy.Default;
        }

        
        /// <summary>
        ///   Sets the password to be used on the <c>ZipOutputStream</c> instance.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        ///   When writing a zip archive, this password is applied to the entries, not
        ///   to the zip archive itself. It applies to any <c>ZipEntry</c> subsequently
        ///   written to the <c>ZipOutputStream</c>.
        /// </para>
        /// 
        /// <para>
        ///   Using a password does not encrypt or protect the "directory" of the
        ///   archive - the list of entries contained in the archive.  If you set the
        ///   <c>Password</c> property, the password actually applies to individual
        ///   entries that are added to the archive, subsequent to the setting of this
        ///   property.  The list of filenames in the archive that is eventually created
        ///   will appear in clear text, but the contents of the individual files are
        ///   encrypted.  This is how Zip encryption works.
        /// </para>
        /// 
        /// <para>
        ///   If you set this property, and then add a set of entries to the archive via
        ///   calls to <c>PutNextEntry</c>, then each entry is encrypted with that
        ///   password.  You may also want to change the password between adding
        ///   different entries. If you set the password, add an entry, then set the
        ///   password to <c>null</c> (<c>Nothing</c> in VB), and add another entry, the
        ///   first entry is encrypted and the second is not.
        /// </para>
        /// 
        /// <para>
        ///   When setting the <c>Password</c>, you may also want to explicitly set the <see
        ///   cref="Encryption"/> property, to specify how to encrypt the entries added
        ///   to the ZipFile.  If you set the <c>Password</c> to a non-null value and do not
        ///   set <see cref="Encryption"/>, then PKZip 2.0 ("Weak") encryption is used.
        ///   This encryption is relatively weak but is very interoperable. If
        ///   you set the password to a <c>null</c> value (<c>Nothing</c> in VB),
        ///   <c>Encryption</c> is reset to None.
        /// </para>
        /// 
        /// <para>
        ///   Special case: if you wrap a ZipOutputStream around a non-seekable stream,
        ///   and use encryption, and emit an entry of zero bytes, the <c>Close()</c> or
        ///   <c>PutNextEntry()</c> following the entry will throw an exception.
        /// </para>
        ///
        /// </remarks>
        public String Password
        {
            set
            {
                if (_closed)
                {
                    _exceptionPending = true;
                    throw new System.InvalidOperationException("The stream has been closed.");
                }
                
                _password = value;
                if (_password == null)
                {
                    _encryption = EncryptionAlgorithm.None;
                }
                else if (_encryption == EncryptionAlgorithm.None)
                {
                    _encryption = EncryptionAlgorithm.PkzipWeak;
                }
            }
        }

        
        /// <summary>
        ///   The Encryption to use for entries added to the <c>ZipOutputStream</c>.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   The specified Encryption is applied to the entries subsequently
        ///   written to the <c>ZipOutputStream</c> instance.  
        /// </para>
        /// 
        /// <para>
        ///   If you set this to something other than
        ///   EncryptionAlgorithm.None, you will also need to set the
        ///   <see cref="Password"/> to a non-null, non-empty value in
        ///   order to actually get encryption on the entry.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <seealso cref="Password">ZipOutputStream.Password</seealso>
        /// <seealso cref="Ionic.Zip.ZipEntry.Encryption">ZipEntry.Encryption</seealso>
        public EncryptionAlgorithm Encryption
        {
            get
            {
                return _encryption;
            }
            set
            {
                if (_closed)
                {
                    _exceptionPending = true;
                    throw new System.InvalidOperationException("The stream has been closed.");
                }
                if (value == EncryptionAlgorithm.Unsupported)
                {
                    _exceptionPending = true;
                    throw new InvalidOperationException("You may not set Encryption to that value.");
                }
                _encryption= value;
            }
        }


        /// <summary>
        ///   Size of the work buffer to use for the ZLIB codec during compression.
        /// </summary>
        ///
        /// <remarks>
        ///   Setting this may affect performance.  For larger files, setting this to a
        ///   larger size may improve performance, but I'm not sure.  Sorry, I don't
        ///   currently have good recommendations on how to set it.  You can test it if
        ///   you like.
        /// </remarks>
        public int CodecBufferSize
        {
            get;
            set;
        }

        
        /// <summary>
        ///   The compression strategy to use for all entries.
        /// </summary>
        ///
        /// <remarks>
        ///   Set the Strategy used by the ZLIB-compatible compressor, when compressing
        ///   data for the entries in the zip archive. Different compression strategies
        ///   work better on different sorts of data. The strategy parameter can affect
        ///   the compression ratio and the speed of compression but not the correctness
        ///   of the compresssion.  For more information see <see
        ///   cref="Ionic.Zlib.CompressionStrategy "/>.
        /// </remarks>
        public Ionic.Zlib.CompressionStrategy Strategy
        {
            get ;
            set ;
        }


        /// <summary>
        ///   The type of timestamp attached to the ZipEntry.
        /// </summary>
        ///
        /// <remarks>
        ///   Set this in order to specify the kind of timestamp that should be emitted
        ///   into the zip file for each entry.
        /// </remarks>
        public ZipEntryTimestamp Timestamp
        {
            get
            {
                return _timestamp;
            }
            set
            {
                if (_closed)
                {
                    _exceptionPending = true;
                    throw new System.InvalidOperationException("The stream has been closed.");
                }
                _timestamp= value;
            }
        }

        
        /// <summary>
        ///   Sets the compression level to be used for entries subsequently added to
        ///   the zip archive.
        /// </summary>
        ///
        /// <remarks>
        ///  <para>
        ///    Varying the compression level used on entries can affect the
        ///    size-vs-speed tradeoff when compression and decompressing data streams
        ///    or files.
        ///  </para>
        ///
        ///  <para>
        ///    As with some other properties on the <c>ZipOutputStream</c> class, like <see
        ///    cref="Password"/>, and <see cref="Encryption"/>, 
        ///    setting this property on a <c>ZipOutputStream</c>
        ///    instance will cause the specified <c>CompressionLevel</c> to be used on all
        ///    <see cref="ZipEntry"/> items that are subsequently added to the
        ///    <c>ZipOutputStream</c> instance. 
        ///  </para>
        ///
        ///  <para>
        ///    If you do not set this property, the default compression level is used,
        ///    which normally gives a good balance of compression efficiency and
        ///    compression speed.  In some tests, using <c>BestCompression</c> can
        ///    double the time it takes to compress, while delivering just a small
        ///    increase in compression efficiency.  This behavior will vary with the
        ///    type of data you compress.  If you are in doubt, just leave this setting
        ///    alone, and accept the default.
        ///  </para>
        /// </remarks>
        public Ionic.Zlib.CompressionLevel CompressionLevel
        {
            get;
            set;
        }

                
        /// <summary>
        ///   A comment attached to the zip archive.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para>
        ///   The application sets this property to specify a comment to be embedded
        ///   into the generated zip archive.
        /// </para>
        ///
        /// <para>
        ///   According to <see
        ///   href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">PKWARE's
        ///   zip specification</see>, the comment is not encrypted, even if there is a
        ///   password set on the zip file.
        /// </para>
        ///
        /// <para>
        ///   The zip specification does not describe how to encode the comment string
        ///   in a code page other than IBM437. Therefore, for "compliant" zip tools and
        ///   libraries, comments will use IBM437. However, there are situations where
        ///   you want an encoded Comment, for example using code page 950 "Big-5
        ///   Chinese".  DotNetZip will encode the comment in the code page specified by
        ///   <see cref="ProvisionalAlternateEncoding"/>, at the time of the call to
        ///   <c>Close()</c>.
        /// </para>
        ///
        /// </remarks>
        public string Comment
        {
            get { return _comment; }
            set
            {
                if (_closed)
                {
                    _exceptionPending = true;
                    throw new System.InvalidOperationException("The stream has been closed.");
                }
                _comment = value;
            }
        }



        /// <summary>
        ///   Specify whether to use ZIP64 extensions when saving a zip archive. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   The default value for the property is <see
        ///   cref="Zip64Option.Never"/>. <see cref="Zip64Option.AsNecessary"/> is
        ///   safest, in the sense that you will not get an Exception if a
        ///   pre-ZIP64 limit is exceeded.
        /// </para>
        ///
        /// <para>
        ///   You must set this property before calling <c>Write()</c>. 
        /// </para>
        ///
        /// </remarks>
        public Zip64Option EnableZip64
        {
            get
            {
                return _zip64;
            }
            set
            {
                if (_closed)
                {
                    _exceptionPending = true;
                    throw new System.InvalidOperationException("The stream has been closed.");
                }
                _zip64 = value;
            }
        }


        /// <summary>
        ///   Indicates whether ZIP64 extensions were used when saving the zip archive. 
        /// </summary>
        ///
        /// <remarks>
        ///   This is interesting after the <c>ZipOutputStream</c> has been closed.
        /// </remarks>
        public bool OutputUsedZip64
        {
            get
            {
                return _anyEntriesUsedZip64 || _directoryNeededZip64;
            }
        }



        /// <summary>
        ///   Indicates whether to encode entry filenames and entry comments using
        ///   Unicode (UTF-8).
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">The
        ///   PKWare zip specification</see> provides for encoding file names and file
        ///   comments in either the IBM437 code page, or in UTF-8.  This flag selects
        ///   the encoding according to that specification.  By default, this flag is
        ///   false, and filenames and comments are encoded into the zip file in the
        ///   IBM437 codepage.  Setting this flag to true will specify that filenames
        ///   and comments that cannot be encoded with IBM437 will be encoded with
        ///   UTF-8.
        /// </para>
        ///
        /// <para>
        ///   Zip files created with strict adherence to the PKWare specification with
        ///   respect to UTF-8 encoding can contain entries with filenames containing
        ///   any combination of Unicode characters, including the full range of
        ///   characters from Chinese, Latin, Hebrew, Greek, Cyrillic, and many other
        ///   alphabets.  However, because at this time, the UTF-8 portion of the PKWare
        ///   specification is not broadly supported by other zip libraries and
        ///   utilities, such zip files may not be readable by your favorite zip tool or
        ///   archiver. In other words, interoperability will decrease if you set this
        ///   flag to true.
        /// </para>
        ///
        /// <para>
        ///   In particular, Zip files created with strict adherence to the PKWare
        ///   specification with respect to UTF-8 encoding will not work well with
        ///   Explorer in Windows XP or Windows Vista, because Windows compressed
        ///   folders, as far as I know, do not support UTF-8 in zip files.  Vista can
        ///   read the zip files, but shows the filenames incorrectly. Unpacking from
        ///   Windows Vista Explorer will result in filenames that have rubbish
        ///   characters in place of the high-order UTF-8 bytes.
        /// </para>
        ///
        /// <para>
        ///   Also, zip files that use UTF-8 encoding will not work well with Java
        ///   applications that use the java.util.zip classes, as of v5.0 of the Java
        ///   runtime. The Java runtime does not correctly implement the PKWare
        ///   specification in this regard.
        /// </para>
        ///
        /// <para>
        ///   As a result, we have the unfortunate situation that "correct" behavior by
        ///   the DotNetZip library with regard to Unicode encoding of filenames during
        ///   zip creation will result in zip files that are readable by strictly
        ///   compliant and current tools (for example the most recent release of the
        ///   commercial WinZip tool); but these zip files will not be readable by
        ///   various other tools or libraries, including Windows Explorer.
        /// </para>
        ///
        /// <para>
        ///   The DotNetZip library can read and write zip files with UTF8-encoded
        ///   entries, according to the PKware spec.  If you use DotNetZip for both
        ///   creating and reading the zip file, and you use UTF-8, there will be no
        ///   loss of information in the filenames. For example, using a self-extractor
        ///   created by this library will allow you to unpack files correctly with no
        ///   loss of information in the filenames.
        /// </para>
        ///
        /// <para>
        ///   If you do not set this flag, it will remain false.  If this flag is false,
        ///   the <c>ZipOutputStream</c> will encode all filenames and comments using
        ///   the IBM437 codepage.  This can cause "loss of information" on some
        ///   filenames, but the resulting zipfile will be more interoperable with other
        ///   utilities. As an example of the loss of information, diacritics can be
        ///   lost.  The o-tilde character will be down-coded to plain o.  The c with a
        ///   cedilla (Unicode 0xE7) used in Portugese will be downcoded to a c.
        ///   Likewise, the O-stroke character (Unicode 248), used in Danish and
        ///   Norwegian, will be down-coded to plain o. Chinese characters cannot be
        ///   represented in codepage IBM437; when using the default encoding, Chinese
        ///   characters in filenames will be represented as ?. These are all examples
        ///   of "information loss".
        /// </para>
        ///
        /// <para>
        ///   The loss of information associated to the use of the IBM437 encoding is
        ///   inconvenient, and can also lead to runtime errors. For example, using
        ///   IBM437, any sequence of 4 Chinese characters will be encoded as ????.  If
        ///   your application creates a <c>ZipOutputStream</c>, does not set the
        ///   encoding, then adds two files, each with names of four Chinese characters
        ///   each, this will result in a duplicate filename exception.  In the case
        ///   where you add a single file with a name containing four Chinese
        ///   characters, the zipfile will save properly, but extracting that file
        ///   later, with any zip tool, will result in an error, because the question
        ///   mark is not legal for use within filenames on Windows.  These are just a
        ///   few examples of the problems associated to loss of information.
        /// </para>
        ///
        /// <para>
        ///   This flag is independent of the encoding of the content within the entries
        ///   in the zip file. Think of the zip file as a container - it supports an
        ///   encoding.  Within the container are other "containers" - the file entries
        ///   themselves.  The encoding within those entries is independent of the
        ///   encoding of the zip archive container for those entries.
        /// </para>
        ///
        /// <para>
        ///   Rather than specify the encoding in a binary fashion using this flag, an
        ///   application can specify an arbitrary encoding via the <see
        ///   cref="ProvisionalAlternateEncoding"/> property.  Setting the encoding
        ///   explicitly when creating zip archives will result in non-compliant zip
        ///   files that, curiously, are fairly interoperable.  The challenge is, the
        ///   PKWare specification does not provide for a way to specify that an entry
        ///   in a zip archive uses a code page that is neither IBM437 nor UTF-8.
        ///   Therefore if you set the encoding explicitly when creating a zip archive,
        ///   you must take care upon reading the zip archive to use the same code page.
        ///   If you get it wrong, the behavior is undefined and may result in incorrect
        ///   filenames, exceptions, stomach upset, hair loss, and acne.
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
                _provisionalAlternateEncoding = (value) ? System.Text.Encoding.GetEncoding("UTF-8") :
                    Ionic.Zip.ZipFile.DefaultEncoding;
            }
        }


        /// <summary>
        ///   The text encoding to use when emitting entries into the zip archive, for
        ///   those entries whose filenames or comments cannot be encoded with the
        ///   default (IBM437) encoding.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        ///   In <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">its
        ///   zip specification</see>, PKWare describes two options for encoding
        ///   filenames and comments: using IBM437 or UTF-8.  But, some archiving tools
        ///   or libraries do not follow the specification, and instead encode
        ///   characters using the system default code page.  For example, WinRAR when
        ///   run on a machine in Shanghai may encode filenames with the Big-5 Chinese
        ///   (950) code page.  This behavior is contrary to the Zip specification, but
        ///   it occurs anyway.
        /// </para>
        ///
        /// <para>
        ///   When using DotNetZip to write zip archives that will be read by one of
        ///   these other archivers, set this property to specify the code page to use
        ///   when encoding the <see cref="ZipEntry.FileName"/> and <see
        ///   cref="ZipEntry.Comment"/> for each <c>ZipEntry</c> in the zip file, for
        ///   values that cannot be encoded with the default codepage for zip files,
        ///   IBM437.  This is why this property is "provisional".  In all cases, IBM437
        ///   is used where possible, in other words, where no loss of data would
        ///   result. It is possible, therefore, to have a given entry with a
        ///   <c>Comment</c> encoded in IBM437 and a <c>FileName</c> encoded with the
        ///   specified "provisional" codepage.
        /// </para>
        ///
        /// <para>
        ///   Be aware that a zip file created after you've explicitly set the 
        ///   <c>ProvisionalAlternateEncoding</c> property to a value other than
        ///   IBM437 may not be compliant to the PKWare specification, and may not be
        ///   readable by compliant archivers.  On the other hand, many (most?)
        ///   archivers are non-compliant and can read zip files created in arbitrary
        ///   code pages.  The trick is to use or specify the proper codepage when
        ///   reading the zip.
        /// </para>
        ///
        /// <para>
        ///   When creating a zip archive using this library, it is possible to change
        ///   the value of <c>ProvisionalAlternateEncoding</c> between each
        ///   entry you add, and between adding entries and the call to
        ///   <c>Close()</c>. Don't do this. It will likely result in a zipfile that is
        ///   not readable.  For best interoperability, either leave
        ///   <c>ProvisionalAlternateEncoding</c> alone, or specify it only once,
        ///   before adding any entries to the <c>ZipOutputStream</c> instance.  There is one
        ///   exception to this recommendation, described later.
        /// </para>
        ///
        /// <para>
        ///   When using an arbitrary, non-UTF8 code page for encoding, there is no
        ///   standard way for the creator application - whether DotNetZip, WinZip,
        ///   WinRar, or something else - to formally specify in the zip file which
        ///   codepage has been used for the entries. As a result, readers of zip files
        ///   are not able to inspect the zip file and determine the codepage that was
        ///   used for the entries contained within it.  It is left to the application
        ///   or user to determine the necessary codepage when reading zip files encoded
        ///   this way.  If you use an incorrect codepage when reading a zipfile, you
        ///   will get entries with filenames that are incorrect, and the incorrect
        ///   filenames may even contain characters that are not legal for use within
        ///   filenames in Windows. Extracting entries with illegal characters in the
        ///   filenames will lead to exceptions. It's too bad, but this is just the way
        ///   things are with code pages in zip files. Caveat Emptor.
        /// </para>
        ///
        /// <para>
        ///   One possible approach for specifying the code page for a given zip file is
        ///   to describe the code page in a human-readable form in the Zip comment. For
        ///   example, the comment may read "Entries in this archive are encoded in the
        ///   Big5 code page".  For maximum interoperability, the zip comment in this
        ///   case should be encoded in the default, IBM437 code page.  In this case,
        ///   the zip comment is encoded using a different page than the filenames.  To
        ///   do this, Specify <c>ProvisionalAlternateEncoding</c> to your desired
        ///   region-specific code page, once before adding any entries, and then set
        ///   the <see cref="Comment"/> property and reset
        ///   <c>ProvisionalAlternateEncoding</c> to IBM437 before calling <c>Close()</c>.
        /// </para>
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
        

        private void InsureUniqueEntry(ZipEntry ze1)
        {
            foreach (ZipEntry ze2 in _entriesWritten)
            {
                if (SharedUtilities.TrimVolumeAndSwapSlashes(ze1.FileName) == ze2.FileName)
                {
                    _exceptionPending = true;
                    throw new ArgumentException(String.Format("The entry '{0}' already exists in the zip archive.", ze1.FileName));
                }
            }
        }

        internal Stream OutputStream
        {
            get
            {
                return _outputStream; 
            }
        }

        
        /// <summary>
        ///   Returns true if an entry by the given name has already been written
        ///   to the ZipOutputStream.
        /// </summary>
        /// 
        /// <param name="name">
        ///   The name of the entry to scan for.
        /// </param>
        ///
        /// <returns>
        /// true if an entry by the given name has already been written.
        /// </returns>
        public bool ContainsEntry(string name)
        {
            foreach (var e in _entriesWritten)
            {
                if (e.FileName == name)
                    return true;
            }
            return false;
        }

        
        /// <summary>
        ///   Write the data from the buffer to the stream.
        /// </summary>
        ///
        /// <remarks>
        ///   As the application writes data into this stream, the data may be
        ///   compressed and encrypted before being written out to the underlying
        ///   stream, depending on the settings of the <see cref="CompressionLevel"/>
        ///   and the <see cref="Encryption"/> properties.
        /// </remarks>
        ///
        /// <param name="buffer">The buffer holding data to write to the stream.</param>
        /// <param name="offset">the offset within that data array to find the first byte to write.</param>
        /// <param name="count">the number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_closed)
            {
                _exceptionPending = true;
                throw new System.InvalidOperationException("The stream has been closed.");
            }
            
            if (_currentEntry==null)
            {
                _exceptionPending = true;
                throw new System.InvalidOperationException("must call PutNextEntry() before Write()");
            }
            
            if (_wantEntryHeader)
                _InitiateCurrentEntry();

            _entryOutputStream.Write(buffer, offset, count);
        }


        private void _InitiateCurrentEntry()
        {
            _entriesWritten.Add(_currentEntry);

            // write out the header
            _currentEntry.WriteHeader(_outputStream, 0);
            _currentEntry.WriteSecurityMetadata(_outputStream);
            _currentEntry.PrepOutputStream(_outputStream, out _outputCounter, out _encryptor, out _deflater, out _entryOutputStream);
            _wantEntryHeader = false;
        }


        
        /// <summary>
        ///   Specify the name of the next entry that will be written to the zip file.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   Call this method just before calling <see cref="Write(byte[], int, int)"/>, to
        ///   specify the name of the entry that the next set of bytes written to
        ///   the <c>ZipOutputStream</c> belongs to. All subsequent calls to <c>Write</c>,
        ///   until the next call to <c>PutNextEntry</c>, 
        ///   will be inserted into the named entry in the zip file. 
        /// </para>
        ///
        /// <para>
        ///   If you don't call <c>Write()</c> between two calls to
        ///   <c>PutNextEntry()</c>, the first entry is inserted into the zip file as a
        ///   file of zero size.  This may be what you want.  
        /// </para>
        ///
        /// <para>
        ///   Because <c>PutNextEntry()</c> closes out the prior entry, if any, this
        ///   method may throw if there is a problem with the prior entry.  One such
        ///   condition occurs when zero bytes have been written for an entry, and
        ///   Encryption is in use, and the wrapped stream is non-seekable.
        /// </para>
        ///
        /// <para>
        ///   This method returns the <c>ZipEntry</c>.  You can modify public properties
        ///   on the ZipEntry, such as <see cref="ZipEntry.Encryption"/>, <see
        ///   cref="ZipEntry.Password"/>, and so on, until the first call to
        ///   <c>ZipOutputStream.Write()</c>.  If you modify the <c>ZipEntry</c>
        ///   <em>after</em> having called <c>Write()</c>, you may get a runtime
        ///   exception, or you may silently get an invalid zip archive.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <example>
        ///
        ///   This example shows how to create a zip file, using the
        ///   ZipOutputStream class.
        ///
        /// <code>
        /// private void Zipup()
        /// {
        ///     using (FileStream fs raw = File.Open(_outputFileName, FileMode.Create, FileAccess.ReadWrite ))
        ///     {
        ///         using (var output= new ZipOutputStream(fs))
        ///         {
        ///             output.Password = "VerySecret!";
        ///             output.Encryption = EncryptionAlgorithm.WinZipAes256;
        ///             output.PutNextEntry("entry1.txt");
        ///             byte[] buffer= System.Text.Encoding.ASCII.GetBytes("This is the content for entry #1."); 
        ///             output.Write(buffer,0,buffer.Length);
        ///             output.PutNextEntry("entry2.txt");  // this will be zero length
        ///             output.PutNextEntry("entry3.txt"); 
        ///             buffer= System.Text.Encoding.ASCII.GetBytes("This is the content for entry #3."); 
        ///             output.Write(buffer,0,buffer.Length);
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        ///
        /// <param name="entryName">
        ///   The name of the entry to be added, including any path to be used
        ///   within the zip file. 
        /// </param>
        ///
        /// <returns>
        ///   The ZipEntry created.
        /// </returns>
        ///
        /// <exception cref="ZipException">
        ///   Thrown if Encryption is not <c>None</c>, and the previous entry was zero
        ///   bytes in length, and the wrapped stream is non-seekable.
        /// </exception>
        ///
        public ZipEntry PutNextEntry(String entryName)
        {
            if (_closed)
            {
                _exceptionPending = true;
                throw new System.InvalidOperationException("The stream has been closed.");
            }

            _FinishCurrentEntry();
            _currentEntry = ZipEntry.CreateForZipOutputStream(entryName);
            _currentEntry._container = new ZipContainer(this);
            _currentEntry.FileName = entryName;
            _currentEntry._BitField |= 0x0008;  // workitem 8932
            _currentEntry.SetEntryTimes(DateTime.Now,DateTime.Now,DateTime.Now);
            _currentEntry.CompressionLevel = CompressionLevel;
            _currentEntry.Encryption = Encryption;
            _currentEntry.Password = _password;
            _currentEntry.EmitTimesInWindowsFormatWhenSaving = ((_timestamp & ZipEntryTimestamp.Windows) != 0);
            _currentEntry.EmitTimesInUnixFormatWhenSaving = ((_timestamp & ZipEntryTimestamp.Unix) != 0);
            _currentEntry._container = new ZipContainer(this);
            InsureUniqueEntry(_currentEntry);
            _wantEntryHeader= true;

            return _currentEntry;
        }


        
        private void _FinishCurrentEntry()
        {
            if (_currentEntry!=null)
            {
                if (_wantEntryHeader)
                    _InitiateCurrentEntry();
                
                _currentEntry.FinishOutputStream(_outputStream, _outputCounter, _encryptor, _deflater, _entryOutputStream);
                _currentEntry.PostProcessOutput(_outputStream);
                _anyEntriesUsedZip64 |= _currentEntry.OutputUsedZip64.Value;
            }
        }


        /// <summary>
        ///   Close the stream.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        ///   This method writes the Zip Central directory, then Closes the stream.  The
        ///   application must call Close() in order to produce a valid zip file. 
        /// </para>
        ///
        /// <para>
        ///   Typically the application will call <c>Close()</c> implicitly, via a <c>using</c>
        ///   statement in C#, or a <c>Using</c> statement in VB.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <exception cref="ZipException">
        ///   Thrown if Encryption is not <c>None</c>, and the previous entry was zero
        ///   bytes in length, and the wrapped stream is non-seekable.
        /// </exception>
        ///
        public override void Close()
        {
            if (_closed) return;

            // When ZipOutputStream is used within a using clause, and an exception is thrown,
            // Close() is invoked.  But we don't want to try to write anything in that case.
            // Eventually the exception will be propagated to the application.
            if (_exceptionPending) return;

            _FinishCurrentEntry();
            _directoryNeededZip64 = ZipOutput.WriteCentralDirectoryStructure(_outputStream,
                                                                             _entriesWritten,
                                                                             0, // _numberOfSegmentsForMostRecentSave, 
                                                                             _zip64,
                                                                             Comment,
                                                                             ProvisionalAlternateEncoding);
            
            if (!_leaveUnderlyingStreamOpen)
                _outputStream.Close();
            
            _closed= true;
        }


        /// <summary>
        /// Always returns false.
        /// </summary>
        public override bool CanRead  { get { return false; } }
        
        /// <summary>
        /// Always returns false.
        /// </summary>
        public override bool CanSeek  { get { return false; } }
        
        /// <summary>
        /// Always returns true.
        /// </summary>
        public override bool CanWrite { get { return true; } }
        
        /// <summary>
        /// Always returns a NotSupportedException.
        /// </summary>
        public override long Length   { get { throw new NotSupportedException(); }}

        /// <summary>
        /// Always returns a NotSupportedException.
        /// </summary>
        public override long Position
        {
            get { throw new NotSupportedException();}
            set { throw new NotSupportedException();}
        }

        /// <summary>
        /// This is a no-op.
        /// </summary>
        public override void Flush() { }

        /// <summary>
        /// This method always throws a NotSupportedException.
        /// </summary>
        /// <param name="buffer">ignored</param>
        /// <param name="offset">ignored</param>
        /// <param name="count">ignored</param>
        /// <returns>nothing</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Read");
        }

        /// <summary>
        /// This method always throws a NotSupportedException.
        /// </summary>
        /// <param name="offset">ignored</param>
        /// <param name="origin">ignored</param>
        /// <returns>nothing</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Seek");
        }

        /// <summary>
        /// This method always throws a NotSupportedException.
        /// </summary>
        /// <param name="value">ignored</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        
        private Ionic.Zlib.CrcCalculatorStream _entryOutputStream;
        
        private EncryptionAlgorithm _encryption;
        private ZipEntryTimestamp _timestamp;
        internal String _password;
        private String _comment;
        private Stream _outputStream;
        private ZipEntry _currentEntry;
        internal Zip64Option _zip64;
        private List<ZipEntry> _entriesWritten;
        private System.Text.Encoding _provisionalAlternateEncoding;
        private bool _leaveUnderlyingStreamOpen;
        private bool _closed;
        private bool _exceptionPending;
        private bool _anyEntriesUsedZip64, _directoryNeededZip64;
        private CountingStream _outputCounter;
        private Stream _encryptor;
        private Stream _deflater;
        private bool _wantEntryHeader;
    }


    
    internal class ZipContainer
    {
        private ZipFile _zf;
        private ZipOutputStream _zos;
        public ZipContainer(Object o)
        {
            _zf= (o as ZipFile) ;
            _zos = (o as ZipOutputStream);
        }

        public ZipFile ZipFile
        {
            get { return _zf; }
        }
        
        public ZipOutputStream ZipOutputStream
        {
            get { return _zos; }
        }
        
        public string Password
        {
            get
            {
                if (_zf!=null) return _zf._Password;
                return _zos._password;
            }
        }
        
        public Zip64Option Zip64
        {
            get
            {
                if (_zf!=null) return _zf._zip64;
                return _zos._zip64;
            }
        }
        
        public int BufferSize
        {
            get
            {
                if (_zf!=null) return _zf.BufferSize;
                return 0;
            }
        }
        
        public int CodecBufferSize
        {
            get
            {
                if (_zf!=null) return _zf.CodecBufferSize;
                return _zos.CodecBufferSize;
            }
        }
        
        public Ionic.Zlib.CompressionStrategy Strategy
        {
            get
            {
                if (_zf!=null) return _zf.Strategy;
                return _zos.Strategy;
            }
        }

        public Zip64Option UseZip64WhenSaving
        {
            get
            {
                if (_zf!=null) return _zf.UseZip64WhenSaving;
                return _zos.EnableZip64;
            }
        }
        
        public bool ContainsEntry(string name)
        {
            if (_zf!=null) return _zf.ContainsEntry(name);
            return _zos.ContainsEntry(name);
        }
    }
    
}