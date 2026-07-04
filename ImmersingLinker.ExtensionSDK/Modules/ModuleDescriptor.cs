namespace ImmersingLinker.ExtensionSDK.Modules;

public class ModuleDescriptor
{
    public Guid Guid { get; }
    public string Name { get; }
    public ModuleType ModuleType { get; }

    public ModuleDescriptor(Guid guid, string name, ModuleType moduleType)
    {
        Guid = guid;
        Name = name;
        ModuleType = moduleType;
    }
}
