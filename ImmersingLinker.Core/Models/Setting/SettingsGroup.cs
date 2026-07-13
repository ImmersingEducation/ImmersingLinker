namespace ImmersingLinker.Core.Models.Setting;

public class SettingsGroup : SettingItemBase
{
    public required IReadOnlyList<SettingItemBase> SettingItems { get; init; }

    public SettingsGroup(IReadOnlyList<SettingItemBase> settingItems)
    {
        List<string> keyList = [];
        foreach (var item in settingItems)
        {
            if (keyList.Contains(item.Key))
                throw new ArgumentException($"Key {item.Key} was set by several setting items.");
            keyList.Add(item.Key);
        }
        SettingItems = settingItems;
        foreach (var item in SettingItems)
        {
            item.ValueChanged += (s, e) => OnValueChanged(e);
        }
    }
    
    public SettingItemBase? this[string key] => SettingItems.FirstOrDefault(x => x.Key == key);
}