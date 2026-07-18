using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class PropertiesView : UserControl, ICommandCategoryView
{
    private readonly CharacterChipPicker _targets;
    private bool _showList;

    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => true;

    public PropertiesView(GameData data, CharacterChipPicker targets)
    {
        InitializeComponent();
        _targets = targets;

        foreach (var p in data.Properties)
            PropCombo.Items.Add(p);
        PropCombo.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(Field_Changed));
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

    public CommandResult BuildCommand()
    {
        if (_showList)
            return CommandResult.Ok(PropertiesCommandBuilder.BuildList(_targets.GetSelectedTargets()));
        return string.IsNullOrWhiteSpace(PropCombo.Text)
            ? CommandResult.NeedsInput("Pick or type a property")
            : CommandResult.Ok(PropertiesCommandBuilder.Build(_targets.GetSelectedTargets(), PropCombo.Text.Trim()));
    }
}
