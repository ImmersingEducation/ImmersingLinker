using System.Reflection;

namespace ImmersingLinker.ExtensionSDK.Modules;

public abstract class ModuleManagerBase
{
    private readonly List<ModuleDescriptor> _descriptors = [];
    private readonly List<IAppModule> _instances = [];

    public IReadOnlyList<ModuleDescriptor> Modules => _descriptors.AsReadOnly();

    protected abstract void LoadModules();

    protected void AddModule<T>()
        where T : IAppModule, new()
    {
        AddModule(typeof(T), ModuleType.API);
    }

    protected void AddModule(Type moduleType)
    {
        AddModule(moduleType, ModuleType.API);
    }

    protected void AddModule(Type type, ModuleType moduleType)
    {
        var instance = (IAppModule)Activator.CreateInstance(type)!;
        _descriptors.Add(CreateDescriptor(type, moduleType));
        _instances.Add(instance);
    }

    
    
    protected void AddModules(Assembly assembly, ModuleType moduleType)
    {
        var moduleTypes = assembly.GetExportedTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IAppModule).IsAssignableFrom(t));

        foreach (var type in moduleTypes)
        {
            AddModule(type, moduleType);
        }
    }

    protected void AddModule(ModuleDescriptor descriptor, IAppModule instance)
    {
        _descriptors.Add(descriptor);
        _instances.Add(instance);
    }

    protected virtual ModuleDescriptor CreateDescriptor(Type moduleType)
    {
        return CreateDescriptor(moduleType, ModuleType.API);
    }

    protected virtual ModuleDescriptor CreateDescriptor(Type type, ModuleType moduleType)
    {
        return new ModuleDescriptor(Guid.NewGuid(), type.Name, moduleType);
    }

    public virtual void PreConfigureServices(ServiceConfigurationContext context)
    {
        foreach (var module in _instances)
        {
            module.PreConfigureServices(context);
        }
    }

    public virtual void ConfigureServices(ServiceConfigurationContext context)
    {
        foreach (var module in _instances)
        {
            module.ConfigureServices(context);
        }
    }

    public virtual void PostConfigureServices(ServiceConfigurationContext context)
    {
        foreach (var module in _instances)
        {
            module.PostConfigureServices(context);
        }
    }

    public virtual void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        foreach (var module in _instances)
        {
            module.OnApplicationInitialization(context);
        }
    }

    public virtual void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        for (var i = _instances.Count - 1; i >= 0; i--)
        {
            _instances[i].OnApplicationShutdown(context);
        }
    }

    public void Initialize()
    {
        _descriptors.Clear();
        _instances.Clear();
        LoadModules();
    }
}
