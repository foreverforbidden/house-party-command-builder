namespace HpCommander.Builders;

public enum CommandResultKind
{
    /// <summary>A real, copyable command.</summary>
    Ok,

    /// <summary>The user still has something to fill in. Shown as hint text; not copyable.</summary>
    NeedsInput,

    /// <summary>This view or tab can never produce a command (reference-only, or a known-locked item).</summary>
    Unavailable,

    /// <summary>A builder threw, or a switch fell through. Should not happen; surfaced so it isn't swallowed.</summary>
    Error,
}

/// <summary>
/// What a view's <c>BuildCommand</c> produced. Replaces the old convention of returning a string
/// that starts with "(" to mean "not a real command" — a channel that guidance text, error
/// messages and placeholder output all shared, so nothing downstream could tell them apart.
/// </summary>
public readonly record struct CommandResult
{
    private CommandResult(CommandResultKind kind, string text)
    {
        Kind = kind;
        Text = text;
    }

    public CommandResultKind Kind { get; }

    /// <summary>The command when <see cref="IsOk"/>, otherwise the guidance or reason to display.</summary>
    public string Text { get; }

    public bool IsOk => Kind == CommandResultKind.Ok;

    public static CommandResult Ok(string command) => new(CommandResultKind.Ok, command);

    /// <summary>For commands that must be pasted as several lines, e.g. an item that needs enabling first.</summary>
    public static CommandResult Ok(IEnumerable<string> lines) =>
        new(CommandResultKind.Ok, string.Join(Environment.NewLine, lines));

    public static CommandResult NeedsInput(string guidance) => new(CommandResultKind.NeedsInput, guidance);

    public static CommandResult Unavailable(string reason) => new(CommandResultKind.Unavailable, reason);

    public static CommandResult Error(string message) => new(CommandResultKind.Error, message);
}
