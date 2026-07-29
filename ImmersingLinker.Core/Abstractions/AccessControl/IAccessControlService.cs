using ImmersingLinker.Core.Models.AccessControl;
using ImmersingLinker.Core.Models.Class;

namespace ImmersingLinker.Core.Abstractions.AccessControl;

public enum AccessCheckResult
{
    Allowed,
    Denied,
    GetOnly
}

public interface IAccessControlService
{
    AccessCheckResult CheckAccess(Application application, string? ipAddress, string httpMethod);

    List<AccessControlEntry> GetWhitelist();
    List<AccessControlEntry> GetBlacklist();

    void AddToWhitelist(AccessControlEntry entry);
    bool RemoveFromWhitelist(string applicationId);
    void AddToBlacklist(AccessControlEntry entry);
    bool RemoveFromBlacklist(string applicationId);

    Task LoadAsync();
    Task SaveAsync();
}
