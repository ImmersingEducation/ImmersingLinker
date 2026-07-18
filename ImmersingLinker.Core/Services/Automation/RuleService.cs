using System.Reflection;
using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Attributes.Automation;

namespace ImmersingLinker.Core.Services.Automation;

public sealed class RuleService : IRuleService
{
    private readonly Dictionary<string, Type> _ruleTypes = [];

    public void RegisterRule(Type ruleType)
    {
        var attr = ruleType.GetCustomAttribute<RuleAttribute>(inherit: false);
        if (attr is null) return;

        _ruleTypes[attr.Key] = ruleType;
    }

    public Type? GetRule(string key)
    {
        return _ruleTypes.TryGetValue(key, out var type) ? type : null;
    }

    public bool UnregisterRule(string key)
    {
        return _ruleTypes.Remove(key);
    }

    public void ScanAssembly(Assembly assembly)
    {
        var ruleTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(Rule).IsAssignableFrom(t));

        foreach (var type in ruleTypes)
        {
            RegisterRule(type);
        }
    }
}
