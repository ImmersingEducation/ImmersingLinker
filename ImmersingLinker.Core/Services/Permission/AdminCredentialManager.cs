using System.Text.Json;
using ImmersingLinker.Core.Abstractions.AccessControl;
using ImmersingLinker.Core.Abstractions.Permission;
using ImmersingLinker.Core.Models.AccessControl;
using ImmersingLinker.Core.Models.Class;
using ImmersingLinker.Core.Models.Permission;

namespace ImmersingLinker.Core.Services.Permission;

public sealed class AdminCredentialManager
{
    private static readonly TimeSpan RotationInterval = TimeSpan.FromDays(7);
    private static readonly TimeSpan GracePeriod = TimeSpan.FromHours(1);
    private static readonly string AdminName = "AdminUI";

    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    private readonly string _credPath;
    private readonly IPermissionService _permissionService;
    private readonly IAccessControlService _accessControlService;

    public AdminCredentialManager(
        string credPath,
        IPermissionService permissionService,
        IAccessControlService accessControlService)
    {
        _credPath = credPath;
        _permissionService = permissionService;
        _accessControlService = accessControlService;
    }

    public AdminCredential? LoadFromFile()
    {
        if (!File.Exists(_credPath))
            return null;

        var json = File.ReadAllText(_credPath);
        return JsonSerializer.Deserialize<AdminCredential>(json);
    }

    public async Task<AdminCredential> EnsureValidAsync()
    {
        var current = LoadFromFile();

        if (current is null || DateTime.UtcNow >= current.ExpiresAt)
            return await RotateAsync(current);

        return current;
    }

    public async Task<AdminCredential> RotateAsync(AdminCredential? oldCred = null)
    {
        var newId = Guid.NewGuid().ToString();
        var newSecret = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var newCred = new AdminCredential(newId, newSecret, now, now + RotationInterval);

        var newApp = new RegisteredApp(
            new Application { UniqueId = newId, Name = AdminName },
            newSecret,
            now);

        _permissionService.Register(newApp);
        _accessControlService.AddToWhitelist(new AccessControlEntry(
            new Application { UniqueId = newId, Name = AdminName },
            null,
            now));

        await SaveToFile(newCred);

        if (oldCred is not null)
            ScheduleCleanup(oldCred.AppId);

        return newCred;
    }

    private void ScheduleCleanup(string oldAppId)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(GracePeriod);
            _permissionService.Unregister(oldAppId);
            _accessControlService.RemoveFromWhitelist(oldAppId);
            await _permissionService.SaveAsync();
            await _accessControlService.SaveAsync();
        });
    }

    private async Task SaveToFile(AdminCredential cred)
    {
        var dir = Path.GetDirectoryName(_credPath);
        if (dir is not null) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(cred, _jsonOptions);
        await File.WriteAllTextAsync(_credPath, json);
    }
}
