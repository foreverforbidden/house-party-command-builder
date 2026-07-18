using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class DoorView : UserControl, ICommandCategoryView
{
    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => false;

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
        foreach (var a in data.DoorActions) ActionCombo.Items.Add(a);
        if (ActionCombo.Items.Count > 0) ActionCombo.SelectedIndex = 0;

        DoorCombo.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(Field_Changed));
    }

    private void Selector_Changed(object sender, SelectionChangedEventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    private void Field_Changed(object sender, RoutedEventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    public CommandResult BuildCommand()
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
