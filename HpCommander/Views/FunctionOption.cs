namespace HpCommander.Views;

/// <summary>
/// One entry in the Run view's function dropdown, tagged with the heading it sits under.
/// </summary>
/// <remarks>
/// Public, and a plain property rather than a field, because <c>PropertyGroupDescription</c>
/// reaches <see cref="Group"/> by reflection - WPF cannot see an internal type's members.
/// <see cref="ToString"/> is what makes the editable ComboBox, its filter and
/// <c>FilteringComboBox.EffectiveValue</c> all keep working unchanged against a non-string item.
/// </remarks>
public sealed record FunctionOption(string Name, string Group)
{
    public override string ToString() => Name;
}
