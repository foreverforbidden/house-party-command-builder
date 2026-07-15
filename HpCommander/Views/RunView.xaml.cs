using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class RunView : UserControl, ICommandCategoryView
{
    private readonly CharacterChipPicker _targets;

    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => true;

    public RunView(GameData data, CharacterChipPicker targets)
    {
        InitializeComponent();
        _targets = targets;

        foreach (var f in data.RunFunctions)
            FuncCombo.Items.Add(f);
        FuncCombo.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, _) => CommandChanged?.Invoke(this, EventArgs.Empty)));
    }

    public string BuildCommand() =>
        string.IsNullOrWhiteSpace(FuncCombo.Text)
            ? "(type a run function name)"
            : RunCommandBuilder.Build(_targets.GetSelectedTargets(), FuncCombo.Text.Trim());
}
