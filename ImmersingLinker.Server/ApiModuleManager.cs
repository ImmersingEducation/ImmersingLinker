using System.Reflection;
using ImmersingLinker.ExtensionSDK.Modules;

namespace ImmersingLinker.Server;

public class ApiModuleManager : ModuleManagerBase
{
    protected override void LoadModules()
    {
        try
        {
            foreach (var extensionFile in ScanExtensionFiles())
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