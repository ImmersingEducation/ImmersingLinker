using System.Text.Json;
using ImmersingLinker.Core.Models;

namespace ImmersingLinker.Server.Services;

public class ClassStorageService
{
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    
    public async Task<List<ClassInfo>> GetClassInfos()
    {
        List<ClassInfo> infos = [];
        foreach (var guid in Directory.GetFiles("./Data/Classes", "*.json").Select(p => Path.GetFileNameWithoutExtension(p)))
        {
            infos.Add(new ClassInfo
            {
                Guid = Guid.Parse(guid),
                Name = GetClass(Guid.Parse(guid)).Result?.Name ?? string.Empty
            });
        }

        return infos;
    }

    public async Task<Class?> GetClass(Guid guid)
    {
        var path = $"./Data/Classes/{guid}.json";
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<Class>(await File.ReadAllTextAsync(path), _options);
    }

    public async Task SaveClass(Class @class)
    {
        await File.WriteAllTextAsync($"./Data/Classes/{@class.Guid}.json", JsonSerializer.Serialize(@class, _options));
    }

    public void DeleteClass(Guid guid)
    {
        var path = $"./Data/Classes/{guid}.json";
        if (File.Exists(path)) File.Delete(path);
    }
}