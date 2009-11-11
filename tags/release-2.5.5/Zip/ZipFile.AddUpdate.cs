// ZipFile.AddUpdate.cs
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
// Time-stamp: <2009-October-29 07:45:36>
//
// ------------------------------------------------------------------
//
// This module defines the methods for Adding and Updating entries in
// the ZipFile.
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
        ///   Adds an item, either a file or a directory, to a zip file archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        ///   This method is handy if you are adding things to zip archive and don't
        ///   want to bother distinguishing between directories or files.  Any files are
        ///   added as single entries.  A directory added through this method is added
        ///   recursively: all files and subdirectories contained within the directory
        ///   are added to the <c>ZipFile</c>.
        /// </para>
        /// 
        /// <para>
        ///   The name of the item may be a relative path or a fully-qualified
        ///   path. Remember, the items contained in <c>ZipFile</c> instance get written
        ///   to the disk only when you call <see cref="ZipFile.Save()"/> or a similar
        ///   save method.
        /// </para>
        ///
        /// <para>
        ///   The directory name used for the file within the archive is the same as the
        ///   directory name (potentially a relative path) specified in the
        ///   <paramref name="fileOrDirectoryName"/>.
        /// </para>
        ///
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to the
        ///   <c>ZipEntry</c> added.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.AddFile(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.AddDirectory(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateItem(string)"/>
        ///
        /// <overloads>This method has two overloads.</overloads>
        /// <param name="fileOrDirectoryName">
        /// the name of the file or directory to add.</param>
        /// 
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry AddItem(string fileOrDirectoryName)
        {
            return AddItem(fileOrDirectoryName, null);
        }


        /// <summary>
        ///   Adds an item, either a file or a directory, to a zip file archive,
        ///   explicitly specifying the directory path to be used in the archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        ///   If adding a directory, the add is recursive on all files and
        ///   subdirectories contained within it.
        /// </para>
        /// <para>
        ///   The name of the item may be a relative path or a fully-qualified path.
        ///   The item added by this call to the <c>ZipFile</c> is not read from the
        ///   disk nor written to the zip file archive until the application calls
        ///   Save() on the <c>ZipFile</c>.
        /// </para>
        /// 
        /// <para>
        ///   This version of the method allows the caller to explicitly specify the
        ///   directory path to be used in the archive, which would override the
        ///   "natural" path of the filesystem file.
        /// </para>
        /// 
        /// <para>
        ///   Encryption will be used on the file data if the <c>Password</c> has been set on
        ///   the <c>ZipFile</c> object, prior to calling this method.
        /// </para>
        /// 
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to the
        ///   <c>ZipEntry</c> added.
        /// </para>
        ///
        /// </remarks>
        /// 
        /// <exception cref="System.IO.FileNotFoundException">
        ///   Thrown if the file or directory passed in does not exist. 
        /// </exception>
        ///
        /// <param name="fileOrDirectoryName">the name of the file or directory to add.
        /// </param>
        ///
        /// <param name="directoryPathInArchive">
        ///   The name of the directory path to use within the zip archive.  This path
        ///   need not refer to an extant directory in the current filesystem.  If the
        ///   files within the zip are later extracted, this is the path used for the
        ///   extracted file.  Passing <c>null</c> (<c>Nothing</c> in VB) will use the
        ///   path on the fileOrDirectoryName.  Passing the empty string ("") will
        ///   insert the item at the root path within the archive.
        /// </param>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.AddFile(string, string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.AddDirectory(string, string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateItem(string, string)"/>
        ///
        /// <example>
        ///   This example shows how to zip up a set of files into a flat hierarchy,
        ///   regardless of where in the filesystem the files originated. The resulting
        ///   zip archive will contain a toplevel directory named "flat", which itself
        ///   will contain files Readme.txt, MyProposal.docx, and Image1.jpg.  A
        ///   subdirectory under "flat" called SupportFiles will contain all the files
        ///   in the "c:\SupportFiles" directory on disk.
        /// 
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
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry AddItem(String fileOrDirectoryName, String directoryPathInArchive)
        {
            if (File.Exists(fileOrDirectoryName))
                return AddFile(fileOrDirectoryName, directoryPathInArchive);
            
            if (Directory.Exists(fileOrDirectoryName))
                return AddDirectory(fileOrDirectoryName, directoryPathInArchive);

            throw new FileNotFoundException(String.Format("That file or directory ({0}) does not exist!",
                                                          fileOrDirectoryName));
        }

        /// <summary>
        ///   Adds a File to a Zip file archive. 
        /// </summary>
        /// <remarks>
        ///
        /// <para>
        ///   This call collects metadata for the named file in the filesystem,
        ///   including the file attributes and the timestamp, and inserts that metadata
        ///   into the resulting ZipEntry.  Only when the application calls Save() on
        ///   the <c>ZipFile</c>, does DotNetZip read the file from the filesystem and
        ///   then write the content to the zip file archive.
        /// </para>
        ///
        /// <para>
        ///   This method will throw an exception if an entry with the same name already
        ///   exists in the <c>ZipFile</c>.
        /// </para>
        ///
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to the
        ///   <c>ZipEntry</c> added.
        /// </para>
        ///
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        ///   In this example, three files are added to a Zip archive. The ReadMe.txt
        ///   file will be placed in the root of the archive. The .png file will be
        ///   placed in a folder within the zip called photos\personal.  The pdf file
        ///   will be included into a folder within the zip called Desktop.
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
        ///   The name of the file to add. It should refer to a file in the filesystem.
        ///   The name of the file may be a relative path or a fully-qualified path.
        /// </param>
        /// <returns>The <c>ZipEntry</c> corresponding to the File added.</returns>
        public ZipEntry AddFile(string fileName)
        {
            return AddFile(fileName, null);
        }





        /// <summary>
        ///   Adds a File to a Zip file archive, potentially overriding the path to be
        ///   used within the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        ///   The file added by this call to the <c>ZipFile</c> is not written to the
        ///   zip file archive until the application calls Save() on the <c>ZipFile</c>.
        /// </para>
        /// 
        /// <para>
        ///   This method will throw an exception if an entry with the same name already
        ///   exists in the <c>ZipFile</c>.
        /// </para>
        ///
        /// <para>
        ///   This version of the method allows the caller to explicitly specify the
        ///   directory path to be used in the archive.
        /// </para>
        /// 
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to the
        ///   <c>ZipEntry</c> added.
        /// </para>
        ///
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        ///   In this example, three files are added to a Zip archive. The ReadMe.txt
        ///   file will be placed in the root of the archive. The .png file will be
        ///   placed in a folder within the zip called images.  The pdf file will be
        ///   included into a folder within the zip called files\docs, and will be
        ///   encrypted with the given password.
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
        ///   The name of the file to add.  The name of the file may be a relative path
        ///   or a fully-qualified path.
        /// </param>
        ///
        /// <param name="directoryPathInArchive">
        ///   Specifies a directory path to use to override any path in the fileName.
        ///   This path may, or may not, correspond to a real directory in the current
        ///   filesystem.  If the files within the zip are later extracted, this is the
        ///   path used for the extracted file.  Passing <c>null</c> (<c>Nothing</c> in
        ///   VB) will use the path on the fileName, if any.  Passing the empty string
        ///   ("") will insert the item at the root path within the archive.
        /// </param>
        ///
        /// <returns>The <c>ZipEntry</c> corresponding to the file added.</returns>
        public ZipEntry AddFile(string fileName, String directoryPathInArchive)
        {
            string nameInArchive = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
            ZipEntry ze = ZipEntry.CreateFromFile(fileName, nameInArchive);
            if (Verbose) StatusMessageTextWriter.WriteLine("adding {0}...", fileName);
            return _InternalAddEntry(ze);
        }


        /// <summary>
        ///   This method removes a collection of entries from the <c>ZipFile</c>.
        /// </summary>
        ///
        /// <param name="entriesToRemove">
        ///   A collection of ZipEntry instances from this zip file to be removed. For
        ///   example, you can pass in an array of ZipEntry instances; or you can call
        ///   SelectEntries(), and then add or remove entries from that
        ///   ICollection&lt;ZipEntry&gt; (ICollection(Of ZipEntry) in VB), and pass
        ///   that ICollection to this method.
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
        ///   This method removes a collection of entries from the <c>ZipFile</c>, by name.
        /// </summary>
        ///
        /// <param name="entriesToRemove">
        ///   A collection of strings that refer to names of entries to be removed from
        ///   the <c>ZipFile</c>.  For example, you can pass in an array or a List of Strings
        ///   that provide the names of entries to be removed. 
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
        ///   This method adds a set of files to the <c>ZipFile</c>.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   Use this method to add a set of files to the zip archive, in one call.
        ///   For example, a list of files received from
        ///   <c>System.IO.Directory.GetFiles()</c> can be added to a zip archive in one
        ///   call.
        /// </para>
        ///
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to each
        ///   ZipEntry added.
        /// </para>
        /// </remarks>
        ///
        /// <param name="fileNames">
        ///   The collection of names of the files to add. Each string should refer to a
        ///   file in the filesystem. The name of the file may be a relative path or a
        ///   fully-qualified path.
        /// </param>
        ///
        /// <example>
        ///   This example shows how to create a zip file, and add a few files into it. 
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
        public void AddFiles(System.Collections.Generic.IEnumerable<String> fileNames)
        {
            this.AddFiles(fileNames, null);
        }


        /// <summary>
        ///   Adds or updates a set of files in the <c>ZipFile</c>.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   Any files that already exist in the archive are updated. Any files that
        ///   don't yet exist in the archive are added.
        /// </para>
        ///
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to each
        ///   ZipEntry added.
        /// </para>
        /// </remarks>
        ///
        /// <param name="fileNames">
        ///   The collection of names of the files to update. Each string should refer to a file in 
        ///   the filesystem. The name of the file may be a relative path or a fully-qualified path. 
        /// </param>
        ///
        public void UpdateFiles(System.Collections.Generic.IEnumerable<String> fileNames)
        {
            this.UpdateFiles(fileNames, null);
        }


        /// <summary>
        ///   Adds a set of files to the <c>ZipFile</c>, using the
        ///   specified directory path in the archive.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   Any directory structure that may be present in the
        ///   filenames contained in the list is "flattened" in the
        ///   archive.  Each file in the list is added to the archive in
        ///   the specified top-level directory.
        /// </para>
        ///
        /// <para>
        ///   For <c>ZipFile</c> properties including <see
        ///   cref="Encryption"/>, <see cref="Password"/>, <see
        ///   cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see
        ///   cref="ExtractExistingFile"/>, <see
        ///   cref="ZipErrorAction"/>, and <see
        ///   cref="CompressionLevel"/>, their respective values at the
        ///   time of this call will be applied to each ZipEntry added.
        /// </para>
        /// </remarks>
        ///
        /// <param name="fileNames">
        ///   The names of the files to add. Each string should refer to
        ///   a file in the filesystem.  The name of the file may be a
        ///   relative path or a fully-qualified path.
        /// </param>
        ///
        /// <param name="directoryPathInArchive">
        ///   Specifies a directory path to use to override any path in the file name.
        ///   Th is path may, or may not, correspond to a real directory in the current
        ///   filesystem.  If the files within the zip are later extracted, this is the
        ///   path used for the extracted file.  Passing <c>null</c> (<c>Nothing</c> in
        ///   VB) will use the path on each of the <c>fileNames</c>, if any.  Passing
        ///   the empty string ("") will insert the item at the root path within the
        ///   archive.
        /// </param>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.AddSelectedFiles(String, String)" />
        public void AddFiles(System.Collections.Generic.IEnumerable<String> fileNames, String directoryPathInArchive)
        {
            AddFiles(fileNames, false, directoryPathInArchive);
        }



        /// <summary>
        ///   Adds a set of files to the <c>ZipFile</c>, using the specified directory
        ///   path in the archive, and preserving the full directory structure in the
        ///   filenames.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   If preserveDirHierarchy is true, any directory structure present in the
        ///   filenames contained in the list is preserved in the archive.  On the other
        ///   hand, if preserveDirHierarchy is false, any directory structure that may
        ///   be present in the filenames contained in the list is "flattened" in the
        ///   archive; Each file in the list is added to the archive in the specified
        ///   top-level directory.
        /// </para>
        /// 
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to each
        ///   ZipEntry added.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <param name="fileNames">
        ///   The names of the files to add. Each string should refer to a file in the
        ///   filesystem.  The name of the file may be a relative path or a
        ///   fully-qualified path.
        /// </param>
        ///
        /// <param name="directoryPathInArchive">
        ///   Specifies a directory path to use to override any path in the file name.
        ///   This path may, or may not, correspond to a real directory in the current
        ///   filesystem.  If the files within the zip are later extracted, this is the
        ///   path used for the extracted file.  Passing <c>null</c> (<c>Nothing</c> in
        ///   VB) will use the path on each of the <c>fileNames</c>, if any.  Passing
        ///   the empty string ("") will insert the item at the root path within the
        ///   archive.
        /// </param>
        ///
        /// <param name="preserveDirHierarchy">
        ///   whether the entries in the zip archive will reflect the directory
        ///   hierarchy that is present in the various filenames.  For example, if <paramref name="fileNames"/>
        ///   includes two paths, \Animalia\Chordata\Mammalia\Info.txt and
        ///   \Plantae\Magnoliophyta\Dicotyledon\Info.txt, then calling this method with
        ///   <paramref name="preserveDirHierarchy"/> = <c>false</c> will result in an
        ///   exception because of a duplicate entry name, while calling this method
        ///   with <paramref name="preserveDirHierarchy"/> = <c>true</c> will result in the
        ///   full direcory paths being included in the entries added to the ZipFile.
        /// </param>
        /// <seealso cref="Ionic.Zip.ZipFile.AddSelectedFiles(String, String)" />
        public void AddFiles(System.Collections.Generic.IEnumerable<String> fileNames,
                             bool preserveDirHierarchy,
                             String directoryPathInArchive)
        {
            OnAddStarted();
            if (preserveDirHierarchy)
            {
                foreach (var f in fileNames)
                {
                    if (directoryPathInArchive != null)
                    {
                        string s = SharedUtilities.NormalizePath(Path.Combine(directoryPathInArchive, Path.GetDirectoryName(f)));
                        this.AddFile(f, s);
                    }
                    else
                        this.AddFile(f, null);
                }
            }
            else
            {
                foreach (var f in fileNames)
                    this.AddFile(f, directoryPathInArchive);

            }
            OnAddCompleted();
        }


        /// <summary>
        ///   Adds or updates a set of files to the <c>ZipFile</c>, using the specified
        ///   directory path in the archive.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para>
        ///   Any files that already exist in the archive are updated. Any files that
        ///   don't yet exist in the archive are added.
        /// </para>
        ///
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to each
        ///   ZipEntry added.
        /// </para>
        /// </remarks>
        ///
        /// <param name="fileNames">
        ///   The names of the files to add or update. Each string should refer to a
        ///   file in the filesystem.  The name of the file may be a relative path or a
        ///   fully-qualified path.
        /// </param>
        ///
        /// <param name="directoryPathInArchive">
        ///   Specifies a directory path to use to override any path in the file name.
        ///   This path may, or may not, correspond to a real directory in the current
        ///   filesystem.  If the files within the zip are later extracted, this is the
        ///   path used for the extracted file.  Passing <c>null</c> (<c>Nothing</c> in
        ///   VB) will use the path on each of the <c>fileNames</c>, if any.  Passing
        ///   the empty string ("") will insert the item at the root path within the
        ///   archive.
        /// </param>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.AddSelectedFiles(String, String)" />
        public void UpdateFiles(System.Collections.Generic.IEnumerable<String> fileNames, String directoryPathInArchive)
        {
            OnAddStarted();
            foreach (var f in fileNames)
                this.UpdateFile(f, directoryPathInArchive);
            OnAddCompleted();
        }




        /// <summary>
        ///   Adds or Updates a File in a Zip file archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        ///   This method adds a file to a zip archive, or, if the file already exists
        ///   in the zip archive, this method Updates the content of that given filename
        ///   in the zip archive.  The <c>UpdateFile</c> method might more accurately be
        ///   called "AddOrUpdateFile".
        /// </para>
        ///
        /// <para>
        ///   Upon success, there is no way for the application to learn whether the file
        ///   was added versus updated.
        /// </para>
        ///
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to the
        ///   <c>ZipEntry</c> added.
        /// </para>
        /// </remarks>
        ///
        /// <example>
        ///
        ///   This example shows how to Update an existing entry in a zipfile. The first
        ///   call to UpdateFile adds the file to the newly-created zip archive.  The
        ///   second call to UpdateFile updates the content for that file in the zip
        ///   archive.
        ///
        /// <code>
        /// using (ZipFile zip1 = new ZipFile())
        /// {
        ///   // UpdateFile might more accurately be called "AddOrUpdateFile"
        ///   zip1.UpdateFile("MyDocuments\\Readme.txt");
        ///   zip1.UpdateFile("CustomerList.csv");
        ///   zip1.Comment = "This zip archive has been created.";
        ///   zip1.Save("Content.zip");
        /// }
        /// 
        /// using (ZipFile zip2 = ZipFile.Read("Content.zip"))
        /// {
        ///   zip2.UpdateFile("Updates\\Readme.txt");
        ///   zip2.Comment = "This zip archive has been updated: The Readme.txt file has been changed.";
        ///   zip2.Save();
        /// }
        ///
        /// </code>
        /// <code lang="VB">
        ///   Using zip1 As New ZipFile
        ///       ' UpdateFile might more accurately be called "AddOrUpdateFile"
        ///       zip1.UpdateFile("MyDocuments\Readme.txt")
        ///       zip1.UpdateFile("CustomerList.csv")
        ///       zip1.Comment = "This zip archive has been created."
        ///       zip1.Save("Content.zip")
        ///   End Using
        ///
        ///   Using zip2 As ZipFile = ZipFile.Read("Content.zip")
        ///       zip2.UpdateFile("Updates\Readme.txt")
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
        ///   The name of the file to add or update. It should refer to a file in the
        ///   filesystem.  The name of the file may be a relative path or a
        ///   fully-qualified path.
        /// </param>
        ///
        /// <returns>
        ///   The <c>ZipEntry</c> corresponding to the File that was added or updated.
        /// </returns>
        public ZipEntry UpdateFile(string fileName)
        {
            return UpdateFile(fileName, null);
        }



        /// <summary>
        ///   Adds or Updates a File in a Zip file archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        ///   This method adds a file to a zip archive, or, if the file already exists
        ///   in the zip archive, this method Updates the content of that given filename
        ///   in the zip archive.
        /// </para>
        /// 
        /// <para>
        ///   This version of the method allows the caller to explicitly specify the
        ///   directory path to be used in the archive.  The entry to be added or
        ///   updated is found by using the specified directory path, combined with the
        ///   basename of the specified filename.
        /// </para>
        /// 
        /// <para>
        ///   Upon success, there is no way for the application to learn if the file was
        ///   added versus updated.
        /// </para>
        ///
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to the
        ///   <c>ZipEntry</c> added.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="Ionic.Zip.ZipFile.AddFile(string,string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateDirectory(string,string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateItem(string,string)"/>
        ///
        /// <param name="fileName">
        ///   The name of the file to add or update. It should refer to a file in the
        ///   filesystem.  The name of the file may be a relative path or a
        ///   fully-qualified path.
        /// </param>
        ///
        /// <param name="directoryPathInArchive">
        ///   Specifies a directory path to use to override any path in the
        ///   <c>fileName</c>.  This path may, or may not, correspond to a real
        ///   directory in the current filesystem.  If the files within the zip are
        ///   later extracted, this is the path used for the extracted file.  Passing
        ///   <c>null</c> (<c>Nothing</c> in VB) will use the path on the
        ///   <c>fileName</c>, if any.  Passing the empty string ("") will insert the
        ///   item at the root path within the archive.
        /// </param>
        ///
        /// <returns>
        ///   The <c>ZipEntry</c> corresponding to the File that was added or updated.
        /// </returns>
        public ZipEntry UpdateFile(string fileName, String directoryPathInArchive)
        {
            // ideally this would all be transactional!
            var key = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
            if (this[key] != null)
                this.RemoveEntry(key);
            return this.AddFile(fileName, directoryPathInArchive);
        }





        /// <summary>
        ///   Add or update a directory in a zip archive.  
        /// </summary>
        ///
        /// <remarks>
        ///   If the specified directory does not exist in the archive, then this method
        ///   is equivalent to calling <c>AddDirectory()</c>.  If the specified
        ///   directory already exists in the archive, then this method updates any
        ///   existing entries, and adds any new entries. Any entries that are in the
        ///   zip archive but not in the specified directory, are left alone.  In other
        ///   words, the contents of the zip file will be a union of the previous
        ///   contents and the new files.
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateFile(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.AddDirectory(string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateItem(string)"/>
        ///
        /// <param name="directoryName">
        ///   The path to the directory to be added to the zip archive, or updated in
        ///   the zip archive.
        /// </param>
        /// 
        /// <returns>
        /// The <c>ZipEntry</c> corresponding to the Directory that was added or updated.
        /// </returns>
        public ZipEntry UpdateDirectory(string directoryName)
        {
            return UpdateDirectory(directoryName, null);
        }


        /// <summary>
        ///   Add or update a directory in the zip archive at the specified root
        ///   directory in the archive.
        /// </summary>
        ///
        /// <remarks>
        ///   If the specified directory does not exist in the archive, then this method
        ///   is equivalent to calling <c>AddDirectory()</c>.  If the specified
        ///   directory already exists in the archive, then this method updates any
        ///   existing entries, and adds any new entries. Any entries that are in the
        ///   zip archive but not in the specified directory, are left alone.  In other
        ///   words, the contents of the zip file will be a union of the previous
        ///   contents and the new files.
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateFile(string,string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.AddDirectory(string,string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateItem(string,string)"/>
        ///
        /// <param name="directoryName">
        ///   The path to the directory to be added to the zip archive, or updated in the
        ///   zip archive.
        /// </param>
        ///
        /// <param name="directoryPathInArchive">
        ///   Specifies a directory path to use to override any path in the
        ///   <c>directoryName</c>.  This path may, or may not, correspond to a real
        ///   directory in the current filesystem.  If the files within the zip are
        ///   later extracted, this is the path used for the extracted file.  Passing
        ///   <c>null</c> (<c>Nothing</c> in VB) will use the path on the
        ///   <c>directoryName</c>, if any.  Passing the empty string ("") will insert
        ///   the item at the root path within the archive.
        /// </param>
        /// 
        /// <returns>
        ///   The <c>ZipEntry</c> corresponding to the Directory that was added or updated.
        /// </returns>
        public ZipEntry UpdateDirectory(string directoryName, String directoryPathInArchive)
        {
            return this.AddOrUpdateDirectoryImpl(directoryName, directoryPathInArchive, AddOrUpdateAction.AddOrUpdate);
        }





        /// <summary>
        ///   Add or update a file or directory in the zip archive. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   This is useful when the application is not sure or does not care if the
        ///   item to be added is a file or directory, and does not know or does not
        ///   care if the item already exists in the <c>ZipFile</c>. Calling this method
        ///   is equivalent to calling <c>RemoveEntry()</c> if an entry by the same name
        ///   already exists, followed calling by <c>AddItem()</c>.
        /// </para>
        ///
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to the
        ///   <c>ZipEntry</c> added.
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
        ///   Add or update a file or directory.  
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   This method is useful when the application is not sure or does not care if
        ///   the item to be added is a file or directory, and does not know or does not
        ///   care if the item already exists in the <c>ZipFile</c>. Calling this method
        ///   is equivalent to calling <c>RemoveEntry()</c>, if an entry by that name
        ///   exists, and then calling <c>AddItem()</c>.
        /// </para>
        /// 
        /// <para>
        ///   This version of the method allows the caller to explicitly specify the
        ///   directory path to be used for the item being added to the archive.  The
        ///   entry or entries that are added or updated will use the specified
        ///   <c>DirectoryPathInArchive</c>. Extracting the entry from the archive will
        ///   result in a file stored in that directory path.
        /// </para>
        ///
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to the
        ///   <c>ZipEntry</c> added.
        /// </para>
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.AddItem(string, string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateFile(string, string)"/>
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateDirectory(string, string)"/>
        ///
        /// <param name="itemName">The path for the File or Directory to be added or updated.</param>
        /// <param name="directoryPathInArchive">
        ///   Specifies a directory path to use to override any path in the
        ///   <c>itemName</c>.  This path may, or may not, correspond to a real
        ///   directory in the current filesystem.  If the files within the zip are
        ///   later extracted, this is the path used for the extracted file.  Passing
        ///   <c>null</c> (<c>Nothing</c> in VB) will use the path on the
        ///   <c>itemName</c>, if any.  Passing the empty string ("") will insert the
        ///   item at the root path within the archive.
        /// </param>
        public void UpdateItem(string itemName, string directoryPathInArchive)
        {
            if (File.Exists(itemName))
                UpdateFile(itemName, directoryPathInArchive);

            else if (Directory.Exists(itemName))
                UpdateDirectory(itemName, directoryPathInArchive);

            else
                throw new FileNotFoundException(String.Format("That file or directory ({0}) does not exist!", itemName));
        }


    

        /// <summary>
        ///   Adds a named entry into the zip archive, taking content for the entry
        ///   from a string.
        /// </summary>
        ///
        /// <remarks>
        ///   Calling this method creates an entry using the given fileName and
        ///   directory path within the archive.  There is no need for a file by the
        ///   given name to exist in the filesystem; the name is used within the zip
        ///   archive only. The content for the entry is encoded using the default text
        ///   encoding (<see cref="System.Text.Encoding.Default"/>).
        /// </remarks>
        ///
        /// <param name="content">
        ///   The content of the file, should it be extracted from the zip.
        /// </param>
        ///
        /// <param name="entryName">
        ///   The name, including any path, to use for the entry within the archive.
        /// </param>
        ///
        /// <returns>The <c>ZipEntry</c> added.</returns>
        /// 
        /// <example>
        ///
        /// This example shows how to add an entry to the zipfile, using a string as
        /// content for that entry.
        ///
        /// <code lang="C#">
        /// string Content = "This string will be the content of the Readme.txt file in the zip archive.";
        /// using (ZipFile zip1 = new ZipFile())
        /// {
        ///   zip1.AddFile("MyDocuments\\Resume.doc", "files");
        ///   zip1.AddEntry("Readme.txt", Content); 
        ///   zip1.Comment = "This zip file was created at " + System.DateTime.Now.ToString("G");
        ///   zip1.Save("Content.zip");
        /// }
        /// 
        /// </code>
        /// <code lang="VB">
        /// Public Sub Run()
        ///   Dim Content As String = "This string will be the content of the Readme.txt file in the zip archive."
        ///   Using zip1 As ZipFile = New ZipFile
        ///     zip1.AddEntry("Readme.txt", Content)
        ///     zip1.AddFile("MyDocuments\Resume.doc", "files")
        ///     zip1.Comment = ("This zip file was created at " &amp; DateTime.Now.ToString("G"))
        ///     zip1.Save("Content.zip")
        ///   End Using
        /// End Sub
        /// </code>
        /// </example>
        public ZipEntry AddEntry(string entryName, string content)
        {
            return AddEntry(entryName, content, System.Text.Encoding.Default);
        }



        /// <summary>
        ///   Adds a named entry into the zip archive, taking content for the entry
        ///   from a string, and using the specified text encoding.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para>
        ///   Calling this method creates an entry using the given fileName and
        ///   directory path within the archive.  There is no need for a file by the
        ///   given name to exist in the filesystem; the name is used within the zip
        ///   archive only.
        /// </para>
        /// 
        /// <para>
        ///   The content for the entry, a string value, is encoded using the given text
        ///   encoding. No Byte-order-mark (BOM) is emitted into the file.
        /// </para>
        ///
        /// <para>
        ///   If you wish to create within a zip file a file entry with Unicode-encoded
        ///   content that includes a byte-order-mark, you can convert your string to a
        ///   byte array using the appropriate <see
        ///   cref="System.Text.Encoding.GetBytes(String)">System.Text.Encoding.GetBytes()</see>
        ///   method, then prepend to that byte array the output of <see
        ///   cref="System.Text.Encoding.GetPreamble()">System.Text.Encoding.GetPreamble()</see>,
        ///   and use the <see cref="AddEntry(string,byte[])"/> method, to add the
        ///   entry.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <param name="entryName">
        ///   The name, including any path, to use within the archive for the entry.
        /// </param>
        ///
        /// <param name="content">
        ///   The content of the file, should it be extracted from the zip.
        /// </param>
        ///
        /// <param name="encoding">
        ///   The text encoding to use when encoding the string. Be aware: This is
        ///   distinct from the text encoding used to encode the fileName, as specified
        ///   in <see cref="ProvisionalAlternateEncoding" />.
        /// </param>
        ///
        /// <returns>The <c>ZipEntry</c> added.</returns>
        /// 
        public ZipEntry AddEntry(string entryName, string content, System.Text.Encoding encoding)
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms, encoding);

            sw.Write(content);
            sw.Flush();

            // reset to allow reading later
            ms.Seek(0, SeekOrigin.Begin);

            return AddEntry(entryName, ms);
        }


        /// <summary>
        ///   Create an entry in the <c>ZipFile</c> using the given <c>Stream</c> as input.
        ///   The entry will have the given filename. 
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para>
        ///   The application can provide an open, readable stream; in this case it will
        ///   be read during the call to <see cref="ZipFile.Save()"/> or one of its
        ///   overloads.
        /// </para>
        ///
        /// <para>
        ///   In cases where a large number of streams will be added to the
        ///   <c>ZipFile</c>, the application may wish to avoid maintaining all of the
        ///   streams open simultaneously.  To handle this situation, the application
        ///   should use the <see cref="AddEntry(string, OpenDelegate, CloseDelegate)"/>
        ///   overload. 
        /// </para>
        ///
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to the
        ///   <c>ZipEntry</c> added.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <example>
        /// <para>
        ///   This example adds a single entry to a <c>ZipFile</c> via a <c>Stream</c>. 
        /// </para>
        ///
        /// <code lang="C#">
        /// String zipToCreate = "Content.zip";
        /// String fileNameInArchive = "Content-From-Stream.bin";
        /// using (System.IO.Stream streamToRead = MyStreamOpener())
        /// {
        ///   using (ZipFile zip = new ZipFile())
        ///   {
        ///     ZipEntry entry= zip.AddEntry(fileNameInArchive, streamToRead);
        ///     zip.AddFile("Readme.txt");
        ///     zip.Save(zipToCreate);  // the stream is read implicitly here
        ///   }
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim zipToCreate As String = "Content.zip"
        /// Dim fileNameInArchive As String = "Content-From-Stream.bin"
        /// Using streamToRead as System.IO.Stream = MyStreamOpener()
        ///   Using zip As ZipFile = New ZipFile()
        ///     Dim entry as ZipEntry = zip.AddEntry(fileNameInArchive, streamToRead)
        ///     zip.AddFile("Readme.txt")
        ///     zip.Save(zipToCreate)  '' the stream is read implicitly, here
        ///   End Using
        /// End Using
        /// </code>
        /// </example>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.UpdateEntry(string, System.IO.Stream)"/>
        ///
        /// <param name="entryName">
        ///   The name, including any path, which is shown in the zip file for the added
        ///   entry.
        /// </param>
        /// <param name="stream">The input stream from which to grab content for the file</param>
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry AddEntry(string entryName, Stream stream)
        {
            ZipEntry ze = ZipEntry.CreateForStream(entryName, stream);
            ze.SetEntryTimes(DateTime.Now,DateTime.Now,DateTime.Now);
            if (Verbose) StatusMessageTextWriter.WriteLine("adding {0}...", entryName);
            return _InternalAddEntry(ze);
        }

        

        /// <summary>
        ///   Add a ZipEntry for which content is written directly by the application.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   When the application needs to write the zip entry data, use this method to
        ///   add the ZipEntry.  For example, in the case that the application wishes to
        ///   write the XML representation of a DataSet into a ZipEntry, the application
        ///   can use this method to do so.
        /// </para>
        ///
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to the
        ///   <c>ZipEntry</c> added.
        /// </para>
        ///
        /// <para>
        ///   NB: With PKZip encryption, it's necessary to compute the CRC before
        ///   compressing or encrypting the data.  Therefore, when using PKZip
        ///   encryption with a WriteDelegate, the WriteDelegate will be called twice:
        ///   once to compute the CRC, and the second time to (potentially) compress and
        ///   encrypt. For each call of the delegate, your application must stream the
        ///   same entry data in its entirety. If your application writes different data
        ///   during the second call, it will result in a corrupt zip file.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <param name="entryName">name of the entry to add</param>
        /// <param name="writer">the delegate which will write the entry content</param>
        /// <returns>the ZipEntry added</returns>
        ///
        /// <example>
        ///
        ///   This example shows an application filling a DataSet, then saving the
        ///   contents of that DataSet as XML, into a ZipEntry in a ZipFile, using an
        ///   anonymous delegate in C#. The DataSet XML is never saved to a disk file. 
        ///
        /// <code lang="C#">
        /// var c1= new System.Data.SqlClient.SqlConnection(connstring1);
        /// var da = new System.Data.SqlClient.SqlDataAdapter()
        ///     {
        ///         SelectCommand=  new System.Data.SqlClient.SqlCommand(strSelect, c1)
        ///     };
        ///
        /// DataSet ds1 = new DataSet();
        /// da.Fill(ds1, "Invoices");
        ///
        /// using(Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile())
        /// {
        ///     zip.AddEntry(zipEntryName, (name,stream) => ds1.WriteXml(stream) );
        ///     zip.Save(zipFileName);
        /// }
        /// </code>
        /// </example>
        ///
        /// <example>
        ///
        /// This example uses an anonymous method in C# as the WriteDelegate to provide
        /// the data for the ZipEntry. The example is a bit contrived - the
        /// <c>AddFile()</c> method is a simpler way to insert the contents of a file
        /// into an entry in a zip file. On the other hand, if there is some sort of
        /// processing or transformation of the file contents required before writing,
        /// the application could use the <c>WriteDelegate</c> to do it, in this way.
        ///
        /// <code lang="C#">
        /// using (var input = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ))
        /// {
        ///     using(Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile())
        ///     {
        ///         zip.AddEntry(zipEntryName, (name,output) =>
        ///             {
        ///                 byte[] buffer = new byte[BufferSize];
        ///                 int n;
        ///                 while ((n = input.Read(buffer, 0, buffer.Length)) != 0)
        ///                 {
        ///                     // could transform the data here...
        ///                     output.Write(buffer, 0, n);
        ///                     // could update a progress bar here
        ///                 }
        ///             });
        ///         
        ///         zip.Save(zipFileName);
        ///     }
        /// }
        /// </code>
        /// </example>
        ///
        /// <example>
        /// 
        /// This example uses a named delegate in VB to write data for the given
        /// ZipEntry (VB9 does not have anonymous delegates). The example here is a bit
        /// contrived - a simpler way to add the contents of a file to a ZipEntry is to
        /// simply use the appropriate <c>AddFile()</c> method.  The key scenario for
        /// which the <c>WriteDelegate</c> makes sense is saving a DataSet, in XML
        /// format, to the zip file. The DataSet can write XML to a stream, and the
        /// WriteDelegate is the perfect place to write into the zip file.  There may be
        /// other data structures that can write to a stream, but cannot be read as a
        /// stream.  The <c>WriteDelegate</c> would be appropriate for those cases as
        /// well.
        /// 
        /// <code lang="VB">
        /// Private Sub WriteEntry (ByVal name As String, ByVal output As Stream) 
        ///     Using input As FileStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        ///         Dim n As Integer = -1
        ///         Dim buffer As Byte() = New Byte(BufferSize){}
        ///         Do While n &lt;&gt; 0
        ///             n = input.Read(buffer, 0, buffer.Length)
        ///             output.Write(buffer, 0, n)
        ///         Loop
        ///     End Using
        /// End Sub
        ///
        /// Public Sub Run()
        ///     Using zip = New ZipFile
        ///         zip.AddEntry(zipEntryName, New WriteDelegate(AddressOf WriteEntry))
        ///         zip.Save(zipFileName)
        ///     End Using
        /// End Sub
        /// </code>
        /// </example>
        public ZipEntry AddEntry(string entryName, WriteDelegate writer)
        {
            ZipEntry ze = ZipEntry.CreateForWriter(entryName, writer);
            if (Verbose) StatusMessageTextWriter.WriteLine("adding {0}...", entryName);
            return _InternalAddEntry(ze);
        }


        /// <summary>
        ///   Add an entry, for which the application will provide a stream, just-in-time. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   In cases where the application wishes to open the stream that holds the content for
        ///   the ZipEntry, on a just-in-time basis, the application can use this method and
        ///   provide delegates to open and close the stream.
        /// </para>
        /// 
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to the
        ///   <c>ZipEntry</c> added.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <example>
        ///   This example uses anonymous methods in C# to open and close
        ///   the input stream for the content for a zip entry.
        /// <code lang="C#">
        /// using(Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile())
        /// {
        ///     zip.AddEntry(zipEntryName,
        ///                  (name) =>  File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ),
        ///                  (name, stream) =>  stream.Close()
        ///                  );
        ///         
        ///     zip.Save(zipFileName);
        /// }
        /// </code>
        /// </example>
        public ZipEntry AddEntry(string entryName, OpenDelegate opener, CloseDelegate closer)
        {
            ZipEntry ze = ZipEntry.CreateForJitStreamProvider(entryName, opener, closer);
            ze.SetEntryTimes(DateTime.Now,DateTime.Now,DateTime.Now);
            if (Verbose) StatusMessageTextWriter.WriteLine("adding {0}...", entryName);
            return _InternalAddEntry(ze);
        }

        

        private ZipEntry _InternalAddEntry(ZipEntry ze)
        {
            ze.CompressionLevel = CompressionLevel;
            ze.ExtractExistingFile = ExtractExistingFile;
            ze.ZipErrorAction = this.ZipErrorAction;
            ze.SetCompression = SetCompression;
            ze.ProvisionalAlternateEncoding = ProvisionalAlternateEncoding;
            ze._container = new ZipContainer(this);
            ze.Password = _Password;
            ze.Encryption = Encryption;
            ze.EmitTimesInWindowsFormatWhenSaving = _emitNtfsTimes;
            ze.EmitTimesInUnixFormatWhenSaving = _emitUnixTimes;
            InsureUniqueEntry(ze);
            _entries.Add(ze);
            AfterAddEntry(ze);
            _contentsChanged = true;
            return ze;
        }

        


        /// <summary>
        ///   Updates the given entry in the <c>ZipFile</c>, using the given string as
        ///   input.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para>
        ///   Calling this method is equivalent to removing the <c>ZipEntry</c> for the
        ///   given file name and directory path, if it exists, and then calling <see
        ///   cref="AddEntry(String,String)" />.  See the documentation for that
        ///   method for further explanation.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <param name="entryName">
        ///   The name, including any path, to use within the archive for the entry.
        /// </param>
        ///
        /// <param name="content">
        ///   The content of the file, should it be extracted from the zip.
        /// </param>
        ///
        /// <returns>The <c>ZipEntry</c> added.</returns>
        /// 
        public ZipEntry UpdateEntry(string entryName, string content)
        {
            return UpdateEntry(entryName, content, System.Text.Encoding.Default);
        }


        /// <summary>
        ///   Updates the given entry in the <c>ZipFile</c>, using the given string as
        ///   content for the <c>ZipEntry</c>.
        /// </summary>
        ///
        /// <remarks>
        ///   Calling this method is equivalent to removing the <c>ZipEntry</c> for the
        ///   given file name and directory path, if it exists, and then calling <see
        ///   cref="AddEntry(String,String, System.Text.Encoding)" />.  See the
        ///   documentation for that method for further explanation.
        /// </remarks>
        ///
        /// <param name="entryName">
        ///   The name, including any path, to use within the archive for the entry.
        /// </param>
        ///
        /// <param name="content">
        ///   The content of the file, should it be extracted from the zip.
        /// </param>
        ///
        /// <param name="encoding">
        ///   The text encoding to use when encoding the string. Be aware: This is
        ///   distinct from the text encoding used to encode the filename. See <see
        ///   cref="ProvisionalAlternateEncoding" />.
        /// </param>
        ///
        /// <returns>The <c>ZipEntry</c> added.</returns>
        /// 
        public ZipEntry UpdateEntry(string entryName, string content, System.Text.Encoding encoding)
        {
            string directoryPathInArchive = null;
            if (entryName.IndexOf('\\') != -1)
            {
                directoryPathInArchive = Path.GetDirectoryName(entryName);
                entryName = Path.GetFileName(entryName);
            }
            string key = ZipEntry.NameInArchive(entryName, directoryPathInArchive);

            if (this[key] != null)
                this.RemoveEntry(key);

            return AddEntry(entryName, content, encoding);
        }


        /// <summary>
        ///   Updates the given entry in the <c>ZipFile</c>, using the given stream as
        ///   input, and the given filename and given directory Path.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   Calling the method is equivalent to calling <c>RemoveEntry()</c> if an
        ///   entry by the same name already exists, and then calling <c>AddEntry()</c>
        ///   with the given <c>fileName</c> and stream.
        /// </para>
        ///
        /// <para>
        ///   The stream must be open and readable during the call to
        ///   <c>ZipFile.Save</c>.  You can dispense the stream on a just-in-time basis
        ///   using the <see cref="ZipEntry.InputStream"/> property. Check the
        ///   documentation of that property for more information.
        /// </para>
        /// 
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to the
        ///   <c>ZipEntry</c> added.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.AddEntry(string, System.IO.Stream)"/>
        /// <seealso cref="Ionic.Zip.ZipEntry.InputStream"/>
        ///
        /// <param name="entryName">
        ///   The name, including any path, to use within the archive for the entry.
        /// </param>
        ///
        /// <param name="stream">The input stream from which to read file data.</param>
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry UpdateEntry(string entryName, Stream stream)
        {
            string directoryPathInArchive = null;
            if (entryName.IndexOf('\\') != -1)
            {
                directoryPathInArchive = Path.GetDirectoryName(entryName);
                entryName = Path.GetFileName(entryName);
            }
            string key = ZipEntry.NameInArchive(entryName, directoryPathInArchive);

            if (this[key] != null)
                this.RemoveEntry(key);

            return AddEntry(entryName, stream);
        }




        /// <summary>
        /// Add an entry into the zip archive using the given filename and directory
        /// path within the archive, and the given content for the file. No file is
        /// created in the filesystem.
        /// </summary>
        ///
        /// <param name="byteContent">The data to use for the entry.</param>
        ///
        /// <param name="entryName">
        ///   The name, including any path, to use within the archive for the entry.
        /// </param>
        ///
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry AddEntry(string entryName, byte[] byteContent)
        {
            if (byteContent == null) throw new ArgumentException("bad argument", "byteContent");
            var ms = new MemoryStream(byteContent);
            return AddEntry(entryName, ms);
        }

        
        /// <summary>
        /// Updates the given entry in the <c>ZipFile</c>, using the given byte array as
        /// content for the entry.
        /// </summary>
        ///
        /// <remarks>
        /// Calling this method is equivalent to removing the <c>ZipEntry</c> for the
        /// given filename and directory path, if it exists, and then calling <see
        /// cref="AddEntry(String,byte[])" />.
        /// See the documentation for that method for further explanation.
        /// </remarks>
        ///
        /// <param name="entryName">
        ///   The name, including any path, to use within the archive for the entry.
        /// </param>
        ///
        /// <param name="byteContent">The content to use for the <c>ZipEntry</c>.</param>
        ///
        /// <returns>The <c>ZipEntry</c> added.</returns>
        /// 
        public ZipEntry UpdateEntry(string entryName, byte[] byteContent)
        {
            string directoryPathInArchive = null;
            if (entryName.IndexOf('\\') != -1)
            {
                directoryPathInArchive = Path.GetDirectoryName(entryName);
                entryName = Path.GetFileName(entryName);
            }
            var key = ZipEntry.NameInArchive(entryName, directoryPathInArchive);
            if (this[key] != null)
                this.RemoveEntry(key);

            return AddEntry(entryName, byteContent);
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
        ///   Adds the contents of a filesystem directory to a Zip file archive. 
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        ///   The name of the directory may be a relative path or a fully-qualified
        ///   path. Any files within the named directory are added to the archive.  Any
        ///   subdirectories within the named directory are also added to the archive,
        ///   recursively.
        /// </para>
        /// 
        /// <para>
        ///   Top-level entries in the named directory will appear as top-level entries
        ///   in the zip archive.  Entries in subdirectories in the named directory will
        ///   result in entries in subdirectories in the zip archive.
        /// </para>
        /// 
        /// <para>
        ///   If you want the entries to appear in a containing directory in the zip
        ///   archive itself, then you should call the AddDirectory() overload that
        ///   allows you to explicitly specify a directory path for use in the archive.
        /// </para>
        /// 
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to each
        ///   ZipEntry added.
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
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry AddDirectory(string directoryName)
        {
            return AddDirectory(directoryName, null);
        }


        /// <summary>
        ///   Adds the contents of a filesystem directory to a Zip file archive,
        ///   overriding the path to be used for entries in the archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        ///   The name of the directory may be a relative path or a fully-qualified
        ///   path. The add operation is recursive, so that any files or subdirectories
        ///   within the name directory are also added to the archive.
        /// </para>
        /// 
        /// <para>
        ///   Top-level entries in the named directory will appear as top-level entries
        ///   in the zip archive.  Entries in subdirectories in the named directory will
        ///   result in entries in subdirectories in the zip archive.
        /// </para>
        /// 
        /// <para>
        ///   For <c>ZipFile</c> properties including <see cref="Encryption"/>, <see
        ///   cref="Password"/>, <see cref="SetCompression"/>, <see
        ///   cref="ProvisionalAlternateEncoding"/>, <see cref="ExtractExistingFile"/>,
        ///   <see cref="ZipErrorAction"/>, and <see cref="CompressionLevel"/>, their
        ///   respective values at the time of this call will be applied to each
        ///   ZipEntry added.
        /// </para>
        ///
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        ///   In this code, calling the ZipUp() method with a value of "c:\reports" for
        ///   the directory parameter will result in a zip file structure in which all
        ///   entries are contained in a toplevel "reports" directory.
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
        ///   Specifies a directory path to use to override any path in the
        ///   DirectoryName.  This path may, or may not, correspond to a real directory
        ///   in the current filesystem.  If the zip is later extracted, this is the
        ///   path used for the extracted file or directory.  Passing <c>null</c>
        ///   (<c>Nothing</c> in VB) or the empty string ("") will insert the items at
        ///   the root path within the archive.
        /// </param>
        /// 
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry AddDirectory(string directoryName, string directoryPathInArchive)
        {
            return AddOrUpdateDirectoryImpl(directoryName, directoryPathInArchive, AddOrUpdateAction.AddOnly);
        }


        /// <summary>
        ///   Creates a directory in the zip archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        ///   Use this when you want to create a directory in the archive but there is
        ///   no corresponding filesystem representation for that directory.
        /// </para>
        ///
        /// <para>
        ///   You will probably not need to do this in your code. One of the only times
        ///   you will want to do this is if you want an empty directory in the zip
        ///   archive.  The reason: if you add a file to a zip archive that is stored
        ///   within a multi-level directory, all of the directory tree is implicitly
        ///   created in the zip archive.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="directoryNameInArchive">
        ///   The name of the directory to create in the archive.
        /// </param>
        /// <returns>The <c>ZipEntry</c> added.</returns>
        public ZipEntry AddDirectoryByName(string directoryNameInArchive)
        {
            ZipEntry dir = ZipEntry.CreateFromNothing(directoryNameInArchive);    // workitem 9073
            dir.MarkAsDirectory();
            dir.ProvisionalAlternateEncoding = this.ProvisionalAlternateEncoding;  // workitem 8984
            dir.SetEntryTimes(DateTime.Now,DateTime.Now,DateTime.Now);
            dir.EmitTimesInWindowsFormatWhenSaving = _emitNtfsTimes;
            dir.EmitTimesInUnixFormatWhenSaving = _emitUnixTimes;
            dir._Source = ZipEntrySource.Stream;
            dir._container = new ZipContainer(this);
            InsureUniqueEntry(dir);
            _entries.Add(dir);
            AfterAddEntry(dir);
            _contentsChanged = true;
            return dir;
        }



        private ZipEntry AddOrUpdateDirectoryImpl(string directoryName, string rootDirectoryPathInArchive, AddOrUpdateAction action)
        {
            if (rootDirectoryPathInArchive == null)
            {
                rootDirectoryPathInArchive = "";
            }

            return AddOrUpdateDirectoryImpl(directoryName, rootDirectoryPathInArchive, action, 0);
        }



        private ZipEntry AddOrUpdateDirectoryImpl(string directoryName, string rootDirectoryPathInArchive, AddOrUpdateAction action, int level)
        {
            if (Verbose) StatusMessageTextWriter.WriteLine("{0} {1}...",
                                                           (action == AddOrUpdateAction.AddOnly) ? "adding" : "Adding or updating", directoryName);

            if (level == 0)
                OnAddStarted();
            
            string dirForEntries = rootDirectoryPathInArchive;
            ZipEntry baseDir = null;

            if (level > 0)
            {
                int f = directoryName.Length;
                for (int i = level; i > 0; i--)
                    f = directoryName.LastIndexOfAny("/\\".ToCharArray(), f - 1, f - 1);

                dirForEntries = directoryName.Substring(f + 1);
                dirForEntries = Path.Combine(rootDirectoryPathInArchive, dirForEntries);
            }

            // if not top level, or if the root is non-empty, then explicitly add the directory
            if (level > 0 || rootDirectoryPathInArchive != "")
            {
                baseDir = ZipEntry.CreateFromFile(directoryName, dirForEntries);
                baseDir.ProvisionalAlternateEncoding = this.ProvisionalAlternateEncoding;  // workitem 6410
                baseDir.MarkAsDirectory();
                baseDir.EmitTimesInWindowsFormatWhenSaving = _emitNtfsTimes;
                baseDir.EmitTimesInUnixFormatWhenSaving = _emitUnixTimes;
                baseDir._container = new ZipContainer(this);

                // check for uniqueness:
                ZipEntry e = this[baseDir.FileName];
                if (e == null)
                {
                    _entries.Add(baseDir);
                    AfterAddEntry(baseDir);
                    _contentsChanged = true;
                }
                dirForEntries = baseDir.FileName;
            }

            String[] filenames = Directory.GetFiles(directoryName);

            // add the files: 
            foreach (String filename in filenames)
            {
                if (action == AddOrUpdateAction.AddOnly)
                    AddFile(filename, dirForEntries);
                else
                    UpdateFile(filename, dirForEntries);
            }

            // add the subdirectories:
            String[] dirnames = Directory.GetDirectories(directoryName);
            foreach (String dir in dirnames)
            {
                // workitem 8617: Optionally traverse reparse points
#if NETCF
                FileAttributes fileAttrs = (FileAttributes) NetCfFile.GetAttributes(dir);
#else
                FileAttributes fileAttrs = System.IO.File.GetAttributes(dir);
#endif
                if (this.AddDirectoryWillTraverseReparsePoints ||
                    ((fileAttrs & FileAttributes.ReparsePoint) == 0)) 
                    AddOrUpdateDirectoryImpl(dir, rootDirectoryPathInArchive, action, level + 1);
            }

            if (level == 0)
                OnAddCompleted();
            
            return baseDir;
        }

    }
    
}
