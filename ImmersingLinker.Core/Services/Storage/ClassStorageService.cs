using System.Text.Json;
using ImmersingLinker.Core.Abstractions.Storage;
using ImmersingLinker.Core.Models.Class;

namespace ImmersingLinker.Core.Services.Storage;

public sealed class ClassStorageService : IClassStorageService
{
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private static readonly string _dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data", "Classes");

    public static ClassStorageService Instance { get; } = new();

    public ClassStorageService()
    {
        Directory.CreateDirectory(_dataDirectory);
    }

    public async Task<List<ClassInfo>> GetInfos()
    {
        List<ClassInfo> infos = [];
        var dataDir = new DirectoryInfo(_dataDirectory);
        if (!dataDir.Exists) return infos;

        foreach (var guid in dataDir.GetFiles("*.json").Select(p => Path.GetFileNameWithoutExtension(p.Name)))
        {
            var parsedGuid = Guid.Parse(guid);
            var @class = await GetData(parsedGuid);
            infos.Add(new ClassInfo
            {
                Guid = parsedGuid,
                Name = @class?.Name ?? string.Empty
            });
        }

        return infos;
    }

    public async Task<Class?> GetData(Guid guid)
    {
        var path = Path.Combine(_dataDirectory, $"{guid}.json");
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<Class>(await File.ReadAllTextAsync(path), _options);
    }

    public async Task SaveData(Class @class)
    {
        var path = Path.Combine(_dataDirectory, $"{@class.Guid}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(@class, _options));
    }

    public void DeleteData(Guid guid)
    {
        var path = Path.Combine(_dataDirectory, $"{guid}.json");
        if (File.Exists(path)) File.Delete(path);
    }
}
