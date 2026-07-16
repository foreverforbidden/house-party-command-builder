using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HpCommander.Controls;
using HpCommander.Data;
using HpCommander.Views;

namespace HpCommander;

public partial class MainWindow : Window
{
    private static readonly string[] CategoryNames =
    {
        "Change", "Outfit", "Inventory", "Values", "Quest", "States", "Properties",
        "Run", "Addforce", "Misc", "Legacy (V1)", "Intimacy", "Size", "Time",
    };

    private readonly GameData _data;
    private readonly CharacterChipPicker _chipPicker;
    private readonly Dictionary<string, ICommandCategoryView> _viewCache = new();
    private ICommandCategoryView? _activeView;

    public MainWindow()
    {
        InitializeComponent();
        _data = LoadGameData();

        _chipPicker = new CharacterChipPicker();
        _chipPicker.SetCharacters(_data.Characters);
        _chipPicker.SelectionChanged += (_, _) =>
        {
            if (_activeView?.NeedsGlobalTargets == true)
                Recompute();
        };
        ChipPickerHost.Content = _chipPicker;

        CategoryList.ItemsSource = CategoryNames;
        CategoryList.SelectedIndex = 0;
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
        if (CategoryList.SelectedItem is string name)
            ShowCategory(name);
    }

    private void ShowCategory(string name)
    {
        if (_activeView != null)
            _activeView.CommandChanged -= ActiveView_CommandChanged;

        if (!_viewCache.TryGetValue(name, out var view))
        {
            view = CreateView(name);
            _viewCache[name] = view;
        }

        _activeView = view;
        _activeView.CommandChanged += ActiveView_CommandChanged;
        ContentHost.Content = view;
        TargetsCard.Visibility = view.NeedsGlobalTargets ? Visibility.Visible : Visibility.Collapsed;
        Recompute();
    }

    private ICommandCategoryView CreateView(string name) => name switch
    {
        "Change" => new ChangeView(_data, _chipPicker),
        "Outfit" => new OutfitView(_data, _chipPicker),
        "Inventory" => new InventoryView(_data),
        "Values" => new ValuesView(_data, _chipPicker),
        "Quest" => new QuestView(_data),
        "States" => new StatesView(_data, _chipPicker),
        "Properties" => new PropertiesView(_data, _chipPicker),
        "Run" => new RunView(_data, _chipPicker),
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
