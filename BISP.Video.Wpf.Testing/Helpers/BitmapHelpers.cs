using System.Runtime.InteropServices;

namespace BISP.Video.Wpf.Testing.Helpers;

internal static class BitmapHelpers
{
    ///// <summary>
    ///// Normal Converter
    ///// </summary>
    ///// <param name="bitmap"></param>
    ///// <returns></returns>
    //public static BitmapImage ToBitmapImage(this Bitmap bitmap)
    //{
    //    using MemoryStream memory = new MemoryStream();

    //    bitmap.Save(memory, ImageFormat.Png);
    //    memory.Position = 0;

    //    BitmapImage bitmapImage = new();
    //    bitmapImage.BeginInit();
    //    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
    //    bitmapImage.StreamSource = memory;
    //    bitmapImage.EndInit();
    //    bitmapImage.Freeze(); // Optimize for multi-threaded access

    //    return bitmapImage;
    //}

    ///// <summary>
    ///// High performance Converter
    ///// </summary>
    ///// <param name="bitmap"></param>
    ///// <returns></returns>
    //public static BitmapSource ToBitmapSource(this Bitmap bitmap)
    //{
    //    IntPtr hBitmap = bitmap.GetHbitmap();
    //    try
    //    {
    //        return Imaging.CreateBitmapSourceFromHBitmap(
    //            hBitmap,
    //            IntPtr.Zero,
    //            Int32Rect.Empty,
    //            BitmapSizeOptions.FromEmptyOptions());
    //    }
    //    finally
    //    {
    //        DeleteObject(hBitmap);
    //    }
    //}

    //public static BitmapSource ToBitmapSourceMarshal(this Bitmap bitmap)
    //{
    //    BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

    //    try
    //    {
    //        IntPtr ptr = bmpData.Scan0;
    //        int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
    //        byte[] rgbValues = new byte[bytes];
    //        Marshal.Copy(ptr, rgbValues, 0, bytes);

    //        return BitmapSource.Create(
    //            bitmap.Width,
    //            bitmap.Height,
    //            bitmap.HorizontalResolution,
    //            bitmap.VerticalResolution,
    //            PixelFormats.Bgra32,
    //            null,
    //            rgbValues,
    //            bmpData.Stride);
    //    }
    //    finally
    //    {
    //        bitmap.UnlockBits(bmpData);
    //    }
    //}

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);
}