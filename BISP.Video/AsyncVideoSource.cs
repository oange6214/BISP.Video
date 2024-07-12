using BISP.Video.Core;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace BISP.Video;

/// <summary>
/// Proxy video source for asynchronous processing of another nested video source.
/// </summary>
public class AsyncVideoSource : IVideoSource
{
    private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private readonly ConcurrentQueue<Bitmap> frameQueue = new ConcurrentQueue<Bitmap>();
    private readonly IVideoSource nestedVideoSource;
    private readonly Task processingTask;

    private int framesProcessed;
    private bool skipFramesIfBusy;

    public AsyncVideoSource(IVideoSource nestedVideoSource)
    {
        this.nestedVideoSource = nestedVideoSource;
        processingTask = Task.Run(ProcessFramesAsync, cancellationTokenSource.Token);
    }

    public AsyncVideoSource(IVideoSource nestedVideoSource, bool skipFramesIfBusy) : this(nestedVideoSource)
    {
        this.skipFramesIfBusy = skipFramesIfBusy;
    }

    public event NewFrameEventHandler NewFrame;

    public event PlayingFinishedEventHandler PlayingFinished
    {
        add { nestedVideoSource.PlayingFinished += value; }
        remove { nestedVideoSource.PlayingFinished -= value; }
    }

    public event VideoSourceErrorEventHandler VideoSourceError
    {
        add { nestedVideoSource.VideoSourceError += value; }
        remove { nestedVideoSource.VideoSourceError -= value; }
    }

    public long BytesReceived => nestedVideoSource.BytesReceived;

    public int FramesProcessed
    {
        get
        {
            int frames = framesProcessed;
            framesProcessed = 0;
            return frames;
        }
    }

    public int FramesReceived => nestedVideoSource.FramesReceived;
    public bool IsRunning => nestedVideoSource.IsRunning;
    public IVideoSource NestedVideoSource => nestedVideoSource;

    public bool SkipFramesIfBusy
    {
        get => skipFramesIfBusy;
        set => skipFramesIfBusy = value;
    }

    public string Source => nestedVideoSource.Source;

    public void SignalToStop()
    {
        nestedVideoSource.SignalToStop();
        cancellationTokenSource.Cancel();
    }

    public void Start()
    {
        if (!IsRunning)
        {
            framesProcessed = 0;
            nestedVideoSource.NewFrame += NestedVideoSource_NewFrame;
            nestedVideoSource.Start();
        }
    }

    public void Stop()
    {
        nestedVideoSource.Stop();
        Free();
    }

    public void WaitForStop()
    {
        nestedVideoSource.WaitForStop();
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
        nestedVideoSource.NewFrame -= NestedVideoSource_NewFrame;
        cancellationTokenSource.Cancel();
        processingTask.Wait();
    }

    private void NestedVideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
    {
        if (NewFrame == null) return;

        var clonedFrame = CloneImage(eventArgs.Frame);

        if (skipFramesIfBusy && frameQueue.Count > 0)
        {
            clonedFrame.Dispose();
            return;
        }

        frameQueue.Enqueue(clonedFrame);
    }

    private async Task ProcessFramesAsync()
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            if (frameQueue.TryDequeue(out var frame))
            {
                NewFrame?.Invoke(this, new NewFrameEventArgs(frame));
                frame.Dispose();
                Interlocked.Increment(ref framesProcessed);
            }

            await Task.Delay(1); // Small delay to prevent busy waiting
        }
    }
}