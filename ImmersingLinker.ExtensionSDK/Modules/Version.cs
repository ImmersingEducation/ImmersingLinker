namespace ImmersingLinker.ExtensionSDK.Modules;

public record Version(
    int              Major,
    int              Minor,
    int              Patch,
    VersionTag Tag
);

public enum VersionTag
{
    Develop,
    Beta,
    Stable
}