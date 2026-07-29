using ImmersingLinker.Core.Abstractions.Permission;
using ImmersingLinker.Core.Abstractions.Storage;
using ImmersingLinker.Core.Models.Permission;

namespace ImmersingLinker.Core.Services.Permission;

public sealed class PermissionService : IPermissionService
{
    private readonly IPermissionStorageService _storage;
    private Dictionary<string, RegisteredApp> _apps = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public PermissionService(IPermissionStorageService storage)
    {
        _storage = storage;
    }

    public RegisteredApp? GetByAppId(string appId)
    {
        lock (_lock)
            return _apps.GetValueOrDefault(appId);
    }

    public void Register(RegisteredApp app)
    {
        lock (_lock)
            _apps[app.Application.UniqueId] = app;
    }

    public bool Unregister(string appId)
    {
        lock (_lock)
            return _apps.Remove(appId);
    }

    public List<RegisteredApp> GetAll()
    {
        lock (_lock)
            return [.. _apps.Values];
    }

    public async Task LoadAsync()
    {
        var loaded = await _storage.LoadAsync();
        if (loaded is not null)
        {
            lock (_lock)
                _apps = loaded.Apps.ToDictionary(
                    a => a.Application.UniqueId,
                    a => a,
                    StringComparer.OrdinalIgnoreCase);
        }
    }

    public async Task SaveAsync()
    {
        PermissionData snapshot;
        lock (_lock)
            snapshot = new PermissionData { Apps = [.. _apps.Values] };
        await _storage.SaveAsync(snapshot);
    }
}
