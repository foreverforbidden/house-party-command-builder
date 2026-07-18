using HpCommander.Controls;
using HpCommander.Data;
using HpCommander.Views;

namespace HpCommander;

/// <summary>Everything a category view needs in order to be constructed.</summary>
public sealed record ViewContext(GameData Data, CharacterChipPicker Targets);

/// <summary>A sidebar entry: either a group header, or a selectable category with its search
/// keywords and the factory that builds its view.</summary>
public sealed class NavEntry
{
    public string Name { get; }
    public bool IsHeader { get; }
    public string[] Keywords { get; }

    /// <summary>Null for headers only.</summary>
    public Func<ViewContext, CommandCategoryViewBase>? Factory { get; }

    private NavEntry(string name, bool isHeader, string[] keywords, Func<ViewContext, CommandCategoryViewBase>? factory)
    {
        Name = name;
        IsHeader = isHeader;
        Keywords = keywords;
        Factory = factory;
    }

    public static NavEntry Header(string name) => new(name, true, [], null);

    public static NavEntry Item(string name, Func<ViewContext, CommandCategoryViewBase> factory, params string[] keywords) =>
        new(name, false, keywords, factory);

    public bool Matches(string query) =>
        Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        Keywords.Any(k => k.Contains(query, StringComparison.OrdinalIgnoreCase));
}
