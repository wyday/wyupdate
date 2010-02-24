// ZipSegmentedStream.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009-2010 Dino Chiesa.
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
// Time-stamp: <2010-February-14 18:44:15>
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
        private string _baseDir;
        private string _currentName;
        private string _currentTempName;
        private uint _currentDiskNumber;
        private uint _maxDiskNumber;
        private int _maxSegmentSize;
        private System.IO.Stream _innerStream;

        private ZipSegmentedStream() : base() { }

        public static ZipSegmentedStream ForReading(string name, uint initialDiskNumber, uint maxDiskNumber)
        {
            ZipSegmentedStream zss = new ZipSegmentedStream()
                {
                    rw = 1,  // 1 == readonly
                    CurrentSegment = initialDiskNumber,
                    _maxDiskNumber = maxDiskNumber,
                    _baseName = name,
                };

            zss._SetReadStream();
            return zss;
        }


        public static ZipSegmentedStream ForWriting(string name, int maxSegmentSize)
        {
            ZipSegmentedStream zss = new ZipSegmentedStream()
                {
                    rw = 2, // 2 == write
                    CurrentSegment = 0,
                    _baseName = name,
                    _maxSegmentSize = maxSegmentSize,
                    _baseDir = Path.GetDirectoryName(name)
                };

            // workitem 9522
            if (zss._baseDir=="") zss._baseDir=".";

            zss._SetWriteStream(0);
            return zss;
        }


        public static ZipSegmentedStream ForUpdate(string name, uint diskNumber)
        {
            // ForUpdate is used only when updating the zip entry metadata for
            // a segmented zip file, when the starting segment is earlier
            // than the ending segment, for a particular entry.

            ZipSegmentedStream zss = new ZipSegmentedStream()
                {
                    rw = 3,  // 3 == update
                    CurrentSegment = diskNumber,
                    _baseName = name,
                    _maxSegmentSize = Int32.MaxValue  // insure no rollover
                };

            // It's safe to assume that the update will not expand the size of the segment.
            // It's an in-place update of zip metadata.

            // Console.WriteLine("ZipSegmentedStream: update ({0})", name);
            zss._SetUpdateStream();
            return zss;
        }

        private void _SetUpdateStream()
        {
            _innerStream = new FileStream(CurrentName, FileMode.Open);
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
            private set
            {
                _currentDiskNumber = value;
                _currentName = null; // it will get updated next time referenced
            }
        }

        public String CurrentName
        {
            get
            {
                if (_currentName==null)
                    _currentName = _NameForSegment(CurrentSegment);

                return _currentName;
            }
        }

        private string _NameForSegment(uint diskNumber)
        {
            return String.Format("{0}.z{1:D2}",
                                 Path.Combine(Path.GetDirectoryName(_baseName),
                                              Path.GetFileNameWithoutExtension(_baseName)),
                                 diskNumber + 1);
        }


        // Returns the segment that WILL be current if writing
        // a block of the given length.
        // This isn't exactly true. It could roll over beyond
        // this number.
        public UInt32 ComputeSegment(int length)
        {
            if (_innerStream.Position + length > _maxSegmentSize)
                // the block will go AT LEAST into the next segment
                return CurrentSegment + 1;

            // it will fit in the current segment
            return CurrentSegment;
        }


        public override String ToString()
        {
            return String.Format("{0}[{1}][{2}], pos=0x{3:X})",
                                 "ZipSegmentedStream", CurrentName,
                                 (rw == 1) ? "Read" : (rw == 2) ? "Write" : (rw == 3) ? "Update" : "???",
                                 this.Position);
        }


        public void ResetWriter()
        {
            CurrentSegment = 0;
            _SetWriteStream(0);
        }


        private void _SetReadStream()
        {
            if (_innerStream != null)
                _innerStream.Close();

            if (CurrentSegment + 1 == _maxDiskNumber)
            {
                _currentName = _baseName;
            }
//             else
//             {
//                 _currentName = String.Format("{0}.z{1:D2}",
//                                          Path.Combine(Path.GetDirectoryName(_baseName),
//                                                       Path.GetFileNameWithoutExtension(_baseName)),
//                                          CurrentSegment + 1);
//             }

            //Console.WriteLine("ZipSegmentedStream::SetReadStream  ({0})", CcurrentName);
            _innerStream = File.OpenRead(CurrentName);
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
            //CurrentName, offset, count, r);
            while (r1 != count)
            {
                if (_innerStream.Position != _innerStream.Length)
                    throw new ZipException(String.Format("Read error in file {0}", CurrentName));

                //Console.WriteLine("ZipSegmentedStream::Read[{0}] pos(0x{1:X}) len(0x{2:X}) r({3}) count({4})",
                //CurrentName, _innerStream.Position,  _innerStream.Length, r, count);

                if (CurrentSegment + 1 == _maxDiskNumber)
                    return r; // no more to read

                CurrentSegment++;
                _SetReadStream();
                offset += r1;
                count -= r1;
                r1 = _innerStream.Read(buffer, offset, count);
                //Console.WriteLine("ZipSegmentedStream::ReadMore[{0}] ({1},{2}) = {3}",
                //CurrentName, offset, count, r1);
                r += r1;
            }
            return r;
        }



        private void _SetWriteStream(uint increment)
        {
            if (_innerStream != null)
            {
                _innerStream.Close();
                if (File.Exists(CurrentName))
                    File.Delete(CurrentName);
                File.Move(_currentTempName, CurrentName);
            }

            if (increment > 0)
                CurrentSegment += increment;

            SharedUtilities.CreateAndOpenUniqueTempFile(_baseDir,
                                                        out _innerStream,
                                                        out _currentTempName);

            //Console.WriteLine("ZipSegmentedStream::SetWriteStream  ({0})", CurrentName);

            if (CurrentSegment == 0)
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
                        //Console.WriteLine("Inc for Contiguous ({0}) p(0x{1:X}) c(0x{2:X}) sz(0x{3:X})",
                        //                  CurrentName, _innerStream.Position, count, _maxSegmentSize);
                        _SetWriteStream(1);
                    }
                }
                else
                {
                    while (_innerStream.Position + count > _maxSegmentSize)
                    {
                        int c = unchecked(_maxSegmentSize - (int)_innerStream.Position);
                        //Console.WriteLine("ZipSegmentedStream::Write[{0}] pos(0x{1:X}) off(0x{2:X}) c(0x{3:X})",
                        //        CurrentName, _innerStream.Position,  offset, c);
                        _innerStream.Write(buffer, offset, c);
                        //Console.WriteLine("All Full ({0}) p(0x{1:X}) c(0x{2:X}) sz(0x{3:X})",
                        //                  CurrentName, _innerStream.Position, count, _maxSegmentSize);

                        _SetWriteStream(1);
                        count -= c;
                        offset += c;
                    }
                }
                //Console.WriteLine("ZipSegmentedStream::Write[{0}] pos(0x{1:X}) off(0x{2:X}) c(0x{3:X})",
                //                  CurrentName, _innerStream.Position, offset, count);

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


        public long TruncateBackward(uint diskNumber, long offset)
        {
            // Console.WriteLine("***ZSS.Trunc to disk {0}", diskNumber);
            // Console.WriteLine("***ZSS.Trunc:  current disk {0}", CurrentSegment);

            if (rw!=2)
                throw new ZipException("bad state.");

            // Seek back in the segmented stream to a (maybe) prior segment.

            // Check if it is the same segment.  If it is, very simple.
            if (diskNumber == CurrentSegment)
            {
                var x =_innerStream.Seek(offset, SeekOrigin.Begin);
                // workitem 10178
                Ionic.Zip.SharedUtilities.Workaround_Ladybug318918(_innerStream);
                return x;
            }

            // Seeking back to a prior segment.
            // The current segment and any intervening segments must be removed.
            // First, remove the current segment.
            if (_innerStream != null)
            {
                _innerStream.Close();
                if (File.Exists(_currentTempName))
                    File.Delete(_currentTempName);
            }

            // Now, remove intervening segments.
            for (uint j= CurrentSegment-1; j > diskNumber; j--)
            {
                string s = _NameForSegment(j);
                // Console.WriteLine("***ZSS.Trunc:  removing file {0}", s);
                if (File.Exists(s))
                    File.Delete(s);
            }

            // now, open the desired segment.  It must exist.
            CurrentSegment = diskNumber;

            // get a new temp file, try 3 times:
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    _currentTempName = SharedUtilities.InternalGetTempFileName();
                    File.Move(CurrentName, _currentTempName);  // move the .z0x file back to a temp name
                }
                catch(IOException)
                {
                    if (i == 2) throw;
                }
            }

            _innerStream = new FileStream(_currentTempName, FileMode.Open);

            var r =  _innerStream.Seek(offset, SeekOrigin.Begin);

            // workitem 10178
            Ionic.Zip.SharedUtilities.Workaround_Ladybug318918(_innerStream);

            return r;
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
            var x = _innerStream.Seek(offset, origin);
            // workitem 10178
            Ionic.Zip.SharedUtilities.Workaround_Ladybug318918(_innerStream);
            return x;
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
                    if (File.Exists(CurrentName))
                        File.Delete(CurrentName);
                    if (File.Exists(_currentTempName))
                    {
                        File.Move(_currentTempName, CurrentName);
                    }
                }
            }

            base.Close();
        }

    }

}