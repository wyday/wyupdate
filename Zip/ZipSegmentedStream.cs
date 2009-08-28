// ZipSegmentedStream.cs
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
// Time-stamp: <2009-August-28 16:26:14>
//
// ------------------------------------------------------------------
//
// This module defines logic for streams that span disk files. 
//
// 
// ------------------------------------------------------------------


using System;
using System.IO;

namespace Ionic.Zip
{
    internal class ZipSegmentedStream : System.IO.Stream, System.IDisposable
    {
        private int rw;
        private string _baseName;
        private string _currentName;
        private string _currentTempName;
        private uint _currentDiskNumber;
        private uint _maxDiskNumber;
        private int _maxSegmentSize;
        private System.IO.Stream _innerStream;

        private ZipSegmentedStream() : base() { }

        public static ZipSegmentedStream ForReading(string name, uint initialDiskNumber, uint maxDiskNumber)
        {
            ZipSegmentedStream zss = new ZipSegmentedStream();
            zss._currentDiskNumber = initialDiskNumber;
            zss._maxDiskNumber = maxDiskNumber;
            zss._baseName = name;
            //Console.WriteLine("ZipSegmentedStream: reading ({0})", name);
            zss._SetReadStream();
            zss.rw = 1;
            return zss;
        }


        public static ZipSegmentedStream ForWriting(string name, int maxSegmentSize)
        {
            ZipSegmentedStream zss = new ZipSegmentedStream();
            zss.rw = 2;
            zss._currentDiskNumber = 0;
            zss._baseName = name;
            zss._maxSegmentSize = maxSegmentSize;
            // Console.WriteLine("ZipSegmentedStream: writing ({0})", name);
            zss._SetWriteStream();
            return zss;
        }

        public static ZipSegmentedStream ForUpdate(string name, uint diskNumber)
        {
            ZipSegmentedStream zss = new ZipSegmentedStream();
            zss.rw = 3;
            zss._currentDiskNumber = diskNumber;
            zss._baseName = name;
            zss._maxSegmentSize = Int32.MaxValue;
            // Console.WriteLine("ZipSegmentedStream: update ({0})", name);
            zss._SetUpdateStream();
            return zss;
        }

        private void _SetUpdateStream()
        {
            _currentName = String.Format("{0}.z{1:D2}",
                                         Path.Combine(Path.GetDirectoryName(_baseName),
                                                      Path.GetFileNameWithoutExtension(_baseName)),
                                         _currentDiskNumber + 1);

            // Console.WriteLine("ZipSegmentedStream::SetUpdateStream  ({0})", _currentName);
            _innerStream = new FileStream(_currentName, FileMode.Open);
        }



        public bool ContiguousWrite
        {
            get;
            set;
        }

        public UInt32 CurrentSegment
        {
            get
            {
                return _currentDiskNumber;
            }
        }

        public String CurrentName
        {
            get
            {
                return _currentName;
            }
        }


        // returns the segment that WILL be current if writing
        // a block of the given length
        public UInt32 ComputeSegment(int length)
        {
            if (_innerStream.Position + length > _maxSegmentSize)
                return _currentDiskNumber + 1; // the block will go in the next segment

            // it will fit in the current segment
            return _currentDiskNumber;
        }


        public override String ToString()
        {
            return String.Format("{0}[{1}][{2}], pos=0x{3:X})",
                                 "ZipSegmentedStream", _currentName,
                                 (rw == 1) ? "Read" : (rw == 2) ? "Write" : (rw == 3) ? "Update" : "???",
                                 this.Position);
        }


        public void ResetWriter()
        {
            _currentDiskNumber = 0;
            _SetWriteStream();
        }


        private void _SetReadStream()
        {
            if (_innerStream != null)
                _innerStream.Close();

            if (_currentDiskNumber + 1 == _maxDiskNumber)
            {
                _currentName = _baseName;
            }
            else
            {
                _currentName = String.Format("{0}.z{1:D2}",
                                         Path.Combine(Path.GetDirectoryName(_baseName),
                                                      Path.GetFileNameWithoutExtension(_baseName)),
                                         _currentDiskNumber + 1);
            }
            //Console.WriteLine("ZipSegmentedStream::SetReadStream  ({0})", _currentName);
            _innerStream = File.OpenRead(_currentName);
        }



        /// <summary>
        /// Read from the stream
        /// </summary>
        /// <param name="buffer">the buffer to read</param>
        /// <param name="offset">the offset at which to start</param>
        /// <param name="count">the number of bytes to read</param>
        /// <returns>the number of bytes actually read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (rw != 1) throw new ZipException("Stream Error: Cannot Read.");

            int r = _innerStream.Read(buffer, offset, count);
            int r1 = r;
            //Console.WriteLine("ZipSegmentedStream::Read[{0}] ({1},{2}) = {3}",
            //_currentName, offset, count, r);
            while (r1 != count)
            {
                if (_innerStream.Position != _innerStream.Length)
                    throw new ZipException(String.Format("Read error in file {0}", _currentName));

                //Console.WriteLine("ZipSegmentedStream::Read[{0}] pos(0x{1:X}) len(0x{2:X}) r({3}) count({4})",
                //_currentName, _innerStream.Position,  _innerStream.Length, r, count);

                if (_currentDiskNumber + 1 == _maxDiskNumber)
                    return r; // no more to read

                _currentDiskNumber++;
                _SetReadStream();
                offset += r1;
                count -= r1;
                r1 = _innerStream.Read(buffer, offset, count);
                //Console.WriteLine("ZipSegmentedStream::ReadMore[{0}] ({1},{2}) = {3}",
                //_currentName, offset, count, r1);
                r += r1;
            }
            return r;
        }



        private void _SetWriteStream()
        {
            if (_innerStream != null)
            {
                _innerStream.Close();
                if (File.Exists(_currentName))
                    File.Delete(_currentName);
                File.Move(_currentTempName, _currentName);
            }

            _currentName = String.Format("{0}.z{1:D2}",
                                         Path.Combine(Path.GetDirectoryName(_baseName),
                                                      Path.GetFileNameWithoutExtension(_baseName)),
                                         _currentDiskNumber + 1);

            _currentTempName = SharedUtilities.GetTempFilename();
            //Console.WriteLine("ZipSegmentedStream::SetWriteStream  ({0})", _currentName);
            _innerStream = new FileStream(_currentTempName, FileMode.CreateNew);
            if (_currentDiskNumber == 0)
                _innerStream.Write(BitConverter.GetBytes(ZipConstants.SplitArchiveSignature), 0, 4);
        }


        /// <summary>
        /// Write to the stream. 
        /// </summary>
        /// <param name="buffer">the buffer from which to write</param>
        /// <param name="offset">the offset at which to start writing</param>
        /// <param name="count">the number of bytes to write</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (rw == 2)
            {
                if (ContiguousWrite)
                {
                    // enough space for a contiguous write?
                    if (_innerStream.Position + count > _maxSegmentSize)
                    {
                        _currentDiskNumber++;
                        //Console.WriteLine("Inc for Contiguous ({0}) p(0x{1:X}) c(0x{2:X}) sz(0x{3:X})",
                        //                  _currentName, _innerStream.Position, count, _maxSegmentSize);
                        _SetWriteStream();
                    }
                }
                else
                {
                    while (_innerStream.Position + count > _maxSegmentSize)
                    {
                        int c = unchecked(_maxSegmentSize - (int)_innerStream.Position);
                        //Console.WriteLine("ZipSegmentedStream::Write[{0}] pos(0x{1:X}) off(0x{2:X}) c(0x{3:X})",
                        //        _currentName, _innerStream.Position,  offset, c);
                        _innerStream.Write(buffer, offset, c);
                        //Console.WriteLine("All Full ({0}) p(0x{1:X}) c(0x{2:X}) sz(0x{3:X})",
                        //                  _currentName, _innerStream.Position, count, _maxSegmentSize);

                        _currentDiskNumber++;
                        _SetWriteStream();
                        count -= c;
                        offset += c;
                    }
                }
                //Console.WriteLine("ZipSegmentedStream::Write[{0}] pos(0x{1:X}) off(0x{2:X}) c(0x{3:X})",
                //                  _currentName, _innerStream.Position, offset, count);

                _innerStream.Write(buffer, offset, count);
                
            }
            else if (rw == 3)
            {
                // updating a segment.  There is no possibility for rollover
                _innerStream.Write(buffer, offset, count);
            }
            else
                throw new ZipException("Stream Error: Cannot Write.");
            
        }




        public override bool CanRead
        {
            get { return (rw == 1 && _innerStream.CanRead); }
        }


        public override bool CanSeek
        {
            get { return _innerStream.CanSeek; }
        }


        public override bool CanWrite
        {
            get { return (rw == 2) && _innerStream.CanWrite; }
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override long Length
        {
            get
            {
                return _innerStream.Length;
            }
        }

        public override long Position
        {
            get { return _innerStream.Position; }
            set { _innerStream.Position = value; }
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            if (rw != 2)
                throw new NotImplementedException();
            _innerStream.SetLength(value);
        }

        void IDisposable.Dispose()
        {
            Close();
        }

        public override void Close()
        {
            if (_innerStream != null)
            {
                _innerStream.Close();
                _innerStream = null;
                if (rw == 2)
                {
                    if (File.Exists(_currentName))
                        File.Delete(_currentName);
                    if (File.Exists(_currentTempName))
                    {
                        File.Move(_currentTempName, _currentName);
                    }
                }
            }

            base.Close();
        }

    }

}