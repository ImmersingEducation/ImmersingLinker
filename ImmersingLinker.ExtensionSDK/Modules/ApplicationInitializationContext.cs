using Microsoft.Extensions.DependencyInjection;

namespace ImmersingLinker.ExtensionSDK.Modules;

public class ApplicationInitializationContext
{
    public IServiceProvider ServiceProvider { get; }

    public ApplicationInitializationContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
}
