using System.Reactive;
using System.Windows.Media.Imaging;

namespace BISP.Video.Wpf.Interfaces;

public interface IObservableVideoSource : IDisposable
{
    /// <summary>
    /// Observable stream of video frames.
    /// </summary>
    IObservable<IntPtr> FrameStream { get; }

    /// <summary>
    /// Observable stream of video source errors.
    /// </summary>
    IObservable<VideoSourceErrorEventArgs> ErrorStream { get; }

    /// <summary>
    /// Observable that completes when video playing is finished.
    /// </summary>
    IObservable<ReasonToFinishPlaying> PlayingFinishedStream { get; }

    /// <summary>
    /// Video source.
    /// </summary>
    string Source { get; }

    /// <summary>
    /// Received frames count.
    /// </summary>
    int FramesReceived { get; }

    /// <summary>
    /// Received bytes count.
    /// </summary>
    long BytesReceived { get; }

    /// <summary>
    /// State of the video source.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Start video source.
    /// </summary>
    void Start();

    /// <summary>
    /// Signal video source to stop its work.
    /// </summary>
    void SignalToStop();

    /// <summary>
    /// Wait for video source has stopped.
    /// </summary>
    IObservable<Unit> WaitForStop();

    /// <summary>
    /// Stop video source.
    /// </summary>
    void Stop();
}