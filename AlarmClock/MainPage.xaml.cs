using AlarmClock.ViewModels;
using AlarmClock.Views;

namespace AlarmClock;

public partial class MainPage : ContentPage
{
    private readonly ClockViewModel model;
    private readonly ClockDrawable drawable = new();
    private readonly IDispatcherTimer timer;

    public MainPage(ClockViewModel model)
    {
        InitializeComponent();
        BindingContext = this.model = model;
        Dial.Drawable = drawable;

        timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(250);  // the second changes on time, not up to 1 s late
        timer.Tick += (_, _) => Tick();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Tick();
        timer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        timer.Stop();
    }

    private void Tick()
    {
        model.Tick();
        drawable.Now = model.Now;
        drawable.Alarm = model.NextRing;
        drawable.Ringing = model.IsRinging;
        Dial.Invalidate();
        // a bedside clock: keep the screen on while an alarm is set
        DeviceDisplay.Current.KeepScreenOn = model.IsEnabled;
    }
}
