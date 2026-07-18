using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class ValuesView : TargetedCommandCategoryViewBase
{
    public ValuesView(GameData data, CharacterChipPicker targets) : base(targets)
    {
        InitializeComponent();

        using (SuspendRecompute())
        {
            Fill(TraitCombo, data.Traits);
            Fill(RelOtherCombo, data.Characters);
            Fill(RelTypeCombo, data.RelationshipTypes);
            Fill(GenCombo, data.GenericValues);
        }
    }

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles; only react to the tab strip itself.
        if (!ReferenceEquals(e.OriginalSource, ModeTabs)) return;
        Recompute();
    }

    public override CommandResult BuildCommand() => ModeTabs.SelectedIndex switch
    {
        0 => TraitCombo.SelectedItem is string trait
            ? WithTargets(t => ValuesCommandBuilder.BuildTrait(t, trait, (double)TraitValueStepper.Value))
            : CommandResult.NeedsInput("Pick a trait"),
        1 => RelOtherCombo.SelectedItem is string other && RelTypeCombo.SelectedItem is string relType
            ? WithTargets(t => ValuesCommandBuilder.BuildRelationship(t, other, relType, (double)RelValueStepper.Value))
            : CommandResult.NeedsInput("Pick the other character and a relationship type"),
        2 => GenCombo.SelectedItem is GenericValue gv
            ? CommandResult.Ok(ValuesCommandBuilder.BuildGeneric(gv.Id, gv.Property, GenCheckBox.IsChecked == true ? 1 : 0))
            : CommandResult.NeedsInput("Pick an object value"),
        3 => WithTargets(t => ValuesCommandBuilder.BuildList(t, FilterBox.Text)),
        _ => CommandResult.Error($"Unhandled tab index {ModeTabs.SelectedIndex}"),
    };
}
