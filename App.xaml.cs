using rendezvous_companion.Pages;
using rendezvous_companion.Services;

namespace rendezvous_companion;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}