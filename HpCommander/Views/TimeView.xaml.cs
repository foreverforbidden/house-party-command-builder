using System.Windows.Controls;
using HpCommander.Builders;

namespace HpCommander.Views;

public partial class TimeView : UserControl, ICommandCategoryView
{
    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => false;

    public TimeView()
    {
        InitializeComponent();
    }

    private void ScaleStepper_ValueChanged(object? sender, EventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    public CommandResult BuildCommand() =>
        CommandResult.Ok(TimeCommandBuilder.BuildScale((double)ScaleStepper.Value));
}
