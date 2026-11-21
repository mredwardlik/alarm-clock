using AlarmClock.Core;

namespace AlarmClock.Views;

/// <summary>
/// The dial: hour and minute hands, the alarm mark and a fading arc from the
/// hour hand to the alarm. Drawn in a 200×200 box, scaled to the view.
/// </summary>
public sealed class ClockDrawable : IDrawable
{
    private static readonly Color Pink = Color.FromArgb("#DE0469");
    private static readonly Color Dark = Color.FromArgb("#262626");
    private static readonly Color Tick = Color.FromArgb("#C9CCD9");

    public DateTime Now { get; set; } = DateTime.Now;

    /// <summary>When the alarm rings next, or null if it is off.</summary>
    public DateTime? Alarm { get; set; }

    public bool Ringing { get; set; }

    public void Draw(ICanvas canvas, RectF rect)
    {
        var size = Math.Min(rect.Width, rect.Height);
        canvas.SaveState();
        canvas.Translate(rect.Center.X, rect.Center.Y);
        canvas.Scale(size / 200f, size / 200f);

        // the face with a soft shadow
        canvas.SetShadow(new SizeF(0, 6), 18, Color.FromRgba(80, 90, 130, 60));
        canvas.FillColor = Colors.White;
        canvas.FillCircle(0, 0, 88);
        canvas.SetShadow(SizeF.Zero, 0, null);

        // hour marks
        canvas.StrokeColor = Tick;
        canvas.StrokeLineCap = LineCap.Round;
        for (var hour = 0; hour < 12; hour++)
        {
            var (x1, y1) = Dial.Point(0, 0, hour % 3 == 0 ? 72 : 76, hour * 30);
            var (x2, y2) = Dial.Point(0, 0, 80, hour * 30);
            canvas.StrokeSize = hour % 3 == 0 ? 2.5f : 1.5f;
            canvas.DrawLine(x1, y1, x2, y2);
        }

        if (Alarm is { } alarm)
        {
            DrawArc(canvas, alarm);
            var (x, y) = Dial.Point(0, 0, 64, Dial.Hour(TimeOnly.FromDateTime(alarm)));
            canvas.FillColor = Pink;
            canvas.FillCircle(x, y, Ringing ? 4 : 3);
        }

        var now = TimeOnly.FromDateTime(Now);
        DrawHand(canvas, Dial.Hour(now), 5, 52, Dark, 4);
        DrawHand(canvas, Dial.Minute(now), 10, 78, Pink, 2);

        canvas.FillColor = Pink;
        canvas.FillCircle(0, 0, 5);
        canvas.FillColor = Colors.White;
        canvas.FillCircle(0, 0, 1.8f);
        canvas.RestoreState();
    }

    /// <summary>The arc of the first version, now with a meaning: the time left until the alarm.</summary>
    private void DrawArc(ICanvas canvas, DateTime alarm)
    {
        if (Dial.ArcToAlarm(Now, alarm) is not { } arc || arc.Sweep < 4)
            return;  // a few minutes would be a tiny stroke that looks like a mistake
        const int parts = 48;
        canvas.StrokeSize = 5;
        canvas.StrokeLineCap = LineCap.Butt;
        for (var i = 0; i < parts; i++)
        {
            var from = arc.Start + arc.Sweep * i / parts;
            var to = arc.Start + arc.Sweep * (i + 1) / parts + 0.4f;  // a little overlap, no gaps
            canvas.StrokeColor = Pink.WithAlpha(0.08f + 0.92f * (i + 1) / parts);
            // MAUI angles go counterclockwise from 3 o'clock, the dial goes clockwise from 12
            canvas.DrawArc(-95, -95, 190, 190, 90 - from, 90 - to, true, false);
        }
    }

    private static void DrawHand(ICanvas canvas, float degrees, float back, float length, Color color, float width)
    {
        var (x1, y1) = Dial.Point(0, 0, -back, degrees);
        var (x2, y2) = Dial.Point(0, 0, length, degrees);
        canvas.StrokeColor = color;
        canvas.StrokeSize = width;
        canvas.StrokeLineCap = LineCap.Square;
        canvas.DrawLine(x1, y1, x2, y2);
    }
}
