using Android.Content;
using rendezvous_companion.Services;

namespace rendezvous_companion.Platforms.Android;

public class AndroidAppService : IAppService
{
    public void StopAppAndService()
    {
        var context = global::Android.App.Application.Context;

        // Stop the foreground service
        var serviceIntent = new Intent(context, typeof(CompanionForegroundService));
        context.StopService(serviceIntent);

        // Tell MAUI to try and quit gracefully
        Application.Current?.Quit();

        // If it's still alive, forcefully kill the process so it doesn't linger in memory
        Java.Lang.JavaSystem.Exit(0);
    }
}
