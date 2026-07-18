using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class InventoryView : CommandCategoryViewBase
{
    private enum ItemKind { Plain, Alias, RequiresEnable, Numbered, Locked }

    private sealed record ItemEntry(string Label, ItemKind Kind, string? Internal, string? DisplayOverride = null)
    {
        public string Value => Internal ?? Label;
        public override string ToString() => Label;
    }

    private readonly GameData _data;

    public InventoryView(GameData data)
    {
        InitializeComponent();
        _data = data;

        foreach (var name in _data.Items.Plain)
            ItemCombo.Items.Add(new ItemEntry(name, ItemKind.Plain, null));
        foreach (var alias in _data.Items.AliasNames)
            ItemCombo.Items.Add(new ItemEntry(alias.Display, ItemKind.Alias, alias.Internal));
        foreach (var req in _data.Items.RequiresEnable)
            ItemCombo.Items.Add(new ItemEntry(req.Display + " (requires enable)", ItemKind.RequiresEnable, req.EnableName, req.Display));
        foreach (var num in _data.Items.NumberedRange)
            ItemCombo.Items.Add(new ItemEntry(num.Label + " (numbered)", ItemKind.Numbered, num.BaseName));
        foreach (var locked in _data.Items.Locked)
            ItemCombo.Items.Add(new ItemEntry(locked + " (no known unlock method)", ItemKind.Locked, null));
    }

    private void ItemCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        NumberStepper.IsEnabled = ItemCombo.SelectedItem is ItemEntry { Kind: ItemKind.Numbered };
        if (ItemCombo.SelectedItem is ItemEntry { Kind: ItemKind.Numbered } numEntry &&
            _data.Items.NumberedRange.FirstOrDefault(n => n.BaseName == numEntry.Value) is { } range)
        {
            NumberStepper.Minimum = range.Min;
            NumberStepper.Maximum = range.Max;
            NumberStepper.Value = range.Min;
        }
        Recompute();
    }

    public override CommandResult BuildCommand()
    {
        if (ItemCombo.SelectedItem is not ItemEntry entry)
            return CommandResult.NeedsInput("Pick an item");
        return entry.Kind switch
        {
            ItemKind.Locked => CommandResult.Unavailable("No known command for this item yet"),
            ItemKind.Plain => CommandResult.Ok(InventoryCommandBuilder.BuildPlain(entry.Value)),
            ItemKind.Alias => CommandResult.Ok(InventoryCommandBuilder.BuildPlain(entry.Internal ?? entry.Value)),
            ItemKind.Numbered => CommandResult.Ok(InventoryCommandBuilder.BuildPlain(entry.Value, (int)NumberStepper.Value)),
            ItemKind.RequiresEnable => CommandResult.Ok(
                InventoryCommandBuilder.BuildRequiresEnable(entry.Value, entry.DisplayOverride ?? entry.Value)),
            _ => CommandResult.Error($"Unhandled item kind {entry.Kind}"),
        };
    }
}
