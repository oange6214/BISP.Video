﻿using BISP.Video.DirectShow.Wpf;
using BISP.Video.Wpf.Interfaces;
using BISP.Video.Wpf.Testing.Helpers;
using BISP.Video.Wpf.Testing.Toolkit.Mvvm;
using BISP.Video.Wpf.Testing.Toolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BISP.Video.Wpf.Testing.ViewModels;

public class MainViewModelWriteableBitmapWpfActionBlock : ObservableObject
{
    #region Fields

    private FilterInfo _currentDevice;
    private double _currentFPS;
    private IDisposable _errorSubscription;
    private FpsHelper _fpsHelper;
    private IDisposable _frameSubscription;
    private IDisposable _playingFinishedSubscription;

    //private WriteableBitmap VideoPlayer;
    private IActionBlockVideoSource _videoSource;

    private int _width;
    private int _height;
    private int _srcStride;
    private int _dstStride;

    #endregion Fields

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

    public ObservableCollection<FilterInfo> VideoDevices { get; set; }

    public WriteableBitmap VideoPlayer { get; set; }

    #endregion Properties

    #region Ctors

    public MainViewModelWriteableBitmapWpfActionBlock()
    {
        _fpsHelper = new FpsHelper();
        GetVideoDevices();

        ResetVideoPlayer();
    }

    #endregion Ctors

    #region Commands

    private IRelayCommand _windowClosingCommand;
    private IRelayCommand _startCommand;
    private IRelayCommand _stopCommand;
    public IRelayCommand StartCommand => _startCommand ??= new RelayCommand(Start);

    public IRelayCommand StopCommand => _stopCommand ??= new RelayCommand(Stop);
    public IRelayCommand WindowClosingCommand => _windowClosingCommand ??= new RelayCommand(CloseWindow);

    #endregion Commands

    #region Private Methods

    private void CloseWindow()
    {
        StopCamera();
    }

    private BitmapSource CreateFrozenBitmap(WriteableBitmap source)
    {
        BitmapSource bitmapSource = BitmapSource.Create(
            source.PixelWidth,
            source.PixelHeight,
            source.DpiX,
            source.DpiY,
            source.Format,
            source.Palette,
            source.BackBuffer,
            source.BackBufferStride * source.PixelHeight,
            source.BackBufferStride
        );
        bitmapSource.Freeze();
        return bitmapSource;
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

    private void HandlePlayingFinished(ReasonToFinishPlaying reason)
    {
        Debug.WriteLine($"Video playing finished. Reason: {reason}");
    }

    private void HandleVideoError(VideoSourceErrorEventArgs error)
    {
        Debug.WriteLine($"Video source error: {error.Description}");
    }

    private void Start()
    {
        StartCamera();
    }

    private void StartCamera()
    {
        if (CurrentDevice != null)
        {
            _videoSource = new ActionBlockCaptureDevice(CurrentDevice.MonikerString);

            // 初始化 ActionBlock
            _videoSource.FrameProcessor = new ActionBlock<IntPtr>(VideoNewFrame, new ExecutionDataflowBlockOptions { BoundedCapacity = 2 });

            _videoSource.ErrorProcessor = new ActionBlock<VideoSourceErrorEventArgs>(HandleVideoError);

            _videoSource.PlayingFinishedProcessor = new ActionBlock<ReasonToFinishPlaying>(HandlePlayingFinished);

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
            _videoSource.WaitForStop().Wait();

            _frameSubscription?.Dispose();
            _errorSubscription?.Dispose();
            _playingFinishedSubscription?.Dispose();

            _videoSource.Dispose();
            _videoSource = null;

            ResetVideoPlayer();

            CurrentFPS = 0;
        }
    }

    private void ResetVideoPlayer()
    {
        _width = 1920;
        _height = 1080;
        _srcStride = _width * 3;
        VideoPlayer = new WriteableBitmap(_width, _height, 96, 96, PixelFormats.Bgr24, null);
        _dstStride = VideoPlayer.BackBufferStride;
    }

    public void VideoNewFrame(IntPtr newFrame)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ProcessFrame(newFrame);
        });
    }

    private void ProcessFrame2(IntPtr newFrame)
    {
        try
        {
            VideoPlayer.Lock();

            unsafe
            {
                byte* srcPtr = (byte*)newFrame.ToPointer();
                byte* dstPtr = (byte*)VideoPlayer.BackBuffer.ToPointer();

                Parallel.For(0, _height, y =>
                {
                    byte* srcLine = srcPtr + y * _srcStride;
                    byte* dstLine = dstPtr + (_height - 1 - y) * _dstStride;

                    Buffer.MemoryCopy(srcLine, dstLine, _dstStride, _srcStride);
                });
            }

            VideoPlayer.AddDirtyRect(new Int32Rect(0, 0, _width, _height));
        }
        finally
        {
            VideoPlayer.Unlock();
            CurrentFPS = _fpsHelper.UpdateFPS();
        }
    }

    private void ProcessFrame(IntPtr newFrame)
    {
        try
        {
            VideoPlayer.Lock();

            for (int y = 0; y < _height; y++)
            {
                IntPtr srcLine = IntPtr.Add(newFrame, y * _srcStride);
                int dstY = _height - 1 - y;

                try
                {
                    VideoPlayer.WritePixels(
                        new Int32Rect(0, dstY, _width, 1),
                        srcLine,
                        _srcStride,
                        _srcStride
                    );
                }
                catch
                {
                }
            }

            VideoPlayer.AddDirtyRect(new Int32Rect(0, 0, _width, _height));
        }
        finally
        {
            VideoPlayer.Unlock();
            CurrentFPS = _fpsHelper.UpdateFPS();
        }
    }

    #endregion Private Methods
}