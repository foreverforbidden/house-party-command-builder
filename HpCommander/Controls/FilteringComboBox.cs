using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace HpCommander.Controls;

/// <summary>
/// An editable ComboBox that narrows its list as you type. Typing "bed" in a 270-entry location
/// list leaves bedleft / bedright / masterbedroom rather than making you scroll for them.
/// </summary>
/// <remarks>
/// Filters <see cref="ItemsControl.Items"/> directly. That is deliberately not a shared
/// <c>CollectionViewSource.GetDefaultView</c> over some backing list: every ItemsControl owns its
/// own ItemCollection, so two combos populated from the same source cannot fight over one filter -
/// the classic WPF bug where typing in one dropdown empties another. It does mean items must be
/// added to <c>Items</c> rather than bound through <c>ItemsSource</c>, which is what the view base
/// class's Fill helpers already do.
/// </remarks>
public class FilteringComboBox : ComboBox
{
    private TextBox? _editBox;
    private string _filter = "";
    private bool _reentrant;

    public FilteringComboBox()
    {
        IsEditable = true;
        // Built-in prefix matching would fight the filter, jumping the selection and rewriting
        // the text out from under the user as they type.
        IsTextSearchEnabled = false;
        StaysOpenOnEdit = true;

        Items.Filter = Matches;
        AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnEditTextChanged));
    }

    /// <summary>
    /// What the user actually means: the picked item, or failing that whatever they typed.
    /// Filtering drops <see cref="Selector.SelectedItem"/> to null the moment the text stops
    /// matching the selection exactly, so reading SelectedItem alone makes the output flicker to
    /// guidance text mid-word. Read this instead.
    /// </summary>
    public string EffectiveValue =>
        SelectedItem as string ?? SelectedItem?.ToString() ?? Text?.Trim() ?? string.Empty;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _editBox = GetTemplateChild("PART_EditableTextBox") as TextBox;
    }

    private bool Matches(object item) =>
        _filter.Length == 0 ||
        (item?.ToString() ?? string.Empty).Contains(_filter, StringComparison.OrdinalIgnoreCase);

    private void OnEditTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_reentrant)
            return;

        _reentrant = true;
        try
        {
            var text = Text ?? string.Empty;
            var caret = _editBox?.CaretIndex ?? text.Length;

            _filter = text;
            Items.Filter = Matches;

            // Only open while they are actually typing into it - not when Fill or a programmatic
            // assignment changes the text.
            if (IsKeyboardFocusWithin)
                IsDropDownOpen = Items.Count > 0;

            // Restore last, and in this order. Refreshing can drop the selection (which makes the
            // ComboBox rewrite its own text), and opening the drop-down selects the whole edit
            // box - so the next keystroke would replace everything typed so far. Collapsing the
            // selection back to a caret is what stops "bed" from arriving as "ed".
            if (Text != text)
                Text = text;
            if (_editBox != null)
            {
                _editBox.CaretIndex = Math.Min(caret, (Text ?? string.Empty).Length);
                _editBox.SelectionLength = 0;
            }
        }
        finally
        {
            _reentrant = false;
        }
    }

    protected override void OnDropDownClosed(EventArgs e)
    {
        base.OnDropDownClosed(e);

        // Drop the filter once they are done, so reopening shows the whole list again instead of
        // just the one entry whose name is now sitting in the text box.
        if (_filter.Length == 0)
            return;

        _reentrant = true;
        try
        {
            var text = Text;
            _filter = "";
            Items.Filter = Matches;
            Text = text;
        }
        finally
        {
            _reentrant = false;
        }
    }
}
