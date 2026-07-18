namespace HpCommander.Builders;

public enum BoolMode
{
    Toggle,
    ForceTrue,
    ForceFalse,
}

public static class TargetHelper
{
    public const string AllCharactersTarget = "characters";

    public static string Join(IEnumerable<string> targets)
    {
        var list = targets.ToList();
        if (list.Count == 0)
            throw new InvalidOperationException("At least one target must be selected.");
        return string.Join(".", list);
    }

    public static string BoolSuffix(BoolMode mode) => mode switch
    {
        BoolMode.ForceTrue => " = True",
        BoolMode.ForceFalse => " = False",
        _ => "",
    };
}

public static class ChangeCommandBuilder
{
    public static string Build(IEnumerable<string> targets, string partOrItemId, BoolMode boolMode) =>
        $"{TargetHelper.Join(targets)}.change({partOrItemId}){TargetHelper.BoolSuffix(boolMode)}";

    public static string BuildList(IEnumerable<string> targets) =>
        $"{TargetHelper.Join(targets)}.change.list";
}

public static class OutfitCommandBuilder
{
    public static string Build(IEnumerable<string> targets, string outfitId) =>
        $"{TargetHelper.Join(targets)}.outfit({outfitId})";
}

public static class InventoryCommandBuilder
{
    public static string BuildPlain(string itemName, int? number = null) =>
        $"Inventory.{itemName}{(number.HasValue ? number.Value.ToString() : "")} = true";

    public static string[] BuildRequiresEnable(string enableName, string displayName)
    {
        var quotedEnableName = enableName.Contains(' ') ? $"\"{enableName}\"" : enableName;
        return new[]
        {
            $"Item {quotedEnableName} setenabled true",
            $"{displayName}.inventory = True",
        };
    }
}

public static class ValuesCommandBuilder
{
    public static string BuildTrait(IEnumerable<string> targets, string trait, double n) =>
        $"{TargetHelper.Join(targets)}.values.set(trait:{trait}) = {n}";

    public static string BuildRelationship(IEnumerable<string> targets, string otherCharacter, string relationshipType, double n) =>
        $"{TargetHelper.Join(targets)}.values.set(Relationship:{otherCharacter}:{relationshipType}) = {n}";

    public static string BuildGeneric(string objectId, string property, int boolValue) =>
        $"values.{objectId}.set({property})={boolValue}";

    public static string BuildList(IEnumerable<string> targets, string? filter) =>
        string.IsNullOrWhiteSpace(filter)
            ? $"{TargetHelper.Join(targets)}.values.list"
            : $"{TargetHelper.Join(targets)}.values.list.filter({filter})";
}

public static class StatesCommandBuilder
{
    public static string Build(IEnumerable<string> targets, string state) =>
        $"{TargetHelper.Join(targets)}.states({state})";
}

public static class PropertiesCommandBuilder
{
    public static string Build(IEnumerable<string> targets, string property) =>
        $"{TargetHelper.Join(targets)}.properties({property})";

    public static string BuildList(IEnumerable<string> targets) =>
        $"{TargetHelper.Join(targets)}.properties.list";
}

public static class RunCommandBuilder
{
    public static string Build(IEnumerable<string> targets, string function) =>
        $"{TargetHelper.Join(targets)}.run({function})";
}

public static class SizeCommandBuilder
{
    public static string BuildWhole(IEnumerable<string> targets, double scale) =>
        $"{TargetHelper.Join(targets)}.size = {FormatScale(scale)}";

    public static string BuildPart(IEnumerable<string> targets, string part, double scale) =>
        $"{TargetHelper.Join(targets)}.size({part}) = {FormatScale(scale)}";

    internal static string FormatScale(double scale) => scale.ToString("0.###");
}

public static class TimeCommandBuilder
{
    public static string BuildScale(double scale) => $"time.scale = {SizeCommandBuilder.FormatScale(scale)}";
}

public static class AddforceCommandBuilder
{
    public static string Build(IEnumerable<string> targets, int rightLeft, int upDown, int forwardBackward) =>
        $"{TargetHelper.Join(targets)}.addforce({rightLeft}:{upDown}:{forwardBackward})";
}

public static class SimpleCommandBuilder
{
    public const string AchievementsClear = "Achievements.clear";

    public static string Unstuck(string target) => $"{target}.unstuck";

    public static string HelpV2 => "Help.V2";
}

public static class IntimacyCommandBuilder
{
    public static string StartTwoCharacterAct(string char1, string char2, string eventName) =>
        $"sexualact intimacy {char1} {char2} {eventName}";

    public static string StartSingleCharacterAct(string character, string eventName) =>
        $"sexualact {character} {eventName} intimacy";

    public static string EndAct(string character) =>
        $"sexualact intimacy {character} end";

    public static string ActionSpeed(string character, string subcommand) =>
        $"intimacy {character} {subcommand}";

    public static string ResetGuess(string character) =>
        $"intimacy {character} reset";
}

public static class LegacyCommandBuilder
{
    public static string ItemSetEnabled(string itemName)
    {
        var quoted = itemName.Contains(' ') ? $"\"{itemName}\"" : itemName;
        return $"Item {quoted} setenabled true";
    }

    /// <summary>passout | wakeup | cancel. <paramref name="character"/> may be "all".</summary>
    public static string Combat(string character, string subcommand) =>
        $"combat {character} {subcommand}";

    /// <summary>An empty <paramref name="target"/> is only valid for the "all" free-for-all form.</summary>
    public static string CombatFight(string attacker, string? target) =>
        string.IsNullOrWhiteSpace(target)
            ? $"combat {attacker} fight"
            : attacker.Equals(CombatAllTarget, StringComparison.OrdinalIgnoreCase)
                ? $"combat all fight {target}"
                : $"combat {attacker} {target} fight";

    public static string SetNpcEnabled(string character, bool enabled) =>
        $"{(enabled ? "EnableNPC" : "DisableNPC")} {character}";

    public const string CombatAllTarget = "all";
    public const string HelpIntimacy = "help intimacy";
    public const string ExampleIntimacy = "example intimacy";
}

public static class MovementCommandBuilder
{
    public const string AllTarget = "all";

    /// <summary>Instant teleport. Destination is another character, a location name, or "x y z"
    /// coordinates. Mirrors `warpto vickie player`, `warpto frank bedleft`, `warpto player 11 0 12`.</summary>
    public static string WarpTo(string character, string destination) =>
        $"warpto {character} {destination}";

    /// <summary>Mirrors `warpto player 11 0 12` (also the `0 0 0` stuck-fix).</summary>
    public static string WarpToCoords(string character, int x, int y, int z) =>
        $"warpto {character} {x} {y} {z}";

    /// <summary>Pathfind to a destination. Mirrors `walkto brittney hottubseat1`,
    /// `walkto all outside cancel`.</summary>
    public static string WalkTo(string character, string destination, bool cancel) =>
        cancel ? $"walkto {character} {destination} cancel" : $"walkto {character} {destination}";

    /// <summary>Mirrors `warpovertime vickie player 3`.</summary>
    public static string WarpOverTime(string character, string destination, double seconds) =>
        $"warpovertime {character} {destination} {seconds.ToString("0.###")}";

    /// <summary>Mirrors `turn derek around`.</summary>
    public static string TurnAround(string character) =>
        $"turn {character} around";

    /// <summary>Mirrors `turn rachael toward player`, `turn katherine toward toaster true`.</summary>
    public static string TurnToward(string character, string target, bool instant) =>
        instant ? $"turn {character} toward {target} true" : $"turn {character} toward {target}";

    /// <summary>Mirrors `roaming derek list`.</summary>
    public static string RoamingList(string character) =>
        $"roaming {character} list";

    /// <summary>Mirrors `roaming all allow false`. <paramref name="character"/> may be "all".</summary>
    public static string RoamingAllow(string character, bool allow) =>
        $"roaming {character} allow {(allow ? "true" : "false")}";

    /// <summary>Mirrors `roaming derek allowlocation hottub`.</summary>
    public static string RoamingAllowLocation(string character, string destination) =>
        $"roaming {character} allowlocation {destination}";

    /// <summary>Mirrors `derek prohibitlocation roaming stephanie` (verb-first form).</summary>
    public static string RoamingProhibitLocation(string character, string destination) =>
        $"roaming {character} prohibitlocation {destination}";

    /// <summary>Mirrors `vickie roaming clearlists` (verb-first form).</summary>
    public static string RoamingClearLists(string character) =>
        $"roaming {character} clearlists";
}

public static class SocialCommandBuilder
{
    public const string AllTarget = "all";

    /// <summary>drunk | mood. Mirrors `social all drunk 25 equals`. <paramref name="character"/> may be "all".</summary>
    public static string Value(string character, string value, int amount, string modifier) =>
        $"social {character} {value} {amount} {modifier}";

    /// <summary>friendship | romance. Mirrors `social rachael player romance equals 10`.</summary>
    public static string Relationship(string character, string target, string relationship, string modifier, int amount) =>
        $"social {character} {target} {relationship} {modifier} {amount}";

    /// <summary>Mirrors `social amy sendtext`.</summary>
    public static string Action(string character, string action) =>
        $"social {character} {action}";

    /// <summary>Mirrors `social madison derek talkto`.</summary>
    public static string ActionWithTarget(string character, string target, string action) =>
        $"social {character} {target} {action}";
}

public static class DoorCommandBuilder
{
    /// <summary>Doors are V2 (per the game devs, issue #2): `values.{door}.set(IsLocked)=1/0`
    /// for lock/unlock and `IsOpen` for open/close. The door id is the console-normalised
    /// name, e.g. "frontdoor".</summary>
    public static string Build(string doorConsole, string property, int value) =>
        $"values.{doorConsole}.set({property})={value}";

    /// <summary>Normalise a free-typed door name to the console form: "Front Door" -> "frontdoor".</summary>
    public static string Normalise(string name) =>
        System.Text.RegularExpressions.Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]", "");
}

public static class CutsceneCommandBuilder
{
    /// <summary>Mirrors `cutscene playscene PlayerMasterBedroomSex1 player amy`. The cast is the
    /// star followed by each NPC; the scene needs the right NPC count to play.</summary>
    public static string PlayScene(string scene, string star, IEnumerable<string> npcs)
    {
        var cast = new List<string> { star };
        cast.AddRange(npcs.Where(n => !string.IsNullOrWhiteSpace(n)));
        return $"cutscene playscene {scene} {string.Join(" ", cast)}";
    }

    /// <summary>Mirrors `cutscene endscene PlayerMasterBedroomSex1`.</summary>
    public static string EndScene(string scene) => $"cutscene endscene {scene}";

    /// <summary>Mirrors `cutscene EndAnySceneWithPlayer`.</summary>
    public const string EndAnyWithPlayer = "cutscene EndAnySceneWithPlayer";

    /// <summary>Mirrors `playrandomscenefromlocation masterbedroomzone cutscene player rachael`.</summary>
    public static string RandomFromLocation(string zone, string other) =>
        $"playrandomscenefromlocation {zone} cutscene player {other}";
}

public static class QuestCommandBuilder
{
    /// <summary>start | fail | increment | complete. Quest names are quoted (they contain spaces/apostrophes).</summary>
    public static string Manage(string subcommand, string questName) =>
        $"quest {subcommand} \"{questName}\"";

    public static string List(string character) =>
        $"quest list {character}";
}
