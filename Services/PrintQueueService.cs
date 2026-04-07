using System.Collections.ObjectModel;
using System.Text.Json;
using rendezvous_companion.Models;

namespace rendezvous_companion.Services;

/// <summary>
/// Persistent print queue. Failed jobs survive app restarts via Preferences.
/// </summary>
public class PrintQueueService
{
    private const string PrefsKey = "print_queue";
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public ObservableCollection<PrintQueueItem> Items { get; } = new();

    public event Action? QueueChanged;

    public PrintQueueService()
    {
        Load();
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    public void Enqueue(PrintQueueItem item)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Items.Insert(0, item);
            Save();
            QueueChanged?.Invoke();
        });
    }

    public void MarkPrinted(string jobId)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var item = Items.FirstOrDefault(i => i.JobId == jobId);
            if (item != null)
            {
                item.Status = PrintJobStatus.Printed;
                RefreshItem(item);
                Save();
                QueueChanged?.Invoke();
            }
        });
    }

    public void MarkFailed(string jobId)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var item = Items.FirstOrDefault(i => i.JobId == jobId);
            if (item != null)
            {
                item.Status = PrintJobStatus.Failed;
                item.RetryCount++;
                RefreshItem(item);
                Save();
                QueueChanged?.Invoke();
            }
        });
    }

    public void Remove(string jobId)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var item = Items.FirstOrDefault(i => i.JobId == jobId);
            if (item != null)
            {
                Items.Remove(item);
                Save();
                QueueChanged?.Invoke();
            }
        });
    }

    public int PendingCount => Items.Count(i => i.Status == PrintJobStatus.Pending);
    public int FailedCount => Items.Count(i => i.Status == PrintJobStatus.Failed);

    // ─── Persistence ──────────────────────────────────────────────────────────

    private void Load()
    {
        try
        {
            var json = Preferences.Get(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;
            var list = JsonSerializer.Deserialize<List<PrintQueueItem>>(json, _json);
            if (list == null) return;
            // Only restore non-printed items
            foreach (var item in list.Where(i => i.Status != PrintJobStatus.Printed))
                Items.Add(item);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PrintQueue] Load error: {ex.Message}");
        }
    }

    private void Save()
    {
        try
        {
            // Keep last 100 items max
            var toSave = Items.Take(100).ToList();
            Preferences.Set(PrefsKey, JsonSerializer.Serialize(toSave, _json));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PrintQueue] Save error: {ex.Message}");
        }
    }

    // Forces CollectionView to refresh the item in-place
    private void RefreshItem(PrintQueueItem item)
    {
        var idx = Items.IndexOf(item);
        if (idx >= 0)
        {
            Items.RemoveAt(idx);
            Items.Insert(idx, item);
        }
    }
}
