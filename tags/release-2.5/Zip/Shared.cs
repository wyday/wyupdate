// Shared.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2006, 2007, 2008, 2009 Dino Chiesa and Microsoft Corporation.  
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
// Time-stamp: <2009-September-01 12:13:31>
//
// ------------------------------------------------------------------
//
// This module defines some shared utility classes and methods.
//
// Created: Tue, 27 Mar 2007  15:30
// 

using System;
using System.IO;
using System.Security.Permissions;

namespace Ionic.Zip
{
    /// <summary>
    /// Collects general purpose utility methods.
    /// </summary>
    internal class SharedUtilities
    {
        /// private null constructor
        private SharedUtilities() { }


        // workitem 8423
        public static Int64 GetFileLength(string fileName)
        {
            if (!File.Exists(fileName))
                throw new System.IO.FileNotFoundException(fileName);
            
            long fileLength = 0L;
            FileShare fs = FileShare.ReadWrite;
#if !NETCF
            // FileShare.Delete is not defined for the Compact Framework
            fs |= FileShare.Delete;
#endif
            using (var s = File.Open(fileName, FileMode.Open, FileAccess.Read, fs))
            {
                fileLength = s.Length;
            }
            return fileLength;
        }

                        
#if LEGACY
        /// <summary>
        /// Round the given DateTime value to an even second value.  
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// Round up in the case of an odd second value.  The rounding does not consider
        /// fractional seconds.
        /// </para>
        /// <para>
        /// This is useful because the Zip spec allows storage of time only to the nearest
        /// even second.  So if you want to compare the time of an entry in the archive with
        /// it's actual time in the filesystem, you need to round the actual filesystem
        /// time, or use a 2-second threshold for the comparison.
        /// </para>
        /// <para>
        /// This is most nautrally an extension method for the DateTime class but this
        /// library is built for .NET 2.0, not for .NET 3.5; This means extension methods
        /// are a no-no.
        /// </para>
        /// </remarks>
        /// <param name="source">The DateTime value to round</param>
        /// <returns>The ruonded DateTime value</returns>
        public static DateTime RoundToEvenSecond(DateTime source)
        {
            // round to nearest second:
            if ((source.Second % 2) == 1)
                source += new TimeSpan(0, 0, 1);

            DateTime dtRounded = new DateTime(source.Year, source.Month, source.Day, source.Hour, source.Minute, source.Second);
            //if (source.Millisecond >= 500) dtRounded = dtRounded.AddSeconds(1);
            return dtRounded;
        }
#endif

        internal static string NormalizePath(string path)
        {
            // remove leading single dot slash
            if (path.StartsWith(".\\")) path = path.Substring(2);

            // remove intervening dot-slash
            path = path.Replace("\\.\\", "\\");

            // remove double dot when preceded by a directory name
            var re = new System.Text.RegularExpressions.Regex(@"^(.*\\)?([^\\\.]+\\\.\.\\)(.+)$");
            path = re.Replace(path, "$1$3");
            return path;
        }

        internal static string NormalizeFwdSlashPath(string path)
        {
            if (path.StartsWith("./")) path = path.Substring(2);
            path = path.Replace("/./", "/");
            var re = new System.Text.RegularExpressions.Regex(@"^(.*/)b?([^/\\.]+/\\.\\./)(.+)$");
            path = re.Replace(path, "$1$3");
            return path;
        }

        /// <summary>
        /// Utility routine for transforming path names. 
        /// </summary>
        /// <param name="pathName">source path.</param>
        /// <returns>transformed path</returns>
        public static string TrimVolumeAndSwapSlashes(string pathName)
        {
            //return (((pathname[1] == ':') && (pathname[2] == '\\')) ? pathname.Substring(3) : pathname)
            //    .Replace('\\', '/');
            if (String.IsNullOrEmpty(pathName)) return pathName;
            if (pathName.Length < 2) return pathName.Replace('\\', '/');
            return (((pathName[1] == ':') && (pathName[2] == '\\')) ? pathName.Substring(3) : pathName)
                .Replace('\\', '/');
        }

        static System.Text.Encoding ibm437 = System.Text.Encoding.GetEncoding("IBM437");
        static System.Text.Encoding utf8 = System.Text.Encoding.GetEncoding("UTF-8");

        internal static byte[] StringToByteArray(string value, System.Text.Encoding encoding)
        {
            byte[] a = encoding.GetBytes(value);
            return a;
        }
        internal static byte[] StringToByteArray(string value)
        {
            return StringToByteArray(value, ibm437);
        }

        //internal static byte[] Utf8StringToByteArray(string value)
        //{
        //    return StringToByteArray(value, utf8);
        //}

        //internal static string StringFromBuffer(byte[] buf, int maxlength)
        //{
        //    return StringFromBuffer(buf, maxlength, ibm437);
        //}

        internal static string Utf8StringFromBuffer(byte[] buf)
        {
            return StringFromBuffer(buf, utf8);
        }

        internal static string StringFromBuffer(byte[] buf, System.Text.Encoding encoding)
        {
            // this form of the GetString() method is required for .NET CF compatibility
            string s = encoding.GetString(buf, 0, buf.Length);
            return s;
        }


        internal static int ReadSignature(System.IO.Stream s)
        {
            // workitem 7711
            //return _ReadFourBytes(s, "Could not read signature - no data!  (position 0x{0:X8})");
            int x = 0;
            try { x = _ReadFourBytes(s, "nul"); }
            catch (BadReadException) { }
            return x;
        }

        internal static int ReadInt(System.IO.Stream s)
        {
            return _ReadFourBytes(s, "Could not read block - no data!  (position 0x{0:X8})");
        }

        private static int _ReadFourBytes(System.IO.Stream s, string message)
        {
            int n = 0;
            byte[] block = new byte[4];
            n = s.Read(block, 0, block.Length);
            if (n != block.Length) throw new BadReadException(String.Format(message, s.Position));
            int data = unchecked((((block[3] * 256 + block[2]) * 256) + block[1]) * 256 + block[0]);
            return data;
        }



        /// <summary>
        /// Finds a signature in the zip stream. This is useful for finding 
        /// the end of a zip entry, for example. 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="SignatureToFind"></param>
        /// <returns></returns>
        protected internal static long FindSignature(System.IO.Stream stream, int SignatureToFind)
        {
            long startingPosition = stream.Position;

            int BATCH_SIZE = 65536; //  8192;
            byte[] targetBytes = new byte[4];
            targetBytes[0] = (byte)(SignatureToFind >> 24);
            targetBytes[1] = (byte)((SignatureToFind & 0x00FF0000) >> 16);
            targetBytes[2] = (byte)((SignatureToFind & 0x0000FF00) >> 8);
            targetBytes[3] = (byte)(SignatureToFind & 0x000000FF);
            byte[] batch = new byte[BATCH_SIZE];
            int n = 0;
            bool success = false;
            do
            {
                n = stream.Read(batch, 0, batch.Length);
                if (n != 0)
                {
                    for (int i = 0; i < n; i++)
                    {
                        if (batch[i] == targetBytes[3])
                        {
                            long curPosition = stream.Position;
                            stream.Seek(i - n, System.IO.SeekOrigin.Current);

                            // workitem 7711
                            int sig = ReadSignature(stream);

                            success = (sig == SignatureToFind);
                            if (!success)
                            {
                                stream.Seek(curPosition, System.IO.SeekOrigin.Begin);
                            }
                            else
                                break; // out of for loop
                        }
                    }
                }
                else break;
                if (success) break;
            } while (true);

            if (!success)
            {
                stream.Seek(startingPosition, System.IO.SeekOrigin.Begin);
                return -1;  // or throw?
            }

            // subtract 4 for the signature.
            long bytesRead = (stream.Position - startingPosition) - 4;

            return bytesRead;
        }


        // If I have a time in the .NET environment, and I want to use it for 
        // SetWastWriteTime() etc, then I need to adjust it for Win32. 
        internal static DateTime AdjustTime_DotNetToWin32(DateTime time)
        {
            if (time.Kind == DateTimeKind.Utc) return time;
            DateTime adjusted = time;
            if (DateTime.Now.IsDaylightSavingTime() && !time.IsDaylightSavingTime())
                adjusted = time - new System.TimeSpan(1, 0, 0);

            else if (!DateTime.Now.IsDaylightSavingTime() && time.IsDaylightSavingTime())
                adjusted = time + new System.TimeSpan(1, 0, 0);

            return adjusted;
        }

        // If I read a time from a file with GetLastWriteTime() (etc), I need
        // to adjust it for display in the .NET environment.  
        internal static DateTime AdjustTime_Win32ToDotNet(DateTime time)
        {
            if (time.Kind == DateTimeKind.Utc) return time;
            DateTime adjusted = time;
            if (DateTime.Now.IsDaylightSavingTime() && !time.IsDaylightSavingTime())
                adjusted = time + new System.TimeSpan(1, 0, 0);

            else if (!DateTime.Now.IsDaylightSavingTime() && time.IsDaylightSavingTime())
                adjusted = time - new System.TimeSpan(1, 0, 0);

            return adjusted;
        }



        internal static DateTime PackedToDateTime(Int32 packedDateTime)
        {
            // workitem 7074 & workitem 7170
            if (packedDateTime == 0xFFFF || packedDateTime == 0)
                return new System.DateTime(1995, 1, 1, 0, 0, 0, 0);  // return a fixed date when none is supplied.

            Int16 packedTime = unchecked((Int16)(packedDateTime & 0x0000ffff));
            Int16 packedDate = unchecked((Int16)((packedDateTime & 0xffff0000) >> 16));

            int year = 1980 + ((packedDate & 0xFE00) >> 9);
            int month = (packedDate & 0x01E0) >> 5;
            int day = packedDate & 0x001F;

            int hour = (packedTime & 0xF800) >> 11;
            int minute = (packedTime & 0x07E0) >> 5;
            //int second = packedTime & 0x001F;
            int second = (packedTime & 0x001F) * 2;

            // validation and error checking.
            // this is not foolproof but will catch most errors.
            if (second >= 60) { minute++; second = 0; }
            if (minute >= 60) { hour++; minute = 0; }
            if (hour >= 24) { day++; hour = 0; }

            DateTime d = System.DateTime.Now;
            bool success = false;
            try
            {
                d = new System.DateTime(year, month, day, hour, minute, second, 0);
                success = true;
            }
            catch (System.ArgumentOutOfRangeException)
            {
                if (year == 1980 && (month == 0 || day == 0))
                {
                    try
                    {
                        d = new System.DateTime(1980, 1, 1, hour, minute, second, 0);
                        success = true;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        try
                        {
                            d = new System.DateTime(1980, 1, 1, 0, 0, 0, 0);
                            success = true;
                        }
                        catch (System.ArgumentOutOfRangeException) { }

                    }
                }
            }
            if (!success)
            {
                string msg = String.Format("y({0}) m({1}) d({2}) h({3}) m({4}) s({5})", year, month, day, hour, minute, second);
                throw new ZipException(String.Format("Bad date/time format in the zip file. ({0})", msg));

            }
            d = DateTime.SpecifyKind(d, DateTimeKind.Local);
            return d;
        }


        internal
         static Int32 DateTimeToPacked(DateTime time)
        {
            // The time is passed in here only for purposes of writing LastModified to the
            // zip archive. It should always be LocalTime, but we convert anyway.  And,
            // since the time is being written out, it needs to be adjusted. 

            time = time.ToLocalTime();
            // workitem 7966
            time = AdjustTime_Win32ToDotNet(time);

            // see http://www.vsft.com/hal/dostime.htm for the format
            UInt16 packedDate = (UInt16)((time.Day & 0x0000001F) | ((time.Month << 5) & 0x000001E0) | (((time.Year - 1980) << 9) & 0x0000FE00));
            UInt16 packedTime = (UInt16)((time.Second / 2 & 0x0000001F) | ((time.Minute << 5) & 0x000007E0) | ((time.Hour << 11) & 0x0000F800));

            Int32 result = (Int32)(((UInt32)(packedDate << 16)) | packedTime);
            return result;
        }


        private static System.Random _rnd = new System.Random();

        /// <summary>
        /// Return a random filename, suitable for use as a temporary file.
        /// </summary>
        /// <remarks>
        /// The System.IO.Path.GetRandomFileName() method is not available on the Compact
        /// Framework, so this library provides its own substitute. 
        /// </remarks>
        /// <returns>a filename of the form DotNetZip-xxxxxxxx.tmp, where xxxxxxxx is replaced 
        /// by randomly chosen characters.</returns>
        public static string GetTempFilename()
        {
            string candidate = null;
            do
            {
                candidate = "DotNetZip-" + GenerateRandomStringImpl(8, 97) + ".tmp";
            } while (System.IO.File.Exists(candidate));

            return candidate;
        }


        private static string GenerateRandomStringImpl(int length, int delta)
        {
            bool WantMixedCase = (delta == 0);

            string result = "";
            char[] a = new char[length];

            for (int i = 0; i < length; i++)
            {
                if (WantMixedCase)
                    delta = (_rnd.Next(2) == 0) ? 65 : 97;
                a[i] = GetOneRandomChar(delta);
            }

            result = new System.String(a);
            return result;
        }


        private static char GetOneRandomChar(int delta)
        {
            // delta == 65 means uppercase
            // delta == 97 means lowercase
            return (char)(_rnd.Next(26) + delta);
        }


        /// <summary>
        /// Workitem 7889: handle ERROR_LOCK_VIOLATION during read
        /// </summary>
        /// <remarks>
        /// This could be gracefully handled with an extension attribute, but
        /// This assembly is built for .NET 2.0, so I cannot use them. 
        /// </remarks>
        internal static int ReadWithRetry(System.IO.Stream s, byte[] buffer, int offset, int count, string FileName)
        {
            int n = 0;
            bool done = false;
            int retries = 0;
            do
            {
                try
                {
                    n = s.Read(buffer, offset, count);
                    done = true;
                }
                catch (System.IO.IOException ioexc1)
                {

#if !NETCF
                    // Check if we can call GetHRForException, 
                    // which makes unmanaged code calls.
                    var p = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
                    if (p.IsUnrestricted())
                    {
#endif
                        uint hresult = _HRForException(ioexc1);
                        if (hresult != 0x80070021)  // ERROR_LOCK_VIOLATION
                            throw new System.IO.IOException(String.Format("Cannot read file {0}", FileName), ioexc1);
                        retries++;
                        if (retries > 10)
                            throw new System.IO.IOException(String.Format("Cannot read file {0}, at offset 0x{1:X8} after 10 retries", FileName, offset), ioexc1);

                        // max time waited on last retry = 250 + 10*550 = 5.75s
                        // aggregate time waited after 10 retries: 250 + 55*550 = 30.5s
                        System.Threading.Thread.Sleep(250 + retries * 550);

#if !NETCF
                    }
                    else
                    {
                        // The permission.Demand() failed. Therefore, we cannot call
                        // GetHRForException, and cannot do the subtle handling of
                        // ERROR_LOCK_VIOLATION.  Just bail.
                        throw;
                    }
#endif

                }
            }
            while (!done);

            return n;
        }


#if !NETCF
        // workitem 8009
        //
        // This method must remain separate.
        //
        // Marshal.GetHRForException() is needed to do special exception handling for
        // the read.  But, that method requires UnmanagedCode permissions, and is marked
        // with LinkDemand for UnmanagedCode.  In an ASP.NET medium trust environment,
        // where UnmanagedCode is restricted, will generate a SecurityException at the
        // time of JIT of the method that calls a method that is marked with LinkDemand
        // for UnmanagedCode. The SecurityException, if it is restricted, will occur
        // when this method is JITed.
        //
        // The Marshal.GetHRForException() is factored out of ReadWithRetry in order to
        // avoid the SecurityException at JIT compile time. Because _HRForException is
        // called only when the UnmanagedCode is allowed.  This means .NET never
        // JIT-compiles this method when UnmanagedCode is disallowed, and thus never
        // generates the JIT-compile time exception.
        //
#endif
        private static uint _HRForException(System.Exception ex1)
        {
            return unchecked((uint)System.Runtime.InteropServices.Marshal.GetHRForException(ex1));
        }

    }




    /// <summary> 
    /// A Stream wrapper, used for bookkeeping on input or output
    /// streams.  In some cases, it is not possible to get the Position
    /// of a stream, let's say, on a write-only output stream like
    /// ASP.NET's Response.Output, or on a different write-only stream
    /// provided as the destination for the zip by the application.
    /// In this case, we can use this counting stream to count the bytes
    /// read or written.
    /// </summary>
    internal class CountingStream : System.IO.Stream
    {
        private System.IO.Stream _s;
        private Int64 _bytesWritten;
        private Int64 _bytesRead;

        /// <summary>
        /// The  constructor.
        /// </summary>
        /// <param name="s">The underlying stream</param>
        public CountingStream(System.IO.Stream s)
            : base()
        {
            _s = s;
        }

        public Int64 BytesWritten
        {
            get { return _bytesWritten; }
        }

        public Int64 BytesRead
        {
            get { return _bytesRead; }
        }

        public void Adjust(Int64 delta)
        {
            _bytesWritten -= delta;
            if (_bytesWritten < 0)
                throw new InvalidOperationException();
            if (_s as CountingStream != null)
                ((CountingStream)_s).Adjust(delta);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = _s.Read(buffer, offset, count);
            _bytesRead += n;
            return n;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count == 0) return;
            _s.Write(buffer, offset, count);
            _bytesWritten += count;
        }

        public override bool CanRead
        {
            get { return _s.CanRead; }
        }
        public override bool CanSeek
        {
            get { return _s.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _s.CanWrite; }
        }

        public override void Flush()
        {
            _s.Flush();
        }

        public override long Length
        {
            get { return _s.Length; }   // bytesWritten??
        }

        public override long Position
        {
            get { return _s.Position; }
            set
            {
                _s.Seek(value, System.IO.SeekOrigin.Begin);
            }
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            return _s.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _s.SetLength(value);
        }
    }


}
