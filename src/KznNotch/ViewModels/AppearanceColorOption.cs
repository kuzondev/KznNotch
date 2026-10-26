namespace KznNotch.ViewModels;

public sealed class AppearanceColorOption(string key, string label, string value) : ObservableObject
{
    private string _value = value;
    public string Key { get; } = key;
    public string Label { get; } = label;
    public string Value { get => _value; set { if (Set(ref _value, value)) { Raise(nameof(Swatch)); Raise(nameof(IsValid)); } } }
    public bool IsValid { get { try { return ColorConverter.ConvertFromString(Value) is Color; } catch { return false; } } }
    public Brush Swatch { get { try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(Value)); } catch { return Brushes.Transparent; } } }
}
