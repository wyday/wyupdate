// ZipFile.Extract.cs
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
// Time-stamp: <2009-August-04 12:30:04>
//
// ------------------------------------------------------------------
//
// This module defines the methods for Extract operations on zip files.
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
        /// Extracts all of the items in the zip archive, to the specified path in the
        /// filesystem.  The path can be relative or fully-qualified.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This method will extract all entries in the <c>ZipFile</c> to the specified path. 
        /// </para>
        ///
        /// <para>
        /// If an extraction of a file from the zip archive would overwrite an existing
        /// file in the filesystem, the action taken is dictated by the
        /// ExtractExistingFile property, which overrides any setting you may have made
        /// on individual ZipEntry instances.  By default, if you have not set that
        /// property on the <c>ZipFile</c> instance, the entry will not be extracted,
        /// the existing file will not be overwritten and an exception will be
        /// thrown. To change this, set the property, or use the <see
        /// cref="ZipFile.ExtractAll(string, Ionic.Zip.ExtractExistingFileAction)" />
        /// overload that allows you to specify an ExtractExistingFileAction parameter.
        /// </para>
        ///
        /// <para>
        /// The action to take when an extract would overwrite an existing file applies
        /// to all entries.  If you want to set this on a per-entry basis, then you must
        /// use one of the <see cef="ZipEntry.Extract" >ZipEntry.Extract</see> methods.
        /// </para>
        ///
        /// <para>
        /// This method will send verbose output messages to the
        /// StatusMessageTextWriter, if it is set on the <c>ZipFile</c> instance.
        /// </para>
        ///
        /// <para>
        /// You may wish to take advantage of the <c>ExtractProgress</c> event.
        /// </para>
        ///
        /// <para>
        /// About Timestamps: When extracting a file entry from a zip archive, the
        /// extracted file gets the last modified time of the entry as stored in the
        /// archive. The archive may also store extended file timestamp information,
        /// including last accessed and created times. If these are present in the
        /// ZipEntry, then the extracted file will also get these times.
        /// </para>
        ///
        /// <para>
        /// A Directory entry is somewhat different. It will get the times as described
        /// for a file entry, but, if there are file entries in the zip archive that,
        /// when extracted, appear in the just-created directory, then when those file
        /// entries are extracted, the last modified and last accessed times of the
        /// directory will change, as a side effect.  The result is that after an
        /// extraction of a directory and a number of files within the directory, the
        /// last modified and last accessed timestamps on the directory will reflect the
        /// time that the last file was extracted into the directory, rather than the
        /// time stored in the zip archive for the directory.
        /// </para>
        ///
        /// <para>
        /// To compensate, when extracting an archive with <c>ExtractAll</c>, DotNetZip
        /// will extract all the file and directory entries as described above, but it
        /// will then make a second pass on the directories, and reset the times on the
        /// directories to reflect what is stored in the zip archive.
        /// </para>
        ///
        /// <para>
        /// This compensation is performed only within the context of an
        /// <c>ExtractAll</c>. If you call <c>ZipEntry.Extract</c> on a directory entry,
        /// the timestamps on directory in the filesystem will reflect the times stored
        /// in the zip.  If you then call <c>ZipEntry.Extract</c> on a file entry, which
        /// is extracted into the directory, the timestamps on the directory will be
        /// updated to the current time.
        /// </para>
        /// </remarks>
        ///
        /// <example>
        /// This example extracts all the entries in a zip archive file, to the
        /// specified target directory.  The extraction will overwrite any existing
        /// files silently.
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
        /// This method will send verbose output messages to the
        /// StatusMessageTextWriter, if it is set on the <c>ZipFile</c> instance.
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
        /// <param name="path">the path to which the contents of the zipfile are extracted.
        /// </param>
        /// <param name="wantOverwrite">true to overwrite any existing files on extraction
        /// </param>
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
        /// Extracts all of the items in the zip archive, to the specified path in the
        /// filesystem, using the specified behavior when extraction would overwrite an
        /// existing file.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para>
        /// This method will extract all entries in the <c>ZipFile</c> to the specified
        /// path.  For an extraction that would overwrite an existing file, the behavior
        /// is dictated by the extractExistingFile parameter, which overrides any
        /// setting you may have made on individual ZipEntry instances.
        /// </para>
        ///
        /// <para>
        /// The action to take when an extract would overwrite an existing file applies
        /// to all entries.  If you want to set this on a per-entry basis, then you must
        /// use one of the <see cef="ZipEntry.Extract" /> methods.
        /// </para>
        ///
        /// <para>
        /// Calling this method is equivalent to setting the <see
        /// cref="ExtractExistingFile"/> property and then calling <see
        /// cref="ExtractAll(String)"/>.
        /// </para>
        ///
        /// <para>
        /// This method will send verbose output messages to the
        /// StatusMessageTextWriter, if it is set on the <c>ZipFile</c> instance.
        /// </para>
        /// </remarks>
        ///
        /// <example>
        /// This example extracts all the entries in a zip archive file, to the
        /// specified target directory.  It does not overwrite any existing files.
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

                // workitem 8264: 
                // now, set times on directory entries, again.
                // The problem is, extracting a file changes the times on the parent
                // directory.  So after all files have been extracted, we have to
                // run through the directories again. 
                foreach (ZipEntry e in _entries)
                {
                    // check if it is a directory
                    if ((e.IsDirectory) || (e.FileName.EndsWith("/")))
                    {
                        string outputFile = (e.FileName.StartsWith("/"))
                            ? Path.Combine(path, e.FileName.Substring(1))
                            : Path.Combine(path, e.FileName);
                        
                        e._SetTimes(outputFile, false);
                    }
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
        /// A file corresponding to the entry named by the <c>fileName</c> parameter,
        /// including any relative qualifying path for the entry, is created at the
        /// specified directory.
        /// </para>
        ///
        /// <para>
        /// An entry that requires a password for extraction is extracted using the <see
        /// cref="ZipFile.Password"/> that is specified on the <c>ZipFile</c> instance,
        /// which overrides any Password you may have set directly on the
        /// <c>ZipEntry</c> instance. If you have not set the <see
        /// cref="ZipFile.Password"/> property, or if you have set it to <c>null</c>,
        /// and the entry requires a password for extraction, an Exception will be
        /// thrown.  An exception will also be thrown if the entry requires a password
        /// for extraction, and the password specified on the <c>ZipFile</c> instance
        /// does not match that required for the <c>ZipEntry</c>.
        /// </para>
        ///
        /// <para>
        /// For an extraction that would overwrite an existing file, the action taken is
        /// dictated by the <see cref="ZipFile.ExtractExistingFile" /> property, which
        /// overrides any setting you may have made on the individual ZipEntry instance,
        /// unless it is not the default "Throw" action.  If it is the default "Throw",
        /// then the action taken is that specified in the <see
        /// cref="ZipEntry.ExtractExistingFile" /> property on the <c>ZipEntry</c>
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
        /// The file to extract. It must be the exact name of the file, including the path
        /// contained in the archive, if any. The filename match is not case-sensitive by
        /// default; you can use the <c>CaseSensitiveRetrieval</c> property to change
        /// this behavior.
        /// </param>
        [Obsolete("Please use method ZipEntry.Extract()")]
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
        /// A file corresponding to the entry named by the <c>fileName</c> parameter,
        /// including any relative qualifying path for the entry, is created at the
        /// specified directory.
        /// </para>
        ///
        /// <para>
        /// An entry that requires a password for extraction is extracted using the <see
        /// cref="ZipFile.Password"/> that is specified on the <c>ZipFile</c> instance,
        /// which overrides any Password you may have set directly on the
        /// <c>ZipEntry</c> instance. If you have not set the <see
        /// cref="ZipFile.Password"/> property, or if you have set it to <c>null</c>,
        /// and the entry requires a password for extraction, an Exception will be
        /// thrown.  An exception will also be thrown if the entry requires a password
        /// for extraction, and the password specified on the <c>ZipFile</c> instance
        /// does not match that required for the <c>ZipEntry</c>.
        /// </para>
        ///
        /// <para>
        /// For an extraction that would overwrite an existing file, the action taken is
        /// dictated by the <see cref="ZipFile.ExtractExistingFile" /> property, which
        /// overrides any setting you may have made on the individual ZipEntry instance,
        /// unless it is not the default "Throw" action.  If it is the default "Throw",
        /// then the action taken is that specified in the <see
        /// cref="ZipEntry.ExtractExistingFile" /> property on the <c>ZipEntry</c>
        /// instance.
        /// </para>
        ///
        /// <para>
        /// The ExtractProgress event is invoked before and after extraction, if it has
        /// been set.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="entryName">
        /// the name of the entry to extract. It must be the exact filename, including
        /// the path specified on the entry in the archive, if any. The match is not
        /// case-sensitive by default; you can use the <c>CaseSensitiveRetrieval</c>
        /// property to change this behavior.
        /// </param>
        /// <param name="directoryName">
        /// the directory into which to extract. It will be created 
        /// if it does not exist.
        /// </param>
        [Obsolete("Please use method ZipEntry.Extract(string)")]
        public void Extract(string entryName, string directoryName)
        {
            ZipEntry e = this[entryName];
            if (this.ExtractExistingFile != ExtractExistingFileAction.Throw)
                e.ExtractExistingFile = this.ExtractExistingFile;
            e.Password = _Password; // possibly null
            e.Extract(directoryName);
        }



        /// <summary>
        /// Extract a single item from the archive to the current working directory,
        /// potentially overwriting any existing file in the filesystem by the same
        /// name.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// A file corresponding to the entry named by the <c>fileName</c> parameter,
        /// including any relative qualifying path for the entry, is created at the
        /// current working directory.
        /// </para>
        ///
        /// <para>
        /// An entry that requires a password for extraction is extracted using the <see
        /// cref="ZipFile.Password"/> that is specified on the <c>ZipFile</c> instance,
        /// which overrides any Password you may have set directly on the
        /// <c>ZipEntry</c> instance. If you have not set the <see
        /// cref="ZipFile.Password"/> property, or if you have set it to <c>null</c>,
        /// and the entry requires a password for extraction, an Exception will be
        /// thrown.  An exception will also be thrown if the entry requires a password
        /// for extraction, and the password specified on the <c>ZipFile</c> instance
        /// does not match that required for the <c>ZipEntry</c>.
        /// </para>
        ///
        /// <para>
        /// The ExtractProgress event is invoked before and after extraction, if it has
        /// been set.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.CaseSensitiveRetrieval"/>
        /// <seealso cref="Ionic.Zip.ZipFile.Extract(String,ExtractExistingFileAction)"/>
        ///
        /// <param name="entryName">
        /// The name of the entry to extract. It must be the exact name, including the
        /// path specified on the entry in the archive, if any. The match is not
        /// case-sensitive by default; you can use the <c>CaseSensitiveRetrieval</c>
        /// property to change this behavior.  The path, if any, can use forward-slashes
        /// or backward slashes.
        /// </param>
        ///
        /// <param name="wantOverwrite">
        /// True if the caller wants to overwrite any existing files by the given name.
        /// </param>
        [Obsolete("Please use method ZipEntry.Extract(ExtractExistingFileAction)")]
        public void Extract(string entryName, bool wantOverwrite)
        {
            ZipEntry e = this[entryName];
            // legacy behavior
            e.ExtractExistingFile = (wantOverwrite)
            ? ExtractExistingFileAction.OverwriteSilently
            : ExtractExistingFileAction.Throw;
            e.Password = _Password; // possibly null
            e.Extract(Directory.GetCurrentDirectory());
        }



        /// <summary>
        /// Extract a single item from the archive to the current working directory,
        /// potentially overwriting any existing file in the filesystem by the same
        /// name.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Using this method, the entry is extracted using the Password that is
        /// specified on the <c>ZipFile</c> instance. If you have not set the Password
        /// property, then the password is <c>null</c>, and the entry is extracted with
        /// no password.  The file, including any relative qualifying path, is created
        /// at the current working directory.
        /// </para>
        ///
        /// <para>
        /// For an extraction that would overwrite an existing file, the action taken is
        /// dictated by the extractExistingFile parameter, which overrides any setting
        /// you may have made on the individual ZipEntry instance.  To avoid this, use
        /// one of the <c>ZipEntry.Extract</c> methods.
        /// </para>
        ///
        /// <para>
        /// The ExtractProgress event is invoked before and after extraction, if it has
        /// been set.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.CaseSensitiveRetrieval"/>
        ///
        /// <param name="entryName">
        /// The name of the entry to extract. It must be the exact name, including the
        /// path specified on the entry in the archive, if any. The match is not
        /// case-sensitive by default; you can use the <c>CaseSensitiveRetrieval</c>
        /// property to change this behavior.  The path, if any, can use forward-slashes
        /// or backward slashes.
        /// </param>
        ///
        /// <param name="extractExistingFile">
        /// The action to take if extraction would overwrite an existing file.
        /// </param>
        [Obsolete("Please use method ZipEntry.Extract(ExtractExistingFileAction)")]
        public void Extract(string entryName, ExtractExistingFileAction extractExistingFile)
        {
            ZipEntry e = this[entryName];
            e.ExtractExistingFile = extractExistingFile;
            e.Password = _Password; // possibly null
            e.Extract(Directory.GetCurrentDirectory());
        }


        /// <summary>
        /// Extract a single item from the archive, into the specified directory,
        /// potentially overwriting any existing file in the filesystem by the same
        /// name.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// A file corresponding to the entry named by the <c>fileName</c> parameter,
        /// including any relative qualifying path for the entry, is created at the
        /// specified directory.
        /// </para>
        ///
        /// <para>
        /// An entry that requires a password for extraction is extracted using the <see
        /// cref="ZipFile.Password"/> that is specified on the <c>ZipFile</c> instance,
        /// which overrides any Password you may have set directly on the
        /// <c>ZipEntry</c> instance. If you have not set the <see
        /// cref="ZipFile.Password"/> property, or if you have set it to <c>null</c>,
        /// and the entry requires a password for extraction, an Exception will be
        /// thrown.  An exception will also be thrown if the entry requires a password
        /// for extraction, and the password specified on the <c>ZipFile</c> instance
        /// does not match that required for the <c>ZipEntry</c>.
        /// </para>
        ///
        /// <para>
        /// The ExtractProgress event is invoked before and after extraction, if it has
        /// been set.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="Extract(String, String, ExtractExistingFileAction)"/>
        /// 
        /// <param name="entryName">
        /// The name of the entry to extract. It must be the exact name, including the
        /// path specified on the entry in the archive, if any. The match is not
        /// case-sensitive by default; you can use the <c>CaseSensitiveRetrieval</c>
        /// property to change this behavior. The path, if any, can use forward-slashes
        /// or backward slashes.
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
        [Obsolete("Please use method ZipEntry.Extract(String,ExtractExistingFileAction)")]
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
        /// A file corresponding to the entry named by the <c>fileName</c> parameter,
        /// including any relative qualifying path for the entry, is created at the
        /// specified directory.
        /// </para>
        ///
        /// <para>
        /// An entry that requires a password for extraction is extracted using the <see
        /// cref="ZipFile.Password"/> that is specified on the <c>ZipFile</c> instance,
        /// which overrides any Password you may have set directly on the
        /// <c>ZipEntry</c> instance. If you have not set the <see
        /// cref="ZipFile.Password"/> property, or if you have set it to <c>null</c>,
        /// and the entry requires a password for extraction, an Exception will be
        /// thrown.  An exception will also be thrown if the entry requires a password
        /// for extraction, and the password specified on the <c>ZipFile</c> instance
        /// does not match that required for the <c>ZipEntry</c>.
        /// </para>
        ///
        /// <para>
        /// For an extraction that would overwrite an existing file, the action taken is
        /// dictated by the extractExistingFile parameter, which overrides any setting
        /// you may have made on the individual ZipEntry instance.  To avoid this, use
        /// one of the <c>ZipEntry.Extract</c> methods.
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
        [Obsolete("Please use method ZipEntry.Extract(string, ExtractExistingFileAction)")]
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
        /// cref="ZipFile.Password"/> that is specified on the <c>ZipFile</c> instance,
        /// which overrides any Password you may have set directly on the
        /// <c>ZipEntry</c> instance. If you have not set the <see
        /// cref="ZipFile.Password"/> property, or if you have set it to <c>null</c>,
        /// and the entry requires a password for extraction, an Exception will be
        /// thrown.  An exception will also be thrown if the entry requires a password
        /// for extraction, and the password specified on the <c>ZipFile</c> instance
        /// does not match that required for the <c>ZipEntry</c>.
        /// </para>
        ///
        /// <para>
        /// The ExtractProgress event is invoked before and after extraction, if it has
        /// been set.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="Ionic.Zip.ZipException">
        /// Thrown if the outputStream is not writable, or if the filename is
        /// <c>null</c> or empty. The inner exception is an ArgumentException in each
        /// case.
        /// </exception>
        ///
        /// <param name="entryName">
        /// the name of the entry to extract, including the path used in the archive, if
        /// any.  The match is not case-sensitive by default; you can use the
        /// <c>CaseSensitiveRetrieval</c> property to change this behavior. The
        /// application can specify pathnames using forward-slashes or backward slashes.
        /// </param>
        ///
        /// <param name="outputStream">
        /// the stream to which the extacted, decompressed file data is written. 
        /// The stream must be writable.
        /// </param>
        [Obsolete("Please use method ZipEntry.Extract(Stream)")]
        public void Extract(string entryName, Stream outputStream)
        {
            if (outputStream == null || !outputStream.CanWrite)
                throw new ZipException("Cannot extract.", new ArgumentException("The OutputStream must be a writable stream.", "outputStream"));

            if (String.IsNullOrEmpty(entryName))
                throw new ZipException("Cannot extract.", new ArgumentException("The file name must be neither null nor empty.", "entryName"));

            ZipEntry e = this[entryName];
            e.Password = _Password; // possibly null
            e.Extract(outputStream);
        }
        
    }
}
