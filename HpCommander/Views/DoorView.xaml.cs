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

    public DoorView(GameData data)
    {
        InitializeComponent();

        foreach (var d in data.Doors) DoorCombo.Items.Add(d);
        foreach (var a in data.DoorActions) ActionCombo.Items.Add(a);
        if (ActionCombo.Items.Count > 0) ActionCombo.SelectedIndex = 0;

        DoorCombo.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(Field_Changed));
    }

    private void Selector_Changed(object sender, SelectionChangedEventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    private void Field_Changed(object sender, RoutedEventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    public string BuildCommand()
    {
        var door = DoorCombo.Text.Trim();
        if (string.IsNullOrWhiteSpace(door))
            return "(pick or type a door)";
        if (ActionCombo.SelectedItem is not string action)
            return "(pick an action)";
        return DoorCommandBuilder.Build(door, action);
    }
}
