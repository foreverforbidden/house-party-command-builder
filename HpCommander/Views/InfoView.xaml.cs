using System.Diagnostics;
using System.IO;
using System.Windows;
using HpCommander.Builders;

namespace HpCommander.Views;

public partial class InfoView : CommandCategoryViewBase
{
    private const string IssuesUrl = "https://github.com/foreverforbidden/house-party-command-builder/issues";

    private static string InfoPath => Path.Combine(AppContext.BaseDirectory, "Data", "info.txt");

    public InfoView()
    {
        InitializeComponent();
        try
        {
            InfoText.Text = File.Exists(InfoPath) ? File.ReadAllText(InfoPath) : "";
        }
        catch (Exception ex)
        {
            InfoText.Text = $"(could not load Data/info.txt: {ex.Message})";
        }
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

    // Info isn't a command builder; there is nothing for the output bar to copy.
    public override CommandResult BuildCommand() =>
        CommandResult.Unavailable("Info is reference only - nothing to copy");
}
