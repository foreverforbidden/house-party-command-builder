using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class SocialView : CommandCategoryViewBase
{
    public SocialView(GameData data)
    {
        InitializeComponent();

        using (SuspendRecompute())
        {
            // 'all' is valid for the value form (`social all drunk 25 equals`).
            FillChars(ValueCharCombo, data, allTarget: SocialCommandBuilder.AllTarget);
            Fill(ValueCombo, data.SocialValues);
            Fill(ValueModifierCombo, data.SocialModifiers);

            FillChars(RelCharCombo, data);
            FillChars(RelTargetCombo, data, selectedIndex: 1);
            Fill(RelTypeCombo, data.SocialRelationships);
            Fill(RelModifierCombo, data.SocialModifiers);

            FillChars(ActionCharCombo, data);
            Fill(ActionCombo, data.SocialActions);
            FillChars(ActionTargetCombo, data, selectedIndex: 1);

            UpdateActionTargetEnabled();
        }
    }

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles; only react to the tab strip itself.
        if (!ReferenceEquals(e.OriginalSource, ModeTabs)) return;
        Recompute();
    }

    private void ActionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateActionTargetEnabled();
        Recompute();
    }

    private void UpdateActionTargetEnabled()
    {
        var needsTarget = (ActionCombo.SelectedItem as SocialAction)?.NeedsTarget == true;
        ActionTargetCombo.IsEnabled = needsTarget;
        ActionTargetLabel.Opacity = needsTarget ? 1.0 : 0.5;
    }

    public override CommandResult BuildCommand() => ModeTabs.SelectedIndex switch
    {
        0 => ValueCharCombo.SelectedItem is string c && ValueCombo.SelectedItem is string v
             && ValueModifierCombo.SelectedItem is string m
            ? CommandResult.Ok(SocialCommandBuilder.Value(c, v, (int)ValueAmount.Value, m))
            : CommandResult.NeedsInput("Pick a character, value and modifier"),

        1 => RelCharCombo.SelectedItem is string rc && RelTargetCombo.SelectedItem is string rt
             && RelTypeCombo.SelectedItem is string rel && RelModifierCombo.SelectedItem is string rm
            ? CommandResult.Ok(SocialCommandBuilder.Relationship(rc, rt, rel, rm, (int)RelAmount.Value))
            : CommandResult.NeedsInput("Pick both characters, a relationship and a modifier"),

        2 => ActionCharCombo.SelectedItem is string ac && ActionCombo.SelectedItem is SocialAction act
            ? (act.NeedsTarget
                ? ActionTargetCombo.SelectedItem is string at
                    ? CommandResult.Ok(SocialCommandBuilder.ActionWithTarget(ac, at, act.Name))
                    : CommandResult.NeedsInput("Pick a target")
                : CommandResult.Ok(SocialCommandBuilder.Action(ac, act.Name)))
            : CommandResult.NeedsInput("Pick a character and an action"),

        _ => CommandResult.Error($"Unhandled tab index {ModeTabs.SelectedIndex}"),
    };
}
