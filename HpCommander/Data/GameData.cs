using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
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

public sealed class GameDoor
{
    [JsonPropertyName("display")] public string Display { get; set; } = "";

    /// <summary>The console-normalised form, e.g. "frontdoor", used as the value-object name.</summary>
    [JsonPropertyName("console")] public string Console { get; set; } = "";

    public override string ToString() => Display.Length > 0 ? Display : Console;
}

public sealed class DoorAction
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";

    /// <summary>The V2 door value property: IsOpen (open/close) or IsLocked (lock/unlock).</summary>
    [JsonPropertyName("property")] public string Property { get; set; } = "";

    [JsonPropertyName("value")] public int Value { get; set; }

    public override string ToString() => Name;
}

public sealed class Cutscene
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("npcs")] public int Npcs { get; set; }
    [JsonPropertyName("zone")] public string Zone { get; set; } = "";

    public override string ToString() =>
        Zone.Length > 0 ? $"{Name}  ({Npcs} NPC, {Zone})" : $"{Name}  ({Npcs} NPC)";
}

public sealed class GameLocation
{
    /// <summary>The form the console accepts, e.g. "hottubseat1".</summary>
    [JsonPropertyName("consoleName")] public string ConsoleName { get; set; } = "";

    /// <summary>How the game data spells it, e.g. "HotTub Seat 1".</summary>
    [JsonPropertyName("displayName")] public string DisplayName { get; set; } = "";

    public override string ToString() => DisplayName.Length > 0 ? DisplayName : ConsoleName;
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
    [JsonPropertyName("doorActions")] public List<DoorAction> DoorActions { get; set; } = new();
    [JsonPropertyName("doors")] public List<GameDoor> Doors { get; set; } = new();
    [JsonPropertyName("cutscenes")] public List<Cutscene> Cutscenes { get; set; } = new();
    /// <summary>Quests are story-specific: story -> character -> quest names.</summary>
    [JsonPropertyName("questsByStory")] public Dictionary<string, Dictionary<string, List<string>>> QuestsByStory { get; set; } = new();

    /// <summary>Walk/warp destinations. Also includes character and item names,
    /// since those are valid movement targets too.</summary>
    [JsonPropertyName("locations")] public List<GameLocation> Locations { get; set; } = new();
    [JsonPropertyName("intimacy")] public IntimacyCatalog Intimacy { get; set; } = new();

    /// <summary>Bumped when the on-disk shape changes. A mismatch is a hard error: a renamed or
    /// mistyped key otherwise deserializes to an empty list and shows up only as an empty combo,
    /// which is very plausibly how several sections rotted unnoticed.</summary>
    public const int ExpectedSchemaVersion = 2;

    [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static string DefaultDirectory =>
        Path.Combine(AppContext.BaseDirectory, "Data");

    /// <summary>
    /// Reads every Data/*.json and merges their top-level properties into one object before
    /// deserializing. Splitting by domain keeps diffs reviewable once the generated data is large,
    /// and costs the views nothing: they still see a single flat GameData.
    /// </summary>
    public static GameData Load(string? directory = null)
    {
        directory ??= DefaultDirectory;

        var merged = new JsonObject();
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.EnumerateFiles(directory, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            var node = JsonNode.Parse(File.ReadAllText(file), documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
            });

            if (node is not JsonObject obj)
                throw new InvalidDataException($"{Path.GetFileName(file)}: expected a JSON object at the top level.");

            foreach (var (key, value) in obj.ToList())
            {
                if (seen.TryGetValue(key, out var owner))
                    throw new InvalidDataException(
                        $"'{key}' is defined in both {owner} and {Path.GetFileName(file)}.");

                seen[key] = Path.GetFileName(file);
                obj.Remove(key);
                merged[key] = value;
            }
        }

        var data = JsonSerializer.Deserialize<GameData>(merged.ToJsonString(), SerializerOptions) ?? new GameData();

        if (data.SchemaVersion != ExpectedSchemaVersion)
            throw new InvalidDataException(
                $"Data schemaVersion is {data.SchemaVersion}, expected {ExpectedSchemaVersion}. " +
                "The Data folder is from a different version of the app.");

        if (data.Characters.Count == 0)
            throw new InvalidDataException("No characters loaded - the Data folder looks incomplete.");

        return data;
    }
}
