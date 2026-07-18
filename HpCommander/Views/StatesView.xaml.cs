using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class StatesView : TargetedCommandCategoryViewBase
{
    public StatesView(GameData data, CharacterChipPicker targets) : base(targets)
    {
        InitializeComponent();
        Fill(StateCombo, data.States, selectedIndex: -1);
    }

    public override CommandResult BuildCommand() =>
        string.IsNullOrWhiteSpace(StateCombo.Text)
            ? CommandResult.NeedsInput("Pick or type a state")
            : WithTargets(t => StatesCommandBuilder.Build(t, StateCombo.Text.Trim()));
}
