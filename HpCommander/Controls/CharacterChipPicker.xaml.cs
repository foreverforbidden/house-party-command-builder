using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using HpCommander.Builders;

namespace HpCommander.Controls;

public partial class CharacterChipPicker : UserControl
{
    private readonly ObservableCollection<CharacterChipItem> _items = new();
    private readonly ICollectionView _view;

    public event EventHandler? SelectionChanged;

    public CharacterChipPicker()
    {
        InitializeComponent();
        _view = CollectionViewSource.GetDefaultView(_items);
        _view.Filter = FilterPredicate;
        ChipsControl.ItemsSource = _view;
    }

    public void SetCharacters(IEnumerable<string> characters)
    {
        foreach (var item in _items)
            item.PropertyChanged -= Item_PropertyChanged;
        _items.Clear();

        foreach (var name in characters)
        {
            var item = new CharacterChipItem(name);
            item.PropertyChanged += Item_PropertyChanged;
            _items.Add(item);
        }
    }

    public IReadOnlyList<string> GetSelectedTargets()
    {
        if (AllCharactersChip.IsChecked == true)
            return new[] { TargetHelper.AllCharactersTarget };
        return _items.Where(i => i.IsChecked).Select(i => i.Name).ToList();
    }

    public string? GetSingleSelectedCharacter()
    {
        var targets = GetSelectedTargets();
        return targets.Count == 1 && targets[0] != TargetHelper.AllCharactersTarget ? targets[0] : null;
    }

    private bool FilterPredicate(object obj)
    {
        if (obj is not CharacterChipItem item)
            return true;
        var filter = FilterBox.Text;
        return string.IsNullOrWhiteSpace(filter) || item.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e) => _view.Refresh();

    private void AllCharactersChip_Checked(object sender, RoutedEventArgs e)
    {
        foreach (var item in _items)
            item.IsEnabled = false;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AllCharactersChip_Unchecked(object sender, RoutedEventArgs e)
    {
        foreach (var item in _items)
            item.IsEnabled = true;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CharacterChipItem.IsChecked))
            SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
