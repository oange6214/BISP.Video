using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace BISP.Wpf.Mvvm.Helpers;

internal static class BitmapHelpers
{
    /// <summary>
    /// Normal Converter
    /// </summary>
    /// <param name="bitmap"></param>
    /// <returns></returns>
    public static BitmapImage ToBitmapImage(this Bitmap bitmap)
    {
        using MemoryStream memory = new MemoryStream();

        bitmap.Save(memory, ImageFormat.Png);
        memory.Position = 0;

        BitmapImage bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = memory;
        bitmapImage.EndInit();
        bitmapImage.Freeze(); // Optimize for multi-threaded access

        return bitmapImage;
    }

    /// <summary>
    /// High performance Converter
    /// </summary>
    /// <param name="bitmap"></param>
    /// <returns></returns>
    public static BitmapSource ToBitmapSource(this System.Drawing.Bitmap bitmap)
    {
        IntPtr hBitmap = bitmap.GetHbitmap();
        try
        {
            return Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);
}