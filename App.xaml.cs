using rendezvous_companion.Pages;
using rendezvous_companion.Services;

namespace rendezvous_companion;

public partial class App : Application
{
    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        
        var page = serviceProvider.GetRequiredService<PrinterSettingsPage>();
        MainPage = new NavigationPage(page);
    }
}