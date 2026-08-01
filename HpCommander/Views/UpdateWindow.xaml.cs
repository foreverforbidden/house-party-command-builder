using System.Windows;
using HpCommander.Services;

namespace HpCommander.Views;

/// <summary>
/// Runs the download-verify-swap for one release and reports what happened. A window rather than a
/// MessageBox because the asset is upwards of a hundred megabytes, and a dialog that cannot show
/// progress or be cancelled would just look hung.
/// </summary>
public partial class UpdateWindow : Window
{
    private readonly ReleaseInfo _release;
    private readonly UpdateService _service;
    private readonly CancellationTokenSource _cancellation = new();

    /// <summary>Set when the swap succeeded; the caller relaunches this and shuts down.</summary>
    public string? RelaunchPath { get; private set; }

    public UpdateWindow(ReleaseInfo release, UpdateService service)
    {
        InitializeComponent();
        _release = release;
        _service = service;

        StatusText.Text = $"Downloading version {release.Version}...";
        Loaded += async (_, _) => await RunAsync();
        Closed += (_, _) => _cancellation.Dispose();
    }

    private async Task RunAsync()
    {
        try
        {
            var progress = new Progress<double>(fraction =>
            {
                Progress.Value = fraction;
                DetailText.Text = $"{fraction:P0} of {Format(_release.DownloadSize)}";
            });

            var archive = await _service.DownloadAsync(_release, progress, _cancellation.Token);

            // Past this point cancelling would leave the install half-swapped.
            CancelButton.IsEnabled = false;
            Progress.IsIndeterminate = true;
            StatusText.Text = "Installing...";
            DetailText.Text = "Replacing the program and its Data folder.";

            RelaunchPath = await Task.Run(() => UpdateService.Apply(archive), CancellationToken.None);
            DialogResult = true;
        }
        catch (OperationCanceledException)
        {
            DialogResult = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"The update could not be installed:\n\n{ex.Message}\n\n" +
                "Your existing installation has been left as it was. You can download the new " +
                "version manually from the releases page instead.",
                "Update failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            DialogResult = false;
        }
    }

    private static string Format(long bytes) =>
        bytes > 0 ? $"{bytes / 1024d / 1024d:N0} MB" : "unknown size";

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _cancellation.Cancel();
        DialogResult = false;
    }
}
