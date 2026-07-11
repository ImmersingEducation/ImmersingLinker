namespace ImmersingLinker.ExtensionSDK.Modules;

public record ModuleVersion(
    int              Major,
    int              Minor,
    int              Patch,
    ModuleVersionTag Tag
);

public enum ModuleVersionTag
{
    Develop,
    Beta,
    Stable
}