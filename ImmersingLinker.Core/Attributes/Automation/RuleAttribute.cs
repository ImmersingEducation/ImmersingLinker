namespace ImmersingLinker.Core.Attributes.Automation;

[AttributeUsage(AttributeTargets.Class, AllowMultiple =  false, Inherited = false)]
public class RuleAttribute(string key, string name) : Attribute
{
    public string Key { get; init; } = key;
    public string Name { get; init; } = name;
}