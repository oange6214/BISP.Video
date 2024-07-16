using System.Collections.Concurrent;
using System.Drawing;

namespace BISP.Video;

/// <summary>
/// Proxy video source for asynchronous processing of another nested video source.
/// </summary>
public class AsyncVideoSource : IVideoSource
{
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly ConcurrentQueue<Bitmap> _frameQueue = new ConcurrentQueue<Bitmap>();
    private readonly IVideoSource _nestedVideoSource;
    private readonly Task _processingTask;

    private int framesProcessed;
    private bool skipFramesIfBusy;

    public AsyncVideoSource(IVideoSource nestedVideoSource)
    {
        this._nestedVideoSource = nestedVideoSource;
        _processingTask = Task.Run(ProcessFramesAsync, _cancellationTokenSource.Token);
    }

    public AsyncVideoSource(IVideoSource nestedVideoSource, bool skipFramesIfBusy) : this(nestedVideoSource)
    {
        this.skipFramesIfBusy = skipFramesIfBusy;
    }

    public event NewFrameEventHandler NewFrame;

    public event PlayingFinishedEventHandler PlayingFinished
    {
        add { _nestedVideoSource.PlayingFinished += value; }
        remove { _nestedVideoSource.PlayingFinished -= value; }
    }

    public event VideoSourceErrorEventHandler VideoSourceError
    {
        add { _nestedVideoSource.VideoSourceError += value; }
        remove { _nestedVideoSource.VideoSourceError -= value; }
    }

    public long BytesReceived => _nestedVideoSource.BytesReceived;

    public int FramesProcessed
    {
        get
        {
            int frames = framesProcessed;
            framesProcessed = 0;
            return frames;
        }
    }

    public int FramesReceived => _nestedVideoSource.FramesReceived;
    public bool IsRunning => _nestedVideoSource.IsRunning;
    public IVideoSource NestedVideoSource => _nestedVideoSource;

    public bool SkipFramesIfBusy
    {
        get => skipFramesIfBusy;
        set => skipFramesIfBusy = value;
    }

    public string Source => _nestedVideoSource.Source;

    public void SignalToStop()
    {
        _nestedVideoSource.SignalToStop();
        _cancellationTokenSource.Cancel();
    }

    public void Start()
    {
        if (!IsRunning)
        {
            framesProcessed = 0;
            _nestedVideoSource.NewFrame += NestedVideoSource_NewFrame;
            _nestedVideoSource.Start();
        }
    }

    public void Stop()
    {
        _nestedVideoSource.Stop();
        Free();
    }

    public void WaitForStop()
    {
        _nestedVideoSource.WaitForStop();
        Free();
    }

    private static Bitmap CloneImage(Bitmap source)
    {
        var destination = new Bitmap(source.Width, source.Height, source.PixelFormat);
        using (var g = Graphics.FromImage(destination))
        {
            g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height));
        }
        return destination;
    }

    private void Free()
    {
        _nestedVideoSource.NewFrame -= NestedVideoSource_NewFrame;
        _cancellationTokenSource.Cancel();
        _processingTask.Wait();
    }

    private void NestedVideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
    {
        if (NewFrame == null) return;

        var clonedFrame = CloneImage(eventArgs.Frame);

        if (skipFramesIfBusy && _frameQueue.Count > 0)
        {
            clonedFrame.Dispose();
            return;
        }

        _frameQueue.Enqueue(clonedFrame);
    }

    private async Task ProcessFramesAsync()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            if (_frameQueue.TryDequeue(out var frame))
            {
                NewFrame?.Invoke(this, new NewFrameEventArgs(frame));
                frame.Dispose();
                Interlocked.Increment(ref framesProcessed);
            }

            await Task.Delay(1); // Small delay to prevent busy waiting
        }
    }
}