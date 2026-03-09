using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace rendezvous_companion.Platforms.Android;

[Service(ForegroundServiceType = ForegroundService.TypeConnectedDevice)]
public class CompanionForegroundService : Service
{
    private const int NotificationId = 10001;
    private const string ChannelId = "CompanionServiceChannel";

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    public override StartCommandResult OnStartCommand(
        Intent? intent,
        StartCommandFlags flags,
        int startId
    )
    {
        CreateNotificationChannel();

        var notification = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("Rendezvous Companion")
            .SetContentText("Running in background to maintain printer connection")
            .SetSmallIcon(Resource.Mipmap.appicon) // Make sure this matches your app icon
            .SetOngoing(true)
            .Build();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            StartForeground(NotificationId, notification, ForegroundService.TypeConnectedDevice);
        }
        else
        {
            StartForeground(NotificationId, notification);
        }

        // Keep service running until explicitly stopped
        return StartCommandResult.Sticky;
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(
                ChannelId,
                "Companion Background Service",
                NotificationImportance.Low
            )
            {
                Description =
                    "Keeps the Companion App running in the background for printer connectivity.",
            };

            var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
            notificationManager?.CreateNotificationChannel(channel);
        }
    }
}
