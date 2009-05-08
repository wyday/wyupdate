// ZipFile.Selector.cs
//
// Copyright (c) 2009 Microsoft Corporation.  All rights reserved.
//
// This module defines methods in the ZipFile class associated to the FileFilter capability -
// selecting files to add into the archive, or selecting entries to retrieve from the archive
// based on criteria including the filename, size, date, or attributes. 
//
// These methods are segregated into a different module to facilitate easy exclusion for those
// people who wish to have a smaller library without this function. 
// 
// This code is released under the Microsoft Public License . 
// See the License.txt for details.  
//

using System;


namespace Ionic.Zip
{

    partial class ZipFile
    {
        /// <summary>
        /// Adds to the ZipFile a set of files from the disk that conform to the specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method selects files from the the current working directory matching the specified
        /// criteria, and adds them to the ZipFile.  The selection does not recurse into
        /// subdirectories.
        /// </para>
        ///
        /// <para>
        /// Specify the criteria in statements of 3 elements: a noun, an operator, and a value.
        /// Consider the string "name != *.doc" .  The noun is "name".  The operator is "!=",
        /// implying "Not Equal".  The value is "*.doc".  That criterion, in English, says "all
        /// files with a name that does not end in the .doc extension."
        /// </para> 
        ///
        /// <para>
        /// Supported nouns include "name" for the filename; "atime", "mtime", and "ctime" for
        /// last access time, last modfied time, and created time of the file, respectively;
        /// "attributes" for the file attributes; and "size" for the file length (uncompressed).
        /// The "attributes" and "name" nouns both support = and != as operators.  The "size",
        /// "atime", "mtime", and "ctime" nouns support = and !=, and &gt;, &gt;=, &lt;, &lt;=
        /// as well.
        /// </para> 
        ///
        /// <para>
        /// Specify values for the file attributes as a string with one or more of the
        /// characters H,R,S,A in any order, implying Hidden, ReadOnly, System, and Archive,
        /// respectively.  To specify a time, use YYYY-MM-DD-HH:mm:ss as the format.  If you
        /// omit the HH:mm:ss portion, it is assumed to be 00:00:00 (midnight). The value for a
        /// size criterion is expressed in integer quantities of bytes, kilobytes (use k or kb
        /// after the number), megabytes (m or mb), or gigabytes (g or gb).  The value for a
        /// name is a pattern to match against the filename, potentially including wildcards.
        /// The pattern follows CMD.exe glob rules: * implies one or more of any character,
        /// while ? implies one character.  If the name pattern contains any slashes, it is
        /// matched to the entire filename, including the path; otherwise, it is matched
        /// against only the filename without the path.  This means a pattern of "*\*.*" matches 
        /// all files one directory level deep, while a pattern of "*.*" matches all files in 
        /// all directories.  
        /// </para> 
        ///
        /// <para>
        /// To specify a name pattern that includes spaces, use single quotes around the pattern.
        /// A pattern of "'* *.*'" will match all files that have spaces in the filename.  The full 
        /// criteria string for that would be "name = '* *.*'" . 
        /// </para> 
        ///
        /// <para>
        /// Some examples: a string like "attributes != H" retrieves all entries whose
        /// attributes do not include the Hidden bit.  A string like "mtime > 2009-01-01"
        /// retrieves all entries with a last modified time after January 1st, 2009.  For
        /// example "size &gt; 2gb" retrieves all entries whose uncompressed size is greater
        /// than 2gb.
        /// </para> 
        ///
        /// <para>
        /// You can combine criteria with the conjunctions AND or OR. Using a string like "name
        /// = *.txt AND size &gt;= 100k" for the selectionCriteria retrieves entries whose names
        /// end in  .txt, and whose uncompressed size is greater than or equal to
        /// 100 kilobytes.
        /// </para>
        ///
        /// <para>
        /// For more complex combinations of criteria, you can use parenthesis to group clauses
        /// in the boolean logic.  Without parenthesis, the precedence of the criterion atoms is
        /// determined by order of appearance.  Unlike the C# language, the AND conjunction does
        /// not take precendence over the logical OR.  This is important only in strings that
        /// contain 3 or more criterion atoms.  In other words, "name = *.txt and size &gt; 1000
        /// or attributes = H" implies "((name = *.txt AND size &gt; 1000) OR attributes = H)"
        /// while "attributes = H OR name = *.txt and size &gt; 1000" evaluates to "((attributes
        /// = H OR name = *.txt) AND size &gt; 1000)".  When in doubt, use parenthesis.
        /// </para>
        ///
        /// <para>
        /// Using time properties requires some extra care. If you want to retrieve all entries
        /// that were last updated on 2009 February 14, specify a time range like so:"mtime
        /// &gt;= 2009-02-14 AND mtime &lt; 2009-02-15".  Read this to say: all files updated
        /// after 12:00am on February 14th, until 12:00am on February 15th.  You can use the
        /// same bracketing approach to specify any time period - a year, a month, a week, and
        /// so on.
        /// </para>
        ///
        /// <para>
        /// The syntax allows one special case: if you provide a string with no spaces, it is
        /// treated as a pattern to match for the filename.  Therefore a string like "*.xls"
        /// will be equivalent to specifying "name = *.xls".
        /// </para>
        /// 
        /// <para>
        /// There is no logic in this method that insures that the file inclusion criteria are
        /// internally consistent.  For example, it's possible to specify criteria that says the
        /// file must have a size of less than 100 bytes, as well as a size that is greater than
        /// 1000 bytes.  Obviously no file will ever satisfy such criteria, but this method does
        /// not detect such inconsistencies. The caller is responsible for insuring the criteria
        /// are sensible.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
	/// This example zips up all *.csv files in the current working directory. 
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// {
	///     // To just match on filename wildcards,
        ///     // use the shorthand form of the selectionCriteria string.
        ///     zip.AddSelectedFiles("*.csv");
        ///     zip.Save(PathToZipArchive);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = New ZipFile()
        ///     zip.AddSelectedFiles("*.csv")
        ///     zip.Save(PathToZipArchive)
        /// End Using
        /// </code>
        /// </example>
	///
        /// <param name="selectionCriteria">The criteria for file selection</param>
        public void AddSelectedFiles(String selectionCriteria)
        {
            this.AddSelectedFiles(selectionCriteria, ".", null, false);
        }

        /// <summary>
        /// Adds to the ZipFile a set of files from the disk that conform to the specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method selects files from the the current working directory matching the specified
        /// criteria, and adds them to the ZipFile.  The selection optionally recurses into
        /// subdirectories.
        /// </para>
        ///
        /// <para>
        /// For details on the syntax for the selectionCriteria parameter, see <see
        /// cref="AddSelectedFiles(String)"/>.
        /// </para>
        /// </remarks>
	///
        /// <example>
	/// This example zips up all *.xml files in the current working directory, or any subdirectory, that
	/// are larger than 1mb.
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// {
	///     // Use a compound expression in the selectionCriteria string.
        ///     zip.AddSelectedFiles("name = *.xml  and  size > 1024kb", true);
        ///     zip.Save(PathToZipArchive);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = New ZipFile()
	///     ' Use a compound expression in the selectionCriteria string.
        ///     zip.AddSelectedFiles("name = *.xml  and  size > 1024kb", true)
        ///     zip.Save(PathToZipArchive)
        /// End Using
        /// </code>
        /// </example>
	/// 
        /// <param name="selectionCriteria">The criteria for file selection</param>
        /// <param name="recurseDirectories">If true, the file selection will recurse into subdirectories.</param>
        public void AddSelectedFiles(String selectionCriteria, bool recurseDirectories)
        {
            this.AddSelectedFiles(selectionCriteria, ".", null, recurseDirectories);
        }

        /// <summary>
        /// Adds to the ZipFile a set of files from the disk that conform to the specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// This method selects files from the the specified disk directory matching the specified
        /// criteria, and adds them to the ZipFile.  The search does not recurse into
        /// subdirectores.  For details on the syntax for the selectionCriteria parameter, see <see
        /// cref="AddSelectedFiles(String)"/>.
        /// </remarks>
        /// 
        /// <example>
	/// This example zips up all *.xml files larger than 1mb in the directory given by "d:\rawdata".
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// {
	///     // Use a compound expression in the selectionCriteria string.
        ///     zip.AddSelectedFiles("name = *.xml  and  size > 1024kb", "d:\\rawdata");
        ///     zip.Save(PathToZipArchive);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = New ZipFile()
	///     ' Use a compound expression in the selectionCriteria string.
        ///     zip.AddSelectedFiles("name = *.xml  and  size > 1024kb", "d:\rawdata)
        ///     zip.Save(PathToZipArchive)
        /// End Using
        /// </code>
        /// </example>
	/// 
        /// <param name="selectionCriteria">The criteria for file selection</param>
        /// <param name="directoryOnDisk">The name of the directory on the disk from which to select files. </param>
        public void AddSelectedFiles(String selectionCriteria, String directoryOnDisk)
        {
            this.AddSelectedFiles(selectionCriteria, directoryOnDisk, null, false);
        }


        /// <summary>
        /// Adds to the ZipFile a set of files from the disk that conform to the specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// This method selects files from the the specified disk directory matching the specified
        /// selection criteria, and adds them to the ZipFile.  The search will recurse into subdirectores if
        /// recurseDirectories is true.  For details on the syntax for the selectionCriteria
        /// parameter, see <see cref="AddSelectedFiles(String)"/>.
        /// </remarks>
        /// 
        /// <example>
	/// This example zips up all *.csv files in the "files" directory, or any subdirectory, that
	/// have been saved since 2009 February 14th.
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// {
	///     // Use a compound expression in the selectionCriteria string.
        ///     zip.AddSelectedFiles("name = *.csv  and  mtime > 2009-02-14", "files", true);
        ///     zip.Save(PathToZipArchive);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = New ZipFile()
	///     ' Use a compound expression in the selectionCriteria string.
        ///     zip.AddSelectedFiles("name = *.csv  and  mtime > 2009-02-14", "files", true)
        ///     zip.Save(PathToZipArchive)
        /// End Using
        /// </code>
        /// </example>
	/// 
        /// <param name="selectionCriteria">The criteria for file selection</param>
        /// <param name="directoryOnDisk">The name of the directory on the disk from which to select files. </param>
        /// <param name="recurseDirectories">If true, the file selection will recurse into subdirectories.</param>
        public void AddSelectedFiles(String selectionCriteria, String directoryOnDisk, bool recurseDirectories)
        {
            this.AddSelectedFiles(selectionCriteria, directoryOnDisk, null, recurseDirectories);
        }


        /// <summary>
        /// Adds to the ZipFile a selection of files from the disk that conform to the specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// This method selects files from the specified disk directory matching the specified
        /// selection criteria, and adds those files to the ZipFile, using the specified directory
        /// path in the archive.  The search does not recurse into subdirectories.  For details on
        /// the syntax for the selectionCriteria parameter, see <see
        /// cref="AddSelectedFiles(String)" />.
        /// </remarks>
        /// 
        /// <example>
	/// This example zips up all *.psd files in the "photos" directory that
	/// have been saved since 2009 February 14th, and puts them all in a zip file, using the directory 
	/// name of "content" in the zip archive itself. When the zip archive is unzipped, the folder containing 
	/// the .psd files will be named "content".
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// {
	///     // Use a compound expression in the selectionCriteria string.
        ///     zip.AddSelectedFiles("name = *.psd  and  mtime > 2009-02-14", "photos", "content");
        ///     zip.Save(PathToZipArchive);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = New ZipFile
        ///     zip.AddSelectedFiles("name = *.psd  and  mtime > 2009-02-14", "photos", "content")
        ///     zip.Save(PathToZipArchive)
        /// End Using
        /// </code>
        /// </example>
	/// 
        /// <param name="selectionCriteria">The criteria for selection of files to Add</param>
        ///
        /// <param name="directoryOnDisk">The name of the directory on the disk from which to select files. </param>
	///
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the FileName.  This path may,
        /// or may not, correspond to a real directory in the current filesystem.  If the files
        /// within the zip are later extracted, this is the path used for the extracted file.
        /// Passing null (nothing in VB) will use the path on the FileName, if any.  Passing the
        /// empty string ("") will insert the item at the root path within the archive.
        /// </param>
        public void AddSelectedFiles(String selectionCriteria, String directoryOnDisk, String directoryPathInArchive)
        {
            this.AddSelectedFiles(selectionCriteria, directoryOnDisk, directoryPathInArchive, false);
        }

        /// <summary>
        /// Adds to the ZipFile a selection of files from the disk that conform to the specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// This method selects files from the specified disk directory that match the specified
        /// selection criteria, and adds those files to the ZipFile, using the specified directory
        /// path in the archive.  If recurseDirectories is true, files are also selected from
        /// subdirectories.  For details on the syntax for the selectionCriteria parameter, see
        /// <see cref="AddSelectedFiles(String)" />.
        /// </remarks>
        /// 
        /// <example>
	/// This example zips up all files that are NOT *.pst files, in the current working directory 
	/// and any subdirectories.
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// {
        ///     zip.AddSelectedFiles("name != *.pst", SourceDirectory, "backup", true);
        ///     zip.Save(PathToZipArchive);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = New ZipFile
        ///     zip.AddSelectedFiles("name != *.pst", SourceDirectory, "backup", true)
        ///     zip.Save(PathToZipArchive)
        /// End Using
        /// </code>
        /// </example>
	///
        /// <param name="selectionCriteria">The criteria for selection of files to Add</param>
        ///
        /// <param name="directoryOnDisk">The name of the directory on the disk from which to select files. </param>
        /// 
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the FileName.  This path may,
        /// or may not, correspond to a real directory in the current filesystem.  If the files
        /// within the zip are later extracted, this is the path used for the extracted file.
        /// Passing null (nothing in VB) will use the path on the FileName, if any.  Passing the
        /// empty string ("") will insert the item at the root path within the archive.
        /// </param>
        ///
        /// <param name="recurseDirectories">
        /// If true, the method also scans subdirectories for files matching the criteria.
        /// </param>
        public void AddSelectedFiles(String selectionCriteria, String directoryOnDisk, String directoryPathInArchive, bool recurseDirectories)
        {
	    if (String.IsNullOrEmpty(directoryOnDisk)) directoryOnDisk= ".";
            if (Verbose) StatusMessageTextWriter.WriteLine("adding selection '{0}' from dir '{1}'...", selectionCriteria, directoryOnDisk);
            Ionic.FileSelector ff = new Ionic.FileSelector(selectionCriteria);
            var filesToAdd = ff.SelectFiles(directoryOnDisk, recurseDirectories);
            if (Verbose) StatusMessageTextWriter.WriteLine("found {0} files...", filesToAdd.Count);

	    foreach (var f in filesToAdd)
	    {
		if (directoryPathInArchive!=null) 
		{
		    string dirInArchive = System.IO.Path.GetDirectoryName(f).Replace(directoryOnDisk, directoryPathInArchive);
		    this.AddFile(f, dirInArchive);
		}
		else
		    this.AddFile(f, null);
	    }
        }



        /// <summary>
        /// Retrieve entries from the zipfile by specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method allows callers to retrieve the collection of entries from the zipfile
        /// that fit the specified criteria.  The criteria are described in a string format, and
        /// can include patterns for the filename; constraints on the size of the entry;
        /// constraints on the last modified, created, or last accessed time for the file
        /// described by the entry; or the attributes of the entry.
        /// </para>
        ///
        /// <para>
        /// For details on the syntax for the selectionCriteria parameter, see <see
        /// cref="AddSelectedFiles(String)"/>.
        /// </para>
        /// 
        /// <para>
        /// This method is intended for use with a ZipFile that has been read from storage.
        /// When creating a new ZipFile, this method will work only after the ZipArchive has
        /// been Saved to the disk (the ZipFile class subsequently and implicitly reads the Zip
        /// archive from storage.)  Calling SelectEntries on a ZipFile that has not yet been
        /// saved will deliver undefined results.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="System.Exception">
        /// Thrown if selectionCriteria has an invalid syntax.
        /// </exception>
        /// 
        /// <example>
	/// This example selects all the PhotoShop files from within an archive, and extracts them
	/// to the current working directory.
        /// <code>
        /// using (ZipFile zip1 = ZipFile.Read(ZipFileName))
        /// {
        ///     var PhotoShopFiles = zip1.SelectEntries("*.psd");
        ///     foreach (ZipEntry psd in PhotoShopFiles)
        ///     {
        ///         psd.Extract();
        ///     }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip1 As ZipFile = ZipFile.Read(ZipFileName)
        ///     Dim PhotoShopFiles as ICollection(Of ZipEntry)
        ///     PhotoShopFiles = zip1.SelectEntries("*.psd")
        ///     Dim psd As ZipEntry
        ///     For Each psd In PhotoShopFiles
        ///         psd.Extract
        ///     Next
        /// End Using
        /// </code>
        /// </example>
        /// <param name="selectionCriteria">the string that specifies which entries to select</param>
        /// <returns>a collection of ZipEntry objects that conform to the inclusion spec</returns>
        public System.Collections.Generic.ICollection<ZipEntry> SelectEntries(String selectionCriteria)
        {
            Ionic.FileSelector ff = new Ionic.FileSelector(selectionCriteria);
            return ff.SelectEntries(this);
        }


        /// <summary>
        /// Retrieve entries from the zipfile by specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method allows callers to retrieve the collection of entries from the zipfile
        /// that fit the specified criteria.  The criteria are described in a string format, and
        /// can include patterns for the filename; constraints on the size of the entry;
        /// constraints on the last modified, created, or last accessed time for the file
        /// described by the entry; or the attributes of the entry.
        /// </para>
        ///
        /// <para>
        /// For details on the syntax for the selectionCriteria parameter, see <see
        /// cref="AddSelectedFiles(String)"/>.
        /// </para>
        /// 
        /// <para>
        /// This method is intended for use with a ZipFile that has been read from storage.
        /// When creating a new ZipFile, this method will work only after the ZipArchive has
        /// been Saved to the disk (the ZipFile class subsequently and implicitly reads the Zip
        /// archive from storage.)  Calling SelectEntries on a ZipFile that has not yet been
        /// saved will deliver undefined results.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="System.Exception">
        /// Thrown if selectionCriteria has an invalid syntax.
        /// </exception>
        /// 
        /// <example>
        /// <code>
        /// using (ZipFile zip1 = ZipFile.Read(ZipFileName))
        /// {
        ///     var UpdatedPhotoShopFiles = zip1.SelectEntries("*.psd", "UpdatedFiles");
        ///     foreach (ZipEntry e in UpdatedPhotoShopFiles)
        ///     {
	///         // prompt for extract here
	///         if (WantExtract(e.FileName))
        ///             e.Extract();
        ///     }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip1 As ZipFile = ZipFile.Read(ZipFileName)
        ///     Dim UpdatedPhotoShopFiles As ICollection(Of ZipEntry) = zip1.SelectEntries("*.psd", "UpdatedFiles")
        ///     Dim e As ZipEntry
        ///     For Each e In UpdatedPhotoShopFiles
	///         ' prompt for extract here
        ///         If Me.WantExtract(e.FileName) Then
        ///             e.Extract
        ///         End If
        ///     Next
        /// End Using
        /// </code>
        /// </example>
        /// <param name="selectionCriteria">the string that specifies which entries to select</param>
        /// 
        /// <param name="directoryPathInArchive">
        /// the directory in the archive from which to select entries. If null, then 
        /// all directories in the archive are used. 
        /// </param>
        /// 
        /// <returns>a collection of ZipEntry objects that conform to the inclusion spec</returns>
        public System.Collections.Generic.ICollection<ZipEntry> SelectEntries(String selectionCriteria, string directoryPathInArchive)
        {
            Ionic.FileSelector ff = new Ionic.FileSelector(selectionCriteria);
            return ff.SelectEntries(this, directoryPathInArchive);
        }



        /// <summary>
        /// Remove entries from the zipfile by specified criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method allows callers to remove the collection of entries from the zipfile
        /// that fit the specified criteria.  The criteria are described in a string format, and
        /// can include patterns for the filename; constraints on the size of the entry;
        /// constraints on the last modified, created, or last accessed time for the file
        /// described by the entry; or the attributes of the entry.
        /// </para>
        ///
        /// <para>
        /// For details on the syntax for the selectionCriteria parameter, see <see
        /// cref="AddSelectedFiles(String)"/>.
        /// </para>
        /// 
        /// <para>
        /// This method is intended for use with a ZipFile that has been read from storage.
        /// When creating a new ZipFile, this method will work only after the ZipArchive has
        /// been Saved to the disk (the ZipFile class subsequently and implicitly reads the Zip
        /// archive from storage.)  Calling SelectEntries on a ZipFile that has not yet been
        /// saved will deliver undefined results.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="System.Exception">
        /// Thrown if selectionCriteria has an invalid syntax.
        /// </exception>
        /// 
        /// <example>
	/// This example removes all entries in a zip file that were modified prior to January 1st, 2008.
        /// <code>
        /// using (ZipFile zip1 = ZipFile.Read(ZipFileName))
        /// {
        ///     // remove all entries from prior to Jan 1, 2008
        ///     zip1.RemoveEntries("mtime &lt; 2008-01-01");
	///     // don't forget to save the archive!
	///     zip1.Save();
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = ZipFile.Read(ZipFileName)
        ///     ' remove all entries from prior to Jan 1, 2008
        ///     zip1.RemoveEntries("mtime &lt; 2008-01-01")
	///     ' do not forget to save the archive!
	///     zip1.Save
        /// End Using 
        /// </code>
        /// </example>
        /// <param name="selectionCriteria">the string that specifies which entries to select</param>
        /// <returns>the number of entries removed</returns>
        public int RemoveSelectedEntries(String selectionCriteria)
        {
            var selection = this.SelectEntries(selectionCriteria);
            this.RemoveEntries(selection);
            return selection.Count;
        }


        /// <summary>
        /// Remove entries from the zipfile by specified criteria, and within the specified 
        /// path in the archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method allows callers to remove the collection of entries from the zipfile
        /// that fit the specified criteria.  The criteria are described in a string format, and
        /// can include patterns for the filename; constraints on the size of the entry;
        /// constraints on the last modified, created, or last accessed time for the file
        /// described by the entry; or the attributes of the entry.
        /// </para>
        ///
        /// <para>
        /// For details on the syntax for the selectionCriteria parameter, see <see
        /// cref="AddSelectedFiles(String)"/>.
        /// </para>
        /// 
        /// <para>
        /// This method is intended for use with a ZipFile that has been read from storage.
        /// When creating a new ZipFile, this method will work only after the ZipArchive has
        /// been Saved to the disk (the ZipFile class subsequently and implicitly reads the Zip
        /// archive from storage.)  Calling SelectEntries on a ZipFile that has not yet been
        /// saved will deliver undefined results.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="System.Exception">
        /// Thrown if selectionCriteria has an invalid syntax.
        /// </exception>
        /// 
        /// <example>
        /// <code>
        /// using (ZipFile zip1 = ZipFile.Read(ZipFileName))
        /// {
        ///     // remove all entries from prior to Jan 1, 2008
        ///     zip1.RemoveEntries("mtime &lt; 2008-01-01", "documents");
	///     // a call to ZipFile.Save will make the modifications permanent
	///     zip1.Save();
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip As ZipFile = ZipFile.Read(ZipFileName)
        ///     ' remove all entries from prior to Jan 1, 2008
        ///     zip1.RemoveEntries("mtime &lt; 2008-01-01", "documents")
	///     ' a call to ZipFile.Save will make the modifications permanent
	///     zip1.Save
        /// End Using 
        /// </code>
        /// </example>
	/// 
        /// <param name="selectionCriteria">the string that specifies which entries to select</param>
        /// <param name="directoryPathInArchive">
        /// the directory in the archive from which to select entries. If null, then 
        /// all directories in the archive are used. 
        /// </param>
        /// <returns>the number of entries removed</returns>
        public int RemoveSelectedEntries(String selectionCriteria, string directoryPathInArchive)
        {
            var selection = this.SelectEntries(selectionCriteria, directoryPathInArchive);
            this.RemoveEntries(selection);
            return selection.Count;
        }


        /// <summary>
        /// Selects and Extracts a set of Entries from the ZipFile.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The entries are extracted into the current working directory. 
        /// </para>
        /// 
        /// <para>
        /// If any of the files to be extracted already exist, then the action taken is as
        /// specified in the <see cref="ZipEntry.ExtractExistingFile"/> property on the
        /// corresponding ZipEntry instance.  By default, the action taken in this case is to
        /// throw an exception.
        /// </para>
        /// 
        /// <para>
        /// For information on the syntax of the selectionCriteria string,
        /// see <see cref="AddSelectedFiles(String)" />.
        /// </para>
        /// </remarks>
        ///
        /// <example>
        /// This example shows how extract all XML files modified after 15 January 2009. 
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(zipArchiveName))
        /// {
        ///   zip.ExtractSelectedEntries("name = *.xml  and  mtime &gt; 2009-01-15");
        /// }
        /// </code>
        /// </example>
        /// <param name="selectionCriteria">the selection criteria for entries to extract.</param>
        ///
        /// <seealso cref="ExtractSelectedEntries(String,ExtractExistingFileAction)"/>
        public void ExtractSelectedEntries(String selectionCriteria)
        {
            foreach (ZipEntry e in SelectEntries(selectionCriteria))
            {
                e.Password = _Password; // possibly null
                e.Extract();
            }
        }


        /// <summary>
        /// Selects and Extracts a set of Entries from the ZipFile.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The entries are extracted into the current working directory. When extraction would would
        /// overwrite an existing filesystem file, the action taken is as specified in the
        /// extractExistingFile parameter.
        /// </para>
        /// 
        /// <para>
        /// For information on the syntax of the string describing the entry selection criteria, 
        /// see <see cref="AddSelectedFiles(String)" />.
        /// </para> 
        /// </remarks>
        ///
        /// <example>
        /// This example shows how extract all XML files modified after 15 January 2009, 
        /// overwriting any existing files. 
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(zipArchiveName))
        /// {
        ///   zip.ExtractSelectedEntries("name = *.xml  and  mtime &gt; 2009-01-15",
        ///                              ExtractExistingFileAction.OverwriteSilently);
        /// }
        /// </code>
        /// </example>
        /// 
        /// <param name="selectionCriteria">the selection criteria for entries to extract.</param>
        ///
        /// <param name="extractExistingFile">
        /// The action to take if extraction would overwrite an existing file.
        /// </param>
        public void ExtractSelectedEntries(String selectionCriteria, ExtractExistingFileAction extractExistingFile)
        {
            foreach (ZipEntry e in SelectEntries(selectionCriteria))
            {
                e.Password = _Password; // possibly null
                e.Extract(extractExistingFile);
            }
        }


        /// <summary>
        /// Selects and Extracts a set of Entries from the ZipFile.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The entries are selected from the specified directory within the archive, and then 
        /// extracted into the current working directory. 
        /// </para>
        /// 
        /// <para>
        /// If any of the files to be extracted already exist, then the action taken is as
        /// specified in the <see cref="ZipEntry.ExtractExistingFile"/> property on the
        /// corresponding ZipEntry instance.  By default, the action taken in this case is to
        /// throw an exception.
        /// </para>
        /// 
        /// <para>
        /// For information on the syntax of the string describing the entry selection criteria, 
        /// see <see cref="AddSelectedFiles(String)" />.
        /// </para> 
        /// </remarks>
        /// 
        /// <example>
        /// This example shows how extract all XML files modified after 15 January 2009, 
        /// and writes them to the "unpack" directory.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(zipArchiveName))
        /// {
        ///   zip.ExtractSelectedEntries("name = *.xml  and  mtime &gt; 2009-01-15","unpack");
        /// }
        /// </code>
        /// </example>
        ///
        /// <param name="selectionCriteria">the selection criteria for entries to extract.</param>
        ///
        /// <param name="directoryPathInArchive">
        /// the directory in the archive from which to select entries. If null, then 
        /// all directories in the archive are used. 
        /// </param>
        ///
        /// <seealso cref="ExtractSelectedEntries(String,String,String,ExtractExistingFileAction)"/>
        public void ExtractSelectedEntries(String selectionCriteria, String directoryPathInArchive)
        {
            foreach (ZipEntry e in SelectEntries(selectionCriteria, directoryPathInArchive))
            {
                e.Password = _Password; // possibly null
                e.Extract();
            }
        }


        /// <summary>
        /// Selects and Extracts a set of Entries from the ZipFile.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The entries are extracted into the specified directory. If any of the files to be
        /// extracted already exist, an exception will be thrown.
        /// </para>
        /// <para>
        /// For information on the syntax of the string describing the entry selection criteria, 
        /// see <see cref="AddSelectedFiles(String)" />.
        /// </para> 
        /// </remarks>
        ///
        /// <param name="selectionCriteria">the selection criteria for entries to extract.</param>
        ///
        /// <param name="directoryInArchive">
        /// the directory in the archive from which to select entries. If null, then 
        /// all directories in the archive are used. 
        /// </param>
        /// 
        /// <param name="extractDirectory">
        /// the directory on the disk into which to extract. It will be created 
        /// if it does not exist.
        /// </param>
        public void ExtractSelectedEntries(String selectionCriteria, string directoryInArchive, string extractDirectory)
        {
            foreach (ZipEntry e in SelectEntries(selectionCriteria, directoryInArchive))
            {
                e.Password = _Password; // possibly null
                e.Extract(extractDirectory);
            }
        }


        /// <summary>
        /// Selects and Extracts a set of Entries from the ZipFile.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The entries are extracted into the specified directory. When extraction would would
        /// overwrite an existing filesystem file, the action taken is as specified in the
        /// extractExistingFile parameter.
        /// </para>
        /// 
        /// <para>
        /// For information on the syntax of the string describing the entry selection criteria, 
        /// see <see cref="AddSelectedFiles(String)" />.
        /// </para> 
        /// </remarks>
        ///
        /// <example>
        /// This example shows how extract all files  with an XML extension or with  a size larger than 100,000 bytes, 
        /// and puts them in the unpack directory.  For any files that already exist in 
        /// that destination directory, they will not be overwritten.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(zipArchiveName))
        /// {
        ///   zip.ExtractSelectedEntries("name = *.xml  or  size  &gt; 100000",
        ///                              null, 
        ///                              "unpack", 
        ///                              ExtractExistingFileAction.DontOverwrite);
        /// }
        /// </code>
        /// </example>
        ///
        /// <param name="selectionCriteria">the selection criteria for entries to extract.</param>
        ///
        /// <param name="extractDirectory">
        /// The directory on the disk into which to extract. It will be created if it does not exist.
        /// </param>
        /// 
        /// <param name="directoryPathInArchive">
        /// The directory in the archive from which to select entries. If null, then 
        /// all directories in the archive are used. 
        /// </param>
        /// 
        /// <param name="extractExistingFile">
        /// The action to take if extraction would overwrite an existing file.
        /// </param>
        /// 
        public void ExtractSelectedEntries(String selectionCriteria, string directoryPathInArchive, string extractDirectory, ExtractExistingFileAction extractExistingFile)
        {
            foreach (ZipEntry e in SelectEntries(selectionCriteria, directoryPathInArchive))
            {
                e.Password = _Password; // possibly null
                e.Extract(extractDirectory, extractExistingFile);
            }
        }

    }

}



namespace Ionic
{
    internal abstract partial class SelectionCriterion
    {
        internal abstract bool Evaluate(Ionic.Zip.ZipEntry entry);
    }


    internal partial class NameCriterion : SelectionCriterion
    {
        internal override bool Evaluate(Ionic.Zip.ZipEntry entry)
        {
            // swap forward slashes in the entry.FileName for backslashes
            string transformedFileName = entry.FileName.Replace("/", "\\");

            return _Evaluate(transformedFileName);
        }
    }


    internal partial class SizeCriterion : SelectionCriterion
    {
        internal override bool Evaluate(Ionic.Zip.ZipEntry entry)
        {
            return _Evaluate(entry.UncompressedSize);
        }
    }

    internal partial class TimeCriterion : SelectionCriterion
    {
        internal override bool Evaluate(Ionic.Zip.ZipEntry entry)
        {
            DateTime x;
            switch (Which)
            {
                case WhichTime.atime:
                    x = entry.Atime;
                    break;
                case WhichTime.mtime:
                    x = entry.Mtime;
                    break;
                case WhichTime.ctime:
                    x = entry.Ctime;
                    break;
                default:
                    throw new ArgumentException("?time");
            }
            return _Evaluate(x);
        }
    }


    internal partial class AttributesCriterion : SelectionCriterion
    {
        internal override bool Evaluate(Ionic.Zip.ZipEntry entry)
        {
            System.IO.FileAttributes fileAttrs = entry.Attributes;
            return _Evaluate(fileAttrs);
        }
    }


    internal partial class CompoundCriterion : SelectionCriterion
    {
        internal override bool Evaluate(Ionic.Zip.ZipEntry entry)
        {
            bool result = Left.Evaluate(entry);
            switch (Conjunction)
            {
                case LogicalConjunction.AND:
                    if (result)
                        result = Right.Evaluate(entry);
                    break;
                case LogicalConjunction.OR:
                    if (!result)
                        result = Right.Evaluate(entry);
                    break;
                case LogicalConjunction.XOR:
                    result ^= Right.Evaluate(entry);
                    break;
                default:
                    throw new ArgumentException("Conjunction");
            }
            return result;
        }
    }



    public partial class FileSelector
    {
        private bool Evaluate(Ionic.Zip.ZipEntry entry)
        {
            bool result = _Criterion.Evaluate(entry);
            return result;
        }

        /// <summary>
        /// Retrieve the ZipEntry items in the ZipFile that conform to the specified criteria.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        /// This method applies the criteria set in the FileSelector instance (as described in
        /// the <see cref="FileSelector.SelectionCriteria"/>) to the specified ZipFile.  Using this
        /// method, for example, you can retrieve all entries from the given ZipFile that
        /// have filenames ending in .txt.
        /// </para>
        ///
        /// <para>
        /// Normally, applications would not call this method directly.  This method is used 
        /// by the ZipFile class.
        /// </para>
        ///
        /// <para>
        /// Using the appropriate SelectionCriteria, you can retrieve entries based on size,
        /// time, and attributes. See <see cref="FileSelector.SelectionCriteria"/> for a
        /// description of the syntax of the SelectionCriteria string.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <param name="zip">The ZipFile from which to retrieve entries.</param>
        ///
        /// <returns>a collection of ZipEntry objects that conform to the criteria.</returns>
        public System.Collections.Generic.ICollection<Ionic.Zip.ZipEntry> SelectEntries(Ionic.Zip.ZipFile zip)
        {
            var list = new System.Collections.Generic.List<Ionic.Zip.ZipEntry>();

            foreach (Ionic.Zip.ZipEntry e in zip)
            {
                if (this.Evaluate(e))
                    list.Add(e);
            }

            //return list.AsReadOnly();
            return list;
        }


        /// <summary>
        /// Retrieve the ZipEntry items in the ZipFile that conform to the specified criteria.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        /// This method applies the criteria set in the FileSelector instance (as described in
        /// the <see cref="FileSelector.SelectionCriteria"/>) to the specified ZipFile.  Using this
        /// method, for example, you can retrieve all entries from the given ZipFile that
        /// have filenames ending in .txt.
        /// </para>
        ///
        /// <para>
        /// Normally, applications would not call this method directly.  This method is used 
        /// by the ZipFile class.
        /// </para>
        ///
        /// <para>
        /// This overload allows the selection of ZipEntry instances from the ZipFile to be restricted 
        /// to entries contained within a particular directory in the ZipFile.
        /// </para>
        ///
        /// <para>
        /// Using the appropriate SelectionCriteria, you can retrieve entries based on size,
        /// time, and attributes. See <see cref="FileSelector.SelectionCriteria"/> for a
        /// description of the syntax of the SelectionCriteria string.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <param name="zip">The ZipFile from which to retrieve entries.</param>
        ///
        /// <param name="directoryPathInArchive">
        /// the directory in the archive from which to select entries. If null, then 
        /// all directories in the archive are used. 
        /// </param>
        /// 
        /// <returns>a collection of ZipEntry objects that conform to the criteria.</returns>
        public System.Collections.Generic.ICollection<Ionic.Zip.ZipEntry> SelectEntries(Ionic.Zip.ZipFile zip, string directoryPathInArchive)
        {
            var list = new System.Collections.Generic.List<Ionic.Zip.ZipEntry>();

            foreach (Ionic.Zip.ZipEntry e in zip)
            {
                if (directoryPathInArchive == null || System.IO.Path.GetDirectoryName(e.FileName) == directoryPathInArchive)
                    if (this.Evaluate(e))
                        list.Add(e);
            }

            //return list.AsReadOnly();
            return list.AsReadOnly();
        }

    }
}
