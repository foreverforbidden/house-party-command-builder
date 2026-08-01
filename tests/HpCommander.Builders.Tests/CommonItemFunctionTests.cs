using System.IO;
using HpCommander.Data;
using Xunit;

namespace HpCommander.Builders.Tests;

/// <summary>
/// The item function dropdown is only useful if the common/unique split is right, and the split is
/// derived from frequency rather than authored (see GameData.CommonItemFunctions). These assert the
/// derivation against the shipped data, and that the threshold sits in the gap rather than on a
/// slope: over the 133 shipped items, 25 functions appear on 80-100% of them and the next most
/// common appears on 10.5%, so 25% and 75% must pick out exactly the same set.
/// </summary>
public class CommonItemFunctionTests
{
    private static GameData Shipped() => GameData.Load(
        Path.Combine(AppContext.BaseDirectory, "Data"));

    /// <summary>The 14 functions every single item carries, straight from docs/console-reference.md.</summary>
    private static readonly string[] UniversalFunctions =
    [
        "AddLargeForwardMomentum", "AddMediumForwardMomentum", "AddPhysicsRigidBody",
        "AddSmallForwardMomentum", "LoopSoundEffect1", "LoopSoundEffect2", "PlaySoundEffect1",
        "PlaySoundEffect2", "ResetToOriginalPosition", "ResetToOriginalRotation", "StopAllAudio",
        "SwitchToAlternateTexture1", "SwitchToAlternateTexture2", "SwitchToOriginalTexture",
    ];

    [Fact]
    public void EveryUniversalFunctionIsClassifiedAsCommon()
    {
        var common = Shipped().CommonItemFunctions;

        Assert.All(UniversalFunctions, f => Assert.Contains(f, common));
    }

    [Fact]
    public void TheDistinctivelyPerItemFunctionsAreNotClassifiedAsCommon()
    {
        var common = Shipped().CommonItemFunctions;

        // The ones that make the feature worth having - what someone picking "Coffee" came for.
        Assert.All(
            new[] { "EnableSteam", "TurnOn", "TurnOff", "UntieShirt", "OpenVent", "PlayBGC", "DestroyForest" },
            f => Assert.DoesNotContain(f, common));
    }

    [Fact]
    public void TheSplitLeavesEachItemAShortListOfItsOwn()
    {
        var data = Shipped();

        var unique = data.ItemFunctions.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Count(f => !data.CommonItemFunctions.Contains(f)));

        // Nothing is left with a list as long as the undifferentiated one it replaced.
        Assert.True(unique.Values.Average() < 5, $"average unique count was {unique.Values.Average():N1}");
        Assert.Equal(2, unique["Coffee"]);
        Assert.Equal(2, unique["AshleyTop"]);
    }

    [Fact]
    public void TheThresholdSitsInTheGapRatherThanOnASlope()
    {
        var data = Shipped();

        // Recount independently of the production code: if a data refresh ever narrows the gap
        // between "on most items" and "on a handful", this is what notices.
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var functions in data.ItemFunctions.Values)
        foreach (var function in functions.Distinct(StringComparer.Ordinal))
            counts[function] = counts.GetValueOrDefault(function) + 1;

        var total = data.ItemFunctions.Count;
        var lowestCommon = counts.Where(c => data.CommonItemFunctions.Contains(c.Key)).Min(c => c.Value);
        var highestUnique = counts.Where(c => !data.CommonItemFunctions.Contains(c.Key)).Max(c => c.Value);

        Assert.True(lowestCommon / (double)total > 0.75,
            $"least common 'common' function is on {lowestCommon}/{total} items");
        Assert.True(highestUnique / (double)total < 0.25,
            $"most common 'unique' function is on {highestUnique}/{total} items");
    }

    [Fact]
    public void ASmallDatasetIsLeftUnclassifiedRatherThanCallingEverythingCommon()
    {
        // "On half of two items" is not evidence of anything, so below the minimum item count the
        // derivation declines to guess and the dropdown stays flat.
        var tiny = new Dictionary<string, List<string>>
        {
            ["Lamp"] = ["TurnOn", "DestroyItem"],
            ["Chair"] = ["DestroyItem"],
        };

        Assert.Empty(GameData.DeriveCommonItemFunctions(tiny));
    }

    [Fact]
    public void AboveTheMinimumTheSharedFunctionsAreFound()
    {
        var items = Enumerable.Range(0, 12).ToDictionary(
            i => $"Item{i}",
            i => new List<string> { "DestroyItem", $"Unique{i}" });

        Assert.Equal(["DestroyItem"], GameData.DeriveCommonItemFunctions(items).Order());
    }
}
