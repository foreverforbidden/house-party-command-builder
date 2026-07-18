using System.Windows;
using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class LegacyView : CommandCategoryViewBase
{
    private const string FreeForAllTarget = "(free-for-all - no target)";
    private const string FightSubcommand = "fight";

    private CommandResult _output = CommandResult.NeedsInput("Click a button");

    public LegacyView(GameData data)
    {
        InitializeComponent();

        using (SuspendRecompute())
        {
            Fill(EnableNpcCombo, data.Characters, selectedIndex: -1);

            var combatActions = data.LegacyCombatActions.ToList();
            Fill(CombatSubcommandCombo, combatActions, selectedIndex: Math.Max(0, combatActions.IndexOf("passout")));

            FillChars(CombatCharacterCombo, data, allTarget: LegacyCommandBuilder.CombatAllTarget, selectedIndex: -1);
            FillChars(CombatFightTargetCombo, data, allTarget: FreeForAllTarget);

            UpdateFightTargetEnabled();

            // Safe to fire NpcToggle_Changed now that every control exists.
            NpcEnableRadio.IsChecked = true;
        }
    }

    // ---------------- Enable / Disable ----------------

    private bool IsEnableSelected => NpcEnableRadio.IsChecked == true;

    private void NpcToggle_Changed(object sender, RoutedEventArgs e)
    {
        EnableNpcButton.Content = IsEnableSelected
            ? "Build 'EnableNPC <character>'"
            : "Build 'DisableNPC <character>'";

        // Keep an already-generated NPC command in sync with the toggle instead of leaving it stale.
        if (_output.IsOk &&
            (_output.Text.StartsWith("EnableNPC ", StringComparison.Ordinal) ||
             _output.Text.StartsWith("DisableNPC ", StringComparison.Ordinal)))
        {
            BuildNpcCommand();
        }
    }

    private void EnableNpcButton_Click(object sender, RoutedEventArgs e) => BuildNpcCommand();

    private void BuildNpcCommand() =>
        SetOutput(SafeBuild(() => CommandResult.Ok(LegacyCommandBuilder.SetNpcEnabled(EnableNpcCombo.Text.Trim(), IsEnableSelected))));

    // ---------------- Item enable ----------------

    private void EnableItemButton_Click(object sender, RoutedEventArgs e) =>
        SetOutput(SafeBuild(() => CommandResult.Ok(LegacyCommandBuilder.ItemSetEnabled(EnableItemBox.Text.Trim()))));

    // ---------------- Combat ----------------

    private void CombatSubcommandCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateFightTargetEnabled();

    private void UpdateFightTargetEnabled()
    {
        var isFight = string.Equals(CombatSubcommandCombo.SelectedItem as string, FightSubcommand, StringComparison.OrdinalIgnoreCase);
        CombatFightTargetCombo.IsEnabled = isFight;
        CombatFightTargetLabel.Opacity = isFight ? 1.0 : 0.5;
    }

    private void CombatButton_Click(object sender, RoutedEventArgs e) => SetOutput(SafeBuild(BuildCombatCommand));

    private CommandResult BuildCombatCommand()
    {
        var subcommand = CombatSubcommandCombo.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(subcommand))
            return CommandResult.NeedsInput("Pick a combat subcommand");

        var character = CombatCharacterCombo.Text.Trim();
        if (string.IsNullOrWhiteSpace(character))
            return CommandResult.NeedsInput("Pick a character, or 'all'");

        if (!string.Equals(subcommand, FightSubcommand, StringComparison.OrdinalIgnoreCase))
            return CommandResult.Ok(LegacyCommandBuilder.Combat(character, subcommand));

        var rawTarget = CombatFightTargetCombo.Text.Trim();
        var target = rawTarget == FreeForAllTarget ? "" : rawTarget;
        var attackerIsAll = character.Equals(LegacyCommandBuilder.CombatAllTarget, StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(target) && !attackerIsAll)
            return CommandResult.NeedsInput("Pick a fight target, or set the attacker to 'all' for a free-for-all");

        return CommandResult.Ok(LegacyCommandBuilder.CombatFight(character, target));
    }

    // ---------------- Intimacy reference ----------------

    private void IntimacyHelpButton_Click(object sender, RoutedEventArgs e) =>
        SetOutput(CommandResult.Ok(LegacyCommandBuilder.HelpIntimacy));

    private void IntimacyExampleButton_Click(object sender, RoutedEventArgs e) =>
        SetOutput(CommandResult.Ok(LegacyCommandBuilder.ExampleIntimacy));

    // ---------------- Shared ----------------

    private void SetOutput(CommandResult result)
    {
        _output = result;
        Recompute();
    }

    private static CommandResult SafeBuild(Func<CommandResult> build)
    {
        try
        {
            return build();
        }
        catch (Exception ex)
        {
            return CommandResult.Error(ex.Message);
        }
    }

    public override CommandResult BuildCommand() => _output;
}
