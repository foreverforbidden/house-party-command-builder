using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using HpCommander.Builders;
using HpCommander.Controls;
using HpCommander.Data;
using HpCommander.Views;

namespace HpCommander;

public partial class MainWindow : Window
{
    private readonly GameData _data;
    private readonly CharacterChipPicker _chipPicker;
    private readonly ViewContext _context;
    private readonly ICollectionView _navView;
    private readonly Dictionary<string, CommandCategoryViewBase> _viewCache = new();
    private CommandCategoryViewBase? _activeView;
    private string? _activeName;
    private CommandResult _current;

    private readonly AppSettings _settings;
    // Trailing-edge: restarted on every change, so it fires once the command settles rather than
    // once per keystroke.
    private readonly DispatcherTimer _autoCopyTimer = new() { Interval = TimeSpan.FromMilliseconds(600) };
    private readonly DispatcherTimer _copyStatusTimer = new() { Interval = TimeSpan.FromMilliseconds(1200) };
    private CommandResult _pendingAutoCopy;
    private string? _lastCopied;
    private bool _suppressSettingEvents;

    public MainWindow()
    {
        InitializeComponent();
        _data = LoadGameData();

        _settings = AppSettings.Load();

        _suppressSettingEvents = true;
        AutoCopyCheck.IsChecked = _settings.AutoCopy;
        DarkModeCheck.IsChecked = _settings.ThemeOrDefault == AppTheme.Dark;
        _suppressSettingEvents = false;
        Theme.Apply(_settings.ThemeOrDefault);

        _autoCopyTimer.Tick += AutoCopyTimer_Tick;
        _copyStatusTimer.Tick += (_, _) =>
        {
            _copyStatusTimer.Stop();
            CopyStatus.Opacity = 0;
        };
        Closing += (_, _) =>
        {
            // A running DispatcherTimer roots the window.
            _autoCopyTimer.Stop();
            _copyStatusTimer.Stop();
            _settings.Save();
        };

        _chipPicker = new CharacterChipPicker();
        _chipPicker.SetCharacters(_data.Characters);
        _chipPicker.SelectionChanged += (_, _) =>
        {
            if (_activeView?.NeedsGlobalTargets == true)
            {
                _activeView.OnTargetsChanged();
                Recompute();
            }
        };
        ChipPickerHost.Content = _chipPicker;
        _context = new ViewContext(_data, _chipPicker);

        // Filtering a view over one fixed list keeps selection identity across searches;
        // swapping ItemsSource used to drop the selection whenever the active category was
        // filtered out, leaving the sidebar unhighlighted while its view stayed on screen.
        _navView = CollectionViewSource.GetDefaultView(CategoryRegistry.All);
        _navView.Filter = NavFilter;
        CategoryList.ItemsSource = _navView;
        CategoryList.SelectedItem = CategoryRegistry.All.First(n => !n.IsHeader);
    }

    private static GameData LoadGameData()
    {
        try
        {
            return GameData.Load();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not load the Data folder ({ex.Message}). Starting with empty data - fix the JSON and restart.",
                "HP Commander", MessageBoxButton.OK, MessageBoxImage.Warning);
            return new GameData();
        }
    }

    private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryList.SelectedItem is NavEntry { IsHeader: false } entry)
            ShowCategory(entry);
    }

    private void ShowCategory(NavEntry entry)
    {
        if (_activeName == entry.Name)
            return;

        if (_activeView != null)
        {
            _activeView.CommandChanged -= ActiveView_CommandChanged;
            _activeView.CopyRequested -= ActiveView_CopyRequested;
        }

        if (!_viewCache.TryGetValue(entry.Name, out var view))
        {
            view = entry.Factory!(_context);
            _viewCache[entry.Name] = view;
        }

        _activeName = entry.Name;
        _activeView = view;
        _activeView.CommandChanged += ActiveView_CommandChanged;
        _activeView.CopyRequested += ActiveView_CopyRequested;
        // Per-character lists follow the current target selection, so a view opened
        // after the selection changed still reflects it without a manual refresh.
        _activeView.OnTargetsChanged();
        ContentHost.Content = view;
        ContentScroll.ScrollToTop();
        TargetsCard.Visibility = view.NeedsGlobalTargets ? Visibility.Visible : Visibility.Collapsed;
        Recompute();
    }

    // ---------------- Sidebar search ----------------

    /// <summary>A header survives the filter only while some category under it does.</summary>
    private bool NavFilter(object obj)
    {
        if (obj is not NavEntry entry)
            return true;

        var query = SearchBox.Text.Trim();
        if (query.Length == 0)
            return true;

        if (!entry.IsHeader)
            return entry.Matches(query);

        return CategoryRegistry.All
            .SkipWhile(e => !ReferenceEquals(e, entry))
            .Skip(1)
            .TakeWhile(e => !e.IsHeader)
            .Any(e => e.Matches(query));
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _navView.Refresh();

        // Filtering the active category out clears the ListBox selection, and it does not come
        // back on its own when the filter widens again - which left the sidebar showing nothing
        // selected while its view was still on screen. Re-select as soon as it is visible again.
        if (CategoryList.SelectedItem == null && _activeName != null)
            CategoryList.SelectedItem = _navView.Cast<NavEntry>().FirstOrDefault(n => n.Name == _activeName);
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var target = _navView.Cast<NavEntry>().FirstOrDefault(n => !n.IsHeader);
            if (target != null)
            {
                SearchBox.Clear();
                CategoryList.SelectedItem = target;
                CategoryList.ScrollIntoView(target);
                CategoryList.UpdateLayout();
                (CategoryList.ItemContainerGenerator.ContainerFromItem(target) as ListBoxItem)?.Focus();
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            SearchBox.Clear();
            e.Handled = true;
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            CopyToClipboard(_current);
            e.Handled = true;
        }
    }

    private void ActiveView_CommandChanged(object? sender, EventArgs e) => Recompute();

    /// <summary>A double-clicked reference name is a deliberate copy, not a built command.</summary>
    private void ActiveView_CopyRequested(object? sender, string text) => CopyToClipboard(CommandResult.Ok(text));

    private void Recompute()
    {
        if (_activeView == null)
            return;
        try
        {
            _current = _activeView.BuildCommand();
        }
        catch (Exception ex)
        {
            _current = CommandResult.Error(ex.Message);
        }

        OutputBox.Text = _current.Text;
        // Guidance and errors are styled as hint text so they can't be mistaken for output,
        // and the Copy button disables rather than silently doing nothing.
        OutputBox.Foreground = (Brush)FindResource(_current.IsOk ? "TextPrimaryBrush" : "TextSecondaryBrush");
        OutputBox.FontStyle = _current.IsOk ? FontStyles.Normal : FontStyles.Italic;
        CopyButton.IsEnabled = _current.IsOk;

        MaybeAutoCopy(_current);
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e) => CopyToClipboard(_current);

    /// <summary>An explicit copy: it may interrupt, and it belongs in history.</summary>
    private void CopyToClipboard(CommandResult result)
    {
        if (!result.IsOk || string.IsNullOrWhiteSpace(result.Text))
            return;
        if (TrySetClipboard(result.Text, silent: false))
            PushHistory(result.Text);
    }

    private bool TrySetClipboard(string text, bool silent)
    {
        try
        {
            Clipboard.SetText(text);
            _lastCopied = text;
            return true;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Right for a deliberate copy, catastrophic for an automatic one: a process holding
            // the clipboard would otherwise produce a dialog every time the timer fires.
            if (!silent)
                MessageBox.Show(text, "Clipboard busy - copy manually", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    // ---------------- auto-copy ----------------

    private void AutoCopyCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressSettingEvents)
            return;

        if (AutoCopyCheck.IsChecked == true && !_settings.AutoCopyConsentGiven)
        {
            var answer = MessageBox.Show(
                "HP Commander will replace your clipboard contents automatically, every time the " +
                "command it builds changes - including while you are still typing.\n\n" +
                "This only affects the clipboard on this computer; nothing is sent anywhere.\n\n" +
                "You can turn it off again with the same checkbox.\n\n" +
                "Turn auto-copy on?",
                "Enable auto-copy",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

            if (answer != MessageBoxResult.OK)
            {
                // Assigning IsChecked re-raises this handler.
                _suppressSettingEvents = true;
                AutoCopyCheck.IsChecked = false;
                _suppressSettingEvents = false;
                return;
            }

            _settings.AutoCopyConsentGiven = true;
        }

        _settings.AutoCopy = AutoCopyCheck.IsChecked == true;
        _settings.Save();

        if (!_settings.AutoCopy)
            _autoCopyTimer.Stop();
    }

    private void DarkModeCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressSettingEvents)
            return;

        var theme = DarkModeCheck.IsChecked == true ? AppTheme.Dark : AppTheme.Light;
        Theme.Apply(theme);
        _settings.Theme = theme.ToString();
        _settings.Save();
    }

    private void MaybeAutoCopy(CommandResult result)
    {
        // Guidance and errors are never copied, so a half-built command simply leaves the
        // clipboard alone. This gate is what CommandResult bought us.
        if (AutoCopyCheck.IsChecked != true || !result.IsOk)
        {
            _autoCopyTimer.Stop();
            return;
        }

        _pendingAutoCopy = result;
        _autoCopyTimer.Stop();
        _autoCopyTimer.Start();
    }

    private void AutoCopyTimer_Tick(object? sender, EventArgs e)
    {
        _autoCopyTimer.Stop();

        // Idempotent: flipping between tabs that build the same command must not re-copy.
        if (!_pendingAutoCopy.IsOk || _pendingAutoCopy.Text == _lastCopied)
            return;

        // Deliberately does not push history. History is "things I chose to copy"; auto-copying
        // into it would fill all ten slots with intermediate states within seconds.
        if (TrySetClipboard(_pendingAutoCopy.Text, silent: true))
            FlashCopyStatus();
    }

    /// <summary>Without this, auto-copy is invisible and users can't tell whether it worked.</summary>
    private void FlashCopyStatus()
    {
        CopyStatus.Opacity = 1;
        _copyStatusTimer.Stop();
        _copyStatusTimer.Start();
    }

    private void PushHistory(string text)
    {
        // Move-to-front: comparing only against the newest entry left duplicates behind when
        // copying A, B, A.
        for (var i = HistoryList.Items.Count - 1; i >= 0; i--)
            if (Equals(HistoryList.Items[i], text))
                HistoryList.Items.RemoveAt(i);

        HistoryList.Items.Insert(0, text);
        while (HistoryList.Items.Count > 10)
            HistoryList.Items.RemoveAt(HistoryList.Items.Count - 1);
    }

    private void HistoryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (HistoryList.SelectedItem is string s)
            CopyToClipboard(CommandResult.Ok(s));
    }
}
