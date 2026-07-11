using System.Reflection;
using ImmersingLinker.ExtensionSDK.Modules;

namespace ImmersingLinker.Server;

public class ApiModuleManager : ModuleManagerBase
{
    protected override void LoadModules()
    {
        try
        {
            var extensionFiles = Directory.GetFiles(@"Extensions", "*.dll", SearchOption.TopDirectoryOnly);
            foreach (var extensionFile in extensionFiles)
            {
                var assembly = Assembly.LoadFrom(extensionFile);
                AddModules(assembly, ModuleType.API);
            }
        }
        catch (DirectoryNotFoundException e)
        {
        }
    }
}