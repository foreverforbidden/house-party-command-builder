using System.Windows;
using HpCommander.Builders;
using HpCommander.Controls;

namespace HpCommander.Views;

public partial class MiscView : TargetedCommandCategoryViewBase
{
    private CommandResult _output = CommandResult.NeedsInput("Click a button");

    public MiscView(CharacterChipPicker targets) : base(targets)
    {
        InitializeComponent();
    }

    private void AchButton_Click(object sender, RoutedEventArgs e) => SetOutput(SimpleCommandBuilder.AchievementsClear);

    private void UnstuckButton_Click(object sender, RoutedEventArgs e)
    {
        var target = Targets.GetSelectedTargets().FirstOrDefault(t => t != TargetHelper.AllCharactersTarget) ?? "Player";
        SetOutput(SimpleCommandBuilder.Unstuck(target));
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e) => SetOutput(SimpleCommandBuilder.HelpV2);

    private void SetOutput(string command)
    {
        _output = CommandResult.Ok(command);
        Recompute();
    }

    public override CommandResult BuildCommand() => _output;
}
