using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace HpCommander.Views;

public partial class InfoView : UserControl, ICommandCategoryView
{
    private const string IssuesUrl = "https://github.com/foreverforbidden/house-party-command-builder/issues";

    private static string InfoPath => Path.Combine(AppContext.BaseDirectory, "Data", "info.txt");

#pragma warning disable CS0067 // Info builds no command; the output bar stays empty for this view.
    public event EventHandler? CommandChanged;
#pragma warning restore CS0067

    public bool NeedsGlobalTargets => false;

    public InfoView()
    {
        InitializeComponent();
        LoadInfo();
    }

    private void LoadInfo()
    {
        try
        {
            InfoBox.Text = File.Exists(InfoPath) ? File.ReadAllText(InfoPath) : "";
            StatusText.Text = "";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Could not load: {ex.Message}";
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(InfoPath)!);
            File.WriteAllText(InfoPath, InfoBox.Text);
            StatusText.Text = $"Saved to Data/info.txt at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Could not save: {ex.Message}";
        }
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e) => LoadInfo();

    private void IssueButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(IssuesUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Could not open browser: {ex.Message}";
        }
    }

    // Info isn't a command builder; the output bar shows nothing for this view.
    public string BuildCommand() => "";
}
