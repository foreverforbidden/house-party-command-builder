using System.Diagnostics;
using System.IO;
using System.Windows;
using HpCommander.Builders;
using HpCommander.Services;

namespace HpCommander.Views;

public partial class InfoView : CommandCategoryViewBase
{
    private const string IssuesUrl = UpdateService.RepositoryUrl + "/issues";

    private readonly AppSettings _settings;
    private bool _suppressSettingEvents;

    private static string InfoPath => Path.Combine(AppContext.BaseDirectory, "Data", "info.txt");

    public InfoView(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        try
        {
            InfoText.Text = File.Exists(InfoPath) ? File.ReadAllText(InfoPath) : "";
        }
        catch (Exception ex)
        {
            InfoText.Text = $"(could not load Data/info.txt: {ex.Message})";
        }

        VersionText.Text = $"Version {AppVersion.CurrentDisplay}";

        _suppressSettingEvents = true;
        AutoUpdateCheck.IsChecked = _settings.UpdateCheckEnabled;
        _suppressSettingEvents = false;
    }

    private void IssueButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(IssuesUrl) { UseShellExecute = true });
        }
        catch
        {
            // Opening the browser is best-effort; nothing to recover if it fails.
        }
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateButton.IsEnabled = false;
        Report("Checking...");
        try
        {
            Report(await Prompt().RunManualCheckAsync());
        }
        finally
        {
            UpdateButton.IsEnabled = true;
            // Consent may have been granted from inside the check.
            _suppressSettingEvents = true;
            AutoUpdateCheck.IsChecked = _settings.UpdateCheckEnabled;
            _suppressSettingEvents = false;
        }
    }

    private void AutoUpdateCheck_Click(object sender, RoutedEventArgs e)
    {
        if (_suppressSettingEvents)
            return;

        if (AutoUpdateCheck.IsChecked == true && !_settings.UpdateCheckConsentGiven)
        {
            // The consent dialog is what actually sets the flag, and the user may say no.
            var enabled = Prompt().EnsureConsent();
            _suppressSettingEvents = true;
            AutoUpdateCheck.IsChecked = enabled;
            _suppressSettingEvents = false;
            return;
        }

        _settings.UpdateCheckEnabled = AutoUpdateCheck.IsChecked == true;
        _settings.Save();
    }

    private UpdatePrompt Prompt() => new(Window.GetWindow(this)!, _settings);

    private void Report(string message)
    {
        UpdateStatus.Text = message;
        UpdateStatus.Visibility = Visibility.Visible;
    }

    // Info isn't a command builder; there is nothing for the output bar to copy.
    public override CommandResult BuildCommand() =>
        CommandResult.Unavailable("Info is reference only - nothing to copy");
}
