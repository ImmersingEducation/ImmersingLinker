namespace ImmersingLinker.ExtensionSDK.Modules;

public class ApplicationShutdownContext
{
    public ApplicationShutdownContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }
}