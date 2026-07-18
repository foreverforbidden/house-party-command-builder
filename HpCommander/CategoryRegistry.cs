using System.Diagnostics;
using HpCommander.Views;

namespace HpCommander;

/// <summary>
/// The single source of truth for the sidebar. Name, search keywords and view factory live on
/// one line each; previously they were spread across a Nav array and a separate switch keyed by
/// the same magic string, where a typo silently fell through to a placeholder with no compile error.
/// </summary>
public static class CategoryRegistry
{
    public static readonly IReadOnlyList<NavEntry> All =
    [
        NavEntry.Header("APPEARANCE"),
        NavEntry.Item("Change", c => new ChangeView(c.Data, c.Targets),
            "clothing", "clothes", "top", "bottom", "underwear", "undershirt", "naked", "undress", "hair", "accessories", "glasses", "shoes", "strapon"),
        NavEntry.Item("Outfit", c => new OutfitView(c.Data, c.Targets),
            "clothes", "dress", "costume", "default"),
        NavEntry.Item("Size", c => new SizeView(c.Data, c.Targets),
            "scale", "body", "head", "chest", "grow", "shrink"),

        NavEntry.Header("CHARACTER"),
        NavEntry.Item("Values", c => new ValuesView(c.Data, c.Targets),
            "trait", "relationship", "friendship", "romance", "exhibitionism", "jealous", "list", "filter"),
        NavEntry.Item("Social", c => new SocialView(c.Data),
            "drunk", "mood", "friendship", "romance", "sendtext", "talkto"),
        NavEntry.Item("States", c => new StatesView(c.Data, c.Targets),
            "sweating", "dance", "fire", "erect"),
        NavEntry.Item("Properties", c => new PropertiesView(c.Data, c.Targets),
            "bloody"),
        NavEntry.Item("Intimacy", c => new IntimacyView(c.Data),
            "sex", "sexualact", "masturbation", "act", "speed"),

        NavEntry.Header("ACTIONS"),
        NavEntry.Item("Movement", c => new MovementView(c.Data),
            "walk", "walkto", "warp", "warpto", "teleport", "tp", "roam", "roaming", "turn", "location", "coordinates", "overtime"),
        NavEntry.Item("Addforce", c => new AddforceView(c.Targets),
            "push", "force", "physics", "launch"),
        NavEntry.Item("Run", c => new RunView(c.Data, c.Targets),
            "function", "effect", "animation"),

        NavEntry.Header("WORLD"),
        NavEntry.Item("Inventory", c => new InventoryView(c.Data),
            "item", "give", "spawn", "beer", "nattylite", "condom", "key"),
        NavEntry.Item("Door", c => new DoorView(c.Data),
            "lock", "unlock", "open", "close"),
        NavEntry.Item("Cutscene", c => new CutsceneView(c.Data),
            "scene", "playscene", "sex", "play", "random", "endscene"),
        NavEntry.Item("Quest", c => new QuestView(c.Data),
            "story", "start", "complete", "increment", "fail", "mission"),
        NavEntry.Item("Time", _ => new TimeView(),
            "timescale", "slow", "fast", "speed"),

        NavEntry.Header("CONSOLE"),
        NavEntry.Item("Misc", c => new MiscView(c.Targets),
            "achievements", "unstuck", "help", "clear"),
        NavEntry.Item("Legacy (V1)", c => new LegacyView(c.Data),
            "enablenpc", "disablenpc", "npc", "combat", "fight", "passout", "wakeup", "setenabled", "enable", "disable"),
        NavEntry.Item("Info", _ => new InfoView(),
            "about", "readme", "issue", "report", "bug", "version"),
    ];

    static CategoryRegistry()
    {
        Debug.Assert(All.Where(e => !e.IsHeader).All(e => e.Factory != null), "every category needs a factory");
        Debug.Assert(All.Select(e => e.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == All.Count,
            "category names must be unique");
    }
}
