namespace ImmersingLinker.ExtensionSDK.Modules;

public class ModuleDescriptor
{
    public Type Type { get; }
    public IAppModule Instance { get; }
    public string Name { get; }

    public ModuleDescriptor(Type type, IAppModule instance)
    {
        Type = type;
        Instance = instance;
        Name = type.Name;
    }
}
