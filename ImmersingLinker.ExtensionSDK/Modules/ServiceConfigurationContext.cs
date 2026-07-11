using Microsoft.Extensions.DependencyInjection;

namespace ImmersingLinker.ExtensionSDK.Modules;

public class ServiceConfigurationContext
{
    public ServiceConfigurationContext(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }
}