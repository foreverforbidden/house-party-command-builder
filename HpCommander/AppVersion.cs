using System.IO;
using System.Reflection;

namespace HpCommander;

/// <summary>
/// What version of itself the app believes it is, and where it lives on disk. Both are needed by
/// the update check, and neither is as obvious as it looks in a single-file publish.
/// </summary>
public static class AppVersion
{
    /// <summary>
    /// The version from the csproj, without the <c>+&lt;sha&gt;</c> suffix the SDK appends.
    /// </summary>
    public static Version Current { get; } = ReadCurrent();

    /// <summary>Display form, e.g. "1.7.0" - trailing zero components dropped rather than shown as
    /// the "1.7.0.0" that <see cref="Version.ToString()"/> would give.</summary>
    public static string CurrentDisplay { get; } =
        Current.Revision > 0 ? Current.ToString(4) : Current.ToString(3);

    /// <summary>
    /// The running executable. <c>Assembly.Location</c> is empty under PublishSingleFile, which is
    /// exactly how the app ships, so it is never the right thing to ask.
    /// </summary>
    public static string? ExecutablePath => Environment.ProcessPath;

    /// <summary>The folder holding the exe and its Data directory - the two things an update has
    /// to replace together.</summary>
    public static string? InstallDirectory =>
        ExecutablePath is { } path ? Path.GetDirectoryName(path) : null;

    private static Version ReadCurrent()
    {
        var informational = typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        // "1.7.0+6b09ec6d..." -> "1.7.0"
        if (informational is not null)
        {
            var plus = informational.IndexOf('+');
            if (plus >= 0)
                informational = informational[..plus];

            if (Version.TryParse(informational, out var parsed))
                return parsed;
        }

        return typeof(AppVersion).Assembly.GetName().Version ?? new Version(0, 0, 0);
    }
}
