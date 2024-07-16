namespace BISP.Video.Wpf.Testing.Helpers;

public class FpsHelper
{
    private int _frameCount;
    private DateTime _lastFpsUpdateTime;

    public double CurrentFPS { get; private set; }

    public double UpdateFPS()
    {
        _frameCount++;
        var now = DateTime.UtcNow;
        if ((now - _lastFpsUpdateTime).TotalSeconds >= 1)
        {
            CurrentFPS = _frameCount / (now - _lastFpsUpdateTime).TotalSeconds;
            _lastFpsUpdateTime = now;
            _frameCount = 0;
        }

        return CurrentFPS;
    }
}