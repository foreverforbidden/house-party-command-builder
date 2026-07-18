using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Controls;

namespace HpCommander.Views;

public partial class AddforceView : UserControl, ICommandCategoryView
{
    private readonly CharacterChipPicker _targets;

    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => true;

    public AddforceView(CharacterChipPicker targets)
    {
        InitializeComponent();
        _targets = targets;
    }

    private void Field_Changed(object? sender, EventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    public CommandResult BuildCommand() => CommandResult.Ok(AddforceCommandBuilder.Build(
        _targets.GetSelectedTargets(), (int)RightStepper.Value, (int)UpStepper.Value, (int)ForwardStepper.Value));
}
