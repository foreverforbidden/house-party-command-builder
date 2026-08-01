using System.Runtime.ExceptionServices;
using System.Windows.Data;
using HpCommander.Controls;
using HpCommander.Views;
using Xunit;

namespace HpCommander.Builders.Tests;

/// <summary>
/// The Run view's function dropdown groups an item's own functions above the boilerplate every
/// item carries (issue #11). These cover the two things that were not safe to assume: that grouping
/// survives the substring filter <see cref="FilteringComboBox"/> installs, and that reading a
/// picked option still yields a bare function name now that the list holds objects rather than
/// strings. The first attempt at this used <c>Items.GroupDescriptions</c> on an Items-populated
/// combo, which silently does nothing - <c>CanGroup</c> is false in that mode - hence the tests.
/// </summary>
public class RunFunctionGroupingTests
{
    /// <summary>WPF controls can only be constructed on an STA thread and xunit's runner is MTA.
    /// A local helper rather than the Xunit.StaFact package: one thread is cheaper than a
    /// dependency.</summary>
    private static void Sta(Action body)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { body(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private const string Common = "Common to every item";

    private static FunctionOption[] Fridge() =>
    [
        new("PlayBGC", "Fridge"),
        new("PartiallyOpen", "Fridge"),
        new("PlaySoundEffect1", Common),
        new("DestroyItem", Common),
    ];

    private static FilteringComboBox GroupedCombo(params FunctionOption[] options)
    {
        var combo = new FilteringComboBox();
        combo.SetGroupedItems(options, nameof(FunctionOption.Group));
        return combo;
    }

    private static List<CollectionViewGroup> NonEmptyGroups(FilteringComboBox combo) =>
        combo.Items.Groups!.Cast<CollectionViewGroup>().Where(g => g.ItemCount > 0).ToList();

    [Fact]
    public void GroupsInInsertionOrderSoTheItemsOwnFunctionsComeFirst() => Sta(() =>
    {
        var groups = NonEmptyGroups(GroupedCombo(Fridge()));

        Assert.Equal(["Fridge", Common], groups.Select(g => g.Name));
        Assert.Equal(["PlayBGC", "PartiallyOpen"], groups[0].Items.Select(i => i.ToString()));
    });

    [Fact]
    public void FilterAppliesAcrossEveryGroupRatherThanOnlyTheFirst() => Sta(() =>
    {
        var combo = GroupedCombo(Fridge());

        // What FilteringComboBox installs as the user types.
        combo.Items.Filter = o => o.ToString()!.Contains("Play", StringComparison.OrdinalIgnoreCase);

        var groups = NonEmptyGroups(combo);

        Assert.Equal(["Fridge", Common], groups.Select(g => g.Name));
        Assert.Equal(["PlayBGC"], groups[0].Items.Select(i => i.ToString()));
        Assert.Equal(["PlaySoundEffect1"], groups[1].Items.Select(i => i.ToString()));
    });

    [Fact]
    public void AGroupWithNoSurvivingItemsDisappears() => Sta(() =>
    {
        var combo = GroupedCombo(Fridge());

        combo.Items.Filter = o => o.ToString() == "DestroyItem";

        Assert.Equal([Common], NonEmptyGroups(combo).Select(g => g.Name));
    });

    /// <summary>Refilling rewrites Text to preserve it. If the in-progress filter were dropped on
    /// the swap, the list would flash back to full mid-word.</summary>
    [Fact]
    public void RefillingKeepsTheTypedTextAndTheActiveFilter() => Sta(() =>
    {
        var combo = GroupedCombo(Fridge());
        combo.Items.Filter = o => o.ToString()!.StartsWith("Turn", StringComparison.Ordinal);
        combo.Text = "Turn";

        combo.SetGroupedItems([new FunctionOption("TurnOn", "AC Unit"), new FunctionOption("DestroyItem", Common)],
            nameof(FunctionOption.Group));

        Assert.Equal("Turn", combo.Text);
        Assert.Equal(["AC Unit"], NonEmptyGroups(combo).Select(g => g.Name));
    });

    /// <summary>The dropdown holds FunctionOption objects now, so the view reads EffectiveValue
    /// instead of Text. This is the seam where a regression would start emitting
    /// "HpCommander.Views.FunctionOption" into the command.</summary>
    [Fact]
    public void EffectiveValueResolvesAPickedOptionToItsFunctionName() => Sta(() =>
    {
        var combo = GroupedCombo(Fridge());
        combo.SelectedIndex = 0;

        Assert.Equal("PlayBGC", combo.EffectiveValue);
    });

    [Fact]
    public void EffectiveValueStillHonoursFreeTypedTextWhenNothingIsSelected() => Sta(() =>
    {
        var combo = GroupedCombo(Fridge());
        combo.Text = "  SomeUndocumentedFunction  ";

        Assert.Equal("SomeUndocumentedFunction", combo.EffectiveValue);
    });
}
