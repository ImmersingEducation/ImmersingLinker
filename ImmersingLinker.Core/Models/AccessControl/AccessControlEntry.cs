using ImmersingLinker.Core.Models.Class;

namespace ImmersingLinker.Core.Models.AccessControl;

public sealed record AccessControlEntry(
    Application Application,
    string? IpAddress,
    DateTime CreatedAt);
