using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HpCommander;

/// <summary>
/// The app's only persisted state.
/// </summary>
/// <remarks>
/// This used to carry a note that it should stay at two booleans because a third would demand
/// migration logic. The update check (issue #8) needed four more fields and the note turned out to
/// be pessimistic: System.Text.Json fills a property that is absent from the file with its default,
/// so <em>adding</em> a setting migrates itself. The real rule is narrower - a property here may be
/// added freely, but renaming or repurposing one silently discards what users already have, and
/// that is the change that needs migration logic.
/// </remarks>
public sealed class AppSettings
{
    /// <summary>Off unless the user turns it on. Auto-copy takes over a shared OS resource,
    /// so it is never on by default.</summary>
    public bool AutoCopy { get; set; }

    /// <summary>Records that the one-time explanation has been shown, so turning auto-copy off
    /// and on again doesn't re-prompt.</summary>
    public bool AutoCopyConsentGiven { get; set; }

    /// <summary>Light or Dark. Unparseable values fall back to Light rather than failing to load.</summary>
    public string Theme { get; set; } = nameof(AppTheme.Light);

    /// <summary>Off unless the user turns it on. This is the only thing the app does over the
    /// network, so it is opt-in for the same reason auto-copy is.</summary>
    public bool UpdateCheckEnabled { get; set; }

    /// <summary>Records that the one-time explanation has been shown, so declining once is not
    /// re-asked on every launch.</summary>
    public bool UpdateCheckConsentGiven { get; set; }

    /// <summary>A version the user chose to skip. Anything newer still prompts.</summary>
    public string? SkippedVersion { get; set; }

    /// <summary>Throttles the startup check to once a day.</summary>
    public DateTime? LastUpdateCheckUtc { get; set; }

    /// <summary>Derived, so it must not be written back into the file.</summary>
    [JsonIgnore]
    public AppTheme ThemeOrDefault =>
        Enum.TryParse<AppTheme>(Theme, ignoreCase: true, out var parsed) ? parsed : AppTheme.Light;

    // Not AppContext.BaseDirectory: an install under Program Files isn't writable.
    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HpCommander", "settings.json");

    /// <summary>Never throws. A missing, unreadable or corrupt file yields defaults.</summary>
    public static AppSettings Load()
    {
        try
        {
            return File.Exists(FilePath)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings()
                : new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <summary>Never throws. Writes to a temp file and moves it into place, so a crash
    /// mid-write can't leave a truncated file that then fails to parse.</summary>
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, FilePath, overwrite: true);
        }
        catch
        {
            // A settings file we can't write is not worth interrupting the user over.
        }
    }
}
