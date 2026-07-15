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

    public static string Combat(string target, string action) =>
        $"Combat {target} {action}";

    public static string EnableNpc(string character) =>
        $"EnableNPC {character}";

    public const string HelpIntimacy = "help intimacy";
    public const string ExampleIntimacy = "example intimacy";
}
