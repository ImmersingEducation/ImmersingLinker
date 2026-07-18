using System.Reflection;

namespace ImmersingLinker.Core.Abstractions.Automation;

public interface IActionService
{
    void RegisterAction(Type actionType);
    Type? GetAction(string key);
    bool UnregisterAction(string key);
    void ScanAssembly(Assembly assembly);
}
