using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class RunView : TargetedCommandCategoryViewBase
{
    public RunView(GameData data, CharacterChipPicker targets) : base(targets)
    {
        InitializeComponent();
        Fill(FuncCombo, data.RunFunctions, selectedIndex: -1);
    }

    public override CommandResult BuildCommand() =>
        string.IsNullOrWhiteSpace(FuncCombo.Text)
            ? CommandResult.NeedsInput("Type a run function name")
            : WithTargets(t => RunCommandBuilder.Build(t, FuncCombo.Text.Trim()));
}
