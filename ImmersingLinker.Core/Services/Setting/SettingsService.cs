using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using ImmersingLinker.Core.Abstractions.Storage;
using ImmersingLinker.Core.Models.Setting;

namespace ImmersingLinker.Core.Services.Setting;

public sealed class SettingsService
{
    private readonly ConcurrentDictionary<string, SettingsGroup> _mountedSettingsGroups = [];
    private readonly ISettingsStorageService _storageService;
    private bool _initialized;

    public SettingsService(ISettingsStorageService storageService)
    {
        _storageService = storageService;
    }

    public event EventHandler? AnyChanged;

    public IReadOnlyList<SettingsGroup> GetAllGroups()
    {
        return _mountedSettingsGroups.Values.ToList().AsReadOnly();
    }

    public SettingsGroup? GetGroupByKey(string key)
    {
        return _mountedSettingsGroups.TryGetValue(key, out var group) ? group : null;
    }

    public void MountSettingsGroup(SettingsGroup group)
    {
        if (!_mountedSettingsGroups.TryAdd(group.Key, group))
            throw new InvalidOperationException($"A settings group with key '{group.Key}' is already mounted.");
        group.ValueChanged += OnAnyChanged;
    }

    public void UnmountSettingsGroup(string key)
    {
        if (_mountedSettingsGroups.TryRemove(key, out var group))
            group.ValueChanged -= OnAnyChanged;
    }

    public SettingItemBase GetSettingItem(string[] keys)
    {
        SettingItemBase? item = null;
        foreach (var key in keys)
            item = item switch
            {
                null => _mountedSettingsGroups.TryGetValue(key, out var g) ? g : throw new KeyNotFoundException(),
                SettingsGroup g => g[key] ?? throw new KeyNotFoundException(),
                _ => throw new InvalidOperationException(
                    $"Cannot navigate into setting item of type '{item.GetType().Name}' (key: '{item.Key}'). Only SettingsGroup supports nested paths.")
            };

        return item;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        AnyChanged += SaveSettingsTrigger;
        await LoadSettingsAsync();
        ApplyDefaultValues();
        _initialized = true;
    }

    public async Task LoadSettingsAsync()
    {
        var data = await _storageService.LoadAsync();
        if (data is null)
            return;

        foreach (var (groupKey, items) in data)
        {
            _mountedSettingsGroups.TryGetValue(groupKey, out var group);
            if (group is null)
                continue;

            ApplySettingsToGroup(group, items);
        }
    }

    public async Task SaveSettingsAsync()
    {
        var data = new Dictionary<string, Dictionary<string, JsonElement>>();
        foreach (var (key, group) in _mountedSettingsGroups) data[key] = CollectGroupValues(group);

        await _storageService.SaveAsync(data);
    }

    private void OnAnyChanged(object? sender, EventArgs e)
    {
        AnyChanged?.Invoke(this, e);
    }

    private async void SaveSettingsTrigger(object? sender, EventArgs e)
    {
        await SaveSettingsAsync();
    }

    private void ApplyDefaultValues()
    {
        foreach (var group in _mountedSettingsGroups.Values) ApplyGroupDefaults(group);
    }

    private static void ApplyGroupDefaults(SettingsGroup group)
    {
        foreach (var item in group.SettingItems)
        {
            if (item is SettingsGroup subGroup)
            {
                ApplyGroupDefaults(subGroup);
                continue;
            }

            ApplyItemDefault(item);
        }
    }

    private static void ApplyItemDefault(SettingItemBase item)
    {
        var itemType = item.GetType();
        if (!itemType.IsGenericType || itemType.GetGenericTypeDefinition() != typeof(SettingItem<>))
            return;

        var (getter, setter) = SettingItemAccessor.GetOrCreateValueAccessors(itemType);
        if (getter is null || setter is null)
            return;

        var currentValue = getter(item);
        if (currentValue is not null)
            return;

        var defaultValueProp = itemType.GetProperty("DefaultValue", BindingFlags.Public | BindingFlags.Instance);
        if (defaultValueProp is null)
            return;

        var defaultValue = defaultValueProp.GetValue(item);
        if (defaultValue is not null)
            setter(item, defaultValue);
    }

    private static Dictionary<string, JsonElement> CollectGroupValues(SettingsGroup group)
    {
        var result = new Dictionary<string, JsonElement>();
        foreach (var item in group.SettingItems) CollectItemValue(item, result);
        return result;
    }

    private static void CollectItemValue(SettingItemBase item, Dictionary<string, JsonElement> result)
    {
        if (item is SettingsGroup subGroup)
        {
            foreach (var child in subGroup.SettingItems) CollectItemValue(child, result);
            return;
        }

        var (getter, _) = SettingItemAccessor.GetOrCreateValueAccessors(item.GetType());
        if (getter is null)
            return;

        var value = getter(item);
        if (value is not null)
            result[item.Key] = JsonSerializer.SerializeToElement(value, value.GetType());
    }

    private static void ApplySettingsToGroup(SettingsGroup group, Dictionary<string, JsonElement> items)
    {
        foreach (var item in group.SettingItems) ApplySettingValue(item, items);
    }

    private static void ApplySettingValue(SettingItemBase item, Dictionary<string, JsonElement> items)
    {
        if (item is SettingsGroup subGroup)
        {
            foreach (var child in subGroup.SettingItems) ApplySettingValue(child, items);
            return;
        }

        if (!items.TryGetValue(item.Key, out var element))
            return;

        var itemType = item.GetType();
        if (!itemType.IsGenericType || itemType.GetGenericTypeDefinition() != typeof(SettingItem<>))
            return;

        var valueType = itemType.GetGenericArguments()[0];
        var value = JsonSerializer.Deserialize(element.GetRawText(), valueType);
        var (_, setter) = SettingItemAccessor.GetOrCreateValueAccessors(itemType);
        setter?.Invoke(item, value);
    }
}