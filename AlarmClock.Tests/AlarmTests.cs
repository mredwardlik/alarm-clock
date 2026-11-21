using AlarmClock.Core;
using Xunit;

namespace AlarmClock.Tests;

public class AlarmTests
{
    private static readonly DateTime Evening = new(2026, 11, 21, 20, 0, 0);
    private static readonly TimeOnly Morning = new(7, 30);

    [Fact]
    public void Next_occurrence_is_today_or_tomorrow()
    {
        Assert.Equal(new DateTime(2026, 11, 21, 21, 0, 0), Alarm.NextOccurrence(new TimeOnly(21, 0), Evening));
        Assert.Equal(new DateTime(2026, 11, 22, 7, 30, 0), Alarm.NextOccurrence(Morning, Evening));
        // exactly now means tomorrow, the alarm doesn't ring right after it is set
        Assert.Equal(new DateTime(2026, 11, 22, 20, 0, 0), Alarm.NextOccurrence(new TimeOnly(20, 0), Evening));
    }

    [Fact]
    public void New_month_and_year()
    {
        var lastEvening = new DateTime(2026, 12, 31, 23, 0, 0);
        Assert.Equal(new DateTime(2027, 1, 1, 7, 30, 0), Alarm.NextOccurrence(Morning, lastEvening));
    }

    [Fact]
    public void Disabled_alarm_never_rings()
    {
        var alarm = new Alarm(Morning, enabled: false, Evening);
        Assert.Equal(AlarmState.Off, alarm.State);
        Assert.Null(alarm.NextRing);
        Assert.False(alarm.Tick(Evening.AddDays(2)));
        Assert.Null(alarm.Remaining(Evening));
    }

    [Fact]
    public void Rings_once_at_the_time()
    {
        var alarm = new Alarm(Morning, enabled: true, Evening);
        var ring = new DateTime(2026, 11, 22, 7, 30, 0);

        Assert.False(alarm.Tick(ring.AddSeconds(-1)));
        Assert.True(alarm.Tick(ring));
        Assert.True(alarm.IsRinging);
        Assert.False(alarm.Tick(ring.AddSeconds(1)));  // already ringing: no second start
        Assert.Equal(ring, alarm.RingingSince);
    }

    [Fact]
    public void Ticks_after_a_pause_still_ring()
    {
        // the app was in the background and missed the exact second
        var alarm = new Alarm(Morning, enabled: true, Evening);
        Assert.True(alarm.Tick(new DateTime(2026, 11, 22, 7, 32, 10)));
    }

    [Fact]
    public void Stop_waits_for_tomorrow()
    {
        var alarm = new Alarm(Morning, enabled: true, Evening);
        var ring = new DateTime(2026, 11, 22, 7, 30, 0);
        alarm.Tick(ring);
        alarm.Stop(ring.AddSeconds(20));

        Assert.Equal(AlarmState.Waiting, alarm.State);
        Assert.Equal(ring.AddDays(1), alarm.NextRing);
        Assert.False(alarm.Tick(ring.AddMinutes(1)));
    }

    [Fact]
    public void Snooze_rings_again_in_five_minutes()
    {
        var alarm = new Alarm(Morning, enabled: true, Evening);
        var ring = new DateTime(2026, 11, 22, 7, 30, 0);
        alarm.Tick(ring);
        alarm.Snooze(ring.AddSeconds(30));

        Assert.False(alarm.IsRinging);
        Assert.True(alarm.IsSnoozed);
        Assert.Equal(ring.AddSeconds(30) + Alarm.SnoozeTime, alarm.NextRing);
        Assert.False(alarm.Tick(ring.AddMinutes(5)));
        Assert.True(alarm.Tick(ring.AddMinutes(5).AddSeconds(30)));

        Assert.False(alarm.IsSnoozed);
        alarm.Stop(ring.AddMinutes(6));
        Assert.Equal(ring.AddDays(1), alarm.NextRing);  // after the snooze: the usual time tomorrow
    }

    [Fact]
    public void Stop_and_snooze_do_nothing_when_not_ringing()
    {
        var alarm = new Alarm(Morning, enabled: true, Evening);
        var next = alarm.NextRing;
        alarm.Stop(Evening);
        alarm.Snooze(Evening);
        Assert.Equal(next, alarm.NextRing);
        Assert.Equal(AlarmState.Waiting, alarm.State);
    }

    [Fact]
    public void Ringing_stops_by_itself_after_ten_minutes()
    {
        var alarm = new Alarm(Morning, enabled: true, Evening);
        var ring = new DateTime(2026, 11, 22, 7, 30, 0);
        alarm.Tick(ring);
        alarm.Tick(ring.AddMinutes(9));
        Assert.True(alarm.IsRinging);
        alarm.Tick(ring + Alarm.RingLimit);
        Assert.False(alarm.IsRinging);
        Assert.Equal(ring.AddDays(1), alarm.NextRing);
    }

    [Fact]
    public void Set_new_time_and_toggle()
    {
        var alarm = new Alarm(Morning, enabled: false, Evening);
        alarm.Set(new TimeOnly(22, 15), Evening);
        Assert.True(alarm.IsEnabled);
        Assert.Equal(new TimeOnly(22, 15), alarm.Time);
        Assert.Equal(TimeSpan.FromMinutes(135), alarm.Remaining(Evening));

        alarm.Disable();
        Assert.False(alarm.IsEnabled);
        alarm.Enable(Evening);
        Assert.Equal(new DateTime(2026, 11, 21, 22, 15, 0), alarm.NextRing);
    }

    [Fact]
    public void Disable_while_ringing_stops_it()
    {
        var alarm = new Alarm(Morning, enabled: true, Evening);
        alarm.Tick(new DateTime(2026, 11, 22, 7, 30, 0));
        alarm.Disable();
        Assert.Equal(AlarmState.Off, alarm.State);
        Assert.Null(alarm.RingingSince);
    }

    [Theory]
    [InlineData(0, "less than a minute until the alarm")]
    [InlineData(30, "less than a minute until the alarm")]
    [InlineData(61, "2m until the alarm")]
    [InlineData(45 * 60, "45m until the alarm")]
    [InlineData(3 * 3600, "3h until the alarm")]
    [InlineData(11 * 3600 + 30 * 60 - 20, "11h 30m until the alarm")]
    public void Remaining_time_text(int seconds, string text)
    {
        Assert.Equal(text, Alarm.DescribeRemaining(TimeSpan.FromSeconds(seconds)));
    }
}

public class DialTests
{
    [Theory]
    [InlineData(0, 0, 0f)]
    [InlineData(3, 0, 90f)]
    [InlineData(15, 0, 90f)]
    [InlineData(20, 15, 247.5f)]  // the mark of the first version was near 8 o'clock
    [InlineData(11, 30, 345f)]
    public void Hour_hand(int hour, int minute, float degrees)
    {
        Assert.Equal(degrees, Dial.Hour(new TimeOnly(hour, minute)));
    }

    [Fact]
    public void Minute_and_second_hands()
    {
        var time = new TimeOnly(10, 45, 30);
        Assert.Equal(273f, Dial.Minute(time));
        Assert.Equal(180f, Dial.Second(time));
    }

    [Fact]
    public void Arc_to_the_alarm()
    {
        var now = new DateTime(2026, 11, 21, 20, 0, 0);
        var arc = Dial.ArcToAlarm(now, now.AddHours(2.5));
        Assert.NotNull(arc);
        Assert.Equal(240f, arc.Value.Start);
        Assert.Equal(75f, arc.Value.Sweep);
        Assert.Null(Dial.ArcToAlarm(now, now.AddHours(12)));
        Assert.Null(Dial.ArcToAlarm(now, now.AddMinutes(-1)));
    }

    [Fact]
    public void Points_on_the_dial()
    {
        var (x, y) = Dial.Point(100, 100, 50, 90);
        Assert.Equal(150, x, 3);
        Assert.Equal(100, y, 3);
        (x, y) = Dial.Point(100, 100, 50, 0);
        Assert.Equal(100, x, 3);
        Assert.Equal(50, y, 3);
    }
}
