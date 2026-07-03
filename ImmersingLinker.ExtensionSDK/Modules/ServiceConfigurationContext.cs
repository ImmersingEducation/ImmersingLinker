using Microsoft.Extensions.DependencyInjection;

namespace ImmersingLinker.ExtensionSDK.Modules;

public class ServiceConfigurationContext
{
    public IServiceCollection Services { get; }

    public ServiceConfigurationContext(IServiceCollection services)
    {
        Services = services;
    }
}
