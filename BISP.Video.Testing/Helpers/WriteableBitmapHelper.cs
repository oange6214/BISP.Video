using System.IO;
using System.Windows.Media.Imaging;

namespace BISP.Video.Testing.Helpers;

public class WriteableBitmapHelper
{
    public static void BitmapCopyToWriteableBitmap(System.Drawing.Bitmap src, WriteableBitmap dst, System.Drawing.Rectangle srcRect, int destX, int destY, System.Drawing.Imaging.PixelFormat srcPixelFormat)
    {
        var data = src.LockBits(new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), src.Size), System.Drawing.Imaging.ImageLockMode.ReadOnly, srcPixelFormat);
        try
        {
            dst.Lock();
            try
            {
                unsafe
                {
                    int srcStride = data.Stride;
                    int dstStride = dst.BackBufferStride;
                    int bytesPerPixel = srcPixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb ? 4 : 3;
                    int copyWidth = srcRect.Width * bytesPerPixel;

                    byte* srcPtr = (byte*)data.Scan0 + (srcRect.Y * srcStride) + (srcRect.X * bytesPerPixel);
                    byte* dstPtr = (byte*)dst.BackBuffer + (destY * dstStride) + (destX * 4);

                    if (srcRect.Height > 1 && srcRect.Width > 1000)
                    {
                        Parallel.For(0, srcRect.Height, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, y =>
                        {
                            byte* srcRow = srcPtr + (y * srcStride);
                            byte* dstRow = dstPtr + (y * dstStride);
                            Buffer.MemoryCopy(srcRow, dstRow, copyWidth, copyWidth);
                        });
                    }
                    else
                    {
                        for (int y = 0; y < srcRect.Height; y++)
                        {
                            Buffer.MemoryCopy(srcPtr, dstPtr, copyWidth, copyWidth);
                            srcPtr += srcStride;
                            dstPtr += dstStride;
                        }
                    }
                }
                dst.AddDirtyRect(new System.Windows.Int32Rect(destX, destY, srcRect.Width, srcRect.Height));
            }
            finally
            {
                dst.Unlock();
            }
        }
        finally
        {
            src.UnlockBits(data);
        }
    }

    public static WriteableBitmap BitmapToWriteablBitmap(System.Drawing.Bitmap src)
    {
        var wb = CreateCompatibleWriteableBitmap(src);

        BitmapCopyToWriteableBitmap(src, wb, new System.Drawing.Rectangle(0, 0, src.Width, src.Height), 0, 0, src.PixelFormat);
        return wb;
    }

    public static BitmapImage ConvertWriteableBitmapToBitmapImage(WriteableBitmap wbm)
    {
        using var stream = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(wbm));
        encoder.Save(stream);

        var bmImage = new BitmapImage();
        bmImage.BeginInit();
        bmImage.CacheOption = BitmapCacheOption.OnLoad;
        bmImage.StreamSource = stream;
        bmImage.EndInit();
        bmImage.Freeze();

        return bmImage;
    }

    public static WriteableBitmap CreateCompatibleWriteableBitmap(System.Drawing.Bitmap src)
    {
        System.Windows.Media.PixelFormat format;

        switch (src.PixelFormat)
        {
            case System.Drawing.Imaging.PixelFormat.Format16bppRgb555:
                format = System.Windows.Media.PixelFormats.Bgr555;
                break;

            case System.Drawing.Imaging.PixelFormat.Format16bppRgb565:
                format = System.Windows.Media.PixelFormats.Bgr565;
                break;

            case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                format = System.Windows.Media.PixelFormats.Bgr24;
                break;

            case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                format = System.Windows.Media.PixelFormats.Bgr32;
                break;

            case System.Drawing.Imaging.PixelFormat.Format32bppPArgb:
                format = System.Windows.Media.PixelFormats.Pbgra32;
                break;

            case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                format = System.Windows.Media.PixelFormats.Bgra32;
                break;

            default:
                return null;
        }

        return new WriteableBitmap(src.Width, src.Height, 0, 0, format, null);
    }
}