using Plugin.Maui.Audio;

namespace AlarmClock.Services;

public interface IAlarmSound
{
    Task StartAsync();
    void Stop();
}

/// <summary>Plays Resources/Raw/alarm.wav in a loop and vibrates on phones.</summary>
public sealed class AlarmSound(IAudioManager audio) : IAlarmSound
{
    private IAudioPlayer? player;
    private CancellationTokenSource? vibration;

    public async Task StartAsync()
    {
        Stop();
        try
        {
            player = audio.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("alarm.wav"));
            player.Loop = true;
            player.Play();
        }
        catch (Exception)
        {
            player = null;  // no sound device: the alarm still shows on the screen
        }
        StartVibration();
    }

    public void Stop()
    {
        vibration?.Cancel();
        vibration = null;
        if (player is not null)
        {
            player.Stop();
            player.Dispose();
            player = null;
        }
        try
        {
            if (Vibration.Default.IsSupported)
                Vibration.Default.Cancel();
        }
        catch (Exception)
        {
            // no vibration on this device
        }
    }

    private void StartVibration()
    {
        if (!Vibration.Default.IsSupported)
            return;
        var cancel = vibration = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(600));
                }
                catch (Exception)
                {
                    return;
                }
                await Task.Delay(1500, cancel.Token).ConfigureAwait(false);
            }
        }, cancel.Token);
    }
}
