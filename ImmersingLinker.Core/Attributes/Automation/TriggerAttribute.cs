namespace ImmersingLinker.Core.Attributes.Automation;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class TriggerAttribute(string name) : Attribute
{
    public string Name { get; init; } = name;
}