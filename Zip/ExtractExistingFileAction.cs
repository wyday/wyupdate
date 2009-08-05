// ExtractExistingFileAction.cs
// ------------------------------------------------------------------
//
// Copyright (c)  2009 Dino Chiesa
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
// Time-stamp: <2009-August-04 12:46:00>
//
// ------------------------------------------------------------------
//
// This module defines the ExtractExistingFileAction enum
//
// 
// ------------------------------------------------------------------


namespace Ionic.Zip
{

    /// <summary>
    /// An enum for the options when extracting an entry would overwrite an existing file. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// This enum describes the actions that the library can take when an
    /// <c>Extract()</c> or <c>ExtractWithPassword()</c> method is called to extract an
    /// entry to a filesystem, and the extraction would overwrite an existing filesystem
    /// file.
    /// </para>
    /// </remarks>
    public enum ExtractExistingFileAction
    {
        /// <summary>
        /// Throw an exception when extraction would overwrite an existing file. (For
        /// COM clients, this is a 0 (zero).)
        /// </summary>
        Throw,

        /// <summary>
        /// When extraction would overwrite an existing file, overwrite the file silently.
        /// The overwrite will happen even if the target file is marked as read-only.
        /// (For COM clients, this is a 1.)
        /// </summary>
        OverwriteSilently,

        /// <summary>
        /// When extraction would overwrite an existing file, don't overwrite the file, silently. 
        /// (For COM clients, this is a 2.)
        /// </summary>
        DontOverwrite,

        /// <summary>
        /// When extraction would overwrite an existing file, invoke the ExtractProgress
        /// event, using an event type of <see
        /// cref="ZipProgressEventType.Extracting_ExtractEntryWouldOverwrite"/>.  In
        /// this way, the application can decide, just-in-time, whether to overwrite the
        /// file. For example, a GUI application may wish to pop up a dialog to allow
        /// the user to choose.  (For COM clients, this is a 3.)
        /// </summary>
        InvokeExtractProgressEvent,
    }

}
