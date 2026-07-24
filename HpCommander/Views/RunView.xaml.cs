using System.Windows;
using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

/// <summary>
/// `run` is the one verb whose target can be either a character or an item:
/// `leah.run(SwitchToAlternateTexture) = 0` and `ashleytop.run(UntieShirt)` are both valid, and
/// either can be listed with `&lt;target&gt;.run.list`. The tab picks which kind of target is in
/// play; on the Item tab the shell's character picker is irrelevant and hides itself.
/// </summary>
public partial class RunView : TargetedCommandCategoryViewBase
{
    private enum TargetMode { Character, Item }

    private readonly GameData _data;

    // Decorated dropdown label -> the item's internal name (the itemFunctions key).
    private readonly Dictionary<string, string> _itemByLabel = new(StringComparer.OrdinalIgnoreCase);

    private bool _showList;

    public RunView(GameData data, CharacterChipPicker targets) : base(targets)
    {
        InitializeComponent();
        _data = data;

        using (SuspendRecompute())
        {
            var labelled = _data.ItemFunctions.Keys
                .Select(name => (Name: name, Label: Describe(name)))
                .OrderBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var (name, label) in labelled)
                _itemByLabel[label] = name;

            Fill(ItemCombo, labelled.Select(x => x.Label), selectedIndex: -1);
            FillFunctions();
            ApplyTabContext();
        }
    }

    private TargetMode Current => (TargetMode)Math.Max(0, TargetTabs.SelectedIndex);

    /// <summary>The Item tab supplies its own target, so the shell's chip picker only belongs on
    /// the Character tab. Read on every recompute, not just on category switch.</summary>
    public override bool NeedsGlobalTargets => Current == TargetMode.Character;

    private string Describe(string internalName)
    {
        if (!_data.ItemDetails.TryGetValue(internalName, out var detail) || detail.DisplayName.Length == 0)
            return internalName;
        return detail.DisplayName.Equals(internalName, StringComparison.OrdinalIgnoreCase)
            ? internalName
            : $"{detail.DisplayName}  ({internalName})";
    }

    /// <summary>The console target for an item is its internal name normalised: "AshleyTop" ->
    /// "ashleytop". A free-typed name gets the same treatment, so "Ashley Top" also works.</summary>
    private string SelectedItemName()
    {
        var typed = ItemCombo.Text.Trim();
        return _itemByLabel.TryGetValue(typed, out var internalName) ? internalName : typed;
    }

    /// <summary>Character functions come from the hand-maintained list; item functions come from
    /// the item itself, which is the only place the interesting ones (UntieShirt, the texture
    /// switches) are enumerated.</summary>
    private void FillFunctions()
    {
        IEnumerable<string> functions = _data.RunFunctions;

        if (Current == TargetMode.Item &&
            _data.ItemFunctions.TryGetValue(SelectedItemName(), out var itemFunctions))
        {
            functions = itemFunctions;
        }

        RefillPreservingText(FuncCombo, functions);
    }

    private void ApplyTabContext()
    {
        FuncLabel.Text = Current == TargetMode.Item
            ? "Function name (the list follows the chosen item)"
            : "Function name (unbounded - type any known run function)";

        // In list mode the function and value are not part of the command; disabling them says so
        // more clearly than silently ignoring what is typed there.
        FuncCombo.IsEnabled = !_showList;
        ValueBox.IsEnabled = !_showList;
        ValueLabel.Opacity = _showList ? 0.5 : 1.0;
        ListNote.Visibility = _showList ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TargetTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles; only react to the tab strip itself.
        if (!ReferenceEquals(e.OriginalSource, TargetTabs)) return;

        using (SuspendRecompute())
        {
            FillFunctions();
            ApplyTabContext();
        }
    }

    private void ItemCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        FillFunctions();
        Recompute();
    }

    /// <summary>Typing a function or value means the user is done looking at the list.</summary>
    protected override void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_showList && (ReferenceEquals(sender, FuncCombo) || ReferenceEquals(sender, ValueBox)))
        {
            _showList = false;
            ApplyTabContext();
        }

        // Retyping the item narrows the function list too, but only once the text names a real item.
        if (ReferenceEquals(sender, ItemCombo))
            FillFunctions();

        base.OnTextChanged(sender, e);
    }

    private void ListButton_Click(object sender, RoutedEventArgs e)
    {
        _showList = true;
        ApplyTabContext();
        Recompute();
    }

    public override CommandResult BuildCommand() =>
        Current == TargetMode.Item ? BuildForItem() : BuildForCharacters();

    private CommandResult BuildForCharacters()
    {
        if (_showList)
            return WithTargets(RunCommandBuilder.BuildList);

        return string.IsNullOrWhiteSpace(FuncCombo.Text)
            ? CommandResult.NeedsInput("Type a run function name")
            : WithTargets(t => RunCommandBuilder.Build(t, FuncCombo.Text.Trim(), ValueBox.Text));
    }

    private CommandResult BuildForItem()
    {
        var item = SelectedItemName();
        if (item.Length == 0)
            return CommandResult.NeedsInput("Pick an item");

        string[] target = [TargetHelper.ConsoleName(item)];

        if (_showList)
            return CommandResult.Ok(RunCommandBuilder.BuildList(target));

        return string.IsNullOrWhiteSpace(FuncCombo.Text)
            ? CommandResult.NeedsInput("Pick a function for this item")
            : CommandResult.Ok(RunCommandBuilder.Build(target, FuncCombo.Text.Trim(), ValueBox.Text));
    }
}
