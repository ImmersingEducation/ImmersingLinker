using System.Text.Json;
using ImmersingLinker.Core.Models.Class;

namespace ImmersingLinker.Services.Services.Storage;

public sealed class ClassStorageService
{
    public static ClassStorageService Instance { get; } = new();
    
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private static readonly string _dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data", "Classes");

    public ClassStorageService()
    {
        Directory.CreateDirectory(_dataDirectory);
    }

    public async Task<List<ClassInfo>> GetClassInfos()
    {
        List<ClassInfo> infos = [];
        var dataDir = new DirectoryInfo(_dataDirectory);
        if (!dataDir.Exists) return infos;

        foreach (var guid in dataDir.GetFiles("*.json").Select(p => Path.GetFileNameWithoutExtension(p.Name)))
            infos.Add(new ClassInfo
            {
                Guid = Guid.Parse(guid),
                Name = GetClass(Guid.Parse(guid)).Result?.Name ?? string.Empty
            });

        return infos;
    }

    public async Task<Class?> GetClass(Guid guid)
    {
        var path = Path.Combine(_dataDirectory, $"{guid}.json");
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<Class>(await File.ReadAllTextAsync(path), _options);
    }

    public async Task SaveClass(Class @class)
    {
        var path = Path.Combine(_dataDirectory, $"{@class.Guid}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(@class, _options));
    }

    public void DeleteClass(Guid guid)
    {
        var path = Path.Combine(_dataDirectory, $"{guid}.json");
        if (File.Exists(path)) File.Delete(path);
    }
}