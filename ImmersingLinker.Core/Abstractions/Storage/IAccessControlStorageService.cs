using ImmersingLinker.Core.Models.AccessControl;

namespace ImmersingLinker.Core.Abstractions.Storage;

public interface IAccessControlStorageService : ISecureStorageService<AccessControlData>;
