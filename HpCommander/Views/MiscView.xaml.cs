using System.Windows;
using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Controls;

namespace HpCommander.Views;

public partial class MiscView : UserControl, ICommandCategoryView
{
    private readonly CharacterChipPicker _targets;
    private CommandResult _output = CommandResult.NeedsInput("Click a button");

    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => true;

    public MiscView(CharacterChipPicker targets)
    {
        InitializeComponent();
        _targets = targets;
    }

    private void AchButton_Click(object sender, RoutedEventArgs e) => SetOutput(SimpleCommandBuilder.AchievementsClear);

    private void UnstuckButton_Click(object sender, RoutedEventArgs e)
    {
        var target = _targets.GetSelectedTargets().FirstOrDefault(t => t != TargetHelper.AllCharactersTarget) ?? "Player";
        SetOutput(SimpleCommandBuilder.Unstuck(target));
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e) => SetOutput(SimpleCommandBuilder.HelpV2);

    private void SetOutput(string command)
    {
        _output = CommandResult.Ok(command);
        CommandChanged?.Invoke(this, EventArgs.Empty);
    }

    public CommandResult BuildCommand() => _output;
}
