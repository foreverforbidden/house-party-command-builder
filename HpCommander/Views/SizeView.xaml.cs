using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class SizeView : TargetedCommandCategoryViewBase
{
    public SizeView(GameData data, CharacterChipPicker targets) : base(targets)
    {
        InitializeComponent();
        Fill(PartCombo, data.SizeParts);
    }

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles; only react to the tab strip itself.
        if (!ReferenceEquals(e.OriginalSource, ModeTabs)) return;
        Recompute();
    }

    public override CommandResult BuildCommand() => ModeTabs.SelectedIndex switch
    {
        0 => WithTargets(t => SizeCommandBuilder.BuildWhole(t, (double)WholeScaleStepper.Value)),
        1 => PartCombo.SelectedItem is string part
            ? WithTargets(t => SizeCommandBuilder.BuildPart(t, part, (double)PartScaleStepper.Value))
            : CommandResult.NeedsInput("Pick a body part"),
        _ => CommandResult.Error($"Unhandled tab index {ModeTabs.SelectedIndex}"),
    };
}
