// DeflateStream.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa and Microsoft Corporation.  
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
// Time-stamp: <2009-May-31 09:29:26>
//
// ------------------------------------------------------------------
//
// This module defines the DeflateStream class, which can be used as a replacement for
// the System.IO.Compression.DeflateStream class in the .NET BCL.
//
// ------------------------------------------------------------------


using System;

namespace Ionic.Zlib
{
    /// <summary>
    /// A class for compressing and decompressing streams using the Deflate algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Data can be compressed or decompressed, and either of those can be through
    /// reading or writing.  For more information on the Deflate algorithm, see <see
    /// href="http://www.ietf.org/rfc/rfc1951.txt">IETF RFC 1951, "DEFLATE Compressed
    /// Data Format Specification version 1.3."</see>
    /// </para>
    /// <para>
    /// This class is similar to <see cref="ZlibStream"/>, except that <c>ZlibStream</c>
    /// adds the <see href="http://www.ietf.org/rfc/rfc1950.txt">RFC 1950 - ZLIB</see>
    /// header bytes to a compressed stream when compressing, or expects the RFC1950
    /// header bytes when decompressing. The <c>DeflateStream</c> does not.
    /// </para>
    /// </remarks>
    /// <seealso cref="DeflateStream" />
    /// <seealso cref="GZipStream" />
    public class DeflateStream : System.IO.Stream
    {
        internal ZlibBaseStream _baseStream;
        bool _disposed;

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
        ///     Using compressor As Stream = New DeflateStream(raw, CompressionMode.Compress)
        ///         Dim buffer As Byte() = New Byte(4096) {}
        ///         Dim n As Integer = -1
        ///         Do While (n &lt;&gt; 0)
        ///             If (n &gt; 0) Then
        ///                 compressor.Write(buffer, 0, n)
        ///             End If
        ///             n = input.Read(buffer, 0, buffer.Length)
        ///         Loop
        ///     End Using
        ///     End Using
        /// End Using
        /// </code>
        /// </example>
        /// <param name="stream">The stream which will be read or written.</param>
        /// <param name="mode">Indicates whether the DeflateStream will compress or decompress.</param>
        public DeflateStream(System.IO.Stream stream, CompressionMode mode)
            : this(stream, mode, CompressionLevel.Default, false)
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
        ///                                                      CompressionLevel.BestCompression))
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
        ///     Using compressor As Stream = New DeflateStream(raw, CompressionMode.Compress, CompressionLevel.BestCompression)
        ///         Dim buffer As Byte() = New Byte(4096) {}
        ///         Dim n As Integer = -1
        ///         Do While (n &lt;&gt; 0)
        ///             If (n &gt; 0) Then
        ///                 compressor.Write(buffer, 0, n)
        ///             End If
        ///             n = input.Read(buffer, 0, buffer.Length)
        ///         Loop
        ///     End Using
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
            : this(stream, mode, CompressionLevel.Default, leaveOpen)
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
        ///         using (Stream compressor = new DeflateStream(raw, CompressionMode.Compress, CompressionLevel.BestCompression, true))
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
        ///     Using compressor As Stream = New DeflateStream(raw, CompressionMode.Compress, CompressionLevel.BestCompression, True)
        ///         Dim buffer As Byte() = New Byte(4096) {}
        ///         Dim n As Integer = -1
        ///         Do While (n &lt;&gt; 0)
        ///             If (n &gt; 0) Then
        ///                 compressor.Write(buffer, 0, n)
        ///             End If
        ///             n = input.Read(buffer, 0, buffer.Length)
        ///         Loop
        ///     End Using
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
        /// </summary>
        /// <remarks> See the ZLIB documentation for the meaning of the flush behavior.
        /// </remarks>
        virtual public FlushType FlushMode
        {
            get { return (this._baseStream._flushMode); }
            set
            {
                if (_disposed) throw new ObjectDisposedException("DeflateStream");
                this._baseStream._flushMode = value;
            }
        }

        /// <summary>
        /// The size of the working buffer for the compression codec. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The working buffer is used for all stream operations.  The default size is 1024 bytes.
        /// The minimum size is 128 bytes. You may get better performance with a larger buffer.
        /// Then again, you might not.  You would have to test it.
        /// </para>
        ///
        /// <para>
        /// Set this before the first call to Read()  or Write() on the stream. If you try to set it 
        /// afterwards, it will throw.
        /// </para>
        /// </remarks>
        public int BufferSize
        {
            get
            {
                return this._baseStream._bufferSize;
            }
            set
            {
            if (_disposed) throw new ObjectDisposedException("DeflateStream");
                if (this._baseStream._workingBuffer != null)
                    throw new ZlibException("The working buffer is already set.");
                if (value < ZlibConstants.WorkingBufferSizeMin)
                    throw new ZlibException(String.Format("Don't be silly. {0} bytes?? Use a bigger buffer.", value));
                this._baseStream._bufferSize = value;
            }
        }

        /// <summary>The ZLIB strategy to be used during compression.</summary>
        /// <remarks>By tweaking this parameter, you may be able to optimize the compression for 
        /// data with particular characteristics.</remarks>
        public CompressionStrategy Strategy
        {
            get
            {
                return this._baseStream.Strategy;
            }
            set
            {
            if (_disposed) throw new ObjectDisposedException("DeflateStream");
                this._baseStream.Strategy = value;
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
        /// Dispose the stream.  
        /// </summary>
        /// <remarks>
        /// This may or may not result in a Close() call on the captive stream. 
        /// See the constructors that have a leaveOpen parameter for more information.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!_disposed)
                {
                    if (disposing && (this._baseStream != null))
                        this._baseStream.Close();
                    _disposed = true;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
    
        }

        

        /// <summary>
        /// Indicates whether the stream can be read.
        /// </summary>
        /// <remarks>
        /// The return value depends on whether the captive stream supports reading.
        /// </remarks>
        public override bool CanRead
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("DeflateStream");
                return _baseStream._stream.CanRead;
            }
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
            get
            {
                if (_disposed) throw new ObjectDisposedException("DeflateStream");
                return _baseStream._stream.CanWrite;
            }
        }

        /// <summary>
        /// Flush the stream.
        /// </summary>
        public override void Flush()
        {
            if (_disposed) throw new ObjectDisposedException("DeflateStream");
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
        /// The position of the stream pointer. 
        /// </summary>
        /// <remarks>
        /// Writing this property always throws a NotImplementedException. Reading will
        /// return the total bytes written out, if used in writing, or the total bytes 
        /// read in, if used in reading.   The count may refer to compressed bytes or 
        /// uncompressed bytes, depending on how you've used the stream.
        /// </remarks>
        public override long Position
        {
            get
            {
                if (this._baseStream._streamMode == Ionic.Zlib.ZlibBaseStream.StreamMode.Writer)
                    return this._baseStream._z.TotalBytesOut;
                if (this._baseStream._streamMode == Ionic.Zlib.ZlibBaseStream.StreamMode.Reader)
                    return this._baseStream._z.TotalBytesIn;
                return 0;
            }
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
            if (_disposed) throw new ObjectDisposedException("DeflateStream");
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
        /// If you wish to use the DeflateStream to compress data while writing, you can
        /// create a DeflateStream with CompressionMode.Compress, and a writable output
        /// stream.  Then call Write() on that DeflateStream, providing uncompressed
        /// data as input.  The data sent to the output stream will be the compressed
        /// form of the data written.  If you wish to use the DeflateStream to
        /// decompress data while writing, you can create a DeflateStream with
        /// CompressionMode.Decompress, and a writable output stream.  Then call Write()
        /// on that stream, providing previously compressed data. The data sent to the
        /// output stream will be the decompressed form of the data written.
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
            if (_disposed) throw new ObjectDisposedException("DeflateStream");
            _baseStream.Write(buffer, offset, count);
        }
        #endregion
    }

}

