using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class SocialView : UserControl, ICommandCategoryView
{
    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => false;

    public SocialView(GameData data)
    {
        InitializeComponent();

        // 'all' is valid for the value form (`social all drunk 25 equals`).
        ValueCharCombo.Items.Add(SocialCommandBuilder.AllTarget);
        foreach (var c in data.Characters) ValueCharCombo.Items.Add(c);
        ValueCharCombo.SelectedIndex = 0;

        Fill(ValueCombo, data.SocialValues);
        Fill(ValueModifierCombo, data.SocialModifiers);

        foreach (var c in data.Characters) RelCharCombo.Items.Add(c);
        if (RelCharCombo.Items.Count > 0) RelCharCombo.SelectedIndex = 0;
        foreach (var c in data.Characters) RelTargetCombo.Items.Add(c);
        if (RelTargetCombo.Items.Count > 1) RelTargetCombo.SelectedIndex = 1;
        Fill(RelTypeCombo, data.SocialRelationships);
        Fill(RelModifierCombo, data.SocialModifiers);

        foreach (var c in data.Characters) ActionCharCombo.Items.Add(c);
        if (ActionCharCombo.Items.Count > 0) ActionCharCombo.SelectedIndex = 0;
        foreach (var a in data.SocialActions) ActionCombo.Items.Add(a);
        if (ActionCombo.Items.Count > 0) ActionCombo.SelectedIndex = 0;
        foreach (var c in data.Characters) ActionTargetCombo.Items.Add(c);
        if (ActionTargetCombo.Items.Count > 1) ActionTargetCombo.SelectedIndex = 1;

        UpdateActionTargetEnabled();
    }

    private static void Fill(ComboBox combo, IEnumerable<string> values)
    {
        foreach (var v in values) combo.Items.Add(v);
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
    }

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles; only react to the tab strip itself.
        if (!ReferenceEquals(e.OriginalSource, ModeTabs)) return;
        CommandChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Selector_Changed(object sender, SelectionChangedEventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    private void Field_Changed(object? sender, EventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    private void ActionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateActionTargetEnabled();
        CommandChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateActionTargetEnabled()
    {
        var needsTarget = (ActionCombo.SelectedItem as SocialAction)?.NeedsTarget == true;
        ActionTargetCombo.IsEnabled = needsTarget;
        ActionTargetLabel.Opacity = needsTarget ? 1.0 : 0.5;
    }

    public CommandResult BuildCommand() => ModeTabs.SelectedIndex switch
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
