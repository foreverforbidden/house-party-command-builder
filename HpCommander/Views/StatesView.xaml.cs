using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class StatesView : UserControl, ICommandCategoryView
{
    private readonly CharacterChipPicker _targets;

    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => true;

    public StatesView(GameData data, CharacterChipPicker targets)
    {
        InitializeComponent();
        _targets = targets;

        foreach (var s in data.States)
            StateCombo.Items.Add(s);
        StateCombo.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, _) => CommandChanged?.Invoke(this, EventArgs.Empty)));
    }

    public CommandResult BuildCommand() =>
        string.IsNullOrWhiteSpace(StateCombo.Text)
            ? CommandResult.NeedsInput("Pick or type a state")
            : CommandResult.Ok(StatesCommandBuilder.Build(_targets.GetSelectedTargets(), StateCombo.Text.Trim()));
}
