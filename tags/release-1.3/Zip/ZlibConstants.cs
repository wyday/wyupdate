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
* This program is based on zlib-1.1.3; credit to authors
* Jean-loup Gailly(jloup@gzip.org) and Mark Adler(madler@alumni.caltech.edu)
* and contributors of zlib.
*/


using System;

namespace Ionic.Zlib
{
    /// <summary>
    /// A bunch of constants used in the Zlib interface.
    /// </summary>
    public static class ZlibConstants
    {
        /// <summary>
        /// No flush at all.
        /// </summary>
        public const int Z_NO_FLUSH = 0;

        /// <summary>
        /// A little bit of flush?
        /// </summary>
        public const int Z_PARTIAL_FLUSH = 1;

        /// <summary>
        /// Sync flush?
        /// </summary>
        public const int Z_SYNC_FLUSH = 2;

        /// <summary>
        /// Full flush?
        /// </summary>
        public const int Z_FULL_FLUSH = 3;

        /// <summary>
        /// Finish?
        /// </summary>
        public const int Z_FINISH = 4;

        /// <summary>
        /// The maximum number of window bits for the Deflate algorithm.
        /// </summary>
        public const int WINDOW_BITS_MAX = 15; // 32K LZ77 window

        /// <summary>
        /// The default number of window bits for the Deflate algorithm.
        /// </summary>
        public const int WINDOW_BITS_DEFAULT = WINDOW_BITS_MAX;

        /// <summary>
        /// indicates everything is A-OK
        /// </summary>
        public const int Z_OK = 0;

        /// <summary>
        /// Indicates that the last operation reached the end of the stream.
        /// </summary>
        public const int Z_STREAM_END = 1;

        /// <summary>
        /// The operation ended in need of a dictionary. 
        /// </summary>
        public const int Z_NEED_DICT = 2;

        /// <summary>
        /// There was an error with the stream - not enough data, not open and readable, etc.
        /// </summary>
        public const int Z_STREAM_ERROR = -2;

        /// <summary>
        /// There was an error with the data - not enough data, bad data, etc.
        /// </summary>
        public const int Z_DATA_ERROR = -3;

        /// <summary>
        /// There was an error with the working buffer.
        /// </summary>
        public const int Z_BUF_ERROR = -5;

    }

}

