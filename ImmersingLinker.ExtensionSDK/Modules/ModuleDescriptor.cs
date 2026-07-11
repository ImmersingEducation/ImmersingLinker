namespace ImmersingLinker.ExtensionSDK.Modules;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ModuleDescriptor : Attribute
{
    public ModuleDescriptor(Guid guid,
        string name,
        string author, 
        string description, 
        ModuleType moduleType, 
        Version version)
    {
        Guid = guid;
        Name = name;
        Author = author;
        Description = description;
        ModuleType = moduleType;
        Version = version;
    }

    public Guid Guid { get; }
    public string Name { get; }
    public string Author { get; }
    public string Description { get; }
    public ModuleType ModuleType { get; }
    public Version Version { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class ControllerDescriptor : Attribute
{
    public ControllerDescriptor(Guid guid,
        string name,
        string description,
        Version version)
    {
        Guid = guid;
        Name = name;
        Description = description;
        Version = version;
    }
    
    public Guid Guid { get; }
    public string Name { get; }
    public string Description { get; }
    public Version Version { get; }
}