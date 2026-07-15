using System.Windows;
using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class InventoryView : UserControl, ICommandCategoryView
{
    private enum ItemKind { Plain, Alias, RequiresEnable, Numbered, Locked }

    private sealed record ItemEntry(string Label, ItemKind Kind, string? Internal, string? DisplayOverride = null)
    {
        public string Value => Internal ?? Label;
        public override string ToString() => Label;
    }

    private readonly GameData _data;

    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => false;

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
        CommandChanged?.Invoke(this, EventArgs.Empty);
    }

    private void NumberStepper_ValueChanged(object? sender, EventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    public string BuildCommand()
    {
        if (ItemCombo.SelectedItem is not ItemEntry entry)
            return "(pick an item)";
        return entry.Kind switch
        {
            ItemKind.Locked => "(no known command for this item yet)",
            ItemKind.Plain => InventoryCommandBuilder.BuildPlain(entry.Value),
            ItemKind.Alias => InventoryCommandBuilder.BuildPlain(entry.Internal ?? entry.Value),
            ItemKind.Numbered => InventoryCommandBuilder.BuildPlain(entry.Value, (int)NumberStepper.Value),
            ItemKind.RequiresEnable => string.Join(Environment.NewLine,
                InventoryCommandBuilder.BuildRequiresEnable(entry.Value, entry.DisplayOverride ?? entry.Value)),
            _ => "(unhandled item kind)",
        };
    }
}
