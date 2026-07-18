using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HpCommander;

/// <summary>
/// The app's only persisted state. Deliberately two booleans: the moment a second unrelated
/// setting lands here, someone needs migration logic, and this is not worth that.
/// </summary>
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
