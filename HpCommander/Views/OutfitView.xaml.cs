using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class OutfitView : UserControl, ICommandCategoryView
{
    private readonly GameData _data;
    private readonly CharacterChipPicker _targets;

    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => true;

    public OutfitView(GameData data, CharacterChipPicker targets)
    {
        InitializeComponent();
        _data = data;
        _targets = targets;

        OutfitCombo.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(Field_Changed));
        OnTargetsChanged();
    }

    public void OnTargetsChanged()
    {
        var current = OutfitCombo.Text;
        OutfitCombo.Items.Clear();
        var character = _targets.GetSingleSelectedCharacter();
        if (character != null && _data.OutfitsByCharacter.TryGetValue(character, out var outfits))
            foreach (var o in outfits)
                OutfitCombo.Items.Add(o);
        OutfitCombo.Text = current;
    }

    private void Field_Changed(object sender, RoutedEventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    public string BuildCommand()
    {
        if (string.IsNullOrWhiteSpace(OutfitCombo.Text))
            return "(pick or type an outfit ID)";
        return OutfitCommandBuilder.Build(_targets.GetSelectedTargets(), OutfitCombo.Text.Trim());
    }
}
