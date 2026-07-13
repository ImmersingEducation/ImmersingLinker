namespace ImmersingLinker.Core.Models.Setting;

public class SettingItem<T> : SettingItemBase 
{
    public T? DefaultValue { get; init; }

    public T? Value
    {
        get => field;
        set
        {
            if (Validator.Invoke(value))
            {
                var old = field;
                field = value;
                OnValueChanged(new SettingValueEventArg<T> { Key = Key, OldValue = old, NewValue = value });
            }
            else
            {

            }
        }
    }

    public Func<T?, bool> Validator { get; init; }
}