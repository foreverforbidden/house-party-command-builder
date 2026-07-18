using System.Windows;
using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class ValuesView : UserControl, ICommandCategoryView
{
    private readonly CharacterChipPicker _targets;

    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => true;

    public ValuesView(GameData data, CharacterChipPicker targets)
    {
        InitializeComponent();
        _targets = targets;

        foreach (var t in data.Traits)
            TraitCombo.Items.Add(t);
        if (TraitCombo.Items.Count > 0)
            TraitCombo.SelectedIndex = 0;

        foreach (var c in data.Characters)
            RelOtherCombo.Items.Add(c);
        if (RelOtherCombo.Items.Count > 0)
            RelOtherCombo.SelectedIndex = 0;

        foreach (var t in data.RelationshipTypes)
            RelTypeCombo.Items.Add(t);
        if (RelTypeCombo.Items.Count > 0)
            RelTypeCombo.SelectedIndex = 0;

        foreach (var g in data.GenericValues)
            GenCombo.Items.Add(g);
        if (GenCombo.Items.Count > 0)
            GenCombo.SelectedIndex = 0;
    }

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles; only react to the tab strip itself.
        if (!ReferenceEquals(e.OriginalSource, ModeTabs)) return;
        CommandChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Selector_Changed(object sender, SelectionChangedEventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    private void Field_Changed(object? sender, EventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    private void Field_ChangedRouted(object sender, RoutedEventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    public CommandResult BuildCommand() => ModeTabs.SelectedIndex switch
    {
        0 => TraitCombo.SelectedItem is string trait
            ? CommandResult.Ok(ValuesCommandBuilder.BuildTrait(_targets.GetSelectedTargets(), trait, (double)TraitValueStepper.Value))
            : CommandResult.NeedsInput("Pick a trait"),
        1 => RelOtherCombo.SelectedItem is string other && RelTypeCombo.SelectedItem is string relType
            ? CommandResult.Ok(ValuesCommandBuilder.BuildRelationship(_targets.GetSelectedTargets(), other, relType, (double)RelValueStepper.Value))
            : CommandResult.NeedsInput("Pick the other character and a relationship type"),
        2 => GenCombo.SelectedItem is GenericValue gv
            ? CommandResult.Ok(ValuesCommandBuilder.BuildGeneric(gv.Id, gv.Property, GenCheckBox.IsChecked == true ? 1 : 0))
            : CommandResult.NeedsInput("Pick an object value"),
        3 => CommandResult.Ok(ValuesCommandBuilder.BuildList(_targets.GetSelectedTargets(), FilterBox.Text)),
        _ => CommandResult.Error($"Unhandled tab index {ModeTabs.SelectedIndex}"),
    };
}
