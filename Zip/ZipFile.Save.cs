// ZipFile.Save.cs
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
// Time-stamp: <2009-August-25 12:06:53>
//
// ------------------------------------------------------------------
//
// This module defines the methods for Save operations on zip files.
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
        /// Saves the Zip archive to a file, specified by the Name property of the <c>ZipFile</c>. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The <c>ZipFile</c> instance is written to storage, typically a zip file in a
        /// filesystem, only when the caller calls <c>Save</c>.  The Save operation writes
        /// the zip content to a temporary file, and then renames the temporary file
        /// to the desired name. If necessary, this method will delete a pre-existing file
        /// before the rename.
        /// </para>
        ///
        /// <para> The <see cref="ZipFile.Name"/> property is specified either
        /// explicitly, or implicitly using one of the parameterized ZipFile
        /// constructors.  For COM Automation clients, the <c>Name</c> property must be
        /// set explicitly, because COM Automation clients cannot call parameterized
        /// constructors.  </para>
        ///
        /// <para>
        /// When using a filesystem file for the Zip output, it is possible to call
        /// <c>Save</c> multiple times on the <c>ZipFile</c> instance. With each call the zip
        /// content is re-written to the same output file.
        /// </para>
        ///
        /// <para>
        /// Data for entries that have been added to the <c>ZipFile</c> instance is written
        /// to the output when the <c>Save</c> method is called. This means that the input
        /// streams for those entries must be available at the time the application calls
        /// <c>Save</c>.  If, for example, the application adds entries with <c>AddEntry</c>
        /// using a dynamically-allocated <c>MemoryStream</c>, the memory stream must not
        /// have been disposed before the call to <c>Save</c>. See the <see
        /// cref="ZipEntry.InputStream"/> property for more discussion of the availability
        /// requirements of the input stream for an entry, and an approach for providing
        /// just-in-time stream lifecycle management.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.AddEntry(String, String, System.IO.Stream)"/>
        ///
        /// <exception cref="Ionic.Zip.BadStateException">
        /// Thrown if you haven't specified a location or stream for saving the zip,
        /// either in the constructor or by setting the Name property, or if you try to
        /// save a regular zip archive to a filename with a .exe extension.
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

                if (_name != null && _name.EndsWith(".exe") && !_SavingSfx)
                    throw new BadStateException("You specified an EXE for a plain zip file.");
                
                // check if modified, before saving. 
                if (!_contentsChanged) return;
                
                if (Verbose) StatusMessageTextWriter.WriteLine("saving....");

                // validate the number of entries
                if (_entries.Count >= 0xFFFF && _zip64 == Zip64Option.Never)
                    throw new ZipException("The number of entries is 65535 or greater. Consider setting the UseZip64WhenSaving property on the ZipFile instance.");

                {
                    // write an entry in the zip for each file
                    int n = 0;
                    foreach (ZipEntry e in _entries)
                    {
                        OnSaveEntry(n, e, true);
                        e.Write(WriteStream);
                        if (_saveOperationCanceled)
                            break;
                        e._zipfile = this;
                        n++;
                        OnSaveEntry(n, e, false);
                        if (_saveOperationCanceled)
                            break;

                        if (e.IncludedInMostRecentSave)
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
                if (_temporaryFileName != null && _name != null)
                {
                    // _temporaryFileName may remain null if we are writing to a stream.
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
                        File.Delete(_name);
                        OnSaveEvent(ZipProgressEventType.Saving_BeforeRenameTempArchive);
                        File.Move(_temporaryFileName, _name);
                        OnSaveEvent(ZipProgressEventType.Saving_AfterRenameTempArchive);
                    }
                    else
                        File.Move(_temporaryFileName, _name);

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
                if (File.Exists(_temporaryFileName))
                {
                    File.Delete(_temporaryFileName);
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
                        // workitem 7704
#if NETCF20
                        _writestream.Close();
#else
                        _writestream.Dispose();
#endif
                    }
                    catch { }
                }
                _writestream = null;
                RemoveTempFile();
                _temporaryFileName = null;
            }
        }


        /// <summary>
        /// Save the file to a new zipfile, with the given name. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method allows the application to explicitly specify the name of the zip
        /// file when saving. Use this when creating a new zip file, or when 
        /// updating a zip archive.  
        /// </para>
        /// 
        /// <para>
        /// An application can also save a zip archive in several places by calling this
        /// method multiple times in succession, with different filenames.
        /// </para>
        ///
        /// <para>
        /// The <c>ZipFile</c> instance is written to storage, typically a zip file in a
        /// filesystem, only when the caller calls <c>Save</c>.  The Save operation writes
        /// the zip content to a temporary file, and then renames the temporary file
        /// to the desired name. If necessary, this method will delete a pre-existing file
        /// before the rename.
        /// </para>
        ///
        /// </remarks>
        /// 
        /// <exception cref="System.ArgumentException">
        /// Thrown if you specify a directory for the filename.
        /// </exception>
        ///
        /// <param name="fileName">
        /// The name of the zip archive to save to. Existing files will 
        /// be overwritten with great prejudice.
        /// </param>
        ///
        /// <example>
        /// This example shows how to create and Save a zip file.
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// {
        ///   zip.AddDirectory(@"c:\reports\January");
        ///   zip.Save("January.zip");
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As New ZipFile()
        ///   zip.AddDirectory("c:\reports\January")
        ///   zip.Save("January.zip")
        /// End Using
        /// </code>
        ///
        /// </example>
        ///
        /// <example>
        /// This example shows how to update a zip file.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read("ExistingArchive.zip"))
        /// {
        ///   zip.AddFile("NewData.csv");
        ///   zip.Save("UpdatedArchive.zip");
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As ZipFile = ZipFile.Read("ExistingArchive.zip")
        ///   zip.AddFile("NewData.csv")
        ///   zip.Save("UpdatedArchive.zip")
        /// End Using
        /// </code>
        ///
        /// </example>
        public void Save(String fileName)
        {
            // Check for the case where we are re-saving a zip archive 
            // that was originally instantiated with a stream.  In that case, 
            // the _name will be null. If so, we set _writestream to null, 
            // which insures that we'll cons up a new WriteStream (with a filesystem
            // file backing it) in the Save() method.
            if (_name == null)
                _writestream = null;

            _name = fileName;
            if (Directory.Exists(_name))
                throw new ZipException("Bad Directory", new System.ArgumentException("That name specifies an existing directory. Please specify a filename.", "fileName"));
            _contentsChanged = true;
            _fileAlreadyExists = File.Exists(_name);
            Save();
        }


        /// <summary>
        /// Save the zip archive to the specified stream.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The <c>ZipFile</c> instance is written to storage - typically a zip file in a
        /// filesystem, but using this overload, the storage can eb anything accessible via
        /// a writable stream - only when the caller calls <c>Save</c>.
        /// </para>
        ///
        /// <para>
        /// Use this method to save the zip content to a stream directly.  A common
        /// scenario is an ASP.NET application that dynamically generates a zip file and
        /// allows the browser to download it. The application can call
        /// <c>Save(Response.OutputStream)</c> to write a zipfile directly to the output
        /// stream, without creating a zip file on the disk on the ASP.NET server.
        /// </para>
        ///
        /// </remarks>
        /// 
        /// <param name="outputStream">
        /// The <c>System.IO.Stream</c> to write to. It must be writable.
        /// </param>
        public void Save(Stream outputStream)
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
            // We need to keep track of the start and
            // Finish of the Central Directory Structure.

            // Cannot always use WriteStream.Length or Position; some streams do not 
            // support these. (eg, ASP.NET Response.OutputStream)
            // In those cases we have a CountingStream.

            var output = s as CountingStream;
            long Start = (output != null) ? output.BytesWritten : s.Position;

            foreach (ZipEntry e in _entries)
            {
                if (e.IncludedInMostRecentSave)
                    e.WriteCentralDirectoryEntry(s);  // this writes a ZipDirEntry corresponding to the ZipEntry
            }

            long Finish = (output != null) ? output.BytesWritten : s.Position;

            Int64 SizeOfCentralDirectory = Finish - Start;

            _NeedZip64CentralDirectory =
        _zip64 == Zip64Option.Always ||
        CountEntries() >= 0xFFFF ||
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



        private int CountEntries()
        {
            // cannot just emit _entries.Count, because some of the entries
            // may have been skipped.
            int count = 0;
            foreach (var entry in _entries)
                if (entry.IncludedInMostRecentSave) count++;
            return count;
        }

        
        private void WriteZip64EndOfCentralDirectory(Stream s,
                                                     long StartOfCentralDirectory,
                                                     long EndOfCentralDirectory)
        {
            const int bufferLength = 12 + 44 + 20;

            byte[] bytes = new byte[bufferLength];

            int i = 0;
            // signature
            byte[] sig = BitConverter.GetBytes(ZipConstants.Zip64EndOfCentralDirectoryRecordSignature);
            Array.Copy(sig, 0, bytes, i, 4);
            i+=4;
            
            // There is a possibility to include "Extensible" data in the zip64
            // end-of-central-dir record.  I cannot figure out what it might be used to
            // store, so the size of this record is always fixed.  Maybe it is used for
            // strong encryption data?  That is for another day.
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

            long numberOfEntries = CountEntries();
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
            sig = BitConverter.GetBytes(ZipConstants.Zip64EndOfCentralDirectoryLocatorSignature);
            Array.Copy(sig, 0, bytes, i, 4);
            i+=4;
            
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




        private void WriteCentralDirectoryFooter(Stream s,
                                                 long StartOfCentralDirectory,
                                                 long EndOfCentralDirectory)
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
            byte[] sig = BitConverter.GetBytes(ZipConstants.EndOfCentralDirectorySignature);
            Array.Copy(sig, 0, bytes, i, 4);
            i+=4;
            
            // number of this disk
            bytes[i++] = 0;
            bytes[i++] = 0;

            // number of the disk with the start of the central directory
            bytes[i++] = 0;
            bytes[i++] = 0;

            // handle ZIP64 extensions for the end-of-central-directory 
            if (CountEntries() >= 0xFFFF || _zip64 == Zip64Option.Always)
            {
                // the ZIP64 version.
                for (j = 0; j < 4; j++)
                    bytes[i++] = 0xFF;
            }
            else
            {
                int c = CountEntries();
                // the standard version.
                // total number of entries in the central dir on this disk
                bytes[i++] = (byte)(c & 0x00FF);
                bytes[i++] = (byte)((c & 0xFF00) >> 8);

                // total number of entries in the central directory
                bytes[i++] = (byte)(c & 0x00FF);
                bytes[i++] = (byte)((c & 0xFF00) >> 8);
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

    }
    
}
