using System.Globalization;
using AlarmClock.Core;
using AlarmClock.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlarmClock.ViewModels;

public sealed partial class ClockViewModel : ObservableObject
{
    private const string TimeKey = "alarm.time";
    private const string EnabledKey = "alarm.enabled";
    private static readonly TimeOnly DefaultTime = new(7, 30);

    private readonly IPreferences preferences;
    private readonly IAlarmSound sound;
    private readonly Func<DateTime> clock;
    private readonly Alarm alarm;

    public ClockViewModel(IPreferences preferences, IAlarmSound sound, Func<DateTime>? clock = null)
    {
        this.preferences = preferences;
        this.sound = sound;
        this.clock = clock ?? (() => DateTime.Now);

        var time = TimeOnly.TryParse(preferences.Get(TimeKey, ""), CultureInfo.InvariantCulture, out var saved)
            ? saved : DefaultTime;
        alarm = new Alarm(time, preferences.Get(EnabledKey, true), this.clock());
        AlarmTime = time.ToTimeSpan();
        IsEnabled = alarm.IsEnabled;
        Update();
    }

    public DateTime Now { get; private set; }

    public DateTime? NextRing => alarm.NextRing;

    [ObservableProperty] public partial string Time { get; set; } = "";
    [ObservableProperty] public partial string Period { get; set; } = "";
    [ObservableProperty] public partial string Seconds { get; set; } = "";
    [ObservableProperty] public partial string Status { get; set; } = "";
    [ObservableProperty] public partial bool IsRinging { get; set; }
    [ObservableProperty] public partial double SwipeProgress { get; set; }

    /// <summary>12 or 24 hours, as in the settings of the device.</summary>
    public string TimeFormat { get; } =
        CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains('h') ? "h:mm tt" : "HH:mm";

    /// <summary>The time in the time picker.</summary>
    [ObservableProperty] public partial TimeSpan AlarmTime { get; set; }

    /// <summary>The on/off switch.</summary>
    [ObservableProperty] public partial bool IsEnabled { get; set; }

    /// <summary>Called by the page every second.</summary>
    public void Tick()
    {
        if (alarm.Tick(clock()))
            _ = sound.StartAsync();
        if (!alarm.IsRinging && IsRinging)
            sound.Stop();  // went quiet by itself after the ring limit
        Update();
    }

    partial void OnAlarmTimeChanged(TimeSpan value)
    {
        var time = TimeOnly.FromTimeSpan(value);
        if (time == alarm.Time)
            return;
        alarm.Set(time, clock());
        sound.Stop();
        IsEnabled = true;
        Save();
        Update();
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (value == alarm.IsEnabled)
            return;
        if (value)
            alarm.Enable(clock());
        else
            alarm.Disable();
        sound.Stop();
        Save();
        Update();
    }

    /// <summary>The swipe slider: dragged to the end stops the alarm.</summary>
    partial void OnSwipeProgressChanged(double value)
    {
        if (value >= 0.95 && alarm.IsRinging)
            Stop();
    }

    /// <summary>Released before the end: the thumb goes back.</summary>
    [RelayCommand]
    private void ResetSwipe()
    {
        if (SwipeProgress < 0.95)
            SwipeProgress = 0;
    }

    [RelayCommand]
    private void Stop()
    {
        alarm.Stop(clock());
        sound.Stop();
        SwipeProgress = 0;
        Update();
    }

    [RelayCommand]
    private void Snooze()
    {
        alarm.Snooze(clock());
        sound.Stop();
        SwipeProgress = 0;
        Update();
    }

    private void Save()
    {
        preferences.Set(TimeKey, alarm.Time.ToString("HH:mm", CultureInfo.InvariantCulture));
        preferences.Set(EnabledKey, alarm.IsEnabled);
    }

    private void Update()
    {
        Now = clock();
        var culture = CultureInfo.CurrentCulture;
        var twelveHours = culture.DateTimeFormat.ShortTimePattern.Contains('h');
        Time = Now.ToString(twelveHours ? "hh:mm" : "HH:mm", culture);
        Period = twelveHours ? Now.ToString("tt", CultureInfo.InvariantCulture) : "";
        Seconds = Now.ToString("ss", culture);
        IsRinging = alarm.IsRinging;

        var time = alarm.Time.ToString(twelveHours ? "h:mm tt" : "HH:mm", CultureInfo.InvariantCulture);
        Status = alarm.State switch
        {
            AlarmState.Ringing => $"⏰ It's {time}, time to wake up!",
            AlarmState.Waiting when alarm.IsSnoozed => $"Snoozed, {Alarm.DescribeRemaining(alarm.Remaining(Now)!.Value)}",
            AlarmState.Waiting => Alarm.DescribeRemaining(alarm.Remaining(Now)!.Value),
            _ => "The alarm is off",
        };
        OnPropertyChanged(nameof(NextRing));
        OnPropertyChanged(nameof(Now));
    }
}
