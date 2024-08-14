using System.Threading.Channels;
using System.Windows.Media.Imaging;

namespace BISP.Video.Wpf.Interfaces;

public interface IChannelVideoSource : IDisposable
{
    ChannelReader<WriteableBitmap> GetFrameReader();

    ChannelReader<VideoSourceErrorEventArgs> GetErrorReader();

    ChannelReader<ReasonToFinishPlaying> GetPlayingFinishedReader();

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
    void WaitForStop();

    /// <summary>
    /// Stop video source.
    /// </summary>
    void Stop();
}