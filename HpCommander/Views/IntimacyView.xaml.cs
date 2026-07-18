using System.Windows.Controls;
using System.Windows.Input;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class IntimacyView : UserControl, ICommandCategoryView
{
    private sealed record IntimacyRow(string Name, string IdText);

    public event EventHandler? CommandChanged;
    public event EventHandler<string>? CopyRequested;

    public bool NeedsGlobalTargets => false;

    public IntimacyView(GameData data)
    {
        InitializeComponent();

        var eventNames = data.Intimacy.Events
            .Select(e => e.Name)
            .Where(n => !n.Equals("End", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var speedSubcommands = data.Intimacy.Subcommands
            .Select(s => s.Name)
            .Where(n => n.Equals("IncreaseActionSpeed", StringComparison.OrdinalIgnoreCase) || n.Equals("DecreaseActionSpeed", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var c in data.Characters) Char1Combo.Items.Add(c);
        if (Char1Combo.Items.Count > 0) Char1Combo.SelectedIndex = 0;
        foreach (var c in data.Characters) Char2Combo.Items.Add(c);
        if (Char2Combo.Items.Count > 1) Char2Combo.SelectedIndex = 1;
        foreach (var e in eventNames) TwoEventCombo.Items.Add(e);
        if (TwoEventCombo.Items.Count > 0) TwoEventCombo.SelectedIndex = 0;

        foreach (var c in data.Characters) OneCharCombo.Items.Add(c);
        if (OneCharCombo.Items.Count > 0) OneCharCombo.SelectedIndex = 0;
        foreach (var e in eventNames) OneEventCombo.Items.Add(e);
        var masturbationIndex = OneEventCombo.Items.IndexOf("StartMasturbation");
        OneEventCombo.SelectedIndex = masturbationIndex >= 0 ? masturbationIndex : (OneEventCombo.Items.Count > 0 ? 0 : -1);

        foreach (var c in data.Characters) EndCharCombo.Items.Add(c);
        if (EndCharCombo.Items.Count > 0) EndCharCombo.SelectedIndex = 0;

        foreach (var c in data.Characters) SpeedCharCombo.Items.Add(c);
        if (SpeedCharCombo.Items.Count > 0) SpeedCharCombo.SelectedIndex = 0;
        foreach (var s in speedSubcommands) SpeedSubCombo.Items.Add(s);
        if (SpeedSubCombo.Items.Count > 0) SpeedSubCombo.SelectedIndex = 0;

        foreach (var c in data.Characters) ResetCharCombo.Items.Add(c);
        if (ResetCharCombo.Items.Count > 0) ResetCharCombo.SelectedIndex = 0;

        SubcommandsList.ItemsSource = data.Intimacy.Subcommands.Select(s => new IntimacyRow(s.Name, s.Id?.ToString() ?? "-")).ToList();
        EventsList.ItemsSource = data.Intimacy.Events.Select(e => new IntimacyRow(e.Name, e.Id?.ToString() ?? "-")).ToList();
    }

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles; only react to the tab strip itself.
        if (!ReferenceEquals(e.OriginalSource, ModeTabs)) return;
        CommandChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Selector_Changed(object sender, SelectionChangedEventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    private void SubcommandsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SubcommandsList.SelectedItem is IntimacyRow row)
            CopyRequested?.Invoke(this, row.Name);
    }

    private void EventsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (EventsList.SelectedItem is IntimacyRow row)
            CopyRequested?.Invoke(this, row.Name);
    }

    public CommandResult BuildCommand() => ModeTabs.SelectedIndex switch
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
