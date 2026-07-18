using System.Globalization;
using HpCommander.Builders;
using Xunit;

namespace HpCommander.Builders.Tests;

/// <summary>
/// Deliberately narrow. Most builders are one interpolated string, so asserting their output
/// only restates the format - the real ground truth for syntax is docs/console-reference.md,
/// which was checked against the game's own help/example output. These cover the places where
/// there is actual logic: a three-way branch, a normaliser, quoting, and culture handling.
/// </summary>
public class BuilderTests
{
    // ---------------- Culture ----------------
    // A comma-decimal locale used to emit "Amy.size = 1,5", which the console will not parse.

    /// <summary>Runs an action with the current culture forced, then restores it.</summary>
    private static void InCulture(string name, Action body)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(name);
            body();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("pl-PL")]
    [InlineData("fr-FR")]
    [InlineData("en-US")]
    [InlineData("invariant")]
    public void NumbersUseInvariantDecimalSeparatorInEveryCulture(string culture)
    {
        void Assertions()
        {
            string[] targets = ["Amy"];
            Assert.Equal("Amy.size = 1.5", SizeCommandBuilder.BuildWhole(targets, 1.5));
            Assert.Equal("Amy.size(head) = 0.75", SizeCommandBuilder.BuildPart(targets, "head", 0.75));
            Assert.Equal("time.scale = 0.25", TimeCommandBuilder.BuildScale(0.25));
            Assert.Equal("warpovertime vickie player 3.5", MovementCommandBuilder.WarpOverTime("vickie", "player", 3.5));
            Assert.Equal("Amy.values.set(trait:Nice) = 5.5", ValuesCommandBuilder.BuildTrait(targets, "Nice", 5.5));
            Assert.Equal("Amy.values.set(Relationship:player:romance) = 7.5",
                ValuesCommandBuilder.BuildRelationship(targets, "player", "romance", 7.5));
        }

        if (culture == "invariant") Assertions();
        else InCulture(culture, Assertions);
    }

    [Fact]
    public void WholeNumbersDoNotGainATrailingZero()
    {
        Assert.Equal("Amy.size = 2", SizeCommandBuilder.BuildWhole(["Amy"], 2.0));
    }

    // ---------------- CombatFight: the one genuine three-way branch ----------------

    [Fact]
    public void CombatFightWithNoTargetIsAFreeForAll() =>
        Assert.Equal("combat all fight", LegacyCommandBuilder.CombatFight("all", null));

    [Fact]
    public void CombatFightWithBlankTargetIsAlsoAFreeForAll() =>
        Assert.Equal("combat all fight", LegacyCommandBuilder.CombatFight("all", "   "));

    [Fact]
    public void CombatFightFromAllAgainstOneTargetPutsTheTargetLast() =>
        Assert.Equal("combat all fight derek", LegacyCommandBuilder.CombatFight("all", "derek"));

    [Fact]
    public void CombatFightBetweenTwoCharactersPutsTheVerbLast() =>
        Assert.Equal("combat rachael derek fight", LegacyCommandBuilder.CombatFight("rachael", "derek"));

    [Fact]
    public void CombatFightTreatsAllCaseInsensitively() =>
        Assert.Equal("combat all fight derek", LegacyCommandBuilder.CombatFight("ALL", "derek"));

    // ---------------- Door name normalisation ----------------

    [Theory]
    [InlineData("Front Door", "frontdoor")]
    [InlineData("frontdoor", "frontdoor")]
    [InlineData("Bedroom Door #2", "bedroomdoor2")]
    [InlineData("  Spaced  Out  ", "spacedout")]
    [InlineData("A-B_C.D", "abcd")]
    [InlineData("", "")]
    public void NormaliseStripsEverythingButLettersAndDigits(string input, string expected) =>
        Assert.Equal(expected, DoorCommandBuilder.Normalise(input));

    // ---------------- Quoting names that contain spaces ----------------

    [Fact]
    public void ItemNamesWithSpacesAreQuoted() =>
        Assert.Equal("Item \"Rachael's Phone\" setenabled true", LegacyCommandBuilder.ItemSetEnabled("Rachael's Phone"));

    [Fact]
    public void ItemNamesWithoutSpacesAreNotQuoted() =>
        Assert.Equal("Item Beer setenabled true", LegacyCommandBuilder.ItemSetEnabled("Beer"));

    [Fact]
    public void RequiresEnableQuotesTheEnableNameButNotTheDisplayName()
    {
        var lines = InventoryCommandBuilder.BuildRequiresEnable("Master Key", "MasterKey");
        Assert.Equal(["Item \"Master Key\" setenabled true", "MasterKey.inventory = True"], lines);
    }

    // ---------------- Target joining ----------------

    [Fact]
    public void JoinChainsTargetsWithDots() =>
        Assert.Equal("Rachael.Amy", TargetHelper.Join(["Rachael", "Amy"]));

    [Fact]
    public void JoinIsTotalAndDoesNotThrowOnAnEmptySelection() =>
        Assert.Equal("", TargetHelper.Join([]));

    // ---------------- Quest names ----------------

    [Fact]
    public void QuestNamesAreQuotedBecauseTheyContainSpacesAndApostrophes() =>
        Assert.Equal("quest start \"Ashley's Secret\"", QuestCommandBuilder.Manage("start", "Ashley's Secret"));
}
