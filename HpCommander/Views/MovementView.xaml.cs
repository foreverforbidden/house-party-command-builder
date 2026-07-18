using System.Windows;
using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class MovementView : CommandCategoryViewBase
{
    private const string All = MovementCommandBuilder.AllTarget;

    private enum Mode { Warp, Walk, OverTime, Turn, Roaming }

    public MovementView(GameData data)
    {
        InitializeComponent();

        using (SuspendRecompute())
        {
            // One character combo and one destination combo for all five tabs. Each tab used to
            // own a copy, independently defaulted, so picking Vickie on Warp and switching to
            // Walk silently emitted `walkto all <dest>` - a valid command for the wrong subject.
            // Index 1 is the first real character: defaulting to "all" is what made that bite.
            FillChars(CharCombo, data, allTarget: All, selectedIndex: 1);
            Fill(DestCombo, data.Locations.Select(l => l.ConsoleName), selectedIndex: -1);
            Fill(RoamActionCombo, data.RoamingActions);

            // Set default radio states here, not in XAML: an IsChecked="True" in XAML fires
            // the Checked handler during InitializeComponent, before the panels it toggles exist.
            WarpDestRadio.IsChecked = true;
            TurnAroundRadio.IsChecked = true;

            ApplyTabContext();
        }
    }

    private Mode Current => (Mode)Math.Max(0, ModeTabs.SelectedIndex);

    /// <summary>Only Warp, Walk and Roaming accept "all" as the mover.</summary>
    private bool CurrentTabAcceptsAll => Current is Mode.Warp or Mode.Walk or Mode.Roaming;

    private static bool IsAll(string value) => string.Equals(value, All, StringComparison.OrdinalIgnoreCase);

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles; without this the shared header would be re-labelled every
        // time a child combo changed.
        if (!ReferenceEquals(e.OriginalSource, ModeTabs)) return;
        ApplyTabContext();
        Recompute();
    }

    /// <summary>Points the shared header at whatever the current tab means by it. The fields keep
    /// their values across the switch; only the labels and visibility change.</summary>
    private void ApplyTabContext()
    {
        CharLabel.Text = CurrentTabAcceptsAll ? "Character (or 'all')" : "Character";

        DestLabel.Text = Current switch
        {
            Mode.Turn => "Toward (character or item)",
            Mode.Roaming => "Location / character",
            _ => "Destination",
        };

        var showDest = Current switch
        {
            Mode.Warp => WarpCoordRadio.IsChecked != true,
            Mode.Turn => TurnTowardRadio.IsChecked == true,
            Mode.Roaming => (RoamActionCombo.SelectedItem as string) is "allowlocation" or "prohibitlocation",
            _ => true,
        };
        DestPanel.Visibility = showDest ? Visibility.Visible : Visibility.Collapsed;

        RoamAllowPanel.Visibility =
            Current == Mode.Roaming && (RoamActionCombo.SelectedItem as string) == "allow"
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    private void WarpMode_Changed(object sender, RoutedEventArgs e)
    {
        ApplyTabContext();
        Recompute();
    }

    private void TurnMode_Changed(object sender, RoutedEventArgs e)
    {
        ApplyTabContext();
        Recompute();
    }

    private void RoamAction_Changed(object sender, SelectionChangedEventArgs e)
    {
        ApplyTabContext();
        Recompute();
    }

    // ---------------- building ----------------

    /// <summary>The shared mover, or guidance explaining why it isn't usable here.</summary>
    private CommandResult? MoverProblem(out string character)
    {
        character = CharCombo.EffectiveValue;
        if (string.IsNullOrWhiteSpace(character))
            return CommandResult.NeedsInput("Pick a character");
        if (IsAll(character) && !CurrentTabAcceptsAll)
            return CommandResult.NeedsInput($"'{All}' is not accepted here - pick a single character");
        return null;
    }

    public override CommandResult BuildCommand()
    {
        if (MoverProblem(out var c) is { } problem)
            return problem;

        var dest = DestCombo.EffectiveValue;

        return Current switch
        {
            Mode.Warp => BuildWarp(c, dest),
            Mode.Walk => string.IsNullOrWhiteSpace(dest)
                ? CommandResult.NeedsInput("Pick a destination")
                : CommandResult.Ok(MovementCommandBuilder.WalkTo(c, dest, WalkCancelCheck.IsChecked == true)),
            Mode.OverTime => string.IsNullOrWhiteSpace(dest)
                ? CommandResult.NeedsInput("Pick a destination")
                : CommandResult.Ok(MovementCommandBuilder.WarpOverTime(c, dest, (double)OverSeconds.Value)),
            Mode.Turn => BuildTurn(c, dest),
            Mode.Roaming => BuildRoaming(c, dest),
            _ => CommandResult.Error($"Unhandled tab index {ModeTabs.SelectedIndex}"),
        };
    }

    private CommandResult BuildWarp(string character, string destination)
    {
        if (WarpCoordRadio.IsChecked == true)
            return CommandResult.Ok(
                MovementCommandBuilder.WarpToCoords(character, (int)WarpX.Value, (int)WarpY.Value, (int)WarpZ.Value));
        return string.IsNullOrWhiteSpace(destination)
            ? CommandResult.NeedsInput("Pick a destination")
            : CommandResult.Ok(MovementCommandBuilder.WarpTo(character, destination));
    }

    private CommandResult BuildTurn(string character, string target)
    {
        if (TurnAroundRadio.IsChecked == true)
            return CommandResult.Ok(MovementCommandBuilder.TurnAround(character));
        return string.IsNullOrWhiteSpace(target)
            ? CommandResult.NeedsInput("Pick who to turn toward")
            : CommandResult.Ok(MovementCommandBuilder.TurnToward(character, target, TurnInstantCheck.IsChecked == true));
    }

    private CommandResult BuildRoaming(string character, string location) =>
        (RoamActionCombo.SelectedItem as string) switch
        {
            "list" => CommandResult.Ok(MovementCommandBuilder.RoamingList(character)),
            "allow" => CommandResult.Ok(MovementCommandBuilder.RoamingAllow(character, RoamAllowTrue.IsChecked == true)),
            "allowlocation" => string.IsNullOrWhiteSpace(location)
                ? CommandResult.NeedsInput("Pick a location")
                : CommandResult.Ok(MovementCommandBuilder.RoamingAllowLocation(character, location)),
            "prohibitlocation" => string.IsNullOrWhiteSpace(location)
                ? CommandResult.NeedsInput("Pick a location or character")
                : CommandResult.Ok(MovementCommandBuilder.RoamingProhibitLocation(character, location)),
            "clearlists" => CommandResult.Ok(MovementCommandBuilder.RoamingClearLists(character)),
            _ => CommandResult.NeedsInput("Pick an action"),
        };
}
