using System.Text.Json;
using ImmersingLinker.Core.Abstractions.Storage;

namespace ImmersingLinker.Core.Services.Storage;

public sealed class SettingsStorageService : ISettingsStorageService
{
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private readonly string _filePath;

    public SettingsStorageService()
        : this(Path.Combine(AppContext.BaseDirectory, "Data", "Settings.json"))
    {
    }

    internal SettingsStorageService(string filePath)
    {
        _filePath = filePath;
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null)
            Directory.CreateDirectory(directory);
    }

    public async Task<Dictionary<string, Dictionary<string, JsonElement>>?> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return null;

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(json);
    }

    public async Task SaveAsync(Dictionary<string, Dictionary<string, JsonElement>> data)
    {
        var json = JsonSerializer.Serialize(data, _options);
        await File.WriteAllTextAsync(_filePath, json);
    }
}