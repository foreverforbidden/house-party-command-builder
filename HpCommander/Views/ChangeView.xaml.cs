using System.Windows;
using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class ChangeView : TargetedCommandCategoryViewBase
{
    private readonly GameData _data;

    // Decorated dropdown label -> the id the console wants.
    private readonly Dictionary<string, string> _idByLabel = new(StringComparer.OrdinalIgnoreCase);

    private bool _showList;

    public ChangeView(GameData data, CharacterChipPicker targets) : base(targets)
    {
        InitializeComponent();
        _data = data;

        ClothingSourceNote.Text = string.Equals(_data.ClothingSource, "heuristic", StringComparison.OrdinalIgnoreCase)
            ? "Which character a garment is filed under is inferred from its ID and may occasionally " +
              "be wrong. Tick the box above to search the full catalogue, or type an ID directly."
            : "";

        OnTargetsChanged();
    }

    public override void OnTargetsChanged() => RefreshItems();

    private void ShowAll_Changed(object sender, RoutedEventArgs e)
    {
        RefreshItems();
        Recompute();
    }

    /// <summary>
    /// Slot keywords first, then clothing. The per-character list is a heuristic bucketing, so the
    /// checkbox exposes the whole catalogue: a mis-filed garment must always still be reachable,
    /// otherwise a wrong guess looks like a missing feature.
    /// </summary>
    private void RefreshItems()
    {
        _idByLabel.Clear();

        var character = Targets.GetSingleSelectedCharacter();
        IEnumerable<string> ids;

        if (ShowAllClothingCheck.IsChecked == true || character is null)
        {
            ids = _data.ClothingById.Keys;
        }
        else
        {
            var own = _data.ClothingByCharacter.TryGetValue(character, out var mine) ? mine : [];
            var shared = _data.ClothingByCharacter.TryGetValue("*", out var all) ? all : [];
            ids = own.Concat(shared);
        }

        var labelled = ids
            .Select(id => (Id: id, Label: Describe(id)))
            .OrderBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var (id, label) in labelled)
            _idByLabel[label] = id;

        RefillPreservingText(PartCombo, _data.ChangeParts.Concat(labelled.Select(x => x.Label)));
    }

    private string Describe(string id)
    {
        if (!_data.ClothingById.TryGetValue(id, out var item))
            return id;
        var label = $"{item.Name} - {item.Type}";
        if (item.Pack.Length > 0 && !item.Pack.Equals("Base", StringComparison.OrdinalIgnoreCase))
            label += $" [{item.Pack}]";
        return $"{label}  ({id})";
    }

    /// <summary>Typing a slot or item means the user is done looking at the list.</summary>
    protected override void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _showList = false;
        base.OnTextChanged(sender, e);
    }

    private void ListButton_Click(object sender, RoutedEventArgs e)
    {
        _showList = true;
        Recompute();
    }

    public override CommandResult BuildCommand()
    {
        if (_showList)
            return WithTargets(ChangeCommandBuilder.BuildList);

        var typed = PartCombo.Text.Trim();
        if (string.IsNullOrWhiteSpace(typed))
            return CommandResult.NeedsInput("Pick a clothing slot or item ID");

        // A picked row resolves through its decorated label; a free-typed id passes straight through.
        var value = _idByLabel.TryGetValue(typed, out var id) ? id : typed;

        var mode = TrueRadio.IsChecked == true ? BoolMode.ForceTrue
            : FalseRadio.IsChecked == true ? BoolMode.ForceFalse
            : BoolMode.Toggle;
        return WithTargets(t => ChangeCommandBuilder.Build(t, value, mode));
    }
}
