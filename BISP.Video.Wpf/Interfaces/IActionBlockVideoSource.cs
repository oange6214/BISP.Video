using System.Reactive;
using System.Threading.Tasks.Dataflow;

namespace BISP.Video.Wpf.Interfaces;

public interface IActionBlockVideoSource : IDisposable
{
    /// <summary>
    /// Observable stream of video frames.
    /// </summary>
    ActionBlock<IntPtr> FrameProcessor { get; set; }

    /// <summary>
    /// Observable stream of video source errors.
    /// </summary>
    ActionBlock<VideoSourceErrorEventArgs> ErrorProcessor { get; set; }

    /// <summary>
    /// Observable that completes when video playing is finished.
    /// </summary>
    ActionBlock<ReasonToFinishPlaying> PlayingFinishedProcessor { get; set; }

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