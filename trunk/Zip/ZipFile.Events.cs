// ZipFile.Events.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2008, 2009 Dino Chiesa and Microsoft Corporation.  
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
// Time-stamp: <2009-May-29 17:35:04>
//
// ------------------------------------------------------------------
//
// This module defines the methods for issuing events from the ZipFile class.
//
// ------------------------------------------------------------------
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

        #region Events

        private string ArchiveNameForEvent
        {
            get
            {
                return (_name != null) ? _name : "(stream)";
            }
        }


        /// <summary>
        /// An event handler invoked when a Save() starts, before and after each entry has been
        /// written to the archive, when a Save() completes, and during other Save events.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// Depending on the particular event, different properties on the
        /// SaveProgressEventArgs parameter are set.  The following table 
        /// summarizes the available EventTypes and the conditions under which this 
        /// event handler is invoked with a SaveProgressEventArgs with the given EventType.
        /// </para>
        /// 
        /// <list type="table">
        /// <listheader>
        /// <term>value of EntryType</term>
        /// <description>Meaning and conditions</description>
        /// </listheader>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_Started</term>
        /// <description>Fired when ZipFile.Save() begins. 
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_BeforeSaveEntry</term>
        /// <description>Fired within ZipFile.Save(), just before writing data for each particular entry. 
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_AfterSaveEntry</term>
        /// <description>Fired within ZipFile.Save(), just after having finished writing data for each 
        /// particular entry. 
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_Completed</term>
        /// <description>Fired when ZipFile.Save() has completed. 
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_AfterSaveTempArchive</term>
        /// <description>Fired after the temporary file has been created.  This happens only
        /// when saving to a disk file.  This event will not be invoked when saving to a stream.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_BeforeRenameTempArchive</term>
        /// <description>Fired just before renaming the temporary file to the permanent location.  This 
        /// happens only when saving to a disk file.  This event will not be invoked when saving to a stream.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_AfterRenameTempArchive</term>
        /// <description>Fired just after renaming the temporary file to the permanent location.  This 
        /// happens only when saving to a disk file.  This event will not be invoked when saving to a stream.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_AfterCompileSelfExtractor</term>
        /// <description>Fired after a self-extracting archive has finished compiling. 
        /// This EventType is used only within SaveSelfExtractor().
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Saving_BytesRead</term>
        /// <description>Set during the save of a particular entry, to update progress of the Save(). 
        /// When this EventType is set, the BytesTransferred is the number of bytes that have been read from the 
        /// source stream.  The TotalBytesToTransfer is the number of bytes in the uncompressed file.
        /// </description>
        /// </item>
        /// 
        /// </list>
        /// </remarks>
        ///
        /// <example>
        /// <code lang="C#">
        /// static bool justHadByteUpdate= false;
        /// public static void SaveProgress(object sender, SaveProgressEventArgs e)
        /// {
        ///     if (e.EventType == ZipProgressEventType.Saving_Started)
        ///         Console.WriteLine("Saving: {0}", e.ArchiveName);
        /// 
        ///     else if (e.EventType == ZipProgressEventType.Saving_Completed)
        ///     {
        ///         justHadByteUpdate= false; 
        ///         Console.WriteLine();
        ///         Console.WriteLine("Done: {0}", e.ArchiveName);
        ///     }
        /// 
        ///     else if (e.EventType == ZipProgressEventType.Saving_BeforeWriteEntry)
        ///     {
        ///         if (justHadByteUpdate) 
        ///             Console.WriteLine();
        ///         Console.WriteLine("  Writing: {0} ({1}/{2})",  
        ///                           e.CurrentEntry.FileName, e.EntriesSaved, e.EntriesTotal);
        ///         justHadByteUpdate= false;
        ///     }
        /// 
        ///     else if (e.EventType == ZipProgressEventType.Saving_EntryBytesRead)
        ///     {
        ///         if (justHadByteUpdate)
        ///             Console.SetCursorPosition(0, Console.CursorTop);
        ///          Console.Write("     {0}/{1} ({2:N0}%)", e.BytesTransferred, e.TotalBytesToTransfer,
        ///                       e.BytesTransferred / (0.01 * e.TotalBytesToTransfer ));
        ///         justHadByteUpdate= true;
        ///     }
        /// }
        /// 
        /// public static ZipUp(string targetZip, string directory)
        /// {
        ///   using (var zip = new ZipFile()) {
        ///     zip.SaveProgress += SaveProgress; 
        ///     zip.AddDirectory(directory);
        ///     zip.Save(targetZip);
        ///   }
        /// }
        ///
        /// </code>
        ///
        /// <code lang="VB">
        /// Public Sub SaveProgress(ByVal sender As Object, ByVal e As SaveProgressEventArgs)
        /// 
        ///     If (e.EventType = ZipProgressEventType.Saving_Started) Then
        ///         Console.WriteLine("Saving: {0}", e.ArchiveName)
        /// 
        ///     Elseif (e.EventType = ZipProgressEventType.Saving_Completed) Then
        ///     
        ///         justHadByteUpdate= False
        ///         Console.WriteLine()
        ///         Console.WriteLine("Done: {0}", e.ArchiveName)
        /// 
        ///     ElseIf (e.EventType = ZipProgressEventType.Saving_BeforeWriteEntry) Then
        ///         If (justHadByteUpdate) Then Console.WriteLine()
        ///         Console.WriteLine("  Writing: {0} ({1}/{2})", _
        ///                           e.CurrentEntry.FileName, e.EntriesSaved, e.EntriesTotal)
        ///         justHadByteUpdate= False
        ///     
        /// 
        ///     ElseIf (e.EventType = ZipProgressEventType.Saving_EntryBytesRead) Then
        ///         If (justHadByteUpdate) Then
        ///             Console.SetCursorPosition(0, Console.CursorTop)
        ///         End If
        ///         Console.Write("     {0}/{1} ({2:N0}%)", e.BytesTransferred, e.TotalBytesToTransfer, _
        ///                       (CDbl(e.BytesTransferred) / (0.01 * e.TotalBytesToTransfer )))
        ///         justHadByteUpdate= True
        ///     End If
        /// End Sub
        /// </code>
        ///
        /// <para>
        /// This is an example of using the SaveProgress events in a WinForms app.
        /// </para>
        /// <code>
        /// delegate void SaveEntryProgress(SaveProgressEventArgs e);
        /// delegate void ButtonClick(object sender, EventArgs e);
        ///
        /// public class WorkerOptions
        /// {
        ///     public string ZipName;
        ///     public string Folder;
        ///     public string Encoding;
        ///     public string Comment;
        ///     public int ZipFlavor;
        ///     public Zip64Option Zip64;
        /// }
        ///
        /// private int _progress2MaxFactor;
        /// private bool _saveCanceled;
        /// private long _totalBytesBeforeCompress;
        /// private long _totalBytesAfterCompress;
        /// private Thread _workerThread;
        ///
        ///
        /// private void btnZipup_Click(object sender, EventArgs e)
        /// {
        ///     KickoffZipup();
        /// }
        ///
        /// private void btnCancel_Click(object sender, EventArgs e)
        /// {
        ///     if (this.lblStatus.InvokeRequired)
        ///     {
        ///         this.lblStatus.Invoke(new ButtonClick(this.btnCancel_Click), new object[] { sender, e });
        ///     }
        ///     else
        ///     {
        ///         _saveCanceled = true;
        ///         lblStatus.Text = "Canceled...";
        ///         ResetState();
        ///     }
        /// }
        ///
        /// private void KickoffZipup()
        /// {
        ///     _folderName = tbDirName.Text;
        ///
        ///     if (_folderName == null || _folderName == "") return;
        ///     if (this.tbZipName.Text == null || this.tbZipName.Text == "") return;
        ///
        ///     // check for existence of the zip file:
        ///     if (System.IO.File.Exists(this.tbZipName.Text))
        ///     {
        ///         var dlgResult = MessageBox.Show(String.Format("The file you have specified ({0}) already exists." + 
        ///                                                       "  Do you want to overwrite this file?", this.tbZipName.Text), 
        ///                                         "Confirmation is Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        ///         if (dlgResult != DialogResult.Yes) return;
        ///         System.IO.File.Delete(this.tbZipName.Text);
        ///     }
        ///
        ///      _saveCanceled = false;
        ///     _nFilesCompleted = 0;
        ///     _totalBytesAfterCompress = 0;
        ///     _totalBytesBeforeCompress = 0;
        ///     this.btnOk.Enabled = false;
        ///     this.btnOk.Text = "Zipping...";
        ///     this.btnCancel.Enabled = true;
        ///     lblStatus.Text = "Zipping...";
        ///
        ///     var options = new WorkerOptions
        ///     {
        ///         ZipName = this.tbZipName.Text,
        ///         Folder = _folderName,
        ///         Encoding = "ibm437"
        ///     };
        ///
        ///     if (this.comboBox1.SelectedIndex != 0)
        ///     {
        ///         options.Encoding = this.comboBox1.SelectedItem.ToString();
        ///     }
        ///
        ///     if (this.radioFlavorSfxCmd.Checked)
        ///         options.ZipFlavor = 2;
        ///     else if (this.radioFlavorSfxGui.Checked)
        ///         options.ZipFlavor = 1;
        ///     else options.ZipFlavor = 0;
        ///
        ///     if (this.radioZip64AsNecessary.Checked)
        ///         options.Zip64 = Zip64Option.AsNecessary;
        ///     else if (this.radioZip64Always.Checked)
        ///         options.Zip64 = Zip64Option.Always;
        ///     else options.Zip64 = Zip64Option.Never;
        ///
        ///     options.Comment = String.Format("Encoding:{0} || Flavor:{1} || ZIP64:{2}\r\nCreated at {3} || {4}\r\n",
        ///                 options.Encoding,
        ///                 FlavorToString(options.ZipFlavor),
        ///                 options.Zip64.ToString(),
        ///                 System.DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss"),
        ///                 this.Text);
        ///
        ///     if (this.tbComment.Text != TB_COMMENT_NOTE)
        ///         options.Comment += this.tbComment.Text;
        ///
        ///     _workerThread = new Thread(this.DoSave);
        ///     _workerThread.Name = "Zip Saver thread";
        ///     _workerThread.Start(options);
        ///     this.Cursor = Cursors.WaitCursor;
        ///  }
        ///
        ///
        /// private void DoSave(Object p)
        /// {
        ///     WorkerOptions options = p as WorkerOptions;
        ///     try
        ///     {
        ///         using (var zip1 = new ZipFile())
        ///         {
        ///             zip1.ProvisionalAlternateEncoding = System.Text.Encoding.GetEncoding(options.Encoding);
        ///             zip1.Comment = options.Comment;
        ///             zip1.AddDirectory(options.Folder);
        ///             _entriesToZip = zip1.EntryFileNames.Count;
        ///             SetProgressBars();
        ///             zip1.SaveProgress += this.zip1_SaveProgress;
        ///
        ///             zip1.UseZip64WhenSaving = options.Zip64;
        ///
        ///             if (options.ZipFlavor == 1)
        ///                 zip1.SaveSelfExtractor(options.ZipName, SelfExtractorFlavor.WinFormsApplication);
        ///             else if (options.ZipFlavor == 2)
        ///                 zip1.SaveSelfExtractor(options.ZipName, SelfExtractorFlavor.ConsoleApplication);
        ///             else
        ///                 zip1.Save(options.ZipName);
        ///         }
        ///     }
        ///     catch (System.Exception exc1)
        ///     {
        ///         MessageBox.Show(String.Format("Exception while zipping: {0}", exc1.Message));
        ///         btnCancel_Click(null, null);
        ///     }
        /// }
        ///
        ///
        ///
        /// void zip1_SaveProgress(object sender, SaveProgressEventArgs e)
        /// {
        ///     switch (e.EventType)
        ///     {
        ///         case ZipProgressEventType.Saving_AfterWriteEntry:
        ///             StepArchiveProgress(e);
        ///             break;
        ///         case ZipProgressEventType.Saving_EntryBytesRead:
        ///             StepEntryProgress(e);
        ///             break;
        ///         case ZipProgressEventType.Saving_Completed:
        ///             SaveCompleted();
        ///             break;
        ///         case ZipProgressEventType.Saving_AfterSaveTempArchive:
        ///             // this event only occurs when saving an SFX file
        ///             TempArchiveSaved();
        ///             break;
        ///     }
        ///     if (_saveCanceled)
        ///         e.Cancel = true;
        /// }
        ///
        ///
        ///
        /// private void StepArchiveProgress(SaveProgressEventArgs e)
        /// {
        ///     if (this.progressBar1.InvokeRequired)
        ///     {
        ///         this.progressBar1.Invoke(new SaveEntryProgress(this.StepArchiveProgress), new object[] { e });
        ///     }
        ///     else
        ///     {
        ///         if (!_saveCanceled)
        ///         {
        ///             _nFilesCompleted++;
        ///             this.progressBar1.PerformStep();
        ///             _totalBytesAfterCompress += e.CurrentEntry.CompressedSize;
        ///             _totalBytesBeforeCompress += e.CurrentEntry.UncompressedSize;
        ///
        ///             // reset the progress bar for the entry:
        ///             this.progressBar2.Value = this.progressBar2.Maximum = 1;
        ///
        ///             this.Update();
        ///         }
        ///     }
        /// }
        ///
        ///
        /// private void StepEntryProgress(SaveProgressEventArgs e)
        /// {
        ///     if (this.progressBar2.InvokeRequired)
        ///     {
        ///         this.progressBar2.Invoke(new SaveEntryProgress(this.StepEntryProgress), new object[] { e });
        ///     }
        ///     else
        ///     {
        ///         if (!_saveCanceled)
        ///         {
        ///             if (this.progressBar2.Maximum == 1)
        ///             {
        ///                 // reset
        ///                 Int64 max = e.TotalBytesToTransfer;
        ///                 _progress2MaxFactor = 0;
        ///                 while (max > System.Int32.MaxValue)
        ///                 {
        ///                     max /= 2;
        ///                     _progress2MaxFactor++;
        ///                 }
        ///                 this.progressBar2.Maximum = (int)max;
        ///                 lblStatus.Text = String.Format("{0} of {1} files...({2})",
        ///                     _nFilesCompleted + 1, _entriesToZip, e.CurrentEntry.FileName);
        ///             }
        ///
        ///              int xferred = e.BytesTransferred >> _progress2MaxFactor;
        ///
        ///              this.progressBar2.Value = (xferred >= this.progressBar2.Maximum)
        ///                 ? this.progressBar2.Maximum
        ///                 : xferred;
        ///
        ///              this.Update();
        ///         }
        ///     }
        /// }
        ///
        /// private void SaveCompleted()
        /// {
        ///     if (this.lblStatus.InvokeRequired)
        ///     {
        ///         this.lblStatus.Invoke(new MethodInvoker(this.SaveCompleted));
        ///     }
        ///     else
        ///     {
        ///         lblStatus.Text = String.Format("Done, Compressed {0} files, {1:N0}% of original.",
        ///             _nFilesCompleted, (100.00 * _totalBytesAfterCompress) / _totalBytesBeforeCompress);
        ///          ResetState();
        ///     }
        /// }
        ///
        /// private void ResetState()
        /// {
        ///     this.btnCancel.Enabled = false;
        ///     this.btnOk.Enabled = true;
        ///     this.btnOk.Text = "Zip it!";
        ///     this.progressBar1.Value = 0;
        ///     this.progressBar2.Value = 0;
        ///     this.Cursor = Cursors.Default;
        ///     if (!_workerThread.IsAlive)
        ///         _workerThread.Join();
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Public Sub ZipUp(ByVal targetZip As String, ByVal directory As String)
        ///     Try 
        ///         Using zip As ZipFile = New ZipFile
        ///             AddHandler zip.SaveProgress, AddressOf MySaveProgress
        ///             zip.AddDirectory(directory)
        ///             zip.Save(targetZip)
        ///         End Using
        ///     Catch ex1 As Exception
        ///         Console.Error.WriteLine(("exception: " &amp; ex1.ToString))
        ///     End Try
        /// End Sub
        /// 
        /// Private Shared justHadByteUpdate As Boolean = False
        /// 
        /// Public Shared Sub MySaveProgress(ByVal sender As Object, ByVal e As SaveProgressEventArgs)
        ///     If (e.EventType Is ZipProgressEventType.Saving_Started) Then
        ///         Console.WriteLine("Saving: {0}", e.ArchiveName)
        /// 
        ///     ElseIf (e.EventType Is ZipProgressEventType.Saving_Completed) Then
        ///         CreateLargeZip.justHadByteUpdate = False
        ///         Console.WriteLine
        ///         Console.WriteLine("Done: {0}", e.ArchiveName)
        /// 
        ///     ElseIf (e.EventType Is ZipProgressEventType.Saving_BeforeWriteEntry) Then
        ///         If CreateLargeZip.justHadByteUpdate Then
        ///             Console.WriteLine
        ///         End If
        ///         Console.WriteLine("  Writing: {0} ({1}/{2})", e.CurrentEntry.FileName, e.EntriesSaved, e.EntriesTotal)
        ///         CreateLargeZip.justHadByteUpdate = False
        /// 
        ///     ElseIf (e.EventType Is ZipProgressEventType.Saving_EntryBytesRead) Then
        ///         If CreateLargeZip.justHadByteUpdate Then
        ///             Console.SetCursorPosition(0, Console.CursorTop)
        ///         End If
        ///         Console.Write("     {0}/{1} ({2:N0}%)", e.BytesTransferred, _
        ///                       e.TotalBytesToTransfer, _
        ///                       (CDbl(e.BytesTransferred) / (0.01 * e.TotalBytesToTransfer)))
        ///         CreateLargeZip.justHadByteUpdate = True
        ///     End If
        /// End Sub
        /// </code>
        /// </example>
        public event EventHandler<SaveProgressEventArgs> SaveProgress;


        internal bool OnSaveBlock(ZipEntry entry, Int64 bytesXferred, Int64 totalBytesToXfer)
        {
            if (SaveProgress != null)
            {
                lock (LOCK)
                {
                    var e = SaveProgressEventArgs.ByteUpdate(ArchiveNameForEvent, entry,
                                  bytesXferred, totalBytesToXfer);
                    SaveProgress(this, e);
                    if (e.Cancel)
                        _saveOperationCanceled = true;
                }
            }
            return _saveOperationCanceled;
        }

        private void OnSaveEntry(int current, ZipEntry entry, bool before)
        {
            if (SaveProgress != null)
            {
                lock (LOCK)
                {
                    var e = new SaveProgressEventArgs(ArchiveNameForEvent, before, _entries.Count, current, entry);
                    SaveProgress(this, e);
                    if (e.Cancel)
                        _saveOperationCanceled = true;
                }
            }
        }

        private void OnSaveEvent(ZipProgressEventType eventFlavor)
        {
            if (SaveProgress != null)
            {
                lock (LOCK)
                {
                    var e = new SaveProgressEventArgs(ArchiveNameForEvent, eventFlavor);
                    SaveProgress(this, e);
                    if (e.Cancel)
                        _saveOperationCanceled = true;
                }
            }
        }

        private void OnSaveStarted()
        {
            if (SaveProgress != null)
            {
                lock (LOCK)
                {
                    var e = SaveProgressEventArgs.Started(ArchiveNameForEvent);
                    SaveProgress(this, e);
                }
            }
        }
        private void OnSaveCompleted()
        {
            if (SaveProgress != null)
            {
                lock (LOCK)
                {
                    var e = SaveProgressEventArgs.Completed(ArchiveNameForEvent);
                    SaveProgress(this, e);
                }
            }
        }




        /// <summary>
        /// An event handler invoked before, during, and after the reading of a zip archive.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// Depending on the particular event being signaled, different properties on the
        /// ReadProgressEventArgs parameter are set.  The following table 
        /// summarizes the available EventTypes and the conditions under which this 
        /// event handler is invoked with a ReadProgressEventArgs with the given EventType.
        /// </para>
        /// 
        /// <list type="table">
        /// <listheader>
        /// <term>value of EntryType</term>
        /// <description>Meaning and conditions</description>
        /// </listheader>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Reading_Started</term>
        /// <description>Fired just as ZipFile.Read() begins. Meaningful properties: ArchiveName.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Reading_Completed</term>
        /// <description>Fired when ZipFile.Read() has completed. Meaningful properties: ArchiveName.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Reading_ArchiveBytesRead</term>
        /// <description>Fired while reading, updates the number of bytes read for the entire archive. 
        /// Meaningful properties: ArchiveName, CurrentEntry, BytesTransferred, TotalBytesToTransfer.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Reading_BeforeReadEntry</term>
        /// <description>Indicates an entry is about to be read from the archive.
        /// Meaningful properties: ArchiveName, EntriesTotal.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Reading_AfterReadEntry</term>
        /// <description>Indicates an entry has just been read from the archive.
        /// Meaningful properties: ArchiveName, EntriesTotal, CurrentEntry.
        /// </description>
        /// </item>
        ///
        /// </list>
        /// </remarks>
        public event EventHandler<ReadProgressEventArgs> ReadProgress;

        private void OnReadStarted()
        {
            if (ReadProgress != null)
            {
                lock (LOCK)
                {
                    var e = ReadProgressEventArgs.Started(ArchiveNameForEvent);
                    ReadProgress(this, e);
                }
            }
        }

        private void OnReadCompleted()
        {
            if (ReadProgress != null)
            {
                lock (LOCK)
                {
                    var e = ReadProgressEventArgs.Completed(ArchiveNameForEvent);
                    ReadProgress(this, e);
                }
            }
        }

        internal void OnReadBytes(ZipEntry entry)
        {
            if (ReadProgress != null)
            {
                lock (LOCK)
                {
                    var e = ReadProgressEventArgs.ByteUpdate(ArchiveNameForEvent,
                                        entry,
                                        ReadStream.Position,
                                        LengthOfReadStream);
                    ReadProgress(this, e);
                }
            }
        }

        internal void OnReadEntry(bool before, ZipEntry entry)
        {
            if (ReadProgress != null)
            {
                lock (LOCK)
                {
                    ReadProgressEventArgs e = (before)
                    ? ReadProgressEventArgs.Before(ArchiveNameForEvent, _entries.Count)
                    : ReadProgressEventArgs.After(ArchiveNameForEvent, entry, _entries.Count);
                    ReadProgress(this, e);
                }
            }
        }

        private Int64 _lengthOfReadStream = -99;
        private Int64 LengthOfReadStream
        {
            get
            {
                if (_lengthOfReadStream == -99)
                {
                    if (_ReadStreamIsOurs)
                    {
                        System.IO.FileInfo fi = new System.IO.FileInfo(_name);
                        _lengthOfReadStream = fi.Length;
                    }
                    else _lengthOfReadStream = -1;
                }
                return _lengthOfReadStream;
            }
        }


        /// <summary>
        /// An event handler invoked before, during, and after extraction of entries 
        /// in the zip archive. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// Depending on the particular event, different properties on the
        /// ExtractProgressEventArgs parameter are set.  The following table 
        /// summarizes the available EventTypes and the conditions under which this 
        /// event handler is invoked with a ExtractProgressEventArgs with the given EventType.
        /// </para>
        /// 
        /// <list type="table">
        /// <listheader>
        /// <term>value of EntryType</term>
        /// <description>Meaning and conditions</description>
        /// </listheader>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Extracting_BeforeExtractAll</term>
        /// <description>Set when ExtractAll() begins.  The ArchiveName, Overwrite,
        /// and ExtractLocation properties are meaningful.</description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Extracting_AfterExtractAll</term>
        /// <description>Set when ExtractAll() has completed.  The ArchiveName, 
        /// Overwrite, and ExtractLocation properties are meaningful.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Extracting_BeforeExtractEntry</term>
        /// <description>Set when an Extract() on an entry in the ZipFile has begun.  
        /// Properties that are meaningful:  ArchiveName, EntriesTotal, CurrentEntry, Overwrite, 
        /// ExtractLocation, EntriesExtracted.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Extracting_AfterExtractEntry</term>
        /// <description>Set when an Extract() on an entry in the ZipFile has completed.  
        /// Properties that are meaningful:  ArchiveName, EntriesTotal, CurrentEntry, Overwrite, 
        /// ExtractLocation, EntriesExtracted.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>ZipProgressEventType.Extracting_EntryBytesWritten</term>
        /// <description>Set within a call to Extract() on an entry in the ZipFile, as
        /// data is extracted for the entry.  Properties that are meaningful:  ArchiveName, 
        /// CurrentEntry, BytesTransferred, TotalBytesToTransfer. 
        /// </description>
        /// </item>
        /// 
        /// </list>
        /// 
        /// </remarks>
        ///
        /// <example>
        /// <code>
        /// private static bool justHadByteUpdate = false;
        /// public static void ExtractProgress(object sender, ExtractProgressEventArgs e)
        /// {
        ///   if(e.EventType == ZipProgressEventType.Extracting_EntryBytesWritten)
        ///   {
        ///     if (justHadByteUpdate)
        ///       Console.SetCursorPosition(0, Console.CursorTop);
        ///
        ///     Console.Write("   {0}/{1} ({2:N0}%)", e.BytesTransferred, e.TotalBytesToTransfer,
        ///                   e.BytesTransferred / (0.01 * e.TotalBytesToTransfer ));
        ///     justHadByteUpdate = true;
        ///   }
        ///   else if(e.EventType == ZipProgressEventType.Extracting_BeforeExtractEntry)
        ///   {
        ///     if (justHadByteUpdate) 
        ///       Console.WriteLine();
        ///     Console.WriteLine("Extracting: {0}", e.CurrentEntry.FileName);
        ///     justHadByteUpdate= false;
        ///   }
        /// }
        ///
        /// public static ExtractZip(string zipToExtract, string directory)
        /// {
        ///   string TargetDirectory= "extract";
        ///   using (var zip = ZipFile.Read(zipToExtract)) {
        ///     zip.ExtractProgress += ExtractProgress; 
        ///     foreach (var e in zip1)
        ///     {
        ///       e.Extract(TargetDirectory, true);
        ///     }
        ///   }
        /// }
        ///
        /// </code>
        /// <code lang="VB">
        /// Public Shared Sub Main(ByVal args As String())
        ///     Dim ZipToUnpack As String = "C1P3SML.zip"
        ///     Dim TargetDir As String = "ExtractTest_Extract"
        ///     Console.WriteLine("Extracting file {0} to {1}", ZipToUnpack, TargetDir)
        ///     Using zip1 As ZipFile = ZipFile.Read(ZipToUnpack)
        ///         AddHandler zip1.ExtractProgress, AddressOf MyExtractProgress
        ///         Dim e As ZipEntry
        ///         For Each e In zip1
        ///             e.Extract(TargetDir, True)
        ///         Next
        ///     End Using
        /// End Sub
        /// 
        /// Private Shared justHadByteUpdate As Boolean = False
        /// 
        /// Public Shared Sub MyExtractProgress(ByVal sender As Object, ByVal e As ExtractProgressEventArgs)
        ///     If (e.EventType Is ZipProgressEventType.Extracting_EntryBytesWritten) Then
        ///         If ExtractTest.justHadByteUpdate Then
        ///             Console.SetCursorPosition(0, Console.CursorTop)
        ///         End If
        ///         Console.Write("   {0}/{1} ({2:N0}%)", e.BytesTransferred, e.TotalBytesToTransfer, (CDbl(e.BytesTransferred) / (0.01 * e.TotalBytesToTransfer)))
        ///         ExtractTest.justHadByteUpdate = True
        ///     ElseIf (e.EventType Is ZipProgressEventType.Extracting_BeforeExtractEntry) Then
        ///         If ExtractTest.justHadByteUpdate Then
        ///             Console.WriteLine
        ///         End If
        ///         Console.WriteLine("Extracting: {0}", e.CurrentEntry.FileName)
        ///         ExtractTest.justHadByteUpdate = False
        ///     End If
        /// End Sub
        /// </code>
        /// </example>
        public event EventHandler<ExtractProgressEventArgs> ExtractProgress;



        private void OnExtractEntry(int current, bool before, ZipEntry currentEntry, string path)
        {
            if (ExtractProgress != null)
            {
                lock (LOCK)
                {
                    var e = new ExtractProgressEventArgs(ArchiveNameForEvent, before, _entries.Count, current, currentEntry, path);
                    ExtractProgress(this, e);
                    if (e.Cancel)
                        _extractOperationCanceled = true;
                }
            }
        }


        // Can be called from within ZipEntry._ExtractOne.
        internal bool OnExtractBlock(ZipEntry entry, Int64 bytesWritten, Int64 totalBytesToWrite)
        {
            if (ExtractProgress != null)
            {
                lock (LOCK)
                {
                    var e = ExtractProgressEventArgs.ByteUpdate(ArchiveNameForEvent, entry,
                                bytesWritten, totalBytesToWrite);
                    ExtractProgress(this, e);
                    if (e.Cancel)
                        _extractOperationCanceled = true;
                }
            }
            return _extractOperationCanceled;
        }


        // Can be called from within ZipEntry.InternalExtract.
        internal bool OnSingleEntryExtract(ZipEntry entry, string path, bool before)
        {
            if (ExtractProgress != null)
            {
                lock (LOCK)
                {
                    var e = (before)
            ? ExtractProgressEventArgs.BeforeExtractEntry(ArchiveNameForEvent, entry, path)
            : ExtractProgressEventArgs.AfterExtractEntry(ArchiveNameForEvent, entry, path);
                    ExtractProgress(this, e);
                    if (e.Cancel)
                        _extractOperationCanceled = true;
                }
            }
            return _extractOperationCanceled;
        }

        internal bool OnExtractExisting(ZipEntry entry, string path)
        {
            if (ExtractProgress != null)
            {
                lock (LOCK)
                {
                    var e = ExtractProgressEventArgs.ExtractExisting(ArchiveNameForEvent, entry, path);
                    ExtractProgress(this, e);
                    if (e.Cancel)
                        _extractOperationCanceled = true;
                }
            }
            return _extractOperationCanceled;
        }


        private void OnExtractAllCompleted(string path)
        {
            if (ExtractProgress != null)
            {
                lock (LOCK)
                {
                    var e = ExtractProgressEventArgs.ExtractAllCompleted(ArchiveNameForEvent,
                         path );
                    ExtractProgress(this, e);
                }
            }
        }


        private void OnExtractAllStarted(string path)
        {
            if (ExtractProgress != null)
            {
                lock (LOCK)
                {
                    var e = ExtractProgressEventArgs.ExtractAllStarted(ArchiveNameForEvent,
                         path );
                    ExtractProgress(this, e);
                }
            }
        }


        #endregion


    }
}
