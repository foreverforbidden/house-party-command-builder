using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HpCommander.Controls;

public partial class NumericStepper : UserControl
{
    private decimal _value;

    public event EventHandler? ValueChanged;

    public decimal Minimum { get; set; } = 0m;
    public decimal Maximum { get; set; } = 100m;
    public decimal Increment { get; set; } = 1m;
    public int DecimalPlaces { get; set; } = 0;

    public decimal Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, Minimum, Maximum);
            ValueBox.Text = _value.ToString(DecimalPlaces > 0 ? "F" + DecimalPlaces : "F0");
        }
    }

    public NumericStepper()
    {
        InitializeComponent();
    }

    private void CommitText()
    {
        if (decimal.TryParse(ValueBox.Text, out var parsed))
            Value = parsed;
        else
            ValueBox.Text = Value.ToString(DecimalPlaces > 0 ? "F" + DecimalPlaces : "F0");
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ValueBox_LostFocus(object sender, RoutedEventArgs e) => CommitText();

    private void ValueBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            CommitText();
    }

    private void UpButton_Click(object sender, RoutedEventArgs e)
    {
        Value += Increment;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private void DownButton_Click(object sender, RoutedEventArgs e)
    {
        Value -= Increment;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }
}
