using AlarmClock.Services;
using AlarmClock.ViewModels;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;

namespace AlarmClock;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .AddAudio()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton(Preferences.Default);
        builder.Services.AddSingleton<IAlarmSound, AlarmSound>();
        builder.Services.AddSingleton<ClockViewModel>(services => new ClockViewModel(
            services.GetRequiredService<IPreferences>(), services.GetRequiredService<IAlarmSound>()));
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
