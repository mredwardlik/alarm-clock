namespace AlarmClock.Core;

/// <summary>Angles of the hands on a 12-hour dial, in degrees clockwise from 12.</summary>
public static class Dial
{
    public static float Hour(TimeOnly time) => 30f * (time.Hour % 12) + time.Minute / 2f + time.Second / 120f;

    public static float Minute(TimeOnly time) => 6f * time.Minute + time.Second / 10f;

    public static float Second(TimeOnly time) => 6f * time.Second;

    /// <summary>
    /// The arc from the hour hand to the alarm mark, clockwise. Null when the
    /// alarm is 12 hours or more away: the hand would go around the dial.
    /// </summary>
    public static (float Start, float Sweep)? ArcToAlarm(DateTime now, DateTime alarm)
    {
        var remaining = alarm - now;
        if (remaining <= TimeSpan.Zero || remaining >= TimeSpan.FromHours(12))
            return null;
        return (Hour(TimeOnly.FromDateTime(now)), (float)(remaining.TotalHours * 30));
    }

    /// <summary>
    /// A point on a circle for an angle of the dial.
    /// </summary>
    public static (float X, float Y) Point(float centerX, float centerY, float radius, float degrees)
    {
        var radians = degrees * MathF.PI / 180;
        return (centerX + radius * MathF.Sin(radians), centerY - radius * MathF.Cos(radians));
    }
}
