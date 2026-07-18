using System.Windows;

namespace HpCommander;

public enum AppTheme
{
    Light,
    Dark,
}

/// <summary>
/// Swaps the palette dictionary at the front of the application's merged dictionaries.
/// Everything that paints itself reaches the palette through DynamicResource, so the swap
/// repaints the live UI - StaticResource would have resolved once at load and ignored it.
/// </summary>
public static class Theme
{
    private const string LightSource = "Styles/Themes/Light.xaml";
    private const string DarkSource = "Styles/Themes/Dark.xaml";

    public static AppTheme Current { get; private set; } = AppTheme.Light;

    public static void Apply(AppTheme theme)
    {
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var source = new Uri(theme == AppTheme.Dark ? DarkSource : LightSource, UriKind.Relative);

        var replacement = new ResourceDictionary { Source = source };

        // The palette is always first; the style dictionaries after it depend on its keys.
        var existing = dictionaries.FirstOrDefault(d =>
            d.Source is not null && d.Source.OriginalString.Contains("Styles/Themes/", StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
            dictionaries[dictionaries.IndexOf(existing)] = replacement;
        else
            dictionaries.Insert(0, replacement);

        Current = theme;
    }
}
