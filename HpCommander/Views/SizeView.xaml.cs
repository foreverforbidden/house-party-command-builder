using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class SizeView : UserControl, ICommandCategoryView
{
    private readonly CharacterChipPicker _targets;

    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => true;

    public SizeView(GameData data, CharacterChipPicker targets)
    {
        InitializeComponent();
        _targets = targets;

        foreach (var p in data.SizeParts)
            PartCombo.Items.Add(p);
        if (PartCombo.Items.Count > 0)
            PartCombo.SelectedIndex = 0;
    }

    private void Field_Changed(object? sender, EventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles; only react to the tab strip itself.
        if (!ReferenceEquals(e.OriginalSource, ModeTabs)) return;
        CommandChanged?.Invoke(this, EventArgs.Empty);
    }

    private void PartCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    public string BuildCommand() => ModeTabs.SelectedIndex switch
    {
        0 => SizeCommandBuilder.BuildWhole(_targets.GetSelectedTargets(), (double)WholeScaleStepper.Value),
        1 => PartCombo.SelectedItem is string part
            ? SizeCommandBuilder.BuildPart(_targets.GetSelectedTargets(), part, (double)PartScaleStepper.Value)
            : "(pick a body part)",
        _ => "",
    };
}
