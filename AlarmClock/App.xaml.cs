namespace AlarmClock;

public partial class App : Application
{
    private readonly IServiceProvider services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        this.services = services;
    }

    // The page is created after InitializeComponent, so it can use the resources of App.xaml.
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(services.GetRequiredService<MainPage>())
        {
            Title = "Alarm Clock",
            Width = 420,
            Height = 820,
            MinimumWidth = 360,
            MinimumHeight = 680,
        };
}
