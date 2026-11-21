namespace AlarmClock.Core;

public enum AlarmState
{
    /// <summary>The alarm is turned off.</summary>
    Off,
    /// <summary>Waiting for <see cref="Alarm.NextRing"/>.</summary>
    Waiting,
    /// <summary>Ringing until it is stopped or snoozed.</summary>
    Ringing,
}

/// <summary>
/// A daily alarm. The clock calls <see cref="Tick"/> every second, the alarm
/// decides when to ring. Time is passed in, so the logic is easy to test.
/// </summary>
public sealed class Alarm
{
    public static readonly TimeSpan SnoozeTime = TimeSpan.FromMinutes(5);

    /// <summary>An alarm nobody stops goes quiet after this time, like on a phone.</summary>
    public static readonly TimeSpan RingLimit = TimeSpan.FromMinutes(10);

    public Alarm(TimeOnly time, bool enabled, DateTime now)
    {
        Time = time;
        State = enabled ? AlarmState.Waiting : AlarmState.Off;
        NextRing = enabled ? NextOccurrence(time, now) : null;
    }

    /// <summary>Time of day when the alarm rings.</summary>
    public TimeOnly Time { get; private set; }

    public AlarmState State { get; private set; }

    public bool IsEnabled => State != AlarmState.Off;

    public bool IsRinging => State == AlarmState.Ringing;

    /// <summary>The next moment the alarm rings: today, tomorrow, or after a snooze.</summary>
    public DateTime? NextRing { get; private set; }

    public DateTime? RingingSince { get; private set; }

    /// <summary>Waiting after a snooze, not for the usual time.</summary>
    public bool IsSnoozed { get; private set; }

    /// <summary>The first moment after <paramref name="now"/> with this time of day.</summary>
    public static DateTime NextOccurrence(TimeOnly time, DateTime now)
    {
        var today = now.Date + time.ToTimeSpan();
        return today > now ? today : today.AddDays(1);
    }

    /// <summary>Set a new time and turn the alarm on.</summary>
    public void Set(TimeOnly time, DateTime now)
    {
        Time = time;
        Enable(now);
    }

    public void Enable(DateTime now)
    {
        State = AlarmState.Waiting;
        NextRing = NextOccurrence(Time, now);
        RingingSince = null;
        IsSnoozed = false;
    }

    public void Disable()
    {
        State = AlarmState.Off;
        NextRing = null;
        RingingSince = null;
        IsSnoozed = false;
    }

    /// <summary>Check the time. Returns true at the moment the alarm starts ringing.</summary>
    public bool Tick(DateTime now)
    {
        if (State == AlarmState.Ringing && now - RingingSince >= RingLimit)
        {
            Stop(now);
            return false;
        }
        if (State != AlarmState.Waiting || now < NextRing)
            return false;

        State = AlarmState.Ringing;
        RingingSince = now;
        IsSnoozed = false;
        return true;
    }

    /// <summary>Stop ringing, the alarm stays on for tomorrow.</summary>
    public void Stop(DateTime now)
    {
        if (State != AlarmState.Ringing)
            return;
        State = AlarmState.Waiting;
        RingingSince = null;
        NextRing = NextOccurrence(Time, now);
        IsSnoozed = false;
    }

    /// <summary>Stop ringing and ring again in <see cref="SnoozeTime"/>.</summary>
    public void Snooze(DateTime now)
    {
        if (State != AlarmState.Ringing)
            return;
        State = AlarmState.Waiting;
        RingingSince = null;
        NextRing = now + SnoozeTime;
        IsSnoozed = true;
    }

    public TimeSpan? Remaining(DateTime now) =>
        State == AlarmState.Waiting && NextRing is { } next ? next - now : null;

    /// <summary>"7h 42m until the alarm", rounded up to whole minutes.</summary>
    public static string DescribeRemaining(TimeSpan remaining)
    {
        var minutes = (int)Math.Ceiling(Math.Max(0, remaining.TotalMinutes));
        if (minutes <= 1)
            return "less than a minute until the alarm";
        var hours = minutes / 60;
        minutes %= 60;
        var text = hours == 0 ? $"{minutes}m" : minutes == 0 ? $"{hours}h" : $"{hours}h {minutes}m";
        return $"{text} until the alarm";
    }
}
