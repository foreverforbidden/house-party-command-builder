using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;

namespace HpCommander.Views;

/// <summary>
/// Shared plumbing for every category view: the change notification, the four XAML event
/// handlers, combo population, and automatic wiring of editable combos. Views subclass this
/// instead of re-declaring the same dozen lines each.
/// </summary>
public abstract class CommandCategoryViewBase : UserControl
{
    private bool _inputsWired;
    private int _suspendDepth;

    protected CommandCategoryViewBase()
    {
        // The logical tree (including every TabItem's content) exists by Loaded, but not in the
        // base constructor - that runs before the derived InitializeComponent. Views are cached
        // and re-parented into the content host on every category switch, so Loaded fires many
        // times per view; without the guard we would stack handlers and fire N recomputes per
        // keystroke, degrading the longer the app runs.
        Loaded += (_, _) =>
        {
            if (_inputsWired) return;
            _inputsWired = true;
            WireEditableCombos(this);
        };
    }

    // ---------------- contract ----------------

    public event EventHandler? CommandChanged;

    /// <summary>Asks the shell to put arbitrary text on the clipboard - used by reference lists
    /// where a row is a name to copy rather than a command to build.</summary>
    public event EventHandler<string>? CopyRequested;

    public abstract CommandResult BuildCommand();

    public virtual bool NeedsGlobalTargets => false;

    public virtual void OnTargetsChanged()
    {
    }

    // ---------------- recompute ----------------

    protected void Recompute()
    {
        if (_suspendDepth == 0)
            CommandChanged?.Invoke(this, EventArgs.Empty);
    }

    protected void RequestCopy(string text) => CopyRequested?.Invoke(this, text);

    /// <summary>Collapses a burst of programmatic changes into a single recompute:
    /// <c>using (SuspendRecompute()) { ... }</c>.</summary>
    protected IDisposable SuspendRecompute()
    {
        _suspendDepth++;
        return new Scope(this);
    }

    private sealed class Scope(CommandCategoryViewBase owner) : IDisposable
    {
        public void Dispose()
        {
            if (--owner._suspendDepth == 0)
                owner.Recompute();
        }
    }

    // ---------------- the XAML handlers, declared once ----------------
    // Distinct names rather than overloads of one method: overload resolution would pick
    // correctly, but it is a subtle rule to rely on across 19 XAML files and it fails with
    // confusing errors when it doesn't.

    // Virtual so a view can react to input beyond recomputing - e.g. clearing a "show the list
    // instead" flag that a button set.

    protected virtual void OnSelectionChanged(object sender, SelectionChangedEventArgs e) => Recompute();

    protected virtual void OnToggleChanged(object sender, RoutedEventArgs e) => Recompute();

    protected virtual void OnTextChanged(object sender, TextChangedEventArgs e) => Recompute();

    /// <summary>For NumericStepper, whose ValueChanged is a plain CLR event.</summary>
    protected virtual void OnValueChanged(object? sender, EventArgs e) => Recompute();

    // ---------------- combo population ----------------

    protected static void Fill<T>(ComboBox combo, IEnumerable<T> items, string? prepend = null, int selectedIndex = 0)
    {
        combo.Items.Clear();
        if (prepend is not null)
            combo.Items.Add(prepend);
        foreach (var item in items)
            combo.Items.Add(item!);
        combo.SelectedIndex = combo.Items.Count == 0
            ? -1
            : Math.Clamp(selectedIndex, -1, combo.Items.Count - 1);
    }

    protected static void FillChars(ComboBox combo, GameData data, string? allTarget = null, int selectedIndex = 0) =>
        Fill(combo, data.Characters, prepend: allTarget, selectedIndex: selectedIndex);

    /// <summary>Re-fills a combo without discarding what the user typed or picked.</summary>
    protected static void RefillPreservingText<T>(ComboBox combo, IEnumerable<T> items)
    {
        var current = combo.Text;
        Fill(combo, items, selectedIndex: -1);
        combo.Text = current;
    }

    // ---------------- automatic input wiring ----------------

    /// <summary>Editable combos do not raise SelectionChanged while the user types, so each view
    /// used to add this handler by hand. Now it happens once, for all of them.</summary>
    private void WireEditableCombos(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if (child is ComboBox { IsEditable: true } editable)
                editable.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnTextChanged));

            WireEditableCombos(child);
        }
    }
}

/// <summary>A view whose subject comes from the shell's shared character chip picker.</summary>
public abstract class TargetedCommandCategoryViewBase : CommandCategoryViewBase
{
    protected TargetedCommandCategoryViewBase(CharacterChipPicker targets) => Targets = targets;

    protected CharacterChipPicker Targets { get; }

    public sealed override bool NeedsGlobalTargets => true;

    /// <summary>Builds against the current target selection, or asks for one. This is the state
    /// check that replaces TargetHelper.Join throwing on every keystroke.</summary>
    protected CommandResult WithTargets(Func<IReadOnlyList<string>, string> build)
    {
        var targets = Targets.GetSelectedTargets();
        return targets.Count == 0
            ? CommandResult.NeedsInput("Select at least one character above")
            : CommandResult.Ok(build(targets));
    }
}
