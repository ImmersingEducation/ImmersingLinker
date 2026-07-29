using ImmersingLinker.Core.Models.Permission;

namespace ImmersingLinker.Core.Abstractions.Storage;

public interface IPermissionStorageService : ISecureStorageService<PermissionData>;
