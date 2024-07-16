using System.Collections.Concurrent;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BISP.Video.Wpf;

/// <summary>
/// Proxy video source for asynchronous processing of another nested video source.
/// </summary>
public class AsyncVideoSource : IVideoSource
{
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly ConcurrentQueue<WriteableBitmap> _frameQueue = new ConcurrentQueue<WriteableBitmap>();
    private readonly IVideoSource _nestedVideoSource;
    private readonly Task _processingTask;
    private readonly object _lockObject = new object();

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

    private static WriteableBitmap CloneImage(WriteableBitmap source)
    {
        var destination = new WriteableBitmap(source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY, source.Format, null);

        int stride = source.BackBufferStride;
        int bufferSize = stride * source.PixelHeight;

        source.Lock();
        destination.Lock();

        unsafe
        {
            Buffer.MemoryCopy(
                source.BackBuffer.ToPointer(),
                destination.BackBuffer.ToPointer(),
                bufferSize,
                bufferSize);
        }

        destination.AddDirtyRect(new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight));

        destination.Unlock();
        source.Unlock();

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

        lock (_lockObject)
        {
            if (skipFramesIfBusy && _frameQueue.Count > 0)
            {
                return; // Skip this frame
            }

            var clonedFrame = CloneImage(eventArgs.Frame);
            _frameQueue.Enqueue(clonedFrame);
        }
    }

    private async Task ProcessFramesAsync()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            if (_frameQueue.TryDequeue(out var frame))
            {
                try
                {
                    NewFrame?.Invoke(this, new NewFrameEventArgs(frame));
                    Interlocked.Increment(ref framesProcessed);
                }
                finally
                {
                }
            }
            else
            {
                await Task.Delay(1, _cancellationTokenSource.Token);
            }
        }
    }
}