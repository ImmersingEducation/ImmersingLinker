using ImmersingLinker.Core.Abstractions.AccessControl;
using ImmersingLinker.Core.Abstractions.Storage;
using ImmersingLinker.Core.Models.AccessControl;
using ImmersingLinker.Core.Models.Class;

namespace ImmersingLinker.Core.Services.AccessControl;

public sealed class AccessControlService : IAccessControlService
{
    private readonly IAccessControlStorageService _storage;
    private AccessControlData _data = new();
    private readonly object _lock = new();

    public AccessControlService(IAccessControlStorageService storage)
    {
        _storage = storage;
    }

    public AccessCheckResult CheckAccess(Application application, string? ipAddress, string httpMethod)
    {
        var id = application.UniqueId;

        lock (_lock)
        {
            if (_data.Blacklist.Any(e => Matches(e, id, ipAddress)))
                return AccessCheckResult.Denied;

            if (_data.Whitelist.Any(e => Matches(e, id, ipAddress)))
                return AccessCheckResult.Allowed;
        }

        if (string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(httpMethod, "HEAD", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(httpMethod, "OPTIONS", StringComparison.OrdinalIgnoreCase))
            return AccessCheckResult.GetOnly;

        return AccessCheckResult.Denied;
    }

    private static bool Matches(AccessControlEntry entry, string applicationId, string? ipAddress)
    {
        if (!string.Equals(entry.Application.UniqueId, applicationId, StringComparison.OrdinalIgnoreCase))
            return false;

        return entry.IpAddress is null ||
               string.Equals(entry.IpAddress, ipAddress, StringComparison.OrdinalIgnoreCase);
    }

    public List<AccessControlEntry> GetWhitelist()
    {
        lock (_lock) return [.. _data.Whitelist];
    }

    public List<AccessControlEntry> GetBlacklist()
    {
        lock (_lock) return [.. _data.Blacklist];
    }

    public void AddToWhitelist(AccessControlEntry entry)
    {
        lock (_lock)
        {
            _data.Whitelist.RemoveAll(e => e.Application.UniqueId == entry.Application.UniqueId);
            _data.Whitelist.Add(entry);
        }
    }

    public bool RemoveFromWhitelist(string applicationId)
    {
        lock (_lock)
        {
            var count = _data.Whitelist.RemoveAll(e => e.Application.UniqueId == applicationId);
            return count > 0;
        }
    }

    public void AddToBlacklist(AccessControlEntry entry)
    {
        lock (_lock)
        {
            _data.Blacklist.RemoveAll(e => e.Application.UniqueId == entry.Application.UniqueId);
            _data.Blacklist.Add(entry);
        }
    }

    public bool RemoveFromBlacklist(string applicationId)
    {
        lock (_lock)
        {
            var count = _data.Blacklist.RemoveAll(e => e.Application.UniqueId == applicationId);
            return count > 0;
        }
    }

    public async Task LoadAsync()
    {
        var loaded = await _storage.LoadAsync();
        if (loaded is not null)
        {
            lock (_lock) _data = loaded;
        }
    }

    public async Task SaveAsync()
    {
        AccessControlData snapshot;
        lock (_lock) snapshot = _data;
        await _storage.SaveAsync(snapshot);
    }
}
