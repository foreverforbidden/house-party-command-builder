using HpCommander.Builders;
using HpCommander.Controls;

namespace HpCommander.Views;

public partial class AddforceView : TargetedCommandCategoryViewBase
{
    public AddforceView(CharacterChipPicker targets) : base(targets)
    {
        InitializeComponent();
    }

    public override CommandResult BuildCommand() => WithTargets(t => AddforceCommandBuilder.Build(
        t, (int)RightStepper.Value, (int)UpStepper.Value, (int)ForwardStepper.Value));
}
