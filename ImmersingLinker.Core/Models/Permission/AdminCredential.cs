namespace ImmersingLinker.Core.Models.Permission;

public sealed record AdminCredential(
    string AppId,
    string Secret,
    DateTime CreatedAt,
    DateTime ExpiresAt);
