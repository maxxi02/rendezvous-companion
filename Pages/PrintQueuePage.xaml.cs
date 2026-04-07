using System.Collections.ObjectModel;
using rendezvous_companion.Models;
using rendezvous_companion.Services;

namespace rendezvous_companion.Pages;

public partial class PrintQueuePage : ContentPage
{
    private readonly PrintQueueService _queue;
    private readonly PrintManager _printManager;

    public ObservableCollection<PrintQueueItem> QueueItems => _queue.Items;

    public string SummaryText =>
        $"{_queue.FailedCount} failed · {_queue.PendingCount} pending · {_queue.Items.Count(i => i.Status == PrintJobStatus.Printed)} printed";

    public bool HasFailed => _queue.FailedCount > 0 && !IsRetryingAll;

    private bool _isRetryingAll;
    public bool IsRetryingAll
    {
        get => _isRetryingAll;
        set { _isRetryingAll = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasFailed)); }
    }

    public PrintQueuePage(PrintQueueService queue, PrintManager printManager)
    {
        InitializeComponent();
        _queue = queue;
        _printManager = printManager;
        BindingContext = this;

        _queue.QueueChanged += () => MainThread.BeginInvokeOnMainThread(RefreshSummary);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(HasFailed));
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PrintQueueItem item)
        {
            btn.IsEnabled = false;
            var ok = await _printManager.RetryQueueItemAsync(item);
            btn.IsEnabled = true;
            if (!ok)
                await DisplayAlertAsync("Retry Failed", "Could not print. Check printer connection.", "OK");
        }
    }

    private async void OnRetryAllClicked(object? sender, EventArgs e)
    {
        var failed = _queue.Items.Where(i => i.Status == PrintJobStatus.Failed).ToList();
        if (failed.Count == 0) return;

        IsRetryingAll = true;
        try
        {
            int success = 0;
            foreach (var item in failed)
            {
                if (await _printManager.RetryQueueItemAsync(item))
                    success++;
            }
            await DisplayAlertAsync("Retry Complete",
                $"{success}/{failed.Count} jobs printed successfully.", "OK");
        }
        finally
        {
            IsRetryingAll = false;
        }
    }

    private void OnDismissClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PrintQueueItem item)
            _queue.Remove(item.JobId);
    }
}
