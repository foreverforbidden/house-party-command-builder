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

    private const string CommonItemGroup = "Common to every item";
    private const string CharacterGroup = "Character run functions";

    private readonly GameData _data;

    // Decorated dropdown label -> the item's internal name (the itemFunctions key).
    private readonly Dictionary<string, string> _itemByLabel = new(StringComparer.OrdinalIgnoreCase);

    // Internal name, case-folded -> that name as spelled in the JSON. ItemFunctions is an ordinal
    // dictionary, so without this a typed "ac unit" misses the "AC Unit" entry and the function
    // list silently falls back to the character one.
    private readonly Dictionary<string, string> _itemByName = new(StringComparer.OrdinalIgnoreCase);

    // characterRunFunctions is hand-authored, so match its keys leniently rather than silently
    // falling back to the shared list because someone typed "ashley" instead of "Ashley".
    private readonly Dictionary<string, List<string>> _characterRunFunctions =
        new(StringComparer.OrdinalIgnoreCase);

    private bool _showList;
    private bool _refilling;

    public RunView(GameData data, CharacterChipPicker targets) : base(targets)
    {
        InitializeComponent();
        _data = data;

        foreach (var (character, functions) in _data.CharacterRunFunctions)
            _characterRunFunctions[character] = functions;

        using (SuspendRecompute())
        {
            var labelled = _data.ItemFunctions.Keys
                .Select(name => (Name: name, Label: Describe(name)))
                .OrderBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var (name, label) in labelled)
            {
                _itemByLabel[label] = name;
                _itemByName[name] = name;
            }

            Fill(ItemCombo, labelled.Select(x => x.Label), selectedIndex: -1);
            FillFunctions();
            ApplyTabContext();
        }
    }

    private TargetMode Current => (TargetMode)Math.Max(0, TargetTabs.SelectedIndex);

    /// <summary>The Item tab supplies its own target, so the shell's chip picker only belongs on
    /// the Character tab. Read on every recompute, not just on category switch.</summary>
    public override bool NeedsGlobalTargets => Current == TargetMode.Character;

    private string DisplayName(string internalName) =>
        _data.ItemDetails.TryGetValue(internalName, out var detail) && detail.DisplayName.Length > 0
            ? detail.DisplayName
            : internalName;

    private string Describe(string internalName)
    {
        var display = DisplayName(internalName);
        return display.Equals(internalName, StringComparison.OrdinalIgnoreCase)
            ? internalName
            : $"{display}  ({internalName})";
    }

    /// <summary>The console target for an item is its internal name normalised: "AshleyTop" ->
    /// "ashleytop". A free-typed name gets the same treatment, so "Ashley Top" also works.</summary>
    private string SelectedItemName()
    {
        var typed = ItemCombo.Text.Trim();
        return _itemByLabel.TryGetValue(typed, out var internalName) ? internalName : typed;
    }

    /// <summary>Resolves whatever is typed or picked to an entry in the item table, canonicalising
    /// the casing on the way so a typed "ac unit" still finds "AC Unit".</summary>
    private bool TryGetItemFunctions(out string itemName, out List<string> functions)
    {
        itemName = SelectedItemName();
        if (_itemByName.TryGetValue(itemName, out var canonical))
            itemName = canonical;

        return _data.ItemFunctions.TryGetValue(itemName, out functions!);
    }

    /// <summary>
    /// An item's dropdown is ~27 entries of which ~25 are the physics/audio/texture boilerplate
    /// every item carries. Splitting them into two headed groups puts the two or three functions
    /// that are actually specific to the item - TurnOn, EnableSteam, UntieShirt - at the top,
    /// which is what people are looking for when they already have an item in mind (issue #11).
    /// </summary>
    private void FillFunctions()
    {
        // Refilling rewrites FuncCombo.Text to preserve it, which raises TextChanged. Without the
        // flag that programmatic echo looks exactly like the user typing a function, and would
        // knock the view out of list mode every time the item changed.
        _refilling = true;
        try
        {
            FuncCombo.SetGroupedItems(
                Current == TargetMode.Item ? ItemOptions() : CharacterOptions(),
                nameof(FunctionOption.Group));
        }
        finally
        {
            _refilling = false;
        }
    }

    private IEnumerable<FunctionOption> ItemOptions()
    {
        if (!TryGetItemFunctions(out var itemName, out var functions))
        {
            // An item we have no table for - free-typed, or one of the 220 in itemDetails that
            // never appeared in an itemfunction dump - still supports the universal functions.
            // That is a better offer than the two-entry character list this used to fall back to.
            return Grouped(_data.CommonItemFunctions, CommonItemGroup);
        }

        return Grouped(functions.Where(f => !_data.CommonItemFunctions.Contains(f)), DisplayName(itemName))
            .Concat(Grouped(functions.Where(_data.CommonItemFunctions.Contains), CommonItemGroup));
    }

    private IEnumerable<FunctionOption> CharacterOptions()
    {
        // Per-character lists are sparse - see characterRunFunctions in values.json - so anyone
        // without a confirmed list of their own gets the shared one. Only meaningful for a single
        // target, which is what GetSingleSelectedCharacter already screens for.
        var character = Targets.GetSingleSelectedCharacter();
        if (character is not null && _characterRunFunctions.TryGetValue(character, out var own))
            return Grouped(own, $"{character} run functions");

        return Grouped(_data.RunFunctions, CharacterGroup);
    }

    /// <summary>Alphabetical within a group: the JSON order is whatever the dump emitted, which is
    /// no help at all once a group runs to 25 entries.</summary>
    private static IEnumerable<FunctionOption> Grouped(IEnumerable<string> functions, string group) =>
        functions.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                 .Select(f => new FunctionOption(f, group));

    private void ApplyTabContext()
    {
        FuncLabel.Text = Current == TargetMode.Item
            ? "Function name (the list follows the chosen item)"
            : "Function name (unbounded - type any known run function)";

        // The function and value inputs are deliberately never disabled here. Typing into them is
        // one of the two ways out of list mode, and disabling them made both the note below and
        // the only reset path in OnTextChanged unreachable - which is what left the app stuck
        // emitting run.list until restart (issue #10).
        ListButton.Content = _showList
            ? "Back to building a run() command"
            : "Build run.list command instead";
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

    public override void OnTargetsChanged()
    {
        // The character list can depend on who is selected, so it has to be rebuilt here too.
        if (Current == TargetMode.Character)
            FillFunctions();
    }

    /// <summary>Typing a function or value means the user is done looking at the list. Retyping
    /// the item does not: listing several items in a row is a reasonable thing to want, and the
    /// button toggles back regardless.</summary>
    protected override void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_showList && !_refilling && (ReferenceEquals(sender, FuncCombo) || ReferenceEquals(sender, ValueBox)))
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
        _showList = !_showList;
        ApplyTabContext();
        Recompute();
    }

    public override CommandResult BuildCommand() =>
        Current == TargetMode.Item ? BuildForItem() : BuildForCharacters();

    private CommandResult BuildForCharacters()
    {
        if (_showList)
            return WithTargets(RunCommandBuilder.BuildList);

        // EffectiveValue, not Text: the dropdown holds FunctionOption objects now, and this is
        // what resolves a picked one to its name while still honouring free-typed text.
        var function = FuncCombo.EffectiveValue;
        return string.IsNullOrWhiteSpace(function)
            ? CommandResult.NeedsInput("Type a run function name")
            : WithTargets(t => RunCommandBuilder.Build(t, function, ValueBox.Text));
    }

    private CommandResult BuildForItem()
    {
        var item = SelectedItemName();
        if (item.Length == 0)
            return CommandResult.NeedsInput("Pick an item");

        string[] target = [TargetHelper.ConsoleName(item)];

        if (_showList)
            return CommandResult.Ok(RunCommandBuilder.BuildList(target));

        var function = FuncCombo.EffectiveValue;
        return string.IsNullOrWhiteSpace(function)
            ? CommandResult.NeedsInput("Pick a function for this item")
            : CommandResult.Ok(RunCommandBuilder.Build(target, function, ValueBox.Text));
    }
}
