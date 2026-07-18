using System.Windows;
using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class ChangeView : TargetedCommandCategoryViewBase
{
    private readonly GameData _data;
    private bool _showList;

    public ChangeView(GameData data, CharacterChipPicker targets) : base(targets)
    {
        InitializeComponent();
        _data = data;
        OnTargetsChanged();
    }

    public override void OnTargetsChanged()
    {
        // Slot keywords first, then whatever the selected character can wear.
        var character = Targets.GetSingleSelectedCharacter();
        var items = character != null && _data.ChangeItemsByCharacter.TryGetValue(character, out var byChar)
            ? byChar.Select(i => i.Id)
            : [];
        RefillPreservingText(PartCombo, _data.ChangeParts.Concat(items));
    }

    /// <summary>Typing a slot or item means the user is done looking at the list.</summary>
    protected override void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _showList = false;
        base.OnTextChanged(sender, e);
    }

    private void ListButton_Click(object sender, RoutedEventArgs e)
    {
        _showList = true;
        Recompute();
    }

    public override CommandResult BuildCommand()
    {
        if (_showList)
            return WithTargets(ChangeCommandBuilder.BuildList);

        if (string.IsNullOrWhiteSpace(PartCombo.Text))
            return CommandResult.NeedsInput("Pick a clothing slot or item ID");

        var mode = TrueRadio.IsChecked == true ? BoolMode.ForceTrue
            : FalseRadio.IsChecked == true ? BoolMode.ForceFalse
            : BoolMode.Toggle;
        return WithTargets(t => ChangeCommandBuilder.Build(t, PartCombo.Text.Trim(), mode));
    }
}
