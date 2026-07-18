using System.Windows;
using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class SocialView : CommandCategoryViewBase
{
    private const string All = SocialCommandBuilder.AllTarget;

    private enum Mode { Value, Relationship, Action }

    public SocialView(GameData data)
    {
        InitializeComponent();

        using (SuspendRecompute())
        {
            // One subject and one target for all three tabs. Previously five character combos,
            // each with its own default, so every tab switch meant re-picking the cast.
            FillChars(CharCombo, data, allTarget: All, selectedIndex: 1);
            FillChars(TargetCombo, data, selectedIndex: 1);
            Fill(ModifierCombo, data.SocialModifiers);

            Fill(ValueCombo, data.SocialValues);
            Fill(RelTypeCombo, data.SocialRelationships);
            Fill(ActionCombo, data.SocialActions);

            ApplyTabContext();
        }
    }

    private Mode Current => (Mode)Math.Max(0, ModeTabs.SelectedIndex);

    /// <summary>Only the drunk/mood form documents "all" as a subject.</summary>
    private bool CurrentTabAcceptsAll => Current == Mode.Value;

    private bool ActionNeedsTarget => (ActionCombo.SelectedItem as SocialAction)?.NeedsTarget == true;

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles; only react to the tab strip itself.
        if (!ReferenceEquals(e.OriginalSource, ModeTabs)) return;
        ApplyTabContext();
        Recompute();
    }

    private void ActionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyTabContext();
        Recompute();
    }

    /// <summary>Points the shared header at what the current tab means by it.</summary>
    private void ApplyTabContext()
    {
        CharLabel.Text = Current switch
        {
            Mode.Value => "Character (or 'all')",
            Mode.Relationship => "Character (whose feelings change)",
            _ => "Character",
        };

        TargetLabel.Text = Current == Mode.Relationship ? "Toward" : "Target";

        var showTarget = Current switch
        {
            Mode.Relationship => true,
            Mode.Action => ActionNeedsTarget,
            _ => false,
        };
        TargetPanel.Visibility = showTarget ? Visibility.Visible : Visibility.Collapsed;

        // The action form takes no modifier.
        ModifierPanel.Visibility = Current == Mode.Action ? Visibility.Collapsed : Visibility.Visible;
    }

    public override CommandResult BuildCommand()
    {
        var character = CharCombo.EffectiveValue;
        if (string.IsNullOrWhiteSpace(character))
            return CommandResult.NeedsInput("Pick a character");
        if (string.Equals(character, All, StringComparison.OrdinalIgnoreCase) && !CurrentTabAcceptsAll)
            return CommandResult.NeedsInput($"'{All}' is not accepted here - pick a single character");

        var target = TargetCombo.EffectiveValue;
        var modifier = ModifierCombo.SelectedItem as string;

        return Current switch
        {
            Mode.Value => ValueCombo.SelectedItem is string v && modifier is not null
                ? CommandResult.Ok(SocialCommandBuilder.Value(character, v, (int)ValueAmount.Value, modifier))
                : CommandResult.NeedsInput("Pick a value and modifier"),

            Mode.Relationship => RelTypeCombo.SelectedItem is string rel && modifier is not null
                ? string.IsNullOrWhiteSpace(target)
                    ? CommandResult.NeedsInput("Pick who the feelings are toward")
                    : CommandResult.Ok(SocialCommandBuilder.Relationship(character, target, rel, modifier, (int)RelAmount.Value))
                : CommandResult.NeedsInput("Pick a relationship and modifier"),

            Mode.Action => ActionCombo.SelectedItem is SocialAction act
                ? act.NeedsTarget
                    ? string.IsNullOrWhiteSpace(target)
                        ? CommandResult.NeedsInput("Pick a target")
                        : CommandResult.Ok(SocialCommandBuilder.ActionWithTarget(character, target, act.Name))
                    : CommandResult.Ok(SocialCommandBuilder.Action(character, act.Name))
                : CommandResult.NeedsInput("Pick an action"),

            _ => CommandResult.Error($"Unhandled tab index {ModeTabs.SelectedIndex}"),
        };
    }
}
