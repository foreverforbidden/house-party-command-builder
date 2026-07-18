using System.Windows;
using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class PropertiesView : TargetedCommandCategoryViewBase
{
    private bool _showList;

    public PropertiesView(GameData data, CharacterChipPicker targets) : base(targets)
    {
        InitializeComponent();
        Fill(PropCombo, data.Properties, selectedIndex: -1);
    }

    private void ListButton_Click(object sender, RoutedEventArgs e)
    {
        _showList = true;
        Recompute();
    }

    /// <summary>Typing a property name means the user is done looking at the list.</summary>
    protected override void OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _showList = false;
        base.OnTextChanged(sender, e);
    }

    public override CommandResult BuildCommand()
    {
        if (_showList)
            return WithTargets(PropertiesCommandBuilder.BuildList);
        return string.IsNullOrWhiteSpace(PropCombo.Text)
            ? CommandResult.NeedsInput("Pick or type a property")
            : WithTargets(t => PropertiesCommandBuilder.Build(t, PropCombo.Text.Trim()));
    }
}
