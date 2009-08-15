// ZipFile.Read.cs
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
// Time-stamp: <2009-August-05 13:05:54>
//
// ------------------------------------------------------------------
//
// This module defines the methods for Reading zip files.
//
// ------------------------------------------------------------------
//


using System;
using System.IO;
using System.Collections.Generic;

namespace Ionic.Zip
{

    public partial class ZipFile
    {
        /// <summary>
        /// Reads a zip file archive and returns the instance.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The stream is read using the default <c>System.Text.Encoding</c>, which is the
        /// <c>IBM437</c> codepage.
        /// </para>
        /// </remarks>
        ///
        /// <exception cref="System.Exception">
        /// Thrown if the <c>ZipFile</c> cannot be read. The implementation of this method
        /// relies on <c>System.IO.File.OpenRead</c>, which can throw a variety of exceptions,
        /// including specific exceptions if a file is not found, an unauthorized access
        /// exception, exceptions for poorly formatted filenames, and so on.
        /// </exception>
        /// 
        /// <param name="fileName">
        /// The name of the zip archive to open.  This can be a fully-qualified or relative
        /// pathname.
        /// </param>
        /// 
        /// <overloads>This method has a bunch of interesting overloads. They are all
        /// static (Shared in VB).  One of them is bound to be right for you.  The
        /// reason there are so many is that there are a few properties on the
        /// <c>ZipFile</c> class that must be set before you read the zipfile in, for
        /// them to be useful.  The set of overloads covers the most interesting cases.
        /// Probably there are still too many, though.</overloads>
        ///
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string fileName)
        {
            return ZipFile.Read(fileName, null, DefaultEncoding);
        }

        /// <summary>
        /// Reads a zip file archive and returns the instance, using the specified
        /// ReadProgress event handler.  
        /// </summary>
        /// 
        /// <param name="fileName">
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
        public static ZipFile Read(string fileName, EventHandler<ReadProgressEventArgs> readProgress)
        {
            return ZipFile.Read(fileName, null, DefaultEncoding, readProgress);
        }

        /// <summary>
        /// Reads a zip file archive using the specified text encoding, and returns the
        /// instance.
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
        /// <param name="fileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to use for writing verbose status messages
        /// during operations on the zip archive.  A console application may wish to
        /// pass <c>System.Console.Out</c> to get messages on the Console. A graphical
        /// or headless application may wish to capture the messages in a different
        /// <c>TextWriter</c>, such as a <c>System.IO.StringWriter</c>.
        /// </param>
        /// 
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string fileName, TextWriter statusMessageWriter)
        {
            return ZipFile.Read(fileName, statusMessageWriter, DefaultEncoding);
        }


        /// <summary>
        /// Reads a zip file archive using the specified text encoding, and the
        /// specified ReadProgress event handler, and returns the instance.  
        /// </summary>
        /// 
        /// <param name="fileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to use for writing verbose status messages
        /// during operations on the zip archive.  A console application may wish to
        /// pass <c>System.Console.Out</c> to get messages on the Console. A graphical
        /// or headless application may wish to capture the messages in a different
        /// <c>TextWriter</c>, such as a <c>System.IO.StringWriter</c>.
        /// </param>
        /// 
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string fileName,
                   TextWriter statusMessageWriter,
                   EventHandler<ReadProgressEventArgs> readProgress)
        {
            return ZipFile.Read(fileName, statusMessageWriter, DefaultEncoding, readProgress);
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
        /// This example shows how to read a zip file using the Big-5 Chinese code page
        /// (950), and extract each entry in the zip file.  For this code to work as
        /// desired, the zipfile must have been created using the big5 code page
        /// (CP950). This is typical, for example, when using WinRar on a machine with
        /// CP950 set as the default code page.  In that case, the names of entries
        /// within the Zip archive will be stored in that code page, and reading the zip
        /// archive must be done using that code page.  If the application did not use
        /// the correct code page in ZipFile.Read(), then names of entries within the
        /// zip archive would not be correctly retrieved.
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
        /// <param name="fileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="encoding">
        /// The <c>System.Text.Encoding</c> to use when reading in the zip archive. Be
        /// careful specifying the encoding.  If the value you use here is not the same
        /// as the Encoding used when the zip archive was created (possibly by a
        /// different archiver) you will get unexpected results and possibly exceptions.
        /// </param>
        /// 
        /// <seealso cref="ProvisionalAlternateEncoding"/>.
        ///
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string fileName, System.Text.Encoding encoding)
        {
            return ZipFile.Read(fileName, null, encoding);
        }


        /// <summary>
        /// Reads a zip file archive using the specified text encoding and ReadProgress
        /// event handler, and returns the instance.  
        /// </summary>
        /// 
        /// <param name="fileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <param name="encoding">
        /// The <c>System.Text.Encoding</c> to use when reading in the zip archive. Be
        /// careful specifying the encoding.  If the value you use here is not the same
        /// as the Encoding used when the zip archive was created (possibly by a
        /// different archiver) you will get unexpected results and possibly exceptions.
        /// </param>
        /// 
        /// <returns>The instance read from the zip archive.</returns>
        ///
        public static ZipFile Read(string fileName,
                                   System.Text.Encoding encoding,
                                   EventHandler<ReadProgressEventArgs> readProgress)
        {
            return ZipFile.Read(fileName, null, encoding, readProgress);
        }


        /// <summary>
        /// Reads a zip file archive using the specified text encoding and the specified
        /// TextWriter for status messages, and returns the instance.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This version of the method allows the caller to pass in a <c>TextWriter</c>
        /// and an <c>Encoding</c>.  The ZipFile is read in using the specified encoding
        /// for entries where UTF-8 encoding is not explicitly specified.
        /// </para>
        /// </remarks>
        /// 
        /// 
        /// <example>
        /// This example shows how to read a zip file using the Big-5 Chinese code page
        /// (950), and extract each entry in the zip file, while sending status messages
        /// out to the Console.
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
        /// <param name="fileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to use for writing verbose status messages
        /// during operations on the zip archive.  A console application may wish to
        /// pass <c>System.Console.Out</c> to get messages on the Console. A graphical
        /// or headless application may wish to capture the messages in a different
        /// <c>TextWriter</c>, such as a <c>System.IO.StringWriter</c>.
        /// </param>
        /// 
        /// <param name="encoding">
        /// The <c>System.Text.Encoding</c> to use when reading in the zip archive. Be
        /// careful specifying the encoding.  If the value you use here is not the same
        /// as the Encoding used when the zip archive was created (possibly by a
        /// different archiver) you will get unexpected results and possibly exceptions.
        /// </param>
        /// 
        /// <seealso cref="ProvisionalAlternateEncoding"/>
        ///
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string fileName,
                                   TextWriter statusMessageWriter,
                                   System.Text.Encoding encoding)
        {
            return Read(fileName, statusMessageWriter, encoding, null);
        }

        /// <summary>
        /// Reads a zip file archive using the specified text encoding,  the specified
        /// TextWriter for status messages, and the specified ReadProgress event handler, 
        /// and returns the instance.  
        /// </summary>
        /// 
        /// <param name="fileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to use for writing verbose status messages
        /// during operations on the zip archive.  A console application may wish to
        /// pass <c>System.Console.Out</c> to get messages on the Console. A graphical
        /// or headless application may wish to capture the messages in a different
        /// <c>TextWriter</c>, such as a <c>System.IO.StringWriter</c>.
        /// </param>
        /// 
        /// <param name="encoding">
        /// The <c>System.Text.Encoding</c> to use when reading in the zip archive. Be
        /// careful specifying the encoding.  If the value you use here is not the same
        /// as the Encoding used when the zip archive was created (possibly by a
        /// different archiver) you will get unexpected results and possibly exceptions.
        /// </param>
        /// 
        /// <returns>The instance read from the zip archive.</returns>
        ///
        public static ZipFile Read(string fileName,
                                   TextWriter statusMessageWriter,
                                   System.Text.Encoding encoding,
                                   EventHandler<ReadProgressEventArgs> readProgress)
        {
            ZipFile zf = new ZipFile();
            zf.ProvisionalAlternateEncoding = encoding;
            zf._StatusMessageTextWriter = statusMessageWriter;
            zf._name = fileName;
            if (readProgress != null)
                zf.ReadProgress = readProgress;

            if (zf.Verbose) zf._StatusMessageTextWriter.WriteLine("reading from {0}...", fileName);

            try
            {
                ReadIntoInstance(zf);
                zf._fileAlreadyExists = true;
            }
            catch (Exception e1)
            {
                throw new ZipException(String.Format("{0} could not be read", fileName), e1);
            }
            return zf;
        }

        /// <summary>
        /// Reads a zip archive from a stream.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This is useful when when the zip archive content is available from an
        /// already-open stream. The stream must be open and readable when calling this
        /// method.  The stream is left open when the reading is completed.
        /// </para>
        ///
        /// <para>
        /// Using this overload, the stream is read using the default
        /// <c>System.Text.Encoding</c>, which is the <c>IBM437</c> codepage. If you
        /// want to specify the encoding to use when reading the zipfile content, check
        /// out the other overloads of the ZipFile constructor.
        /// </para>
        ///
        /// <para>
        /// Reading of zip content begins at the current position in the stream.  This
        /// means if you have a stream that concatenates regular data and zip data, if
        /// you position the open, readable stream at the start of the zip data, you
        /// will be able to read the zip archive using this constructor, or any of the
        /// ZipFile constructors that accept a <see cref="System.IO.Stream" /> as
        /// input. Some examples of where this might be useful: the zip content is
        /// concatenated at the end of a regular EXE file, as some self-extracting
        /// archives do.  (Note: SFX files produced by DotNetZip do not work this
        /// way). Another example might be a stream being read from a database, where
        /// the zip content is embedded within an aggregate stream of data.
        /// </para>
        /// </remarks>
        ///
        /// <example>
        /// <para>
        /// This example shows how to Read zip content from a stream, and extract one
        /// entry into a different stream. In this example, the filename
        /// "NameOfEntryInArchive.doc", refers only to the name of the entry within the
        /// zip archive.  A file by that name is not created in the filesystem.  The I/O
        /// is done strictly with the given streams.
        /// </para>
        /// 
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(InputStream))
        /// {
        ///    zip.Extract("NameOfEntryInArchive.doc", OutputStream);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip as ZipFile = ZipFile.Read(InputStream)
        ///    zip.Extract("NameOfEntryInArchive.doc", OutputStream)
        /// End Using
        /// </code>
        /// </example>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        ///
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(Stream zipStream)
        {
            return Read(zipStream, null, DefaultEncoding);
        }

        /// <summary>
        /// Reads a zip archive from a stream, with a given ReadProgress event handler.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// When opening large zip archives, you may want to display a progress bar or
        /// other indicator of status progress while reading.  This Read() method allows
        /// you to specify a ReadProgress Event Handler directly.  The stream is read
        /// using the default encoding (IBM437).  
        /// </para>
        ///
        /// <para>
        /// Reading of zip content begins at the current position in the stream.  This
        /// means if you have a stream that concatenates regular data and zip data, if
        /// you position the open, readable stream at the start of the zip data, you
        /// will be able to read the zip archive using this constructor, or any of the
        /// ZipFile constructors that accept a <see cref="System.IO.Stream" /> as
        /// input. Some examples of where this might be useful: the zip content is
        /// concatenated at the end of a regular EXE file, as some self-extracting
        /// archives do.  (Note: SFX files produced by DotNetZip do not work this
        /// way). Another example might be a stream being read from a database, where
        /// the zip content is embedded within an aggregate stream of data.
        /// </para>
        /// </remarks>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        ///
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile corresponding to the stream being read.</returns>
        public static ZipFile Read(Stream zipStream,
                                   EventHandler<ReadProgressEventArgs> readProgress)
        {
            return Read(zipStream, null, DefaultEncoding, readProgress);
        }


        /// <summary>
        /// Reads a zip archive from a stream, using the specified TextWriter for status
        /// messages.
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
        /// The stream is read using the default <c>System.Text.Encoding</c>, which is
        /// the <c>IBM437</c> codepage.  For more information on the encoding, see the
        /// <see cref="ProvisionalAlternateEncoding"/> property.
        /// </para>
        /// 
        /// <para>
        /// Reading of zip content begins at the current position in the stream.  This
        /// means if you have a stream that concatenates regular data and zip data, if
        /// you position the open, readable stream at the start of the zip data, you
        /// will be able to read the zip archive using this constructor, or any of the
        /// ZipFile constructors that accept a <see cref="System.IO.Stream" /> as
        /// input. Some examples of where this might be useful: the zip content is
        /// concatenated at the end of a regular EXE file, as some self-extracting
        /// archives do.  (Note: SFX files produced by DotNetZip do not work this
        /// way). Another example might be a stream being read from a database, where
        /// the zip content is embedded within an aggregate stream of data.
        /// </para>
        ///
        /// </remarks>
        /// 
        /// <exception cref="Ionic.Zip.ZipException">
        /// Thrown if zipStream is <c>null</c> (<c>Nothing</c> in VB).
        /// In this case, the inner exception is an ArgumentException.
        /// </exception>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written
        /// during operations on the <c>ZipFile</c>.  For example, in a console
        /// application, System.Console.Out works, and will get a message for each entry
        /// added to the ZipFile.  If the TextWriter is <c>null</c>, no verbose messages
        /// are written.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(Stream zipStream, TextWriter statusMessageWriter)
        {
            return Read(zipStream, statusMessageWriter, DefaultEncoding);
        }


        /// <summary>
        /// Reads a zip archive from a stream, using the specified TextWriter for status
        /// messages, and the specified ReadProgress event handler.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The stream is read using the default <c>System.Text.Encoding</c>, which is
        /// the <c>IBM437</c> codepage.  For more information on the encoding, see the
        /// <see cref="ProvisionalAlternateEncoding"/> property.
        /// </para>
        /// 
        /// <para>
        /// Reading of zip content begins at the current position in the stream.  This
        /// means if you have a stream that concatenates regular data and zip data, if
        /// you position the open, readable stream at the start of the zip data, you
        /// will be able to read the zip archive using this constructor, or any of the
        /// ZipFile constructors that accept a <see cref="System.IO.Stream" /> as
        /// input. Some examples of where this might be useful: the zip content is
        /// concatenated at the end of a regular EXE file, as some self-extracting
        /// archives do.  (Note: SFX files produced by DotNetZip do not work this
        /// way). Another example might be a stream being read from a database, where
        /// the zip content is embedded within an aggregate stream of data.
        /// </para>
        /// </remarks>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written
        /// during operations on the <c>ZipFile</c>.  For example, in a console
        /// application, System.Console.Out works, and will get a message for each entry
        /// added to the ZipFile.  If the TextWriter is <c>null</c>, no verbose messages
        /// are written.
        /// </param>
        /// 
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(Stream zipStream,
                                   TextWriter statusMessageWriter,
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
        ///
        /// <para>
        /// Reading of zip content begins at the current position in the stream.  This
        /// means if you have a stream that concatenates regular data and zip data, if
        /// you position the open, readable stream at the start of the zip data, you
        /// will be able to read the zip archive using this constructor, or any of the
        /// ZipFile constructors that accept a <see cref="System.IO.Stream" /> as
        /// input. Some examples of where this might be useful: the zip content is
        /// concatenated at the end of a regular EXE file, as some self-extracting
        /// archives do.  (Note: SFX files produced by DotNetZip do not work this
        /// way). Another example might be a stream being read from a database, where
        /// the zip content is embedded within an aggregate stream of data.
        /// </para>
        /// </remarks>
        ///
        /// <exception cref="Ionic.Zip.ZipException">
        /// Thrown if zipStream is <c>null</c> (<c>Nothing</c> in VB).
        /// In this case, the inner exception is an ArgumentException.
        /// </exception>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        /// 
        /// <param name="encoding">
        /// The text encoding to use when reading entries that do not have the UTF-8
        /// encoding bit set.  Be careful specifying the encoding.  If the value you use
        /// here is not the same as the Encoding used when the zip archive was created
        /// (possibly by a different archiver) you will get unexpected results and
        /// possibly exceptions.  See the <see cref="ProvisionalAlternateEncoding"/>
        /// property for more information.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(Stream zipStream, System.Text.Encoding encoding)
        {
            return Read(zipStream, null, encoding);
        }

        /// <summary>
        /// Reads a zip archive from a stream, using the specified encoding, and
        /// and the specified ReadProgress event handler.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Reading of zip content begins at the current position in the stream.  This
        /// means if you have a stream that concatenates regular data and zip data, if
        /// you position the open, readable stream at the start of the zip data, you
        /// will be able to read the zip archive using this constructor, or any of the
        /// ZipFile constructors that accept a <see cref="System.IO.Stream" /> as
        /// input. Some examples of where this might be useful: the zip content is
        /// concatenated at the end of a regular EXE file, as some self-extracting
        /// archives do.  (Note: SFX files produced by DotNetZip do not work this
        /// way). Another example might be a stream being read from a database, where
        /// the zip content is embedded within an aggregate stream of data.
        /// </para>
        /// </remarks>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        /// 
        /// <param name="encoding">
        /// The text encoding to use when reading entries that do not have the UTF-8
        /// encoding bit set.  Be careful specifying the encoding.  If the value you use
        /// here is not the same as the Encoding used when the zip archive was created
        /// (possibly by a different archiver) you will get unexpected results and
        /// possibly exceptions.  See the <see cref="ProvisionalAlternateEncoding"/>
        /// property for more information.
        /// </param>
        /// 
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(Stream zipStream,
                                   System.Text.Encoding encoding,
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
        /// This method is useful when when the zip archive content is available from an
        /// already-open stream. The stream must be open and readable when calling this
        /// method.  The stream is left open when the reading is completed.
        /// </para>
        ///
        /// <para>
        /// Reading of zip content begins at the current position in the stream.  This
        /// means if you have a stream that concatenates regular data and zip data, if
        /// you position the open, readable stream at the start of the zip data, you
        /// will be able to read the zip archive using this constructor, or any of the
        /// ZipFile constructors that accept a <see cref="System.IO.Stream" /> as
        /// input. Some examples of where this might be useful: the zip content is
        /// concatenated at the end of a regular EXE file, as some self-extracting
        /// archives do.  (Note: SFX files produced by DotNetZip do not work this
        /// way). Another example might be a stream being read from a database, where
        /// the zip content is embedded within an aggregate stream of data.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <exception cref="Ionic.Zip.ZipException">
        /// Thrown if zipStream is <c>null</c> (<c>Nothing</c> in VB).
        /// In this case, the inner exception is an ArgumentException.
        /// </exception>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        ///
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written
        /// during operations on the <c>ZipFile</c>.  For example, in a console
        /// application, System.Console.Out works, and will get a message for each entry
        /// added to the ZipFile.  If the TextWriter is <c>null</c>, no verbose messages
        /// are written.
        /// </param>
        ///
        /// <param name="encoding">
        /// The text encoding to use when reading entries that do not have the UTF-8
        /// encoding bit set.  Be careful specifying the encoding.  If the value you use
        /// here is not the same as the Encoding used when the zip archive was created
        /// (possibly by a different archiver) you will get unexpected results and
        /// possibly exceptions.  See the <see cref="ProvisionalAlternateEncoding"/>
        /// property for more information.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(Stream zipStream,
                   TextWriter statusMessageWriter,
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
        /// <remarks>
        /// <para>
        /// Reading of zip content begins at the current position in the stream.  This
        /// means if you have a stream that concatenates regular data and zip data, if
        /// you position the open, readable stream at the start of the zip data, you
        /// will be able to read the zip archive using this constructor, or any of the
        /// ZipFile constructors that accept a <see cref="System.IO.Stream" /> as
        /// input. Some examples of where this might be useful: the zip content is
        /// concatenated at the end of a regular EXE file, as some self-extracting
        /// archives do.  (Note: SFX files produced by DotNetZip do not work this
        /// way). Another example might be a stream being read from a database, where
        /// the zip content is embedded within an aggregate stream of data.
        /// </para>
        /// </remarks>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        ///
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written
        /// during operations on the <c>ZipFile</c>.  For example, in a console
        /// application, System.Console.Out works, and will get a message for each entry
        /// added to the ZipFile.  If the TextWriter is <c>null</c>, no verbose messages
        /// are written.
        /// </param>
        ///
        /// <param name="encoding">
        /// The text encoding to use when reading entries that do not have the UTF-8
        /// encoding bit set.  Be careful specifying the encoding.  If the value you use
        /// here is not the same as the Encoding used when the zip archive was created
        /// (possibly by a different archiver) you will get unexpected results and
        /// possibly exceptions.  See the <see cref="ProvisionalAlternateEncoding"/>
        /// property for more information.
        /// </param>
        /// 
        /// <param name="readProgress">
        /// An event handler for Read operations.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(Stream zipStream,
                                   TextWriter statusMessageWriter,
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
            if (zf.Verbose) zf._StatusMessageTextWriter.WriteLine("reading from stream...");

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
        /// <returns>
        /// an instance of ZipFile. The name on the <c>ZipFile</c> will be <c>null</c>
        /// (<c>Nothing</c> in VB).
        /// </returns>
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
        /// This method is useful when the data for the zipfile is contained in a byte
        /// array, for example when retrieving the data from a database or other
        /// non-filesystem store.  The default Text Encoding (IBM437) is used to read
        /// the zipfile data.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="buffer">the byte array containing the zip data.</param>
        ///
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written
        /// during operations on the <c>ZipFile</c>.  For example, in a console
        /// application, System.Console.Out works, and will get a message for each entry
        /// added to the ZipFile.  If the TextWriter is <c>null</c>, no verbose messages
        /// are written.
        /// </param>
        /// 
        /// <returns>
        /// an instance of ZipFile. The name is set to <c>null</c> (<c>Nothing</c> in VB).
        /// </returns>
        /// 
        public static ZipFile Read(byte[] buffer, TextWriter statusMessageWriter)
        {
            return Read(buffer, statusMessageWriter, DefaultEncoding);
        }


        /// <summary>
        /// Reads a zip archive from a byte array, using the given StatusMessageWriter and text Encoding.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method is useful when the data for the zipfile is contained in a byte
        /// array, for example when retrieving the data from a database or other
        /// non-filesystem store.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="buffer">the byte array containing the zip data.</param>
        ///
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written
        /// during operations on the <c>ZipFile</c>.  For example, in a console
        /// application, System.Console.Out works, and will get a message for each entry
        /// added to the ZipFile.  If the TextWriter is <c>null</c>, no verbose messages
        /// are written.
        /// </param>
        /// 
        /// <param name="encoding">
        /// The text encoding to use when reading entries that do not have the UTF-8
        /// encoding bit set.  Be careful specifying the encoding.  If the value you use
        /// here is not the same as the Encoding used when the zip archive was created
        /// (possibly by a different archiver) you will get unexpected results and
        /// possibly exceptions.  See the <see cref="ProvisionalAlternateEncoding"/>
        /// property for more information.
        /// </param>
        /// 
        /// <returns>
        /// an instance of ZipFile. The name is set to <c>null</c> (<c>Nothing</c> in VB).
        /// </returns>
        /// 
        public static ZipFile Read(byte[] buffer, TextWriter statusMessageWriter, System.Text.Encoding encoding)
        {
            ZipFile zf = new ZipFile();
            zf._StatusMessageTextWriter = statusMessageWriter;
            zf._provisionalAlternateEncoding = encoding;
            zf._readstream = new MemoryStream(buffer);
            zf._ReadStreamIsOurs = true;
            if (zf.Verbose) zf._StatusMessageTextWriter.WriteLine("reading from byte[]...");

            ReadIntoInstance(zf);
            return zf;
        }


        private static void ReadIntoInstance(ZipFile zf)
        {
            Stream s = zf.ReadStream;
            try
            {
                if (!s.CanSeek)
                {
                    ReadIntoInstance_Orig(zf);
                    return;
                }

                zf.OnReadStarted();

                // change for workitem 8098
                zf._originPosition = s.Position;

                // Try reading the central directory, rather than scanning the file. 

                uint datum = VerifyBeginningOfZipFile(s);

                if (datum == ZipConstants.EndOfCentralDirectorySignature)
                    return;


                // start at the end of the file...
                // seek backwards a bit, then look for the EoCD signature. 
                int nTries = 0;
                bool success = false;

                // The size of the end-of-central-directory-footer plus 2 bytes is 18.
                // This implies an archive comment length of 0.  We'll add a margin of
                // safety and start "in front" of that, when looking for the
                // EndOfCentralDirectorySignature
                long posn = s.Length - 64;
                long maxSeekback = Math.Max(s.Length - 0x4000, 10);
                do
                {
                    s.Seek(posn, SeekOrigin.Begin);
                    long bytesRead = SharedUtilities.FindSignature(s, (int)ZipConstants.EndOfCentralDirectorySignature);
                    if (bytesRead != -1)
                        success = true;
                    else
                    {
                        nTries++;
                        // Weird: with NETCF, negative offsets from SeekOrigin.End DO
                        // NOT WORK. So rather than seek a negative offset, we seek
                        // from SeekOrigin.Begin using a smaller number.
                        posn -= (32 * (nTries + 1) * nTries); // increasingly larger
                        if (posn < 0) posn = 0;  // BOF
                    }
                }
                while (!success && posn > maxSeekback);

                if (success)
                {
                    // workitem 8299
                    zf._locEndOfCDS = s.Position - 4;
                    byte[] block = new byte[16];
                    zf.ReadStream.Read(block, 0, block.Length);
                    int i = 12;

                    uint offset32 = (uint)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
                    if (offset32 == 0xFFFFFFFF)
                    {
                        Zip64SeekToCentralDirectory(zf);
                    }
                    else
                    {
                        // change for workitem 8098
                        //s.Seek(Offset32, SeekOrigin.Begin);
                        zf.SeekFromOrigin(offset32);
                    }

                    ReadCentralDirectory(zf);
                }
                else
                {
                    // Could not find the central directory.
                    // Fallback to the old method.
                    // workitem 8098: ok
                    s.Seek(zf._originPosition, SeekOrigin.Begin);
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



        private static void Zip64SeekToCentralDirectory(ZipFile zf)
        {
            Stream s = zf.ReadStream;

            byte[] block = new byte[16];

            // seek back to find the ZIP64 EoCD
            // I think this might not work for .NET CF ? 
            s.Seek(-40, SeekOrigin.Current);
            s.Read(block, 0, 16);

            Int64 Offset64 = BitConverter.ToInt64(block, 8);
            // change for workitem 8098
            //s.Seek(Offset64, SeekOrigin.Begin);
            zf.SeekFromOrigin(Offset64);

            uint datum = (uint)Ionic.Zip.SharedUtilities.ReadInt(s);
            if (datum != ZipConstants.Zip64EndOfCentralDirectoryRecordSignature)
                throw new BadReadException(String.Format("  ZipFile::Read(): Bad signature (0x{0:X8}) looking for ZIP64 EoCD Record at position 0x{1:X8}", datum, s.Position));

            s.Read(block, 0, 8);
            Int64 Size = BitConverter.ToInt64(block, 0);

            block = new byte[Size];
            s.Read(block, 0, block.Length);

            Offset64 = BitConverter.ToInt64(block, 36);
            // change for workitem 8098
            //s.Seek(Offset64, SeekOrigin.Begin);
            zf.SeekFromOrigin(Offset64);
        }


        private static uint VerifyBeginningOfZipFile(Stream s)
        {
            uint datum = (uint)Ionic.Zip.SharedUtilities.ReadInt(s);
            // workitem 8337
//             if (datum != ZipConstants.PackedToRemovableMedia              // weird edge case #1
//                 && datum != ZipConstants.ZipEntryDataDescriptorSignature  // weird edge case #2
//                 && datum != ZipConstants.ZipDirEntrySignature             // weird edge case #3 - DynaZip
//                 && datum != ZipConstants.ZipEntrySignature                // normal BOF marker
//                 && datum != ZipConstants.EndOfCentralDirectorySignature   // for zip file with no entries
//                 && (datum & 0x0000FFFF) != 0x00005A4D                     // PE/COFF BOF marker (for SFX)
//                 )
//             {
//                 //Console.WriteLine("WTF, datum = 0x{0:X8}", datum);
//                 throw new BadReadException(String.Format("  ZipFile::Read(): Bad signature (0x{0:X8}) at start of file at position 0x{1:X8}", datum, s.Position));
//             }
            return datum;
        }



        private static void ReadCentralDirectory(ZipFile zf)
        {
            ZipEntry de;
            while ((de = ZipEntry.ReadDirEntry(zf)) != null)
            {
                de.ResetDirEntry();
                zf.OnReadEntry(true, null);

                if (zf.Verbose)
                    zf.StatusMessageTextWriter.WriteLine("entry {0}", de.FileName);

                zf._entries.Add(de);
            }

            // workitem 8299
            if (zf._locEndOfCDS > 0)
                zf.SeekFromOrigin(zf._locEndOfCDS);
            ReadCentralDirectoryFooter(zf);

            if (zf.Verbose && !String.IsNullOrEmpty(zf.Comment))
                zf.StatusMessageTextWriter.WriteLine("Zip file Comment: {0}", zf.Comment);

            // We keep the read stream open after reading. 

            if (zf.Verbose)
                zf.StatusMessageTextWriter.WriteLine("read in {0} entries.", zf._entries.Count);

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
            while ((de = ZipEntry.ReadDirEntry(zf)) != null)
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

            // workitem 8299
            if (zf._locEndOfCDS > 0)
                zf.SeekFromOrigin(zf._locEndOfCDS);

            ReadCentralDirectoryFooter(zf);

            if (zf.Verbose && !String.IsNullOrEmpty(zf.Comment))
                zf.StatusMessageTextWriter.WriteLine("Zip file Comment: {0}", zf.Comment);

            // when finished slurping in the zip, close the read stream
            //zf.ReadStream.Close();

            zf.OnReadCompleted();

        }




        private static void ReadCentralDirectoryFooter(ZipFile zf)
        {
            Stream s = zf.ReadStream;
            int signature = Ionic.Zip.SharedUtilities.ReadSignature(s);

            byte[] block = null;

            if (signature == ZipConstants.Zip64EndOfCentralDirectoryRecordSignature)
            {
                // We have a ZIP64 EOCD
                // This data block is 4 bytes sig, 8 bytes size, 44 bytes fixed data, 
                // followed by a variable-sized extension block.  We have read the sig already.
                // 8 - datasize (64 bits)
                // 2 - version made by
                // 2 - version needed to extract
                // 4 - number of this disk
                // 4 - number of the disk with the start of the CD
                // 8 - total number of entries in the CD on this disk
                // 8 - total number of entries in the CD 
                // 8 - size of the CD
                // 8 - offset of the CD
                // -----------------------
                // 52 bytes

                block = new byte[8 + 44];
                s.Read(block, 0, block.Length);

                Int64 DataSize = BitConverter.ToInt64(block, 0);  // == 44 + the variable length

                if (DataSize < 44)
                    throw new ZipException("Bad DataSize in the ZIP64 Central Directory.");

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
                s.Seek(-4, SeekOrigin.Current);
                throw new BadReadException(String.Format("  ZipFile::Read(): Bad signature ({0:X8}) at position 0x{1:X8}",
                                                         signature, s.Position));
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


        // workitem 8098
        internal void SeekFromOrigin(long position)
        {
            this.ReadStream.Seek(position + _originPosition, SeekOrigin.Begin);
        }

        //internal long Origin
        //{
        //    get
        //    {
        //        return _originPosition;
        //    }
        //}

        internal long RelativeOffset
        {
            get
            {
                return this.ReadStream.Position - _originPosition;
            }
        }




        /// <summary>
        /// Checks the given file to see if it appears to be a valid zip file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Calling this method is equivalent to calling <see cref="IsZipFile(string,
        /// bool)"/> with the testExtract parameter set to false.
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
        /// Checks a file to see if it is a valid zip file.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This method opens the specified zip file, reads in the zip archive,
        /// verifying the ZIP metadata as it reads.  Then, if testExtract is true, this
        /// method extracts each entry in the archive, dumping all the bits.
        /// </para>
        /// 
        /// <para>
        /// If everything succeeds, then the method returns true.  If anything fails -
        /// for example if an incorrect signature or CRC is found, indicating a corrupt
        /// file, the the method returns false.  This method also returns false for a
        /// file that does not exist.
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
        /// <returns>true if the file contains a valid zip file.</returns>
        public static bool IsZipFile(string fileName, bool testExtract)
        {
            bool result = false;
            try
            {
                if (!File.Exists(fileName)) return false;

                using (var s = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    result = IsZipFile(s, testExtract);
                }
            }
            catch { }
            return result;
        }


        /// <summary>
        /// Checks a stream to see if it contains a valid zip archive.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This method reads the zip archive contained in the specified stream, verifying
        /// the ZIP metadata as it reads.  If testExtract is true, this method also extracts 
        /// each entry in the archive, dumping all the bits into <see cref="Stream.Null"/>.
        /// </para>
        /// 
        /// <para>
        /// If everything succeeds, then the method returns true.  If anything fails -
        /// for example if an incorrect signature or CRC is found, indicating a corrupt
        /// file, the the method returns false.  This method also returns false for a
        /// file that does not exist.
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
        ///
        /// <seealso cref="IsZipFile(string, bool)"/>
        ///
        /// <param name="stream">The stream to check.</param>
        /// <param name="testExtract">true if the caller wants to extract each entry.</param>
        /// <returns>true if the stream contains a valid zip archive.</returns>
        public static bool IsZipFile(Stream stream, bool testExtract)
        {
            bool result = false;
            try
            {
                if (!stream.CanRead) return false;

                var bitBucket = Stream.Null;

                using (ZipFile zip1 = ZipFile.Read(stream, null, System.Text.Encoding.GetEncoding("IBM437")))
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
            catch { }
            return result;
        }

    }
}
