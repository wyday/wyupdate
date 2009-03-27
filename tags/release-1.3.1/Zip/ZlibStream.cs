// Copyright (c) 2009, Dino Chiesa.  
// This code is licensed under the Microsoft public license.  See the license.txt file in the source
// distribution for details. 
//
// The zlib code is derived from the jzlib implementation, but significantly modified.
// The object model is not the same, and many of the behaviors are different.
// Nonetheless, in keeping with the license for jzlib, I am reproducing the copyright to that code here.
// 
// -----------------------------------------------------------------------
// Copyright (c) 2000,2001,2002,2003 ymnk, JCraft,Inc. All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright 
// notice, this list of conditions and the following disclaimer in 
// the documentation and/or other materials provided with the distribution.
// 
// 3. The names of the authors may not be used to endorse or promote products
// derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL JCRAFT,
// INC. OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
// OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
// EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

/*
 * This code is based on zlib-1.1.3;  credit to authors
 * Jean-loup Gailly(jloup@gzip.org) and Mark Adler(madler@alumni.caltech.edu)
 * and contributors of zlib.
 */


using System;
namespace Ionic.Zlib
{
    /// <summary>
    /// A class for compressing and decompressing streams using the Deflate algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Data can be compressed or decompressed, and either of those can be through reading or writing. 
    /// For more information on the Deflate algorithm, see IETF RFC 1951, "DEFLATE Compressed Data 
    /// Format Specification version 1.3." 
    /// </para>
    /// <para>
    /// This class is similar to <see cref="ZlibStream"/>, except that <c>ZlibStream</c> adds the RFC1950 
    /// header bytes to a compressed stream when compressing, or expects the RFC1950 header bytes when 
    /// decompressing. The <c>DeflateStream</c> does not.
    /// </para>
    /// </remarks>
    /// <seealso cref="DeflateStream" />
    /// <seealso cref="GZipStream" />
    public class DeflateStream : System.IO.Stream
    {
        internal ZlibBaseStream _baseStream;

        /// <summary>
        /// Create a DeflateStream using the specified CompressionMode.
        /// </summary>
        /// <remarks>
        /// The DeflateStream will use the default compression level.
        /// </remarks>
        /// <example>
        /// This example shows how to use a DeflateStream to compress data.
        /// <code>
        /// using (System.IO.Stream input = System.IO.File.OpenRead(fileToCompress))
        /// {
        ///     using (var raw = System.IO.File.Create(outputFile))
        ///     {
        ///         using (Stream compressor = new DeflateStream(raw, CompressionMode.Compress))
        ///         {
        ///             byte[] buffer = new byte[WORKING_BUFFER_SIZE];
        ///             int n= -1;
        ///             while (n != 0)
        ///             {
        ///                 if (n &gt; 0) 
        ///                     compressor.Write(buffer, 0, n);
        ///                 n= input.Read(buffer, 0, buffer.Length);
        ///             }
        ///         }
        ///     }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Dim outputFile As String = (fileToCompress &amp; ".compressed")
        /// Using input As Stream = File.OpenRead(fileToCompress)
        ///     Using raw As FileStream = File.Create(outputFile)
        /// 	Using compressor As Stream = New DeflateStream(raw, CompressionMode.Compress)
        /// 	    Dim buffer As Byte() = New Byte(4096) {}
        /// 	    Dim n As Integer = -1
        /// 	    Do While (n &lt;&gt; 0)
        /// 		If (n &gt; 0) Then
        /// 		    compressor.Write(buffer, 0, n)
        /// 		End If
        /// 		n = input.Read(buffer, 0, buffer.Length)
        /// 	    Loop
        /// 	End Using
        ///     End Using
        /// End Using
        /// </code>
        /// </example>
        /// <param name="stream">The stream which will be read or written.</param>
        /// <param name="mode">Indicates whether the DeflateStream will compress or decompress.</param>
        public DeflateStream(System.IO.Stream stream, CompressionMode mode)
            : this(stream, mode, CompressionLevel.LEVEL6_DEFAULT, false)
        {
        }

        /// <summary>
        /// Create a DeflateStream using the specified CompressionMode and the specified CompressionLevel.
        /// </summary>
        /// <example>
        /// This example shows how to use a DeflateStream to compress data.
        /// <code>
        /// using (System.IO.Stream input = System.IO.File.OpenRead(fileToCompress))
        /// {
        ///     using (var raw = System.IO.File.Create(outputFile))
        ///     {
        ///         using (Stream compressor = new DeflateStream(raw,
        ///                                                      CompressionMode.Compress, 
        ///                                                      CompressionLevel.BEST_COMPRESSION))
        ///         {
        ///             byte[] buffer = new byte[WORKING_BUFFER_SIZE];
        ///             int n= -1;
        ///             while (n != 0)
        ///             {
        ///                 if (n &gt; 0) 
        ///                     compressor.Write(buffer, 0, n);
        ///                 n= input.Read(buffer, 0, buffer.Length);
        ///             }
        ///         }
        ///     }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Dim outputFile As String = (fileToCompress &amp; ".compressed")
        /// Using input As Stream = File.OpenRead(fileToCompress)
        ///     Using raw As FileStream = File.Create(outputFile)
        /// 	Using compressor As Stream = New DeflateStream(raw, CompressionMode.Compress, CompressionLevel.BEST_COMPRESSION)
        /// 	    Dim buffer As Byte() = New Byte(4096) {}
        /// 	    Dim n As Integer = -1
        /// 	    Do While (n &lt;&gt; 0)
        /// 		If (n &gt; 0) Then
        /// 		    compressor.Write(buffer, 0, n)
        /// 		End If
        /// 		n = input.Read(buffer, 0, buffer.Length)
        /// 	    Loop
        /// 	End Using
        ///     End Using
        /// End Using
        /// </code>
        /// </example>
        /// <param name="stream">The stream to be read or written while deflating or inflating.</param>
        /// <param name="mode">Indicates whether the DeflateStream will compress or decompress.</param>
        /// <param name="level">A tuning knob to trade speed for effectiveness.</param>
        public DeflateStream(System.IO.Stream stream, CompressionMode mode, CompressionLevel level)
            : this(stream, mode, level, false)
        {
        }

        /// <summary>
        /// Create a DeflateStream using the specified CompressionMode, and explicitly specify whether
        /// the stream should be left open after Deflation or Inflation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This constructor allows the application to request that the captive stream remain open after
        /// the deflation or inflation occurs.  By default, after Close() is called on the stream, the 
        /// captive stream is also closed. In some cases this is not desired, for example if the stream 
        /// is a memory stream that will be re-read after compression.  Specify true for the 
        /// leaveOpen parameter to leave the stream open. 
        /// </para>
        /// <para>
        /// The DeflateStream will use the default compression level.
        /// </para>
        /// <para>
        /// See the other overloads of this constructor for example code.
        /// </para>
        /// </remarks>
        /// <param name="stream">The stream which will be read or written. This is called the 
        /// "captive" stream in other places in this documentation.</param>
        /// <param name="mode">Indicates whether the DeflateStream will compress or decompress.</param>
        /// <param name="leaveOpen">true if the application would like the stream to remain open after inflation/deflation.</param>
        public DeflateStream(System.IO.Stream stream, CompressionMode mode, bool leaveOpen)
            : this(stream, mode, CompressionLevel.LEVEL6_DEFAULT, leaveOpen)
        {
        }

        /// <summary>
        /// Create a DeflateStream using the specified CompressionMode and the specified CompressionLevel, 
        /// and explicitly specify whether
        /// the stream should be left open after Deflation or Inflation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This constructor allows the application to request that the captive stream remain open after
        /// the deflation or inflation occurs.  By default, after Close() is called on the stream, the 
        /// captive stream is also closed. In some cases this is not desired, for example if the stream 
        /// is a memory stream that will be re-read after compression.  Specify true for the 
        /// leaveOpen parameter to leave the stream open. 
        /// </para>
        /// </remarks>
        /// <example>
        /// This example shows how to use a DeflateStream to compress data.
        /// <code>
        /// using (System.IO.Stream input = System.IO.File.OpenRead(fileToCompress))
        /// {
        ///     using (var raw = System.IO.File.Create(outputFile))
        ///     {
        ///         using (Stream compressor = new DeflateStream(raw, CompressionMode.Compress, CompressionLevel.BEST_COMPRESSION, true))
        ///         {
        ///             byte[] buffer = new byte[WORKING_BUFFER_SIZE];
        ///             int n= -1;
        ///             while (n != 0)
        ///             {
        ///                 if (n &gt; 0) 
        ///                     compressor.Write(buffer, 0, n);
        ///                 n= input.Read(buffer, 0, buffer.Length);
        ///             }
        ///         }
        ///     }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Dim outputFile As String = (fileToCompress &amp; ".compressed")
        /// Using input As Stream = File.OpenRead(fileToCompress)
        ///     Using raw As FileStream = File.Create(outputFile)
        /// 	Using compressor As Stream = New DeflateStream(raw, CompressionMode.Compress, CompressionLevel.BEST_COMPRESSION, True)
        /// 	    Dim buffer As Byte() = New Byte(4096) {}
        /// 	    Dim n As Integer = -1
        /// 	    Do While (n &lt;&gt; 0)
        /// 		If (n &gt; 0) Then
        /// 		    compressor.Write(buffer, 0, n)
        /// 		End If
        /// 		n = input.Read(buffer, 0, buffer.Length)
        /// 	    Loop
        /// 	End Using
        ///     End Using
        /// End Using
        /// </code>
        /// </example>
        /// <param name="stream">The stream which will be read or written.</param>
        /// <param name="mode">Indicates whether the DeflateStream will compress or decompress.</param>
        /// <param name="leaveOpen">true if the application would like the stream to remain open after inflation/deflation.</param>
        /// <param name="level">A tuning knob to trade speed for effectiveness.</param>
        public DeflateStream(System.IO.Stream stream, CompressionMode mode, CompressionLevel level, bool leaveOpen)
        {
            _baseStream = new ZlibBaseStream(stream, mode, level, ZlibStreamFlavor.DEFLATE, leaveOpen);
        }

        #region Zlib properties

        /// <summary>
        /// This property sets the flush behavior on the stream.  
        /// Sorry, though, not sure exactly how to describe all the various settings.
        /// </summary>
        virtual public int FlushMode
        {
            get { return (this._baseStream._flushMode); }
            set { this._baseStream._flushMode = value; }
        }

        /// <summary>
        /// Callers can set the buffer size of the working buffer with this property.  
        /// </summary>
        /// <remarks>
        /// The working buffer is used for all stream operations.
        /// The default size is 1024 bytes.  The minimum size is 128 bytes. You may get better 
        /// performance with a larger buffer.  Then again, you might not.  I don't know, I haven't tested it.  
        /// </remarks>
        public int BufferSize
        {
            get
            {
                return this._baseStream._workingBuffer.Length;
            }
            set
            {
                if (value < this._baseStream.WORKING_BUFFER_SIZE_MIN) throw new ZlibException("Don't be silly. Use a bigger buffer.");
                this._baseStream._workingBuffer = new byte[value];
            }
        }

        /// <summary> Returns the total number of bytes input so far.</summary>
        virtual public long TotalIn
        {
            get
            {
                return this._baseStream._z.TotalBytesIn;
            }
        }

        /// <summary> Returns the total number of bytes output so far.</summary>
        virtual public long TotalOut
        {
            get
            {
                return this._baseStream._z.TotalBytesOut;
            }
        }

        #endregion

        #region System.IO.Stream methods
        /// <summary>
        /// Close the stream.  
        /// </summary>
        /// <remarks>
        /// This may or may not close the captive stream. 
        /// See the ctor's with leaveOpen parameters for more information.
        /// </remarks>
        public override void Close()
        {
            _baseStream.Close();
        }

        /// <summary>
        /// Indicates whether the stream can be read.
        /// </summary>
        /// <remarks>
        /// The return value depends on whether the captive stream supports reading.
        /// </remarks>
        public override bool CanRead
        {
            get { return _baseStream._stream.CanRead; }
        }

        /// <summary>
        /// Indicates whether the stream supports Seek operations.
        /// </summary>
        /// <remarks>
        /// Always returns false.
        /// </remarks>
        public override bool CanSeek
        {
            get { return false; }
        }


        /// <summary>
        /// Indicates whether the stream can be written.
        /// </summary>
        /// <remarks>
        /// The return value depends on whether the captive stream supports writing.
        /// </remarks>
        public override bool CanWrite
        {
            get { return _baseStream._stream.CanWrite; }
        }

        /// <summary>
        /// Flush the stream.
        /// </summary>
        public override void Flush()
        {
            _baseStream.Flush();
        }

        /// <summary>
        /// Reading this property always throws a NotImplementedException.
        /// </summary>
        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Reading or Writing this property always throws a NotImplementedException.
        /// </summary>
        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Read data from the stream. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// If you wish to use the DeflateStream to compress data while reading, you can create
        /// a DeflateStream with CompressionMode.Compress, providing an uncompressed data
        /// stream.  Then call Read() on that DeflateStream, and the data read will be
        /// compressed.  If you wish to use the DeflateStream to decompress data while reading,
        /// you can create a DeflateStream with CompressionMode.Decompress, providing a
        /// readable compressed data stream.  Then call Read() on that DeflateStream, and the
        /// data read will be decompressed.
        /// </para>
        /// <para>
        /// A DeflateStream can be used for Read() or Write(), but not both. 
        /// </para>
        /// </remarks>
        /// <param name="buffer">The buffer into which the read data should be placed.</param>
        /// <param name="offset">the offset within that data array to put the first byte read.</param>
        /// <param name="count">the number of bytes to read.</param>
        /// <returns>the number of bytes actually read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _baseStream.Read(buffer, offset, count);
        }


        /// <summary>
        /// Calling this method always throws a <see cref="NotImplementedException"/>.
        /// </summary>
        /// <param name="offset">this is irrelevant, since it will always throw!</param>
        /// <param name="origin">this is irrelevant, since it will always throw!</param>
        /// <returns>irrelevant!</returns>
        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calling this method always throws a NotImplementedException.
        /// </summary>
        /// <param name="value">this is irrelevant, since it will always throw!</param>
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Write data to the stream. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// If you wish to use the DeflateStream to compress data while writing, you can create a DeflateStream with 
        /// CompressionMode.Compress, and a writable output stream.  Then call Write() on that DeflateStream, 
        /// providing uncompressed data as input.  The data sent to the output stream will be the compressed form of the data written.
        /// If you wish to use the DeflateStream to decompress data while writing, you can create a DeflateStream with 
        /// CompressionMode.Decompress, and a writable output stream.  Then call Write() on that stream, providing previously 
        /// compressed data. The data sent to the output stream will be the decompressed form of the data written.   
        /// </para>
        /// <para>
        /// A DeflateStream can be used for Read() or Write(), but not both. 
        /// </para>
        /// </remarks>
        /// <param name="buffer">The buffer holding data to write to the stream.</param>
        /// <param name="offset">the offset within that data array to find the first byte to write.</param>
        /// <param name="count">the number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _baseStream.Write(buffer, offset, count);
        }
        #endregion
    }


    /// <summary>
    /// A class for compressing and decompressing GZIP streams.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Like the GZipStream in the .NET Base Class Library, the Ionic.Zlib.GZipStream can
    /// compress while writing, or decompress while reading, but not vice versa. 
    /// </para>
    /// <para>
    /// A GZipStream can be used for Read() or Write(), and thus decompression or compression, but
    /// not both.
    /// </para>
    /// <para>
    /// If you wish to use the GZipStream to compress (deflate) data, you must wrap it around a
    /// write-able stream. As you call Write() on the GZipStream, the data will be compressed into
    /// the GZIP (IETF RFC 1952) format.
    /// </para>
    /// <para>
    /// If you want to decompress (inflate) data, you must wrap the GZipStream around a readable
    /// stream that contains an IETF RFC 1952-compliant stream.  The data will be decompressed as
    /// you call Read() on the GZipStream.
    /// </para>
    /// <para>
    /// Though the GZIP format allows data from multiple files to be concatenated together, 
    /// this stream handles only a single segment of GZIP format, typically representing a single file.
    /// </para>
    /// <para>
    /// For more information on GZIP, see IETF RFC 1952, "GZIP file format specification version 4.3".
    /// </para>
    /// <para>
    /// This class is similar to <see cref="ZlibStream"/> and <see cref="DeflateStream"/>.
    /// <c>ZlibStream</c> handles RFC1950-compliant streams.  <see cref="DeflateStream"/>
    /// handles RFC1951-compliant streams. And this class handles RFC1952-compliant streams.
    /// </para>
    /// </remarks>
    /// <seealso cref="DeflateStream" />
    /// <seealso cref="ZlibStream" />
    public class GZipStream : System.IO.Stream
    {
        // GZip format
        // source: http://tools.ietf.org/html/rfc1952
        // 
        //  header id:           2 bytes    1F 8B
        //  compress method      1 byte     8= DEFLATE (none other supported)
        //  flag                 1 byte     bitfield (See below)
        //  mtime                4 bytes    time_t (seconds since jan 1, 1970 UTC of the file.
        //  xflg                 1 byte     2 = max compress used , 4 = max speed (can be ignored)
        //  OS                   1 byte     OS for originating archive. set to 0xFF in compression. 
        //  extra field length   2 bytes    optional - only if FEXTRA is set.
        //  extra field          varies     
        //  filename             varies     optional - if FNAME is set.  zero terminated. ISO-8859-1.
        //  file comment         varies     optional - if FCOMMENT is set. zero terminated. ISO-8859-1.
        //  crc16                1 byte     optional - present only if FHCRC bit is set
        //  compressed data      varies
        //  CRC32                4 bytes   
        //  isize                4 bytes    data size modulo 2^32
        // 
        //     FLG (FLaGs)
        //                bit 0   FTEXT - indicates file is ASCII text (can be safely ignored)
        //                bit 1   FHCRC - there is a CRC16 for the header immediately following the header
        //                bit 2   FEXTRA - extra fields are present
        //                bit 3   FNAME - the zero-terminated filename is present. encoding; ISO-8859-1.
        //                bit 4   FCOMMENT  - a zero-terminated file comment is present. encoding: ISO-8859-1
        //                bit 5   reserved
        //                bit 6   reserved
        //                bit 7   reserved
        // 
        // On consumption: 
        // Extra field is a bunch of nonsense and can be safely ignored. 
        // Header CRC and OS, likewise.
        // 
        // on generation:
        // all optional fields get 0, except for the OS, which gets 255.
        // 

        /// <summary>
        /// The Comment on the GZIP stream.
        /// </summary>
        /// <remarks>
        /// The GZIP format allows for each file to have an associated comment stored with the file. 
        /// The comment is encoded with the ISO-8859-1 code page.  To include a comment in a GZIP stream you create, set this Comment before calling Write() for the first 
        /// time on the GZipStream.  When using GZipStream to decompress, you can retrieve this comment after the first call to Read().
        /// </remarks>
        public String Comment;


        /// <summary>
        /// The FileName for the GZIP stream.
        /// </summary>
        /// <remarks>
        /// The GZIP format allows each file to have an associated filename, encoded in the ISO-8859-1 code page. When compressing data (through Write()), set this FileName before 
        /// calling Write() the first time on the GZipStream. When decompressing (through Read()), you can retrieve this value any time after the first Read().
        /// </remarks>
        public String FileName
        {
            get { return _FileName; }
            set
            {
                _FileName = value;
                if (_FileName == null) return;
                if (_FileName.IndexOf("/") != -1)
                {
                    _FileName = _FileName.Replace("/", "\\");
                }
                if (_FileName.EndsWith("\\"))
                    throw new Exception("Illegal filename");
                if (_FileName.IndexOf("\\") != -1)
                {
                    // trim any leading path
                    _FileName = System.IO.Path.GetFileName(_FileName);
                }
            }
        }

        /// <summary>
        /// The last modified time for the GZIP stream.
        /// </summary>
        /// <remarks>
        /// GZIP allows the storage of a last modified time with each GZIP entry.  When compressing data, you can set 
        /// this before the first call to Write().  When decompressing, you can retrieve this value any time after the first
        /// call to Read().
        /// </remarks>
        public DateTime? LastModified;

        /// <summary>
        /// The CRC on the GZIP stream. 
        /// </summary>
        /// <remarks>
        /// This is used for internal error checking. You probably don't need to look at this property.
        /// </remarks>
        public int Crc32 { get { return _Crc32; } }


        internal ZlibBaseStream _baseStream;
        bool _firstReadDone;
        string _FileName;
        int _Crc32;

        /// <summary>
        /// Create a GZipStream using the specified CompressionMode. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// The GZipStream will use the default compression level.  
        /// </para>
        /// <para>
        /// As noted in the class documentation, 
        /// the CompressionMode (Compress or Decompress) also establishes the "direction" of the stream.  
        /// A GZipStream with CompressionMode.Compress works only through Write().  A GZipStream with 
        /// CompressionMode.Decompress works only through Read().
        /// </para>
        /// </remarks>
        /// <example>
        /// This example shows how to use a GZipStream to compress data.
        /// <code>
        /// using (System.IO.Stream input = System.IO.File.OpenRead(fileToCompress))
        /// {
        ///     using (var raw = System.IO.File.Create(outputFile))
        ///     {
        ///         using (Stream compressor = new GZipStream(raw, CompressionMode.Compress))
        ///         {
        ///             byte[] buffer = new byte[WORKING_BUFFER_SIZE];
        ///             int n= -1;
        ///             while (n != 0)
        ///             {
        ///                 if (n &gt; 0) 
        ///                     compressor.Write(buffer, 0, n);
        ///                 n= input.Read(buffer, 0, buffer.Length);
        ///             }
        ///         }
        ///     }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Dim outputFile As String = (fileToCompress &amp; ".compressed")
        /// Using input As Stream = File.OpenRead(fileToCompress)
        ///     Using raw As FileStream = File.Create(outputFile)
        /// 	Using compressor As Stream = New GZipStream(raw, CompressionMode.Compress)
        /// 	    Dim buffer As Byte() = New Byte(4096) {}
        /// 	    Dim n As Integer = -1
        /// 	    Do While (n &lt;&gt; 0)
        /// 		If (n &gt; 0) Then
        /// 		    compressor.Write(buffer, 0, n)
        /// 		End If
        /// 		n = input.Read(buffer, 0, buffer.Length)
        /// 	    Loop
        /// 	End Using
        ///     End Using
        /// End Using
        /// </code>
        /// </example>
        /// <param name="stream">The stream which will be read or written.</param>
        /// <param name="mode">Indicates whether the GZipStream will compress or decompress.</param>
        public GZipStream(System.IO.Stream stream, CompressionMode mode)
            : this(stream, mode, CompressionLevel.LEVEL6_DEFAULT, false)
        {
        }

        /// <summary>
        /// Create a GZipStream using the specified CompressionMode and the specified CompressionLevel.
        /// </summary>
        /// <remarks>
        /// <para>
        /// As noted in the class documentation, 
        /// the CompressionMode (Compress or Decompress) also establishes the "direction" of the stream.  
        /// A GZipStream with CompressionMode.Compress works only through Write().  A GZipStream with 
        /// CompressionMode.Decompress works only through Read().
        /// </para>
        /// </remarks>
        /// <example>
        /// This example shows how to use a GZipStream to compress data.
        /// <code>
        /// using (System.IO.Stream input = System.IO.File.OpenRead(fileToCompress))
        /// {
        ///     using (var raw = System.IO.File.Create(outputFile))
        ///     {
        ///         using (Stream compressor = new GZipStream(raw,
        ///                                                   CompressionMode.Compress, 
        ///                                                   CompressionLevel.BEST_COMPRESSION))
        ///         {
        ///             byte[] buffer = new byte[WORKING_BUFFER_SIZE];
        ///             int n= -1;
        ///             while (n != 0)
        ///             {
        ///                 if (n &gt; 0) 
        ///                     compressor.Write(buffer, 0, n);
        ///                 n= input.Read(buffer, 0, buffer.Length);
        ///             }
        ///         }
        ///     }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Dim outputFile As String = (fileToCompress &amp; ".compressed")
        /// Using input As Stream = File.OpenRead(fileToCompress)
        ///     Using raw As FileStream = File.Create(outputFile)
        /// 	Using compressor As Stream = New GZipStream(raw, CompressionMode.Compress, CompressionLevel.BEST_COMPRESSION)
        /// 	    Dim buffer As Byte() = New Byte(4096) {}
        /// 	    Dim n As Integer = -1
        /// 	    Do While (n &lt;&gt; 0)
        /// 		If (n &gt; 0) Then
        /// 		    compressor.Write(buffer, 0, n)
        /// 		End If
        /// 		n = input.Read(buffer, 0, buffer.Length)
        /// 	    Loop
        /// 	End Using
        ///     End Using
        /// End Using
        /// </code>
        /// </example>
        /// <param name="stream">The stream to be read or written while deflating or inflating.</param>
        /// <param name="mode">Indicates whether the GZipStream will compress or decompress.</param>
        /// <param name="level">A tuning knob to trade speed for effectiveness.</param>
        public GZipStream(System.IO.Stream stream, CompressionMode mode, CompressionLevel level)
            : this(stream, mode, level, false)
        {
        }

        /// <summary>
        /// Create a GZipStream using the specified CompressionMode, and explicitly specify whether
        /// the stream should be left open after Deflation or Inflation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This constructor allows the application to request that the captive stream remain open after
        /// the deflation or inflation occurs.  By default, after Close() is called on the stream, the 
        /// captive stream is also closed. In some cases this is not desired, for example if the stream 
        /// is a memory stream that will be re-read after compressed data has been written to it.  Specify true for the 
        /// leaveOpen parameter to leave the stream open. 
        /// </para>
        /// <para>
        /// As noted in the class documentation, 
        /// the CompressionMode (Compress or Decompress) also establishes the "direction" of the stream.  
        /// A GZipStream with CompressionMode.Compress works only through Write().  A GZipStream with 
        /// CompressionMode.Decompress works only through Read().
        /// </para>
        /// <para>
        /// The DeflateStream will use the default compression level.
        /// </para>
        /// <para>
        /// See the other overloads of this constructor for example code.
        /// </para>
        /// </remarks>
        /// <param name="stream">The stream which will be read or written. This is called the 
        /// "captive" stream in other places in this documentation.</param>
        /// <param name="mode">Indicates whether the GZipStream will compress or decompress.</param>
        /// <param name="leaveOpen">true if the application would like the base stream to remain open after inflation/deflation.</param>
        public GZipStream(System.IO.Stream stream, CompressionMode mode, bool leaveOpen)
            : this(stream, mode, CompressionLevel.LEVEL6_DEFAULT, leaveOpen)
        {
        }

        /// <summary>
        /// Create a GZipStream using the specified CompressionMode and the specified CompressionLevel,
        /// and explicitly specify whether the stream should be left open after Deflation or Inflation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This constructor allows the application to request that the captive stream remain open after
        /// the deflation or inflation occurs.  By default, after Close() is called on the stream, the 
        /// captive stream is also closed. In some cases this is not desired, for example if the stream 
        /// is a memory stream that will be re-read after compressed data has been written to it.  Specify true for the 
        /// leaveOpen parameter to leave the stream open. 
        /// </para>
        /// <para>
        /// As noted in the class documentation, 
        /// the CompressionMode (Compress or Decompress) also establishes the "direction" of the stream.  
        /// A GZipStream with CompressionMode.Compress works only through Write().  A GZipStream with 
        /// CompressionMode.Decompress works only through Read().
        /// </para>
        /// </remarks>
        /// <example>
        /// This example shows how to use a DeflateStream to compress data.
        /// <code>
        /// using (System.IO.Stream input = System.IO.File.OpenRead(fileToCompress))
        /// {
        ///     using (var raw = System.IO.File.Create(outputFile))
        ///     {
        ///         using (Stream compressor = new GZipStream(raw, CompressionMode.Compress, CompressionLevel.BEST_COMPRESSION, true))
        ///         {
        ///             byte[] buffer = new byte[WORKING_BUFFER_SIZE];
        ///             int n= -1;
        ///             while (n != 0)
        ///             {
        ///                 if (n &gt; 0) 
        ///                     compressor.Write(buffer, 0, n);
        ///                 n= input.Read(buffer, 0, buffer.Length);
        ///             }
        ///         }
        ///     }
        /// }
        /// </code>
        /// <code lang="VB">
        /// Dim outputFile As String = (fileToCompress &amp; ".compressed")
        /// Using input As Stream = File.OpenRead(fileToCompress)
        ///     Using raw As FileStream = File.Create(outputFile)
        /// 	Using compressor As Stream = New GZipStream(raw, CompressionMode.Compress, CompressionLevel.BEST_COMPRESSION, True)
        /// 	    Dim buffer As Byte() = New Byte(4096) {}
        /// 	    Dim n As Integer = -1
        /// 	    Do While (n &lt;&gt; 0)
        /// 		If (n &gt; 0) Then
        /// 		    compressor.Write(buffer, 0, n)
        /// 		End If
        /// 		n = input.Read(buffer, 0, buffer.Length)
        /// 	    Loop
        /// 	End Using
        ///     End Using
        /// End Using
        /// </code>
        /// </example>
        /// <param name="stream">The stream which will be read or written.</param>
        /// <param name="mode">Indicates whether the GZipStream will compress or decompress.</param>
        /// <param name="leaveOpen">true if the application would like the stream to remain open after inflation/deflation.</param>
        /// <param name="level">A tuning knob to trade speed for effectiveness.</param>
        public GZipStream(System.IO.Stream stream, CompressionMode mode, CompressionLevel level, bool leaveOpen)
        {
            _baseStream = new ZlibBaseStream(stream, mode, level, ZlibStreamFlavor.GZIP, leaveOpen);
        }

        #region Zlib properties

        /// <summary>
        /// This property sets the flush behavior on the stream.  
        /// Sorry, though, not sure exactly how to describe all the various settings.
        /// </summary>
        virtual public int FlushMode
        {
            get { return (this._baseStream._flushMode); }
            set { this._baseStream._flushMode = value; }
        }

        /// <summary>
        /// Callers can set the buffer size of the working buffer with this property.  
        /// </summary>
        /// <remarks>
        /// The working buffer is used for all stream operations.
        /// The default size is 1024 bytes.  The minimum size is 128 bytes. You may get better 
        /// performance with a larger buffer.  Then again, you might not.  I don't know, I haven't tested it.  
        /// </remarks>
        public int BufferSize
        {
            get
            {
                return this._baseStream._workingBuffer.Length;
            }
            set
            {
                if (value < this._baseStream.WORKING_BUFFER_SIZE_MIN) throw new ZlibException("Don't be silly. Use a bigger buffer.");
                this._baseStream._workingBuffer = new byte[value];
            }
        }

        /// <summary> Returns the total number of bytes input so far.</summary>
        virtual public long TotalIn
        {
            get
            {
                return this._baseStream._z.TotalBytesIn;
            }
        }

        /// <summary> Returns the total number of bytes output so far.</summary>
        virtual public long TotalOut
        {
            get
            {
                return this._baseStream._z.TotalBytesOut;
            }
        }

        #endregion

        #region System.IO.Stream methods
        /// <summary>
        /// Close the stream.  
        /// </summary>
        /// <remarks>
        /// This may or may not close the captive stream. 
        /// See the ctor's with leaveOpen parameters for more information.
        /// </remarks>
        public override void Close()
        {
            _baseStream.Close();
            this._Crc32 = _baseStream.Crc32;
        }

        /// <summary>
        /// Indicates whether the stream can be read.
        /// </summary>
        /// <remarks>
        /// The return value depends on whether the captive stream supports reading.
        /// </remarks>
        public override bool CanRead
        {
            get { return _baseStream._stream.CanRead; }
        }

        /// <summary>
        /// Indicates whether the stream supports Seek operations.
        /// </summary>
        /// <remarks>
        /// Always returns false.
        /// </remarks>
        public override bool CanSeek
        {
            get { return false; }
        }


        /// <summary>
        /// Indicates whether the stream can be written.
        /// </summary>
        /// <remarks>
        /// The return value depends on whether the captive stream supports writing.
        /// </remarks>
        public override bool CanWrite
        {
            get { return _baseStream._stream.CanWrite; }
        }

        /// <summary>
        /// Flush the stream.
        /// </summary>
        public override void Flush()
        {
            _baseStream.Flush();
        }

        /// <summary>
        /// Reading this property always throws a NotImplementedException.
        /// </summary>
        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Reading or Writing this property always throws a NotImplementedException.
        /// </summary>
        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Read and decompress data from the source stream.
        /// </summary>
        /// <remarks>
        /// With a GZipStream, decompression is done through reading.
        /// </remarks>
        /// <example>
        /// <code>
        /// byte[] working = new byte[WORKING_BUFFER_SIZE];
        /// int n= 1;
        /// using (System.IO.Stream input = System.IO.File.OpenRead(_CompressedFile))
        /// {
        ///     using (Stream decompressor= new Ionic.Zlib.GZipStream(input, CompressionMode.Decompress, true))
        ///     {
        ///         using (var output = System.IO.File.Create(_DecompressedFile))
        ///         {
        ///             while (n !=0)
        ///             {
        ///                 n= decompressor.Read(working, 0, working.Length);
        ///                 if (n &gt; 0) 
        ///                 {
        ///                     output.Write(working, 0, n);
        ///                 }
        ///             }
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="buffer">The buffer into which the decompressed data should be placed.</param>
        /// <param name="offset">the offset within that data array to put the first byte read.</param>
        /// <param name="count">the number of bytes to read.</param>
        /// <returns>the number of bytes actually read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = _baseStream.Read(buffer, offset, count);

            // Console.WriteLine("GZipStream::Read(buffer, off({0}), c({1}) = {2}", offset, count, n);
            // Console.WriteLine( Util.FormatByteArray(buffer, offset, n) );

            if (!_firstReadDone)
            {
                _firstReadDone = true;
                FileName = _baseStream._GzipFileName;
                Comment = _baseStream._GzipComment;
            }
            return n;
        }



        /// <summary>
        /// Calling this method always throws a <see cref="NotImplementedException"/>.
        /// </summary>
        /// <param name="offset">this is irrelevant, since it will always throw!</param>
        /// <param name="origin">this is irrelevant, since it will always throw!</param>
        /// <returns>irrelevant!</returns>
        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calling this method always throws a NotImplementedException.
        /// </summary>
        /// <param name="value">this is irrelevant, since it will always throw!</param>
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Write data to the stream. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// If you wish to use the GZipStream to compress data while writing, you can create a
        /// GZipStream with CompressionMode.Compress, and a writable output stream.  Then call
        /// Write() on that GZipStream, providing uncompressed data as input.  The data sent
        /// to the output stream will be the compressed form of the data written.  If you wish
        /// to use the DeflateStream to decompress data while writing, you can create a
        /// GZipStream with CompressionMode.Decompress, and a writable output stream.  Then
        /// call Write() on that stream, providing previously compressed data. The data sent to
        /// the output stream will be the decompressed form of the data written.
        /// </para>
        /// <para>
        /// A GZipStream can be used for Read() or Write(), but not both. 
        /// </para>
        /// </remarks>
        /// <param name="buffer">The buffer holding data to write to the stream.</param>
        /// <param name="offset">the offset within that data array to find the first byte to write.</param>
        /// <param name="count">the number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_baseStream._streamMode == Ionic.Zlib.ZlibBaseStream.StreamMode.Undefined)
            {
                //Console.WriteLine("GZipStream: First write");
                if (_baseStream._wantCompress)
                {
                    // first write in compression, therefore, emit the GZIP header
                    EmitHeader();
                }
                else
                {
                    // first write in decompression, therefore, pop off GZIP header
                    int n = SlurpHeader(buffer, offset);
                    offset += n;
                    count -= n;
                }
            }

            _baseStream.Write(buffer, offset, count);
        }
        #endregion


        internal static System.DateTime _unixEpoch = new System.DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        internal static System.Text.Encoding iso8859dash1 = System.Text.Encoding.GetEncoding("iso-8859-1");


        private int SlurpHeader(byte[] buffer, int offset)
        {
            int totalSlurped = 0;

            byte[] header = new byte[10];
            Array.Copy(buffer, offset, header, 0, header.Length);
            totalSlurped += header.Length;
            offset += header.Length;

            if (header[0] != 0x1F || header[1] != 0x8B || header[2] != 8)
                throw new ZlibException("Bad GZIP header.");

            if ((header[3] & 0x04) == 0x04)  // There is an extra field
            {
                // slurp and discard extra field
                Int16 extraLength = (Int16)(buffer[10] + buffer[11] * 256);

                offset += 2;
                byte[] extra = new byte[extraLength];

                Array.Copy(buffer, offset, extra, 0, extra.Length);
                totalSlurped += extra.Length;
                offset += extra.Length;
            }

            int count = 0;
            if ((header[3] & 0x08) == 0x08)
            {
                FileName = SlurpZeroTerminatedString(buffer, offset, out count);
                totalSlurped += count;
                offset += count;
            }

            if ((header[3] & 0x10) == 0x010)
            {
                Comment = SlurpZeroTerminatedString(buffer, offset, out count);
                totalSlurped += count;
                offset += count;
            }

            if ((header[3] & 0x02) == 0x02)
            {
                // CRC16, ignore the data
                totalSlurped += 1;
                offset += 1;
            }

            return totalSlurped;
        }

        private string SlurpZeroTerminatedString(byte[] buffer, int offset, out int count)
        {
            var list = new System.Collections.Generic.List<byte>();
            bool done = false;
            count = 0;
            do
            {
                int ix = offset + count;
                if (buffer[ix] == 0)
                    done = true;
                else
                    list.Add(buffer[ix]);
                count++;
            } while (!done);
            byte[] a = list.ToArray();
            return GZipStream.iso8859dash1.GetString(a, 0, a.Length);
        }


        private void EmitHeader()
        {

            byte[] commentBytes = (Comment == null) ? null : iso8859dash1.GetBytes(Comment);
            byte[] filenameBytes = (FileName == null) ? null : iso8859dash1.GetBytes(FileName);

            int cbLength = (Comment == null) ? 0 : commentBytes.Length + 1;
            int fnLength = (FileName == null) ? 0 : filenameBytes.Length + 1;

            int bufferLength = 10 + cbLength + fnLength;
            byte[] header = new byte[bufferLength];
            int i = 0;
            // ID
            header[i++] = 0x1F;
            header[i++] = 0x8B;

            // compression method
            header[i++] = 8;
            byte flag = 0;
            if (Comment != null)
                flag ^= 0x10;
            if (FileName != null)
                flag ^= 0x8;

            // flag
            header[i++] = flag;

            // mtime
            if (!LastModified.HasValue) LastModified = DateTime.Now;
            System.TimeSpan delta = LastModified.Value - _unixEpoch;
            Int32 timet = (Int32)delta.TotalSeconds;
            Array.Copy(BitConverter.GetBytes(timet), 0, header, i, 4);
            i += 4;

            // xflg
            header[i++] = 0;    // this field is totally useless
            // OS
            header[i++] = 0xFF; // 0xFF == unspecified

            // extra field length - only if FEXTRA is set, which it is not.
            //header[i++]= 0;
            //header[i++]= 0;

            // filename
            if (fnLength != 0)
            {
                Array.Copy(filenameBytes, 0, header, i, fnLength);
                i += fnLength;
                header[i++] = 0; // terminate
            }

            // comment
            if (cbLength != 0)
            {
                Array.Copy(commentBytes, 0, header, i, cbLength);
                i += cbLength;
                header[i++] = 0; // terminate
            }

            _baseStream._stream.Write(header, 0, header.Length);
        }
    }



    /// <summary>
    /// Represents a Zlib stream for compression or decompression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Data can be compressed or decompressed, and either of those can be through reading or writing. 
    /// For more information on the Deflate algorithm, see IETF RFC 1951, "DEFLATE Compressed Data 
    /// Format Specification version 1.3." 
    /// </para>
    /// <para>
    /// This class is similar to <see cref="DeflateStream"/>, except that it adds the RFC1950 header 
    /// bytes to a compressed stream when compressing, or expects the RFC1950 header bytes when 
    /// decompressing.  It is also similar to the <see cref="GZipStream"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="DeflateStream" />
    /// <seealso cref="GZipStream" />
    public class ZlibStream : System.IO.Stream
    {
        internal ZlibBaseStream _baseStream;

        /// <summary>
        /// Create a ZlibStream using the specified CompressionMode.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The ZlibStream will use the default compression level.
        /// </para>
        /// <para>
        /// See the documentation for the <see cref="DeflateStream"/> constructors for example code.
        /// </para>
        /// </remarks>
        /// <param name="stream">The stream which will be read or written.</param>
        /// <param name="mode">Indicates whether the ZlibStream will compress or decompress.</param>
        public ZlibStream(System.IO.Stream stream, CompressionMode mode)
            : this(stream, mode, CompressionLevel.LEVEL6_DEFAULT, false)
        {
        }

        /// <summary>
        /// Create a ZlibStream using the specified CompressionMode and the specified CompressionLevel.
        /// </summary>
        /// <remarks>
        /// See the documentation for the <see cref="DeflateStream"/> constructors for example code.
        /// </remarks>
        /// <param name="stream">The stream to be read or written while deflating or inflating.</param>
        /// <param name="mode">Indicates whether the ZlibStream will compress or decompress.</param>
        /// <param name="level">A tuning knob to trade speed for effectiveness.</param>
        public ZlibStream(System.IO.Stream stream, CompressionMode mode, CompressionLevel level)
            : this(stream, mode, level, false)
        {
        }

        /// <summary>
        /// Create a ZlibStream using the specified CompressionMode, and explicitly specify whether
        /// the stream should be left open after Deflation or Inflation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This constructor allows the application to request that the captive stream remain open after
        /// the deflation or inflation occurs.  By default, after Close() is called on the stream, the 
        /// captive stream is also closed. In some cases this is not desired, for example if the stream 
        /// is a memory stream that will be re-read after compression.  Specify true for the 
        /// leaveOpen parameter to leave the stream open. 
        /// </para>
        /// <para>
        /// The ZlibStream will use the default compression level.
        /// </para>
        /// <para>
        /// See the documentation for the <see cref="DeflateStream"/> constructors for example code.
        /// </para>
        /// </remarks>
        /// <param name="stream">The stream which will be read or written. This is called the 
        /// "captive" stream in other places in this documentation.</param>
        /// <param name="mode">Indicates whether the ZlibStream will compress or decompress.</param>
        /// <param name="leaveOpen">true if the application would like the stream to remain open after inflation/deflation.</param>
        public ZlibStream(System.IO.Stream stream, CompressionMode mode, bool leaveOpen)
            : this(stream, mode, CompressionLevel.LEVEL6_DEFAULT, leaveOpen)
        {
        }

        /// <summary>
        /// Create a ZlibStream using the specified CompressionMode and the specified CompressionLevel, 
        /// and explicitly specify whether
        /// the stream should be left open after Deflation or Inflation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This constructor allows the application to request that the captive stream remain open after
        /// the deflation or inflation occurs.  By default, after Close() is called on the stream, the 
        /// captive stream is also closed. In some cases this is not desired, for example if the stream 
        /// is a memory stream that will be re-read after compression.  Specify true for the 
        /// leaveOpen parameter to leave the stream open. 
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// <code>
        /// public void TestStreamCompression()
        /// {
        ///     System.IO.MemoryStream msSinkCompressed;
        ///     System.IO.MemoryStream msSinkDecompressed;
        ///     ZlibStream zOut;
        ///     String helloOriginal = "Hello, World!  This String will be compressed...";
        /// 
        ///     // first, compress:
        ///     msSinkCompressed = new System.IO.MemoryStream();
        ///     zOut = new ZlibStream(msSinkCompressed, CompressionMode.Compress, CompressionLevel.LEVEL9_BEST_COMPRESSION, true);
        ///     CopyStream(StringToMemoryStream(helloOriginal), zOut);
        ///     zOut.Close();
        /// 
        ///     // at this point, msSinkCompressed contains the compressed bytes
        /// 
        ///     // now, decompress:
        ///     msSinkDecompressed = new System.IO.MemoryStream();
        ///     zOut = new ZlibStream(msSinkDecompressed, CompressionMode.Decompress);
        ///     msSinkCompressed.Position = 0;
        ///     CopyStream(msSinkCompressed, zOut);
        /// 
        ///     string result = MemoryStreamToString(msSinkDecompressed);
        ///     Console.WriteLine("decompressed: {0}", result);
        /// }
        /// 
        /// private static void CopyStream(System.IO.Stream src, System.IO.Stream dest)
        /// {
        ///     byte[] buffer = new byte[1024];
        ///     int len = src.Read(buffer, 0, buffer.Length);
        ///     while (len &gt; 0)
        ///     {
        ///         dest.Write(buffer, 0, len);
        ///         len = src.Read(buffer, 0, buffer.Length);
        ///     }
        ///     dest.Flush();
        /// }
        /// 
        /// static System.IO.MemoryStream StringToMemoryStream(string s)
        /// {
        ///     System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
        ///     int byteCount = enc.GetByteCount(s.ToCharArray(), 0, s.Length);
        ///     byte[] ByteArray = new byte[byteCount];
        ///     int bytesEncodedCount = enc.GetBytes(s, 0, s.Length, ByteArray, 0);
        ///     System.IO.MemoryStream ms = new System.IO.MemoryStream(ByteArray);
        ///     return ms;
        /// }
        /// 
        /// static String MemoryStreamToString(System.IO.MemoryStream ms)
        /// {
        ///     byte[] ByteArray = ms.ToArray();
        ///     System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
        ///     var s = enc.GetString(ByteArray);
        ///     return s;
        /// }
        /// </code>
        /// </example>
        /// <param name="stream">The stream which will be read or written.</param>
        /// <param name="mode">Indicates whether the ZlibStream will compress or decompress.</param>
        /// <param name="leaveOpen">true if the application would like the stream to remain open after inflation/deflation.</param>
        /// <param name="level">A tuning knob to trade speed for effectiveness.</param>
        public ZlibStream(System.IO.Stream stream, CompressionMode mode, CompressionLevel level, bool leaveOpen)
        {
            _baseStream = new ZlibBaseStream(stream, mode, level, ZlibStreamFlavor.ZLIB, leaveOpen);
        }

        #region Zlib properties

        /// <summary>
        /// This property sets the flush behavior on the stream.  
        /// Sorry, though, not sure exactly how to describe all the various settings.
        /// </summary>
        virtual public int FlushMode
        {
            get { return (this._baseStream._flushMode); }
            set { this._baseStream._flushMode = value; }
        }

        /// <summary>
        /// Callers can set the buffer size of the working buffer with this property.  
        /// </summary>
        /// <remarks>
        /// The working buffer is used for all stream operations.
        /// The default size is 1024 bytes.  The minimum size is 128 bytes. You may get better 
        /// performance with a larger buffer.  Then again, you might not.  I don't know, I haven't tested it.  
        /// </remarks>
        public int BufferSize
        {
            get
            {
                return this._baseStream._workingBuffer.Length;
            }
            set
            {
                if (value < this._baseStream.WORKING_BUFFER_SIZE_MIN) throw new ZlibException("Don't be silly. Use a bigger buffer.");
                this._baseStream._workingBuffer = new byte[value];
            }
        }


        /// <summary> Returns the total number of bytes input so far.</summary>
        virtual public long TotalIn
        {
            get { return this._baseStream._z.TotalBytesIn; }
        }

        /// <summary> Returns the total number of bytes output so far.</summary>
        virtual public long TotalOut
        {
            get { return this._baseStream._z.TotalBytesOut; }
        }

        #endregion

        #region System.IO.Stream methods
        /// <summary>
        /// Close the stream.  
        /// </summary>
        /// <remarks>
        /// This may or may not close the captive stream. 
        /// See the ctor's with leaveOpen parameters for more information.
        /// </remarks>
        public override void Close()
        {
            _baseStream.Close();
        }

        /// <summary>
        /// Indicates whether the stream can be read.
        /// </summary>
        /// <remarks>
        /// The return value depends on whether the captive stream supports reading.
        /// </remarks>
        public override bool CanRead
        {
            get { return _baseStream._stream.CanRead; }
        }

        /// <summary>
        /// Indicates whether the stream supports Seek operations.
        /// </summary>
        /// <remarks>
        /// Always returns false.
        /// </remarks>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        /// Indicates whether the stream can be written.
        /// </summary>
        /// <remarks>
        /// The return value depends on whether the captive stream supports writing.
        /// </remarks>
        public override bool CanWrite
        {
            get { return _baseStream._stream.CanWrite; }
        }

        /// <summary>
        /// Flush the stream.
        /// </summary>
        public override void Flush()
        {
            _baseStream.Flush();
        }

        /// <summary>
        /// Reading this property always throws a NotImplementedException.
        /// </summary>
        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Reading or Writing this property always throws a NotImplementedException.
        /// </summary>
        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Read data from the stream. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// If you wish to use the ZlibStream to compress data while reading, you can create a ZlibStream with 
        /// CompressionMode.Compress, providing an uncompressed data stream.  Then call Read() on that ZlibStream, 
        /// and the data read will be compressed. 
        /// If you wish to use the ZlibStream to decompress data while reading, you can create a ZlibStream with 
        /// CompressionMode.Decompress, providing a readable compressed data stream.  Then call Read() on that ZlibStream, 
        /// and the data read will be decompressed. 
        /// </para>
        /// <para>
        /// A ZlibStream can be used for Read() or Write(), but not both. 
        /// </para>
        /// </remarks>
        /// <param name="buffer">The buffer into which the read data should be placed.</param>
        /// <param name="offset">the offset within that data array to put the first byte read.</param>
        /// <param name="count">the number of bytes to read.</param>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _baseStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Calling this method always throws a NotImplementedException.
        /// </summary>
        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calling this method always throws a NotImplementedException.
        /// </summary>
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Write data to the stream. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// If you wish to use the ZlibStream to compress data while writing, you can create a ZlibStream with 
        /// CompressionMode.Compress, and a writable output stream.  Then call Write() on that ZlibStream, 
        /// providing uncompressed data as input.  The data sent to the output stream will be the compressed form of the data written.
        /// If you wish to use the ZlibStream to decompress data while writing, you can create a ZlibStream with 
        /// CompressionMode.Decompress, and a writable output stream.  Then call Write() on that stream, providing previously 
        /// compressed data. The data sent to the output stream will be the decompressed form of the data written.   
        /// </para>
        /// <para>
        /// A ZlibStream can be used for Read() or Write(), but not both. 
        /// </para>
        /// </remarks>
        /// <param name="buffer">The buffer holding data to write to the stream.</param>
        /// <param name="offset">the offset within that data array to find the first byte to write.</param>
        /// <param name="count">the number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _baseStream.Write(buffer, offset, count);
        }
        #endregion
    }


    internal enum ZlibStreamFlavor { ZLIB = 1950, DEFLATE = 1951, GZIP = 1952 }

    internal class ZlibBaseStream : System.IO.Stream
    {
        protected internal ZlibCodec _z = new ZlibCodec();
        protected internal readonly int WORKING_BUFFER_SIZE_DEFAULT = 16384; // 1024;
        protected internal readonly int WORKING_BUFFER_SIZE_MIN = 128;

        protected internal StreamMode _streamMode = StreamMode.Undefined;
        protected internal int _flushMode;
        protected internal ZlibStreamFlavor _flavor;
        protected internal bool _leaveOpen;
        protected internal byte[] _workingBuffer;
        protected internal byte[] _buf1 = new byte[1];
        protected internal bool _wantCompress;

        protected internal System.IO.Stream _stream;

        // workitem 7159
        Ionic.Zlib.CRC32 crc;
        protected internal string _GzipFileName;
        protected internal string _GzipComment;
        protected internal DateTime _GzipMtime;

        internal int Crc32 { get { if (crc == null) return 0; return crc.Crc32Result; } }

        public ZlibBaseStream(System.IO.Stream stream, CompressionMode compressionMode, CompressionLevel level, ZlibStreamFlavor flavor, bool leaveOpen)
            : base()
        {
            this._flushMode = ZlibConstants.Z_NO_FLUSH;
            this._workingBuffer = new byte[WORKING_BUFFER_SIZE_DEFAULT];
            this._stream = stream;
            this._leaveOpen = leaveOpen;
            this._flavor = flavor;
            bool wantRfc1950Header = (flavor == ZlibStreamFlavor.ZLIB);
            if (compressionMode == CompressionMode.Decompress)
            {
                _z.InitializeInflate(wantRfc1950Header);
                this._wantCompress = false;
            }
            else
            {
                _z.InitializeDeflate(level, wantRfc1950Header);
                this._wantCompress = true;
            }

            // workitem 7159
            if (flavor == ZlibStreamFlavor.GZIP)
            {
                crc = new CRC32();
            }
        }


        public override void WriteByte(byte b)
        {
            _buf1[0] = (byte)b;
            // workitem 7159
            if (crc != null)
                crc.SlurpBlock(_buf1, 0, 1);
            Write(_buf1, 0, 1);
        }


        public override void Write(System.Byte[] buffer, int offset, int count)
        {
            // workitem 7159
            // calculate CRC before writing
            if (crc != null)
                crc.SlurpBlock(buffer, offset, count);

            if (_streamMode == StreamMode.Undefined) _streamMode = StreamMode.Writer;
            if (_streamMode != StreamMode.Writer)
                throw new ZlibException("Cannot Write after Reading.");

            if (count == 0)
                return;

            _z.InputBuffer = buffer;
            _z.NextIn = offset;
            _z.AvailableBytesIn = count;
            bool done = false;
            do
            {
                _z.OutputBuffer = _workingBuffer;
                _z.NextOut = 0;
                _z.AvailableBytesOut = _workingBuffer.Length;
                int rc = (_wantCompress)
                    ? _z.Deflate(_flushMode)
                    : _z.Inflate(_flushMode);
                if (rc != ZlibConstants.Z_OK && rc != ZlibConstants.Z_STREAM_END)
                    throw new ZlibException((_wantCompress ? "de" : "in") + "flating: " + _z.Message);
                _stream.Write(_workingBuffer, 0, _workingBuffer.Length - _z.AvailableBytesOut);

                done = _z.AvailableBytesIn == 0 && _z.AvailableBytesOut != 0;

                // If GZIP and de-compress, we're done when 8 bytes remain.
                if (_flavor == ZlibStreamFlavor.GZIP && !_wantCompress)
                    done = (_z.AvailableBytesIn == 8 && _z.AvailableBytesOut != 0);

            }
            while (!done);
        }



        private void finish()
        {
            if (_z == null) return;

            if (_streamMode == StreamMode.Writer)
            {
                bool done = false;
                do
                {
                    _z.OutputBuffer = _workingBuffer;
                    _z.NextOut = 0;
                    _z.AvailableBytesOut = _workingBuffer.Length;
                    int rc = (_wantCompress)
                        ? _z.Deflate(ZlibConstants.Z_FINISH)
                        : _z.Inflate(ZlibConstants.Z_FINISH);

                    if (rc != ZlibConstants.Z_STREAM_END && rc != ZlibConstants.Z_OK)
                        throw new ZlibException((_wantCompress ? "de" : "in") + "flating: " + _z.Message);

                    if (_workingBuffer.Length - _z.AvailableBytesOut > 0)
                    {
                        _stream.Write(_workingBuffer, 0, _workingBuffer.Length - _z.AvailableBytesOut);
                    }

                    done = _z.AvailableBytesIn == 0 && _z.AvailableBytesOut != 0;
                    // If GZIP and de-compress, we're done when 8 bytes remain.
                    if (_flavor == ZlibStreamFlavor.GZIP && !_wantCompress)
                        done = (_z.AvailableBytesIn == 8 && _z.AvailableBytesOut != 0);

                }
                while (!done);

                Flush();


                // workitem 7159
                if (_flavor == ZlibStreamFlavor.GZIP)
                {
                    //Console.WriteLine("GZipStream: Last write");

                    if (_wantCompress)
                    {
                        // Emit the GZIP trailer: CRC32 and  size mod 2^32
                        int c1 = crc.Crc32Result;
                        _stream.Write(BitConverter.GetBytes(c1), 0, 4);
                        int c2 = (Int32)(crc.TotalBytesRead & 0x00000000FFFFFFFF);
                        _stream.Write(BitConverter.GetBytes(c2), 0, 4);

                        //Console.WriteLine("GZipStream: Writing trailer  crc(0x{0:X8}) isize({1})", c1, c2);

                    }
                    else
                    {
                        //Console.WriteLine("ZlibBaseStream::finish / Writer / GZIP / decompression");
                        // should validate the trailer here
                        throw new ZlibException("Writing with decompression is not supported.");
                    }
                }
            }
            // workitem 7159
            else if (_streamMode == StreamMode.Reader)
            {
                if (_flavor == ZlibStreamFlavor.GZIP)
                {
                    if (!_wantCompress)
                    {
                        // Read and potentially verify the GZIP trailer: CRC32 and  size mod 2^32
                        byte[] trailer = new byte[8];

                        if (_z.AvailableBytesIn != 8)
                            throw new ZlibException(String.Format("Can't handle this! AvailableBytesIn={0}",
								 _z.AvailableBytesIn ));

                        Array.Copy(_z.InputBuffer, _z.NextIn, trailer, 0, trailer.Length);

                        Int32 crc32_expected = BitConverter.ToInt32(trailer, 0);
                        int crc32_actual = crc.Crc32Result;
                        Int32 isize_expected = BitConverter.ToInt32(trailer, 4);
                        Int32 isize_actual = (Int32)(_z.TotalBytesOut & 0x00000000FFFFFFFF);

                        // Console.WriteLine("GZipStream: slurped trailer  crc(0x{0:X8}) isize({1})", crc32_expected, isize_expected);
                        // Console.WriteLine("GZipStream: calc'd data      crc(0x{0:X8}) isize({1})", crc32_actual, isize_actual);

                        if (crc32_actual != crc32_expected)
                            throw new ZlibException(String.Format("Bad CRC32 in GZIP stream. (actual({0:X8})!=expected({1:X8}))", crc32_actual, crc32_expected));

                        if (isize_actual != isize_expected)
                            throw new ZlibException(String.Format("Bad size in GZIP stream. (actual({0})!=expected({1}))", isize_actual, isize_expected));

                    }
                    else
                    {
                        //Console.WriteLine("ZlibBaseStream::finish / Reader / GZIP / compression");
                        // should emit the GZIP trailer here
                        throw new ZlibException("Reading with compression is not supported.");

                    }
                }
            }
        }


        private void end()
        {
            if (_z == null)
                return;
            if (_wantCompress)
            {
                _z.EndDeflate();
            }
            else
            {
                _z.EndInflate();
            }
            _z = null;
        }


        public override void Close()
        {
            try
            {
                try
                {
                    finish();
                }
                catch (System.IO.IOException)
                {
                    // swallow exceptions?
                }
            }
            finally
            {
                end();
                if (!_leaveOpen) _stream.Close();
                _stream = null;
            }
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override System.Int64 Seek(System.Int64 offset, System.IO.SeekOrigin origin)
        {
            throw new NotImplementedException();
            //_outStream.Seek(offset, origin);
        }
        public override void SetLength(System.Int64 value)
        {
            _stream.SetLength(value);
        }


#if NOT
        public int Read()
        {
            if (Read(_buf1, 0, 1) == 0)
                return 0;
	    // calculate CRC after reading
	    if (crc!=null)
		crc.SlurpBlock(_buf1,0,1);
            return (_buf1[0] & 0xFF);
        }
#endif

        private bool nomoreinput = false;



        private string ReadZeroTerminatedString()
        {
            var list = new System.Collections.Generic.List<byte>();
            bool done = false;
            do
            {
                if (Read(_buf1, 0, 1) == 0)
                    throw new ZlibException("Unexpected EOF reading GZIP header.");
                else
                {
                    if (_buf1[0] == 0)
                        done = true;
                    else
                        list.Add(_buf1[0]);
                }
            } while (!done);
            byte[] a = list.ToArray();
            return GZipStream.iso8859dash1.GetString(a, 0, a.Length);
        }


        private void _ReadAndValidateGzipHeader()
        {
            // read the header on the first read
            byte[] header = new byte[10];
            int n = _stream.Read(header, 0, header.Length);

            if (n != 10)
                throw new ZlibException("Not a valid GZIP stream.");

            if (header[0] != 0x1F || header[1] != 0x8B || header[2] != 8)
                throw new ZlibException("Bad GZIP header.");

            Int32 timet = BitConverter.ToInt32(header, 4);
            _GzipMtime = GZipStream._unixEpoch.AddSeconds(timet);

            if ((header[3] & 0x04) == 0x04)
            {
                // read and discard extra field
                n = _stream.Read(header, 0, 2); // 2-byte length field
                Int16 extraLength = (Int16)(header[0] + header[1] * 256);
                byte[] extra = new byte[extraLength];
                n = _stream.Read(extra, 0, extra.Length);
                if (n != extraLength)
                    throw new ZlibException("Unexpected end-of-file reading GZIP header.");
            }
            if ((header[3] & 0x08) == 0x08)
                _GzipFileName = ReadZeroTerminatedString();
            if ((header[3] & 0x10) == 0x010)
                _GzipComment = ReadZeroTerminatedString();
            if ((header[3] & 0x02) == 0x02)
                Read(_buf1, 0, 1); // CRC16, ignore
        }



        public override System.Int32 Read(System.Byte[] buffer, System.Int32 offset, System.Int32 count)
        {
            if (_streamMode == StreamMode.Undefined)
            {
                // for the first read, set up some controls.
                _streamMode = StreamMode.Reader;
                _z.AvailableBytesIn = 0;
                if (_flavor == ZlibStreamFlavor.GZIP)
                    _ReadAndValidateGzipHeader();
            }

            if (_streamMode != StreamMode.Reader)
                throw new ZlibException("Cannot Read after Writing.");

            if (!this._stream.CanRead) throw new ZlibException("The stream is not readable.");
            if (count == 0)
                return 0;

            int rc;

            // set up the output of the deflate/inflate codec:
            _z.OutputBuffer = buffer;
            _z.NextOut = offset;
            _z.AvailableBytesOut = count;

            // this is necessary in case _workingBuffer has been resized. (new byte[])
            _z.InputBuffer = _workingBuffer;

            do
            {
                // need data in _workingBuffer in order to deflate/inflate.  Here, we check if we have any.
                if ((_z.AvailableBytesIn == 0) && (!nomoreinput))
                {
                    // No data available, so try to Read data from the captive stream.
                    _z.NextIn = 0;
                    _z.AvailableBytesIn = SharedUtils.ReadInput(_stream, _workingBuffer, 0, _workingBuffer.Length);
                    //(bufsize<z.avail_out ? bufsize : z.avail_out));
                    if (_z.AvailableBytesIn == -1)
                    {
                        _z.AvailableBytesIn = 0;
                        nomoreinput = true;
                    }
                }
                // we have data in InputBuffer; now compress or decompress as appropriate
                rc = (_wantCompress)
                    ? _z.Deflate(_flushMode)
                    : _z.Inflate(_flushMode);

                if (nomoreinput && (rc == ZlibConstants.Z_BUF_ERROR))
                    throw new ZlibException((_wantCompress ? "de" : "in") + "flating: unexpected end of stream");
                //return (-1);  // should I throw here?

                if (rc != ZlibConstants.Z_OK && rc != ZlibConstants.Z_STREAM_END)
                    throw new ZlibException(String.Format("{0}flating:  rc={1}  msg={2}", (_wantCompress ? "de" : "in"), rc, _z.Message));

                if ((nomoreinput || rc == ZlibConstants.Z_STREAM_END) && (_z.AvailableBytesOut == count))
                    break; // nothing more to read
            }
            while (_z.AvailableBytesOut == count && rc == ZlibConstants.Z_OK);

            rc = (count - _z.AvailableBytesOut);

            // calculate CRC after reading
            if (crc != null)
            {
                crc.SlurpBlock(buffer, offset, rc);
            }

            return rc;
        }



        public override System.Boolean CanRead
        {
            get { return this._stream.CanRead; }
        }

        public override System.Boolean CanSeek
        {
            get { return this._stream.CanSeek; }
        }

        public override System.Boolean CanWrite
        {
            get { return this._stream.CanWrite; }
        }

        public override System.Int64 Length
        {
            get { return _stream.Length; }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        internal enum StreamMode
        {
            Writer,
            Reader,
            Undefined,
        }
    }


}