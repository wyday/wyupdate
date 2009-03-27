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
* This program is based on zlib-1.1.3, so all credit should go authors
* Jean-loup Gailly(jloup@gzip.org) and Mark Adler(madler@alumni.caltech.edu)
* and contributors of zlib.
*/


using System;
namespace Ionic.Zlib
{
    sealed class InflateBlocks
    {
        private const int MANY = 1440;

        // And'ing with mask[n] masks the lower n bits
        //UPGRADE_NOTE: Final was removed from the declaration of 'inflate_mask'. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        private static readonly int[] inflate_mask = new int[] { 0x00000000, 0x00000001, 0x00000003, 0x00000007, 0x0000000f, 0x0000001f, 0x0000003f, 0x0000007f, 0x000000ff, 0x000001ff, 0x000003ff, 0x000007ff, 0x00000fff, 0x00001fff, 0x00003fff, 0x00007fff, 0x0000ffff };

        // Table for deflate from PKZIP's appnote.txt.
        //UPGRADE_NOTE: Final was removed from the declaration of 'border'. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        internal static readonly int[] border = new int[] { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

        private const int TYPE = 0; // get type bits (3, including end bit)
        private const int LENS = 1; // get lengths for stored
        private const int STORED = 2; // processing stored block
        private const int TABLE = 3; // get table lengths
        private const int BTREE = 4; // get bit lengths tree for a dynamic block
        private const int DTREE = 5; // get length, distance trees for a dynamic block
        private const int CODES = 6; // processing fixed or dynamic block
        private const int DRY = 7; // output remaining window bytes
        private const int DONE = 8; // finished last block, done
        private const int BAD = 9; // ot a data error--stuck here

        internal int mode; // current inflate_block mode 

        internal int left; // if STORED, bytes left to copy 

        internal int table; // table lengths (14 bits) 
        internal int index; // index into blens (or border) 
        internal int[] blens; // bit lengths of codes 
        internal int[] bb = new int[1]; // bit length tree depth 
        internal int[] tb = new int[1]; // bit length decoding tree 

        internal InflateCodes codes = new InflateCodes(); // if CODES, current state 

        internal int last; // true if this block is the last block 

        // mode independent information 
        internal int bitk; // bits in bit buffer 
        internal int bitb; // bit buffer 
        internal int[] hufts; // single malloc for tree space 
        internal byte[] window; // sliding window 
        internal int end; // one byte after sliding window 
        internal int read; // window read pointer 
        internal int write; // window write pointer 
        internal System.Object checkfn; // check function 
        internal long check; // check on output 

        internal InfTree inftree = new InfTree();

        internal InflateBlocks(ZlibCodec z, System.Object checkfn, int w)
        {
            hufts = new int[MANY * 3];
            window = new byte[w];
            end = w;
            this.checkfn = checkfn;
            mode = TYPE;
            Reset(z, null);
        }

        internal void Reset(ZlibCodec z, long[] c)
        {
            if (c != null)
                c[0] = check;
            if (mode == BTREE || mode == DTREE)
            {
            }
            if (mode == CODES)
            {
            }
            mode = TYPE;
            bitk = 0;
            bitb = 0;
            read = write = 0;

            if (checkfn != null)
                z._Adler32 = check = Adler.Adler32(0L, null, 0, 0);
        }

        internal int Process(ZlibCodec z, int r)
        {
            int t; // temporary storage
            int b; // bit buffer
            int k; // bits in bit buffer
            int p; // input data pointer
            int n; // bytes available there
            int q; // output window write pointer
            int m; // bytes to end of window or read pointer

            // copy input/output information to locals (UPDATE macro restores)
            {
                p = z.NextIn; n = z.AvailableBytesIn; b = bitb; k = bitk;
            }
            {
                q = write; m = (int)(q < read ? read - q - 1 : end - q);
            }

            // process input based on current state
            while (true)
            {
                switch (mode)
                {

                    case TYPE:

                        while (k < (3))
                        {
                            if (n != 0)
                            {
                                r = ZlibConstants.Z_OK;
                            }
                            else
                            {
                                bitb = b; bitk = k;
                                z.AvailableBytesIn = n;
                                z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                write = q;
                                return Flush(z, r);
                            }
                            ;
                            n--;
                            b |= (z.InputBuffer[p++] & 0xff) << k;
                            k += 8;
                        }
                        t = (int)(b & 7);
                        last = t & 1;

                        switch (SharedUtils.URShift(t, 1))
                        {

                            case 0:  // stored 
                                {
                                    b = SharedUtils.URShift(b, (3)); k -= (3);
                                }
                                t = k & 7; // go to byte boundary
                                {
                                    b = SharedUtils.URShift(b, (t)); k -= (t);
                                }
                                mode = LENS; // get length of stored block
                                break;

                            case 1:  // fixed
                                {
                                    int[] bl = new int[1];
                                    int[] bd = new int[1];
                                    int[][] tl = new int[1][];
                                    int[][] td = new int[1][];

                                    InfTree.inflate_trees_fixed(bl, bd, tl, td, z);
                                    codes.Init(bl[0], bd[0], tl[0], 0, td[0], 0, z);
                                }
                                {
                                    b = SharedUtils.URShift(b, (3)); k -= (3);
                                }

                                mode = CODES;
                                break;

                            case 2:  // dynamic
                                {
                                    b = SharedUtils.URShift(b, (3)); k -= (3);
                                }

                                mode = TABLE;
                                break;

                            case 3:  // illegal
                                {
                                    b = SharedUtils.URShift(b, (3)); k -= (3);
                                }
                                mode = BAD;
                                z.Message = "invalid block type";
                                r = ZlibConstants.Z_DATA_ERROR;

                                bitb = b; bitk = k;
                                z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                write = q;
                                return Flush(z, r);
                        }
                        break;

                    case LENS:

                        while (k < (32))
                        {
                            if (n != 0)
                            {
                                r = ZlibConstants.Z_OK;
                            }
                            else
                            {
                                bitb = b; bitk = k;
                                z.AvailableBytesIn = n;
                                z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                write = q;
                                return Flush(z, r);
                            }
                            ;
                            n--;
                            b |= (z.InputBuffer[p++] & 0xff) << k;
                            k += 8;
                        }

                        if (((SharedUtils.URShift((~b), 16)) & 0xffff) != (b & 0xffff))
                        {
                            mode = BAD;
                            z.Message = "invalid stored block lengths";
                            r = ZlibConstants.Z_DATA_ERROR;

                            bitb = b; bitk = k;
                            z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                            write = q;
                            return Flush(z, r);
                        }
                        left = (b & 0xffff);
                        b = k = 0; // dump bits
                        mode = left != 0 ? STORED : (last != 0 ? DRY : TYPE);
                        break;

                    case STORED:
                        if (n == 0)
                        {
                            bitb = b; bitk = k;
                            z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                            write = q;
                            return Flush(z, r);
                        }

                        if (m == 0)
                        {
                            if (q == end && read != 0)
                            {
                                q = 0; m = (int)(q < read ? read - q - 1 : end - q);
                            }
                            if (m == 0)
                            {
                                write = q;
                                r = Flush(z, r);
                                q = write; m = (int)(q < read ? read - q - 1 : end - q);
                                if (q == end && read != 0)
                                {
                                    q = 0; m = (int)(q < read ? read - q - 1 : end - q);
                                }
                                if (m == 0)
                                {
                                    bitb = b; bitk = k;
                                    z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                    write = q;
                                    return Flush(z, r);
                                }
                            }
                        }
                        r = ZlibConstants.Z_OK;

                        t = left;
                        if (t > n)
                            t = n;
                        if (t > m)
                            t = m;
                        Array.Copy(z.InputBuffer, p, window, q, t);
                        p += t; n -= t;
                        q += t; m -= t;
                        if ((left -= t) != 0)
                            break;
                        mode = last != 0 ? DRY : TYPE;
                        break;

                    case TABLE:

                        while (k < (14))
                        {
                            if (n != 0)
                            {
                                r = ZlibConstants.Z_OK;
                            }
                            else
                            {
                                bitb = b; bitk = k;
                                z.AvailableBytesIn = n;
                                z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                write = q;
                                return Flush(z, r);
                            }
                            ;
                            n--;
                            b |= (z.InputBuffer[p++] & 0xff) << k;
                            k += 8;
                        }

                        table = t = (b & 0x3fff);
                        if ((t & 0x1f) > 29 || ((t >> 5) & 0x1f) > 29)
                        {
                            mode = BAD;
                            z.Message = "too many length or distance symbols";
                            r = ZlibConstants.Z_DATA_ERROR;

                            bitb = b; bitk = k;
                            z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                            write = q;
                            return Flush(z, r);
                        }
                        t = 258 + (t & 0x1f) + ((t >> 5) & 0x1f);
                        if (blens == null || blens.Length < t)
                        {
                            blens = new int[t];
                        }
                        else
                        {
                            for (int i = 0; i < t; i++)
                            {
                                blens[i] = 0;
                            }
                        }
                        {
                            b = SharedUtils.URShift(b, (14)); k -= (14);
                        }

                        index = 0;
                        mode = BTREE;
                        goto case BTREE;

                    case BTREE:
                        while (index < 4 + (SharedUtils.URShift(table, 10)))
                        {
                            while (k < (3))
                            {
                                if (n != 0)
                                {
                                    r = ZlibConstants.Z_OK;
                                }
                                else
                                {
                                    bitb = b; bitk = k;
                                    z.AvailableBytesIn = n;
                                    z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                    write = q;
                                    return Flush(z, r);
                                }
                                ;
                                n--;
                                b |= (z.InputBuffer[p++] & 0xff) << k;
                                k += 8;
                            }

                            blens[border[index++]] = b & 7;

                            {
                                b = SharedUtils.URShift(b, (3)); k -= (3);
                            }
                        }

                        while (index < 19)
                        {
                            blens[border[index++]] = 0;
                        }

                        bb[0] = 7;
                        t = inftree.inflate_trees_bits(blens, bb, tb, hufts, z);
                        if (t != ZlibConstants.Z_OK)
                        {
                            r = t;
                            if (r == ZlibConstants.Z_DATA_ERROR)
                            {
                                blens = null;
                                mode = BAD;
                            }

                            bitb = b; bitk = k;
                            z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                            write = q;
                            return Flush(z, r);
                        }

                        index = 0;
                        mode = DTREE;
                        goto case DTREE;

                    case DTREE:
                        while (true)
                        {
                            t = table;
                            if (!(index < 258 + (t & 0x1f) + ((t >> 5) & 0x1f)))
                            {
                                break;
                            }

                            int i, j, c;

                            t = bb[0];

                            while (k < (t))
                            {
                                if (n != 0)
                                {
                                    r = ZlibConstants.Z_OK;
                                }
                                else
                                {
                                    bitb = b; bitk = k;
                                    z.AvailableBytesIn = n;
                                    z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                    write = q;
                                    return Flush(z, r);
                                }
                                ;
                                n--;
                                b |= (z.InputBuffer[p++] & 0xff) << k;
                                k += 8;
                            }

                            if (tb[0] == -1)
                            {
                                //System.err.println("null...");
                            }

                            t = hufts[(tb[0] + (b & inflate_mask[t])) * 3 + 1];
                            c = hufts[(tb[0] + (b & inflate_mask[t])) * 3 + 2];

                            if (c < 16)
                            {
                                b = SharedUtils.URShift(b, (t)); k -= (t);
                                blens[index++] = c;
                            }
                            else
                            {
                                // c == 16..18
                                i = c == 18 ? 7 : c - 14;
                                j = c == 18 ? 11 : 3;

                                while (k < (t + i))
                                {
                                    if (n != 0)
                                    {
                                        r = ZlibConstants.Z_OK;
                                    }
                                    else
                                    {
                                        bitb = b; bitk = k;
                                        z.AvailableBytesIn = n;
                                        z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                        write = q;
                                        return Flush(z, r);
                                    }
                                    ;
                                    n--;
                                    b |= (z.InputBuffer[p++] & 0xff) << k;
                                    k += 8;
                                }

                                b = SharedUtils.URShift(b, (t)); k -= (t);

                                j += (b & inflate_mask[i]);

                                b = SharedUtils.URShift(b, (i)); k -= (i);

                                i = index;
                                t = table;
                                if (i + j > 258 + (t & 0x1f) + ((t >> 5) & 0x1f) || (c == 16 && i < 1))
                                {
                                    blens = null;
                                    mode = BAD;
                                    z.Message = "invalid bit length repeat";
                                    r = ZlibConstants.Z_DATA_ERROR;

                                    bitb = b; bitk = k;
                                    z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                    write = q;
                                    return Flush(z, r);
                                }

                                c = c == 16 ? blens[i - 1] : 0;
                                do
                                {
                                    blens[i++] = c;
                                }
                                while (--j != 0);
                                index = i;
                            }
                        }

                        tb[0] = -1;
                        {
                            int[] bl = new int[] { 9 };  // must be <= 9 for lookahead assumptions
                            int[] bd = new int[] { 6 }; // must be <= 9 for lookahead assumptions							
                            int[] tl = new int[1];
                            int[] td = new int[1];

                            t = table;
                            t = inftree.inflate_trees_dynamic(257 + (t & 0x1f), 1 + ((t >> 5) & 0x1f), blens, bl, bd, tl, td, hufts, z);

                            if (t != ZlibConstants.Z_OK)
                            {
                                if (t == ZlibConstants.Z_DATA_ERROR)
                                {
                                    blens = null;
                                    mode = BAD;
                                }
                                r = t;

                                bitb = b; bitk = k;
                                z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                write = q;
                                return Flush(z, r);
                            }
                            codes.Init(bl[0], bd[0], hufts, tl[0], hufts, td[0], z);
                        }
                        mode = CODES;
                        goto case CODES;

                    case CODES:
                        bitb = b; bitk = k;
                        z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                        write = q;

                        if ((r = codes.Process(this, z, r)) != ZlibConstants.Z_STREAM_END)
                        {
                            return Flush(z, r);
                        }
                        r = ZlibConstants.Z_OK;
                        p = z.NextIn; n = z.AvailableBytesIn; b = bitb; k = bitk;
                        q = write; m = (int)(q < read ? read - q - 1 : end - q);

                        if (last == 0)
                        {
                            mode = TYPE;
                            break;
                        }
                        mode = DRY;
                        goto case DRY;

                    case DRY:
                        write = q;
                        r = Flush(z, r);
                        q = write; m = (int)(q < read ? read - q - 1 : end - q);
                        if (read != write)
                        {
                            bitb = b; bitk = k;
                            z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                            write = q;
                            return Flush(z, r);
                        }
                        mode = DONE;
                        goto case DONE;

                    case DONE:
                        r = ZlibConstants.Z_STREAM_END;

                        bitb = b; bitk = k;
                        z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                        write = q;
                        return Flush(z, r);

                    case BAD:
                        r = ZlibConstants.Z_DATA_ERROR;

                        bitb = b; bitk = k;
                        z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                        write = q;
                        return Flush(z, r);


                    default:
                        r = ZlibConstants.Z_STREAM_ERROR;

                        bitb = b; bitk = k;
                        z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                        write = q;
                        return Flush(z, r);

                }
            }
        }

        internal void Free(ZlibCodec z)
        {
            Reset(z, null);
            window = null;
            hufts = null;
            //ZFREE(z, s);
        }

        internal void SetDictionary(byte[] d, int start, int n)
        {
            Array.Copy(d, start, window, 0, n);
            read = write = n;
        }

        // Returns true if inflate is currently at the end of a block generated
        // by Z_SYNC_FLUSH or Z_FULL_FLUSH. 
        internal int SyncPoint()
        {
            return mode == LENS ? 1 : 0;
        }

        // copy as much as possible from the sliding window to the output area
        internal int Flush(ZlibCodec z, int r)
        {
            int n;
            int p;
            int q;

            // local copies of source and destination pointers
            p = z.NextOut;
            q = read;

            // compute number of bytes to copy as far as end of window
            n = (int)((q <= write ? write : end) - q);
            if (n > z.AvailableBytesOut)
                n = z.AvailableBytesOut;
            if (n != 0 && r == ZlibConstants.Z_BUF_ERROR)
                r = ZlibConstants.Z_OK;

            // update counters
            z.AvailableBytesOut -= n;
            z.TotalBytesOut += n;

            // update check information
            if (checkfn != null)
                z._Adler32 = check = Adler.Adler32(check, window, q, n);

            // copy as far as end of window
            Array.Copy(window, q, z.OutputBuffer, p, n);
            p += n;
            q += n;

            // see if more to copy at beginning of window
            if (q == end)
            {
                // wrap pointers
                q = 0;
                if (write == end)
                    write = 0;

                // compute bytes to copy
                n = write - q;
                if (n > z.AvailableBytesOut)
                    n = z.AvailableBytesOut;
                if (n != 0 && r == ZlibConstants.Z_BUF_ERROR)
                    r = ZlibConstants.Z_OK;

                // update counters
                z.AvailableBytesOut -= n;
                z.TotalBytesOut += n;

                // update check information
                if (checkfn != null)
                    z._Adler32 = check = Adler.Adler32(check, window, q, n);

                // copy
                Array.Copy(window, q, z.OutputBuffer, p, n);
                p += n;
                q += n;
            }

            // update pointers
            z.NextOut = p;
            read = q;

            // done
            return r;
        }
    }

    sealed class InflateCodes
    {
        //UPGRADE_NOTE: Final was removed from the declaration of 'inflate_mask'. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        private static readonly int[] inflate_mask = new int[] { 0x00000000, 0x00000001, 0x00000003, 0x00000007, 0x0000000f, 0x0000001f, 0x0000003f, 0x0000007f, 0x000000ff, 0x000001ff, 0x000003ff, 0x000007ff, 0x00000fff, 0x00001fff, 0x00003fff, 0x00007fff, 0x0000ffff };

        // waiting for "i:"=input,
        //             "o:"=output,
        //             "x:"=nothing
        private const int START = 0; // x: set up for LEN
        private const int LEN = 1; // i: get length/literal/eob next
        private const int LENEXT = 2; // i: getting length extra (have base)
        private const int DIST = 3; // i: get distance next
        private const int DISTEXT = 4; // i: getting distance extra
        private const int COPY = 5; // o: copying bytes in window, waiting for space
        private const int LIT = 6; // o: got literal, waiting for output space
        private const int WASH = 7; // o: got eob, possibly still output waiting
        private const int END = 8; // x: got eob and all data flushed
        private const int BADCODE = 9; // x: got error

        internal int mode; // current inflate_codes mode

        // mode dependent information
        internal int len;

        internal int[] tree; // pointer into tree
        internal int tree_index = 0;
        internal int need; // bits needed

        internal int lit;

        // if EXT or COPY, where and how much
        internal int get_Renamed; // bits to get for extra
        internal int dist; // distance back to copy from

        internal byte lbits; // ltree bits decoded per branch
        internal byte dbits; // dtree bits decoder per branch
        internal int[] ltree; // literal/length/eob tree
        internal int ltree_index; // literal/length/eob tree
        internal int[] dtree; // distance tree
        internal int dtree_index; // distance tree

        internal InflateCodes()
        {
        }

        internal void Init(int bl, int bd, int[] tl, int tl_index, int[] td, int td_index, ZlibCodec z)
        {
            mode = START;
            lbits = (byte)bl;
            dbits = (byte)bd;
            ltree = tl;
            ltree_index = tl_index;
            dtree = td;
            dtree_index = td_index;
            tree = null;
        }

        internal int Process(InflateBlocks blocks, ZlibCodec z, int r)
        {
            int j; // temporary storage
            int tindex; // temporary pointer
            int e; // extra bits or operation
            int b = 0; // bit buffer
            int k = 0; // bits in bit buffer
            int p = 0; // input data pointer
            int n; // bytes available there
            int q; // output window write pointer
            int m; // bytes to end of window or read pointer
            int f; // pointer to copy strings from

            // copy input/output information to locals (UPDATE macro restores)
            p = z.NextIn; n = z.AvailableBytesIn; b = blocks.bitb; k = blocks.bitk;
            q = blocks.write; m = q < blocks.read ? blocks.read - q - 1 : blocks.end - q;

            // process input and output based on current state
            while (true)
            {
                switch (mode)
                {
                    // waiting for "i:"=input, "o:"=output, "x:"=nothing
                    case START:  // x: set up for LEN
                        if (m >= 258 && n >= 10)
                        {

                            blocks.bitb = b; blocks.bitk = k;
                            z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                            blocks.write = q;
                            r = InflateFast(lbits, dbits, ltree, ltree_index, dtree, dtree_index, blocks, z);

                            p = z.NextIn; n = z.AvailableBytesIn; b = blocks.bitb; k = blocks.bitk;
                            q = blocks.write; m = q < blocks.read ? blocks.read - q - 1 : blocks.end - q;

                            if (r != ZlibConstants.Z_OK)
                            {
                                mode = (r == ZlibConstants.Z_STREAM_END) ? WASH : BADCODE;
                                break;
                            }
                        }
                        need = lbits;
                        tree = ltree;
                        tree_index = ltree_index;

                        mode = LEN;
                        goto case LEN;

                    case LEN:  // i: get length/literal/eob next
                        j = need;

                        while (k < (j))
                        {
                            if (n != 0)
                                r = ZlibConstants.Z_OK;
                            else
                            {

                                blocks.bitb = b; blocks.bitk = k;
                                z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                blocks.write = q;
                                return blocks.Flush(z, r);
                            }
                            n--;
                            b |= (z.InputBuffer[p++] & 0xff) << k;
                            k += 8;
                        }

                        tindex = (tree_index + (b & inflate_mask[j])) * 3;

                        b = SharedUtils.URShift(b, (tree[tindex + 1]));
                        k -= (tree[tindex + 1]);

                        e = tree[tindex];

                        if (e == 0)
                        {
                            // literal
                            lit = tree[tindex + 2];
                            mode = LIT;
                            break;
                        }
                        if ((e & 16) != 0)
                        {
                            // length
                            get_Renamed = e & 15;
                            len = tree[tindex + 2];
                            mode = LENEXT;
                            break;
                        }
                        if ((e & 64) == 0)
                        {
                            // next table
                            need = e;
                            tree_index = tindex / 3 + tree[tindex + 2];
                            break;
                        }
                        if ((e & 32) != 0)
                        {
                            // end of block
                            mode = WASH;
                            break;
                        }
                        mode = BADCODE; // invalid code
                        z.Message = "invalid literal/length code";
                        r = ZlibConstants.Z_DATA_ERROR;

                        blocks.bitb = b; blocks.bitk = k;
                        z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                        blocks.write = q;
                        return blocks.Flush(z, r);


                    case LENEXT:  // i: getting length extra (have base)
                        j = get_Renamed;

                        while (k < (j))
                        {
                            if (n != 0)
                                r = ZlibConstants.Z_OK;
                            else
                            {

                                blocks.bitb = b; blocks.bitk = k;
                                z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                blocks.write = q;
                                return blocks.Flush(z, r);
                            }
                            n--; b |= (z.InputBuffer[p++] & 0xff) << k;
                            k += 8;
                        }

                        len += (b & inflate_mask[j]);

                        b >>= j;
                        k -= j;

                        need = dbits;
                        tree = dtree;
                        tree_index = dtree_index;
                        mode = DIST;
                        goto case DIST;

                    case DIST:  // i: get distance next
                        j = need;

                        while (k < (j))
                        {
                            if (n != 0)
                                r = ZlibConstants.Z_OK;
                            else
                            {

                                blocks.bitb = b; blocks.bitk = k;
                                z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                blocks.write = q;
                                return blocks.Flush(z, r);
                            }
                            n--; b |= (z.InputBuffer[p++] & 0xff) << k;
                            k += 8;
                        }

                        tindex = (tree_index + (b & inflate_mask[j])) * 3;

                        b >>= tree[tindex + 1];
                        k -= tree[tindex + 1];

                        e = (tree[tindex]);
                        if ((e & 16) != 0)
                        {
                            // distance
                            get_Renamed = e & 15;
                            dist = tree[tindex + 2];
                            mode = DISTEXT;
                            break;
                        }
                        if ((e & 64) == 0)
                        {
                            // next table
                            need = e;
                            tree_index = tindex / 3 + tree[tindex + 2];
                            break;
                        }
                        mode = BADCODE; // invalid code
                        z.Message = "invalid distance code";
                        r = ZlibConstants.Z_DATA_ERROR;

                        blocks.bitb = b; blocks.bitk = k;
                        z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                        blocks.write = q;
                        return blocks.Flush(z, r);


                    case DISTEXT:  // i: getting distance extra
                        j = get_Renamed;

                        while (k < (j))
                        {
                            if (n != 0)
                                r = ZlibConstants.Z_OK;
                            else
                            {

                                blocks.bitb = b; blocks.bitk = k;
                                z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                blocks.write = q;
                                return blocks.Flush(z, r);
                            }
                            n--; b |= (z.InputBuffer[p++] & 0xff) << k;
                            k += 8;
                        }

                        dist += (b & inflate_mask[j]);

                        b >>= j;
                        k -= j;

                        mode = COPY;
                        goto case COPY;

                    case COPY:  // o: copying bytes in window, waiting for space
                        f = q - dist;
                        while (f < 0)
                        {
                            // modulo window size-"while" instead
                            f += blocks.end; // of "if" handles invalid distances
                        }
                        while (len != 0)
                        {

                            if (m == 0)
                            {
                                if (q == blocks.end && blocks.read != 0)
                                {
                                    q = 0; m = q < blocks.read ? blocks.read - q - 1 : blocks.end - q;
                                }
                                if (m == 0)
                                {
                                    blocks.write = q; r = blocks.Flush(z, r);
                                    q = blocks.write; m = q < blocks.read ? blocks.read - q - 1 : blocks.end - q;

                                    if (q == blocks.end && blocks.read != 0)
                                    {
                                        q = 0; m = q < blocks.read ? blocks.read - q - 1 : blocks.end - q;
                                    }

                                    if (m == 0)
                                    {
                                        blocks.bitb = b; blocks.bitk = k;
                                        z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                        blocks.write = q;
                                        return blocks.Flush(z, r);
                                    }
                                }
                            }

                            blocks.window[q++] = blocks.window[f++]; m--;

                            if (f == blocks.end)
                                f = 0;
                            len--;
                        }
                        mode = START;
                        break;

                    case LIT:  // o: got literal, waiting for output space
                        if (m == 0)
                        {
                            if (q == blocks.end && blocks.read != 0)
                            {
                                q = 0; m = q < blocks.read ? blocks.read - q - 1 : blocks.end - q;
                            }
                            if (m == 0)
                            {
                                blocks.write = q; r = blocks.Flush(z, r);
                                q = blocks.write; m = q < blocks.read ? blocks.read - q - 1 : blocks.end - q;

                                if (q == blocks.end && blocks.read != 0)
                                {
                                    q = 0; m = q < blocks.read ? blocks.read - q - 1 : blocks.end - q;
                                }
                                if (m == 0)
                                {
                                    blocks.bitb = b; blocks.bitk = k;
                                    z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                    blocks.write = q;
                                    return blocks.Flush(z, r);
                                }
                            }
                        }
                        r = ZlibConstants.Z_OK;

                        blocks.window[q++] = (byte)lit; m--;

                        mode = START;
                        break;

                    case WASH:  // o: got eob, possibly more output
                        if (k > 7)
                        {
                            // return unused byte, if any
                            k -= 8;
                            n++;
                            p--; // can always return one
                        }

                        blocks.write = q; r = blocks.Flush(z, r);
                        q = blocks.write; m = q < blocks.read ? blocks.read - q - 1 : blocks.end - q;

                        if (blocks.read != blocks.write)
                        {
                            blocks.bitb = b; blocks.bitk = k;
                            z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                            blocks.write = q;
                            return blocks.Flush(z, r);
                        }
                        mode = END;
                        goto case END;

                    case END:
                        r = ZlibConstants.Z_STREAM_END;
                        blocks.bitb = b; blocks.bitk = k;
                        z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                        blocks.write = q;
                        return blocks.Flush(z, r);


                    case BADCODE:  // x: got error

                        r = ZlibConstants.Z_DATA_ERROR;

                        blocks.bitb = b; blocks.bitk = k;
                        z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                        blocks.write = q;
                        return blocks.Flush(z, r);


                    default:
                        r = ZlibConstants.Z_STREAM_ERROR;

                        blocks.bitb = b; blocks.bitk = k;
                        z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                        blocks.write = q;
                        return blocks.Flush(z, r);

                }
            }
        }


        // Called with number of bytes left to write in window at least 258
        // (the maximum string length) and number of input bytes available
        // at least ten.  The ten bytes are six bytes for the longest length/
        // distance pair plus four bytes for overloading the bit buffer.

        internal int InflateFast(int bl, int bd, int[] tl, int tl_index, int[] td, int td_index, InflateBlocks s, ZlibCodec z)
        {
            int t; // temporary pointer
            int[] tp; // temporary pointer
            int tp_index; // temporary pointer
            int e; // extra bits or operation
            int b; // bit buffer
            int k; // bits in bit buffer
            int p; // input data pointer
            int n; // bytes available there
            int q; // output window write pointer
            int m; // bytes to end of window or read pointer
            int ml; // mask for literal/length tree
            int md; // mask for distance tree
            int c; // bytes to copy
            int d; // distance back to copy from
            int r; // copy source pointer

            int tp_index_t_3; // (tp_index+t)*3

            // load input, output, bit values
            p = z.NextIn; n = z.AvailableBytesIn; b = s.bitb; k = s.bitk;
            q = s.write; m = q < s.read ? s.read - q - 1 : s.end - q;

            // initialize masks
            ml = inflate_mask[bl];
            md = inflate_mask[bd];

            // do until not enough input or output space for fast loop
            do
            {
                // assume called with m >= 258 && n >= 10
                // get literal/length code
                while (k < (20))
                {
                    // max bits for literal/length code
                    n--;
                    b |= (z.InputBuffer[p++] & 0xff) << k; k += 8;
                }

                t = b & ml;
                tp = tl;
                tp_index = tl_index;
                tp_index_t_3 = (tp_index + t) * 3;
                if ((e = tp[tp_index_t_3]) == 0)
                {
                    b >>= (tp[tp_index_t_3 + 1]); k -= (tp[tp_index_t_3 + 1]);

                    s.window[q++] = (byte)tp[tp_index_t_3 + 2];
                    m--;
                    continue;
                }
                do
                {

                    b >>= (tp[tp_index_t_3 + 1]); k -= (tp[tp_index_t_3 + 1]);

                    if ((e & 16) != 0)
                    {
                        e &= 15;
                        c = tp[tp_index_t_3 + 2] + ((int)b & inflate_mask[e]);

                        b >>= e; k -= e;

                        // decode distance base of block to copy
                        while (k < (15))
                        {
                            // max bits for distance code
                            n--;
                            b |= (z.InputBuffer[p++] & 0xff) << k; k += 8;
                        }

                        t = b & md;
                        tp = td;
                        tp_index = td_index;
                        tp_index_t_3 = (tp_index + t) * 3;
                        e = tp[tp_index_t_3];

                        do
                        {

                            b >>= (tp[tp_index_t_3 + 1]); k -= (tp[tp_index_t_3 + 1]);

                            if ((e & 16) != 0)
                            {
                                // get extra bits to add to distance base
                                e &= 15;
                                while (k < (e))
                                {
                                    // get extra bits (up to 13)
                                    n--;
                                    b |= (z.InputBuffer[p++] & 0xff) << k; k += 8;
                                }

                                d = tp[tp_index_t_3 + 2] + (b & inflate_mask[e]);

                                b >>= (e); k -= (e);

                                // do the copy
                                m -= c;
                                if (q >= d)
                                {
                                    // offset before dest
                                    //  just copy
                                    r = q - d;
                                    if (q - r > 0 && 2 > (q - r))
                                    {
                                        s.window[q++] = s.window[r++]; // minimum count is three,
                                        s.window[q++] = s.window[r++]; // so unroll loop a little
                                        c -= 2;
                                    }
                                    else
                                    {
                                        Array.Copy(s.window, r, s.window, q, 2);
                                        q += 2; r += 2; c -= 2;
                                    }
                                }
                                else
                                {
                                    // else offset after destination
                                    r = q - d;
                                    do
                                    {
                                        r += s.end; // force pointer in window
                                    }
                                    while (r < 0); // covers invalid distances
                                    e = s.end - r;
                                    if (c > e)
                                    {
                                        // if source crosses,
                                        c -= e; // wrapped copy
                                        if (q - r > 0 && e > (q - r))
                                        {
                                            do
                                            {
                                                s.window[q++] = s.window[r++];
                                            }
                                            while (--e != 0);
                                        }
                                        else
                                        {
                                            Array.Copy(s.window, r, s.window, q, e);
                                            q += e; r += e; e = 0;
                                        }
                                        r = 0; // copy rest from start of window
                                    }
                                }

                                // copy all or what's left
                                if (q - r > 0 && c > (q - r))
                                {
                                    do
                                    {
                                        s.window[q++] = s.window[r++];
                                    }
                                    while (--c != 0);
                                }
                                else
                                {
                                    Array.Copy(s.window, r, s.window, q, c);
                                    q += c; r += c; c = 0;
                                }
                                break;
                            }
                            else if ((e & 64) == 0)
                            {
                                t += tp[tp_index_t_3 + 2];
                                t += (b & inflate_mask[e]);
                                tp_index_t_3 = (tp_index + t) * 3;
                                e = tp[tp_index_t_3];
                            }
                            else
                            {
                                z.Message = "invalid distance code";

                                c = z.AvailableBytesIn - n; c = (k >> 3) < c ? k >> 3 : c; n += c; p -= c; k -= (c << 3);

                                s.bitb = b; s.bitk = k;
                                z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                                s.write = q;

                                return ZlibConstants.Z_DATA_ERROR;
                            }
                        }
                        while (true);
                        break;
                    }

                    if ((e & 64) == 0)
                    {
                        t += tp[tp_index_t_3 + 2];
                        t += (b & inflate_mask[e]);
                        tp_index_t_3 = (tp_index + t) * 3;
                        if ((e = tp[tp_index_t_3]) == 0)
                        {

                            b >>= (tp[tp_index_t_3 + 1]); k -= (tp[tp_index_t_3 + 1]);

                            s.window[q++] = (byte)tp[tp_index_t_3 + 2];
                            m--;
                            break;
                        }
                    }
                    else if ((e & 32) != 0)
                    {

                        c = z.AvailableBytesIn - n; c = (k >> 3) < c ? k >> 3 : c; n += c; p -= c; k -= (c << 3);

                        s.bitb = b; s.bitk = k;
                        z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                        s.write = q;

                        return ZlibConstants.Z_STREAM_END;
                    }
                    else
                    {
                        z.Message = "invalid literal/length code";

                        c = z.AvailableBytesIn - n; c = (k >> 3) < c ? k >> 3 : c; n += c; p -= c; k -= (c << 3);

                        s.bitb = b; s.bitk = k;
                        z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
                        s.write = q;

                        return ZlibConstants.Z_DATA_ERROR;
                    }
                }
                while (true);
            }
            while (m >= 258 && n >= 10);

            // not enough input or output--restore pointers and return
            c = z.AvailableBytesIn - n; c = (k >> 3) < c ? k >> 3 : c; n += c; p -= c; k -= (c << 3);

            s.bitb = b; s.bitk = k;
            z.AvailableBytesIn = n; z.TotalBytesIn += p - z.NextIn; z.NextIn = p;
            s.write = q;

            return ZlibConstants.Z_OK;
        }
    }


    internal sealed class InflateManager
    {
        // preset dictionary flag in zlib header
        private const int PRESET_DICT = 0x20;

        private const int Z_DEFLATED = 8;

        private const int METHOD = 0; // waiting for method byte
        private const int FLAG = 1; // waiting for flag byte
        private const int DICT4 = 2; // four dictionary check bytes to go
        private const int DICT3 = 3; // three dictionary check bytes to go
        private const int DICT2 = 4; // two dictionary check bytes to go
        private const int DICT1 = 5; // one dictionary check byte to go
        private const int DICT0 = 6; // waiting for inflateSetDictionary
        private const int BLOCKS = 7; // decompressing blocks
        private const int CHECK4 = 8; // four check bytes to go
        private const int CHECK3 = 9; // three check bytes to go
        private const int CHECK2 = 10; // two check bytes to go
        private const int CHECK1 = 11; // one check byte to go
        private const int DONE = 12; // finished check, done
        private const int BAD = 13; // got an error--stay here

        internal int mode; // current inflate mode

        // mode dependent information
        internal int method; // if FLAGS, method byte

        // if CHECK, check values to compare
        internal long[] was = new long[1]; // computed check value
        internal long need; // stream check value

        // if BAD, inflateSync's marker bytes count
        internal int marker;

        // mode independent information
        //internal int nowrap; // flag for no wrapper
        private bool _handleRfc1950HeaderBytes = true;
        internal bool HandleRfc1950HeaderBytes
        {
            get { return _handleRfc1950HeaderBytes; }
            set { _handleRfc1950HeaderBytes = value; }
        }
        internal int wbits; // log2(window size)  (8..15, defaults to 15)

        internal InflateBlocks blocks; // current inflate_blocks state

        public InflateManager() { }

        public InflateManager(bool expectRfc1950HeaderBytes)
        {
            _handleRfc1950HeaderBytes = expectRfc1950HeaderBytes;
        }

        internal int Reset(ZlibCodec z)
        {
            if (z == null)
                throw new ZlibException("Codec is null.");

            if (z.istate == null)
                throw new ZlibException("InflateManager is null.");

            z.TotalBytesIn = z.TotalBytesOut = 0;
            z.Message = null;
            z.istate.mode = z.istate.HandleRfc1950HeaderBytes ? METHOD : BLOCKS;
            z.istate.blocks.Reset(z, null);
            return ZlibConstants.Z_OK;
        }

        internal int End(ZlibCodec z)
        {
            if (blocks != null)
                blocks.Free(z);
            blocks = null;
            //    ZFREE(z, z->state);
            return ZlibConstants.Z_OK;
        }

        internal int Initialize(ZlibCodec z, int w)
        {
            z.Message = null;
            blocks = null;

            // handle undocumented nowrap option (no zlib header or check)
            //nowrap = 0;
            //if (w < 0)
            //{
            //    w = - w;
            //    nowrap = 1;
            //}

            // set window size
            if (w < 8 || w > 15)
            {
                End(z);
                throw new ZlibException("Bad window size.");

                //return ZlibConstants.Z_STREAM_ERROR;
            }
            wbits = w;

            z.istate.blocks = new InflateBlocks(z,
                z.istate.HandleRfc1950HeaderBytes ? this : null,
                1 << w);

            // reset state
            Reset(z);
            return ZlibConstants.Z_OK;
        }

        internal int Inflate(ZlibCodec z, int f)
        {
            int r;
            int b;

            if (z == null)
                throw new ZlibException("Codec is null. ");
            if (z.istate == null)
                throw new ZlibException("InflateManager is null. ");
            if (z.InputBuffer == null)
                throw new ZlibException("InputBuffer is null. ");

            //return ZlibConstants.Z_STREAM_ERROR;

            f = (f == ZlibConstants.Z_FINISH)
                ? ZlibConstants.Z_BUF_ERROR
                : ZlibConstants.Z_OK;
            r = ZlibConstants.Z_BUF_ERROR;
            while (true)
            {
                switch (z.istate.mode)
                {
                    case METHOD:
                        if (z.AvailableBytesIn == 0)
                            return r;

                        r = f;

                        z.AvailableBytesIn--; z.TotalBytesIn++;

                        if (((z.istate.method = z.InputBuffer[z.NextIn++]) & 0xf) != Z_DEFLATED)
                        {
                            z.istate.mode = BAD;
                            z.Message = String.Format("unknown compression method (0x{0:X2})", z.istate.method);
                            z.istate.marker = 5; // can't try inflateSync
                            break;
                        }
                        if ((z.istate.method >> 4) + 8 > z.istate.wbits)
                        {
                            z.istate.mode = BAD;
                            z.Message = String.Format("invalid window size ({0})", (z.istate.method >> 4) + 8);
                            z.istate.marker = 5; // can't try inflateSync
                            break;
                        }
                        z.istate.mode = FLAG;
                        goto case FLAG;

                    case FLAG:

                        if (z.AvailableBytesIn == 0) return r;

                        r = f;

                        z.AvailableBytesIn--; z.TotalBytesIn++;
                        b = (z.InputBuffer[z.NextIn++]) & 0xff;

                        if ((((z.istate.method << 8) + b) % 31) != 0)
                        {
                            z.istate.mode = BAD;
                            z.Message = "incorrect header check";
                            z.istate.marker = 5; // can't try inflateSync
                            break;
                        }

                        if ((b & PRESET_DICT) == 0)
                        {
                            z.istate.mode = BLOCKS;
                            break;
                        }
                        z.istate.mode = DICT4;
                        goto case DICT4;

                    case DICT4:

                        if (z.AvailableBytesIn == 0) return r;

                        r = f;

                        z.AvailableBytesIn--; z.TotalBytesIn++;
                        z.istate.need = ((z.InputBuffer[z.NextIn++] & 0xff) << 24) & unchecked((int)0xff000000L);
                        z.istate.mode = DICT3;
                        goto case DICT3;

                    case DICT3:

                        if (z.AvailableBytesIn == 0) return r;

                        r = f;

                        z.AvailableBytesIn--; z.TotalBytesIn++;
                        z.istate.need += (((z.InputBuffer[z.NextIn++] & 0xff) << 16) & 0xff0000L);
                        z.istate.mode = DICT2;
                        goto case DICT2;

                    case DICT2:

                        if (z.AvailableBytesIn == 0) return r;

                        r = f;

                        z.AvailableBytesIn--; z.TotalBytesIn++;
                        z.istate.need += (((z.InputBuffer[z.NextIn++] & 0xff) << 8) & 0xff00L);
                        z.istate.mode = DICT1;
                        goto case DICT1;

                    case DICT1:

                        if (z.AvailableBytesIn == 0) return r;

                        r = f;

                        z.AvailableBytesIn--; z.TotalBytesIn++;
                        z.istate.need += (z.InputBuffer[z.NextIn++] & 0xffL);
                        z._Adler32 = z.istate.need;
                        z.istate.mode = DICT0;
                        return ZlibConstants.Z_NEED_DICT;

                    case DICT0:
                        z.istate.mode = BAD;
                        z.Message = "need dictionary";
                        z.istate.marker = 0; // can try inflateSync
                        return ZlibConstants.Z_STREAM_ERROR;

                    case BLOCKS:
                        r = z.istate.blocks.Process(z, r);
                        if (r == ZlibConstants.Z_DATA_ERROR)
                        {
                            z.istate.mode = BAD;
                            z.istate.marker = 0; // can try inflateSync
                            break;
                        }
                        if (r == ZlibConstants.Z_OK) r = f;

                        if (r != ZlibConstants.Z_STREAM_END) return r;

                        r = f;
                        z.istate.blocks.Reset(z, z.istate.was);
                        if (!z.istate.HandleRfc1950HeaderBytes)
                        {
                            z.istate.mode = DONE;
                            break;
                        }
                        z.istate.mode = CHECK4;
                        goto case CHECK4;

                    case CHECK4:

                        if (z.AvailableBytesIn == 0) return r;

                        r = f;

                        z.AvailableBytesIn--; z.TotalBytesIn++;
                        z.istate.need = ((z.InputBuffer[z.NextIn++] & 0xff) << 24) & unchecked((int)0xff000000L);
                        z.istate.mode = CHECK3;
                        goto case CHECK3;

                    case CHECK3:

                        if (z.AvailableBytesIn == 0) return r;

                        r = f;

                        z.AvailableBytesIn--; z.TotalBytesIn++;
                        z.istate.need += (((z.InputBuffer[z.NextIn++] & 0xff) << 16) & 0xff0000L);
                        z.istate.mode = CHECK2;
                        goto case CHECK2;

                    case CHECK2:

                        if (z.AvailableBytesIn == 0) return r;
                        r = f;

                        z.AvailableBytesIn--; z.TotalBytesIn++;
                        z.istate.need += (((z.InputBuffer[z.NextIn++] & 0xff) << 8) & 0xff00L);
                        z.istate.mode = CHECK1;
                        goto case CHECK1;

                    case CHECK1:

                        if (z.AvailableBytesIn == 0) return r;

                        r = f;

                        z.AvailableBytesIn--; z.TotalBytesIn++;
                        z.istate.need += (z.InputBuffer[z.NextIn++] & 0xffL);
                        unchecked
                        {
                            if (((int)(z.istate.was[0])) != ((int)(z.istate.need)))
                            {
                                z.istate.mode = BAD;
                                z.Message = "incorrect data check";
                                z.istate.marker = 5; // can't try inflateSync
                                break;
                            }
                        }
                        z.istate.mode = DONE;
                        goto case DONE;

                    case DONE:
                        return ZlibConstants.Z_STREAM_END;

                    case BAD:
                        throw new ZlibException(String.Format("Bad state ({0})", z.Message));
                    //return ZlibConstants.Z_DATA_ERROR;

                    default:
                        throw new ZlibException("Stream error.");
                    //return ZlibConstants.Z_STREAM_ERROR;

                }
            }
        }


        internal int SetDictionary(ZlibCodec z, byte[] dictionary)
        {
            int index = 0;
            int length = dictionary.Length;
            if (z == null || z.istate == null || z.istate.mode != DICT0)
                throw new ZlibException("Stream error.");

            if (Adler.Adler32(1L, dictionary, 0, dictionary.Length) != z._Adler32)
            {
                return ZlibConstants.Z_DATA_ERROR;
            }

            z._Adler32 = Adler.Adler32(0, null, 0, 0);

            if (length >= (1 << z.istate.wbits))
            {
                length = (1 << z.istate.wbits) - 1;
                index = dictionary.Length - length;
            }
            z.istate.blocks.SetDictionary(dictionary, index, length);
            z.istate.mode = BLOCKS;
            return ZlibConstants.Z_OK;
        }

        private static byte[] mark = new byte[] { 0, 0, 0xff, 0xff };

        internal int Sync(ZlibCodec z)
        {
            int n; // number of bytes to look at
            int p; // pointer to bytes
            int m; // number of marker bytes found in a row
            long r, w; // temporaries to save total_in and total_out

            // set up
            if (z == null || z.istate == null)
                return ZlibConstants.Z_STREAM_ERROR;
            if (z.istate.mode != BAD)
            {
                z.istate.mode = BAD;
                z.istate.marker = 0;
            }
            if ((n = z.AvailableBytesIn) == 0)
                return ZlibConstants.Z_BUF_ERROR;
            p = z.NextIn;
            m = z.istate.marker;

            // search
            while (n != 0 && m < 4)
            {
                if (z.InputBuffer[p] == mark[m])
                {
                    m++;
                }
                else if (z.InputBuffer[p] != 0)
                {
                    m = 0;
                }
                else
                {
                    m = 4 - m;
                }
                p++; n--;
            }

            // restore
            z.TotalBytesIn += p - z.NextIn;
            z.NextIn = p;
            z.AvailableBytesIn = n;
            z.istate.marker = m;

            // return no joy or set up to restart on a new block
            if (m != 4)
            {
                return ZlibConstants.Z_DATA_ERROR;
            }
            r = z.TotalBytesIn; w = z.TotalBytesOut;
            Reset(z);
            z.TotalBytesIn = r; z.TotalBytesOut = w;
            z.istate.mode = BLOCKS;
            return ZlibConstants.Z_OK;
        }

        // Returns true if inflate is currently at the end of a block generated
        // by Z_SYNC_FLUSH or Z_FULL_FLUSH. This function is used by one PPP
        // implementation to provide an additional safety check. PPP uses Z_SYNC_FLUSH
        // but removes the length bytes of the resulting empty stored block. When
        // decompressing, PPP checks that at the end of input packet, inflate is
        // waiting for these length bytes.
        internal int SyncPoint(ZlibCodec z)
        {
            if (z == null || z.istate == null || z.istate.blocks == null)
                return ZlibConstants.Z_STREAM_ERROR;
            return z.istate.blocks.SyncPoint();
        }
    }
}