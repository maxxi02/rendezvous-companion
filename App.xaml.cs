using rendezvous_companion.Services;

namespace rendezvous_companion;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = DevicePreferencesService.LoadTheme();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    protected override void OnResume()
    {
        base.OnResume();
        // Auto-reconnect printers when app comes back from background
        var printManager = Handler?.MauiContext?.Services.GetService<PrintManager>();
        if (printManager != null)
            _ = printManager.TryAutoReconnectAsync();
    }
}
