namespace ImmersingLinker.Core.Attributes.Automation;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class TriggerAttribute(string key, string name) : Attribute
{
    public string Key { get; init; } = key;
    public string Name { get; init; } = name;
}