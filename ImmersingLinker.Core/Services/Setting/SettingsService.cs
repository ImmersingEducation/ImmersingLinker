using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using ImmersingLinker.Core.Abstractions.Storage;
using ImmersingLinker.Core.Models.Setting;
using ImmersingLinker.Core.Services.Storage;

namespace ImmersingLinker.Core.Services.Setting;

public sealed class SettingsService
{
    private readonly ISettingsStorageService _storageService;
    private readonly List<SettingsGroup> _mountedSettingsGroups = [];
    private bool _initialized;

    public event EventHandler? AnyChanged;

    public SettingsService(ISettingsStorageService storageService)
    {
        _storageService = storageService;
    }

    public IReadOnlyList<SettingsGroup> GetAllGroups() => _mountedSettingsGroups.AsReadOnly();

    public SettingsGroup? GetGroupByKey(string key) =>
        _mountedSettingsGroups.Find(x => x.Key == key);

    public async Task MountSettingsGroup(SettingsGroup group)
    {
        _mountedSettingsGroups.Add(group);
        group.ValueChanged += OnAnyChanged;
    }

    public async Task UnmountSettingsGroup(string key)
    {
        var group = _mountedSettingsGroups.FirstOrDefault(x => x.Key == key);
        if (group is not null)
        {
            group.ValueChanged -= OnAnyChanged;
            _mountedSettingsGroups.Remove(group);
        }
    }

    public async Task<SettingItemBase> GetSettingItem(string[] keys)
    {
        SettingItemBase? item = null;
        foreach (var key in keys)
        {
            item = item switch
            {
                null => _mountedSettingsGroups.Find(x => x.Key == key) ?? throw new KeyNotFoundException(),
                SettingsGroup group => group[key] ?? throw new KeyNotFoundException(),
                _ => throw new NotImplementedException()
            };
        }

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
            var group = _mountedSettingsGroups.Find(g => g.Key == groupKey);
            if (group is null)
                continue;

            ApplySettingsToGroup(group, items);
        }
    }

    public async Task SaveSettingsAsync()
    {
        var data = new Dictionary<string, Dictionary<string, JsonElement>>();
        foreach (var group in _mountedSettingsGroups)
        {
            data[group.Key] = CollectGroupValues(group);
        }

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
        foreach (var group in _mountedSettingsGroups)
        {
            ApplyGroupDefaults(group);
        }
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

        var (getter, setter) = GetOrCreateValueAccessors(itemType);
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

    private static readonly ConcurrentDictionary<Type, (Func<object, object?>? Getter, Action<object, object?>? Setter)> _valueAccessors = new();

    private static (Func<object, object?>? Getter, Action<object, object?>? Setter) GetOrCreateValueAccessors(Type type)
    {
        return _valueAccessors.GetOrAdd(type, t =>
        {
            var prop = t.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            if (prop is null) return (null, null);

            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var instanceCast = Expression.Convert(instanceParam, t);
            var propertyAccess = Expression.Property(instanceCast, prop);

            var getter = Expression.Lambda<Func<object, object?>>(
                Expression.Convert(propertyAccess, typeof(object)), instanceParam).Compile();

            var valueParam = Expression.Parameter(typeof(object), "value");
            var setter = Expression.Lambda<Action<object, object?>>(
                Expression.Assign(propertyAccess, Expression.Convert(valueParam, prop.PropertyType)),
                instanceParam, valueParam).Compile();

            return (getter, setter);
        });
    }

    private static Dictionary<string, JsonElement> CollectGroupValues(SettingsGroup group)
    {
        var result = new Dictionary<string, JsonElement>();
        foreach (var item in group.SettingItems)
        {
            CollectItemValue(item, result);
        }
        return result;
    }

    private static void CollectItemValue(SettingItemBase item, Dictionary<string, JsonElement> result)
    {
        if (item is SettingsGroup subGroup)
        {
            foreach (var child in subGroup.SettingItems)
            {
                CollectItemValue(child, result);
            }
            return;
        }

        var (getter, _) = GetOrCreateValueAccessors(item.GetType());
        if (getter is null)
            return;

        var value = getter(item);
        if (value is not null)
            result[item.Key] = JsonSerializer.SerializeToElement(value, value.GetType());
    }

    private static void ApplySettingsToGroup(SettingsGroup group, Dictionary<string, JsonElement> items)
    {
        foreach (var item in group.SettingItems)
        {
            ApplySettingValue(item, items);
        }
    }

    private static void ApplySettingValue(SettingItemBase item, Dictionary<string, JsonElement> items)
    {
        if (item is SettingsGroup subGroup)
        {
            foreach (var child in subGroup.SettingItems)
            {
                ApplySettingValue(child, items);
            }
            return;
        }

        if (!items.TryGetValue(item.Key, out var element))
            return;

        var itemType = item.GetType();
        if (!itemType.IsGenericType || itemType.GetGenericTypeDefinition() != typeof(SettingItem<>))
            return;

        var valueType = itemType.GetGenericArguments()[0];
        var value = JsonSerializer.Deserialize(element.GetRawText(), valueType);
        var (_, setter) = GetOrCreateValueAccessors(itemType);
        setter?.Invoke(item, value);
    }
}
