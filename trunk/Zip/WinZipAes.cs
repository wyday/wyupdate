// WinZipAes.cs
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
// Time-stamp: <2009-July-28 23:27:14>
//
// ------------------------------------------------------------------
//
// This module defines the classes for dealing with WinZip's AES encryption,
// according to the specifications for the format available on WinZip's website.
//
// Created: January 2009
// 
// ------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;

#if AESCRYPTO
namespace Ionic.Zip
{
    /// <summary> 
    /// This is a helper class supporting WinZip AES encryption.  
    /// This class is intended for use only by the DotNetZip library.
    /// </summary>
    /// <remarks>
    /// Most uses of the DotNetZip library will not involve direct calls into the
    /// WinZipAesCrypto class.  Instead, the WinZipAesCrypto class is instantiated and
    /// used by the ZipEntry() class when WinZip AES encryption or decryption on an
    /// entry is employed.
    /// </remarks>
    internal class WinZipAesCrypto
    {
        internal byte[] _Salt;
        internal byte[] _providedPv;
        internal byte[] _generatedPv;
        internal int _KeyStrengthInBits;
        private byte[] _MacInitializationVector;
        private byte[] _StoredMac;
        private byte[] _keyBytes;
        private Int16 PasswordVerificationStored;
        private Int16 PasswordVerificationGenerated;
        private int Rfc2898KeygenIterations = 1000;
        private string _Password;   
        private bool _cryptoGenerated ;

        private WinZipAesCrypto(string password, int KeyStrengthInBits)
        {
            _Password = password;
            _KeyStrengthInBits = KeyStrengthInBits;
        }

        private WinZipAesCrypto()
        {
        }


        public static WinZipAesCrypto Generate(string password, int KeyStrengthInBits)
        {
            WinZipAesCrypto c = new WinZipAesCrypto(password, KeyStrengthInBits);

            int saltSizeInBytes = c._KeyStrengthInBytes / 2;
            c._Salt = new byte[saltSizeInBytes];
            Random rnd = new Random();
            rnd.NextBytes(c._Salt);
            return c;
        }



        public static WinZipAesCrypto ReadFromStream(string password, int KeyStrengthInBits, Stream s)
        {
            // from http://www.winzip.com/aes_info.htm
            //
            // Size(bytes)   Content 
            // -----------------------------------
            // Variable      Salt value 
            // 2             Password verification value 
            // Variable      Encrypted file data 
            // 10            Authentication code 
            //
            // ZipEntry.CompressedSize represents the size of all of those elements.

            // salt size varies with key length:  
            //    128 bit key => 8 bytes salt
            //    192 bits => 12 bytes salt
            //    256 bits => 16 bytes salt

            WinZipAesCrypto c = new WinZipAesCrypto(password, KeyStrengthInBits);

            int saltSizeInBytes = c._KeyStrengthInBytes / 2;
            c._Salt = new byte[saltSizeInBytes];
            c._providedPv = new byte[2];

            s.Read(c._Salt, 0, c._Salt.Length);
            s.Read(c._providedPv, 0, c._providedPv.Length);

            c.PasswordVerificationStored = (Int16)(c._providedPv[0] + c._providedPv[1] * 256);
            if (password != null)
            {
                c.PasswordVerificationGenerated = (Int16)(c.GeneratedPV[0] + c.GeneratedPV[1] * 256);
                if (c.PasswordVerificationGenerated != c.PasswordVerificationStored)
                    throw new BadPasswordException("bad password");
            }

            return c;
        }

        public byte[] GeneratedPV
        {
            get
            {
                if (!_cryptoGenerated) _GenerateCryptoBytes();
                return _generatedPv;
            }
        }


        public byte[] Salt
        {
            get
            {
                return _Salt;
            }
        }


        private int _KeyStrengthInBytes
        {
            get
            {
                return _KeyStrengthInBits / 8;

            }
        }

        public int SizeOfEncryptionMetadata
        {
            get
            {
                // 10 bytes after, (n-10) before the compressed data
                return _KeyStrengthInBytes / 2 + 10 + 2;
            }
        }

        public string Password
        {
            set
            {
                _Password = value;
                if (_Password != null)
                {
                    PasswordVerificationGenerated = (Int16)(GeneratedPV[0] + GeneratedPV[1] * 256);
                    if (PasswordVerificationGenerated != PasswordVerificationStored)
                        throw new Ionic.Zip.BadPasswordException();
                }
            }
        }


        private void _GenerateCryptoBytes()
        {
            //Console.WriteLine(" provided password: '{0}'", _Password);

            System.Security.Cryptography.Rfc2898DeriveBytes rfc2898 =
                new System.Security.Cryptography.Rfc2898DeriveBytes(_Password, Salt, Rfc2898KeygenIterations);

            _keyBytes = rfc2898.GetBytes(_KeyStrengthInBytes); // 16 or 24 or 32 ???
            _MacInitializationVector = rfc2898.GetBytes(_KeyStrengthInBytes);
            _generatedPv = rfc2898.GetBytes(2);

            _cryptoGenerated = true;
        }


        public byte[] KeyBytes
        {
            get
            {
                if (!_cryptoGenerated) _GenerateCryptoBytes();
                return _keyBytes;
            }
        }


        public byte[] MacIv
        {
            get
            {
                if (!_cryptoGenerated) _GenerateCryptoBytes();
                return _MacInitializationVector;
            }
        }

        //public byte[] StoredMac
        //{
        //    get
        //    {
        //        return _StoredMac;
        //    }
        //}

        public byte[] CalculatedMac;


        public void ReadAndVerifyMac(System.IO.Stream s)
        {
            bool invalid = false;

            // read integrityCheckVector.
            // caller must ensure that the file pointer is in the right spot! 
            _StoredMac = new byte[10];  // aka "authentication code"
            s.Read(_StoredMac, 0, _StoredMac.Length);

            if (_StoredMac.Length != CalculatedMac.Length)
                invalid = true;

            if (!invalid)
            {
                for (int i = 0; i < _StoredMac.Length; i++)
                {
                    if (_StoredMac[i] != CalculatedMac[i])
                        invalid = true;
                }
            }

            if (invalid)
                throw new Ionic.Zip.BadStateException("The MAC does not match.");
        }

    }


    #region DONT_COMPILE_BUT_KEEP_FOR_POTENTIAL_FUTURE_USE
#if NO
    internal class Util
    {
        internal static string FormatByteArray(byte[] b)
        {
            int actualLimit = 96;
            System.Text.StringBuilder sb1 = new System.Text.StringBuilder(actualLimit * 2);
            int i = 0;

            if (actualLimit * 2 > b.Length)
            {
                for (i = 0; i < b.Length; i++)
                {
                    if (i != 0 && i % 16 == 0)
                        sb1.Append("\n");
                    sb1.Append(System.String.Format("{0:X2} ", b[i]));
                }
            }
            else
            {
                // first N bytes of the buffer
                for (i = 0; i < actualLimit; i++)
                {
                    if (i != 0 && i % 16 == 0)
                        sb1.Append("\n");
                    sb1.Append(System.String.Format("{0:X2} ", b[i]));
                }

                if (b.Length > actualLimit * 2)
                    sb1.Append(String.Format("\n   ...({0} other bytes here)....\n", b.Length - actualLimit * 2));

                // last N bytes of the buffer
                for (i = 0; i < actualLimit; i++)
                {
                    if (i != 0 && i % 16 == 0)
                        sb1.Append("\n");
                    sb1.Append(System.String.Format("{0:X2} ", b[b.Length - actualLimit + i]));
                }
            }
            return sb1.ToString();
        }


        internal static string FormatByteArray(byte[] b, int limit)
        {
            byte[] b2 = new byte[limit];

            Array.Copy(b, 0, b2, 0, limit);

            return FormatByteArray(b2);
        }
    }
#endif
    #endregion




    /// <summary>
    /// A stream that encrypts as it writes, or decrypts as it reads.  The Crypto is AES in 
    /// CTR (counter) mode, which is 
    /// compatible with the AES encryption employed by WinZip 12.0.
    /// </summary>
    internal class WinZipAesCipherStream : Stream
    {
        private WinZipAesCrypto _params;
        private System.IO.Stream _s;
        private CryptoMode _mode;
        private int _nonce;
        private bool _finalBlock;
        private bool _NextXformWillBeFinal;

        internal HMACSHA1 _mac;

        // Use RijndaelManaged from .NET 2.0. 
        // AesManaged came in .NET 3.5, but we want to limit
        // dependency to .NET 2.0.  AES is just a restricted form
        // of Rijndael (fixed block size of 128, some crypto modes not supported). 

        internal RijndaelManaged _aesCipher;
        internal ICryptoTransform _xform;

        private const int BLOCK_SIZE_IN_BYTES = 16;

        private byte[] counter = new byte[BLOCK_SIZE_IN_BYTES];
        private byte[] counterOut = new byte[BLOCK_SIZE_IN_BYTES];

        // I've had a problem when wrapping a WinZipAesCipherStream inside
        // a DeflateStream. Calling Read() on the DeflateStream results in
        // a Read() on the WinZipAesCipherStream, but the buffer is larger
        // than the total size of the encrypted data, and larger than the
        // initial Read() on the DeflateStream!  When the encrypted
        // bytestream is embedded within a larger stream (As in a zip
        // archive), the Read() doesn't fail with EOF.  This causes bad
        // data to be returned, and it messes up the MAC.

        // This field is used to provide a hard-stop to the size of
        // data that can be read from the stream.  In Read(), if the buffer or
        // read request goes beyond the stop, we truncate it. 

        private long _length;
        private long _totalBytesXferred;


        private byte[] _PendingWriteBuffer;
        private int _pendingCount;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="s">The underlying stream</param>
        /// <param name="mode">To either encrypt or decrypt.</param>
        /// <param name="cryptoParams">The pre-initialized WinZipAesCrypto object.</param>
        /// <param name="length">The maximum number of bytes to read from the stream.</param>
        internal WinZipAesCipherStream(System.IO.Stream s, WinZipAesCrypto cryptoParams, long length, CryptoMode mode)
            : this(s, cryptoParams, mode)
        {
            // don't read beyond this limit! 
            _length = length;
            //Console.WriteLine("max length of AES stream: {0}", _length);
        }


#if WANT_TRACE
            Stream untransformed;
        String traceFileUntransformed;
        Stream transformed;
        String traceFileTransformed;
#endif


        internal WinZipAesCipherStream(System.IO.Stream s, WinZipAesCrypto cryptoParams, CryptoMode mode)
            : base()
        {
            _params = cryptoParams;
            _s = s;
            _mode = mode;
            _nonce = 1;

            if (_params == null)
                throw new BadPasswordException("Supply a password to use AES encryption.");

            int keySizeInBits = _params.KeyBytes.Length * 8;
            if (keySizeInBits != 256 && keySizeInBits != 128 && keySizeInBits != 192)
                throw new ArgumentException("keysize");

            _mac = new HMACSHA1(_params.MacIv);

            _aesCipher = new System.Security.Cryptography.RijndaelManaged();
            _aesCipher.BlockSize = 128;
            _aesCipher.KeySize = keySizeInBits;  // 128, 192, 256
            _aesCipher.Mode = CipherMode.ECB;
            _aesCipher.Padding = PaddingMode.None;

            byte[] iv = new byte[BLOCK_SIZE_IN_BYTES]; // all zeroes

            // Create an ENCRYPTOR, regardless whether doing decryption or encryption. 
            // It is reflexive. 
            _xform = _aesCipher.CreateEncryptor(_params.KeyBytes, iv);


            if (_mode == CryptoMode.Encrypt)
                _PendingWriteBuffer = new byte[BLOCK_SIZE_IN_BYTES];


#if WANT_TRACE
                traceFileUntransformed = "unpack\\WinZipAesCipherStream.trace.untransformed.out";
            traceFileTransformed = "unpack\\WinZipAesCipherStream.trace.transformed.out";

            untransformed = System.IO.File.Create(traceFileUntransformed);
            transformed = System.IO.File.Create(traceFileTransformed);
#endif
        }





        private int ProcessOneBlockWriting(byte[] buffer, int offset, int last)
        {
            if (_finalBlock)
                throw new InvalidOperationException("The final block has already been transformed.");

            int bytesRemaining = last - offset;
            int bytesToRead = (bytesRemaining > BLOCK_SIZE_IN_BYTES)
                ? BLOCK_SIZE_IN_BYTES
                : bytesRemaining;

            // update the counter
            System.Array.Copy(BitConverter.GetBytes(_nonce++), 0, counter, 0, 4);


            if (bytesToRead == (last - offset))
            {
                // We're doing the last bytes in this batch.
                // 
                // For the AES encryption stream to work properly, We must transform full
                // blocks of 16 bytes each, until the very last one.  But, we don't know
                // how to recognize the "last" bytes.  The approach taken here: buffer
                // the last full or partial block of bytes in a batch.  Then at time of
                // Close(), we set the _NextXformWillBeFinal flag and flush that buffer.
                //
                // This works when the caller writes in odd-sized batches, for example
                // 5000 bytes, or in batches that are neat multiples of block-size (16).


                if (_NextXformWillBeFinal) 
                {
                    //Console.WriteLine("WinZipAesCipherStream::ProcessOneBlockWriting:   _NextXformWillBeFinal = true");
                    counterOut = _xform.TransformFinalBlock(counter,
                                                            0,
                                                            BLOCK_SIZE_IN_BYTES);
                    _finalBlock = true;
                }

                else if (buffer==_PendingWriteBuffer && bytesToRead==BLOCK_SIZE_IN_BYTES)
                {
//                  Console.WriteLine("POBW({0},{1,5},{2,5}): pc({4})  flushing {3} bytes ...", 
//                                    _NextXformWillBeFinal, offset, last, bytesToRead, _pendingCount);
                }


                else
                {
                    // NOT the final block, therefore buffer it.

//                  Console.WriteLine("POBW({0},{1,5},{2,5}): pc({4})  buffering {3} more bytes ...", 
//                                    _NextXformWillBeFinal, offset, last, bytesToRead, _pendingCount);

                    Array.Copy(buffer, offset,
                               _PendingWriteBuffer, _pendingCount,
                               bytesToRead);

                    _pendingCount += bytesToRead;

                    // remember to decrement the nonce.
                    _nonce--;
                    return 0;
                }
            }
            

            if (!_finalBlock)
            {
                // Next, do the AES transform.  According to the AES/CTR method used
                // by WinZip, apply the transform to the counter, and then XOR 
                // the result with the ciphertext to get the plaintext.
                _xform.TransformBlock(counter,
                      0, // offset
                      BLOCK_SIZE_IN_BYTES,
                      counterOut,
                      0);  // offset 
            }


            // XOR (in place)
            //Console.Write("POBW({0},{1,5},{2,5}): ", _NextXformWillBeFinal, offset, last);
            for (int i = 0; i < bytesToRead; i++)
            {
                buffer[offset + i] = (byte)(counterOut[i] ^ buffer[offset + i]);
                //Console.Write("{0:X2} ", buffer[offset + i]);
            }
            //Console.WriteLine();

            // when encrypting, do the MAC last
            if (_finalBlock)
                _mac.TransformFinalBlock(buffer, offset, bytesToRead);
            else
                _mac.TransformBlock(buffer, offset, bytesToRead, null, 0);

            return bytesToRead;
        }





        private int ProcessOneBlockReading(byte[] buffer, int offset, int count)
        {
            if (_finalBlock)
                throw new NotSupportedException();

            int bytesRemaining = count - offset;
            int bytesToRead = (bytesRemaining > BLOCK_SIZE_IN_BYTES)
        ? BLOCK_SIZE_IN_BYTES
        : bytesRemaining;

            // When READING,
            // Can we determine if this is the final block based on _length??
            // and totalBytesRead ? YES.
            if (_length > 0)
            {
                //              Console.WriteLine("POBR: Comparing _totalBytesXferred({0}) + count({1}) == _length({2})", 
                //                                _totalBytesXferred,  count,  _length); 
                //              Console.WriteLine("      Comparing bytesToRead({0}) == bytesRemaining({1})",
                //                                bytesToRead, bytesRemaining);
                if ((_totalBytesXferred + count == _length) && (bytesToRead == bytesRemaining))
                {
                    _NextXformWillBeFinal = true;
                    //Console.WriteLine("WinZipAesCipherStream::ProcessOneBlockReading:   _NextXformWillBeFinal = true");
                }
            }

            // update the counter
            System.Array.Copy(BitConverter.GetBytes(_nonce++), 0, counter, 0, 4);

            if (_NextXformWillBeFinal && (bytesToRead == (count - offset)))
            {
                _mac.TransformFinalBlock(buffer, offset, bytesToRead);
                counterOut = _xform.TransformFinalBlock(counter,
                            0,
                            BLOCK_SIZE_IN_BYTES);

                _finalBlock = true;
            }
            else
            {
                // first, do the MAC on the ciphertext
                _mac.TransformBlock(buffer, offset, bytesToRead, null, 0);

                // Next, do the decryption.  According to the AES/CTR method used
                // by WinZip, apply the transform to the counter, and then XOR 
                // the result with the ciphertext to get the plaintext.
                _xform.TransformBlock(counter,
                      0, // offset
                      BLOCK_SIZE_IN_BYTES,
                      counterOut,
                      0);  // offset 
            }

            // XOR (in place)
            //Console.Write("POBR: ");
            for (int i = 0; i < bytesToRead; i++)
            {
                buffer[offset + i] = (byte)(counterOut[i] ^ buffer[offset + i]);
                //Console.Write("{0:X2} ", buffer[offset + i]);
            }
            //Console.WriteLine();

            return bytesToRead;
        }




#if NOTUSED
        public void NotifyFinal()
        {
            // Caller is telling us that the next Read() will be the final Read(),
            // or the next Write() will be the final Write().

            // To get proper results, we need to call TransformFinalBlock() on
            // both The MAC and the decryption xform. But we don't know how to
            // recognize the final block, in order to make those calls. 
            // And we cannot do it after the fact.  

            // A heuristic won't work. For example, we cannot depend on a
            // partial block being the final one.  First, because some files
            // will be an exact multiple of 16-bytes, and there will be no
            // partial blocks.  Second, because the caller may read in batches
            // (let's say 5000 bytes), and we can get partial blocks for each
            // batch. 

            // The only reliable way to know it is the final block is for the caller 
            // to tell us so.  This method how the caller can do that. 

            // After this has been called, then the last block in that batch 
            // will be treated as final. 

            // Typically, the caller will call NotifyFinal() the last time
            // through a loop that also includes a Read() call.

            //_NextXformWillBeFinal = true;
            //Console.WriteLine("WinZipAesCipherStream: next xform() will be the last...");
        }

#endif


        private void TransformInPlace(byte[] buffer, int offset, int count)
        {
            int posn = offset;

            //Console.WriteLine("  TransformInPlace off({0}) count({1})", offset, count);

            while (posn < buffer.Length && posn < count + offset)
            {
                // should I use a delegate for this?
                if (_mode == CryptoMode.Encrypt)
                    ProcessOneBlockWriting(buffer, posn, count + offset);
                else
                    ProcessOneBlockReading(buffer, posn, count + offset);
                posn += BLOCK_SIZE_IN_BYTES;
            }
        }



        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_mode == CryptoMode.Encrypt)
                throw new NotSupportedException();

            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset < 0 || count < 0)
                throw new ArgumentException("Invalid parameters");

            if (buffer.Length < offset + count)
                throw new ArgumentException("The buffer is too small");

            // When I wrap a WinZipAesStream in a DeflateStream, the 
            // DeflateStream asks its captive to read 4k blocks, even if the 
            // encrypted bytestream is smaller than that.  This is a way to 
            // limit the number of bytes read. 

            int bytesToRead = count;

            if (_totalBytesXferred >= _length)
            {
                //Console.WriteLine("done reading... returning 0.");
                return 0; // EOF
            }

            long bytesRemaining = _length - _totalBytesXferred;
            if (bytesRemaining < count) bytesToRead = (int)bytesRemaining;

            int n = _s.Read(buffer, offset, bytesToRead);


#if WANT_TRACE
                untransformed.Write(buffer, offset, bytesToRead);
#endif

            TransformInPlace(buffer, offset, bytesToRead);

#if WANT_TRACE
                transformed.Write(buffer, offset, bytesToRead);
#endif
            _totalBytesXferred += n;

            return n; // ?
            //return bytesToRead;
            //return count;
        }




        /// <summary>
        /// Returns the final HMAC-SHA1-80 
        /// </summary>
        public byte[] FinalAuthentication
        {
            get
            {
                if (!_finalBlock)
                {
                    // special-case zero-byte files
                    if ( _totalBytesXferred != 0)
                        throw new BadStateException("The final hash has not been computed.");

                    // Must call ComputeHash on an empty byte array when no data
                    // has run through the MAC.

                    byte[] b = {  };
                    _mac.ComputeHash(b);
                    // fall through
                }
                byte[] macBytes10 = new byte[10];
                System.Array.Copy(_mac.Hash, 0, macBytes10, 0, 10);
                return macBytes10;
            }
        }




        public override void Write(byte[] buffer, int offset, int count)
        {
            // This class cannot decrypt as it writes. 
            // If you want to decrypt, then READ through the stream.
            if (_mode == CryptoMode.Decrypt)
                throw new NotSupportedException();

            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset < 0 || count < 0)
                throw new ArgumentException("Invalid parameters");

            if (buffer.Length < offset + count)
                throw new ArgumentException("The offset and count are too large");

            if (count == 0)
                return;

            // Console.WriteLine("WinZipAesCipherStream::Write off({0}) count({1})", offset, count);

#if WANT_TRACE
            untransformed.Write(buffer, offset, count);
#endif

            if (_pendingCount != 0)
            {
                if (count + _pendingCount <= BLOCK_SIZE_IN_BYTES) 
                {
                    // not enough to overflow the pending buffer.
                    Array.Copy(buffer, offset,
                           _PendingWriteBuffer, _pendingCount,
                           count);
                    _pendingCount += count;

                    // At this point, _PendingWriteBuffer contains up to BLOCK_SIZE_IN_BYTES
                    // bytes, and _pendingCount ranges from 0 to 16. We don't want to
                    // xform+write them yet, because this may have been the last block.  The
                    // last block gets written at Close().
                    return;
                }

                // We have more than one block of data to write, therefore it is safe
                // to xform+write. 
                int extra = BLOCK_SIZE_IN_BYTES - _pendingCount;

                // NB: extra is possibly zero here.

                //Console.WriteLine("WZACS::Write Flush  extra({0}) pc({1})", extra, _pendingCount);

                Array.Copy(buffer, offset,
                       _PendingWriteBuffer, _pendingCount,
                       extra);

                // adjust counts:
                _pendingCount = 0;
                offset += extra;
                count -= extra;

                // xform and write: 
                ProcessOneBlockWriting(_PendingWriteBuffer, 0, BLOCK_SIZE_IN_BYTES);
                _s.Write(_PendingWriteBuffer, 0, BLOCK_SIZE_IN_BYTES);
                _totalBytesXferred += BLOCK_SIZE_IN_BYTES;
            }

            TransformInPlace(buffer, offset, count);

#if WANT_TRACE
            transformed.Write(buffer, offset, count);
#endif

            //Console.WriteLine("WinZipAesCipherStream::Writing: count({0})", count - _pendingCount);

            _s.Write(buffer, offset, count - _pendingCount);
            _totalBytesXferred += count - _pendingCount;

        }



        public override void Close()
        {
            if (_pendingCount != 0)
            {
                // xform and write whatever is left over
                _NextXformWillBeFinal = true;
                ProcessOneBlockWriting(_PendingWriteBuffer, 0, _pendingCount);
                _s.Write(_PendingWriteBuffer, 0, _pendingCount);
                _totalBytesXferred += _pendingCount;
            }


            _s.Close();
#if WANT_TRACE
            untransformed.Close();
            transformed.Close();
            Console.WriteLine("\nuntransformed bytestream is in  {0}", traceFileUntransformed);
            Console.WriteLine("\ntransformed bytestream is in  {0}", traceFileTransformed);
#endif
        }


        public override bool CanRead
        {
            get
            {
                if (_mode != CryptoMode.Decrypt) return false;
                return true;
            }
        }
        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return (_mode == CryptoMode.Encrypt); }
        }

        public override void Flush()
        {
            _s.Flush();
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

    }



}
#endif
