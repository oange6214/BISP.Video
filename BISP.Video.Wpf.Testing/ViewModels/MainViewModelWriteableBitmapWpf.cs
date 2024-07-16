using BISP.Video.DirectShow.Wpf;
using BISP.Video.Wpf;
using BISP.Video.Wpf.Testing.Helpers;
using BISP.Video.Wpf.Testing.Toolkit.Mvvm;
using BISP.Video.Wpf.Testing.Toolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BISP.Video.Wpf.Testing.ViewModels;

public class MainViewModelWriteableBitmapWpf : ObservableObject
{
    #region Fields

    private FilterInfo _currentDevice;
    private double _currentFPS;
    private FpsHelper _fpsHelper;
    private IRelayCommand _startCommand;
    private IRelayCommand _stopCommand;
    private BitmapSource _videoPlayer;
    private IVideoSource _videoSource;
    private IRelayCommand _windowClosingCommand;

    #endregion Fields

    public MainViewModelWriteableBitmapWpf()
    {
        _fpsHelper = new FpsHelper();
        GetVideoDevices();
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

    public BitmapSource VideoPlayer
    {
        get => _videoPlayer;
        set => SetProperty(ref _videoPlayer, value);
    }

    public IRelayCommand WindowClosingCommand => _windowClosingCommand ??= new RelayCommand(CloseWindow);

    #endregion Properties

    #region Private Methods

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

    private void ProcessVideoFrame(NewFrameEventArgs eventArgs)
    {
        BitmapSource frozenBitmap = CreateFrozenBitmap(eventArgs.Frame);

        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            VideoPlayer = frozenBitmap;
            CurrentFPS = _fpsHelper.UpdateFPS();
        }));
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
            ProcessVideoFrame(eventArgs);
        }
        catch (Exception exc)
        {
            MessageBox.Show("Error on Video_NewFrame:\n" + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StopCamera();
        }
    }

    #endregion Private Methods
}