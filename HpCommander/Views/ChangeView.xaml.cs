using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class ChangeView : UserControl, ICommandCategoryView
{
    private readonly GameData _data;
    private readonly CharacterChipPicker _targets;
    private bool _showList;

    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => true;

    public ChangeView(GameData data, CharacterChipPicker targets)
    {
        InitializeComponent();
        _data = data;
        _targets = targets;

        PartCombo.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(Field_Changed));
        RefreshItems();
    }

    public void OnTargetsChanged() => RefreshItems();

    private void RefreshItems()
    {
        var current = PartCombo.Text;
        PartCombo.Items.Clear();
        foreach (var p in _data.ChangeParts)
            PartCombo.Items.Add(p);
        var character = _targets.GetSingleSelectedCharacter();
        if (character != null && _data.ChangeItemsByCharacter.TryGetValue(character, out var items))
            foreach (var item in items)
                PartCombo.Items.Add(item.Id);
        PartCombo.Text = current;
    }

    private void Field_Changed(object sender, RoutedEventArgs e)
    {
        _showList = false;
        CommandChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ListButton_Click(object sender, RoutedEventArgs e)
    {
        _showList = true;
        CommandChanged?.Invoke(this, EventArgs.Empty);
    }

    public string BuildCommand()
    {
        if (_showList)
            return ChangeCommandBuilder.BuildList(_targets.GetSelectedTargets());

        if (string.IsNullOrWhiteSpace(PartCombo.Text))
            return "(pick a clothing slot or item ID)";

        var mode = TrueRadio.IsChecked == true ? BoolMode.ForceTrue
            : FalseRadio.IsChecked == true ? BoolMode.ForceFalse
            : BoolMode.Toggle;
        return ChangeCommandBuilder.Build(_targets.GetSelectedTargets(), PartCombo.Text.Trim(), mode);
    }
}
