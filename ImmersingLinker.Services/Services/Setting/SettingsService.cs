using System.Text.Json;
using ImmersingLinker.Core.Models.Setting;

namespace ImmersingLinker.Services.Services.Setting;

public sealed class SettingsService
{
    public static SettingsService Instance { get; } = new();
    
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private static readonly string _dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data", "Settings");

    public event EventHandler AnyChanged;
    
    private List<SettingsGroup> _mountedSettingsGroups;
    
    private SettingsService()
    {
        Directory.CreateDirectory(_dataDirectory);
        LoadSettings();
        AnyChanged += SaveSettingsTrigger;
    }

    public void OnAnyChanged(object? sender, EventArgs e)
    {
        AnyChanged?.Invoke(this, e);
    }

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

    public async Task LoadSettings()
    {
        throw new NotImplementedException();
    }

    public async Task SaveSettings()
    {
        throw new NotImplementedException();
    }

    public void SaveSettingsTrigger(object? sender, EventArgs e)
    {
        SaveSettings();
    }
}