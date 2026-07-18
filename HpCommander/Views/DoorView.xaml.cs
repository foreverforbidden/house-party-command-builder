using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class DoorView : CommandCategoryViewBase
{
    // Maps a door's display text (what's shown/typed) to its console form.
    private readonly Dictionary<string, string> _consoleByDisplay = new(StringComparer.OrdinalIgnoreCase);

    public DoorView(GameData data)
    {
        InitializeComponent();

        foreach (var d in data.Doors)
        {
            DoorCombo.Items.Add(d.Display);
            _consoleByDisplay[d.Display] = d.Console;
        }
        Fill(ActionCombo, data.DoorActions);
    }

    public override CommandResult BuildCommand()
    {
        var typed = DoorCombo.Text.Trim();
        if (string.IsNullOrWhiteSpace(typed))
            return CommandResult.NeedsInput("Pick or type a door");
        if (ActionCombo.SelectedItem is not DoorAction action)
            return CommandResult.NeedsInput("Pick an action");
        // A picked door resolves to its console form; a free-typed name is normalised the same way.
        var door = _consoleByDisplay.TryGetValue(typed, out var console)
            ? console
            : DoorCommandBuilder.Normalise(typed);
        return CommandResult.Ok(DoorCommandBuilder.Build(door, action.Property, action.Value));
    }
}
