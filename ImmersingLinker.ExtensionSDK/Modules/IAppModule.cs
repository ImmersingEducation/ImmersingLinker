namespace ImmersingLinker.ExtensionSDK.Modules;

public interface IAppModule
{
    void PreConfigureServices(ServiceConfigurationContext context);
    void ConfigureServices(ServiceConfigurationContext context);
    void PostConfigureServices(ServiceConfigurationContext context);
    void OnApplicationInitialization(ApplicationInitializationContext context);
    void OnApplicationShutdown(ApplicationShutdownContext context);
}