namespace ImmersingLinker.ExtensionSDK.Modules;

public abstract class AppModuleBase : IAppModule
{
    public virtual void PreConfigureServices(ServiceConfigurationContext context)
    {
    }

    public virtual void ConfigureServices(ServiceConfigurationContext context)
    {
    }

    public virtual void PostConfigureServices(ServiceConfigurationContext context)
    {
    }

    public virtual void OnApplicationInitialization(ApplicationInitializationContext context)
    {
    }

    public virtual void OnApplicationShutdown(ApplicationShutdownContext context)
    {
    }
}