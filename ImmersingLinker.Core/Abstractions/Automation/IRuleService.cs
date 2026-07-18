using System.Reflection;

namespace ImmersingLinker.Core.Abstractions.Automation;

public interface IRuleService
{
    void RegisterRule(Type ruleType);
    Type? GetRule(string key);
    bool UnregisterRule(string key);
    void ScanAssembly(Assembly assembly);
}
