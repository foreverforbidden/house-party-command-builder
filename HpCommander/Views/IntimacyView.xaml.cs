using System.Windows.Controls;
using System.Windows.Input;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class IntimacyView : CommandCategoryViewBase
{
    private sealed record IntimacyRow(string Name, string IdText);

    public IntimacyView(GameData data)
    {
        InitializeComponent();

        using (SuspendRecompute())
        {
            var eventNames = data.Intimacy.Events
                .Select(e => e.Name)
                .Where(n => !n.Equals("End", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var speedSubcommands = data.Intimacy.Subcommands
                .Select(s => s.Name)
                .Where(n => n.Equals("IncreaseActionSpeed", StringComparison.OrdinalIgnoreCase)
                         || n.Equals("DecreaseActionSpeed", StringComparison.OrdinalIgnoreCase))
                .ToList();

            FillChars(Char1Combo, data);
            FillChars(Char2Combo, data, selectedIndex: 1);
            Fill(TwoEventCombo, eventNames);

            FillChars(OneCharCombo, data);
            Fill(OneEventCombo, eventNames, selectedIndex: Math.Max(0, eventNames.IndexOf("StartMasturbation")));

            FillChars(EndCharCombo, data);
            FillChars(SpeedCharCombo, data);
            Fill(SpeedSubCombo, speedSubcommands);
            FillChars(ResetCharCombo, data);

            SubcommandsList.ItemsSource = data.Intimacy.Subcommands
                .Select(s => new IntimacyRow(s.Name, s.Id?.ToString() ?? "-")).ToList();
            EventsList.ItemsSource = data.Intimacy.Events
                .Select(e => new IntimacyRow(e.Name, e.Id?.ToString() ?? "-")).ToList();
        }
    }

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles; only react to the tab strip itself.
        if (!ReferenceEquals(e.OriginalSource, ModeTabs)) return;
        Recompute();
    }

    private void SubcommandsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SubcommandsList.SelectedItem is IntimacyRow row)
            RequestCopy(row.Name);
    }

    private void EventsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (EventsList.SelectedItem is IntimacyRow row)
            RequestCopy(row.Name);
    }

    public override CommandResult BuildCommand() => ModeTabs.SelectedIndex switch
    {
        0 => Char1Combo.SelectedItem is string c1 && Char2Combo.SelectedItem is string c2 && TwoEventCombo.SelectedItem is string ev
            ? CommandResult.Ok(IntimacyCommandBuilder.StartTwoCharacterAct(c1, c2, ev))
            : CommandResult.NeedsInput("Pick both characters and an event"),
        1 => OneCharCombo.SelectedItem is string oc && OneEventCombo.SelectedItem is string oe
            ? CommandResult.Ok(IntimacyCommandBuilder.StartSingleCharacterAct(oc, oe))
            : CommandResult.NeedsInput("Pick a character and an event"),
        2 => EndCharCombo.SelectedItem is string ec
            ? CommandResult.Ok(IntimacyCommandBuilder.EndAct(ec))
            : CommandResult.NeedsInput("Pick a character"),
        3 => SpeedCharCombo.SelectedItem is string sc && SpeedSubCombo.SelectedItem is string ss
            ? CommandResult.Ok(IntimacyCommandBuilder.ActionSpeed(sc, ss))
            : CommandResult.NeedsInput("Pick a character and a direction"),
        4 => ResetCharCombo.SelectedItem is string rc
            ? CommandResult.Ok(IntimacyCommandBuilder.ResetGuess(rc))
            : CommandResult.NeedsInput("Pick a character"),
        5 => CommandResult.Unavailable("Reference lookup only - double-click a row to copy a name"),
        _ => CommandResult.Error($"Unhandled tab index {ModeTabs.SelectedIndex}"),
    };
}
