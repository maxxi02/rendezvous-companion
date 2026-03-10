using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting; // ← add this
using Microsoft.Maui.Hosting; // ← add this
using rendezvous_companion.Pages;
using rendezvous_companion.Services;

namespace rendezvous_companion;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>() // ← change UseMaui() to UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        builder.Services.AddSingleton<App>();
        builder.Services.AddSingleton<PrintManager>();
        builder.Services.AddSingleton<SocketService>();
        builder.Services.AddTransient<OrdersPage>();
        builder.Services.AddTransient<PrinterSettingsPage>();

#if ANDROID
        builder.Services.AddSingleton<
            rendezvous_companion.Services.IAppService,
            rendezvous_companion.Platforms.Android.AndroidAppService
        >();
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
