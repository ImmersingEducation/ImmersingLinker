using System.Reflection;

namespace ImmersingLinker.Core.Abstractions.Automation;

public interface ITriggerService
{
    void RegisterTrigger(Type triggerType);
    Type? GetTrigger(string key);
    bool UnregisterTrigger(string key);
    void ScanAssembly(Assembly assembly);
}
