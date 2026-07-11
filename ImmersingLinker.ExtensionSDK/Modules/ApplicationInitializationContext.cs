namespace ImmersingLinker.ExtensionSDK.Modules;

public class ApplicationInitializationContext
{
    public ApplicationInitializationContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }
}