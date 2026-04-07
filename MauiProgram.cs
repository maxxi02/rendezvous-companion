using Microsoft.Extensions.Logging;
using rendezvous_companion.Pages;
using rendezvous_companion.Services;

namespace rendezvous_companion;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ─── Core Services ────────────────────────────────────────────────────
        builder.Services.AddSingleton<AlertService>();
        builder.Services.AddSingleton<PrintQueueService>();
        builder.Services.AddSingleton<PrintManager>();
        builder.Services.AddSingleton<SocketService>();

        // ─── Pages ────────────────────────────────────────────────────────────
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<OrdersPage>();
        builder.Services.AddTransient<PrintQueuePage>();
        builder.Services.AddTransient<PrinterSettingsPage>();

#if ANDROID
        builder.Services.AddSingleton<
            rendezvous_companion.Services.IAppService,
            rendezvous_companion.Platforms.Android.AndroidAppService>();
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
