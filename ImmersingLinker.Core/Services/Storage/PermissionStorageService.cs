using System.Text.Json;
using ImmersingLinker.Core.Abstractions.Storage;
using ImmersingLinker.Core.Models.Permission;

namespace ImmersingLinker.Core.Services.Storage;

public sealed class PermissionStorageService : IPermissionStorageService
{
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private readonly string _filePath;
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public PermissionStorageService()
        : this(Path.Combine(AppContext.BaseDirectory, "Data"))
    {
    }

    internal PermissionStorageService(string dataDir)
    {
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "RegisteredApps.json");
        (_key, _iv) = StorageKeyHelper.LoadOrGenerateKey(dataDir);
    }

    public async Task<PermissionData?> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return null;

        var encrypted = await File.ReadAllBytesAsync(_filePath);
        var decrypted = StorageKeyHelper.Decrypt(encrypted, _key, _iv);
        return JsonSerializer.Deserialize<PermissionData>(decrypted);
    }

    public async Task SaveAsync(PermissionData data)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(data, _options);
        var encrypted = StorageKeyHelper.Encrypt(json, _key, _iv);
        await File.WriteAllBytesAsync(_filePath, encrypted);
    }
}
