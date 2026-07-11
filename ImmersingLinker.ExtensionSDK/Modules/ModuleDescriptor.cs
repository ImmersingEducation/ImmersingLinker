namespace ImmersingLinker.ExtensionSDK.Modules;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ModuleDescriptor : Attribute
{
    public ModuleDescriptor(Guid guid,
                            string name, 
                            string author,
                            string description,
                            ModuleType moduleType, 
                            ModuleVersion version)
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
    public ModuleVersion Version { get; }
}