using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace wyUpdate.Common
{
    public static partial class ReadFiles
    {
        //Image (for pre- 1.0 RC2 wyUpdate Client files)
        public static Image ReadImage(Stream fs)
        {
            byte[] tempBytes;
            byte[] tempLength = new byte[4];

            Image tempImg = null;

            //skip the "length of data" int value
            fs.Position += 4;

            fs.Read(tempLength, 0, 4);
            tempBytes = new byte[BitConverter.ToInt32(tempLength, 0)];

            ReadWholeArray(fs, tempBytes);

            try
            {
                using (MemoryStream ms = new MemoryStream(tempBytes, 0, tempBytes.Length))
                {
                    tempImg = Image.FromStream(ms, true);
                }
            }
            catch (Exception) { return null; /* blank image */ }

            //copy the bitmap to be fully "in memory", so it's not referenced to the filestream 
            //(see http://support.microsoft.com/?id=814675 )
            Bitmap tempBitmap = new Bitmap(tempImg, tempImg.Size);
            Bitmap newBitmap = new Bitmap(tempBitmap.Width, tempBitmap.Height, tempBitmap.PixelFormat);

            Rectangle rect = new Rectangle(new Point(0, 0), tempBitmap.Size);

            //lock the two bitmaps
            System.Drawing.Imaging.BitmapData origData =
                tempBitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                tempBitmap.PixelFormat);

            System.Drawing.Imaging.BitmapData newData =
                newBitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                newBitmap.PixelFormat);

            //calculate the size of the bitmap in memory
            int imgSize = BitmapArraySize(tempBitmap.Width, tempBitmap.Height, tempBitmap.PixelFormat);
            tempBytes = new byte[imgSize];

            //copy the old "file referenced" bitmap to the new non-ref bitmap
            System.Runtime.InteropServices.Marshal.Copy(origData.Scan0, tempBytes, 0, imgSize);
            System.Runtime.InteropServices.Marshal.Copy(tempBytes, 0, newData.Scan0, imgSize);

            //unlock the bits
            tempBitmap.UnlockBits(origData);
            newBitmap.UnlockBits(origData);

            //destroy reference to the old bitmap
            tempBitmap.Dispose();

            return newBitmap;
        }

        // Finds the size of a Bitmap in memory given its pixel format
        private static int BitmapArraySize(int width, int height, System.Drawing.Imaging.PixelFormat pixFormat)
        {
            switch (pixFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Extended:
                case System.Drawing.Imaging.PixelFormat.Max:
                case System.Drawing.Imaging.PixelFormat.Format64bppArgb:
                case System.Drawing.Imaging.PixelFormat.Format64bppPArgb:
                    return width * height * 8;
                case System.Drawing.Imaging.PixelFormat.Format48bppRgb:
                    return width * height * 6;
                case System.Drawing.Imaging.PixelFormat.Canonical:
                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                case System.Drawing.Imaging.PixelFormat.Format32bppPArgb:
                case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                case System.Drawing.Imaging.PixelFormat.Alpha:
                case System.Drawing.Imaging.PixelFormat.PAlpha:
                case System.Drawing.Imaging.PixelFormat.Indexed:
                    return width * height * 4;
                case System.Drawing.Imaging.PixelFormat.Gdi:
                case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                    return width * height * 3;
                case System.Drawing.Imaging.PixelFormat.Format16bppArgb1555:
                case System.Drawing.Imaging.PixelFormat.Format16bppGrayScale:
                case System.Drawing.Imaging.PixelFormat.Format16bppRgb555:
                case System.Drawing.Imaging.PixelFormat.Format16bppRgb565:
                    return width * height * 2;
                case System.Drawing.Imaging.PixelFormat.Format8bppIndexed:
                    return width * height;
                case System.Drawing.Imaging.PixelFormat.Format4bppIndexed:
                    return (width * height) / 2;
                case System.Drawing.Imaging.PixelFormat.Format1bppIndexed:
                    return (width * height) / 8;
                case System.Drawing.Imaging.PixelFormat.Undefined:
                default:
                    return -1;
            }
        }
    }
}
