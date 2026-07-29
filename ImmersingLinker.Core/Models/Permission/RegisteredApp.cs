using ImmersingLinker.Core.Models.Class;

namespace ImmersingLinker.Core.Models.Permission;

public sealed record RegisteredApp(
    Application Application,
    string Secret,
    DateTime RegisteredAt);