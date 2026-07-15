using System.ComponentModel;

namespace HpCommander.Controls;

public sealed class CharacterChipItem : INotifyPropertyChanged
{
    private bool _isChecked;
    private bool _isEnabled = true;

    public string Name { get; }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value)
                return;
            _isChecked = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
                return;
            _isEnabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CharacterChipItem(string name) => Name = name;
}
