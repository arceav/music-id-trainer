using System.IO;
using System.Windows.Media;
using FormsTimer = System.Windows.Forms.Timer;

namespace MusicIdTrainer;

// MediaPlayer is Windows' audio player. It does not use a browser or WebView.
internal sealed class ClipPlayer : IDisposable
{
    private readonly FormsTimer _timer = new();
    private MediaPlayer? _player;
    private Action<string>? _status;
    private double _startSeconds;
    private int _seconds;
    private bool _randomStart;
    public double LastStartSeconds { get; private set; }

    public ClipPlayer()
    {
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            _player?.Pause();
            _status?.Invoke("Clip finished. Choose an answer or replay.");
        };
    }

    public void Play(string filePath, double startSeconds, int seconds, bool randomStart, Action<string> status)
    {
        Stop();
        _status = status;
        _startSeconds = startSeconds;
        _seconds = seconds;
        _randomStart = randomStart;
        try
        {
            _player = new MediaPlayer();
            _player.MediaOpened += OnOpened;
            _player.MediaFailed += OnFailed;
            _player.MediaEnded += OnEnded;
            _status("Opening recording…");
            _player.Open(new Uri(Path.GetFullPath(filePath)));
        }
        catch (Exception ex)
        {
            _status($"Could not play {Path.GetFileName(filePath)}: {ex.Message}");
            Stop();
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (_player is null) return;
        try
        {
            var duration = _player.NaturalDuration.HasTimeSpan
                ? _player.NaturalDuration.TimeSpan.TotalSeconds : 0;
            var latestStart = Math.Max(0, duration - _seconds);
            var start = _randomStart ? Random.Shared.NextDouble() * latestStart
                : Math.Min(_startSeconds, latestStart);
            LastStartSeconds = start;
            _player.Position = TimeSpan.FromSeconds(start);
            _player.Play();
            _timer.Interval = _seconds * 1000;
            _timer.Start();
            _status?.Invoke("Playing excerpt…");
        }
        catch (Exception ex)
        {
            _status?.Invoke($"Could not play this recording: {ex.Message}");
            Stop();
        }
    }

    private void OnFailed(object? sender, ExceptionEventArgs e)
    {
        _status?.Invoke($"Could not play this recording: {e.ErrorException.Message}");
        Stop();
    }

    private void OnEnded(object? sender, EventArgs e)
    {
        _timer.Stop();
        _status?.Invoke("Clip finished. Choose an answer or replay.");
    }

    public void Stop()
    {
        _timer.Stop();
        if (_player is not null)
        {
            _player.MediaOpened -= OnOpened;
            _player.MediaFailed -= OnFailed;
            _player.MediaEnded -= OnEnded;
            _player.Close();
            _player = null;
        }
        _status = null;
    }

    public void Dispose()
    {
        Stop();
        _timer.Dispose();
    }
}
