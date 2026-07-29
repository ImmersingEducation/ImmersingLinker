using ImmersingLinker.Core.Models.Permission;

namespace ImmersingLinker.Core.Abstractions.Permission;

public interface IPermissionService
{
    RegisteredApp? GetByAppId(string appId);
    void Register(RegisteredApp app);
    bool Unregister(string appId);
    List<RegisteredApp> GetAll();
    Task LoadAsync();
    Task SaveAsync();
}
