namespace ImmersingLinker.Core.Models.Setting;

public class SettingValueEventArgs<T> : EventArgs
{
    public required string Key { get; init; }
    public required T? OldValue { get; init; }
    public required T? NewValue { get; init; }
}