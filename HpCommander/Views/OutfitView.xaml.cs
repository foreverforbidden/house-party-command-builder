using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class OutfitView : TargetedCommandCategoryViewBase
{
    private readonly GameData _data;

    public OutfitView(GameData data, CharacterChipPicker targets) : base(targets)
    {
        InitializeComponent();
        _data = data;
        OnTargetsChanged();
    }

    public override void OnTargetsChanged()
    {
        var character = Targets.GetSingleSelectedCharacter();
        var outfits = character != null && _data.OutfitsByCharacter.TryGetValue(character, out var o)
            ? o
            : [];
        RefillPreservingText(OutfitCombo, outfits);
    }

    public override CommandResult BuildCommand() =>
        string.IsNullOrWhiteSpace(OutfitCombo.Text)
            ? CommandResult.NeedsInput("Pick or type an outfit ID")
            : WithTargets(t => OutfitCommandBuilder.Build(t, OutfitCombo.Text.Trim()));
}
