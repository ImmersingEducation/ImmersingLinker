using System.Reflection;

namespace ImmersingLinker.ExtensionSDK.Modules;

public abstract class ModuleManagerBase
{
    private readonly List<ModuleDescriptor> _modules = [];

    public IReadOnlyList<ModuleDescriptor> Modules => _modules.AsReadOnly();

    protected abstract void LoadModules();

    protected void AddModule<T>()
        where T : IAppModule, new()
    {
        AddModule(typeof(T));
    }

    protected void AddModule(Type moduleType)
    {
        var instance = (IAppModule)Activator.CreateInstance(moduleType)!;
        _modules.Add(new ModuleDescriptor(moduleType, instance));
    }

    protected void AddModules(Assembly assembly)
    {
        var moduleTypes = assembly.GetExportedTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IAppModule).IsAssignableFrom(t));

        foreach (var type in moduleTypes)
        {
            AddModule(type);
        }
    }

    public virtual void PreConfigureServices(ServiceConfigurationContext context)
    {
        foreach (var module in _modules)
        {
            module.Instance.PreConfigureServices(context);
        }
    }

    public virtual void ConfigureServices(ServiceConfigurationContext context)
    {
        foreach (var module in _modules)
        {
            module.Instance.ConfigureServices(context);
        }
    }

    public virtual void PostConfigureServices(ServiceConfigurationContext context)
    {
        foreach (var module in _modules)
        {
            module.Instance.PostConfigureServices(context);
        }
    }

    public virtual void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        foreach (var module in _modules)
        {
            module.Instance.OnApplicationInitialization(context);
        }
    }

    public virtual void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        for (var i = _modules.Count - 1; i >= 0; i--)
        {
            _modules[i].Instance.OnApplicationShutdown(context);
        }
    }

    public void Initialize()
    {
        _modules.Clear();
        LoadModules();
    }
}
