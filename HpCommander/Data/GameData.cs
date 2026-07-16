using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HpCommander.Data;

public sealed class IdLabel
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    public override string ToString() => Label;
}

public sealed class ItemAlias
{
    [JsonPropertyName("display")] public string Display { get; set; } = "";
    [JsonPropertyName("enableName")] public string? EnableName { get; set; }
    [JsonPropertyName("internal")] public string? Internal { get; set; }
    public override string ToString() => Display;
}

public sealed class NumberedItem
{
    [JsonPropertyName("baseName")] public string BaseName { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("min")] public int Min { get; set; }
    [JsonPropertyName("max")] public int Max { get; set; }
}

public sealed class GenericValue
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("property")] public string Property { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    public override string ToString() => Label;
}

public sealed class IntimacyEntry
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("id")] public int? Id { get; set; }
    public override string ToString() => Id.HasValue ? $"{Name} ({Id})" : Name;
}

public sealed class IntimacyCatalog
{
    [JsonPropertyName("note")] public string Note { get; set; } = "";
    [JsonPropertyName("subcommands")] public List<IntimacyEntry> Subcommands { get; set; } = new();
    [JsonPropertyName("events")] public List<IntimacyEntry> Events { get; set; } = new();
}

public sealed class SocialAction
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("needsTarget")] public bool NeedsTarget { get; set; }
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    public override string ToString() => Label.Length > 0 ? Label : Name;
}

public sealed class ItemCatalog
{
    [JsonPropertyName("requiresEnable")] public List<ItemAlias> RequiresEnable { get; set; } = new();
    [JsonPropertyName("aliasNames")] public List<ItemAlias> AliasNames { get; set; } = new();
    [JsonPropertyName("plain")] public List<string> Plain { get; set; } = new();
    [JsonPropertyName("numberedRange")] public List<NumberedItem> NumberedRange { get; set; } = new();
    [JsonPropertyName("locked")] public List<string> Locked { get; set; } = new();
}

public sealed class GameData
{
    [JsonPropertyName("characters")] public List<string> Characters { get; set; } = new();
    [JsonPropertyName("changeParts")] public List<string> ChangeParts { get; set; } = new();
    [JsonPropertyName("changeItemsByCharacter")] public Dictionary<string, List<IdLabel>> ChangeItemsByCharacter { get; set; } = new();
    [JsonPropertyName("outfitsByCharacter")] public Dictionary<string, List<string>> OutfitsByCharacter { get; set; } = new();
    [JsonPropertyName("traits")] public List<string> Traits { get; set; } = new();
    [JsonPropertyName("relationshipTypes")] public List<string> RelationshipTypes { get; set; } = new();
    [JsonPropertyName("genericValues")] public List<GenericValue> GenericValues { get; set; } = new();
    [JsonPropertyName("states")] public List<string> States { get; set; } = new();
    [JsonPropertyName("properties")] public List<string> Properties { get; set; } = new();
    [JsonPropertyName("sizeParts")] public List<string> SizeParts { get; set; } = new();
    [JsonPropertyName("runFunctions")] public List<string> RunFunctions { get; set; } = new();
    [JsonPropertyName("items")] public ItemCatalog Items { get; set; } = new();
    [JsonPropertyName("legacyCombatActions")] public List<string> LegacyCombatActions { get; set; } = new();
    [JsonPropertyName("socialValues")] public List<string> SocialValues { get; set; } = new();
    [JsonPropertyName("socialRelationships")] public List<string> SocialRelationships { get; set; } = new();
    [JsonPropertyName("socialModifiers")] public List<string> SocialModifiers { get; set; } = new();
    [JsonPropertyName("socialActions")] public List<SocialAction> SocialActions { get; set; } = new();
    [JsonPropertyName("doorActions")] public List<string> DoorActions { get; set; } = new();
    [JsonPropertyName("doors")] public List<string> Doors { get; set; } = new();
    [JsonPropertyName("questsByCharacter")] public Dictionary<string, List<string>> QuestsByCharacter { get; set; } = new();
    [JsonPropertyName("intimacy")] public IntimacyCatalog Intimacy { get; set; } = new();

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static string DefaultPath =>
        Path.Combine(AppContext.BaseDirectory, "Data", "commands.json");

    public static GameData Load(string? path = null)
    {
        path ??= DefaultPath;
        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<GameData>(json, SerializerOptions);
        return data ?? new GameData();
    }
}
