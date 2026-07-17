using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HpCommander.Controls;
using HpCommander.Data;
using HpCommander.Views;

namespace HpCommander;

public partial class MainWindow : Window
{
    private static readonly NavEntry[] Nav =
    {
        NavEntry.Header("APPEARANCE"),
        NavEntry.Item("Change", "clothing", "clothes", "top", "bottom", "underwear", "undershirt", "naked", "undress", "hair", "accessories", "glasses", "shoes", "strapon"),
        NavEntry.Item("Outfit", "clothes", "dress", "costume", "default"),
        NavEntry.Item("Size", "scale", "body", "head", "chest", "grow", "shrink"),
        NavEntry.Header("CHARACTER"),
        NavEntry.Item("Values", "trait", "relationship", "friendship", "romance", "exhibitionism", "jealous", "list", "filter"),
        NavEntry.Item("Social", "drunk", "mood", "friendship", "romance", "sendtext", "talkto"),
        NavEntry.Item("States", "sweating", "dance", "fire", "erect"),
        NavEntry.Item("Properties", "bloody"),
        NavEntry.Item("Intimacy", "sex", "sexualact", "masturbation", "act", "speed"),
        NavEntry.Header("ACTIONS"),
        NavEntry.Item("Movement", "walk", "walkto", "warp", "warpto", "teleport", "tp", "roam", "roaming", "turn", "location", "coordinates", "overtime"),
        NavEntry.Item("Addforce", "push", "force", "physics", "launch"),
        NavEntry.Item("Run", "function", "effect", "animation"),
        NavEntry.Header("WORLD"),
        NavEntry.Item("Inventory", "item", "give", "spawn", "beer", "nattylite", "condom", "key"),
        NavEntry.Item("Door", "lock", "unlock", "open", "close"),
        NavEntry.Item("Quest", "story", "start", "complete", "increment", "fail", "mission"),
        NavEntry.Item("Time", "timescale", "slow", "fast", "speed"),
        NavEntry.Header("CONSOLE"),
        NavEntry.Item("Misc", "achievements", "unstuck", "help", "clear"),
        NavEntry.Item("Legacy (V1)", "enablenpc", "disablenpc", "npc", "combat", "fight", "passout", "wakeup", "setenabled", "enable", "disable"),
    };

    private readonly GameData _data;
    private readonly CharacterChipPicker _chipPicker;
    private readonly Dictionary<string, ICommandCategoryView> _viewCache = new();
    private ICommandCategoryView? _activeView;
    private string? _activeName;

    public MainWindow()
    {
        InitializeComponent();
        _data = LoadGameData();

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

        CategoryList.ItemsSource = Nav;
        CategoryList.SelectedItem = Nav.First(n => !n.IsHeader);
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
                $"Could not load Data\\commands.json ({ex.Message}). Starting with empty data - edit the JSON file and restart.",
                "HP Commander", MessageBoxButton.OK, MessageBoxImage.Warning);
            return new GameData();
        }
    }

    private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryList.SelectedItem is NavEntry { IsHeader: false } entry)
            ShowCategory(entry.Name);
    }

    private void ShowCategory(string name)
    {
        if (_activeName == name)
            return;

        if (_activeView != null)
            _activeView.CommandChanged -= ActiveView_CommandChanged;

        if (!_viewCache.TryGetValue(name, out var view))
        {
            view = CreateView(name);
            _viewCache[name] = view;
        }

        _activeName = name;
        _activeView = view;
        _activeView.CommandChanged += ActiveView_CommandChanged;
        // Per-character lists follow the current target selection, so a view opened
        // after the selection changed still reflects it without a manual refresh.
        _activeView.OnTargetsChanged();
        ContentHost.Content = view;
        ContentScroll.ScrollToTop();
        TargetsCard.Visibility = view.NeedsGlobalTargets ? Visibility.Visible : Visibility.Collapsed;
        Recompute();
    }

    // ---------------- Sidebar search ----------------

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text.Trim();
        List<NavEntry> visible;

        if (query.Length == 0)
        {
            visible = Nav.ToList();
        }
        else
        {
            visible = new List<NavEntry>();
            NavEntry? pendingHeader = null;
            foreach (var entry in Nav)
            {
                if (entry.IsHeader)
                {
                    pendingHeader = entry;
                }
                else if (entry.Matches(query))
                {
                    if (pendingHeader != null)
                    {
                        visible.Add(pendingHeader);
                        pendingHeader = null;
                    }
                    visible.Add(entry);
                }
            }
        }

        var selected = CategoryList.SelectedItem as NavEntry;
        CategoryList.ItemsSource = visible;
        if (selected != null && visible.Contains(selected))
            CategoryList.SelectedItem = selected;
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var target = (CategoryList.ItemsSource as IEnumerable<NavEntry>)?.FirstOrDefault(n => !n.IsHeader);
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
            CopyToClipboard(OutputBox.Text);
            e.Handled = true;
        }
    }

    private ICommandCategoryView CreateView(string name) => name switch
    {
        "Change" => new ChangeView(_data, _chipPicker),
        "Outfit" => new OutfitView(_data, _chipPicker),
        "Inventory" => new InventoryView(_data),
        "Values" => new ValuesView(_data, _chipPicker),
        "Social" => new SocialView(_data),
        "Quest" => new QuestView(_data),
        "Door" => new DoorView(_data),
        "States" => new StatesView(_data, _chipPicker),
        "Properties" => new PropertiesView(_data, _chipPicker),
        "Run" => new RunView(_data, _chipPicker),
        "Movement" => new MovementView(_data),
        "Addforce" => new AddforceView(_chipPicker),
        "Misc" => new MiscView(_chipPicker),
        "Legacy (V1)" => new LegacyView(_data),
        "Intimacy" => CreateIntimacyView(),
        "Size" => new SizeView(_data, _chipPicker),
        "Time" => new TimeView(),
        _ => new PlaceholderView(name),
    };

    private IntimacyView CreateIntimacyView()
    {
        var view = new IntimacyView(_data);
        view.CopyRequested += (_, text) => CopyToClipboard(text);
        return view;
    }

    private void ActiveView_CommandChanged(object? sender, EventArgs e) => Recompute();

    private void Recompute()
    {
        if (_activeView == null)
            return;
        try
        {
            OutputBox.Text = _activeView.BuildCommand();
        }
        catch (Exception ex)
        {
            OutputBox.Text = $"({ex.Message})";
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e) => CopyToClipboard(OutputBox.Text);

    private void CopyToClipboard(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.StartsWith("("))
            return;
        try
        {
            Clipboard.SetText(text);
            PushHistory(text);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            MessageBox.Show(text, "Clipboard busy - copy manually", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void PushHistory(string text)
    {
        if (HistoryList.Items.Count > 0 && Equals(HistoryList.Items[0], text))
            return;
        HistoryList.Items.Insert(0, text);
        while (HistoryList.Items.Count > 10)
            HistoryList.Items.RemoveAt(HistoryList.Items.Count - 1);
    }

    private void HistoryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (HistoryList.SelectedItem is string s)
            CopyToClipboard(s);
    }
}
