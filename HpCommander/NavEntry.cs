namespace HpCommander;

/// <summary>A sidebar entry: either a group header or a selectable category with search keywords.</summary>
public sealed class NavEntry
{
    public string Name { get; }
    public bool IsHeader { get; }
    public string[] Keywords { get; }

    private NavEntry(string name, bool isHeader, string[] keywords)
    {
        Name = name;
        IsHeader = isHeader;
        Keywords = keywords;
    }

    public static NavEntry Header(string name) => new(name, true, Array.Empty<string>());

    public static NavEntry Item(string name, params string[] keywords) => new(name, false, keywords);

    public bool Matches(string query) =>
        Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        Keywords.Any(k => k.Contains(query, StringComparison.OrdinalIgnoreCase));
}
