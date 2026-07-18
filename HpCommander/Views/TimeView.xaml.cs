using HpCommander.Builders;

namespace HpCommander.Views;

public partial class TimeView : CommandCategoryViewBase
{
    public TimeView()
    {
        InitializeComponent();
    }

    public override CommandResult BuildCommand() =>
        CommandResult.Ok(TimeCommandBuilder.BuildScale((double)ScaleStepper.Value));
}
