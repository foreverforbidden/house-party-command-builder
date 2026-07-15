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

    public string BuildCommand() =>
        string.IsNullOrWhiteSpace(StateCombo.Text)
            ? "(pick or type a state)"
            : StatesCommandBuilder.Build(_targets.GetSelectedTargets(), StateCombo.Text.Trim());
}
