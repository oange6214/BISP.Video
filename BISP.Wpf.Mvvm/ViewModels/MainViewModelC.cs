using BISP.Video;
using BISP.Video.DirectShow;
using BISP.Wpf.Mvvm.Helpers;
using BISP.Wpf.Mvvm.Toolkit.Mvvm;
using BISP.Wpf.Mvvm.Toolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BISP.Wpf.Mvvm.ViewModels;

public class MainViewModelC : ObservableObject
{
    #region Fields

    private readonly byte[] _empty4KBitmapArray = new byte[3840 * 2160 * 4];
    private FilterInfo _currentDevice;
    private double _currentFPS;
    private FpsHelper _fpsHelper;
    private IRelayCommand _startCommand;
    private IRelayCommand _stopCommand;
    private WriteableBitmap _videoPlayer;
    private IVideoSource _videoSource;
    private IRelayCommand _windowClosingCommand;
    private bool _isRendering;

    #endregion Fields

    public MainViewModelC()
    {
        _fpsHelper = new FpsHelper();
        GetVideoDevices();
        _videoPlayer = new WriteableBitmap(1280, 720, 96.0, 96.0, PixelFormats.Pbgra32, null);

        CompositionTarget.Rendering += CompositionTarget_Rendering;
    }

    #region Properties

    public FilterInfo CurrentDevice
    {
        get => _currentDevice;
        set => SetProperty(ref _currentDevice, value);
    }

    public double CurrentFPS
    {
        get => _currentFPS;
        set => SetProperty(ref _currentFPS, value);
    }

    public IRelayCommand StartCommand => _startCommand ??= new RelayCommand(Start);
    public IRelayCommand StopCommand => _stopCommand ??= new RelayCommand(Stop);
    public ObservableCollection<FilterInfo> VideoDevices { get; set; }

    public WriteableBitmap VideoPlayer
    {
        get => _videoPlayer;
        set => SetProperty(ref _videoPlayer, value);
    }

    public IRelayCommand WindowClosingCommand => _windowClosingCommand ??= new RelayCommand(CloseWindow);

    #endregion Properties

    #region Private Methods

    [DllImport("kernel32.dll")]
    private static extern void CopyMemory(IntPtr destination, IntPtr source, uint length);

    private void CloseWindow()
    {
        StopCamera();
    }

    private void GetVideoDevices()
    {
        VideoDevices = new ObservableCollection<FilterInfo>();

        foreach (FilterInfo filterInfo in new FilterInfoCollection(FilterCategory.VideoInputDevice))
        {
            VideoDevices.Add(filterInfo);
        }

        if (VideoDevices.Any())
        {
            CurrentDevice = VideoDevices[0];
        }
        else
        {
            MessageBox.Show("No video sources found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ProcessVideoFrame(Bitmap frame)
    {
        var rect = new System.Drawing.Rectangle(0, 0, frame.Width, frame.Height);
        var bitmapData = frame.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        Application.Current.Dispatcher.Invoke(() =>
        {
            _videoPlayer.WritePixels(new Int32Rect(0, 0, frame.Width, frame.Height), bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
        });

        frame.UnlockBits(bitmapData);
    }

    private unsafe void CompositionTarget_Rendering(object sender, EventArgs e)
    {
        try
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                fixed (byte* ptr = _empty4KBitmapArray)
                {
                    var p = new IntPtr(ptr);
                    CopyMemory(_videoPlayer.BackBuffer, new IntPtr(ptr), (uint)_empty4KBitmapArray.Length);
                }

                _videoPlayer.Lock();
                _videoPlayer.AddDirtyRect(new Int32Rect(0, 0, _videoPlayer.PixelWidth, _videoPlayer.PixelHeight));
                _videoPlayer.Unlock();
            }
        }
        finally
        {
        }
    }

    private unsafe void RenderFrame()
    {
        try
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                fixed (byte* ptr = _empty4KBitmapArray)
                {
                    var p = new IntPtr(ptr);
                    CopyMemory(_videoPlayer.BackBuffer, new IntPtr(ptr), (uint)_empty4KBitmapArray.Length);
                }

                _videoPlayer.AddDirtyRect(new Int32Rect(0, 0, _videoPlayer.PixelWidth, _videoPlayer.PixelHeight));
            }
        }
        catch (Exception)
        {
        }
    }

    private void Start()
    {
        StartCamera();
    }

    private void StartCamera()
    {
        if (CurrentDevice != null)
        {
            _videoSource = new VideoCaptureDevice(CurrentDevice.MonikerString);
            _videoSource.NewFrame += Video_NewFrame;
            _videoSource.Start();
        }
    }

    private void Stop()
    {
        StopCamera();
    }

    private void StopCamera()
    {
        if (_videoSource != null && _videoSource.IsRunning)
        {
            _videoSource.SignalToStop();
            _videoSource.NewFrame -= Video_NewFrame;
        }
    }

    private void Video_NewFrame(object sender, NewFrameEventArgs eventArgs)
    {
        try
        {
            using Bitmap frame = (Bitmap)eventArgs.Frame.Clone();

            ProcessVideoFrame(frame);
        }
        catch (Exception exc)
        {
            MessageBox.Show($"Error on Video_NewFrame:\n{exc.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StopCamera();
        }
    }

    #endregion Private Methods
}