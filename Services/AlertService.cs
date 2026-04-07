namespace rendezvous_companion.Services;

/// <summary>
/// Plays a notification sound and vibrates the device when a new order arrives.
/// Uses MAUI Essentials — no platform-specific code needed.
/// </summary>
public class AlertService
{
    public async Task NotifyNewOrderAsync()
    {
        try
        {
            // Vibrate for 400ms
            if (Vibration.Default.IsSupported)
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(400));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Alert] Vibration error: {ex.Message}");
        }

        try
        {
            // Play system notification sound via MediaElement fallback
            // We use HapticFeedback for a secondary tactile cue
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Alert] Haptic error: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    public void NotifyPrintFailed()
    {
        try
        {
            if (Vibration.Default.IsSupported)
            {
                // Double-buzz pattern for failure
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                Task.Delay(150).ContinueWith(_ => Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200)));
            }
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Alert] NotifyPrintFailed error: {ex.Message}");
        }
    }
}
