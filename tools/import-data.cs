#:property TargetFramework=net10.0
#:property PublishTrimmed=false
#:property Nullable=enable

// Regenerates HpCommander/Data/*.json from the reference dumps in docs/.
//
//   dotnet run tools/import-data.cs
//
// Deliberately not referencing HpCommander.csproj: this runs a handful of times ever, and the
// emitted files - not a shared DTO - are the contract. GameData.Load is what validates them.
//
// Sections the dumps cannot fill (states, properties, runFunctions, socialActions, genericValues,
// doors, items, quests, intimacy) are carried through from the existing files untouched. There is
// no bulk source for them; padding them with guesses would be worse than leaving them short.

using System.Text.Json;
using System.Text.Json.Nodes;

const int SchemaVersion = 2;

var root = FindRepoRoot();
var docs = Path.Combine(root, "docs");
var dataDir = Path.Combine(root, "HpCommander", "Data");

Console.WriteLine($"repo   {root}");
Console.WriteLine($"docs   {docs}");
Console.WriteLine($"data   {dataDir}");
Console.WriteLine();

var problems = new List<string>();

// Existing data is the baseline: anything the dumps don't cover survives regeneration.
var existing = LoadExisting(dataDir);
var characters = existing["characters"]!.AsArray().Select(n => n!.GetValue<string>()).ToList();
var realCharacters = characters.Where(c => c != "Player").ToList();

// ---------------- clothing ----------------

var clothingDump = ReadDoc("clothing-ids.json").AsObject();
var byId = new JsonObject();
var byCharacter = new SortedDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
var universal = new List<string>();

// "Amala_Earrings_Amy" is Amala's earrings *for* Amy: a leading character name is the style
// origin, the trailing one is the wearer. console-reference.md documents the same for
// arin_hair_amy. 256 ids have both, so which end wins is not academic.
var byToken = realCharacters.ToDictionary(c => c.Replace(" ", ""), c => c, StringComparer.OrdinalIgnoreCase);

foreach (var (id, node) in clothingDump)
{
    var entry = node!.AsObject();
    var friendly = entry["friendlyName"]!.GetValue<string>();
    var type = entry["type"]!.GetValue<string>();
    var pack = entry["pack"]!.GetValue<string>();

    byId[id] = new JsonObject
    {
        ["name"] = friendly,
        ["type"] = type,
        ["pack"] = pack,
    };

    var wearer = ResolveWearer(id);
    if (wearer is null)
        universal.Add(id);
    else
    {
        if (!byCharacter.TryGetValue(wearer, out var list))
            byCharacter[wearer] = list = [];
        list.Add(id);
    }
}

string? ResolveWearer(string id)
{
    var tokens = id.Split('_');
    for (var i = tokens.Length - 1; i >= 0; i--)
        if (byToken.TryGetValue(tokens[i], out var name))
            return name;
    return id.Contains("Player", StringComparison.OrdinalIgnoreCase) ? "Player" : null;
}

var clothing = new JsonObject
{
    // Marks the bucketing as inferred, so an in-game-verified import can supersede it later
    // without ambiguity about which entries were guesses.
    ["clothingSource"] = "heuristic",
    ["clothingById"] = byId,
    ["clothingByCharacter"] = ToArrayObject(byCharacter, universal),
};

Console.WriteLine($"clothing        {clothingDump.Count} ids -> {byCharacter.Count} characters + {universal.Count} universal");
foreach (var c in realCharacters.Concat(["Player"]))
    if (!byCharacter.ContainsKey(c))
        Console.WriteLine($"                note: {c} has no clothing (not a dressable character)");

// ---------------- world: locations + cutscenes ----------------

var locations = ReadDoc("locations.json").AsArray().DeepClone();
var cutsceneDump = ReadDoc("cutscenes.json").AsArray();

var cutscenes = new JsonArray();
foreach (var node in cutsceneDump)
{
    var cs = node!.AsObject();
    // pack / isSexScene / starGender were previously dropped; the Play tab can use them to warn
    // before the game silently refuses a scene.
    cutscenes.Add(new JsonObject
    {
        ["name"] = cs["name"]!.GetValue<string>(),
        ["npcs"] = cs["npcs"]!.GetValue<int>(),
        ["zone"] = cs["zone"]?.GetValue<string>() ?? "",
        ["pack"] = cs["pack"]?.GetValue<string>() ?? "",
        ["isSexScene"] = cs["isSexScene"]?.GetValue<bool>() ?? false,
        ["starGender"] = cs["starGender"]?.GetValue<string>() ?? "",
    });
}

var world = new JsonObject
{
    ["doors"] = existing["doors"]!.DeepClone(),
    ["doorActions"] = existing["doorActions"]!.DeepClone(),
    ["locations"] = locations,
    ["cutscenes"] = cutscenes,
};

Console.WriteLine($"locations       {locations.AsArray().Count}");
Console.WriteLine($"cutscenes       {cutscenes.Count} (now carrying pack/isSexScene/starGender)");

// ---------------- values: per-story ----------------

var playerValues = ReadDoc("player-values.json").AsObject().DeepClone();
var storyValues = ReadDoc("story-values.json").AsObject();

// Drop the blank entries the dump carries for characters with no values.
var characterValues = new JsonObject();
var storyValueCount = 0;
foreach (var (story, byChar) in storyValues)
{
    var cleanedStory = new JsonObject();
    foreach (var (character, names) in byChar!.AsObject())
    {
        var cleaned = new JsonArray();
        foreach (var n in names!.AsArray())
        {
            var v = n!.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(v)) { cleaned.Add(v); storyValueCount++; }
        }
        if (cleaned.Count > 0) cleanedStory[character] = cleaned;
    }
    if (cleanedStory.Count > 0) characterValues[story] = cleanedStory;
}

var values = new JsonObject
{
    ["traits"] = existing["traits"]!.DeepClone(),
    ["relationshipTypes"] = existing["relationshipTypes"]!.DeepClone(),
    ["genericValues"] = existing["genericValues"]!.DeepClone(),
    ["states"] = existing["states"]!.DeepClone(),
    ["properties"] = existing["properties"]!.DeepClone(),
    ["runFunctions"] = existing["runFunctions"]!.DeepClone(),
    ["playerValuesByStory"] = playerValues,
    ["characterValuesByStory"] = characterValues,
};

Console.WriteLine($"playerValues    {playerValues.AsObject().Sum(kv => kv.Value!.AsArray().Count)} across {playerValues.AsObject().Count} stories");
Console.WriteLine($"characterValues {storyValueCount} across {characterValues.Count} stories");

// ---------------- items: functions + story metadata ----------------

var itemFunctionsDump = ReadDoc("item-functions.json").AsObject();
var itemsFromStory = ReadDoc("items-from-story.json").AsObject();

var itemFunctions = new JsonObject();
var functionCount = 0;
foreach (var (item, fns) in itemFunctionsDump)
{
    var arr = new JsonArray();
    foreach (var f in fns!.AsArray()) { arr.Add(f!.GetValue<string>()); functionCount++; }
    itemFunctions[item] = arr;
}

var itemDetails = new JsonObject();
foreach (var (item, detail) in itemsFromStory)
{
    var d = detail!.AsObject();
    itemDetails[item] = new JsonObject
    {
        ["displayName"] = d["displayName"]?.GetValue<string>() ?? item,
        ["story"] = d["story"]?.GetValue<string>() ?? "",
    };
}

var items = new JsonObject
{
    ["items"] = existing["items"]!.DeepClone(),
    // `item <name> itemfunction <fn>` - a different command from the character-scoped run(),
    // so this is a new section rather than more runFunctions.
    ["itemFunctions"] = itemFunctions,
    ["itemDetails"] = itemDetails,
};

Console.WriteLine($"itemFunctions   {itemFunctions.Count} items / {functionCount} functions");
Console.WriteLine($"itemDetails     {itemDetails.Count}");

// ---------------- console: examples + achievements ----------------

var examplesDump = ReadDoc("console-examples.json").AsArray();

// Names that can appear where a verb would otherwise sit.
var subjects = characters
    .Select(c => c.Replace(" ", "").ToLowerInvariant())
    .Concat(["all", "player", "characters"])
    .ToHashSet();

var examples = new JsonArray();
foreach (var node in examplesDump)
{
    var e = node!.AsObject();
    var command = e["command"]?.GetValue<string>() ?? "";
    if (string.IsNullOrWhiteSpace(command)) continue;
    examples.Add(new JsonObject
    {
        ["command"] = command,
        ["description"] = e["description"]?.GetValue<string>() ?? "",
        ["verb"] = VerbOf(command, subjects),
    });
}

var achievements = ReadDoc("achievements.json").AsArray().DeepClone();

var social = new JsonObject
{
    ["socialValues"] = existing["socialValues"]!.DeepClone(),
    ["socialRelationships"] = existing["socialRelationships"]!.DeepClone(),
    ["socialModifiers"] = existing["socialModifiers"]!.DeepClone(),
    ["socialActions"] = existing["socialActions"]!.DeepClone(),
    ["legacyCombatActions"] = existing["legacyCombatActions"]!.DeepClone(),
    ["intimacy"] = existing["intimacy"]!.DeepClone(),
    // The console exposes only Achievements.clear, so this is a lookup table, not a command source.
    ["achievements"] = achievements,
    ["examples"] = examples,
};

var distinctVerbs = examples.Select(e => e!["verb"]!.GetValue<string>()).Distinct().Count();
Console.WriteLine($"examples        {examples.Count} across {distinctVerbs} verbs");
Console.WriteLine($"achievements    {achievements.AsArray().Count} (reference only)");

// ---------------- characters ----------------

var charactersFile = new JsonObject
{
    ["schemaVersion"] = SchemaVersion,
    ["characters"] = existing["characters"]!.DeepClone(),
    ["changeParts"] = existing["changeParts"]!.DeepClone(),
    ["sizeParts"] = existing["sizeParts"]!.DeepClone(),
    ["outfitsByCharacter"] = existing["outfitsByCharacter"]!.DeepClone(),
    // Roaming actions were a hardcoded array in MovementView.
    ["roamingActions"] = new JsonArray("list", "allow", "allowlocation", "prohibitlocation", "clearlists"),
};

var quests = new JsonObject { ["questsByStory"] = existing["questsByStory"]!.DeepClone() };

// ---------------- invariants ----------------

if (universal.Count > 30)
    problems.Add($"{universal.Count} clothing ids matched no character - the wearer rule may have broken.");

var previousClothing = existing["changeItemsByCharacter"]?.AsObject().Count ?? 0;
if (byCharacter.Count < previousClothing)
    problems.Add($"clothing coverage regressed: {byCharacter.Count} characters now vs {previousClothing} before.");

foreach (var (name, section) in new (string, JsonNode?)[]
         {
             ("characters", charactersFile["characters"]),
             ("locations", world["locations"]),
             ("cutscenes", world["cutscenes"]),
             ("clothingById", clothing["clothingById"]),
         })
{
    var count = section is JsonArray a ? a.Count : section is JsonObject o ? o.Count : 0;
    if (count == 0) problems.Add($"{name} is empty.");
}

// ---------------- write ----------------

Write(dataDir, "characters.json", charactersFile);
Write(dataDir, "clothing.json", clothing);
Write(dataDir, "values.json", values);
Write(dataDir, "items.json", items);
Write(dataDir, "world.json", world);
Write(dataDir, "social.json", social);
Write(dataDir, "quests.json", quests);

Console.WriteLine();
if (problems.Count > 0)
{
    Console.WriteLine("FAILED:");
    foreach (var p in problems) Console.WriteLine("  " + p);
    return 1;
}

Console.WriteLine("All invariants held. Review the diff before committing.");
return 0;

// ---------------- helpers ----------------

JsonNode ReadDoc(string name)
{
    var path = Path.Combine(docs, name);
    return JsonNode.Parse(File.ReadAllText(path))
           ?? throw new InvalidDataException($"{name} is empty.");
}

/// <summary>
/// The verb an example demonstrates, so a view can look up its own examples.
/// Three shapes to get past: V1 verb-first ("warpto player kitchen" -> warpto), V2 dotted and
/// target-first ("Rachael.Amy.change(top) = True" -> change), and the V1 subject-first forms the
/// game's own help uses ("frank passout" -> passout), since token order is not significant there.
/// </summary>
static string VerbOf(string command, IReadOnlySet<string> subjects)
{
    foreach (var raw in command.Split(' ', StringSplitOptions.RemoveEmptyEntries))
    {
        var token = raw;
        var paren = token.IndexOf('(');
        if (paren >= 0) token = token[..paren];
        var dot = token.LastIndexOf('.');
        if (dot >= 0) token = token[(dot + 1)..];

        token = token.Trim().ToLowerInvariant();
        if (token.Length == 0 || subjects.Contains(token))
            continue;   // a cast member, not the command
        return token;
    }
    return "";
}

static JsonObject ToArrayObject(SortedDictionary<string, List<string>> source, List<string> universal)
{
    var result = new JsonObject();
    foreach (var (key, ids) in source)
    {
        var arr = new JsonArray();
        foreach (var id in ids) arr.Add(id);
        result[key] = arr;
    }
    // "*" is appended to every character's list by the Change view.
    var shared = new JsonArray();
    foreach (var id in universal) shared.Add(id);
    result["*"] = shared;
    return result;
}

static JsonObject LoadExisting(string dataDir)
{
    var merged = new JsonObject();
    foreach (var file in Directory.EnumerateFiles(dataDir, "*.json"))
    {
        var obj = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
        foreach (var (key, value) in obj.ToList())
        {
            obj.Remove(key);
            merged[key] = value;
        }
    }
    return merged;
}

static void Write(string dir, string name, JsonObject content)
{
    var json = content.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(Path.Combine(dir, name), json + Environment.NewLine);
    Console.WriteLine($"wrote {name,-18} {json.Length / 1024.0,7:F1} KB");
}

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "docs")))
        dir = dir.Parent;
    return dir?.FullName ?? throw new DirectoryNotFoundException("Could not find the repo root (no docs/ folder above the cwd).");
}
