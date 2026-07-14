namespace ImmersingLinker.Core.Models.Setting;

public abstract class SettingItemBase
{
    public required string Key { get; init; }
    public required string Name { get; init; }

    public event EventHandler ValueChanged;

    protected void OnValueChanged(EventArgs e) => ValueChanged?.Invoke(this, e);
}