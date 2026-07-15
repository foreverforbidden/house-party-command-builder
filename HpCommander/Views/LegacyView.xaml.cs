using System.Windows;
using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class LegacyView : UserControl, ICommandCategoryView
{
    private string _output = "(click a button)";

    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => false;

    public LegacyView(GameData data)
    {
        InitializeComponent();

        foreach (var c in data.Characters)
            EnableNpcCombo.Items.Add(c);
        foreach (var c in data.Characters)
            CombatTargetCombo.Items.Add(c);
        foreach (var a in data.LegacyCombatActions)
            CombatActionCombo.Items.Add(a);
    }

    private void EnableItemButton_Click(object sender, RoutedEventArgs e) =>
        SetOutput(SafeBuild(() => LegacyCommandBuilder.ItemSetEnabled(EnableItemBox.Text.Trim())));

    private void EnableNpcButton_Click(object sender, RoutedEventArgs e) =>
        SetOutput(SafeBuild(() => LegacyCommandBuilder.EnableNpc(EnableNpcCombo.Text.Trim())));

    private void CombatButton_Click(object sender, RoutedEventArgs e) =>
        SetOutput(SafeBuild(() => LegacyCommandBuilder.Combat(CombatTargetCombo.Text.Trim(), CombatActionCombo.Text.Trim())));

    private void IntimacyHelpButton_Click(object sender, RoutedEventArgs e) => SetOutput(LegacyCommandBuilder.HelpIntimacy);

    private void IntimacyExampleButton_Click(object sender, RoutedEventArgs e) => SetOutput(LegacyCommandBuilder.ExampleIntimacy);

    private void SetOutput(string text)
    {
        _output = text;
        CommandChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string SafeBuild(Func<string> build)
    {
        try
        {
            return build();
        }
        catch (Exception ex)
        {
            return $"({ex.Message})";
        }
    }

    public string BuildCommand() => _output;
}
