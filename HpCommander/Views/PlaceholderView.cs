using System.Windows.Controls;
using HpCommander.Builders;

namespace HpCommander.Views;

/// <summary>Temporary stand-in for a category not yet ported to WPF. Removed once every category has a real view.</summary>
public sealed class PlaceholderView : UserControl, ICommandCategoryView
{
    private readonly string _name;

#pragma warning disable CS0067 // never raised - this view's output never changes
    public event EventHandler? CommandChanged;
#pragma warning restore CS0067

    public bool NeedsGlobalTargets => false;

    public PlaceholderView(string name)
    {
        _name = name;
        Content = new TextBlock
        {
            Text = $"'{name}' not yet ported to WPF.",
            Style = (System.Windows.Style)FindResource("NoteTextStyle"),
        };
    }

    public CommandResult BuildCommand() => CommandResult.Unavailable($"{_name} not yet ported");
}
