using HpCommander.Builders;

namespace HpCommander.Views;

public interface ICommandCategoryView
{
    event EventHandler? CommandChanged;

    CommandResult BuildCommand();

    bool NeedsGlobalTargets { get; }

    /// <summary>Raised when the global target selection changes (and when the view is shown),
    /// so views with per-character option lists can repopulate without a manual refresh.</summary>
    void OnTargetsChanged()
    {
    }
}
