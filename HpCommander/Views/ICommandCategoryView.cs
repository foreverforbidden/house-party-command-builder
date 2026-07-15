namespace HpCommander.Views;

public interface ICommandCategoryView
{
    event EventHandler? CommandChanged;

    string BuildCommand();

    bool NeedsGlobalTargets { get; }
}
