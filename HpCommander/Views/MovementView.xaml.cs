using System.Windows;
using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class MovementView : CommandCategoryViewBase
{
    private const string All = MovementCommandBuilder.AllTarget;

    private static readonly string[] RoamActions =
        ["list", "allow", "allowlocation", "prohibitlocation", "clearlists"];

    public MovementView(GameData data)
    {
        InitializeComponent();

        using (SuspendRecompute())
        {
            var destinations = data.Locations.Select(l => l.ConsoleName).ToList();

            // Movers: real characters, plus "all" where the command supports it.
            FillChars(WarpCharCombo, data, allTarget: All);
            FillChars(WalkCharCombo, data, allTarget: All);
            FillChars(OverCharCombo, data);
            FillChars(TurnCharCombo, data);
            FillChars(RoamCharCombo, data, allTarget: All);

            foreach (var combo in new[] { WarpDestCombo, WalkDestCombo, OverDestCombo, TurnTargetCombo, RoamDestCombo })
                Fill(combo, destinations, selectedIndex: -1);

            Fill(RoamActionCombo, RoamActions);
            UpdateRoamPanels();

            // Set default radio states here, not in XAML: an IsChecked="True" in XAML fires
            // the Checked handler during InitializeComponent, before the panels it toggles exist.
            WarpDestRadio.IsChecked = true;
            TurnAroundRadio.IsChecked = true;
        }
    }

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles; only react to the tab strip itself.
        if (!ReferenceEquals(e.OriginalSource, ModeTabs)) return;
        Recompute();
    }

    private void WarpMode_Changed(object sender, RoutedEventArgs e)
    {
        var coords = WarpCoordRadio.IsChecked == true;
        WarpCoordPanel.Visibility = coords ? Visibility.Visible : Visibility.Collapsed;
        WarpDestCombo.Visibility = coords ? Visibility.Collapsed : Visibility.Visible;
        Recompute();
    }

    private void TurnMode_Changed(object sender, RoutedEventArgs e)
    {
        TurnTowardPanel.Visibility = TurnTowardRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        Recompute();
    }

    private void RoamAction_Changed(object sender, SelectionChangedEventArgs e)
    {
        UpdateRoamPanels();
        Recompute();
    }

    private void UpdateRoamPanels()
    {
        var action = RoamActionCombo.SelectedItem as string;
        RoamAllowPanel.Visibility = action == "allow" ? Visibility.Visible : Visibility.Collapsed;
        var isLocation = action is "allowlocation" or "prohibitlocation";
        RoamLocationPanel.Visibility = isLocation ? Visibility.Visible : Visibility.Collapsed;
    }

    public override CommandResult BuildCommand() => ModeTabs.SelectedIndex switch
    {
        0 => BuildWarp(),
        1 => WalkCharCombo.SelectedItem is string wc
            ? (string.IsNullOrWhiteSpace(WalkDestCombo.Text)
                ? CommandResult.NeedsInput("Pick a destination")
                : CommandResult.Ok(MovementCommandBuilder.WalkTo(wc, WalkDestCombo.Text.Trim(), WalkCancelCheck.IsChecked == true)))
            : CommandResult.NeedsInput("Pick a character"),
        2 => OverCharCombo.SelectedItem is string oc
            ? (string.IsNullOrWhiteSpace(OverDestCombo.Text)
                ? CommandResult.NeedsInput("Pick a destination")
                : CommandResult.Ok(MovementCommandBuilder.WarpOverTime(oc, OverDestCombo.Text.Trim(), (double)OverSeconds.Value)))
            : CommandResult.NeedsInput("Pick a character"),
        3 => BuildTurn(),
        4 => BuildRoaming(),
        _ => CommandResult.Error($"Unhandled tab index {ModeTabs.SelectedIndex}"),
    };

    private CommandResult BuildWarp()
    {
        if (WarpCharCombo.SelectedItem is not string c)
            return CommandResult.NeedsInput("Pick a character");
        if (WarpCoordRadio.IsChecked == true)
            return CommandResult.Ok(MovementCommandBuilder.WarpToCoords(c, (int)WarpX.Value, (int)WarpY.Value, (int)WarpZ.Value));
        return string.IsNullOrWhiteSpace(WarpDestCombo.Text)
            ? CommandResult.NeedsInput("Pick a destination")
            : CommandResult.Ok(MovementCommandBuilder.WarpTo(c, WarpDestCombo.Text.Trim()));
    }

    private CommandResult BuildTurn()
    {
        if (TurnCharCombo.SelectedItem is not string c)
            return CommandResult.NeedsInput("Pick a character");
        if (TurnAroundRadio.IsChecked == true)
            return CommandResult.Ok(MovementCommandBuilder.TurnAround(c));
        return string.IsNullOrWhiteSpace(TurnTargetCombo.Text)
            ? CommandResult.NeedsInput("Pick who to turn toward")
            : CommandResult.Ok(MovementCommandBuilder.TurnToward(c, TurnTargetCombo.Text.Trim(), TurnInstantCheck.IsChecked == true));
    }

    private CommandResult BuildRoaming()
    {
        if (RoamCharCombo.SelectedItem is not string c)
            return CommandResult.NeedsInput("Pick a character");
        return (RoamActionCombo.SelectedItem as string) switch
        {
            "list" => CommandResult.Ok(MovementCommandBuilder.RoamingList(c)),
            "allow" => CommandResult.Ok(MovementCommandBuilder.RoamingAllow(c, RoamAllowTrue.IsChecked == true)),
            "allowlocation" => string.IsNullOrWhiteSpace(RoamDestCombo.Text)
                ? CommandResult.NeedsInput("Pick a location")
                : CommandResult.Ok(MovementCommandBuilder.RoamingAllowLocation(c, RoamDestCombo.Text.Trim())),
            "prohibitlocation" => string.IsNullOrWhiteSpace(RoamDestCombo.Text)
                ? CommandResult.NeedsInput("Pick a location or character")
                : CommandResult.Ok(MovementCommandBuilder.RoamingProhibitLocation(c, RoamDestCombo.Text.Trim())),
            "clearlists" => CommandResult.Ok(MovementCommandBuilder.RoamingClearLists(c)),
            _ => CommandResult.NeedsInput("Pick an action"),
        };
    }
}
